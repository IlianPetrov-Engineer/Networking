using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPresenter : MonoBehaviour
{
    [SerializeField] private Button drawButton;
    [SerializeField] private GameObject drawPileClosed;
    [SerializeField] private Button closeDeckButton;
    [SerializeField] private Button marraigeButton;
    [SerializeField] private Button declareWinButton;
    [SerializeField] private Button showTakenCards;

    [SerializeField] private Button trumpCard;
    [SerializeField] private Image trumpCardImage;
    [SerializeField] private TextMeshProUGUI remainingCardsText;
    [SerializeField] private TextMeshProUGUI marriageText;

    [SerializeField] private CardImageDatabase imageDatabase;

    [SerializeField] private GameObject takenCardsPanel;

    public Action OnDrawPressed;
    public Action OnCloseDeckPressed;
    public Action OnWinDeclared;
    public Action OnMarriagePressed;
    public Action ShowTakenCards;
    public Action OnTrumpPressed;

    private void Awake()
    {
        drawButton.onClick.AddListener(() => OnDrawPressed?.Invoke());
        closeDeckButton.onClick.AddListener(() => OnCloseDeckPressed?.Invoke());
        marraigeButton.onClick.AddListener(() => OnMarriagePressed?.Invoke());
        declareWinButton.onClick.AddListener(() => OnWinDeclared?.Invoke());
        trumpCard.onClick.AddListener(() => OnTrumpPressed?.Invoke());
        showTakenCards.onClick.AddListener(() => ShowTakenCards?.Invoke());
    }

    public void UpdateDrawPile(int remainingCards, CardData trumpCard)
    {
        remainingCardsText.text = $"Remaining Cards: {remainingCards}";

        if (trumpCard != null)
            trumpCardImage.sprite = imageDatabase.GetImage(trumpCard.cardId);

        if (remainingCards <= 1)
            drawButton.gameObject.SetActive(false);
    }

    public void SetMarriageText(string text)
    {
        marriageText.text = text;
    }

    public void SetButtonInteractible(bool draw, bool close, bool marriage, bool win)
    {
        drawButton.interactable = draw;
        closeDeckButton.interactable = close;
        marraigeButton.interactable = marriage;
        declareWinButton.interactable = win;
    }

    public void SetDeckClosedVisual(bool closed)
    {
        drawButton.gameObject.SetActive(!closed);
        drawPileClosed.SetActive(closed);
    }

    public void ShowTakenCardsView(bool show)
    {
        takenCardsPanel.SetActive(show);
    }
}
