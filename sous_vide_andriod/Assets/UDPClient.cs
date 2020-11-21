using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class UDPClient : MonoBehaviour
{
    Thread udpListeningThread;

    UdpClient receivingUdpClient;
    public string clientAddress;

    public int portNumberReceive;
    public string UDPData = null;

    public Text tempatureDisplay;

    private void initListenerThread()
    {
        Debug.Log("Started on : " + portNumberReceive.ToString());
        udpListeningThread = new Thread(new ThreadStart(UdpListener));
        receivingUdpClient = new UdpClient(portNumberReceive);

        receivingUdpClient.Connect(clientAddress, 4210);

        // Run in background
        udpListeningThread.IsBackground = true;
        udpListeningThread.Start();
    }

    public void UdpListener()
    {
        while (true)
        {
            //Listening 
            try
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                //IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Broadcast, 5000);

                // Blocks until a message returns on this socket from a remote host.
                byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);

                if (receiveBytes != null)
                {
                    string returnData = Encoding.ASCII.GetString(receiveBytes);
                    Debug.Log("Message Received " + returnData.ToString());
                    Debug.Log("Address IP Sender " + RemoteIpEndPoint.Address.ToString());
                    Debug.Log("Port Number Sender " + RemoteIpEndPoint.Port.ToString());

                    UDPData = returnData.ToString();

                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
    }

    void Update()
    {
        if (UDPData != null)
        {
            tempatureDisplay.text = UDPData;
        }
    }

    void destroyUDP()
    {
        if (udpListeningThread != null && udpListeningThread.IsAlive)
        {
            udpListeningThread.Abort();
        }

        receivingUdpClient.Close();
    }

    void sendClientHandshake()
    {
        sendData("client_handshake");
    }

    public void restartConnection()
    {
        destroyUDP();
        initListenerThread();
        sendClientHandshake();
    }

    public void sendData(string data)
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes(data);
        receivingUdpClient.Send(sendBytes, sendBytes.Length);
    }

    void Start()
    {
        initListenerThread();
        sendClientHandshake();
    }

    void OnDisable()
    {
        destroyUDP();
    }
}
