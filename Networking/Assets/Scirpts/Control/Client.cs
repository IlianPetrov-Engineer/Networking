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
        localActivePlayer = unityServer.server.GetActivePlayer(); // remove when implementing the networking
        hand = unityServer.server.GetPlayerHand(localActivePlayer);

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

        GUI.BeginGroup(new Rect(30, 90, 200, 200));
        GUILayout.Label($"Trump Suit is: {unityServer.server.trumpCard.suit}");

        GUI.EndGroup();

        GUI.BeginGroup(new Rect(30, 120, 200, 200));
        if (GUILayout.Button($"Close the deck"))
            unityServer.server.CloseDrawingDeck(localActivePlayer);

        GUI.EndGroup();

        if (unityServer.server.CanMarriage(localActivePlayer))
        {
            GUI.BeginGroup(new Rect(30, 150, 200, 200));
            if (GUILayout.Button($"Declare Marraige of Hearts"))
                unityServer.server.DeclareMarriage(localActivePlayer, Suit.Hearts);

            GUI.EndGroup();
            GUI.BeginGroup(new Rect(30, 180, 200, 200));
            if (GUILayout.Button($"Declare Marraige of Clubs"))
                unityServer.server.DeclareMarriage(localActivePlayer, Suit.Clubs);

            GUI.EndGroup();
            GUI.BeginGroup(new Rect(30, 210, 200, 200));
            if (GUILayout.Button($"Declare Marraige of Diamonds"))
                unityServer.server.DeclareMarriage(localActivePlayer, Suit.Diamonds);

            GUI.EndGroup();
            GUI.BeginGroup(new Rect(30, 240, 200, 200));
            if (GUILayout.Button($"Declare Marraige of Spades"))
                unityServer.server.DeclareMarriage(localActivePlayer, Suit.Spades);

            GUI.EndGroup();
        }

        GUI.BeginGroup(new Rect(750, 500, 1000, 1000));
        GUILayout.Label($"Player {localActivePlayer + 1}'s hand: ");

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
