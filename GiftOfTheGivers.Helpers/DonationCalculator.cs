namespace GiftOfTheGivers.Helpers;

public static class DonationCalculator
{
    // Sums donation amounts, ignoring zero/negative entries so bad data can't reduce a total.
    public static decimal Total(IEnumerable<decimal> amounts) =>
        amounts.Where(a => a > 0).Sum();
}
