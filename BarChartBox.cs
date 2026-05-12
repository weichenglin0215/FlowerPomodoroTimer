using System;
using System.Drawing;
using System.Windows.Forms;

namespace Flower_Pomodoro_Timer
{
    /// <summary>
    /// 自訂橫條圖元件，繼承自 PictureBox。
    /// 用於顯示各程序的視窗使用時間比例，以及 CPU／RAM／Disk／GPU／VRAM 等效能監控數值。
    /// 支援文字標籤、填充比例、顏色自訂，並啟用雙緩衝以消除重繪閃爍。
    /// </summary>
    public class BarChartBox : PictureBox
    {
        /// <summary>顯示在橫條上的文字標籤（例如 "CPU: 42.3%"）。</summary>
        public string BarText { get; private set; } = string.Empty;

        /// <summary>橫條填充比例，範圍 0.0（空）~ 1.0（全滿）。</summary>
        public float FillRatio { get; private set; }

        /// <summary>橫條的填充顏色。</summary>
        public Color BarFillColor { get; private set; } = Color.Tomato;

        /// <summary>文字的顯示顏色。</summary>
        public Color BarTextColor { get; private set; } = Color.Brown;

        public BarChartBox()
        {
            BorderStyle = BorderStyle.FixedSingle;
            // 開啟使用者自訂繪製模式與雙緩衝，避免系統預設繪製與重繪時的閃爍
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
        }

        /// <summary>
        /// 設定橫條的顯示內容並立即觸發重繪。
        /// </summary>
        /// <param name="text">顯示在橫條上的說明文字</param>
        /// <param name="ratio">填充比例（超出 0~1 範圍將自動夾制）</param>
        /// <param name="fillColor">填充色塊的顏色</param>
        /// <param name="textColor">文字的顏色</param>
        public void SetBar(string text, float ratio, Color fillColor, Color textColor)
        {
            BarText = text;
            FillRatio = Math.Clamp(ratio, 0f, 1f);
            BarFillColor = fillColor;
            BarTextColor = textColor;
            Invalidate();
        }

        /// <summary>
        /// 自訂繪製流程：
        /// 1. 清除背景
        /// 2. 依填充比例繪製色塊
        /// 3. 垂直置中繪製文字標籤
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            Graphics g = pe.Graphics;
            g.Clear(BackColor);

            // 依比例計算填充寬度，並繪製色塊
            int fillWidth = (int)(Width * FillRatio);
            if (fillWidth > 0)
            {
                using var fillBrush = new SolidBrush(BarFillColor);
                g.FillRectangle(fillBrush, 0, 0, fillWidth, Height);
            }

            // 文字垂直置中顯示，左邊留 4px 間距
            using var textBrush = new SolidBrush(BarTextColor);
            using var font = new Font("Microsoft JhengHei", 10f, FontStyle.Regular);
            g.DrawString(BarText, font, textBrush, new PointF(4, Math.Max(0, (Height - font.Height) / 2f)));
        }
    }
}
