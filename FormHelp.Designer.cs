namespace Flower_Pomodoro_Timer
{
    partial class formHelp
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonOK = new Button();
            buttonOpenStartFolder = new Button();
            label1 = new Label();
            label2 = new Label();
            labelRestImage = new Label();
            textBoxRestImageFolder = new TextBox();
            buttonSelectRestImageFolder = new Button();
            checkBoxEnableRestImage = new CheckBox();
            buttonTestRestImage = new Button();
            buttonOpenUsageAnalysis = new Button();
            SuspendLayout();
            // 
            // buttonOK
            // 
            buttonOK.AutoSize = true;
            buttonOK.FlatStyle = FlatStyle.Flat;
            buttonOK.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            buttonOK.ForeColor = Color.WhiteSmoke;
            buttonOK.Location = new Point(545, 525);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(75, 37);
            buttonOK.TabIndex = 0;
            buttonOK.Text = "懂了";
            buttonOK.UseVisualStyleBackColor = true;
            // buttonOK 的 DialogResult 已在建構子中設定為 OK，無需額外 Click 事件
            // 
            // buttonOpenStartFolder
            // 
            buttonOpenStartFolder.AutoSize = true;
            buttonOpenStartFolder.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonOpenStartFolder.FlatStyle = FlatStyle.Flat;
            buttonOpenStartFolder.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            buttonOpenStartFolder.ForeColor = Color.WhiteSmoke;
            buttonOpenStartFolder.Location = new Point(793, 193);
            buttonOpenStartFolder.Name = "buttonOpenStartFolder";
            buttonOpenStartFolder.Size = new Size(284, 37);
            buttonOpenStartFolder.TabIndex = 1;
            buttonOpenStartFolder.Text = "在自動執行資料夾中建立捷徑";
            buttonOpenStartFolder.UseVisualStyleBackColor = true;
            buttonOpenStartFolder.Click += buttonOpenStartFolder_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("微軟正黑體", 12F);
            label1.ForeColor = Color.WhiteSmoke;
            label1.Location = new Point(68, 35);
            label1.Name = "label1";
            label1.Size = new Size(552, 125);
            label1.TabIndex = 2;
            label1.Text = "一轉眼就來到下班時間才發覺還有些工作還沒完成？\r\n想知道自己一整天都把時間花在那些瑣事上嗎？\r\n番茄花鐘除了每小時會提醒您伸展身體、泡杯飲料放鬆一下，\r\n貼心統計您剛剛在電腦前把時間花在那些應用程式上，\r\n讓您更善於分配自己的工作時間。\r\n";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("微軟正黑體", 12F);
            label2.ForeColor = Color.WhiteSmoke;
            label2.Location = new Point(68, 174);
            label2.Name = "label2";
            label2.Size = new Size(719, 150);
            label2.TabIndex = 3;
            label2.Text = "每次開機後都得去開啟番茄花鐘，是不是覺得有麻煩呢？\r\n貼心服務來了，請點一下右邊的按鍵，下次開機後就會自動開啟番茄花鐘。 --->\r\n\r\n其實只是在Windows的啟動資料夾中放上番茄花鐘的捷徑啦！\r\n\r\n番茄花鐘是綠色、無毒且開放原始碼的好工具。";
            // label2 僅作說明文字使用，不需要 Click 事件
            // 
            // labelRestImage
            // 
            labelRestImage.AutoSize = true;
            labelRestImage.Font = new Font("Microsoft JhengHei UI", 11F, FontStyle.Bold);
            labelRestImage.ForeColor = Color.WhiteSmoke;
            labelRestImage.Location = new Point(68, 346);
            labelRestImage.Name = "labelRestImage";
            labelRestImage.Size = new Size(162, 24);
            labelRestImage.TabIndex = 4;
            labelRestImage.Text = "休息圖片提醒設定";
            // 
            // textBoxRestImageFolder
            // 
            textBoxRestImageFolder.Location = new Point(68, 373);
            textBoxRestImageFolder.Name = "textBoxRestImageFolder";
            textBoxRestImageFolder.Size = new Size(663, 31);
            textBoxRestImageFolder.TabIndex = 5;
            // 
            // buttonSelectRestImageFolder
            // 
            buttonSelectRestImageFolder.AutoSize = true;
            buttonSelectRestImageFolder.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonSelectRestImageFolder.FlatStyle = FlatStyle.Flat;
            buttonSelectRestImageFolder.Font = new Font("Microsoft JhengHei UI", 11F);
            buttonSelectRestImageFolder.ForeColor = Color.WhiteSmoke;
            buttonSelectRestImageFolder.Location = new Point(747, 373);
            buttonSelectRestImageFolder.Name = "buttonSelectRestImageFolder";
            buttonSelectRestImageFolder.Size = new Size(155, 36);
            buttonSelectRestImageFolder.TabIndex = 6;
            buttonSelectRestImageFolder.Text = "選擇圖片資料夾";
            buttonSelectRestImageFolder.UseVisualStyleBackColor = true;
            buttonSelectRestImageFolder.Click += buttonSelectRestImageFolder_Click;
            // 
            // checkBoxEnableRestImage
            // 
            checkBoxEnableRestImage.AutoSize = true;
            checkBoxEnableRestImage.ForeColor = Color.WhiteSmoke;
            checkBoxEnableRestImage.Location = new Point(68, 418);
            checkBoxEnableRestImage.Name = "checkBoxEnableRestImage";
            checkBoxEnableRestImage.Size = new Size(211, 24);
            checkBoxEnableRestImage.TabIndex = 7;
            checkBoxEnableRestImage.Text = "開啟休息時顯示圖片";
            checkBoxEnableRestImage.UseVisualStyleBackColor = true;
            // 
            // buttonTestRestImage
            // 
            buttonTestRestImage.AutoSize = true;
            buttonTestRestImage.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonTestRestImage.FlatStyle = FlatStyle.Flat;
            buttonTestRestImage.Font = new Font("Microsoft JhengHei UI", 11F);
            buttonTestRestImage.ForeColor = Color.WhiteSmoke;
            buttonTestRestImage.Location = new Point(285, 410);
            buttonTestRestImage.Name = "buttonTestRestImage";
            buttonTestRestImage.Size = new Size(174, 36);
            buttonTestRestImage.TabIndex = 8;
            buttonTestRestImage.Text = "測試休息圖片提醒";
            buttonTestRestImage.UseVisualStyleBackColor = true;
            buttonTestRestImage.Click += buttonTestRestImage_Click;
            // 
            // buttonOpenUsageAnalysis
            // 
            buttonOpenUsageAnalysis.AutoSize = true;
            buttonOpenUsageAnalysis.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonOpenUsageAnalysis.FlatStyle = FlatStyle.Flat;
            buttonOpenUsageAnalysis.Font = new Font("Microsoft JhengHei UI", 11F);
            buttonOpenUsageAnalysis.ForeColor = Color.WhiteSmoke;
            buttonOpenUsageAnalysis.Location = new Point(68, 470);
            buttonOpenUsageAnalysis.Name = "buttonOpenUsageAnalysis";
            buttonOpenUsageAnalysis.Size = new Size(263, 36);
            buttonOpenUsageAnalysis.TabIndex = 9;
            buttonOpenUsageAnalysis.Text = "開啟 使用情形分析-番茄花鐘";
            buttonOpenUsageAnalysis.UseVisualStyleBackColor = true;
            buttonOpenUsageAnalysis.Click += buttonOpenUsageAnalysis_Click;
            // 
            // formHelp
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.Tomato;
            ClientSize = new Size(1182, 583);
            Controls.Add(buttonOpenUsageAnalysis);
            Controls.Add(buttonTestRestImage);
            Controls.Add(checkBoxEnableRestImage);
            Controls.Add(buttonSelectRestImageFolder);
            Controls.Add(textBoxRestImageFolder);
            Controls.Add(labelRestImage);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(buttonOpenStartFolder);
            Controls.Add(buttonOK);
            Font = new Font("新細明體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4);
            Name = "formHelp";
            ShowInTaskbar = false;
            Text = "番茄花鐘 溫馨說明";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonOpenStartFolder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelRestImage;
        private System.Windows.Forms.TextBox textBoxRestImageFolder;
        private System.Windows.Forms.Button buttonSelectRestImageFolder;
        private System.Windows.Forms.CheckBox checkBoxEnableRestImage;
        private System.Windows.Forms.Button buttonTestRestImage;
        private System.Windows.Forms.Button buttonOpenUsageAnalysis;
    }
}
