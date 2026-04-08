using UnityEngine;

public class UnityServer : MonoBehaviour
{
    public Server server;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        server = new Server();
        server.StartGame();
    }
}
