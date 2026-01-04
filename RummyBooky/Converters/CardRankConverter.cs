namespace RummyBooky.Converters;

public static class CardRankConverter
{
    public static string ConvertRankToSymbol(CardRanks rank)
    {
        return rank switch
        {
            CardRanks.Ace => "A",
            CardRanks.King => "K",
            CardRanks.Queen => "Q",
            CardRanks.Jack => "J",
            CardRanks.Ten => "10",
            CardRanks.Nine => "9",
            CardRanks.Eight => "8",
            CardRanks.Seven => "7",
            CardRanks.Six => "6",
            CardRanks.Five => "5",
            CardRanks.Four => "4",
            CardRanks.Three => "3",
            CardRanks.Two => "2",
            _ => string.Empty,
        };
    }
}
