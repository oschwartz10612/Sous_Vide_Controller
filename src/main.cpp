#include <Arduino.h>
#include <OneWire.h>
#include <DallasTemperature.h
#include <PID_v1.h>

#define RelayPin 7

#define ONE_WIRE_BUS 2
#define ONE_WIRE_PWR 3
#define ONE_WIRE_GND 4

enum operatingState
{
    OFF = 0,
    SETP,
    RUN
};
operatingState opState = OFF;

int WindowSize = 10000; 
unsigned long windowStartTime;

OneWire oneWire(ONE_WIRE_BUS);

DallasTemperature sensors(&oneWire);

double Setpoint;
double Input;
double Output;

double Kp;
double Ki;
double Kd;

PID myPID(&Input, &Output, &Setpoint, Kp, Ki, Kd, DIRECT);

void setup()
{
    Serial.begin(115200);

    pinMode(RelayPin, OUTPUT);   // Output mode to drive relay
    digitalWrite(RelayPin, LOW); // make sure it is off to start

    pinMode(ONE_WIRE_GND, OUTPUT);
    digitalWrite(ONE_WIRE_GND, LOW);

    pinMode(ONE_WIRE_PWR, OUTPUT);
    digitalWrite(ONE_WIRE_PWR, HIGH);

    sensors.begin();
    if (!sensors.getAddress(tempSensor, 0))
    {
        Serial.println("Sensor not found");
    }
    sensors.setResolution(tempSensor, 12);
    sensors.setWaitForConversion(false);

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

void Off() {
    
}

void Run() {

}

void DoControl() {
        sensors.requestTemperatures();
    float tempC = sensors.getTempCByIndex(0);

    if (sensors.isConversionAvailable(0))
    {
        Input = sensors.getTempC(tempSensor);
        sensors.requestTemperatures(); // prime the pump for the next one - but don't wait
    }

    myPID.Compute();

    onTime = Output;
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