package com.example.androidclient;

import android.content.Context;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.RectF;
import android.util.AttributeSet;
import android.view.View;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

public class RadarView extends View {

    private boolean isObjectDetected = false;

    private final Paint gridPaint = new Paint();
    private final Paint sweepPaint = new Paint();
    private final Paint detectedPaint = new Paint();
    private final Paint textPaint = new Paint();

    private final List<RadarPoint> radarPoints = new ArrayList<>();

    private static final int MAX_DISTANCE = 40;
    private static final int POINT_LIFE_MS = 3000;
    private static final int POINT_ANGLE_BUCKET = 4;

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

    public void addRadarPoint(int angle, int distance) {
        if (distance <= 0 || distance > MAX_DISTANCE) {
            return;
        }

        // 감지 범위 밖이면 점을 안 찍음
        // 30cm 이하만 레이더 감지점으로 표시
        if (distance > 30) {
            removeExpiredRadarPoints();
            invalidate();
            return;
        }

        long now = System.currentTimeMillis();

        boolean timeEnough = now - lastPointTime >= POINT_INTERVAL_MS;
        boolean angleEnough = Math.abs(angle - lastPointAngle) >= POINT_ANGLE_GAP;

        if (!timeEnough && !angleEnough) {
            removeExpiredRadarPoints();
            invalidate();
            return;
        }

        int angleBucket = angle / POINT_ANGLE_BUCKET * POINT_ANGLE_BUCKET;

        // 같은 각도 근처 점은 새 값으로 갱신
        Iterator<RadarPoint> iterator = radarPoints.iterator();
        while (iterator.hasNext()) {
            RadarPoint point = iterator.next();

            if (Math.abs(point.angle - angleBucket) <= POINT_ANGLE_BUCKET / 2) {
                iterator.remove();
            }
        }

        radarPoints.add(new RadarPoint(angleBucket, distance, now));

        removeExpiredRadarPoints();
        invalidate();
    }

    private void removeExpiredRadarPoints() {
        long now = System.currentTimeMillis();

        Iterator<RadarPoint> iterator = radarPoints.iterator();
        while (iterator.hasNext()) {
            RadarPoint point = iterator.next();

            if (now - point.createdAt > POINT_LIFE_MS) {
                iterator.remove();
            }
        }
    }

    public void clearRadarPoints() {
        radarPoints.clear();
        invalidate();
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

        // 최근 3초 동안의 감지점
        drawRadarPoints(canvas, centerX, centerY, maxRadius);

        // 현재 스캔 중심선
        double currentRad = Math.PI * currentAngle / 180.0;
        float lineX = centerX + (float) (maxRadius * Math.cos(Math.PI - currentRad));
        float lineY = centerY - (float) (maxRadius * Math.sin(currentRad));

        canvas.drawLine(centerX, centerY, lineX, lineY, gridPaint);

        // 하단 정보
        canvas.drawText("Angle: " + currentAngle + "°", 15, height - 12, textPaint);
        canvas.drawText("Distance: " + currentDistance + " cm", width - 220, height - 12, textPaint);
    }

    private void drawRadarPoints(Canvas canvas, int centerX, int centerY, int maxRadius) {
        removeExpiredRadarPoints();

        long now = System.currentTimeMillis();

        for (RadarPoint point : radarPoints) {
            if (point.distance <= 0 || point.distance > MAX_DISTANCE) {
                continue;
            }

            float distanceRatio = point.distance / (float) MAX_DISTANCE;
            float pointRadius = maxRadius * distanceRatio;

            double rad = Math.PI * point.angle / 180.0;

            float pointX = centerX + (float) (pointRadius * Math.cos(Math.PI - rad));
            float pointY = centerY - (float) (pointRadius * Math.sin(rad));

            int pointColor;

            if (point.distance <= 15) {
                pointColor = Color.RED;
            } else if (point.distance <= 30) {
                pointColor = Color.rgb(255, 193, 7);
            } else {
                pointColor = Color.rgb(69, 209, 90);
            }

            float ageRatio = (now - point.createdAt) / (float) POINT_LIFE_MS;
            float fade = 1f - ageRatio;

            if (fade < 0f) {
                fade = 0f;
            }

            int glowAlpha = (int) (80 * fade);
            int coreAlpha = (int) (230 * fade);

            Paint pointPaint = new Paint();
            pointPaint.setAntiAlias(true);
            pointPaint.setStyle(Paint.Style.FILL);

            // 바깥 glow
            pointPaint.setColor(applyAlpha(pointColor, glowAlpha));
            canvas.drawCircle(pointX, pointY, 16f, pointPaint);

            // 중심점
            pointPaint.setColor(applyAlpha(pointColor, coreAlpha));
            canvas.drawCircle(pointX, pointY, 7f, pointPaint);
        }
    }

    private int applyAlpha(int color, int alpha) {
        return Color.argb(
                alpha,
                Color.red(color),
                Color.green(color),
                Color.blue(color)
        );
    }

    private long lastPointTime = 0;
    private int lastPointAngle = -999;

    private static final int POINT_INTERVAL_MS = 250;
    private static final int POINT_ANGLE_GAP = 6;
}