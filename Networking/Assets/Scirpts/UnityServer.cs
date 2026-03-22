using UnityEngine;

public class UnityServer : MonoBehaviour
{
    public Server server;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        server = new Server();
        server.StartGame();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
