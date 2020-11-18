using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPClient : MonoBehaviour
{
    Thread udpListeningThread;
    public int portNumberReceive;
    UdpClient receivingUdpClient;
    UdpClient sendingUdpClient;

    private void initListenerThread()
    {
        portNumberReceive = 5000;

        Debug.Log("Started on : " + portNumberReceive.ToString());
        udpListeningThread = new Thread(new ThreadStart(UdpListener));
        receivingUdpClient = new UdpClient(portNumberReceive);

        receivingUdpClient.Connect("192.168.1.233", 4210);

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

                    if (returnData.ToString() == "TextTest")
                    {
                        //Do something if TextTest is received
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
    }

    void OnDisable()
    {
        if (udpListeningThread != null && udpListeningThread.IsAlive)
        {
            udpListeningThread.Abort();
        }

        receivingUdpClient.Close();
    }

    public void sendTest()
    {


        // Sends a message to the host to which you have connected.
        Byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");

        receivingUdpClient.Send(sendBytes, sendBytes.Length);
    }

    void Start()
    {
        initListenerThread();
    }
}
