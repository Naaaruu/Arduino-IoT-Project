using System;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class SerialTcpBridge
{
    private const string ComPort = "COM4";
    private const int BaudRate = 9600;
    private const string ServerIp = "127.0.0.1";
    private const int ServerPort = 9000;
    private const int RetryDelayMs = 3000;

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        using CancellationTokenSource cts = new();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("==================================");
        Console.WriteLine(" Arduino Serial-TCP Bridge Started");
        Console.WriteLine("==================================");

        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                await RunBridgeOnceAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bridge Error] {ex.Message}");
            }

            if (!cts.Token.IsCancellationRequested)
            {
                Console.WriteLine($"[Bridge] Reconnecting in {RetryDelayMs}ms...");
                try
                {
                    await Task.Delay(RetryDelayMs, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        Console.WriteLine("Bridge stopped.");
    }

    private static async Task RunBridgeOnceAsync(CancellationToken token)
    {
        using SerialPort serialPort = new(ComPort, BaudRate)
        {
            NewLine = "\n",
            ReadTimeout = 500
        };

        serialPort.Open();
        Console.WriteLine($"[Serial] Connected to Arduino on {ComPort} at {BaudRate} baud");

        using TcpClient tcpClient = new();
        await tcpClient.ConnectAsync(ServerIp, ServerPort, token);
        Console.WriteLine($"[TCP] Connected to Server at {ServerIp}:{ServerPort}");

        using NetworkStream stream = tcpClient.GetStream();
        using StreamReader tcpReader = new(stream, Encoding.UTF8);
        using StreamWriter tcpWriter = new(stream, new UTF8Encoding(false)) { AutoFlush = true };

        await tcpWriter.WriteLineAsync("ROLE:ARDUINO".AsMemory(), token);
        Console.WriteLine("[Bridge -> Server] ROLE:ARDUINO");

        Task serialToTcpTask = Task.Run(() => BridgeSerialToTcp(serialPort, tcpWriter, token), token);
        Task tcpToSerialTask = Task.Run(() => BridgeTcpToSerial(tcpReader, serialPort, token), token);

        Console.WriteLine("Bridge is running. Press Ctrl+C to stop.");

        await Task.WhenAny(serialToTcpTask, tcpToSerialTask);
    }

    private static async Task BridgeSerialToTcp(SerialPort serialPort, StreamWriter tcpWriter, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (serialPort.BytesToRead > 0)
                {
                    string message = serialPort.ReadLine().Trim();
                    if (!string.IsNullOrEmpty(message))
                    {
                        Console.WriteLine($"[Arduino -> Server] {message}");
                        await tcpWriter.WriteLineAsync(message.AsMemory(), token);
                    }
                }

                await Task.Delay(10, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (TimeoutException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Serial Read Error] {ex.Message}");
        }
    }

    private static async Task BridgeTcpToSerial(StreamReader tcpReader, SerialPort serialPort, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                string? message = await tcpReader.ReadLineAsync(token);
                if (message == null)
                {
                    Console.WriteLine("[TCP] Server disconnected.");
                    break;
                }

                message = message.Trim();
                if (!string.IsNullOrEmpty(message))
                {
                    if (message.StartsWith("CMD:", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[Server -> Arduino] {message}");
                        serialPort.WriteLine(message);
                    }
                    else
                    {
                        Console.WriteLine($"[Server] {message}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TCP Read Error] {ex.Message}");
        }
    }
}
