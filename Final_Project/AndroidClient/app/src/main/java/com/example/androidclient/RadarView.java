package com.example.androidclient;

import android.content.Context;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.RectF;
import android.util.AttributeSet;
import android.view.View;

public class RadarView extends View {

    private boolean isObjectDetected = false;

    private final Paint gridPaint = new Paint();
    private final Paint sweepPaint = new Paint();
    private final Paint detectedPaint = new Paint();
    private final Paint textPaint = new Paint();

    private int currentAngle = 90;
    private int currentDistance = 40;
    private int radarDirection = 1;

    private int maxDistance = 40;

    public RadarView(Context context) {
        super(context);
        init();
    }

    public RadarView(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    private void init() {
        gridPaint.setColor(Color.rgb(0, 255, 0));
        gridPaint.setStrokeWidth(3f);
        gridPaint.setStyle(Paint.Style.STROKE);
        gridPaint.setAntiAlias(true);

        sweepPaint.setColor(Color.argb(120, 0, 255, 0));
        sweepPaint.setStyle(Paint.Style.FILL);
        sweepPaint.setAntiAlias(true);

        detectedPaint.setColor(Color.argb(170, 255, 40, 40));
        detectedPaint.setStyle(Paint.Style.FILL);
        detectedPaint.setAntiAlias(true);

        textPaint.setColor(Color.rgb(0, 255, 0));
        textPaint.setTextSize(28f);
        textPaint.setAntiAlias(true);
    }

    public void updateRadar(int angle, int distance, int direction, boolean detected) {
        currentAngle = angle;
        currentDistance = distance;
        radarDirection = direction;
        isObjectDetected = detected;
        invalidate();
    }

    public void resetRadar() {
        currentAngle = 90;
        currentDistance = 40;
        radarDirection = 1;
        isObjectDetected = false;
        invalidate();
    }

    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);

        int width = getWidth();
        int height = getHeight();

        canvas.drawColor(Color.BLACK);

        int centerX = width / 2;
        int centerY = height - 45;
        int maxRadius = Math.min(width - 40, height - 80);

        RectF fullRect = new RectF(
                centerX - maxRadius,
                centerY - maxRadius,
                centerX + maxRadius,
                centerY + maxRadius
        );

        // 거리 반원 눈금
        for (int r = maxRadius / 4; r <= maxRadius; r += maxRadius / 4) {
            RectF rect = new RectF(
                    centerX - r,
                    centerY - r,
                    centerX + r,
                    centerY + r
            );
            canvas.drawArc(rect, 180, 180, false, gridPaint);
        }

        // 각도선
        for (int angle = 30; angle <= 150; angle += 30) {
            double rad = Math.PI * angle / 180.0;

            float x = centerX + (float) (maxRadius * Math.cos(Math.PI - rad));
            float y = centerY - (float) (maxRadius * Math.sin(rad));

            canvas.drawLine(centerX, centerY, x, y, gridPaint);

            float labelX = centerX + (float) ((maxRadius + 8) * Math.cos(Math.PI - rad));
            float labelY = centerY - (float) ((maxRadius + 8) * Math.sin(rad));

            canvas.drawText(angle + "°", labelX - 25, labelY, textPaint);
        }

        // 바닥선
        canvas.drawLine(centerX - maxRadius, centerY, centerX + maxRadius, centerY, gridPaint);

        // 현재 거리 위치 점
        if (isObjectDetected && currentDistance > 0 && currentDistance <= maxDistance) {
            float distanceRatio = currentDistance / (float) maxDistance;
            float pointRadius = maxRadius * distanceRatio;

            double pointRad = Math.PI * currentAngle / 180.0;

            float pointX = centerX + (float) (pointRadius * Math.cos(Math.PI - pointRad));
            float pointY = centerY - (float) (pointRadius * Math.sin(pointRad));

            Paint pointPaint = new Paint();
            pointPaint.setAntiAlias(true);
            pointPaint.setStyle(Paint.Style.FILL);

            // 바깥 glow
            pointPaint.setColor(Color.argb(90, 255, 0, 0));
            canvas.drawCircle(pointX, pointY, 18f, pointPaint);

            // 중심점
            pointPaint.setColor(Color.argb(230, 255, 0, 0));
            canvas.drawCircle(pointX, pointY, 7f, pointPaint);
        }

        // 현재 스캔 중심선
        double currentRad = Math.PI * currentAngle / 180.0;
        float lineX = centerX + (float) (maxRadius * Math.cos(Math.PI - currentRad));
        float lineY = centerY - (float) (maxRadius * Math.sin(currentRad));

        canvas.drawLine(centerX, centerY, lineX, lineY, gridPaint);

        // 하단 정보
        canvas.drawText("Angle: " + currentAngle + "°", 15, height - 12, textPaint);
        canvas.drawText("Distance: " + currentDistance + " cm", width - 220, height - 12, textPaint);
    }
}