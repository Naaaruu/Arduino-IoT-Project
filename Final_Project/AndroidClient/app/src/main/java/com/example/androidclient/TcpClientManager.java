package com.example.androidclient;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.net.Socket;

public class TcpClientManager {

    private Socket socket;
    private BufferedReader reader;
    private BufferedWriter writer;

    public boolean isConnected() {
        return socket != null && socket.isConnected() && !socket.isClosed();
    }

    public void connect(String ip, int port) throws Exception {
        socket = new Socket(ip, port);

        reader = new BufferedReader(
                new InputStreamReader(socket.getInputStream())
        );

        writer = new BufferedWriter(
                new OutputStreamWriter(socket.getOutputStream())
        );
    }

    public void send(String message) throws Exception {
        if (writer == null) {
            return;
        }

        writer.write(message);
        writer.newLine();
        writer.flush();
    }

    public String receive() throws Exception {
        if (reader == null) {
            return null;
        }

        return reader.readLine();
    }

    public void disconnect() {
        try {
            if (writer != null) writer.close();
            if (reader != null) reader.close();
            if (socket != null) socket.close();
        } catch (Exception ignored) {
        }

        writer = null;
        reader = null;
        socket = null;
    }
}