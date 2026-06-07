package com.example.androidclient;

public class RadarPoint {
    public int angle;
    public int distance;
    public long createdAt;

    public RadarPoint(int angle, int distance, long createdAt) {
        this.angle = angle;
        this.distance = distance;
        this.createdAt = createdAt;
    }
}