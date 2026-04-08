using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardImageDatabase : MonoBehaviour
{
    public List<Sprite> cardImages = new List<Sprite>();
    private Dictionary<int, Sprite> cardMatch = new Dictionary<int, Sprite>();

    private void Awake()
    {
        for (int i = 0; i < cardImages.Count; i++)
        {
            cardMatch[i] = cardImages[i];
        }
    }

    public Sprite GetImage(int cardId)
    {
        return cardMatch[cardId];
    }
}
