package com.example.androidclient;

import android.content.Context;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.PointF;
import android.util.AttributeSet;
import android.view.View;

import java.util.ArrayList;
import java.util.List;

public class DistanceGraphView extends View {

    private final Paint gridPaint = new Paint();
    private final Paint linePaint = new Paint();
    private final Paint textPaint = new Paint();

    private final List<Integer> distanceHistory = new ArrayList<>();

    private int maxDistance = 40;

    public DistanceGraphView(Context context) {
        super(context);
        init();
    }

    public DistanceGraphView(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    private void init() {
        gridPaint.setColor(Color.rgb(0, 180, 0));
        gridPaint.setStrokeWidth(2f);
        gridPaint.setStyle(Paint.Style.STROKE);

        linePaint.setColor(Color.rgb(0, 200, 255));
        linePaint.setStrokeWidth(4f);
        linePaint.setStyle(Paint.Style.STROKE);
        linePaint.setAntiAlias(true);

        textPaint.setColor(Color.rgb(0, 255, 0));
        textPaint.setTextSize(28f);
        textPaint.setAntiAlias(true);
    }

    public void addDistance(int distance) {
        distanceHistory.add(distance);

        if (distanceHistory.size() > 60) {
            distanceHistory.remove(0);
        }

        invalidate();
    }

    public void clearData() {
        distanceHistory.clear();
        invalidate();
    }

    public void setMaxDistance(int maxDistance) {
        this.maxDistance = maxDistance;
        invalidate();
    }

    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);

        int width = getWidth();
        int height = getHeight();

        canvas.drawColor(Color.BLACK);

        int paddingLeft = 55;
        int paddingRight = 20;
        int paddingTop = 25;
        int paddingBottom = 40;

        int graphLeft = paddingLeft;
        int graphTop = paddingTop;
        int graphRight = width - paddingRight;
        int graphBottom = height - paddingBottom;

        int graphWidth = graphRight - graphLeft;
        int graphHeight = graphBottom - graphTop;

        // 테두리
        canvas.drawRect(graphLeft, graphTop, graphRight, graphBottom, gridPaint);

        // 가로 격자
        for (int i = 1; i <= 4; i++) {
            float y = graphTop + graphHeight * i / 4f;
            canvas.drawLine(graphLeft, y, graphRight, y, gridPaint);
        }

        // 세로 격자
        for (int i = 1; i <= 5; i++) {
            float x = graphLeft + graphWidth * i / 5f;
            canvas.drawLine(x, graphTop, x, graphBottom, gridPaint);
        }

        // y축 텍스트
        canvas.drawText(maxDistance + "cm", 5, graphTop + 10, textPaint);
        canvas.drawText("0cm", 10, graphBottom, textPaint);

        if (distanceHistory.size() < 2) {
            canvas.drawText("Waiting data...", graphLeft + 30, height / 2f, textPaint);
            return;
        }

        PointF previous = null;

        for (int i = 0; i < distanceHistory.size(); i++) {
            int distance = distanceHistory.get(i);

            if (distance < 0) distance = 0;
            if (distance > maxDistance) distance = maxDistance;

            float x = graphLeft + graphWidth * (i / (float) (distanceHistory.size() - 1));
            float ratio = distance / (float) maxDistance;
            float y = graphBottom - graphHeight * ratio;

            PointF current = new PointF(x, y);

            if (previous != null) {
                canvas.drawLine(previous.x, previous.y, current.x, current.y, linePaint);
            }

            previous = current;
        }

        int latest = distanceHistory.get(distanceHistory.size() - 1);
        canvas.drawText("Now: " + latest + " cm", graphLeft, height - 10, textPaint);
    }
}