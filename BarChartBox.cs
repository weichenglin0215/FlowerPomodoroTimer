using System;
using System.Drawing;
using System.Windows.Forms;

namespace Flower_Pomodoro_Timer
{
    public class BarChartBox : PictureBox
    {
        public string BarText { get; private set; } = string.Empty;
        public float FillRatio { get; private set; }
        public Color BarFillColor { get; private set; } = Color.Tomato;
        public Color BarTextColor { get; private set; } = Color.Brown;

        public BarChartBox()
        {
            BorderStyle = BorderStyle.FixedSingle;
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
        }

        public void SetBar(string text, float ratio, Color fillColor, Color textColor)
        {
            BarText = text;
            FillRatio = Math.Clamp(ratio, 0f, 1f);
            BarFillColor = fillColor;
            BarTextColor = textColor;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            Graphics g = pe.Graphics;
            g.Clear(BackColor);

            int fillWidth = (int)(Width * FillRatio);
            if (fillWidth > 0)
            {
                using var fillBrush = new SolidBrush(BarFillColor);
                g.FillRectangle(fillBrush, 0, 0, fillWidth, Height);
            }

            using var textBrush = new SolidBrush(BarTextColor);
            using var font = new Font("Microsoft JhengHei", 10f, FontStyle.Regular);
            g.DrawString(BarText, font, textBrush, new PointF(4, Math.Max(0, (Height - font.Height) / 2f)));
        }
    }
}
