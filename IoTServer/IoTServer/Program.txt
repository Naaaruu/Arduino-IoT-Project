using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 9000);
server.Start();

Console.WriteLine("==================================");
Console.WriteLine(" Temporary IoT Server Started");
Console.WriteLine(" Port: 9000");
Console.WriteLine("==================================");
Console.WriteLine("Waiting for client...");

TcpClient client = await server.AcceptTcpClientAsync();
Console.WriteLine("Client connected.");

NetworkStream stream = client.GetStream();

StreamReader reader = new StreamReader(stream, Encoding.UTF8);
StreamWriter writer = new StreamWriter(stream, Encoding.UTF8)
{
    AutoFlush = true
};

// LDR 가짜 센서값 1초마다 전송
_ = Task.Run(async () =>
{
    Random random = new Random();

    while (client.Connected)
    {
        int ldr = random.Next(0, 1024);

        await writer.WriteLineAsync($"LDR:{ldr}");
        Console.WriteLine($"Send: LDR:{ldr}");

        await Task.Delay(1000);
    }
});

// Client에서 LED_ON / LED_OFF 받기
while (client.Connected)
{
    string? message = await reader.ReadLineAsync();

    if (message == null)
    {
        break;
    }

    Console.WriteLine("Receive: " + message);

    if (message == "LED_ON")
    {
        Console.WriteLine("[SIM] LED ON");
        await writer.WriteLineAsync("LED:ON");
    }
    else if (message == "LED_OFF")
    {
        Console.WriteLine("[SIM] LED OFF");
        await writer.WriteLineAsync("LED:OFF");
    }
}

Console.WriteLine("Client disconnected.");

reader.Close();
writer.Close();
client.Close();
server.Stop();