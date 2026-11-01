using GiftOfTheGivers.Helpers;
using Xunit;

namespace GiftOfTheGivers.Tests;

// A.1 - Unit tests: fast, isolated, no network or database.
public class DonationHelperTests
{
    [Theory]
    [InlineData(250, "ZAR")]
    [InlineData(10.50, "usd")]
    [InlineData(1, " EUR ")]
    public void IsValid_AcceptsPositiveAmountWithSupportedCurrency(decimal amount, string currency)
    {
        Assert.True(DonationValidator.IsValid(amount, currency));
    }

    [Theory]
    [InlineData(0, "ZAR")]
    [InlineData(-50, "ZAR")]
    [InlineData(100, "GBP")]
    [InlineData(100, "")]
    [InlineData(100, null)]
    public void IsValid_RejectsBadAmountOrCurrency(decimal amount, string? currency)
    {
        Assert.False(DonationValidator.IsValid(amount, currency));
    }

    [Fact]
    public void Total_SumsAllPositiveDonations()
    {
        var total = DonationCalculator.Total(new[] { 100m, 250.50m, 49.50m });

        Assert.Equal(400m, total);
    }

    [Fact]
    public void Total_IgnoresZeroAndNegativeAmounts()
    {
        var total = DonationCalculator.Total(new[] { 100m, -20m, 0m, 50m });

        Assert.Equal(150m, total);
    }

    [Fact]
    public void Total_ReturnsZeroForNoDonations()
    {
        Assert.Equal(0m, DonationCalculator.Total(Array.Empty<decimal>()));
    }

    [Fact]
    public void FormatNumber_PadsSequenceAndUsesDatePrefix()
    {
        var number = CertificateFormatter.FormatNumber(new DateTime(2026, 10, 29), 42);

        Assert.Equal("GG-TAX-20261029-0042", number);
    }
}
