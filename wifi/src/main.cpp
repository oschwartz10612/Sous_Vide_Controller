#include <Arduino.h>
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include "keys.h"

#define DEBUG 1

WiFiUDP Udp;
unsigned int localUdpPort = 4210; // local port to listen on
char incomingPacket[255];         // buffer for incoming packets

const byte numChars = 32;
char receivedChars[numChars]; // an array to store the received data

boolean newData = false;

void setup()
{
  Serial.begin(9600);
  Serial.println();

#ifdef DEBUG
  Serial.printf("Connecting to %s ", SSID);
#endif
  WiFi.begin(SSID, PASSWORD);
  while (WiFi.status() != WL_CONNECTED)
  {
    delay(500);
#ifdef DEBUG
    Serial.print(".");
#endif
  }
#ifdef DEBUG
  Serial.println(" connected");
#endif

  Udp.begin(localUdpPort);
#ifdef DEBUG
  Serial.printf("Now listening at IP %s, UDP port %d\n", WiFi.localIP().toString().c_str(), localUdpPort);
#endif
}

void recvWithEndMarker()
{
  static byte ndx = 0;
  char endMarker = '\n';
  char rc;

  while (Serial.available() > 0 && newData == false)
  {
    rc = Serial.read();

    if (rc != endMarker)
    {
      receivedChars[ndx] = rc;
      ndx++;
      if (ndx >= numChars)
      {
        ndx = numChars - 1;
      }
    }
    else
    {
      receivedChars[ndx] = '\0'; // terminate the string
      ndx = 0;
      newData = true;
    }
  }
}

void loop()
{
  int packetSize = Udp.parsePacket();
  if (packetSize)
  {
// receive incoming UDP packets
#ifdef DEBUG
    Serial.printf("Received %d bytes from %s, port %d\n", packetSize, Udp.remoteIP().toString().c_str(), Udp.remotePort());
#endif
    int len = Udp.read(incomingPacket, 255);
    if (len > 0)
    {
      incomingPacket[len] = 0;
    }
#ifdef DEBUG
    Serial.printf("UDP packet contents: %s\n", incomingPacket);
#endif

    if (strcmp(incomingPacket, "client_handshake") == 0)
    {
      Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
      Udp.write("server_handshake");
      Udp.endPacket();
    }
    else
    {
      Serial.println(incomingPacket);
    }
  }

  recvWithEndMarker();

  if (newData == true)
  {
#ifdef DEBUG
    Serial.printf("Serial read contents: %s\n", receivedChars);
#endif

    Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
    Udp.write(receivedChars);
    Udp.endPacket();
    newData = false;
  }
}
