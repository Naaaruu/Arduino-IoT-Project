#include "WiFiS3.h"

// 1. 와이파이 이름과 비밀번호를 입력하세요
char ssid[] = "YOUR_WIFI_NAME";     
char pass[] = "YOUR_WIFI_PASSWORD"; 

// 2. C# 서버 프로그램이 실행 중인 PC의 내부 IP 주소를 입력하세요 (예: 192.168.0.15)
// 주의: 127.0.0.1은 아두이노 자기 자신을 의미하므로 사용할 수 없습니다.
IPAddress serverIp(192, 168, 0, 15); 
int serverPort = 9000;

WiFiClient client;

const int LDR_PIN = A0;
const int LED_PIN = 13; // R4의 내장 LED 핀

unsigned long lastSendTime = 0;
const int sendInterval = 1000; 

void setup() {
  Serial.begin(9600);
  pinMode(LED_PIN, OUTPUT);
  pinMode(LDR_PIN, INPUT);

  // WiFi 연결 시도
  Serial.print("Attempting to connect to WPA SSID: ");
  Serial.println(ssid);
  
  while (WiFi.begin(ssid, pass) != WL_CONNECTED) {
    Serial.print(".");
    delay(5000);
  }
  
  Serial.println("\nYou're connected to the network");

  // C# TCP 서버 접속 시도
  connectToServer();
}

void loop() {
  // 서버와 연결이 끊어졌다면 재연결
  if (!client.connected()) {
    Serial.println("Disconnected from server. Reconnecting...");
    connectToServer();
    delay(2000);
    return; // 연결될 때까지 아래 로직은 건너뜀
  }

  // ==========================================
  // 1. 센서값 읽어서 C# TCP 서버로 직접 전송
  // ==========================================
  unsigned long currentTime = millis();
  if (currentTime - lastSendTime >= sendInterval) {
    int ldrValue = analogRead(LDR_PIN);
    
    // client.println()을 사용하면 서버의 ReadLineAsync()와 완벽히 호환됩니다.
    client.print("LDR:");
    client.println(ldrValue); 
    
    lastSendTime = currentTime;
  }

  // ==========================================
  // 2. C# TCP 서버로부터 직접 명령 수신
  // ==========================================
  if (client.available() > 0) {
    String command = client.readStringUntil('\n'); 
    command.trim(); 

    if (command == "LED_ON") {
      digitalWrite(LED_PIN, HIGH);
    } 
    else if (command == "LED_OFF") {
      digitalWrite(LED_PIN, LOW);
    }
    // C# 서버가 초기화 시 보내는 SERVER:READY 등의 메시지는 무시하거나 콘솔에 출력
    else {
      Serial.print("Server MSG: ");
      Serial.println(command);
    }
  }
}

void connectToServer() {
  Serial.print("Connecting to TCP Server...");
  if (client.connect(serverIp, serverPort)) {
    Serial.println("Connected!");
    // 접속 성공 시 아두이노임을 서버에 알림
    client.println("ROLE:ARDUINO");
  } else {
    Serial.println("Connection failed.");
  }
}