#include <Servo.h>

const int TRIG_PIN = 9;
const int ECHO_PIN = 10;
const int SERVO_PIN = 6;
const int RGB_RED_PIN = 3;
const int RGB_GREEN_PIN = 5;
const int RGB_BLUE_PIN = 11;
const int PIEZO_PIN = 8;

const int SERIAL_BAUD_RATE = 9600;
const int SCAN_MIN_ANGLE = 15;
const int SCAN_MAX_ANGLE = 165;
const int SCAN_STEP = 3;
const int DANGER_DISTANCE_CM = 20;
const int NO_ECHO_DISTANCE_CM = 400;
const unsigned long ALLOW_DURATION_MS = 10000;
const unsigned long SCAN_DELAY_MS = 60;
const unsigned long BEEP_INTERVAL_MS = 350;
const unsigned int BEEP_FREQUENCY = 1200;
const unsigned long BEEP_DURATION_MS = 80;

Servo radarServo;

int currentAngle = SCAN_MIN_ANGLE;
int scanDirection = 1;
bool alertActive = false;
bool forcedWarning = false;
unsigned long allowUntilMs = 0;
unsigned long lastBeepMs = 0;

void setup() {
  Serial.begin(SERIAL_BAUD_RATE);

  pinMode(TRIG_PIN, OUTPUT);
  pinMode(ECHO_PIN, INPUT);
  pinMode(RGB_RED_PIN, OUTPUT);
  pinMode(RGB_GREEN_PIN, OUTPUT);
  pinMode(RGB_BLUE_PIN, OUTPUT);
  pinMode(PIEZO_PIN, OUTPUT);

  radarServo.attach(SERVO_PIN);
  radarServo.write(currentAngle);
  setNormalIndicator();

  delay(1000);
  Serial.println("STATUS:READY");
}

void loop() {
  handleSerialCommand();

  radarServo.write(currentAngle);
  delay(SCAN_DELAY_MS);

  int distanceCm = measureDistanceCm();
  Serial.print("RADAR:");
  Serial.print(currentAngle);
  Serial.print(":");
  Serial.println(distanceCm);

  updateAlertState(distanceCm);
  updateBeeper();
  moveToNextAngle();
}

int measureDistanceCm() {
  digitalWrite(TRIG_PIN, LOW);
  delayMicroseconds(2);
  digitalWrite(TRIG_PIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(TRIG_PIN, LOW);

  unsigned long duration = pulseIn(ECHO_PIN, HIGH, 30000UL);
  if (duration == 0) {
    return NO_ECHO_DISTANCE_CM;
  }

  int distance = duration / 58;
  if (distance <= 0) {
    return NO_ECHO_DISTANCE_CM;
  }

  return distance;
}

void updateAlertState(int distanceCm) {
  unsigned long now = millis();
  bool allowActive = allowUntilMs != 0 && now < allowUntilMs;
  bool distanceDanger = distanceCm > 0 && distanceCm <= DANGER_DISTANCE_CM;
  bool shouldAlert = forcedWarning || (!allowActive && distanceDanger);

  setAlertState(shouldAlert);

  if (!shouldAlert && allowActive) {
    setAllowedIndicator();
  } else if (!shouldAlert) {
    setNormalIndicator();
  }
}

void setAlertState(bool enabled) {
  if (alertActive == enabled) {
    if (enabled) {
      setWarningIndicator();
    }
    return;
  }

  alertActive = enabled;

  if (alertActive) {
    setWarningIndicator();
    tone(PIEZO_PIN, BEEP_FREQUENCY, BEEP_DURATION_MS);
    lastBeepMs = millis();
    Serial.println("ALERT:ON");
  } else {
    noTone(PIEZO_PIN);
    Serial.println("ALERT:OFF");
  }
}

void updateBeeper() {
  if (!alertActive) {
    return;
  }

  unsigned long now = millis();
  if (now - lastBeepMs >= BEEP_INTERVAL_MS) {
    tone(PIEZO_PIN, BEEP_FREQUENCY, BEEP_DURATION_MS);
    lastBeepMs = now;
  }
}

void moveToNextAngle() {
  currentAngle += SCAN_STEP * scanDirection;

  if (currentAngle >= SCAN_MAX_ANGLE) {
    currentAngle = SCAN_MAX_ANGLE;
    scanDirection = -1;
  } else if (currentAngle <= SCAN_MIN_ANGLE) {
    currentAngle = SCAN_MIN_ANGLE;
    scanDirection = 1;
  }
}

void handleSerialCommand() {
  while (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();

    if (command.length() == 0) {
      continue;
    }

    if (command == "CMD:ALLOW") {
      forcedWarning = false;
      allowUntilMs = millis() + ALLOW_DURATION_MS;
      setAlertState(false);
      setAllowedIndicator();
      Serial.println("STATUS:ALLOWED");
    } else if (command == "CMD:WARN") {
      allowUntilMs = 0;
      forcedWarning = true;
      setAlertState(true);
      Serial.println("STATUS:FORCED_WARN");
    } else if (command == "CMD:RESET") {
      forcedWarning = false;
      allowUntilMs = 0;
      setAlertState(false);
      setNormalIndicator();
      Serial.println("STATUS:RESET");
    } else {
      Serial.print("STATUS:UNKNOWN_COMMAND:");
      Serial.println(command);
    }
  }
}

void setWarningIndicator() {
  setRgb(255, 0, 0);
}

void setAllowedIndicator() {
  setRgb(0, 180, 0);
}

void setNormalIndicator() {
  setRgb(0, 0, 40);
}

void setRgb(int red, int green, int blue) {
  analogWrite(RGB_RED_PIN, red);
  analogWrite(RGB_GREEN_PIN, green);
  analogWrite(RGB_BLUE_PIN, blue);
}
