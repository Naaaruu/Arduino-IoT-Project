namespace IoTClient
{
    partial class Form1
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
            lblIp = new Label();
            txtIp = new TextBox();
            lblPort = new Label();
            txtPort = new TextBox();
            btnConnect = new Button();
            btnLedOn = new Button();
            btnLedOff = new Button();
            lblLdrValue = new Label();
            lblLog = new Label();
            txtLog = new TextBox();
            pnlChart = new Panel();
            SuspendLayout();
            // 
            // lblIp
            // 
            lblIp.AutoSize = true;
            lblIp.Location = new Point(30, 30);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(65, 15);
            lblIp.TabIndex = 0;
            lblIp.Text = "Server IP : ";
            // 
            // txtIp
            // 
            txtIp.Location = new Point(101, 27);
            txtIp.Name = "txtIp";
            txtIp.Size = new Size(150, 23);
            txtIp.TabIndex = 1;
            txtIp.Text = "127.0.0.1";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(55, 64);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(40, 15);
            lblPort.TabIndex = 2;
            lblPort.Text = "Port : ";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(101, 61);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(80, 23);
            txtPort.TabIndex = 3;
            txtPort.Text = "9000";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(272, 40);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(100, 30);
            btnConnect.TabIndex = 4;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // btnLedOn
            // 
            btnLedOn.Enabled = false;
            btnLedOn.Location = new Point(455, 20);
            btnLedOn.Name = "btnLedOn";
            btnLedOn.Size = new Size(100, 35);
            btnLedOn.TabIndex = 5;
            btnLedOn.Text = "LED ON";
            btnLedOn.UseVisualStyleBackColor = true;
            btnLedOn.Click += btnLedOn_Click;
            // 
            // btnLedOff
            // 
            btnLedOff.Enabled = false;
            btnLedOff.Location = new Point(455, 75);
            btnLedOff.Name = "btnLedOff";
            btnLedOff.Size = new Size(100, 35);
            btnLedOff.TabIndex = 6;
            btnLedOff.Text = "LED OFF";
            btnLedOff.UseVisualStyleBackColor = true;
            btnLedOff.Click += btnLedOff_Click;
            // 
            // lblLdrValue
            // 
            lblLdrValue.AutoSize = true;
            lblLdrValue.Location = new Point(21, 110);
            lblLdrValue.Name = "lblLdrValue";
            lblLdrValue.Size = new Size(83, 15);
            lblLdrValue.TabIndex = 7;
            lblLdrValue.Text = "LDR Value :  -";
            // 
            // lblLog
            // 
            lblLog.AutoSize = true;
            lblLog.Location = new Point(21, 251);
            lblLog.Name = "lblLog";
            lblLog.Size = new Size(27, 15);
            lblLog.TabIndex = 8;
            lblLog.Text = "Log";
            // 
            // txtLog
            // 
            txtLog.Location = new Point(21, 269);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(600, 180);
            txtLog.TabIndex = 9;
            txtLog.WordWrap = false;
            // 
            // pnlChart
            // 
            pnlChart.BackColor = Color.White;
            pnlChart.BorderStyle = BorderStyle.FixedSingle;
            pnlChart.Location = new Point(21, 128);
            pnlChart.Name = "pnlChart";
            pnlChart.Size = new Size(600, 120);
            pnlChart.TabIndex = 10;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(684, 461);
            Controls.Add(pnlChart);
            Controls.Add(txtLog);
            Controls.Add(lblLog);
            Controls.Add(lblLdrValue);
            Controls.Add(btnLedOff);
            Controls.Add(btnLedOn);
            Controls.Add(btnConnect);
            Controls.Add(txtPort);
            Controls.Add(lblPort);
            Controls.Add(txtIp);
            Controls.Add(lblIp);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Smart Light IoT Client";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblIp;
        private TextBox txtIp;
        private Label lblPort;
        private TextBox txtPort;
        private Button btnConnect;
        private Button btnLedOn;
        private Button btnLedOff;
        private Label lblLdrValue;
        private Label lblLog;
        private TextBox txtLog;
        private Panel pnlChart;
    }
}
