using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Drawing;

namespace IoTClient
{
    public partial class Form1 : Form
    {
        private TcpClient? client;
        private StreamReader? reader;
        private StreamWriter? writer;
        private bool isConnected = false;
        private List<int> ldrValues = new List<int>();
        private const int MaxChartPoints = 30;

        public Form1()
        {
            InitializeComponent();

            btnLedOn.Enabled = false;
            btnLedOff.Enabled = false;
            btnDisconnect.Enabled = false;

            pnlChart.Paint += pnlChart_Paint;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string ip = txtIp.Text.Trim();
                int port = int.Parse(txtPort.Text.Trim());

                client = new TcpClient();
                await client.ConnectAsync(ip, port);

                NetworkStream stream = client.GetStream();

                reader = new StreamReader(stream);
                writer = new StreamWriter(stream)
                {
                    AutoFlush = true
                };

                isConnected = true;

                btnConnect.Enabled = false;
                btnLedOn.Enabled = true;
                btnLedOff.Enabled = true;
                btnDisconnect.Enabled = true;
                lblStatus.Text = "Status: Connected";

                AddLog("Server connected.");

                _ = Task.Run(ReceiveLoop);
            }
            catch (Exception ex)
            {
                AddLog("Connect error: " + ex.Message);
                MessageBox.Show("서버 연결 실패: " + ex.Message);
            }
        }

        private async void btnLedOn_Click(object sender, EventArgs e)
        {
            await SendMessageToServer("LED_ON");
        }

        private async void btnLedOff_Click(object sender, EventArgs e)
        {
            await SendMessageToServer("LED_OFF");
        }
        private async Task SendMessageToServer(string message)
        {
            try
            {
                if (!isConnected || writer == null)
                {
                    AddLog("Server is not connected.");
                    return;
                }

                await writer.WriteLineAsync(message);
                AddLog("Send: " + message);
            }
            catch (Exception ex)
            {
                AddLog("Send error: " + ex.Message);
            }
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (isConnected && reader != null)
                {
                    string? message = await reader.ReadLineAsync();

                    if (message == null)
                    {
                        break;
                    }

                    this.Invoke(() =>
                    {
                        HandleServerMessage(message);
                    });
                }
            }
            catch (Exception ex)
            {
                this.Invoke(() =>
                {
                    AddLog("Receive error: " + ex.Message);
                });
            }
            finally
            {
                this.Invoke(() =>
                {
                    Disconnect();
                });
            }
        }

        private void HandleServerMessage(string message)
        {
            AddLog("Receive: " + message);

            if (message.StartsWith("LDR:"))
            {
                string valueText = message.Replace("LDR:", "").Trim();

                lblLdrValue.Text = "LDR Value : " + valueText;

                if (int.TryParse(valueText, out int ldr))
                {
                    ldrValues.Add(ldr);

                    if (ldrValues.Count > MaxChartPoints)
                    {
                        ldrValues.RemoveAt(0);
                    }

                    pnlChart.Invalidate();
                }
            }
            else if (message == "LED:ON")
            {
                AddLog("LED is ON.");
            }
            else if (message == "LED:OFF")
            {
                AddLog("LED is OFF.");
            }
        }

        private void pnlChart_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            int width = pnlChart.Width;
            int height = pnlChart.Height;

            int marginLeft = 40;
            int marginRight = 10;
            int marginTop = 10;
            int marginBottom = 25;

            int graphWidth = width - marginLeft - marginRight;
            int graphHeight = height - marginTop - marginBottom;

            // 축 그리기
            g.DrawLine(Pens.Black, marginLeft, marginTop, marginLeft, marginTop + graphHeight);
            g.DrawLine(Pens.Black, marginLeft, marginTop + graphHeight, marginLeft + graphWidth, marginTop + graphHeight);

            // y축 라벨
            g.DrawString("1023", this.Font, Brushes.Black, 5, marginTop);
            g.DrawString("0", this.Font, Brushes.Black, 20, marginTop + graphHeight - 10);

            if (ldrValues.Count < 2)
            {
                g.DrawString("Waiting for LDR data...", this.Font, Brushes.Black, marginLeft + 10, marginTop + 10);
                return;
            }

            Point[] points = new Point[ldrValues.Count];

            for (int i = 0; i < ldrValues.Count; i++)
            {
                int x = marginLeft + (int)((double)i / (MaxChartPoints - 1) * graphWidth);

                int value = ldrValues[i];
                int y = marginTop + graphHeight - (int)((double)value / 1023 * graphHeight);

                points[i] = new Point(x, y);
            }

            g.DrawLines(Pens.Blue, points);

            int latest = ldrValues[ldrValues.Count - 1];
            g.DrawString($"Current: {latest}", this.Font, Brushes.Black, marginLeft + 10, marginTop + 10);
        }
        private void AddLog(string text)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        }

        private void Disconnect()
        {
            if (!isConnected)
            {
                return;
            }

            isConnected = false;

            try
            {
                reader?.Close();
                writer?.Close();
                client?.Close();
            }
            catch
            {
                // 연결 종료 중 발생하는 예외는 무시
            }

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            btnLedOn.Enabled = false;
            btnLedOff.Enabled = false;

            lblStatus.Text = "Status: Disconnected";

            AddLog("Disconnected.");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isConnected = false;

            reader?.Close();
            writer?.Close();
            client?.Close();

            base.OnFormClosing(e);
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }
    }
}
