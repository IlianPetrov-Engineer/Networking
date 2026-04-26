using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

public enum TurnPhase
{
    WaitingFirstCard,
    WaitingSecondCard,
    Drawing
}

class Program
{
    static Server gameServer = new Server();

    static List<TcpClient> clients = new List<TcpClient>();

    static bool gameStarted = false;

    static void Main()
    {
        StartServer(50011);
    }

    static void StartServer(int port)
    {
        // Start listening for TCP connection requests, on the given port:
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Starting TCP server on port {port} - listening for incoming connection requests");
        Console.WriteLine("Press Q to stop the server");

        ///gameServer.StartGame();

        // Now we handle multiple connected clients simultaneously - 
        // we keep them in a list:
        ///List<TcpClient> clients = new List<TcpClient>();
        ///
        gameServer.OnCardPlayed += (player, card) =>
        {
            SendToAllClients($"CardPlayed|{player}|{card}|{card.Serialize()}");
        };

        gameServer.OnTurnChanged += (player) =>
        {
            SendToAllClients($"TurnChanged|{player}");
        };

        gameServer.OnCardDrawn += (player, card) =>
        {
            SendToAllClients($"CardDrawn|{player}|{card.Serialize()}");
        };

        gameServer.OnDeckClosed += (player) =>
        {
            SendToAllClients($"DeckClosed|{player}");
        };

        gameServer.OnMarriageDeclared += (player, suit) =>
        {
            SendToAllClients($"MarriageDeclared|{player}|{suit}");
        };

        gameServer.OnTrumpExchanged += (player) =>
        {
            SendToAllClients($"TrumpExchanged|{player}");
        };

        gameServer.OnTrumpTaken += (player) =>
        {
            SendToAllClients($"TrumpTaken|{player}");
        };

        gameServer.OnRoundEnd += (winner) =>
        {
            SendToAllClients($"RoundEnd|{winner}");
        };

        gameServer.OnSessionScoreUpdate += (p1, p2) =>
        {
            SendToAllClients($"ScoreUpdate|{p1}|{p2}");
        };

        gameServer.OnActionRejected += (msg) =>
        {
            SendToAllClients($"Error|{msg}");
        };

        gameServer.OnTrickUpdated += (c1, c2) =>
        {
            string id1 = c1 != null ? c1.Serialize() : "null";
            string id2 = c2 != null ? c2.Serialize() : "null";

            SendToAllClients($"TrickUpdate|{id1}|{id2}");
        };

        while (true)
        {
            // Note: there is no error handling in this server! Is it needed? If so, where?
            AcceptNewClients(listener, clients);
            HandleMessages(clients);
            // Clean up disconnected clients. Does this actually ever happen?!
            CleanupClients(clients);
            if (QuitPressed())
            {
                Console.WriteLine("Stopping server");
                break;
            }
            // It's good to give the CPU a break - 10ms is enough, and still gives fast response times:

            Thread.Sleep(10);
        }
        // When stopping the server, properly clean up all resources:
        foreach (TcpClient client in clients)
        {
            client.Close();
        }
        listener.Stop();
        Console.WriteLine("Server stopped");
    }

    static void AcceptNewClients(TcpListener listener, List<TcpClient> clients)
    {
        // Pending will be true if there is an incoming connection request:
        if (listener.Pending())
        {
            // ..if so, accept it and store the new TcpClient:
            // (Note that the AcceptTcpClient call is not blocking now, since we know there's a pending request!)
            TcpClient newClient = listener.AcceptTcpClient();
            clients.Add(newClient);
            Console.WriteLine($"Client connected from remote end point {newClient.Client.RemoteEndPoint}");
        }

        if (clients.Count == 2 && !gameStarted)
        {
            gameStarted = true;
            Console.WriteLine("2 players connected. Starting game.");

            gameServer.StartGame();

            SendToClient(clients[0], "AssignPlayer|0");
            SendToClient(clients[1], "AssignPlayer|1");

            SendToAllClients("GameStarted");
            SendInitialHands();
        }
    }

    static void SendInitialHands()
    {
        for (int i = 0; i < clients.Count; i++)
        {
            var hand = gameServer.GetPlayerHand(i);

            string cards = string.Join(";", hand.ConvertAll(c => c.Serialize()));

            SendToClient(clients[i], $"InitialHand|{cards}");
        }

        SendToAllClients($"TurnChanged|{gameServer.GetActivePlayer()}");
    }

