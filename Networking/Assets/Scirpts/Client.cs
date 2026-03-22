using System.Collections.Generic;
using UnityEngine;

public class Client : MonoBehaviour
{
    public UnityServer unityServer;

    public int localActivePlayer = 0;
    List<CardData> hand;

    private void OnGUI()
    {
        GUI.BeginGroup(new Rect(600, 700, 400, 200));

        GUILayout.Label($"Player {localActivePlayer + 1}'s hand: ");

        localActivePlayer = unityServer.server.GetActivePlayer(); // remove when implementing the networking
        hand = unityServer.server.GetPlayerHand(localActivePlayer);

        for (int i = 0; i < hand.Count; i++)
        {
            if (GUILayout.Button(hand[i].rank + " of " + hand[i].suit))
            {
                unityServer.server.PlayCard(localActivePlayer, hand[i].cardId);
            }
        }
        GUI.EndGroup();

        GUI.BeginGroup(new Rect(30, 0, 200, 200));
        if (GUILayout.Button("Draw card"))
        {
            unityServer.server.DrawCard();
        }
        GUI.EndGroup();
    }
}
