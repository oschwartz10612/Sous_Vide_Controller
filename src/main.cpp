#include <Arduino.h>
#include <OneWire.h>
#include <DallasTemperature.h>
#include <PID_v1.h>

#define RelayPin 7

#define ONE_WIRE_BUS 4

enum operatingState
{
    OFF = 0,
    RUN
};
operatingState opState = OFF;
volatile unsigned long onTime = 0;

unsigned int WindowSize = 10000;
unsigned long windowStartTime;

const int logInterval = 10000; // log every 10 seconds
long lastLogTime = 0;

char inData[20];  // Allocate some space for the string
char inChar = -1; // Where to store the character read
byte index = 0;   // Index into array; where to store the character

unsigned long lastTempRequest = 0;
unsigned int tempDelay = 0;
int resolution = 12;

OneWire oneWire(ONE_WIRE_BUS);

DallasTemperature sensors(&oneWire);

DeviceAddress tempSensor;

double Setpoint;
double Input;
double Output;

double Kp = 850;
double Ki = 0.5;
double Kd = 0.1;

PID myPID(&Input, &Output, &Setpoint, Kp, Ki, Kd, DIRECT);

void setup()
{
    Serial.begin(115200);

    pinMode(RelayPin, OUTPUT);   // Output mode to drive relay
    digitalWrite(RelayPin, LOW); // make sure it is off to start

    sensors.begin();
    if (!sensors.getAddress(tempSensor, 0))
    {
        Serial.println("Sensor not found");
    }
    sensors.setResolution(tempSensor, resolution);
    sensors.setWaitForConversion(false);
    tempDelay = 750 / (1 << (12 - resolution));

    myPID.SetTunings(Kp, Ki, Kd);
    myPID.SetMode(AUTOMATIC);

    myPID.SetSampleTime(1000);
    myPID.SetOutputLimits(0, WindowSize);

    // Run timer2 interrupt every 15 ms
    TCCR2A = 0;
    TCCR2B = 1 << CS22 | 1 << CS21 | 1 << CS20;

    //Timer2 Overflow Interrupt Enable
    TIMSK2 |= 1 << TOIE2;
}

void Off()
{
    digitalWrite(RelayPin, LOW); // make sure it is off

    Serial.println("enter vaild setpoint");

    while (true)
    {
        if (Serial.available() > 0)
        {
            double temp = Serial.parseInt();
            Serial.println(temp);
            if (temp >= 23 && temp <= 300)
            {
                Serial.println("setpoint valid");
                Setpoint = temp;
                break;
            }
            else
            {
                Serial.println("enter vaild setpoint");
            }
        }
    }

    Serial.println("start cooking");
   windowStartTime = millis();
    opState = RUN; // start control
}

void Run()
{
    while (true)
    {

        if (Serial.available())
        {
            String ch;
            ch = Serial.readString();
            ch.trim();
            if (ch == "off")
            {
                opState = OFF;
                Serial.println("stop cooking");
                break;
            }
        }

        if (millis() - lastTempRequest >= tempDelay) // waited long enough??
        {
            sensors.requestTemperatures();

            Input = sensors.getTempCByIndex(0);

            sensors.requestTemperatures();
            lastTempRequest = millis();

            myPID.Compute();

            Serial.print(Input);
            Serial.print(",");
            Serial.println(Output);

            onTime = Output;
        }

        if (millis() - lastLogTime > logInterval)
        {
            lastLogTime = millis();
            Serial.print(Input);
            Serial.print(",");
            Serial.println(Output);
        }
        delay(50);
    }
}

void DriveOutput()
{
    long now = millis();
    // Set the output
    // "on time" is proportional to the PID output
    if (now - windowStartTime > WindowSize)
    { //time to shift the Relay Window
        windowStartTime += WindowSize;
    }
    if ((onTime > 100) && (onTime > (now - windowStartTime)))
    {
        digitalWrite(RelayPin, HIGH);
    }
    else
    {
        digitalWrite(RelayPin, LOW);
    }
}

SIGNAL(TIMER2_OVF_vect)
{
    if (opState == OFF)
    {
        digitalWrite(RelayPin, LOW); // make sure relay is off
    }
    else
    {
        DriveOutput();
    }
}

void loop()
{
    switch (opState)
    {
    case OFF:
        Off();
        break;
    case RUN:
        Run();
        break;
    }
}