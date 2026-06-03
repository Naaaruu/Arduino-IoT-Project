namespace IoT_ClientPC
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lblTitle = new Label();
            pnlRadar = new Panel();
            pnlIndicator = new Panel();
            pnlDistanceIndicator = new Panel();
            pnlControl = new Panel();
            btnWarning = new Button();
            btnAllow = new Button();
            btnReset = new Button();
            lblConnection = new Label();
            pnlDistanceGraph = new Panel();
            lblGraphText = new Label();
            lblIP = new Label();
            txtIp = new TextBox();
            lblPort = new Label();
            txtPort = new TextBox();
            btnConnect = new Button();
            btnDisconnect = new Button();
            lblRgbTitle = new Label();
            lblIndicatorDistance = new Label();
            btnRadarOn = new Button();
            btnRadarOff = new Button();
            radarTimer = new System.Windows.Forms.Timer(components);
            pnlIndicator.SuspendLayout();
            pnlControl.SuspendLayout();
            pnlDistanceGraph.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("맑은 고딕", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblTitle.Location = new Point(25, 20);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(421, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Client PC - 초음파 레이더 모니터링 시스템";
            // 
            // pnlRadar
            // 
            pnlRadar.BackColor = Color.Black;
            pnlRadar.BorderStyle = BorderStyle.FixedSingle;
            pnlRadar.Location = new Point(470, 70);
            pnlRadar.Name = "pnlRadar";
            pnlRadar.Size = new Size(430, 500);
            pnlRadar.TabIndex = 1;
            pnlRadar.Paint += pnlRadar_Paint;
            // 
            // pnlIndicator
            // 
            pnlIndicator.BackColor = Color.White;
            pnlIndicator.BorderStyle = BorderStyle.FixedSingle;
            pnlIndicator.Controls.Add(pnlDistanceIndicator);
            pnlIndicator.Location = new Point(915, 70);
            pnlIndicator.Name = "pnlIndicator";
            pnlIndicator.Size = new Size(50, 500);
            pnlIndicator.TabIndex = 1;
            // 
            // pnlDistanceIndicator
            // 
            pnlDistanceIndicator.BackColor = Color.LimeGreen;
            pnlDistanceIndicator.BorderStyle = BorderStyle.FixedSingle;
            pnlDistanceIndicator.Location = new Point(-1, -1);
            pnlDistanceIndicator.Name = "pnlDistanceIndicator";
            pnlDistanceIndicator.Size = new Size(50, 500);
            pnlDistanceIndicator.TabIndex = 9;
            // 
            // pnlControl
            // 
            pnlControl.Controls.Add(btnWarning);
            pnlControl.Controls.Add(btnAllow);
            pnlControl.Controls.Add(btnReset);
            pnlControl.Controls.Add(lblConnection);
            pnlControl.Controls.Add(pnlDistanceGraph);
            pnlControl.Location = new Point(25, 70);
            pnlControl.Name = "pnlControl";
            pnlControl.Size = new Size(400, 500);
            pnlControl.TabIndex = 0;
            // 
            // btnWarning
            // 
            btnWarning.Font = new Font("맑은 고딕", 16F, FontStyle.Bold);
            btnWarning.Location = new Point(240, 435);
            btnWarning.Name = "btnWarning";
            btnWarning.Size = new Size(90, 40);
            btnWarning.TabIndex = 4;
            btnWarning.Text = "경고";
            btnWarning.UseVisualStyleBackColor = true;
            btnWarning.Click += btnWarning_Click;
            // 
            // btnAllow
            // 
            btnAllow.Font = new Font("맑은 고딕", 16F, FontStyle.Bold);
            btnAllow.Location = new Point(80, 435);
            btnAllow.Name = "btnAllow";
            btnAllow.Size = new Size(90, 40);
            btnAllow.TabIndex = 3;
            btnAllow.Text = "허용";
            btnAllow.UseVisualStyleBackColor = true;
            btnAllow.Click += btnAllow_Click;
            // 
            // btnReset
            // 
            btnReset.Font = new Font("맑은 고딕", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btnReset.Location = new Point(130, 360);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(150, 40);
            btnReset.TabIndex = 2;
            btnReset.Text = "상태 초기화";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += btnReset_Click;
            // 
            // lblConnection
            // 
            lblConnection.AutoSize = true;
            lblConnection.Font = new Font("맑은 고딕", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblConnection.Location = new Point(16, 305);
            lblConnection.Name = "lblConnection";
            lblConnection.Size = new Size(74, 20);
            lblConnection.TabIndex = 1;
            lblConnection.Text = "연결 상태";
            lblConnection.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnlDistanceGraph
            // 
            pnlDistanceGraph.BackColor = Color.Black;
            pnlDistanceGraph.BorderStyle = BorderStyle.FixedSingle;
            pnlDistanceGraph.Controls.Add(lblGraphText);
            pnlDistanceGraph.Location = new Point(15, 13);
            pnlDistanceGraph.Name = "pnlDistanceGraph";
            pnlDistanceGraph.Size = new Size(369, 242);
            pnlDistanceGraph.TabIndex = 0;
            pnlDistanceGraph.Paint += pnlDistanceGraph_Paint;
            // 
            // lblGraphText
            // 
            lblGraphText.AutoSize = true;
            lblGraphText.BackColor = Color.Transparent;
            lblGraphText.ForeColor = Color.White;
            lblGraphText.Location = new Point(0, 0);
            lblGraphText.Name = "lblGraphText";
            lblGraphText.Size = new Size(69, 15);
            lblGraphText.TabIndex = 0;
            lblGraphText.Text = "Raw 그래프";
            lblGraphText.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblIP
            // 
            lblIP.AutoSize = true;
            lblIP.Font = new Font("맑은 고딕", 11F);
            lblIP.Location = new Point(470, 26);
            lblIP.Name = "lblIP";
            lblIP.Size = new Size(76, 20);
            lblIP.TabIndex = 2;
            lblIP.Text = "Server IP :";
            // 
            // txtIp
            // 
            txtIp.Location = new Point(552, 23);
            txtIp.Name = "txtIp";
            txtIp.Size = new Size(110, 23);
            txtIp.TabIndex = 3;
            txtIp.Text = "127.0.0.1";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Font = new Font("맑은 고딕", 10F);
            lblPort.Location = new Point(668, 25);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(43, 19);
            lblPort.TabIndex = 4;
            lblPort.Text = "Port :";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(717, 23);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(100, 23);
            txtPort.TabIndex = 5;
            txtPort.Text = "9000";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(825, 12);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(75, 25);
            btnConnect.TabIndex = 6;
            btnConnect.Text = "연결";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // btnDisconnect
            // 
            btnDisconnect.Enabled = false;
            btnDisconnect.Location = new Point(825, 39);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(75, 25);
            btnDisconnect.TabIndex = 7;
            btnDisconnect.Text = "해제";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;
            // 
            // lblRgbTitle
            // 
            lblRgbTitle.AutoSize = true;
            lblRgbTitle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblRgbTitle.Location = new Point(926, 49);
            lblRgbTitle.Name = "lblRgbTitle";
            lblRgbTitle.Size = new Size(32, 15);
            lblRgbTitle.TabIndex = 8;
            lblRgbTitle.Text = "RGB";
            // 
            // lblIndicatorDistance
            // 
            lblIndicatorDistance.AutoSize = true;
            lblIndicatorDistance.Font = new Font("맑은 고딕", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 129);
            lblIndicatorDistance.Location = new Point(917, 573);
            lblIndicatorDistance.Name = "lblIndicatorDistance";
            lblIndicatorDistance.Size = new Size(46, 13);
            lblIndicatorDistance.TabIndex = 10;
            lblIndicatorDistance.Text = "\"-- cm\"";
            // 
            // btnRadarOn
            // 
            btnRadarOn.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btnRadarOn.Location = new Point(552, 573);
            btnRadarOn.Name = "btnRadarOn";
            btnRadarOn.Size = new Size(80, 35);
            btnRadarOn.TabIndex = 11;
            btnRadarOn.Text = "Radar On";
            btnRadarOn.UseVisualStyleBackColor = true;
            btnRadarOn.Click += btnRadarOn_Click;
            // 
            // btnRadarOff
            // 
            btnRadarOff.Enabled = false;
            btnRadarOff.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 129);
            btnRadarOff.Location = new Point(737, 573);
            btnRadarOff.Name = "btnRadarOff";
            btnRadarOff.Size = new Size(80, 35);
            btnRadarOff.TabIndex = 12;
            btnRadarOff.Text = "Radar Off";
            btnRadarOff.UseVisualStyleBackColor = true;
            btnRadarOff.Click += btnRadarOff_Click;
            // 
            // radarTimer
            // 
            radarTimer.Interval = 80;
            radarTimer.Tick += radarTimer_Tick;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
            ClientSize = new Size(984, 611);
            Controls.Add(btnRadarOff);
            Controls.Add(btnRadarOn);
            Controls.Add(lblIndicatorDistance);
            Controls.Add(lblRgbTitle);
            Controls.Add(btnDisconnect);
            Controls.Add(btnConnect);
            Controls.Add(txtPort);
            Controls.Add(lblPort);
            Controls.Add(txtIp);
            Controls.Add(lblIP);
            Controls.Add(pnlControl);
            Controls.Add(pnlRadar);
            Controls.Add(pnlIndicator);
            Controls.Add(lblTitle);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "IoT Radar Monitoring Client";
            FormClosing += MainForm_FormClosing;
            pnlIndicator.ResumeLayout(false);
            pnlControl.ResumeLayout(false);
            pnlControl.PerformLayout();
            pnlDistanceGraph.ResumeLayout(false);
            pnlDistanceGraph.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Panel pnlRadar;
        private Panel pnlIndicator;
        private Panel pnlControl;
        private Panel pnlDistanceGraph;
        private Button btnWarning;
        private Button btnAllow;
        private Button btnReset;
        private Label lblConnection;
        private Label lblGraphText;
        private Label lblIP;
        private TextBox txtIp;
        private Label lblPort;
        private TextBox txtPort;
        private Button btnConnect;
        private Button btnDisconnect;
        private Label lblRgbTitle;
        private Label lblIndicatorDistance;
        private Panel pnlDistanceIndicator;
        private Button btnRadarOff;
        private Button btnRadarOn;
        private System.Windows.Forms.Timer radarTimer;
    }
}
