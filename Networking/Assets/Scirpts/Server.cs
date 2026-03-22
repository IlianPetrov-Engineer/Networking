using System;
using System.Collections.Generic;
using System.Diagnostics;


// TO DO:
// - When impleting networking, set "localActivePlayer" from "Client" to 0 or 1, depending on order at which players join the server 

public class Server
{
    List<CardData> deck = new List<CardData>();

    List<CardData> player1Cards = new List<CardData>();
    List<CardData> player2Cards = new List<CardData>();

    CardData firstPlayedCard;
    CardData secondPlayedCard;

    private Random random = new Random();

    int activePlayer;
    CardData trumpCard;

    public void StartGame()
    {
        CreateDeck();
        ShuffleDeck();
        DealCardsMain();

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
        }*/

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

        Debug.WriteLine($"{deck.Count}");
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
    }

    void DealCardsToPlayer(int playerIndex)
    {
        for (int i = 0; i< 3; i++)
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

    public bool PlayCard(int playerId, int cardId)
    {
        if (playerId != activePlayer) 
            return false;

        List<CardData> hand = (playerId == 0) ? player1Cards : player2Cards;

        CardData card = hand.Find(c  => c.cardId == cardId);

        if (card == null)
            return false;
    
        hand.Remove(card);

        if (firstPlayedCard == null)
        {
            firstPlayedCard = card;
            activePlayer = 1 - activePlayer;
        }

        else
        {
            secondPlayedCard = card;
            activePlayer = 1 - activePlayer;
        }

        return true;
    }

    public List<CardData> GetPlayerHand(int playerId)
    {
        return (playerId == 0) ? player1Cards : player2Cards;
    }

    public int GetActivePlayer()
    {
        return activePlayer;
    }

    public void DrawCard()
    {
        if (activePlayer == 0)
        {
            player1Cards.Add(deck[deck.Count - 1]);
            deck.RemoveAt(deck.Count - 1);
        }

        else
        {
            player2Cards.Add(deck[deck.Count - 1]);
            deck.RemoveAt(deck.Count - 1);
        }
    }
}
