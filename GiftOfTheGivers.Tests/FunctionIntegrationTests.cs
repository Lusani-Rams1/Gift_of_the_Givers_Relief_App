using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Data.Tables;

namespace GiftOfTheGivers.Tests;

// A.2 - Integration tests: exercise the real Azure Functions over HTTP and verify the
// result landed in storage. PREREQUISITES before running these:
//   1. Azurite (storage emulator) is running
//   2. GiftOfTheGivers.Functions is running (F5 / func start) on http://localhost:7071
// Override the defaults with FUNCTION_BASE_URL / STORAGE_CONNECTION to test the deployed Function App.
public class FunctionIntegrationTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("FUNCTION_BASE_URL") ?? "http://localhost:7071/api";

    private static readonly string StorageConnection =
        Environment.GetEnvironmentVariable("STORAGE_CONNECTION") ?? "UseDevelopmentStorage=true";

    private static readonly HttpClient Http = new();

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    [Fact]
    public async Task Donation_ThroughFunction_ReturnsCertificateForDonor()
    {
        var response = await Http.PostAsync($"{BaseUrl}/GenerateTaxCertificate",
            Json(new { donorName = "Integration Tester", amount = 250, currency = "ZAR" }));

        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("GG-TAX-", html);
        Assert.Contains("Integration Tester", html);
    }

    [Fact]
    public async Task Donation_WithInvalidAmount_IsRejectedByFunction()
    {
        var response = await Http.PostAsync($"{BaseUrl}/GenerateTaxCertificate",
            Json(new { donorName = "Integration Tester", amount = -5, currency = "ZAR" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmployeeUpdate_ThroughFunction_IsPersistedInTableStorage()
    {
        var project = $"IntegrationTest-{Guid.NewGuid():N}";   // unique partition per run

        var response = await Http.PostAsync($"{BaseUrl}/LogProjectUpdate",
            Json(new { projectName = project, updateText = "200 food parcels delivered", postedBy = "Test Employee" }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify the row really exists in the storage "database"
        var table = new TableClient(StorageConnection, "ProjectUpdates");
        var rows = new List<TableEntity>();
        await foreach (var row in table.QueryAsync<TableEntity>(e => e.PartitionKey == project))
        {
            rows.Add(row);
        }

        var saved = Assert.Single(rows);
        Assert.Equal("200 food parcels delivered", saved.GetString("UpdateText"));
        Assert.Equal("Test Employee", saved.GetString("PostedBy"));
    }
}
