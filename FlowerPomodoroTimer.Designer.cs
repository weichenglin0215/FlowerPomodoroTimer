namespace Flower_Pomodoro_Timer
{
    partial class formFlowerPomodoroTimer
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(formFlowerPomodoroTimer));
            labelTimer = new Label();
            buttonStart = new Button();
            buttonTest = new Button();
            labelTotalTimer = new Label();
            buttonHelp = new Button();
            buttonQuit = new Button();
            buttonMinimumSize = new Button();
            buttonBackColor = new Button();
            imageList4TreeView = new ImageList(components);
            imageListPlus = new ImageList(components);
            toolTipAll = new ToolTip(components);
            buttonAlwaysTop = new Button();
            buttonOpacity = new Button();
            buttonShowPerf = new Button();
            labelStopwatch = new Label();
            buttonStopwatchStart = new Button();
            buttonStopwatchReset = new Button();
            panelStopwatch = new Panel();
            labelPoetry = new Label();
            panelStopwatch.SuspendLayout();
            SuspendLayout();
            // 
            // labelTimer
            // 
            labelTimer.Font = new Font("Arial Narrow", 40F);
            labelTimer.ForeColor = Color.FromArgb(255, 224, 192);
            labelTimer.ImageKey = "(無)";
            labelTimer.Location = new Point(90, 10);
            labelTimer.Name = "labelTimer";
            labelTimer.Size = new Size(140, 60);
            labelTimer.TabIndex = 0;
            labelTimer.Text = "00:00";
            labelTimer.TextAlign = ContentAlignment.MiddleCenter;
            toolTipAll.SetToolTip(labelTimer, "番茄時間");
            // 
            // buttonStart
            // 
            buttonStart.AutoSize = true;
            buttonStart.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonStart.FlatStyle = FlatStyle.Flat;
            buttonStart.Font = new Font("Arial", 16F);
            buttonStart.ForeColor = Color.Brown;
            buttonStart.Location = new Point(120, 80);
            buttonStart.MaximumSize = new Size(200, 40);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(84, 40);
            buttonStart.TabIndex = 1;
            buttonStart.Text = "Start";
            toolTipAll.SetToolTip(buttonStart, "啟動/暫停");
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.SizeChanged += buttonStart_SizeChanged;
            buttonStart.Click += buttonStart_Click;
            // 
            // buttonTest
            // 
            buttonTest.Location = new Point(12, 738);
            buttonTest.Name = "buttonTest";
            buttonTest.Size = new Size(75, 23);
            buttonTest.TabIndex = 2;
            buttonTest.Text = "button1";
            buttonTest.UseVisualStyleBackColor = true;
            buttonTest.Click += buttonTest_Click;
            // 
            // labelTotalTimer
            // 
            labelTotalTimer.Font = new Font("Arial Narrow", 24F);
            labelTotalTimer.ForeColor = Color.FromArgb(255, 224, 192);
            labelTotalTimer.ImageKey = "(無)";
            labelTotalTimer.Location = new Point(90, 120);
            labelTotalTimer.Name = "labelTotalTimer";
            labelTotalTimer.Size = new Size(130, 40);
            labelTotalTimer.TabIndex = 3;
            labelTotalTimer.Text = "00:00:00";
            labelTotalTimer.TextAlign = ContentAlignment.MiddleCenter;
            toolTipAll.SetToolTip(labelTotalTimer, "總使用時間");
            // 
            // buttonHelp
            // 
            buttonHelp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonHelp.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonHelp.FlatStyle = FlatStyle.Flat;
            buttonHelp.Font = new Font("新細明體", 16F, FontStyle.Bold);
            buttonHelp.ForeColor = Color.Brown;
            buttonHelp.Location = new Point(1256, 0);
            buttonHelp.Name = "buttonHelp";
            buttonHelp.Size = new Size(30, 30);
            buttonHelp.TabIndex = 4;
            buttonHelp.Text = "？";
            toolTipAll.SetToolTip(buttonHelp, "說明");
            buttonHelp.UseVisualStyleBackColor = true;
            buttonHelp.Click += buttonHelp_Click;
            // 
            // buttonQuit
            // 
            buttonQuit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonQuit.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonQuit.FlatStyle = FlatStyle.Flat;
            buttonQuit.Font = new Font("新細明體", 16F, FontStyle.Bold);
            buttonQuit.ForeColor = Color.Brown;
            buttonQuit.Location = new Point(1314, 0);
            buttonQuit.Name = "buttonQuit";
            buttonQuit.Size = new Size(30, 30);
            buttonQuit.TabIndex = 5;
            buttonQuit.Text = "X";
            toolTipAll.SetToolTip(buttonQuit, "離開");
            buttonQuit.UseVisualStyleBackColor = true;
            buttonQuit.Click += buttonQuit_Click;
            // 
            // buttonMinimumSize
            // 
            buttonMinimumSize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonMinimumSize.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonMinimumSize.FlatStyle = FlatStyle.Flat;
            buttonMinimumSize.Font = new Font("新細明體", 16F);
            buttonMinimumSize.ForeColor = Color.Brown;
            buttonMinimumSize.Location = new Point(1285, 0);
            buttonMinimumSize.Name = "buttonMinimumSize";
            buttonMinimumSize.Size = new Size(30, 30);
            buttonMinimumSize.TabIndex = 6;
            buttonMinimumSize.Text = "◢";
            toolTipAll.SetToolTip(buttonMinimumSize, "縮小至右下方");
            buttonMinimumSize.UseVisualStyleBackColor = true;
            buttonMinimumSize.Click += buttonMinimumSize_Click;
            // 
            // buttonBackColor
            // 
            buttonBackColor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonBackColor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonBackColor.FlatStyle = FlatStyle.Flat;
            buttonBackColor.Font = new Font("新細明體", 15F);
            buttonBackColor.ForeColor = Color.Brown;
            buttonBackColor.Location = new Point(1227, 0);
            buttonBackColor.Name = "buttonBackColor";
            buttonBackColor.Size = new Size(30, 30);
            buttonBackColor.TabIndex = 7;
            buttonBackColor.Text = "●";
            toolTipAll.SetToolTip(buttonBackColor, "更換佈景顏色");
            buttonBackColor.UseVisualStyleBackColor = true;
            buttonBackColor.Click += buttonBackColor_Click;
            // 
            // imageList4TreeView
            // 
            imageList4TreeView.ColorDepth = ColorDepth.Depth8Bit;
            imageList4TreeView.ImageStream = (ImageListStreamer)resources.GetObject("imageList4TreeView.ImageStream");
            imageList4TreeView.TransparentColor = Color.Transparent;
            imageList4TreeView.Images.SetKeyName(0, "18610531421560147113-256.png");
            imageList4TreeView.Images.SetKeyName(1, "CaoCyuanBeiCalligraphy48.ico");
            imageList4TreeView.Images.SetKeyName(2, "PicPick2021-04-01 12 39 15.png");
            // 
            // imageListPlus
            // 
            imageListPlus.ColorDepth = ColorDepth.Depth8Bit;
            imageListPlus.ImageStream = (ImageListStreamer)resources.GetObject("imageListPlus.ImageStream");
            imageListPlus.TransparentColor = Color.Transparent;
            imageListPlus.Images.SetKeyName(0, "icons8_Sum_28px.png");
            imageListPlus.Images.SetKeyName(1, "icons8_Negative_28px.png");
            // 
            // toolTipAll
            // 
            toolTipAll.AutomaticDelay = 300;
            // 
            // buttonAlwaysTop
            // 
            buttonAlwaysTop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonAlwaysTop.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonAlwaysTop.FlatStyle = FlatStyle.Flat;
            buttonAlwaysTop.Font = new Font("新細明體", 15F);
            buttonAlwaysTop.ForeColor = Color.Brown;
            buttonAlwaysTop.Location = new Point(1198, 0);
            buttonAlwaysTop.Name = "buttonAlwaysTop";
            buttonAlwaysTop.Size = new Size(30, 30);
            buttonAlwaysTop.TabIndex = 8;
            buttonAlwaysTop.Text = "╩";
            toolTipAll.SetToolTip(buttonAlwaysTop, "開啟最上層顯示");
            buttonAlwaysTop.UseVisualStyleBackColor = true;
            buttonAlwaysTop.Click += buttonAlwaysTop_Click;
            // 
            // buttonOpacity
            // 
            buttonOpacity.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonOpacity.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonOpacity.FlatStyle = FlatStyle.Flat;
            buttonOpacity.Font = new Font("新細明體", 15F);
            buttonOpacity.ForeColor = Color.Brown;
            buttonOpacity.Location = new Point(1169, 0);
            buttonOpacity.Name = "buttonOpacity";
            buttonOpacity.Size = new Size(30, 30);
            buttonOpacity.TabIndex = 9;
            buttonOpacity.Text = "▊";
            toolTipAll.SetToolTip(buttonOpacity, "改變視窗透明度");
            buttonOpacity.UseVisualStyleBackColor = true;
            buttonOpacity.Click += buttonOpacity_Click;
            // 
            // buttonShowPerf
            // 
            buttonShowPerf.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonShowPerf.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonShowPerf.FlatStyle = FlatStyle.Flat;
            buttonShowPerf.Font = new Font("新細明體", 15F);
            buttonShowPerf.ForeColor = Color.Brown;
            buttonShowPerf.Location = new Point(1140, 0);
            buttonShowPerf.Name = "buttonShowPerf";
            buttonShowPerf.Size = new Size(30, 30);
            buttonShowPerf.TabIndex = 11;
            buttonShowPerf.Text = "II";
            toolTipAll.SetToolTip(buttonShowPerf, "暫停顯示效率");
            buttonShowPerf.UseVisualStyleBackColor = true;
            buttonShowPerf.Click += buttonShowPerf_Click;
            // 
            // labelStopwatch
            // 
            labelStopwatch.Font = new Font("Arial Narrow", 30F);
            labelStopwatch.ForeColor = Color.FromArgb(255, 224, 192);
            labelStopwatch.Location = new Point(10, 5);
            labelStopwatch.Name = "labelStopwatch";
            labelStopwatch.Size = new Size(110, 48);
            labelStopwatch.TabIndex = 0;
            labelStopwatch.Text = "00:00";
            labelStopwatch.TextAlign = ContentAlignment.MiddleCenter;
            toolTipAll.SetToolTip(labelStopwatch, "計時器");
            // 
            // buttonStopwatchStart
            // 
            buttonStopwatchStart.FlatStyle = FlatStyle.Flat;
            buttonStopwatchStart.Font = new Font("Arial", 10F);
            buttonStopwatchStart.ForeColor = Color.Brown;
            buttonStopwatchStart.Location = new Point(10, 61);
            buttonStopwatchStart.Name = "buttonStopwatchStart";
            buttonStopwatchStart.Size = new Size(50, 26);
            buttonStopwatchStart.TabIndex = 1;
            buttonStopwatchStart.Text = "Start";
            toolTipAll.SetToolTip(buttonStopwatchStart, "開始/暫停");
            buttonStopwatchStart.UseVisualStyleBackColor = true;
            buttonStopwatchStart.Click += buttonStopwatchStart_Click;
            // 
            // buttonStopwatchReset
            // 
            buttonStopwatchReset.FlatStyle = FlatStyle.Flat;
            buttonStopwatchReset.Font = new Font("Arial", 10F);
            buttonStopwatchReset.ForeColor = Color.Brown;
            buttonStopwatchReset.Location = new Point(69, 61);
            buttonStopwatchReset.Name = "buttonStopwatchReset";
            buttonStopwatchReset.Size = new Size(50, 26);
            buttonStopwatchReset.TabIndex = 2;
            buttonStopwatchReset.Text = "重置";
            toolTipAll.SetToolTip(buttonStopwatchReset, "重置計時器為 00:00");
            buttonStopwatchReset.UseVisualStyleBackColor = true;
            buttonStopwatchReset.Click += buttonStopwatchReset_Click;
            // 
            // panelStopwatch
            // 
            panelStopwatch.BorderStyle = BorderStyle.FixedSingle;
            panelStopwatch.Controls.Add(labelStopwatch);
            panelStopwatch.Controls.Add(buttonStopwatchStart);
            panelStopwatch.Controls.Add(buttonStopwatchReset);
            panelStopwatch.ForeColor = SystemColors.ControlLightLight;
            panelStopwatch.Location = new Point(1000, 350);
            panelStopwatch.Name = "panelStopwatch";
            panelStopwatch.Size = new Size(130, 100);
            panelStopwatch.TabIndex = 12;
            // 
            // labelPoetry
            // 
            labelPoetry.AutoSize = true;
            labelPoetry.Font = new Font("標楷體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            labelPoetry.Location = new Point(93, 741);
            labelPoetry.Name = "labelPoetry";
            labelPoetry.Size = new Size(69, 20);
            labelPoetry.TabIndex = 10;
            labelPoetry.Text = "label1";
            // 
            // formFlowerPomodoroTimer
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.Tomato;
            ClientSize = new Size(1344, 773);
            ControlBox = false;
            Controls.Add(labelPoetry);
            Controls.Add(panelStopwatch);
            Controls.Add(buttonShowPerf);
            Controls.Add(buttonOpacity);
            Controls.Add(buttonAlwaysTop);
            Controls.Add(buttonBackColor);
            Controls.Add(buttonMinimumSize);
            Controls.Add(buttonQuit);
            Controls.Add(buttonHelp);
            Controls.Add(labelTotalTimer);
            Controls.Add(buttonTest);
            Controls.Add(buttonStart);
            Controls.Add(labelTimer);
            DoubleBuffered = true;
            Font = new Font("新細明體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4);
            Name = "formFlowerPomodoroTimer";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Flower Pomodoro Timer Ver 0.11.1.0   珍惜自己的一分一秒...";
            panelStopwatch.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label labelTimer;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonTest;
        private System.Windows.Forms.Label labelTotalTimer;
        private System.Windows.Forms.Button buttonHelp;
        private System.Windows.Forms.Button buttonQuit;
        private System.Windows.Forms.Button buttonMinimumSize;
        private System.Windows.Forms.Button buttonBackColor;
        private System.Windows.Forms.ImageList imageList4TreeView;
        private System.Windows.Forms.ImageList imageListPlus;
        private System.Windows.Forms.ToolTip toolTipAll;
        private System.Windows.Forms.Button buttonAlwaysTop;
        private System.Windows.Forms.Button buttonOpacity;
        private System.Windows.Forms.Button buttonShowPerf;
        private System.Windows.Forms.Panel panelStopwatch;
        private System.Windows.Forms.Label labelStopwatch;
        private System.Windows.Forms.Button buttonStopwatchStart;
        private System.Windows.Forms.Button buttonStopwatchReset;
        private System.Windows.Forms.Label labelPoetry;
    }
}

