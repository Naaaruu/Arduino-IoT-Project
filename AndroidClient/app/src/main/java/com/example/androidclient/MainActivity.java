package com.example.androidclient;

import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.net.Socket;

public class MainActivity extends AppCompatActivity {

    private EditText editIp;
    private EditText editPort;
    private Button btnConnect;
    private Button btnLedOn;
    private Button btnLedOff;
    private TextView txtLdrValue;
    private TextView txtStatus;
    private TextView txtLog;

    private Socket socket;
    private BufferedReader reader;
    private BufferedWriter writer;
    private boolean isConnected = false;

    private final Handler uiHandler = new Handler(Looper.getMainLooper());

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        editIp = findViewById(R.id.editIp);
        editPort = findViewById(R.id.editPort);
        btnConnect = findViewById(R.id.btnConnect);
        btnLedOn = findViewById(R.id.btnLedOn);
        btnLedOff = findViewById(R.id.btnLedOff);
        txtLdrValue = findViewById(R.id.txtLdrValue);
        txtStatus = findViewById(R.id.txtStatus);
        txtLog = findViewById(R.id.txtLog);

        btnLedOn.setEnabled(false);
        btnLedOff.setEnabled(false);

        btnConnect.setOnClickListener(v -> connectToServer());
        btnLedOn.setOnClickListener(v -> sendMessage("LED_ON"));
        btnLedOff.setOnClickListener(v -> sendMessage("LED_OFF"));
    }

    private void connectToServer() {
        String ip = editIp.getText().toString().trim();
        int port = Integer.parseInt(editPort.getText().toString().trim());

        new Thread(() -> {
            try {
                socket = new Socket(ip, port);

                reader = new BufferedReader(new InputStreamReader(socket.getInputStream()));
                writer = new BufferedWriter(new OutputStreamWriter(socket.getOutputStream()));

                isConnected = true;

                runOnUiThread(() -> {
                    txtStatus.setText("Status: Connected");
                    btnConnect.setEnabled(false);
                    btnLedOn.setEnabled(true);
                    btnLedOff.setEnabled(true);
                    addLog("Server connected.");
                });

                receiveLoop();

            } catch (Exception e) {
                runOnUiThread(() -> {
                    txtStatus.setText("Status: Connection failed");
                    addLog("Connect error: " + e.getMessage());
                });
            }
        }).start();
    }

    private void receiveLoop() {
        try {
            while (isConnected) {
                String message = reader.readLine();

                if (message == null) {
                    break;
                }

                String finalMessage = message;
                runOnUiThread(() -> handleServerMessage(finalMessage));
            }
        } catch (Exception e) {
            runOnUiThread(() -> addLog("Receive error: " + e.getMessage()));
        } finally {
            disconnect();
        }
    }

    private void handleServerMessage(String message) {
        addLog("Receive: " + message);

        if (message.startsWith("LDR:")) {
            String value = message.replace("LDR:", "").trim();
            txtLdrValue.setText("LDR Value: " + value);
        } else if (message.equals("LED:ON")) {
            addLog("LED is ON.");
        } else if (message.equals("LED:OFF")) {
            addLog("LED is OFF.");
        }
    }

    private void sendMessage(String message) {
        if (!isConnected || writer == null) {
            addLog("Server is not connected.");
            return;
        }

        new Thread(() -> {
            try {
                writer.write(message);
                writer.newLine();
                writer.flush();

                runOnUiThread(() -> addLog("Send: " + message));
            } catch (Exception e) {
                runOnUiThread(() -> addLog("Send error: " + e.getMessage()));
            }
        }).start();
    }

    private void addLog(String text) {
        txtLog.append("\n" + text);
    }

    private void disconnect() {
        isConnected = false;

        try {
            if (reader != null) reader.close();
            if (writer != null) writer.close();
            if (socket != null) socket.close();
        } catch (Exception ignored) {
        }

        runOnUiThread(() -> {
            txtStatus.setText("Status: Disconnected");
            btnConnect.setEnabled(true);
            btnLedOn.setEnabled(false);
            btnLedOff.setEnabled(false);
            addLog("Disconnected.");
        });
    }

    @Override
    protected void onDestroy() {
        disconnect();
        super.onDestroy();
    }
}