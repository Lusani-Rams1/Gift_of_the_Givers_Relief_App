using Gift_of_the_Givers_Relief_App.Data;
using Gift_of_the_Givers_Relief_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace Gift_of_the_Givers_Relief_App.Pages.Donators
{
    public class TaxCertificateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public TaxCertificateModel(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public class CertificateRequest
        {
            [Required(ErrorMessage = "Please enter your donation reference.")]
            public string Reference { get; set; } = string.Empty;

            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        [BindProperty]
        public CertificateRequest Requests { get; set; } = new();

        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public CertificateDetails? Certificate { get; set; }

        public class CertificateDetails
        {
            public string CertificateNumber { get; set; } = "";
            public string DonorName { get; set; } = "";
            public decimal Amount { get; set; }
            public DateTime IssuedOn { get; set; }
            public string Message { get; set; } = "";
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 1. Look up the donation by reference (DonationID) and email
            if (!int.TryParse(Requests.Reference.Trim(), out var donationId))
            {
                ErrorMessage = "Invalid donation reference. Please use the numeric donation ID from your receipt.";
                return Page();
            }

            var emailLower = Requests.Email.Trim().ToLowerInvariant();

            var donation = await _db.Donations
                .Include(d => d.Donor)
                    .ThenInclude(don => don.User)
                .FirstOrDefaultAsync(d => d.DonationID == donationId);

            if (donation is null)
            {
                ErrorMessage = $"No donation found with reference #{donationId}.";
                return Page();
            }

            // Verify the email matches the donation record (if we have one)
            var donorEmail = donation.Donor?.User?.Email?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(donorEmail) && donorEmail != emailLower)
            {
                ErrorMessage = "The email does not match the donation record.";
                return Page();
            }

            // 2. Call the Azure Function
            try
            {
                using var http = new HttpClient();

                var donorName = donation.Donor?.User is not null
                    ? $"{donation.Donor.User.FirstName} {donation.Donor.User.LastName}".Trim()
                    : "Anonymous";

                var payload = new
                {
                    DonorName = donorName,
                    DonorEmail = emailLower,
                    Amount = donation.Amount,
                    DonationDate = donation.DonationDate
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Read the function URL from configuration
                var url = _config["Functions:TaxCertificateUrl"];
                if (string.IsNullOrWhiteSpace(url))
                {
                    ErrorMessage = "Certificate service is not configured.";
                    return Page();
                }

                var resp = await http.PostAsync(url, content);

                if (!resp.IsSuccessStatusCode)
                {
                    ErrorMessage = $"Certificate service returned {resp.StatusCode}.";
                    return Page();
                }

                var certJson = await resp.Content.ReadAsStringAsync();

                Certificate = JsonSerializer.Deserialize<CertificateDetails>(certJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Message = "Tax certificate generated successfully.";
                Requests = new CertificateRequest();
            }
            catch (HttpRequestException)
            {
                ErrorMessage = "Could not reach the certificate service. Please ensure the Functions app is running.";
            }

            return Page();
        }
    }
}