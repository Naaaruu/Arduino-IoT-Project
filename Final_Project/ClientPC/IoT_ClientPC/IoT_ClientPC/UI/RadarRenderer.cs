using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IoT_ClientPC.UI
{
    public class RadarRenderer
    {
        public void Draw(
            Graphics g,
            Rectangle bounds,
            int currentAngle,
            int currentDistance,
            int radarDirection)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int width = bounds.Width;
            int height = bounds.Height;

            g.Clear(Color.Black);

            int centerX = width / 2;
            int centerY = height - 45;
            int maxRadius = Math.Min(width - 90, height - 120);
            int maxDistance = 40;

            using Pen gridPen = new Pen(Color.LimeGreen, 2);
            using Pen sweepPen = new Pen(Color.LimeGreen, 3);
            using Brush textBrush = new SolidBrush(Color.LimeGreen);
            using Font font = new Font("맑은 고딕", 9, FontStyle.Bold);

            Rectangle fullRadarRect = new Rectangle(
                centerX - maxRadius,
                centerY - maxRadius,
                maxRadius * 2,
                maxRadius * 2
            );

            // 1. 초록 스캔 잔상
            DrawGreenSweep(g, fullRadarRect, currentAngle, radarDirection);

            // 2. 빨간 감지 영역
            DrawDetectedArea(
                g,
                centerX,
                centerY,
                maxRadius,
                maxDistance,
                currentAngle,
                currentDistance,
                radarDirection
            );

            // 3. 격자선은 색상 효과 위에 다시 그림
            DrawGrid(g, gridPen, font, textBrush, centerX, centerY, maxRadius);

            // 4. 현재 스캔 방향선
            double currentRad = Math.PI * currentAngle / 180.0;

            int lineX = centerX + (int)(maxRadius * Math.Cos(Math.PI - currentRad));
            int lineY = centerY - (int)(maxRadius * Math.Sin(currentRad));

            g.DrawLine(sweepPen, centerX, centerY, lineX, lineY);

            // 5. 하단 정보 표시
            g.DrawString($"Angle: {currentAngle}°", font, textBrush, 20, height - 30);
            g.DrawString($"Distance: {currentDistance} cm", font, textBrush, width - 160, height - 30);
        }

        private void DrawGreenSweep(
            Graphics g,
            Rectangle fullRadarRect,
            int currentAngle,
            int radarDirection)
        {
            int trailCount = 48;
            float sectorWidth = 1.9f;

            for (int i = trailCount - 1; i >= 0; i--)
            {
                float t = i / (float)(trailCount - 1);
                float fade = (float)Math.Pow(1f - t, 1.8f);

                int trailAngle = currentAngle - i * radarDirection;
                float sectorStartAngle = 180 + trailAngle - sectorWidth / 2f;

                Color darkGreen = Color.FromArgb(0, 50, 0);
                Color midGreen = Color.FromArgb(0, 170, 0);
                Color brightGreen = Color.FromArgb(100, 255, 100);

                Color sweepColor;
                if (fade < 0.55f)
                    sweepColor = LerpColor(darkGreen, midGreen, fade / 0.55f);
                else
                    sweepColor = LerpColor(midGreen, brightGreen, (fade - 0.55f) / 0.45f);

                int alpha = (int)(15 + 150 * fade);

                using Brush trailBrush = new SolidBrush(Color.FromArgb(alpha, sweepColor));
                g.FillPie(trailBrush, fullRadarRect, sectorStartAngle, sectorWidth);
            }
        }

        private void DrawDetectedArea(
            Graphics g,
            int centerX,
            int centerY,
            int maxRadius,
            int maxDistance,
            int currentAngle,
            int currentDistance,
            int radarDirection)
        {
            if (currentDistance <= 0 || currentDistance > maxDistance)
                return;

            int detectedRadius = (int)(maxRadius * (currentDistance / (double)maxDistance));

            int redTrailCount = 42;
            float redSectorWidth = 1.7f;

            for (int i = redTrailCount - 1; i >= 0; i--)
            {
                float t = i / (float)(redTrailCount - 1);
                float fade = (float)Math.Pow(1f - t, 1.65f);

                int trailAngle = currentAngle - i * radarDirection;
                float sectorStartAngle = 180 + trailAngle - redSectorWidth / 2f;

                int innerOffset = (int)(8f + i * 2.4f + 9f * (float)Math.Sin(i * 0.48f));
                int innerRadius = detectedRadius + innerOffset;

                if (innerRadius < detectedRadius)
                    innerRadius = detectedRadius;

                if (innerRadius > maxRadius - 18)
                    innerRadius = maxRadius - 18;

                int outerRadius = maxRadius - (int)(i * 0.35f);

                if (outerRadius <= innerRadius + 10)
                    outerRadius = innerRadius + 10;

                if (outerRadius > maxRadius)
                    outerRadius = maxRadius;

                Color darkRed = Color.FromArgb(80, 0, 0);
                Color midRed = Color.FromArgb(210, 20, 20);
                Color brightRed = Color.FromArgb(255, 90, 55);

                Color redTone;
                if (fade < 0.55f)
                    redTone = LerpColor(darkRed, midRed, fade / 0.55f);
                else
                    redTone = LerpColor(midRed, brightRed, (fade - 0.55f) / 0.45f);

                int alpha = (int)(12 + 155 * fade);

                using Brush detectedBrush = new SolidBrush(Color.FromArgb(alpha, redTone));

                FillRadarRingSector(
                    g,
                    detectedBrush,
                    centerX,
                    centerY,
                    innerRadius,
                    outerRadius,
                    sectorStartAngle,
                    redSectorWidth
                );
            }
        }

        private void DrawGrid(
            Graphics g,
            Pen gridPen,
            Font font,
            Brush textBrush,
            int centerX,
            int centerY,
            int maxRadius)
        {
            // 거리 반원 눈금
            for (int r = maxRadius / 4; r <= maxRadius; r += maxRadius / 4)
            {
                Rectangle rect = new Rectangle(centerX - r, centerY - r, r * 2, r * 2);
                g.DrawArc(gridPen, rect, 180, 180);
            }

            // 각도선
            for (int angle = 0; angle <= 180; angle += 30)
            {
                double rad = Math.PI * angle / 180.0;

                int x = centerX + (int)(maxRadius * Math.Cos(Math.PI - rad));
                int y = centerY - (int)(maxRadius * Math.Sin(rad));

                g.DrawLine(gridPen, centerX, centerY, x, y);

                int labelX = centerX + (int)((maxRadius + 12) * Math.Cos(Math.PI - rad));
                int labelY = centerY - (int)((maxRadius + 12) * Math.Sin(rad));

                g.DrawString($"{angle}°", font, textBrush, labelX - 15, labelY - 10);
            }
        }

        private void FillRadarRingSector(
            Graphics g,
            Brush brush,
            int centerX,
            int centerY,
            int innerRadius,
            int outerRadius,
            float startAngle,
            float sweepAngle)
        {
            using GraphicsPath path = new GraphicsPath();

            Rectangle outerRect = new Rectangle(
                centerX - outerRadius,
                centerY - outerRadius,
                outerRadius * 2,
                outerRadius * 2
            );

            Rectangle innerRect = new Rectangle(
                centerX - innerRadius,
                centerY - innerRadius,
                innerRadius * 2,
                innerRadius * 2
            );

            path.AddArc(outerRect, startAngle, sweepAngle);
            path.AddArc(innerRect, startAngle + sweepAngle, -sweepAngle);
            path.CloseFigure();

            g.FillPath(brush, path);
        }

        private Color LerpColor(Color c1, Color c2, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));

            int a = (int)(c1.A + (c2.A - c1.A) * t);
            int r = (int)(c1.R + (c2.R - c1.R) * t);
            int g = (int)(c1.G + (c2.G - c1.G) * t);
            int b = (int)(c1.B + (c2.B - c1.B) * t);

            return Color.FromArgb(a, r, g, b);
        }
    }
}