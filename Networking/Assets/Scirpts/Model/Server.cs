using System;
using System.Collections.Generic;
using System.Diagnostics;

// TO DO:
// - When impleting networking, set "localActivePlayer" from "Client" to 0 or 1, depending on order at which players join the server 
// - Add punishment for spaming "Call Marraige" and "Declare 66"
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
    #endregion

    #region Actions
    public event Action<int, int> OnCardPlayed;
    public event Action<int> OnCardDrawn;
    public event Action<int> OnDeckClosed;
    public event Action<int, Suit> OnMarriageDeclared;
    public event Action<int> OnTurnChanged;
    public event Action<int> OnTrumpExchanged;
    public event Action<int> OnTrumpTaken;

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

    void CreateDeck()
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

    private CardData CreateCard(Suit suit, Rank rank, int points, int power, int cardId)
    {
        return new CardData(suit, rank, points, power, cardId);
    }

    void ShuffleDeck()
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

    void DealCardsMain()
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

    void TrumpCardPower(IEnumerable<CardData> cards)
    {
        foreach (CardData card in cards)
        {
            if (card.suit == trumpCard.suit)
                card.power += 6;
        }
    }

    void DealCardsToPlayer(int playerIndex)
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

    public void PlayCard(int playerId, int cardId)
    {
        if (turnPhase == TurnPhase.Drawing)
        {
            OnActionRejected?.Invoke("Cannot play card during drawing.");
            return;
        }

        if (playerId != activePlayer)
        {
            OnActionRejected?.Invoke("It is not your turn");
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
                    OnActionRejected?.Invoke("You do not have a valid marraige.");
                    return;
                }
            }

            firstPlayedCard = card;
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
                    OnActionRejected?.Invoke("You must play a card with the same suit as the first card.");
                    return;
                }

                if (!hasSameSuit && hasTrump && card.suit != trumpCard.suit)
                {
                    OnActionRejected?.Invoke("You must play a card from the trump suit.");
                    return;
                }
            }

            secondPlayedCard = card;

            trickWinner = TrickWinner();

            AwardPoints(trickWinner);

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
    }

    int TrickWinner()
    {
        if (firstPlayedCard.power > secondPlayedCard.power)
            return trickLeader;

        if (secondPlayedCard.power > firstPlayedCard.power)
            return 1 - trickLeader;

        return trickLeader;
    }

    public void DrawCard()
    {
        if (turnPhase != TurnPhase.Drawing)
        {
            OnActionRejected?.Invoke("You cannot draw a card. It is not your turn.");
            return;
        }

        if (!canDrawCards)
        {
            playersDrawn++;
            activePlayer = 1 - activePlayer;
            OnTurnChanged?.Invoke(activePlayer);
            if (playersDrawn == 2)
                StartNextTurn();

            OnActionRejected?.Invoke("You cannot draw a card. The pile is closed");
            return;
        }

        if (deck.Count == 0)
        {
            OnActionRejected?.Invoke("There are no more cards to draw. The pile is empty.");
            return;
        }

        if (activePlayer == 0)
        {
            player1Cards.Add(deck[deck.Count - 1]);
        }

        else
        {
            player2Cards.Add(deck[deck.Count - 1]);
        }

        deck.RemoveAt(deck.Count - 1);
        OnCardDrawn?.Invoke(activePlayer);

        playersDrawn++;
        activePlayer = 1 - activePlayer;
        OnTurnChanged?.Invoke(activePlayer);

        if (playersDrawn == 2)
        {
            StartNextTurn();
        }
    }

    void StartNextTurn()
    {
        firstPlayedCard = null;
        secondPlayedCard = null;

        playersDrawn = 0;

        marriageDeclaredThisTurn = false;

        turnPhase = TurnPhase.WaitingFirstCard;
        OnTurnChanged?.Invoke(activePlayer);
    }

    public List<CardData> GetPlayerHand(int playerId)
    {
        return (playerId == 0) ? player1Cards : player2Cards;
    }

    public int GetActivePlayer()
    {
        return activePlayer;
    }

    void AwardPoints(int winner)
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

    public int GetPoints(int playerId)
    {
        return (playerId == 0) ? player1Score : player2Score;
    }

    public void DeclareMarriage(int playerId, Suit suit)
    {
        if (!CanMarriage(playerId))
        {
            OnActionRejected?.Invoke("You cannot declare a marraige.");
            return;
        }

        List<CardData> hand = GetPlayerHand(playerId);

        bool hasKing = hand.Exists(c => c.rank == Rank.King && c.suit == suit);
        bool hasQueen = hand.Exists(c => c.rank == Rank.Queen && c.suit == suit);

        if (!hasKing || !hasQueen)
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
            return false;

        if (playerId != activePlayer)
            return false;

        List<CardData> hand = GetPlayerHand(playerId);

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            bool hasKing = hand.Exists(c => c.rank == Rank.King && c.suit == suit);
            bool hasQueen = hand.Exists(c => c.rank == Rank.Queen && c.suit == suit);

            if (hasKing && hasQueen)
                return true;
        }

        return false;
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
            bool hasKing = hand.Exists(c => c.rank == Rank.King && c.suit == suit);
            bool hasQueen = hand.Exists(c => c.rank == Rank.Queen && c.suit == suit);

            if (hasKing && hasQueen)
                result.Add(suit);
        }

        return result;
    }

    public void CloseDrawingDeck(int playerId)
    {
        if (turnPhase != TurnPhase.WaitingFirstCard)
            return;

        if (playerId != activePlayer)
            return;

        if (deck.Count <= 2)
        {
            OnActionRejected?.Invoke("Not enough cards to close deck");
            return;
        }

        canDrawCards = false;
        OnDeckClosed?.Invoke(playerId);
    }

    bool HasSuit(List<CardData> hand, Suit suit)
    {
        return hand.Exists(c => c.suit == suit);
    }

    bool HasTrump(List<CardData> hand)
    {
        return hand.Exists(c => c.suit == trumpCard.suit);
    }

    public int GetDeckCount()
    {
        return deck.Count;
    }

    public void ExchangeTrump(int playerId)
    {
        if (turnPhase != TurnPhase.WaitingFirstCard)
        {
            OnActionRejected?.Invoke("You can exchange the trump card.");
            return;
        }

        if (!canDrawCards)
        {
            OnActionRejected?.Invoke("Cannot exchange the trump card when the deck is closed.");
            return;
        }

        if (playerId != activePlayer)
        {
            OnActionRejected?.Invoke("Not your turn.");
            return;
        }

        if (deck.Count <= 2)
        {
            OnActionRejected?.Invoke("Not enough cards to exchange the trump card.");
            return;
        }

        List<CardData> hand = GetPlayerHand(playerId);

        CardData nine = hand.Find(c => c.rank == Rank.Nine && c.suit == trumpCard.suit);

        if (nine == null)
        {
            OnActionRejected?.Invoke("You need the Nine of trump suit to exchange it.");
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
            OnActionRejected?.Invoke("Trump can only be taken when it is the last card.");
            return;
        }

        if (playerId != activePlayer)
        {
            OnActionRejected?.Invoke("Not your turn.");
            return;
        }

        List<CardData> hand = GetPlayerHand(playerId);

        hand.Add(trumpCard);

        deck.Clear(); 

        OnTrumpTaken?.Invoke(playerId);
    }

    public List<CardData> GetTakenCards(int playerId)
    {
        return playerId == 0 ? player1TakenCards : player2TakenCards;
    }
}