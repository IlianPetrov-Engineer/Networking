using System;
using System.Collections.Generic;
using System.Diagnostics;

// TO DO:
// - When impleting networking, set "localActivePlayer" from "Client" to 0 or 1, depending on order at which players join the server 
// - After all cards are played declare a winner 
// - Add a way to keep track of wins

enum TurnPhase
{
    WaitingFirstCard,
    WaitingSecondCard,
    Drawing
}

public class Server
{
    #region Variables

    #region Global
    private List<CardData> deck = new List<CardData>();
    public CardData trumpCard;
    private CardData firstPlayedCard;
    private CardData secondPlayedCard;
    private Random random = new Random();
    public bool canDrawCards = true;
    private bool marriageDeclaredThisTurn = false;
    private Suit marriageSuit;

    private TurnPhase turnPhase;
    private int activePlayer;
    private int playersDrawn;
    private int trickLeader;
    
    #endregion

    #region Player Related
    private List<CardData> player1Cards = new List<CardData>();
    private List<CardData> player2Cards = new List<CardData>();
    private List<CardData> player1TakenCards = new List<CardData>();
    private List<CardData> player2TakenCards = new List<CardData>();
    private int player1Score;
    private int player2Score;

    private int player1SessionScore = 0;
    private int player2SessionScore = 0;
    private int sessionMaxScore = 5;
    private bool deckClosedByPlayer = false;
    private int closingPlayer = -1;

    private int lastTrickWinner = -1;
    #endregion

    #region Actions
    public event Action<int, int> OnCardPlayed;
    public event Action<int> OnCardDrawn;
    public event Action<int> OnDeckClosed;
    public event Action<int, Suit> OnMarriageDeclared;
    public event Action<int> OnTurnChanged;
    public event Action<int> OnTrumpExchanged;
    public event Action<int> OnTrumpTaken;
    public event Action<int> OnRoundEnd;
    public event Action<int, int> OnSessionScoreUpdate;
    public event Action<int> OnSessionEnd;

    public event Action<CardData, CardData> OnTrickUpdated;
    public event Action<string> OnActionRejected;
    #endregion

    #endregion

    public void StartGame()
    {
        CreateDeck();
        ShuffleDeck();
        DealCardsMain();

        turnPhase = TurnPhase.WaitingFirstCard;

        /*foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                Debug.WriteLine($"{rank} of {suit}");
            }
        }*/

        /*Debug.WriteLine($"{deck.Count}");

        for (int i = 0; i < deck.Count; i++)
        {
            Debug.WriteLine($"{deck[i].rank} of {deck[i].suit} with Id number = {deck[i].cardId}");
        }

        for (int i = 0; i < player1Cards.Count; i++)
        {
            Debug.WriteLine($"Player 1 hand: {player1Cards[i].rank} of {player1Cards[i].suit} with Id number = {player1Cards[i].cardId}");
        }

        for (int i = 0; i < player2Cards.Count; i++)
        {
            Debug.WriteLine($"Player 2 hand: {player2Cards[i].rank} of {player2Cards[i].suit} with Id number = {player2Cards[i].cardId}");
        }

        Debug.WriteLine($"Trump card is {trumpCard.rank} of {trumpCard.suit} with Id number = {trumpCard.cardId}");

        for (int i = 0; i < deck.Count; i++)
        {
            Debug.WriteLine($"{deck[i].rank} of {deck[i].suit} with Id number = {deck[i].cardId}");
        }

        Debug.WriteLine($"{deck.Count}");*/
    }

