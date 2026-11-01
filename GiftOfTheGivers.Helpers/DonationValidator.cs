namespace GiftOfTheGivers.Helpers;

public static class DonationValidator
{
    private static readonly HashSet<string> SupportedCurrencies =
        new(StringComparer.OrdinalIgnoreCase) { "ZAR", "USD", "EUR" };

    // A donation is valid when the amount is positive and the currency is one the foundation accepts.
    public static bool IsValid(decimal amount, string? currency) =>
        amount > 0
        && !string.IsNullOrWhiteSpace(currency)
        && SupportedCurrencies.Contains(currency.Trim());
}
