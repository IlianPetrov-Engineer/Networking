using UnityEngine;
using UnityEngine.SceneManagement;

public class Lobby : MonoBehaviour
{
    [SerializeField] NetworkClient network;

    void Start()
    {
        network.OnMessageReceived += HandleMessage;
    }

    public void Join()
    {
        network.Connect();
    }

    void HandleMessage(string msg)
    {
        if (msg.StartsWith("GameStarted"))
        {
            SceneManager.LoadScene(1);
        }
    }
}
