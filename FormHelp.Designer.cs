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
            SuspendLayout();
            // 
            // buttonOK
            // 
            buttonOK.AutoSize = true;
            buttonOK.FlatStyle = FlatStyle.Flat;
            buttonOK.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            buttonOK.ForeColor = Color.WhiteSmoke;
            buttonOK.Location = new Point(447, 312);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(75, 32);
            buttonOK.TabIndex = 0;
            buttonOK.Text = "懂了";
            buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonOpenStartFolder
            // 
            buttonOpenStartFolder.AutoSize = true;
            buttonOpenStartFolder.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonOpenStartFolder.FlatStyle = FlatStyle.Flat;
            buttonOpenStartFolder.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            buttonOpenStartFolder.ForeColor = Color.WhiteSmoke;
            buttonOpenStartFolder.Location = new Point(687, 173);
            buttonOpenStartFolder.Name = "buttonOpenStartFolder";
            buttonOpenStartFolder.Size = new Size(229, 32);
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
            label1.Location = new Point(68, 53);
            label1.Name = "label1";
            label1.Size = new Size(441, 100);
            label1.TabIndex = 2;
            label1.Text = "一轉眼就來到下班時間才發覺還有些工作還沒完成？\r\n想知道自己一整天都把時間花在那些瑣事上嗎？\r\n番茄花鐘除了每小時會提醒您伸展身體、泡杯飲料放鬆一下，\r\n貼心統計您剛剛在電腦前把時間花在那些應用程式上，\r\n讓您更善於分配自己的工作時間。\r\n";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("微軟正黑體", 12F);
            label2.ForeColor = Color.WhiteSmoke;
            label2.Location = new Point(68, 160);
            label2.Name = "label2";
            label2.Size = new Size(623, 120);
            label2.TabIndex = 3;
            label2.Text = "每次開機後都得去開啟番茄花鐘，是不是覺得有麻煩呢？\r\n貼心服務來了，請點一下右邊的按鍵，下次開機後就會自動開啟番茄花鐘。 ---------->\r\n\r\n其實只是在Windows的啟動資料夾中放上番茄花鐘的捷徑啦！\r\n\r\n番茄花鐘是綠色、無毒且開放原始碼的好工具。";
            // 
            // formHelp
            // 
            AutoScaleDimensions = new SizeF(9F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Tomato;
            ClientSize = new Size(984, 361);
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
    }
}