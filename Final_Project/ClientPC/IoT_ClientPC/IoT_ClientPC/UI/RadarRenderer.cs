using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

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
            using Pen sweepPen = new Pen(Color.LimeGreen, 4);
            using Brush textBrush = new SolidBrush(Color.LimeGreen);
            using Font font = new Font("맑은 고딕", 9, FontStyle.Bold);

            Rectangle fullRadarRect = new Rectangle(
                centerX - maxRadius,
                centerY - maxRadius,
                maxRadius * 2,
                maxRadius * 2
            );

            // 1. 초록 스캔 잔상
            // DrawGreenSweep(g, fullRadarRect, currentAngle, radarDirection);

            // 2. 빨간 감지 영역
            DrawDistancePoint(
                g,
                centerX,
                centerY,
                maxRadius,
                maxDistance,
                currentAngle,
                currentDistance
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

        private void DrawDistancePoint(
            Graphics g,
            int centerX,
            int centerY,
            int maxRadius,
            int maxDistance,
            int currentAngle,
            int currentDistance)
        {
            if (currentDistance <= 0 || currentDistance > maxDistance)
                return;

            float distanceRatio = currentDistance / (float)maxDistance;
            float pointRadiusFromCenter = maxRadius * distanceRatio;

            double rad = Math.PI * currentAngle / 180.0;

            float pointX = centerX + (float)(pointRadiusFromCenter * Math.Cos(Math.PI - rad));
            float pointY = centerY - (float)(pointRadiusFromCenter * Math.Sin(rad));

            Color pointColor = Color.Red;

            float coreSize = 7f;
            float glowSize = 17f;

            using Brush glowBrush = new SolidBrush(Color.FromArgb(80, pointColor));
            using Brush coreBrush = new SolidBrush(Color.FromArgb(230, pointColor));

            g.FillEllipse(
                glowBrush,
                pointX - glowSize,
                pointY - glowSize,
                glowSize * 2,
                glowSize * 2
            );

            g.FillEllipse(
                coreBrush,
                pointX - coreSize,
                pointY - coreSize,
                coreSize * 2,
                coreSize * 2
            );
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
            bool hasDetectedPosition,
            int detectedAngle,
            int detectedDistance,
            int currentAngle)
        {
            if (!hasDetectedPosition)
                return;

            if (detectedDistance <= 0 || detectedDistance > maxDistance)
                return;

            // 스캔바가 감지 위치 근처를 지나갈 때만 보이게 함
            // 이 값을 키우면 빨간 표시가 더 오래 보임
            float visibleAngleRange = 18f;

            float angleDiff = GetAngleDiff(currentAngle, detectedAngle);

            if (angleDiff > visibleAngleRange)
                return;

            // 감지된 거리값을 레이더 반지름으로 변환
            float distanceRatio = detectedDistance / (float)maxDistance;
            float detectedRadius = maxRadius * distanceRatio;

            // 레이더 좌표계 변환
            double rad = Math.PI * detectedAngle / 180.0;

            float targetX = centerX + (float)(detectedRadius * Math.Cos(Math.PI - rad));
            float targetY = centerY - (float)(detectedRadius * Math.Sin(rad));

            // 스캔바 중심에 가까울수록 더 진하게 표시
            float visibility = 1f - (angleDiff / visibleAngleRange);

            int glowAlpha = (int)(60 + 80 * visibility);
            int midAlpha = (int)(90 + 90 * visibility);
            int coreAlpha = (int)(150 + 80 * visibility);

            // 표시 크기
            // 이 값을 키우면 빨간 감지 표시가 더 커짐
            float blobRadius = 16f;
            float glowRadius = blobRadius * 2.2f;
            float midRadius = blobRadius * 1.45f;
            float coreRadius = blobRadius * 0.85f;

            using Brush glowBrush = new SolidBrush(Color.FromArgb(glowAlpha, 255, 40, 20));
            using Brush midBrush = new SolidBrush(Color.FromArgb(midAlpha, 255, 80, 40));
            using Brush coreBrush = new SolidBrush(Color.FromArgb(coreAlpha, 255, 0, 0));

            // 바깥 glow
            g.FillEllipse(
                glowBrush,
                targetX - glowRadius,
                targetY - glowRadius,
                glowRadius * 2,
                glowRadius * 2
            );

            // 중간 빨강
            g.FillEllipse(
                midBrush,
                targetX - midRadius,
                targetY - midRadius,
                midRadius * 2,
                midRadius * 2
            );

            // 중심 진한 빨강
            g.FillEllipse(
                coreBrush,
                targetX - coreRadius,
                targetY - coreRadius,
                coreRadius * 2,
                coreRadius * 2
            );
        }

        private float GetAngleDiff(int angle1, int angle2)
        {
            return Math.Abs(angle1 - angle2);
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