    static void SendToClient(TcpClient client, string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);
        client.GetStream().Write(data, 0, data.Length);
    }

    static void HandleMessages(List<TcpClient> clients)
    {
        foreach (TcpClient client in clients)
        {
            // For each of the connected clients, we check whether there's an incoming message available:
            if (client.Available > 0)
            {
                // ..if so, we read exactly that many bytes into an array:
                NetworkStream stream = client.GetStream();
                int packetLength = client.Available;
                byte[] data = new byte[packetLength];
                stream.Read(data, 0, packetLength);
                Console.WriteLine($"Received a message of length {packetLength} from {client.Client.RemoteEndPoint} - echoing");
                // For now, we don't do anything special with the incoming message - 
                // just send it straight back to the sender:

                string message = Encoding.UTF8.GetString(data);

                HandleClientMessage(client, message);
            }
        }
    }

    static void CleanupClients(List<TcpClient> clients)
    {
        for (int i = clients.Count - 1; i >= 0; i--)
        {
            // If any of our current clients are disconnected, 
            // we close the TcpClient to clean up resources, and remove it from our list:
            // (Note that this type of for loop is needed since we're modifying the collection inside the loop!

            if (clients[i].Client.Poll(0, SelectMode.SelectRead) && clients[i].Available == 0/*clients[i].Connected*/)
            {
                //NetworkStream stream = clients[i].GetStream();
                //int packetLength = clients[i].Available;
                //byte[] data = new byte[packetLength];

                //int bytesRead = stream.Read(data, 0, data.Length);
                // This is one way in which TcpClients can indicate that the remote client has been closed:
                //if (bytesRead == 0)
                //{
                //	clients[i].Close();
                //	clients.RemoveAt(i);
                //	Console.WriteLine($"Removing client. Number of connected clients: {clients.Count}");
                //}

                clients[i].Close(); ///OS release of resources
                clients.RemoveAt(i);
                Console.WriteLine($"Removing client. Number of connected clients: {clients.Count}");
            }
        }
    }

    static void HandleClientMessage(TcpClient client, string message)
    {
        string[] parts = message.Split('|');

        switch (parts[0])
        {
            case "PlayCard":
                int player = int.Parse(parts[1]);
                int cardId = int.Parse(parts[2]);
                gameServer.PlayCard(player, cardId);
                break;

            case "Draw":
                gameServer.DrawCard();
                break;

            case "CloseDeck":
                gameServer.CloseDrawingDeck(int.Parse(parts[1]));
                break;

            case "DeclareMarriage":
                gameServer.DeclareMarriage(
                    int.Parse(parts[1]),
                    Enum.Parse<Suit>(parts[2])
                );
                break;

            case "ExchangeTrump":
                gameServer.ExchangeTrump(int.Parse(parts[1]));
                break;

            case "TakeTrump":
                gameServer.TakeTrump(int.Parse(parts[1]));
                break;

            case "DeclareWin":
                gameServer.ForceRoundEnd(int.Parse(parts[1]));
                break;
        }
    }

    static void SendToAllClients(string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);

        foreach (var client in clients)
        {
            if (client.Connected)
            {
                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);
            }
        }
    }

    static bool QuitPressed()
    {
        if (Console.KeyAvailable)
        {
            char input = Console.ReadKey(true).KeyChar;
            if (input == 'q')
            {
                return true;
            }
        }
        return false;
    }
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

    static TurnPhase turnPhase;
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
    public event Action<int, CardData> OnCardPlayed;
    public event Action<int, CardData> OnCardDrawn;
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

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                Console.WriteLine($"{rank} of {suit}");
            }
        }

        Console.WriteLine($"{deck.Count}");

        for (int i = 0; i < deck.Count; i++)
        {
            Console.WriteLine($"{deck[i].rank} of {deck[i].suit} with Id number = {deck[i].cardId}");
        }

        for (int i = 0; i < player1Cards.Count; i++)
        {
            Console.WriteLine($"Player 1 hand: {player1Cards[i].rank} of {player1Cards[i].suit} with Id number = {player1Cards[i].cardId}");
        }

        for (int i = 0; i < player2Cards.Count; i++)
        {
            Console.WriteLine($"Player 2 hand: {player2Cards[i].rank} of {player2Cards[i].suit} with Id number = {player2Cards[i].cardId}");
        }

        Console.WriteLine($"Trump card is {trumpCard.rank} of {trumpCard.suit} with Id number = {trumpCard.cardId}");

        for (int i = 0; i < deck.Count; i++)
        {
            Console.WriteLine($"{deck[i].rank} of {deck[i].suit} with Id number = {deck[i].cardId}");
        }

        Console.WriteLine($"{deck.Count}");
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

        CardData drawnCard = deck[deck.Count - 1];

        if (activePlayer == 0)
            player1Cards.Add(drawnCard);
        else
            player2Cards.Add(drawnCard);

        deck.RemoveAt(deck.Count - 1);

        OnCardDrawn?.Invoke(activePlayer, drawnCard);

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

        OnCardPlayed?.Invoke(playerId, card);
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