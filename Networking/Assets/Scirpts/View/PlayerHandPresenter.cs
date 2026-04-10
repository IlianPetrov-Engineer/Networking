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

    GameObject CardUI(int cardId, bool isOpponent)
    {
        GameObject cardUI = Instantiate(cardPrefab, content);
        spawnedCards.Add(cardUI);

        CardImage view = cardUI.GetComponent<CardImage>();

        if (isOpponent)
        {
            Sprite backImage = imageDatabase.GetImage(imageDatabase.cardImages.Count - 1);
            view.SetCardImage(-1, backImage);
        }

        else
        {
            Sprite frontImage = imageDatabase.GetImage(cardId);
            view.SetCardImage(cardId, frontImage);
        }

        Button button = cardUI.GetComponent<Button>();
        button.interactable = interactable && !isOpponent;

        if (button.interactable)
        {
            int takenCardsId = cardId;
            button.onClick.AddListener(() => OnCardPlayed?.Invoke(takenCardsId));
        }

        return cardUI;
    }

    public void UpdateHand(List<CardData> hand)
    {
        Clear();

        foreach (CardData card in hand)
        {
            /*GameObject cardUI = Instantiate(cardPrefab, content);
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
            }*/
            CardUI(card.cardId, false);
        }
    }

    public void UpdateOpponentHand(int opponentCount)
    {
        Clear();

        for (int i = 0; i < opponentCount; i++)
        {
            /*GameObject cardUI = Instantiate(cardPrefab, content);
            spawnedCards.Add(cardUI);

            CardImage view = cardUI.GetComponent<CardImage>();
            Sprite cardImage = imageDatabase.GetImage(imageDatabase.cardImages.Count - 1);
            view.SetCardImage(-1, cardImage);

            Button button = cardUI.GetComponent<Button>();
            button.interactable = false;*/
            CardUI(-1, true);
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
}
