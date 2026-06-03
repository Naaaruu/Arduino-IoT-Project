using System;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class SerialTcpBridge
{
    // 아두이노가 연결된 실제 COM 포트로 변경해야 합니다. (예: COM3, COM4)
    private const string ComPort = "COM4"; 
    private const int BaudRate = 9600;

    // ServerProgram.cs가 대기 중인 주소와 포트
    private const string ServerIp = "127.0.0.1";
    private const int ServerPort = 9000;

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        using CancellationTokenSource cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("==================================");
        Console.WriteLine(" Arduino Serial-TCP Bridge Started");
        Console.WriteLine("==================================");

        try
        {
            // 1. 시리얼 포트 연결 (아두이노)
            using SerialPort serialPort = new SerialPort(ComPort, BaudRate);
            serialPort.NewLine = "\n"; // 아두이노의 println()에 맞춤
            serialPort.Open();
            Console.WriteLine($"[Serial] Connected to Arduino on {ComPort}");

            // 2. TCP 클라이언트 연결 (C# 서버)
            using TcpClient tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ServerIp, ServerPort, cts.Token);
            Console.WriteLine($"[TCP] Connected to Server at {ServerIp}:{ServerPort}");

            using NetworkStream stream = tcpClient.GetStream();
            using StreamReader tcpReader = new StreamReader(stream, Encoding.UTF8);
            using StreamWriter tcpWriter = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            // 3. 양방향 데이터 중계 태스크 실행
            Task serialToTcpTask = Task.Run(() => BridgeSerialToTcp(serialPort, tcpWriter, cts.Token));
            Task tcpToSerialTask = Task.Run(() => BridgeTcpToSerial(tcpReader, serialPort, cts.Token));

            Console.WriteLine("Bridge is running. Press Ctrl+C to stop.");

            // 두 통신 중 하나라도 종료되거나 예외가 발생할 때까지 대기
            await Task.WhenAny(serialToTcpTask, tcpToSerialTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] {ex.Message}");
        }
        finally
        {
            cts.Cancel();
            Console.WriteLine("Bridge stopped.");
        }
    }

    // 아두이노(Serial)에서 읽어서 서버(TCP)로 전송
    private static async Task BridgeSerialToTcp(SerialPort serialPort, StreamWriter tcpWriter, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (serialPort.BytesToRead > 0)
                {
                    string message = serialPort.ReadLine().Trim(); // 아두이노 데이터 읽기
                    if (!string.IsNullOrEmpty(message))
                    {
                        Console.WriteLine($"[Arduino -> Server] {message}");
                        await tcpWriter.WriteLineAsync(message.AsMemory(), token); // 서버로 전송
                    }
                }
                await Task.Delay(10); // CPU 점유율 방지
            }
        }
        catch (Exception ex) { Console.WriteLine($"[Serial Read Error] {ex.Message}"); }
    }

    // 서버(TCP)에서 읽어서 아두이노(Serial)로 전송
    private static async Task BridgeTcpToSerial(StreamReader tcpReader, SerialPort serialPort, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                string? message = await tcpReader.ReadLineAsync(token);
                if (message == null) break; // 서버 연결 끊김

                message = message.Trim();
                if (!string.IsNullOrEmpty(message))
                {
                    Console.WriteLine($"[Server -> Arduino] {message}");
                    serialPort.WriteLine(message); // 아두이노로 전송
                }
            }
        }
        catch (Exception ex) { Console.WriteLine($"[TCP Read Error] {ex.Message}"); }
    }
}