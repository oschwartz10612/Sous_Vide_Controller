using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using XCharts;

public class UDPClient : MonoBehaviour
{
    Thread udpListeningThread;
    UdpClient receivingUdpClient;

    public string defaultClientAddress;
    public int receivingPortNumber;
    public int clientPortNumber;
    public string UDPData = null;
    public Text tempatureDisplay;
    public LineChart chart;
    public InputField inputTemp;
    public InputField inputClientIP;

    private Boolean isNewData = false;
    private Boolean isValidConnection = false;
    private Boolean isTimerActive = false;
    private float timeRemaining = 10;

    private void initListenerThread()
    {
        string address = defaultClientAddress;
        ConnectionData data = SaveSystem.LoadData();
        if (data != null)
        {
            address = data.ip;
        }
        inputClientIP.text = address;

        Debug.Log("Started on : " + receivingPortNumber.ToString());
        udpListeningThread = new Thread(new ThreadStart(UdpListener));
        receivingUdpClient = new UdpClient(receivingPortNumber);

        receivingUdpClient.Connect(address, clientPortNumber);

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

                    if (returnData.ToString() == "server_handshake")
                    {
                        isValidConnection = true;
                    }
                    else
                    {
                        UDPData = returnData.ToString();
                        isNewData = true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
    }

    int i = 0;
    void Update()
    {
        if (isNewData)
        {
            isNewData = false;
            chart.AddXAxisData("x" + i);
            chart.AddData(0, float.Parse(UDPData));
            i++;
            if (UDPData != null)
            {
                tempatureDisplay.text = UDPData;
            }
        }

        if (isTimerActive)
        {
            doTimer();
        }
        if (isValidConnection)
        {
            showSuccess("Connection established successfully.");
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
        timeRemaining = 10;
        isValidConnection = false;
        isTimerActive = true;
    }

    void doTimer()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            if (!isValidConnection)
            {
                showError("Failed to establish connection with device. Is the device powered on?");
            }
            isTimerActive = false;
        }
    }

    public void restartConnection()
    {
        destroyUDP();

        string address = inputClientIP.text;
        SaveSystem.SaveData(address);

        initListenerThread();
        sendClientHandshake();
    }

    public void sendStart()
    {
        if (inputTemp.text == "" || inputTemp.text == null)
        {
            showError("Input tempature to start");
        } else
        {
            sendData(inputTemp.text);
        }
    }

    public void sendData(string data)
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes(data);
        receivingUdpClient.Send(sendBytes, sendBytes.Length);
    }

    private void showError(string error)
    {
        Debug.LogError(error);
    }

    private void showSuccess(string error)
    {
        Debug.Log(error);
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
