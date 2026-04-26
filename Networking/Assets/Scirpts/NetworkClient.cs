using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

public class NetworkClient : MonoBehaviour
{
    TcpClient client;
    NetworkStream stream;

    public Action<string> OnMessageReceived;
    ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    public void Connect()
    {
        client = new TcpClient();
        client.Connect("127.0.0.1", 50011);

        stream = client.GetStream();

        Thread thread = new Thread(Listen);
        thread.Start();
    }

    void Listen()
    {
        byte[] buffer = new byte[1024];

        while (true)
        {
            int bytes = stream.Read(buffer, 0, buffer.Length);

            if (bytes == 0) continue;

            string msg = Encoding.UTF8.GetString(buffer, 0, bytes);

            messageQueue.Enqueue(msg);
        }
    }

    public void Send(string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        stream.Write(data, 0, data.Length);
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out string msg))
        {
            OnMessageReceived?.Invoke(msg);
        }
    }
}
