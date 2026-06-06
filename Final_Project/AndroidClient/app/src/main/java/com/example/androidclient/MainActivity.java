package com.example.androidclient;

import android.graphics.Color;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;

import android.os.Handler;
import android.os.Looper;

import androidx.appcompat.app.AppCompatActivity;
import androidx.core.view.ViewCompat;
import androidx.core.view.WindowInsetsCompat;

public class MainActivity extends AppCompatActivity {

    private EditText editIp;
    private EditText editPort;

    private DistanceGraphView graphView;
    private RadarView radarView;

    private Button btnConnect;
    private Button btnReset;
    private Button btnAllow;
    private Button btnWarning;

    private TextView txtConnectionStatus;
    private TextView txtStatus;

    private View viewDistanceIndicator;

    private boolean isConnected = false;
    private boolean isAlarmActive = false;
    private boolean isAllowed = false;
    private boolean isRadarRunning = false;

    private final TcpClientManager tcpClientManager = new TcpClientManager();
    private Thread receiveThread;
    private boolean isReceiving = false;

    private int currentAngle = 90;
    private int currentDistance = 40;
    private int radarDirection = 1;

    private final Handler radarHandler = new Handler(Looper.getMainLooper());

    private static final int DANGER_DISTANCE = 15;
    private static final int WARNING_DISTANCE = 30;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // EdgeToEdge.enable(this);
        setContentView(R.layout.activity_main);

        ViewCompat.setOnApplyWindowInsetsListener(findViewById(R.id.main), (v, insets) -> {
            int left = insets.getInsets(WindowInsetsCompat.Type.systemBars()).left;
            int top = insets.getInsets(WindowInsetsCompat.Type.systemBars()).top;
            int right = insets.getInsets(WindowInsetsCompat.Type.systemBars()).right;
            int bottom = insets.getInsets(WindowInsetsCompat.Type.systemBars()).bottom;

            v.setPadding(left + 16, top + 16, right + 16, bottom + 16);
            return insets;
        });

