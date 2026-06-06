using System.Reflection;
using System.Drawing.Drawing2D;
using IoT_ClientPC.UI;
using System.Collections.Generic;
using IoT_ClientPC.Models;
using IoT_ClientPC.Services;
using IoT_ClientPC.Network;

namespace IoT_ClientPC
{
    public partial class MainForm : Form
    {
        private int currentAngle = 90;
        private int currentDistance = 25;
        private int radarDirection = 1;
        private bool isAlarmActive = false;    // 경보음 + LED가 켜져 있는 상태
        private bool isAllowed = false;        // 허용 상태, 위험 거리여도 자동 경보 안 울림
        private bool isDangerDetected = false; // 위험 거리 감지 상태
        private bool isManualWarningActive = false;
        private bool isRadarEnabled = false;
        private bool hasDetectedPosition = false;
        private int detectedAngle = 90;
        private int detectedDistance = 25;

        private const int DangerDistance = 15;
        private const int WarningDistance = 30;

        private readonly RadarRenderer radarRenderer = new RadarRenderer();
        private readonly DistanceGraphRenderer distanceGraphRenderer = new DistanceGraphRenderer();
        private readonly List<int> distanceHistory = new List<int>();
        private readonly RadarDataParser radarDataParser = new RadarDataParser();
        private readonly TcpClientManager tcpClientManager = new TcpClientManager();

        private CancellationTokenSource? receiveCancellationTokenSource;

        public MainForm()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            EnableDoubleBuffering(pnlRadar);
            EnableDoubleBuffering(pnlDistanceGraph);

            btnDisconnect.Enabled = false;
            btnRadarOff.Enabled = false;

