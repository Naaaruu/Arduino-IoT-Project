using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

public sealed class ServerEndPoint : IAsyncDisposable
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        int port = 9000;

        if (args.Length > 0 && (!int.TryParse(args[0], out port) || port is < 1 or > 65535))
        {
            Console.WriteLine("Invalid port number. Usage: IoTServer.exe [port]");
            return;
        }

        await using ServerEndPoint server = new(port);
        using CancellationTokenSource shutdown = new();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdown.Cancel();
        };

        server.LogReceived += Console.WriteLine;

        try
        {
            await server.StartAsync(shutdown.Token);

            Console.WriteLine("Waiting for clients...");
            Console.WriteLine("Press Ctrl+C to stop the server.");

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, shutdown.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }
        finally
        {
            await server.StopAsync();
        }
    }
    
    private readonly IPAddress _ipAddress;
    private readonly int _port;
    private readonly ConcurrentDictionary<Guid, ClientSession> _clients = new();
    private readonly SemaphoreSlim _arduinoLock = new(1, 1);

    private TcpListener? _listener;
    private CancellationTokenSource? _serverCancellation;
    private Task? _acceptLoopTask;
    private ClientSession? _arduino;
    private string? _lastRadarMessage;
    private string? _lastAlertMessage;
    private string? _lastStatusMessage;

    public ServerEndPoint(int port = 9000)
        : this(IPAddress.Any, port)
    {
    }

    public ServerEndPoint(IPAddress ipAddress, int port = 9000)
    {
        _ipAddress = ipAddress;
        _port = port;
    }

    public event Action<string>? LogReceived;

    public bool IsRunning => _listener is not null;

    public int Port => _port;

    public string? LastRadarMessage => _lastRadarMessage;

    public string? LastAlertMessage => _lastAlertMessage;

    public string? LastStatusMessage => _lastStatusMessage;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listener is not null)
        {
            throw new InvalidOperationException("Server is already running.");
        }

        _serverCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new TcpListener(_ipAddress, _port);
        _listener.Start();

        Log("==================================");
        Log("IoT Server Started");
        Log($" Port: {_port}");
        Log("==================================");

        _acceptLoopTask = AcceptLoopAsync(_serverCancellation.Token);
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_listener is null)
        {
            return;
        }

        _serverCancellation?.Cancel();
        _listener.Stop();

        if (_acceptLoopTask is not null)
        {
            try
            {
                await _acceptLoopTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        foreach (ClientSession client in _clients.Values)
        {
            await RemoveClientAsync(client);
        }

        _listener = null;
        _acceptLoopTask = null;
        _serverCancellation?.Dispose();
        _serverCancellation = null;

        Log("IoT Server stopped.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _arduinoLock.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        TcpListener listener = _listener ?? throw new InvalidOperationException("Server is not running.");

        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient tcpClient;

            try
            {
                tcpClient = await listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            ClientSession client = new(tcpClient);
            _clients[client.Id] = client;

            Log($"Client connected: {client.RemoteEndPoint}");
            _ = Task.Run(() => HandleClientAsync(client, cancellationToken), CancellationToken.None);
        }
    }

    private async Task HandleClientAsync(ClientSession client, CancellationToken cancellationToken)
    {
        try
        {
            await client.SendAsync("STATUS:SERVER_READY", cancellationToken);

            while (!cancellationToken.IsCancellationRequested && client.TcpClient.Connected)
            {
                string? message = await client.Reader.ReadLineAsync(cancellationToken);

                if (message is null)
                {
                    break;
                }

                message = message.Trim();

                if (message.Length == 0)
                {
                    continue;
                }

                Log($"Receive ({client.RemoteEndPoint}): {message}");
                await HandleMessageAsync(client, message, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException ex)
        {
            Log($"Client communication closed: {ex.Message}");
        }
        catch (SocketException ex)
        {
            Log($"Socket error: {ex.Message}");
        }
        finally
        {
            await RemoveClientAsync(client);
        }
    }

    private async Task HandleMessageAsync(ClientSession sender, string message, CancellationToken cancellationToken)
    {
        string command = message.ToUpperInvariant();

        if (command is "ROLE:ARDUINO" or "ARDUINO")
        {
            await RegisterArduinoAsync(sender, cancellationToken);
            return;
        }

        if (command is "ROLE:CLIENT" or "ROLE:APP" or "ROLE:MONITOR" or "APP" or "MONITOR")
        {
            sender.Role = ClientRole.Client;
            await sender.SendAsync("ROLE:CLIENT:OK", cancellationToken);
            await SendCurrentStateAsync(sender, cancellationToken);
            return;
        }

        if (IsArduinoBroadcastMessage(command))
        {
            sender.Role = sender.Role == ClientRole.Unknown ? ClientRole.Arduino : sender.Role;
            await BroadcastToClientsAsync(message, cancellationToken);
            SaveDeviceState(message);
            return;
        }

        if (IsClientCommand(command))
        {
            await ForwardCommandToArduinoAsync(sender, command, cancellationToken);
            return;
        }

        if (command is "PING")
        {
            await sender.SendAsync("PONG", cancellationToken);
            return;
        }

        await sender.SendAsync($"ERROR:UNKNOWN_COMMAND:{message}", cancellationToken);
    }

    private async Task RegisterArduinoAsync(ClientSession client, CancellationToken cancellationToken)
    {
        await _arduinoLock.WaitAsync(cancellationToken);

        try
        {
            if (_arduino is not null && _arduino.Id != client.Id)
            {
                _arduino.Role = ClientRole.Client;
                await _arduino.SendAsync("ROLE:ARDUINO:REPLACED", cancellationToken);
            }

            _arduino = client;
            client.Role = ClientRole.Arduino;
        }
        finally
        {
            _arduinoLock.Release();
        }

        await client.SendAsync("ROLE:ARDUINO:OK", cancellationToken);
        Log($"Arduino registered: {client.RemoteEndPoint}");
    }

    private async Task ForwardCommandToArduinoAsync(ClientSession sender, string command, CancellationToken cancellationToken)
    {
        ClientSession? arduino = _arduino;

        if (arduino is null || !_clients.ContainsKey(arduino.Id))
        {
            await sender.SendAsync("ERROR:ARDUINO_NOT_CONNECTED", cancellationToken);
            return;
        }

        await arduino.SendAsync(command, cancellationToken);
        Log($"Forward to Arduino: {command}");
    }

    private async Task SendCurrentStateAsync(ClientSession client, CancellationToken cancellationToken)
    {
        if (_lastRadarMessage is not null)
        {
            await client.SendAsync(_lastRadarMessage, cancellationToken);
        }

        if (_lastAlertMessage is not null)
        {
            await client.SendAsync(_lastAlertMessage, cancellationToken);
        }

        if (_lastStatusMessage is not null)
        {
            await client.SendAsync(_lastStatusMessage, cancellationToken);
        }
    }

    private void SaveDeviceState(string message)
    {
        string command = message.ToUpperInvariant();

        if (command.StartsWith("RADAR:", StringComparison.Ordinal))
        {
            _lastRadarMessage = message;
        }
        else if (command is "ALERT:ON" or "ALERT:OFF")
        {
            _lastAlertMessage = message;
        }
        else if (command.StartsWith("STATUS:", StringComparison.Ordinal))
        {
            _lastStatusMessage = message;
        }
    }

    private async Task BroadcastToClientsAsync(string message, CancellationToken cancellationToken)
    {
        foreach (ClientSession client in _clients.Values.Where(static client => client.Role == ClientRole.Client))
        {
            try
            {
                await client.SendAsync(message, cancellationToken);
            }
            catch (IOException)
            {
                await RemoveClientAsync(client);
            }
            catch (SocketException)
            {
                await RemoveClientAsync(client);
            }
        }

        Log($"Send: {message}");
    }

    private async Task RemoveClientAsync(ClientSession client)
    {
        if (!_clients.TryRemove(client.Id, out _))
        {
            return;
        }

        await _arduinoLock.WaitAsync();

        try
        {
            if (_arduino?.Id == client.Id)
            {
                _arduino = null;
                Log("Arduino disconnected.");
            }
        }
        finally
        {
            _arduinoLock.Release();
        }

        client.Dispose();
        Log($"Client disconnected: {client.RemoteEndPoint}");
    }

    private static bool IsArduinoBroadcastMessage(string command)
    {
        return command.StartsWith("RADAR:", StringComparison.Ordinal)
            || command is "ALERT:ON" or "ALERT:OFF"
            || command.StartsWith("STATUS:", StringComparison.Ordinal);
    }

    private static bool IsClientCommand(string command)
    {
        return command is "CMD:ALLOW" or "CMD:WARN" or "CMD:RESET";
    }

    private void Log(string message)
    {
        LogReceived?.Invoke(message);
    }

    private sealed class ClientSession : IDisposable
    {
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public ClientSession(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
            NetworkStream stream = tcpClient.GetStream();
            Reader = new StreamReader(stream, Encoding.UTF8);
            Writer = new StreamWriter(stream, new UTF8Encoding(false))
            {
                AutoFlush = true
            };
            RemoteEndPoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
        }

        public Guid Id { get; } = Guid.NewGuid();

        public TcpClient TcpClient { get; }

        public StreamReader Reader { get; }

        public StreamWriter Writer { get; }

        public string RemoteEndPoint { get; }

        public ClientRole Role { get; set; } = ClientRole.Unknown;

        public async Task SendAsync(string message, CancellationToken cancellationToken)
        {
            await _writeLock.WaitAsync(cancellationToken);

            try
            {
                await Writer.WriteLineAsync(message.AsMemory(), cancellationToken);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Dispose()
        {
            _writeLock.Dispose();
            Reader.Dispose();
            Writer.Dispose();
            TcpClient.Close();
            TcpClient.Dispose();
        }
    }

    private enum ClientRole
    {
        Unknown,
        Arduino,
        Client
    }
}
