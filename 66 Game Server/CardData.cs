public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public enum Rank
{
    Nine,
    Jack,
    Queen,
    King,
    Ten,
    Ace
}

public class CardData
{
    public Suit suit;
    public Rank rank;

    public int points;
    public int power;
    public int cardId;
    
    public CardData(Suit suit, Rank rank, int points, int power, int cardId)
    {
        this.suit = suit;
        this.rank = rank;
        this.points = points;
        this.power = power;
        this.cardId = cardId;
    }

    public string Serialize()
    {
        return $"{suit},{rank},{points},{power},{cardId}";
    }
}
