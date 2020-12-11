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

    public Text temperatureDisplay;
    public Text setpointDisplay;
    public Text outputDisplay;
    public Text stateDisplay;
    public Image stateImage;
    public Image logImage;
    public Text logText;

    public LineChart chart;
    public InputField inputTemp;
    public InputField inputClientIP;

    private Boolean isNewData = false;
    private Boolean isValidConnection = false;
    private Boolean connectionFlag = false;

    private Boolean isConnectionTimerActive = false;
    private Boolean isLogTimerActive = false;

    private float connectionTimeRemaining = 10;
    private float logTimeRemaining = 4;


    private class DeviceData
    {
        public string state;
        public float setpoint;
        public float temperature;
        public float output;
    }

    int i = 0;
    void Update()
    {
        if (isNewData)
        {
            isNewData = false;
            string trimmedUdpData = UDPData.Replace("?", "");


            //Parse data
            if (UDPData.Contains("l:"))
            {
                string[] data;
                string v = trimmedUdpData.Replace("l:", "");
                data = v.Split(',');
                DeviceData deviceData = new DeviceData
                {
                    setpoint = float.Parse(data[0]),
                    temperature = float.Parse(data[1]),
                    output = float.Parse(data[2]),
                    state = data[3]
                };
                chart.AddXAxisData("x" + i);
                chart.AddData(0, deviceData.temperature);
                i++;

                setpointDisplay.text = deviceData.setpoint.ToString();
                outputDisplay.text = deviceData.output.ToString();
                stateDisplay.text = deviceData.state;
                temperatureDisplay.text = deviceData.temperature.ToString();
            } else
            {
                switch (trimmedUdpData)
                {
                    case "enter vaild setpoint":
                        showError("Enter valid setpoint...");
                        break;

                    case "start cooking":
                        stateImage.color = new Color32(207, 96, 0, 255);
                        break;

                    case "stop cooking":
                        stateImage.color = new Color32(51, 51, 51, 255);
                        break;

                    case "setpoint valid":
                        showSuccess("Setpoint is valid!");
                        break;

                    default:
                        break;
                }
            }

        }

        if (isConnectionTimerActive)
        {
            doConnectionTimer();
        }
        if (isLogTimerActive)
        {
            doLogTimer();
        }
        if (isValidConnection && connectionFlag)
        {
            showSuccess("Connection established successfully.");
            connectionFlag = false;
        }
    }

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
                    //Debug.Log("Address IP Sender " + RemoteIpEndPoint.Address.ToString());
                    //Debug.Log("Port Number Sender " + RemoteIpEndPoint.Port.ToString());

                    if (returnData.ToString() == "server_handshake")
                    {
                        isValidConnection = true;
                        connectionFlag = true;
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
        connectionTimeRemaining = 10;
        isValidConnection = false;
        isConnectionTimerActive = true;
    }

    void doConnectionTimer()
    {
        if (connectionTimeRemaining > 0)
        {
            connectionTimeRemaining -= Time.deltaTime;
        }
        else
        {
            if (!isValidConnection)
            {
                showError("Failed to establish connection with device. Is the device powered on?");
            }
            isConnectionTimerActive = false;
        }
    }

    void doLogTimer()
    {
        if (logTimeRemaining > 0)
        {
            logTimeRemaining -= Time.deltaTime;
        }
        else
        {
            isLogTimerActive = false;
            logImage.gameObject.SetActive(false);
            logText.gameObject.SetActive(false);
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
        logText.text = error;
        logImage.color = new Color(255, 0, 0, 255);
        logText.gameObject.SetActive(true);
        logImage.gameObject.SetActive(true);
        logTimeRemaining = 5;
        isLogTimerActive = true;
    }

    private void showSuccess(string error)
    {
        Debug.Log(error);
        logText.text = error;
        logImage.color = new Color(0, 255, 0, 255);
        logText.gameObject.SetActive(true);
        logImage.gameObject.SetActive(true);
        logTimeRemaining = 5;
        isLogTimerActive = true;
    }

    void Start()
    {
        initListenerThread();
        sendClientHandshake();
        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void OnDisable()
    {
        destroyUDP();
    }
}
