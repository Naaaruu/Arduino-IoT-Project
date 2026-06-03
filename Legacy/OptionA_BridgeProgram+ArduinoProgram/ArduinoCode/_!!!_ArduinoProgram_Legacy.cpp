const int LDR_PIN = A0;  // 조도 센서가 연결된 아날로그 핀
const int LED_PIN = 13;  // 제어할 LED가 연결된 디지털 핀 (내장 LED)

unsigned long lastSendTime = 0;
const int sendInterval = 1000; // 1000ms (1초)마다 센서값 전송

void setup() {
  // PC의 중계 프로그램과 통신하기 위해 시리얼 통신 시작 (보드레이트 9600)
  Serial.begin(9600); 
  
  pinMode(LED_PIN, OUTPUT);
  pinMode(LDR_PIN, INPUT);

  // 초기 상태 전송 (선택 사항)
  Serial.println("ROLE:ARDUINO");
}

void loop() {
  // ==========================================
  // 1. 센서값 읽어서 PC(중계 프로그램)로 전송
  // ==========================================
  unsigned long currentTime = millis();
  
  if (currentTime - lastSendTime >= sendInterval) {
    int ldrValue = analogRead(LDR_PIN);
    
    // C# 서버가 요구하는 "LDR:값" 형태에 줄바꿈 문자(\n)를 더해 전송
    Serial.print("LDR:");
    Serial.println(ldrValue); 
    
    lastSendTime = currentTime;
  }

  // ==========================================
  // 2. PC(중계 프로그램)로부터 명령 수신 및 LED 제어
  // ==========================================
  if (Serial.available() > 0) {
    // 줄바꿈 문자가 나올 때까지 문자열을 읽음
    String command = Serial.readStringUntil('\n'); 
    command.trim(); // 혹시 모를 공백이나 캐리지 리턴(\r) 제거

    // C# 서버에서 정의한 명령어 처리
    if (command == "LED_ON") {
      digitalWrite(LED_PIN, HIGH);
    } 
    else if (command == "LED_OFF") {
      digitalWrite(LED_PIN, LOW);
    }
  }
}