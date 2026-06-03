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
    private bool _isLedOn;
    private int? _lastLdrValue;

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

    public bool IsLedOn => _isLedOn;

    public int? LastLdrValue => _lastLdrValue;

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
            await client.SendAsync("SERVER:READY", cancellationToken);
            await SendCurrentStateAsync(client, cancellationToken);

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

        if (command is "ROLE:APP" or "ROLE:MONITOR" or "APP" or "MONITOR")
        {
            sender.Role = ClientRole.App;
            await sender.SendAsync("ROLE:APP:OK", cancellationToken);
            return;
        }

        if (TryParseLdrValue(message, out int ldrValue))
        {
            sender.Role = sender.Role == ClientRole.Unknown ? ClientRole.Arduino : sender.Role;
            _lastLdrValue = ldrValue;
            await BroadcastAsync($"LDR:{ldrValue}", cancellationToken);
            return;
        }

        if (TryParseLedCommand(command, out bool ledOn))
        {
            await ChangeLedStateAsync(ledOn, cancellationToken);
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
                _arduino.Role = ClientRole.App;
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
        await client.SendAsync(_isLedOn ? "LED_ON" : "LED_OFF", cancellationToken);
        Log($"Arduino registered: {client.RemoteEndPoint}");
    }

    private async Task ChangeLedStateAsync(bool ledOn, CancellationToken cancellationToken)
    {
        _isLedOn = ledOn;

        string arduinoCommand = ledOn ? "LED_ON" : "LED_OFF";
        string stateMessage = ledOn ? "LED:ON" : "LED:OFF";

        ClientSession? arduino = _arduino;

        if (arduino is not null && _clients.ContainsKey(arduino.Id))
        {
            await arduino.SendAsync(arduinoCommand, cancellationToken);
        }

        await BroadcastAsync(stateMessage, cancellationToken);
        Log($"LED state changed: {(ledOn ? "ON" : "OFF")}");
    }

    private async Task SendCurrentStateAsync(ClientSession client, CancellationToken cancellationToken)
    {
        await client.SendAsync(_isLedOn ? "LED:ON" : "LED:OFF", cancellationToken);

        if (_lastLdrValue.HasValue)
        {
            await client.SendAsync($"LDR:{_lastLdrValue.Value}", cancellationToken);
        }
    }

    private async Task BroadcastAsync(string message, CancellationToken cancellationToken)
    {
        foreach (ClientSession client in _clients.Values)
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

    private static bool TryParseLedCommand(string command, out bool ledOn)
    {
        switch (command)
        {
            case "LED_ON":
            case "LED:ON":
            case "LED=ON":
                ledOn = true;
                return true;

            case "LED_OFF":
            case "LED:OFF":
            case "LED=OFF":
                ledOn = false;
                return true;

            default:
                ledOn = false;
                return false;
        }
    }

    private static bool TryParseLdrValue(string message, out int value)
    {
        value = 0;

        string[] parts = message.Split(':', StringSplitOptions.TrimEntries);

        if (parts.Length != 2 || !parts[0].Equals("LDR", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(parts[1], out value);
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
        App
    }
}
