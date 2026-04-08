using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    [SerializeField] UnityServer unityServer;
    [SerializeField] PlayerHandPresenter playerView;
    [SerializeField] PlayerHandPresenter opponentView;
    [SerializeField] PlayerHandPresenter takenCards;

    [SerializeField] private UIPresenter uiPresenter;

    private List<Suit> availableMarriages = new List<Suit>();
    private int currentMarriageIndex = 0;

    private bool showingTakenCards = false;

    int localPlayerId = 0;

    private void Start()
    {
        unityServer.server.OnCardPlayed += HandleCardPlayed;
        unityServer.server.OnCardDrawn += HandleCardDrawn;
        unityServer.server.OnDeckClosed += HandleDeckClosed;
        unityServer.server.OnMarriageDeclared += HandleMarriage;
        unityServer.server.OnTurnChanged += HandleTurnChanged;
        unityServer.server.OnActionRejected += HandleError;

        playerView.OnCardPlayed += OnCardPlayed;
        uiPresenter.OnDrawPressed += OnDrawPressed;
        uiPresenter.OnCloseDeckPressed += OnCloseDeckPressed;
        uiPresenter.OnMarriagePressed += HandleMarriagePressed;
        uiPresenter.ShowTakenCards += TakenCards;
        uiPresenter.OnTrumpPressed += HandleTrumpPressed;
        unityServer.server.OnTrumpExchanged += HandleTrumpExchanged;
        unityServer.server.OnTrumpTaken += HandleTrumpTaken;

        //drawPileButton.onClick.AddListener(OnDrawPressed);
        //declareWinButton.onClick.AddListener(OnWinDeclare);
        //marraigeButton.onClick.AddListener(OnMarriagePressed());

        LocalPlayer();
        RefreshView();
        UpdateDrawPileUI();
        UpdateMarriageOptions();
    }

    private void OnCardPlayed(int cardId)
    {
        unityServer.server.PlayCard(localPlayerId, cardId);
    }

    void RefreshView()
    {
        //localPlayerId = unityServer.server.GetActivePlayer();

        playerView.UpdateHand(unityServer.server.GetPlayerHand(localPlayerId));

        int opponentId = 1 - localPlayerId;
        opponentView.UpdateOpponentHand(unityServer.server.GetPlayerHand(opponentId).Count);

        //UpdateDrawPileUI();
    }

    void LocalPlayer()
    {
        localPlayerId = unityServer.server.GetActivePlayer();
    }

    void UpdateDrawPileUI()
    {
        uiPresenter.UpdateDrawPile(unityServer.server.GetDeckCount(), unityServer.server.trumpCard);
    }

    public void OnDrawPressed()
    {
        unityServer.server.DrawCard();
    }

    public void OnCloseDeckPressed()
    {
        unityServer.server.CloseDrawingDeck(localPlayerId);
    }

    //public void OnMarriageDeclared(Suit suit)
    //{
    //    unityServer.server.DeclareMarriage(localPlayerId, suit);
    //}

    void HandleCardPlayed(int playerId, int cardId)
    {
        RefreshView(); //temp
        UpdateMarriageOptions();
        UpdateButtons();
    }

    void HandleCardDrawn(int playerId)
    {
        RefreshView();
        UpdateDrawPileUI();
        UpdateMarriageOptions();
        UpdateButtons();
    }

    void HandleDeckClosed(int playerId)
    {
        UpdateDrawPileUI();
        uiPresenter.SetDeckClosedVisual(true);
    }

    void HandleMarriage(int playerId, Suit suit)
    {
        RefreshView();
        UpdateMarriageOptions();
        UpdateButtons();
    }

    void HandleTurnChanged(int playerId)
    {
        LocalPlayer();
        RefreshView();
        UpdateMarriageOptions();
        UpdateButtons();
    }

    void TakenCards()
    {
        showingTakenCards = !showingTakenCards;

        if (showingTakenCards)
        {
            uiPresenter.ShowTakenCardsView(true);
            takenCards.SetInteractable(false);
            takenCards.UpdateHand(unityServer.server.GetTakenCards(localPlayerId));
            uiPresenter.SetButtonInteractible(false, false, false, false);
        }
        
        else
        {
            uiPresenter.ShowTakenCardsView(false);
            takenCards.SetInteractable(true);
            RefreshView();
            UpdateButtons();
        }
    }

    void UpdateMarriageOptions()
    {
        availableMarriages = unityServer.server.GetAvailableMarriages(localPlayerId);

        if (availableMarriages.Count == 0)
        {
            uiPresenter.SetMarriageText("No Marriage");
            return;
        }

        if (currentMarriageIndex >= availableMarriages.Count)
            currentMarriageIndex = 0;

        uiPresenter.SetMarriageText($"Marriage: {availableMarriages[currentMarriageIndex]}");
        uiPresenter.SetButtonInteractible(true, true, availableMarriages.Count > 0, true);
    }

    void HandleTrumpPressed()
    {
        int deckCount = unityServer.server.GetDeckCount();

        if (deckCount > 2 && unityServer.server.canDrawCards)
        {
            unityServer.server.ExchangeTrump(localPlayerId);
        }

        else if (deckCount == 1)
        {
            unityServer.server.TakeTrump(localPlayerId);
        }
    }

    void HandleTrumpExchanged(int playerId)
    {
        RefreshView();
        UpdateDrawPileUI();
    }

    void HandleTrumpTaken(int playerId)
    {
        RefreshView();
        UpdateDrawPileUI();
    }

    void HandleMarriagePressed()
    {
        if (availableMarriages.Count == 0)
            return;

        unityServer.server.DeclareMarriage(localPlayerId, availableMarriages[currentMarriageIndex]);
    }

    void UpdateButtons()
    {
        bool isMyTurn = unityServer.server.GetActivePlayer() == localPlayerId;

        uiPresenter.SetButtonInteractible(isMyTurn, isMyTurn && unityServer.server.GetDeckCount() > 2, availableMarriages.Count > 0, unityServer.server.GetPoints(localPlayerId) >= 66);
    }

    void HandleError(string error)
    {
        Debug.Log(error);
    }
}
