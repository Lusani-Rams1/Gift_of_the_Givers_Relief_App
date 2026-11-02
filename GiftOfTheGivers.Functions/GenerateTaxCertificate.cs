using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GiftOfTheGiversFunctions;

public class GenerateTaxCertificate
{
    private readonly ILogger<GenerateTaxCertificate> _logger;

    public GenerateTaxCertificate(ILogger<GenerateTaxCertificate> logger)
        => _logger = logger;

    public class DonationRequest
    {
        public string DonorName { get; set; } = "";
        public string DonorEmail { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime DonationDate { get; set; }
    }

    public class CertificateResponse
    {
        public string CertificateNumber { get; set; } = "";
        public string DonorName { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime IssuedOn { get; set; }
        public string Message { get; set; } = "";
    }

    [Function("GenerateTaxCertificate")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tax-certificate")]
        HttpRequest req,
        [FromBody] DonationRequest donation)
    {
        _logger.LogInformation("Tax certificate requested for {Donor}", donation?.DonorName);

        if (donation is null || donation.Amount <= 0)
        {
            return new BadRequestObjectResult("Invalid donation payload.");
        }

        var cert = new CertificateResponse
        {
            CertificateNumber = $"GOTG-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            DonorName = donation.DonorName,
            Amount = donation.Amount,
            IssuedOn = DateTime.UtcNow,
            Message = "Tax certificate generated successfully."
        };

        return new OkObjectResult(cert);
    }
}