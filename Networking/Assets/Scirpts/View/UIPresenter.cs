using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPresenter : MonoBehaviour
{
    #region UI
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

    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject endRoundPanel;
    [SerializeField] private TextMeshProUGUI endRoundText;

    [SerializeField] private Image firstCardImage;
    [SerializeField] private Image secondCardImage; 

    #endregion

    [SerializeField] private CardImageDatabase imageDatabase;

    [SerializeField] private GameObject takenCardsPanel;

    #region Actions
    public Action OnDrawPressed;
    public Action OnCloseDeckPressed;
    public Action OnWinDeclared;
    public Action OnMarriagePressed;
    public Action ShowTakenCards;
    public Action OnTrumpPressed;

    #endregion

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

    public void ShowTrick(CardData first, CardData second)
    {
        if (first != null)
        {
            firstCardImage.gameObject.SetActive(true);
            firstCardImage.sprite = imageDatabase.GetImage(first.cardId);
        }

        if (second != null)
        {
            secondCardImage.gameObject.SetActive(true);
            secondCardImage.sprite = imageDatabase.GetImage(second.cardId);
        }
    }

    public void ClearTrick()
    {
        firstCardImage.sprite = null;
        secondCardImage.sprite = null;
        firstCardImage.gameObject.SetActive(false);
        secondCardImage.gameObject.SetActive(false);
    }

    public void SetTurnText(int playerId)
    {
        currentPlayerText.text = $"Player {playerId + 1} Turn. \n You are player {playerId + 1}";
    }

    public void SetScore(int p1, int p2)
    {
        scoreText.text = $"{p1} : {p2}";
    }

    public void ShowRoundEnd(int winner)
    {
        endRoundPanel.SetActive(true);
        endRoundText.text = $"Player {winner + 1} Wins!";
    }

    public void HideRoundEnd()
    {
        endRoundPanel.SetActive(false);
    }
}
