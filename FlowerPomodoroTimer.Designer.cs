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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(formFlowerPomodoroTimer));
            this.labelTimer = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonTest = new System.Windows.Forms.Button();
            this.labelTotalTimer = new System.Windows.Forms.Label();
            this.buttonHelp = new System.Windows.Forms.Button();
            this.buttonQuit = new System.Windows.Forms.Button();
            this.buttonMinimumSize = new System.Windows.Forms.Button();
            this.buttonBackColor = new System.Windows.Forms.Button();
            this.imageList4TreeView = new System.Windows.Forms.ImageList(this.components);
            this.imageListPlus = new System.Windows.Forms.ImageList(this.components);
            this.toolTipAll = new System.Windows.Forms.ToolTip(this.components);
            this.buttonAlwaysTop = new System.Windows.Forms.Button();
            this.buttonOpacity = new System.Windows.Forms.Button();
            this.labelPoetry = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelTimer
            // 
            this.labelTimer.Font = new System.Drawing.Font("Arial Narrow", 90F);
            this.labelTimer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
            this.labelTimer.ImageKey = "(無)";
            this.labelTimer.Location = new System.Drawing.Point(20, 25);
            this.labelTimer.Name = "labelTimer";
            this.labelTimer.Size = new System.Drawing.Size(310, 127);
            this.labelTimer.TabIndex = 0;
            this.labelTimer.Text = "00:00";
            this.labelTimer.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTipAll.SetToolTip(this.labelTimer, "番茄時間");
            // 
            // buttonStart
            // 
            this.buttonStart.AutoSize = true;
            this.buttonStart.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStart.Font = new System.Drawing.Font("Arial", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.ForeColor = System.Drawing.Color.Brown;
            this.buttonStart.Location = new System.Drawing.Point(125, 165);
            this.buttonStart.MaximumSize = new System.Drawing.Size(200, 40);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(88, 40);
            this.buttonStart.TabIndex = 1;
            this.buttonStart.Text = "Start";
            this.toolTipAll.SetToolTip(this.buttonStart, "啟動/暫停");
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.SizeChanged += new System.EventHandler(this.buttonStart_SizeChanged);
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // buttonTest
            // 
            this.buttonTest.Location = new System.Drawing.Point(12, 738);
            this.buttonTest.Name = "buttonTest";
            this.buttonTest.Size = new System.Drawing.Size(75, 23);
            this.buttonTest.TabIndex = 2;
            this.buttonTest.Text = "button1";
            this.buttonTest.UseVisualStyleBackColor = true;
            this.buttonTest.Click += new System.EventHandler(this.buttonTest_Click);
            // 
            // labelTotalTimer
            // 
            this.labelTotalTimer.Font = new System.Drawing.Font("Arial Narrow", 60F);
            this.labelTotalTimer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
            this.labelTotalTimer.ImageKey = "(無)";
            this.labelTotalTimer.Location = new System.Drawing.Point(20, 220);
            this.labelTotalTimer.Name = "labelTotalTimer";
            this.labelTotalTimer.Size = new System.Drawing.Size(310, 95);
            this.labelTotalTimer.TabIndex = 3;
            this.labelTotalTimer.Text = "00:00:00";
            this.labelTotalTimer.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTipAll.SetToolTip(this.labelTotalTimer, "總使用時間");
            // 
            // buttonHelp
            // 
            this.buttonHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonHelp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonHelp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonHelp.Font = new System.Drawing.Font("新細明體", 16F, System.Drawing.FontStyle.Bold);
            this.buttonHelp.ForeColor = System.Drawing.Color.Brown;
            this.buttonHelp.Location = new System.Drawing.Point(1256, 0);
            this.buttonHelp.Name = "buttonHelp";
            this.buttonHelp.Size = new System.Drawing.Size(30, 30);
            this.buttonHelp.TabIndex = 4;
            this.buttonHelp.Text = "？";
            this.toolTipAll.SetToolTip(this.buttonHelp, "說明");
            this.buttonHelp.UseVisualStyleBackColor = true;
            this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
            // 
            // buttonQuit
            // 
            this.buttonQuit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonQuit.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonQuit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonQuit.Font = new System.Drawing.Font("新細明體", 16F, System.Drawing.FontStyle.Bold);
            this.buttonQuit.ForeColor = System.Drawing.Color.Brown;
            this.buttonQuit.Location = new System.Drawing.Point(1314, 0);
            this.buttonQuit.Name = "buttonQuit";
            this.buttonQuit.Size = new System.Drawing.Size(30, 30);
            this.buttonQuit.TabIndex = 5;
            this.buttonQuit.Text = "X";
            this.toolTipAll.SetToolTip(this.buttonQuit, "離開");
            this.buttonQuit.UseVisualStyleBackColor = true;
            this.buttonQuit.Click += new System.EventHandler(this.buttonQuit_Click);
            // 
            // buttonMinimumSize
            // 
            this.buttonMinimumSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMinimumSize.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonMinimumSize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMinimumSize.Font = new System.Drawing.Font("新細明體", 16F);
            this.buttonMinimumSize.ForeColor = System.Drawing.Color.Brown;
            this.buttonMinimumSize.Location = new System.Drawing.Point(1285, 0);
            this.buttonMinimumSize.Name = "buttonMinimumSize";
            this.buttonMinimumSize.Size = new System.Drawing.Size(30, 30);
            this.buttonMinimumSize.TabIndex = 6;
            this.buttonMinimumSize.Text = "◢";
            this.toolTipAll.SetToolTip(this.buttonMinimumSize, "縮小至右下方");
            this.buttonMinimumSize.UseVisualStyleBackColor = true;
            this.buttonMinimumSize.Click += new System.EventHandler(this.buttonMinimumSize_Click);
            // 
            // buttonBackColor
            // 
            this.buttonBackColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBackColor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonBackColor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonBackColor.Font = new System.Drawing.Font("新細明體", 15F);
            this.buttonBackColor.ForeColor = System.Drawing.Color.Brown;
            this.buttonBackColor.Location = new System.Drawing.Point(1227, 0);
            this.buttonBackColor.Name = "buttonBackColor";
            this.buttonBackColor.Size = new System.Drawing.Size(30, 30);
            this.buttonBackColor.TabIndex = 7;
            this.buttonBackColor.Text = "●";
            this.toolTipAll.SetToolTip(this.buttonBackColor, "更換佈景顏色");
            this.buttonBackColor.UseVisualStyleBackColor = true;
            this.buttonBackColor.Click += new System.EventHandler(this.buttonBackColor_Click);
            // 
            // imageList4TreeView
            // 
            this.imageList4TreeView.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList4TreeView.ImageStream")));
            this.imageList4TreeView.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList4TreeView.Images.SetKeyName(0, "18610531421560147113-256.png");
            this.imageList4TreeView.Images.SetKeyName(1, "CaoCyuanBeiCalligraphy48.ico");
            this.imageList4TreeView.Images.SetKeyName(2, "PicPick2021-04-01 12 39 15.png");
            // 
            // imageListPlus
            // 
            this.imageListPlus.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListPlus.ImageStream")));
            this.imageListPlus.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListPlus.Images.SetKeyName(0, "icons8_Sum_28px.png");
            this.imageListPlus.Images.SetKeyName(1, "icons8_Negative_28px.png");
            // 
            // toolTipAll
            // 
            this.toolTipAll.AutomaticDelay = 300;
            // 
            // buttonAlwaysTop
            // 
            this.buttonAlwaysTop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAlwaysTop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonAlwaysTop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonAlwaysTop.Font = new System.Drawing.Font("新細明體", 15F);
            this.buttonAlwaysTop.ForeColor = System.Drawing.Color.Brown;
            this.buttonAlwaysTop.Location = new System.Drawing.Point(1198, 0);
            this.buttonAlwaysTop.Name = "buttonAlwaysTop";
            this.buttonAlwaysTop.Size = new System.Drawing.Size(30, 30);
            this.buttonAlwaysTop.TabIndex = 8;
            this.buttonAlwaysTop.Text = "╩";
            this.toolTipAll.SetToolTip(this.buttonAlwaysTop, "開啟最上層顯示");
            this.buttonAlwaysTop.UseVisualStyleBackColor = true;
            this.buttonAlwaysTop.Click += new System.EventHandler(this.buttonAlwaysTop_Click);
            // 
            // buttonOpacity
            // 
            this.buttonOpacity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOpacity.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonOpacity.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonOpacity.Font = new System.Drawing.Font("新細明體", 15F);
            this.buttonOpacity.ForeColor = System.Drawing.Color.Brown;
            this.buttonOpacity.Location = new System.Drawing.Point(1169, 0);
            this.buttonOpacity.Name = "buttonOpacity";
            this.buttonOpacity.Size = new System.Drawing.Size(30, 30);
            this.buttonOpacity.TabIndex = 9;
            this.buttonOpacity.Text = "▊";
            this.toolTipAll.SetToolTip(this.buttonOpacity, "改變視窗透明度");
            this.buttonOpacity.UseVisualStyleBackColor = true;
            this.buttonOpacity.Click += new System.EventHandler(this.buttonOpacity_Click);
            // 
            // labelPoetry
            // 
            this.labelPoetry.AutoSize = true;
            this.labelPoetry.Font = new System.Drawing.Font("標楷體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelPoetry.Location = new System.Drawing.Point(93, 741);
            this.labelPoetry.Name = "labelPoetry";
            this.labelPoetry.Size = new System.Drawing.Size(56, 16);
            this.labelPoetry.TabIndex = 10;
            this.labelPoetry.Text = "label1";
            // 
            // formFlowerPomodoroTimer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Tomato;
            this.ClientSize = new System.Drawing.Size(1344, 773);
            this.ControlBox = false;
            this.Controls.Add(this.labelPoetry);
            this.Controls.Add(this.buttonOpacity);
            this.Controls.Add(this.buttonAlwaysTop);
            this.Controls.Add(this.buttonBackColor);
            this.Controls.Add(this.buttonMinimumSize);
            this.Controls.Add(this.buttonQuit);
            this.Controls.Add(this.buttonHelp);
            this.Controls.Add(this.labelTotalTimer);
            this.Controls.Add(this.buttonTest);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.labelTimer);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("新細明體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "formFlowerPomodoroTimer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Flower Pomodoro Timer Ver 0.6.0.2   珍惜自己的一分一秒...直到你我相遇...";
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.Label labelPoetry;
    }
}