            UpdateStatusUi();
        }
        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                control,
                new object[] { true }
            );
        }
        private async Task SendCommandAsync(string command)
        {
            if (!tcpClientManager.IsConnected)
            {
                MessageBox.Show("서버에 연결되어 있지 않습니다.");
                return;
            }

            try
            {
                await tcpClientManager.SendAsync(command);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"명령 전송 실패: {ex.Message}");
            }
        }
        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && tcpClientManager.IsConnected)
                {
                    string? message = await tcpClientManager.ReceiveAsync();

                    if (message == null)
                        break;

                    // UI 스레드에서 화면 갱신
                    BeginInvoke(new Action(() =>
                    {
                        ApplyRadarMessage(message);
                    }));
                }
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"수신 오류: {ex.Message}");
                }));
            }
        }
        private void ApplyRadarMessage(string message)
        {
            if (!isRadarEnabled)
                return;

            if (radarDataParser.TryParse(message, out RadarData? data) && data != null)
            {
                currentAngle = data.Angle;
                currentDistance = data.Distance;

                CheckDistanceState();

                distanceHistory.Add(currentDistance);

                if (distanceHistory.Count > 60)
                    distanceHistory.RemoveAt(0);

                pnlRadar.Invalidate();
                pnlDistanceGraph.Invalidate();
            }
        }

        private void pnlRadar_Paint(object sender, PaintEventArgs e)
        {
            radarRenderer.Draw(
                e.Graphics,
                pnlRadar.ClientRectangle,
                currentAngle,
                currentDistance,
                radarDirection
            );
        }
        private void UpdateStatusUi()
        {
            Color indicatorColor = Color.LimeGreen;

            lblIndicatorDistance.Text = $"{currentDistance} cm";

            if (isAlarmActive)
            {
                indicatorColor = Color.LightCoral;
                lblConnection.Text = "경보 상태";
            }
            else if (isAllowed)
            {
                indicatorColor = Color.DeepSkyBlue;
                lblConnection.Text = "허용 상태";
            }
            else if (isDangerDetected)
            {
                indicatorColor = Color.Gold;
                lblConnection.Text = "위험 감지";
            }
            else if (currentDistance <= WarningDistance)
            {
                indicatorColor = Color.Gold;
                lblConnection.Text = "주의 상태";
            }
            else
            {
                indicatorColor = Color.LimeGreen;
                lblConnection.Text = "정상 상태";
            }

            pnlDistanceIndicator.BackColor = indicatorColor;
            pnlDistanceIndicator.Invalidate();
        }
        private void CheckDistanceState()
        {
            isDangerDetected = currentDistance <= DangerDistance;

            if (isAllowed)
            {
                isAlarmActive = false;
            }
            else
            {
                // 수동 경고가 켜져 있거나, 위험 거리면 경보 상태
                isAlarmActive = isManualWarningActive || isDangerDetected;
            }

            UpdateStatusUi();
        }

        private void radarTimer_Tick(object sender, EventArgs e)
        {
            if (tcpClientManager.IsConnected)
            {
                pnlRadar.Invalidate();
                pnlDistanceGraph.Invalidate();
                return;
            }

            currentAngle += radarDirection * 2;

            if (currentAngle >= 150)
            {
                currentAngle = 150;
                radarDirection = -1;
            }
            else if (currentAngle <= 30)
            {
                currentAngle = 30;
                radarDirection = 1;
            }

            // 서버 미연결 상태에서만 UI 확인용 테스트값 사용
            int testDistance = 35;
            string testMessage = $"RADAR:{currentAngle}:{testDistance}";

            ApplyRadarMessage(testMessage);
        }

        private void btnRadarOn_Click(object sender, EventArgs e)
        {
            isRadarEnabled = true;

            btnRadarOn.Enabled = false;
            btnRadarOff.Enabled = true;
        }

        private void btnRadarOff_Click(object sender, EventArgs e)
        {
            isRadarEnabled = false;

            radarTimer.Stop();

            btnRadarOn.Enabled = true;
            btnRadarOff.Enabled = false;

            pnlRadar.Invalidate();
            pnlDistanceGraph.Invalidate();
        }

        private void pnlDistanceGraph_Paint(object sender, PaintEventArgs e)
        {
            distanceGraphRenderer.Draw(
    e.Graphics,
    pnlDistanceGraph.ClientRectangle,
    distanceHistory,
    40
);
        }

        private async void btnWarning_Click(object sender, EventArgs e)
        {
            isManualWarningActive = true;
            isAlarmActive = true;
            isAllowed = false;

            UpdateStatusUi();

            await SendCommandAsync("CMD:WARN");
        }

        private async void btnAllow_Click(object sender, EventArgs e)
        {
            isAllowed = true;
            isManualWarningActive = false;
            isAlarmActive = false;

            UpdateStatusUi();

            await SendCommandAsync("CMD:ALLOW");
        }

        private async void btnReset_Click(object sender, EventArgs e)
        {
            radarTimer.Stop();

            isAlarmActive = false;
            isAllowed = false;
            isDangerDetected = false;
            isManualWarningActive = false;

            hasDetectedPosition = false;
            detectedAngle = 90;
            detectedDistance = 25;

            currentAngle = 90;
            currentDistance = 40;
            radarDirection = 1;

            distanceHistory.Clear();

            UpdateStatusUi();

            pnlRadar.Invalidate();
            pnlDistanceGraph.Invalidate();

            if (tcpClientManager.IsConnected)
            {
                await SendCommandAsync("CMD:RESET");
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string ip = txtIp.Text.Trim();

                if (!int.TryParse(txtPort.Text.Trim(), out int port))
                {
                    MessageBox.Show("Port 번호가 올바르지 않습니다.");
                    return;
                }

                await tcpClientManager.ConnectAsync(ip, port);
                await tcpClientManager.SendAsync("ROLE:CLIENT");

                lblConnection.Text = "Connected";
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;

                receiveCancellationTokenSource = new CancellationTokenSource();
                _ = ReceiveLoopAsync(receiveCancellationTokenSource.Token);

                MessageBox.Show("서버에 연결되었습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"서버 연결 실패: {ex.Message}");

                lblConnection.Text = "Disconnected";
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            receiveCancellationTokenSource?.Cancel();
            receiveCancellationTokenSource = null;

            tcpClientManager.Disconnect();

            lblConnection.Text = "Disconnected";
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;

            MessageBox.Show("서버 연결이 해제되었습니다.");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            receiveCancellationTokenSource?.Cancel();
            receiveCancellationTokenSource = null;

            radarTimer.Stop();

            tcpClientManager.Disconnect();
        }
    }
}
