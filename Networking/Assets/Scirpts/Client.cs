using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Client : MonoBehaviour
{
    public UnityServer unityServer;

    public int localActivePlayer = 0;
    List<CardData> hand;

    private void OnGUI()
    {
        float refWidth = 1920f;
        float refHeight = 1080f;

        float scaleX = Screen.width / refWidth;
        float scaleY = Screen.height / refHeight;

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleX, scaleY, 1f));

        GUI.BeginGroup(new Rect(30, 30, 200, 200));
        if (GUILayout.Button("Draw card"))
        {
            unityServer.server.DrawCard();
        }
        GUI.EndGroup();

        GUI.BeginGroup(new Rect(750, 800, 1000, 1000));
        GUILayout.Label($"Player {localActivePlayer + 1}'s hand: ");

        localActivePlayer = unityServer.server.GetActivePlayer(); // remove when implementing the networking
        hand = unityServer.server.GetPlayerHand(localActivePlayer);

        for (int i = 0; i < hand.Count; i++)
        {
            if (GUILayout.Button(hand[i].rank + " of " + hand[i].suit, GUILayout.Width(200), GUILayout.Height(30)))
            {
                unityServer.server.PlayCard(localActivePlayer, hand[i].cardId);
            }
        }
        GUI.EndGroup();
    }
}
