using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

[System.Serializable]
public class ConnectionData
{
    public string ip;
    public List<float> prevData;

    public ConnectionData(string ipData, List<float> tempData)
    {
        ip = ipData;
        prevData = tempData;
    }

}

public static class SaveSystem
{
    public static void SaveData(string ip, List<float> tempData)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/applicationData.bin";
        FileStream stream = new FileStream(path, FileMode.Create);

        ConnectionData data = new ConnectionData(ip, tempData);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static ConnectionData LoadData()
    {
        string path = Application.persistentDataPath + "/applicationData.bin";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            ConnectionData data = formatter.Deserialize(stream) as ConnectionData;
            stream.Close();

            return data;
        }
        else
        {
            Debug.LogError("Save file not found in " + path);
            return null;
        }
    }
}
