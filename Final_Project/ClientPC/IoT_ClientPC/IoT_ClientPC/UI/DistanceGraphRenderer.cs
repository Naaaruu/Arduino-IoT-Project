using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IoT_ClientPC.UI
{
    public class DistanceGraphRenderer
    {
        public void Draw(Graphics g, Rectangle bounds, List<int> distances, int maxDistance)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Black);

            int width = bounds.Width;
            int height = bounds.Height;

            int paddingLeft = 35;
            int paddingRight = 10;
            int paddingTop = 15;
            int paddingBottom = 25;

            int graphWidth = width - paddingLeft - paddingRight;
            int graphHeight = height - paddingTop - paddingBottom;

            Rectangle graphArea = new Rectangle(
                paddingLeft,
                paddingTop,
                graphWidth,
                graphHeight
            );

            using Pen gridPen = new Pen(Color.FromArgb(80, Color.LimeGreen), 1);
            using Pen axisPen = new Pen(Color.LimeGreen, 1);
            using Pen linePen = new Pen(Color.DeepSkyBlue, 2);
            using Brush textBrush = new SolidBrush(Color.LimeGreen);
            using Font font = new Font("맑은 고딕", 8, FontStyle.Bold);

            // 그래프 테두리
            g.DrawRectangle(axisPen, graphArea);

            // 가로 격자선
            for (int i = 1; i <= 4; i++)
            {
                int y = paddingTop + graphHeight * i / 4;
                g.DrawLine(gridPen, paddingLeft, y, paddingLeft + graphWidth, y);
            }

            // 세로 격자선
            for (int i = 1; i <= 5; i++)
            {
                int x = paddingLeft + graphWidth * i / 5;
                g.DrawLine(gridPen, x, paddingTop, x, paddingTop + graphHeight);
            }

            // Y축 값 표시
            g.DrawString($"{maxDistance}cm", font, textBrush, 2, paddingTop - 5);
            g.DrawString("0cm", font, textBrush, 8, paddingTop + graphHeight - 10);

            if (distances.Count < 2)
            {
                g.DrawString("Waiting data...", font, textBrush, paddingLeft + 20, height / 2 - 10);
                return;
            }

            int count = distances.Count;
            PointF[] points = new PointF[count];

            for (int i = 0; i < count; i++)
            {
                float x = paddingLeft + graphWidth * (i / (float)(count - 1));

                int distance = Math.Max(0, Math.Min(distances[i], maxDistance));
                float ratio = distance / (float)maxDistance;

                float y = paddingTop + graphHeight - graphHeight * ratio;

                points[i] = new PointF(x, y);
            }

            g.DrawLines(linePen, points);

            // 현재값 표시
            int latest = distances[distances.Count - 1];
            string latestText = $"Now: {latest} cm";
            g.DrawString(latestText, font, textBrush, paddingLeft + 5, height - 22);
        }
    }
}
