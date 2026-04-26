using Mono.Cecil;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    #region Variables
    //[SerializeField] private UnityServer unityServer;
    //private Server server;

    [SerializeField] private PlayerHandPresenter playerView;
    [SerializeField] private PlayerHandPresenter opponentView;
    [SerializeField] private PlayerHandPresenter takenCards;
    [SerializeField] private UIPresenter uiPresenter;

    private bool showingTakenCards = false;

    private List<Suit> availableMarriages = new List<Suit>();
    private int currentMarriageIndex = 0;

    private int localPlayerId = 0;

    [SerializeField] private NetworkClient network;

    List<CardData> myHand = new List<CardData>();
    int opponentCardCount = 0;

    int activePlayer = 0;

    CardData firstCard;
    CardData secondCard;

    #endregion

    private void Start()
    {
        //server = unityServer.server;

        #region ServerMessages
        //server.OnCardPlayed += HandleCardPlayed;
        //server.OnCardDrawn += HandleCardDrawn;
        //server.OnDeckClosed += HandleDeckClosed;
        //server.OnMarriageDeclared += HandleMarriage;
        //server.OnTurnChanged += HandleTurnChanged;
        //server.OnActionRejected += HandleError;
        //server.OnTrumpExchanged += HandleTrumpExchanged;
        //server.OnTrumpTaken += HandleTrumpTaken;
        //server.OnRoundEnd += HandleRoundEnd;
        //server.OnSessionScoreUpdate += HandleScoreUpdate;
        //server.OnSessionEnd += HandleSessionEnd;
        //server.SendSessionScore();
        //server.OnTrickUpdated += HandleTrickUpdated;

        network.Connect();

        network.OnMessageReceived += HandleNetworkMessage;

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
        //playerView.UpdateHand(server.GetPlayerHand(localPlayerId));

        //int opponentId = 1 - localPlayerId;
        //opponentView.UpdateOpponentHand(server.GetPlayerHand(opponentId).Count);
    }

    void LocalPlayer()
    {
        //localPlayerId = server.GetActivePlayer();
    }

    private void OnCardPlayed(int cardId)
    {
        network.Send($"PlayCard|{localPlayerId}|{cardId}");
        //server.PlayCard(localPlayerId, cardId);
    }

    public void OnDrawPressed()
    {
        network.Send("Draw");
        //server.DrawCard();
    }

    public void OnCloseDeckPressed()
    {
        network.Send($"CloseDeck|{localPlayerId}");
        //server.CloseDrawingDeck(localPlayerId);
    }

    void UpdateDrawPileUI()
    {
        //uiPresenter.UpdateDrawPile(server.GetDeckCount(), server.trumpCard);
    }

    void HandleCardPlayed(int playerId, CardData card)
    {
        if (playerId == localPlayerId)
        {
            myHand.RemoveAll(c => c.cardId == card.cardId);
            playerView.UpdateHand(myHand);
        }
        else
        {
            opponentCardCount--;
            opponentView.UpdateOpponentHand(opponentCardCount);
        }

        //RefreshView(); //temp
        //UpdateMarriageOptions();
        //UpdateButtons();
    }

    void HandleCardDrawn(int playerId, CardData card)
    {
        if (playerId == localPlayerId)
        {
            myHand.Add(card);
            playerView.UpdateHand(myHand);
        }
        else
        {
            opponentCardCount++;
            opponentView.UpdateOpponentHand(opponentCardCount);
        }

        //RefreshView();
        //UpdateMarriageOptions();
        //UpdateButtons();
        //UpdateDrawPileUI();
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
        activePlayer = playerId;

        uiPresenter.SetTurnText(activePlayer == localPlayerId ? localPlayerId : activePlayer);

        //uiPresenter.SetTurnText(playerId);

        UpdateButtons();

        //LocalPlayer();
        //RefreshView();
        //UpdateMarriageOptions();
        //UpdateButtons();
        //uiPresenter.SetTurnText(localPlayerId);
    }

    void HandleTrickUpdated(CardData first, CardData second)
    {
        if (first == null && second == null)
            uiPresenter.ClearTrick();
        else
            uiPresenter.ShowTrick(first, second);
    }

    void HandleTrumpPressed()
    {
        /*int deckCount = server.GetDeckCount();

        if (deckCount > 2 && server.canDrawCards)
        {
            network.Send($"ExchangeTrump|{localPlayerId}");
            //server.ExchangeTrump(localPlayerId);
        }

        else if (deckCount == 1)
        {
            network.Send($"TakeTrump|{localPlayerId}");
            //server.TakeTrump(localPlayerId);
        }*/
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

        network.Send($"DeclareMarriage|{localPlayerId}|{availableMarriages[currentMarriageIndex]}");
        //server.DeclareMarriage(localPlayerId, availableMarriages[currentMarriageIndex]);
    }

    void HandleWin()
    {
        /*int points = server.GetPoints(localPlayerId);

        if (points < 66)
            return;*/

        network.Send($"DeclareWin|{localPlayerId}");
        //server.ForceRoundEnd(localPlayerId);
    }

    void HandleRoundEnd(int winner)
    {
        uiPresenter.ShowRoundEnd(winner);
        Invoke(nameof(StartNextRound), 3f);
    }

    void StartNextRound()
    {
        uiPresenter.HideRoundEnd();

        showingTakenCards = false;

        RefreshView();
        UpdateDrawPileUI();
        UpdateMarriageOptions();
        UpdateButtons();
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
        bool myTurn = activePlayer == localPlayerId;

        uiPresenter.SetButtonInteractible(myTurn, myTurn, false,myTurn);

        //bool myTurn = server.GetActivePlayer() == localPlayerId;

        //uiPresenter.SetButtonInteractible(myTurn, myTurn && server.GetDeckCount() > 2, myTurn && availableMarriages.Count > 0, myTurn);
    }

    void TakenCards()
    {
        showingTakenCards = !showingTakenCards;

        if (showingTakenCards)
        {
            uiPresenter.ShowTakenCardsView(true);
            takenCards.SetInteractable(false);
            //takenCards.UpdateHand(server.GetTakenCards(localPlayerId));
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
        //availableMarriages = server.GetAvailableMarriages(localPlayerId);

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

    void HandleNetworkMessage(string msg)
    {
        string[] parts = msg.Split('|');

        switch (parts[0])
        {
            case "AssignPlayer":
                localPlayerId = int.Parse(parts[1]);
                Debug.Log($"Assigned player: {localPlayerId}");
                break;

            case "InitialHand":
                myHand.Clear();

                string[] cards = parts[1].Split(';');

                foreach (var c in cards)
                {
                    CardData card = CardData.Deserialize(c);
                    myHand.Add(card);
                }

                playerView.UpdateHand(myHand);

                opponentCardCount = myHand.Count;
                opponentView.UpdateOpponentHand(opponentCardCount);

                break;

            case "TrickUpdate":

                firstCard = parts[1] != "null" ? CardData.Deserialize(parts[1]) : null;
                secondCard = parts[2] != "null" ? CardData.Deserialize(parts[2]) : null;

                if (firstCard == null && secondCard == null)
                    uiPresenter.ClearTrick();
                else
                    uiPresenter.ShowTrick(firstCard, secondCard);

                break;

            case "CardPlayed":
                HandleCardPlayed(int.Parse(parts[1]), CardData.Deserialize(parts[2]));
                break;

            case "TurnChanged":
                HandleTurnChanged(int.Parse(parts[1]));
                break;

            case "CardDrawn":
                HandleCardDrawn(int.Parse(parts[1]), CardData.Deserialize(parts[2]));
                break;

            case "DeckClosed":
                HandleDeckClosed(int.Parse(parts[1]));
                break;

            case "MarriageDeclared":
                HandleMarriage(int.Parse(parts[1]), Enum.Parse<Suit>(parts[2]));
                break;

            case "Error":
                Debug.Log(parts[1]);
                break;

            case "GameStarted":
                Debug.Log("Game Started");
                break;
        }
    }
}
