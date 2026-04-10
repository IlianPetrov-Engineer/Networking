using Mono.Cecil;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    #region Variables
    [SerializeField] private UnityServer unityServer;
    private Server server;

    [SerializeField] private PlayerHandPresenter playerView;
    [SerializeField] private PlayerHandPresenter opponentView;
    [SerializeField] private PlayerHandPresenter takenCards;
    [SerializeField] private UIPresenter uiPresenter;

    private bool showingTakenCards = false;

    private List<Suit> availableMarriages = new List<Suit>();
    private int currentMarriageIndex = 0;

    private int localPlayerId = 0;

    #endregion

    private void Start()
    {
        server = unityServer.server;

        #region ServerMessages
        server.OnCardPlayed += HandleCardPlayed;
        server.OnCardDrawn += HandleCardDrawn;
        server.OnDeckClosed += HandleDeckClosed;
        server.OnMarriageDeclared += HandleMarriage;
        server.OnTurnChanged += HandleTurnChanged;
        server.OnActionRejected += HandleError;
        server.OnTrumpExchanged += HandleTrumpExchanged;
        server.OnTrumpTaken += HandleTrumpTaken;
        server.OnRoundEnd += HandleRoundEnd;
        server.OnSessionScoreUpdate += HandleScoreUpdate;
        server.OnSessionEnd += HandleSessionEnd;
        server.SendSessionScore();

        #endregion

        #region PlayerSubscriptions
        playerView.OnCardPlayed += OnCardPlayed;
        uiPresenter.OnDrawPressed += OnDrawPressed;
        uiPresenter.OnCloseDeckPressed += OnCloseDeckPressed;
        uiPresenter.OnMarriagePressed += HandleMarriagePressed;
        uiPresenter.ShowTakenCards += TakenCards;
        uiPresenter.OnTrumpPressed += HandleTrumpPressed;
        uiPresenter.OnWinDeclared += HandleWin;

        #endregion

        uiPresenter.SetTurnText(localPlayerId);

        LocalPlayer();
        RefreshView();
        UpdateDrawPileUI();
        UpdateMarriageOptions();
    }

    void RefreshView()
    {
        playerView.UpdateHand(server.GetPlayerHand(localPlayerId));

        int opponentId = 1 - localPlayerId;
        opponentView.UpdateOpponentHand(server.GetPlayerHand(opponentId).Count);
    }

    void LocalPlayer()
    {
        localPlayerId = server.GetActivePlayer();
    }

    private void OnCardPlayed(int cardId)
    {
        server.PlayCard(localPlayerId, cardId);
    }

    public void OnDrawPressed()
    {
        server.DrawCard();
    }

    public void OnCloseDeckPressed()
    {
        server.CloseDrawingDeck(localPlayerId);
    }

    void UpdateDrawPileUI()
    {
        uiPresenter.UpdateDrawPile(server.GetDeckCount(), server.trumpCard);
    }

    void HandleCardPlayed(int playerId, int cardId)
    {
        RefreshView(); //temp
        UpdateMarriageOptions();
        UpdateButtons();
    }

    void HandleCardDrawn(int playerId)
    {
        RefreshView();
        UpdateMarriageOptions();
        UpdateButtons();
        UpdateDrawPileUI();
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
        uiPresenter.SetTurnText(playerId);
    }

    void HandleTrumpPressed()
    {
        int deckCount = server.GetDeckCount();

        if (deckCount > 2 && server.canDrawCards)
        {
            server.ExchangeTrump(localPlayerId);
        }

        else if (deckCount == 1)
        {
            server.TakeTrump(localPlayerId);
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

        server.DeclareMarriage(localPlayerId, availableMarriages[currentMarriageIndex]);
    }

    void HandleWin()
    {
        int points = server.GetPoints(localPlayerId);

        if (points < 66)
            return;

        server.ForceRoundEnd(localPlayerId);
    }

    void HandleRoundEnd(int winner)
    {
        uiPresenter.ShowRoundEnd(winner);
        Invoke(nameof(StartNextRound), 3f);
    }

    void StartNextRound()
    {
        uiPresenter.HideRoundEnd();
        RefreshView();
        UpdateDrawPileUI();
    }

    void HandleScoreUpdate(int player1, int player2)
    {
        uiPresenter.SetScore(player1, player2);
    }

    void HandleSessionEnd(int winner)
    {
        Debug.Log($"Player {winner} won the session.");
    }

    void UpdateButtons()
    {
        bool isMyTurn = server.GetActivePlayer() == localPlayerId;

        uiPresenter.SetButtonInteractible(isMyTurn, isMyTurn && server.GetDeckCount() > 2, availableMarriages.Count > 0, true);
    }

    void TakenCards()
    {
        showingTakenCards = !showingTakenCards;

        if (showingTakenCards)
        {
            uiPresenter.ShowTakenCardsView(true);
            takenCards.SetInteractable(false);
            takenCards.UpdateHand(server.GetTakenCards(localPlayerId));
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
        availableMarriages = server.GetAvailableMarriages(localPlayerId);

        if (availableMarriages.Count == 0)
        {
            uiPresenter.SetMarriageText("No Marriage");
            return;
        }

        if (currentMarriageIndex >= availableMarriages.Count)
            currentMarriageIndex = 0;

        uiPresenter.SetMarriageText($"Marriage: {availableMarriages[currentMarriageIndex]}");
        //uiPresenter.SetButtonInteractible(true, true, availableMarriages.Count > 0, true);
    }

    void HandleError(string error)
    {
        Debug.Log(error);
    }
}
