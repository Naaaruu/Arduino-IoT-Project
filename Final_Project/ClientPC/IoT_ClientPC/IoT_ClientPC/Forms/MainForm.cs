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
            radarDirection);

        }
        private void UpdateStatusUi()
        {
            // 거리값 표시
            lblIndicatorDistance.Text = $"{currentDistance} cm";

            if (isAlarmActive)
            {
                // 경보음 + LED ON 상태
                pnlDistanceIndicator.BackColor = Color.LightCoral;
                lblConnection.Text = "경보 상태";
            }
            else if (isAllowed)
            {
                // 허용 상태: 위험 거리여도 자동 경보 막음
                pnlDistanceIndicator.BackColor = Color.DeepSkyBlue;
                lblConnection.Text = "허용 상태";
            }
            else if (isDangerDetected)
            {
                // 위험 거리 감지됐지만, 아직 경보 처리 전 상태
                pnlDistanceIndicator.BackColor = Color.Gold;
                lblConnection.Text = "위험 감지";
            }
            else if (currentDistance <= WarningDistance)
            {
                // 주의 거리
                pnlDistanceIndicator.BackColor = Color.Gold;
                lblConnection.Text = "주의 상태";
            }
            else
            {
                // 정상
                pnlDistanceIndicator.BackColor = Color.LimeGreen;
                lblConnection.Text = "정상 상태";
            }
        }
        private void CheckDistanceState()
        {
            // 위험 거리 감지 여부
            isDangerDetected = currentDistance <= DangerDistance;

            // 위험 거리 안에 들어왔고, 허용 상태가 아니면 자동 경보 발생
            if (isDangerDetected && !isAllowed)
            {
                isAlarmActive = true;
            }

            // 경보는 자동으로 끄지 않음
            // 허용 버튼 또는 초기화 버튼으로만 꺼짐

            UpdateStatusUi();
        }

        private void radarTimer_Tick(object sender, EventArgs e)
        {
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

            // TODO: 실제 서버 연결 후, 이 테스트 메시지는 서버 수신 메시지로 대체
            int testDistance = 25 + (int)(10 * Math.Sin(currentAngle * Math.PI / 180.0));
            string testMessage = $"RADAR:{currentAngle}:{testDistance}";

            ApplyRadarMessage(testMessage);
        }

        private void btnRadarOn_Click(object sender, EventArgs e)
        {
            radarTimer.Start();

            btnRadarOn.Enabled = false;
            btnRadarOff.Enabled = true;

            //// 서버 연결이 되어 있을 때만 명령 전송
            //if (tcpClientManager.IsConnected)
            //{
            //    await SendCommandAsync("RADAR_ON");
            //}
        }

        private void btnRadarOff_Click(object sender, EventArgs e)
        {
            radarTimer.Stop();

            btnRadarOn.Enabled = true;
            btnRadarOff.Enabled = false;

            // 서버 연결이 되어 있을 때만 명령 전송
            //if (tcpClientManager.IsConnected)
            //{
            //    await SendCommandAsync("RADAR_OFF");
            //}
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
            isAlarmActive = true;   // 경보음 + LED ON
            isAllowed = false;      // 허용 상태 해제

            UpdateStatusUi();

            await SendCommandAsync("CMD:WARN");
        }

        private async void btnAllow_Click(object sender, EventArgs e)
        {
            isAllowed = true;       // 허용 상태 ON
            isAlarmActive = false;  // 경보음 + LED OFF

            UpdateStatusUi();

            await SendCommandAsync("CMD:ALLOW");
        }

        private async void btnReset_Click(object sender, EventArgs e)
        {
            radarTimer.Stop();

            btnRadarOn.Enabled = true;
            btnRadarOff.Enabled = false;

            // 경보/허용 상태 초기화
            isAlarmActive = false;
            isAllowed = false;
            isDangerDetected = false;

            // 레이더 기본값
            currentAngle = 90;
            currentDistance = 40;
            radarDirection = 1;

            // 그래프 데이터 초기화
            distanceHistory.Clear();

            // 화면 갱신
            UpdateStatusUi();

            pnlRadar.Invalidate();
            pnlDistanceGraph.Invalidate();


            await SendCommandAsync("CMD:RESET");
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