        initViews();
        initButtonEvents();
        updateStatusUi();
    }

    private void initViews() {
        editIp = findViewById(R.id.editIp);
        editPort = findViewById(R.id.editPort);

        btnConnect = findViewById(R.id.btnConnect);
        btnReset = findViewById(R.id.btnReset);
        btnAllow = findViewById(R.id.btnAllow);
        btnWarning = findViewById(R.id.btnWarning);

        txtConnectionStatus = findViewById(R.id.txtConnectionStatus);
        txtStatus = findViewById(R.id.txtStatus);

        graphView = findViewById(R.id.graphView);
        radarView = findViewById(R.id.radarView);

        viewDistanceIndicator = findViewById(R.id.viewDistanceIndicator);
    }

    private void initButtonEvents() {
        btnConnect.setOnClickListener(v -> {
            if (!tcpClientManager.isConnected()) {
                connectToServer();
            } else {
                disconnectFromServer();
            }
        });

        btnWarning.setOnClickListener(v -> {
            isAlarmActive = true;
            isAllowed = false;

            updateStatusUi();

            sendCommand("CMD:WARN");
        });

        btnAllow.setOnClickListener(v -> {
            isAllowed = true;
            isAlarmActive = false;

            updateStatusUi();

            sendCommand("CMD:ALLOW");
        });

        btnReset.setOnClickListener(v -> {
            stopRadarTest();

            isAlarmActive = false;
            isAllowed = false;

            currentAngle = 90;
            currentDistance = 40;
            radarDirection = 1;

            graphView.clearData();
            radarView.resetRadar();

            updateStatusUi();

            sendCommand("CMD:RESET");
        });
    }

    private void connectToServer() {
        String ip = editIp.getText().toString().trim();
        String portText = editPort.getText().toString().trim();

        if (ip.isEmpty() || portText.isEmpty()) {
            txtConnectionStatus.setText("IP/Port 입력 필요");
            return;
        }

        int port;

        try {
            port = Integer.parseInt(portText);
        } catch (NumberFormatException e) {
            txtConnectionStatus.setText("Port 오류");
            return;
        }

        txtConnectionStatus.setText("Connecting...");

        new Thread(() -> {
            try {
                tcpClientManager.connect(ip, port);

                // 서버에게 스마트폰도 Client 역할이라고 알려줌
                tcpClientManager.send("ROLE:CLIENT");

                runOnUiThread(() -> {
                    isConnected = true;
                    txtConnectionStatus.setText("Connected");
                    btnConnect.setText("해제");
                });

                startReceiveLoop();

            } catch (Exception ex) {
                runOnUiThread(() -> {
                    isConnected = false;
                    txtConnectionStatus.setText("연결 실패");
                    btnConnect.setText("연결");
                });
            }
        }).start();
    }

    private void disconnectFromServer() {
        isReceiving = false;

        if (receiveThread != null) {
            receiveThread.interrupt();
            receiveThread = null;
        }

        tcpClientManager.disconnect();

        isConnected = false;
        txtConnectionStatus.setText("Disconnected");
        btnConnect.setText("연결");
    }


    private void startReceiveLoop() {
        isReceiving = true;

        receiveThread = new Thread(() -> {
            while (isReceiving && tcpClientManager.isConnected()) {
                try {
                    String message = tcpClientManager.receive();

                    if (message == null) {
                        break;
                    }

                    runOnUiThread(() -> {
                        applyRadarMessage(message);
                    });

                } catch (Exception ex) {
                    break;
                }
            }

            runOnUiThread(() -> {
                if (isReceiving) {
                    disconnectFromServer();
                }
            });
        });

        receiveThread.start();
    }

    private void applyRadarMessage(String message) {
        if (message == null) {
            return;
        }

        String[] parts = message.trim().split(":");

        if (parts.length != 3) {
            return;
        }

        if (!parts[0].equals("RADAR")) {
            return;
        }

        try {
            currentAngle = Integer.parseInt(parts[1]);
            currentDistance = Integer.parseInt(parts[2]);

            // 감지 여부 판단
            // 30cm 이하일 때만 레이더에 점 표시
            boolean detected = currentDistance <= 30;

            // 서버에서 실제 RADAR 값을 받았을 때만 그래프/레이더 갱신
            graphView.addDistance(currentDistance);
            radarView.updateRadar(currentAngle, currentDistance, radarDirection, detected);

            updateStatusUi();

        } catch (NumberFormatException ignored) {
        }
    }

    private void updateStatusUi() {
        graphView.addDistance(currentDistance);

        if (isAlarmActive) {
            txtStatus.setText("DANGER");
            txtStatus.setBackgroundColor(Color.rgb(220, 70, 70));
            setIndicatorColor("RED");
        } else if (isAllowed) {
            txtStatus.setText("ALLOWED");
            txtStatus.setBackgroundColor(Color.rgb(30, 144, 255));
            setIndicatorColor("BLUE");
        } else if (currentDistance <= DANGER_DISTANCE) {
            txtStatus.setText("DANGER");
            txtStatus.setBackgroundColor(Color.rgb(220, 70, 70));
            setIndicatorColor("RED");
        } else if (currentDistance <= WARNING_DISTANCE) {
            txtStatus.setText("WARNING");
            txtStatus.setBackgroundColor(Color.rgb(255, 193, 7));
            setIndicatorColor("YELLOW");
        } else {
            txtStatus.setText("SAFE");
            txtStatus.setBackgroundColor(Color.rgb(27, 143, 27));
            setIndicatorColor("GREEN");
        }
    }

    private final Runnable radarRunnable = new Runnable() {
        @Override
        public void run() {
            if (!isRadarRunning) {
                return;
            }

            currentAngle += radarDirection * 2;

            if (currentAngle >= 150) {
                currentAngle = 150;
                radarDirection = -1;
            } else if (currentAngle <= 30) {
                currentAngle = 30;
                radarDirection = 1;
            }

            // TODO: 실제 서버 연결 후, 이 테스트 거리값은 서버 수신값으로 대체
            currentDistance = 25 + (int) (10 * Math.sin(currentAngle * Math.PI / 180.0));

            updateStatusUi();

            radarHandler.postDelayed(this, 80);
        }
    };

    private void startRadarTest() {
        if (isRadarRunning) {
            return;
        }

        isRadarRunning = true;
        radarHandler.post(radarRunnable);
    }

    private void stopRadarTest() {
        isRadarRunning = false;
        radarHandler.removeCallbacks(radarRunnable);
    }

    private void setIndicatorColor(String color) {
        if (color.equals("GREEN")) {
            viewDistanceIndicator.setBackgroundColor(Color.rgb(69, 209, 90));
        } else if (color.equals("YELLOW")) {
            viewDistanceIndicator.setBackgroundColor(Color.rgb(232, 225, 106));
        } else if (color.equals("RED")) {
            viewDistanceIndicator.setBackgroundColor(Color.rgb(240, 80, 80));
        } else if (color.equals("BLUE")) {
            viewDistanceIndicator.setBackgroundColor(Color.rgb(30, 144, 255));
        }
    }

    private void sendCommand(String command) {
        if (!tcpClientManager.isConnected()) {
            txtConnectionStatus.setText("서버 미연결");
            return;
        }

        new Thread(() -> {
            try {
                tcpClientManager.send(command);
            } catch (Exception ex) {
                runOnUiThread(() -> {
                    txtConnectionStatus.setText("전송 실패");
                });
            }
        }).start();
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();

        stopRadarTest();
        disconnectFromServer();
    }
}