    #region StartGame
    private void CreateDeck()
    {
        int id = 0;

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            deck.Add(CreateCard(suit, Rank.Nine, 0, 1, id++));
            deck.Add(CreateCard(suit, Rank.Jack, 2, 2, id++));
            deck.Add(CreateCard(suit, Rank.Queen, 3, 3, id++));
            deck.Add(CreateCard(suit, Rank.King, 4, 4, id++));
            deck.Add(CreateCard(suit, Rank.Ten, 10, 5, id++));
            deck.Add(CreateCard(suit, Rank.Ace, 11, 6, id++));
        }
    }

    private void ShuffleDeck()
    {
        int deckSize = deck.Count;

        while (deckSize > 1)
        {
            deckSize--;
            int cardShuffle = random.Next(deckSize + 1);
            CardData temp = deck[cardShuffle];
            deck[cardShuffle] = deck[deckSize];
            deck[deckSize] = temp;
        }
    }

    private void DealCardsMain()
    {
        activePlayer = random.Next(2);

        for (int dealRound = 0; dealRound < 2; dealRound++)
        {
            DealCardsToPlayer(activePlayer);
            DealCardsToPlayer(1 - activePlayer);
        }

        trumpCard = deck[deck.Count - 1];
        deck.RemoveAt(deck.Count - 1);
        deck.Insert(0, trumpCard);

        TrumpCardPower(deck);
        TrumpCardPower(player1Cards);
        TrumpCardPower(player2Cards);
    }

    private void TrumpCardPower(IEnumerable<CardData> cards)
    {
        foreach (CardData card in cards)
        {
            if (card.suit == trumpCard.suit)
                card.power += 6;
        }
    }

    private void DealCardsToPlayer(int playerIndex)
    {
        for (int i = 0; i < 3; i++)
        {
            CardData cardData = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);

            if (playerIndex == 0)
                player1Cards.Add(cardData);
            else
                player2Cards.Add(cardData);
        }
    }

    private CardData CreateCard(Suit suit, Rank rank, int points, int power, int cardId)
    {
        return new CardData(suit, rank, points, power, cardId);
    }

    #endregion

    #region Play Actions
    public void DrawCard()
    {
        if (turnPhase != TurnPhase.Drawing)
        {
            RejectMessage("Action Rejected: Invalid input.");
            return;
        }

        if (!canDrawCards)
        {
            playersDrawn++;
            activePlayer = 1 - activePlayer;
            OnTurnChanged?.Invoke(activePlayer);

            if (playersDrawn == 2)
                StartNextTurn();

            if (deck.Count > 0)
                RejectMessage("Action Rejected: The deck is closed.");

            else
                RejectMessage("Action Rejected: The deck is empty.");

            return;
        }

        if (activePlayer == 0)
            player1Cards.Add(deck[deck.Count - 1]);

        else
            player2Cards.Add(deck[deck.Count - 1]);

        deck.RemoveAt(deck.Count - 1);
        OnCardDrawn?.Invoke(activePlayer);

        playersDrawn++;
        activePlayer = 1 - activePlayer;
        OnTurnChanged?.Invoke(activePlayer);

        if (playersDrawn == 2)
            StartNextTurn();
    }

    public void PlayCard(int playerId, int cardId)
    {
        if (turnPhase == TurnPhase.Drawing)
        {
            RejectMessage("Action Rejected: Draw a card first.");
            return;
        }

        if (playerId != activePlayer)
        {
            RejectMessage("Action Rejected: Not your turn.");
            return;
        }

        if (deck.Count == 0)
            canDrawCards = false;

        List<CardData> hand = (playerId == 0) ? player1Cards : player2Cards;

        CardData card = hand.Find(c => c.cardId == cardId);

        if (turnPhase == TurnPhase.WaitingFirstCard)
        {
            if (marriageDeclaredThisTurn)
            {
                bool validCards = card.suit == marriageSuit && (card.rank == Rank.King || card.rank == Rank.Queen);

                if (!validCards)
                {
                    RejectMessage("Action Rejected: Not a valid marraige.");
                    return;
                }
            }

            firstPlayedCard = card;
            OnTrickUpdated?.Invoke(firstPlayedCard, null);
            trickLeader = playerId;

            activePlayer = 1 - activePlayer;
            OnTurnChanged?.Invoke(activePlayer);
            turnPhase = TurnPhase.WaitingSecondCard;
        }

        else if (turnPhase == TurnPhase.WaitingSecondCard)
        {
            int trickWinner;

            if (!canDrawCards)
            {
                hand = GetPlayerHand(playerId);

                bool hasSameSuit = HasSuit(hand, firstPlayedCard.suit);
                bool hasTrump = HasTrump(hand);

                if (hasSameSuit && card.suit != firstPlayedCard.suit)
                {
                    RejectMessage("Action Rejected: You must play a card from the same suit.");
                    return;
                }

                if (!hasSameSuit && hasTrump && card.suit != trumpCard.suit)
                {
                    RejectMessage("Action Rejected: You must play a card from the trump suit.");
                    return;
                }
            }

            secondPlayedCard = card;
            OnTrickUpdated?.Invoke(firstPlayedCard, secondPlayedCard);
            trickWinner = TrickWinner();
            AwardPoints(trickWinner);

            lastTrickWinner = trickWinner;

            activePlayer = trickWinner;
            OnTurnChanged?.Invoke(activePlayer);

            turnPhase = TurnPhase.Drawing;

            if (!canDrawCards)
            {
                DrawCard();
                DrawCard();
            }
        }

        hand.Remove(card);

        OnCardPlayed?.Invoke(playerId, cardId);
        CheckGameEnd();
    }

    public void DeclareMarriage(int playerId, Suit suit)
    {
        if (!CanMarriage(playerId))
        {
            RejectMessage("Action Rejected: You cannot declare a marraige.");
            return;
        }

        List<CardData> hand = GetPlayerHand(playerId);

        bool hasKing = hand.Exists(c => c.rank == Rank.King && c.suit == suit);
        bool hasQueen = hand.Exists(c => c.rank == Rank.Queen && c.suit == suit);

        if (!HasMarriage(hand, suit))
            return;

        int points = (suit != trumpCard.suit) ? 20 : 40;

        if (playerId == 0)
            player1Score += points;
        else
            player2Score += points;

        marriageDeclaredThisTurn = true;
        marriageSuit = suit;
        OnMarriageDeclared?.Invoke(playerId, suit);

        Debug.WriteLine($"{points} points awarded to Player {playerId + 1} for marraige");
    }

    public bool CanMarriage(int playerId)
    {
        if (turnPhase != TurnPhase.WaitingFirstCard)
        {
            RejectMessage("Action Rejected: Not your turn.");
            return false;
        }

        if (playerId != activePlayer)
            return false;

        List<CardData> hand = GetPlayerHand(playerId);

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            if (HasMarriage(hand, suit))
                return true;
        }

        return false;
    }

    public void CloseDrawingDeck(int playerId)
    {
        if (turnPhase != TurnPhase.WaitingFirstCard)
            return;

        if (playerId != activePlayer)
            return;  

        if (deck.Count <= 2)
        {
            RejectMessage("Action Rejected: Not enough cards to close deck.");
            return;
        }

        canDrawCards = false;
        deckClosedByPlayer = true;
        closingPlayer = playerId;
        OnDeckClosed?.Invoke(playerId);
    }

    public void ExchangeTrump(int playerId)
    {
        if (turnPhase != TurnPhase.WaitingFirstCard)
        {
            RejectMessage("Action Rejected: Invalid input.");
            return;
        }

        if (playerId != activePlayer)
        {
            RejectMessage("Action Rejected: Not your turn.");
            return;
        }

        if (deck.Count <= 2)
        {
            RejectMessage("Action Rejected: Not enough cards to exchange the trump card.");
            return;
        }

        if (!canDrawCards)
        {
            RejectMessage("Action Rejected: Cannot exchange the trump card when the deck is closed.");
            return;
        }

        List<CardData> hand = GetPlayerHand(playerId);

        CardData nine = hand.Find(c => c.rank == Rank.Nine && c.suit == trumpCard.suit);

        if (nine == null)
        {
            RejectMessage("Action Rejected: You need the Nine of trump suit to exchange it.");
            return;
        }

        hand.Remove(nine);
        hand.Add(trumpCard);

        trumpCard = nine;

        OnTrumpExchanged?.Invoke(playerId);
    }

    public void TakeTrump(int playerId)
    {
        if (deck.Count != 1)
        {
            RejectMessage("Action Rejected: Trump can only be taken when it is the last card.");
            return;
        }

        if (playerId != activePlayer)
        {
            RejectMessage("Action Rejected: Not your turn.");
            return;
        }

        List<CardData> hand = GetPlayerHand(playerId);

        hand.Add(trumpCard);

        deck.Clear();

        OnTrumpTaken?.Invoke(playerId);
    }

    #endregion

    #region ServerCalculations
    private int TrickWinner()
    {
        if (firstPlayedCard.power > secondPlayedCard.power)
            return trickLeader;

        if (secondPlayedCard.power > firstPlayedCard.power)
            return 1 - trickLeader;

        return trickLeader;
    }

    private void AwardPoints(int winner)
    {
        int points = firstPlayedCard.points + secondPlayedCard.points;
        if (winner == 0)
        {
            player1Score += points;
            player1TakenCards.Add(firstPlayedCard);
            player1TakenCards.Add(secondPlayedCard);
        }

        else
        {
            player2Score += points;
            player2TakenCards.Add(firstPlayedCard);
            player2TakenCards.Add(secondPlayedCard);
        }

        Debug.WriteLine($"{points} points awarded to Player {winner + 1}");
    }

    private void StartNextTurn()
    {
        firstPlayedCard = null;
        secondPlayedCard = null;

        playersDrawn = 0;

        marriageDeclaredThisTurn = false;

        turnPhase = TurnPhase.WaitingFirstCard;
        OnTrickUpdated?.Invoke(firstPlayedCard, secondPlayedCard);
        OnTurnChanged?.Invoke(activePlayer);
    }

    private void CheckGameEnd()
    {
        bool emptyHands = player1Cards.Count == 0 && player2Cards.Count == 0;

        if (!emptyHands)
            return;

        int roundWinner = RoundWinner();
        SessionPoints(roundWinner);
        OnRoundEnd?.Invoke(roundWinner);
        CheckSessionEnd();
        Reset();
    }

    public void ForceRoundEnd(int playerId)
    {
        if (GetPoints(playerId) < 66)
        {
            RejectMessage("Action Rejected: You do not have 66 points.");
            return;
        }

        SessionPoints(playerId);
        OnRoundEnd?.Invoke(playerId);
        CheckSessionEnd();
        Reset();
    }

    private void SessionPoints(int winner)
    {
        int loser = 1 - winner;

        int loserScore = GetPoints(loser);
        int points = 1;

        bool loserHasTakenCards = GetTakenCards(loser).Count > 0;

        if (deckClosedByPlayer)
        {
            int score = GetPoints(closingPlayer);

            if (score < 66)
                points = 3;

            else
                points = CalculatePoints(loserScore, loserHasTakenCards);
        }

        else
            points = CalculatePoints(loserScore, loserHasTakenCards);

        if (winner == 0)
            player1SessionScore += points;
        
        else
            player2SessionScore += points;

        OnSessionScoreUpdate?.Invoke(player1SessionScore, player2SessionScore);
    }

    #endregion

    #region SendingInformation
    public int GetActivePlayer()
    {
        return activePlayer;
    }

    public int GetPoints(int playerId)
    {
        return (playerId == 0) ? player1Score : player2Score;
    }

    public int GetDeckCount()
    {
        return deck.Count;
    }

    public List<CardData> GetPlayerHand(int playerId)
    {
        return (playerId == 0) ? player1Cards : player2Cards;
    }

    public List<CardData> GetTakenCards(int playerId)
    {
        return playerId == 0 ? player1TakenCards : player2TakenCards;
    }

    public List<Suit> GetAvailableMarriages(int playerId)
    {
        List<Suit> result = new List<Suit>();

        if (turnPhase != TurnPhase.WaitingFirstCard)
            return result;

        if (playerId != activePlayer)
            return result;

        List<CardData> hand = GetPlayerHand(playerId);

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        { 
            if (HasMarriage(hand, suit))
                result.Add(suit);
        }

        return result;
    }

    private bool HasSuit(List<CardData> hand, Suit suit)
    {
        return hand.Exists(c => c.suit == suit);
    }

    private bool HasTrump(List<CardData> hand)
    {
        return hand.Exists(c => c.suit == trumpCard.suit);
    }

    private bool HasMarriage(List<CardData> hand, Suit suit)
    {
        bool hasKing = hand.Exists(c => c.rank == Rank.King && c.suit == suit);
        bool hasQueen = hand.Exists(c => c.rank == Rank.Queen && c.suit == suit);

        return hasKing && hasQueen;
    }

    private int RoundWinner()
    {
        if (deckClosedByPlayer)
        {
            int score = GetPoints(closingPlayer);

            if (score >= 66)
            {
                return closingPlayer;
            }

            else
                return 1 - closingPlayer;
        }

        if (player1Score >= 66)
            return 0;
        
        if (player2Score >= 66)
            return 1;

        return lastTrickWinner;
    }

    private int CalculatePoints(int loserScore, bool loserHasTakenCards)
    {
        if (!loserHasTakenCards)
            return 3;

        if (loserScore < 33)
            return 2;

        return 1;
    }

    private void CheckSessionEnd()
    {
        if (player1SessionScore >= sessionMaxScore)
            OnSessionEnd?.Invoke(0);

        else if (player2SessionScore >= sessionMaxScore)
            OnSessionEnd?.Invoke(1);
    }

    private void Reset()
    {
        player1Cards.Clear();
        player2Cards.Clear();
        player1TakenCards.Clear();
        player2TakenCards.Clear();

        player1Score = 0;
        player2Score = 0;

        deck.Clear();

        canDrawCards = true;
        deckClosedByPlayer = false;
        closingPlayer = -1;

        StartGame();
    }

    public void SendSessionScore()
    {
        OnSessionScoreUpdate?.Invoke(player1SessionScore, player2SessionScore);
    }

    private void RejectMessage(string message)
    {
        OnActionRejected?.Invoke(message);
    }

    #endregion
}