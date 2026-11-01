namespace GiftOfTheGivers.Helpers;

public static class CertificateFormatter
{
    // e.g. GG-TAX-20261029-0042
    public static string FormatNumber(DateTime issuedOn, int sequence) =>
        $"GG-TAX-{issuedOn:yyyyMMdd}-{sequence:D4}";
}
