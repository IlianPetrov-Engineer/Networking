using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHandPresenter : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private CardImageDatabase imageDatabase;

    private List<GameObject> spawnedCards = new List<GameObject>();

    public Action<int> OnCardPlayed;

    private bool interactable = true;
    //public System.Action OnDrawPressed;
    //public System.Action OnCloseDeckPressed;
    //public System.Action<Suit> OnMarriagePressed;

    public void UpdateHand(List<CardData> hand)
    {
        Clear();

        foreach (CardData card in hand)
        {
            GameObject cardUI = Instantiate(cardPrefab, content);
            spawnedCards.Add(cardUI);

            CardImage view = cardUI.GetComponent<CardImage>();
            Sprite cardImage = imageDatabase.GetImage(card.cardId);
            view.SetCardImage(card.cardId, cardImage);

            Button button = cardUI.GetComponent<Button>();
            int id = card.cardId;

            button.interactable = interactable;

            if (interactable)
            {
                button.onClick.AddListener(() => { OnCardPlayed?.Invoke(id); });
            }
        }
    }

    public void UpdateOpponentHand(int opponentCount)
    {
        Clear();

        for (int i = 0; i < opponentCount; i++)
        {
            GameObject cardUI = Instantiate(cardPrefab, content);
            spawnedCards.Add(cardUI);

            CardImage view = cardUI.GetComponent<CardImage>();
            Sprite cardImage = imageDatabase.GetImage(imageDatabase.cardImages.Count - 1);
            view.SetCardImage(-1, cardImage);

            Button button = cardUI.GetComponent<Button>();
            button.interactable = false;
        }
    }

    private void Clear()
    {
        foreach (GameObject card in spawnedCards)
            Destroy(card);

        spawnedCards.Clear();
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
    }

    //public void DrawPressed()
    //{
    //    OnDrawPressed?.Invoke();
    //}

    //public void CloseDeckPressed()
    //{
    //    OnCloseDeckPressed?.Invoke();
    //}

    //public void MarriagePressed(int suitIndex)
    //{
    //    Suit suit = (Suit)suitIndex;
    //    OnMarriagePressed?.Invoke(suit);
    //}
}
