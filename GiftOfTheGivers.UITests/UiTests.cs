using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace GiftOfTheGivers.UITests;

// A.4 - Selenium UI tests. PREREQUISITES:
//   1. The web app (GiftOfTheGivers) is running - either locally in VS (note the https port it opens on)
//      or deployed. Point the tests at it with the APP_BASE_URL environment variable.
//   2. Google Chrome is installed (Selenium Manager downloads the matching driver automatically).
public class UiTests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl =
        (Environment.GetEnvironmentVariable("APP_BASE_URL") ?? "https://localhost:7000").TrimEnd('/');

    public UiTests()
    {
        var options = new ChromeOptions();
        options.AddArgument("--ignore-certificate-errors");   // local dev HTTPS certificate
        if (Environment.GetEnvironmentVariable("HEADLESS") == "1")
        {
            options.AddArgument("--headless=new");
        }

        _driver = new ChromeDriver(options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    }

    // Scenario 1: seeded employee logs in and reaches the staff dashboard.
    [Fact]
    public void EmployeeLogin_ReachesEmployeeDashboard()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Login");

        _driver.FindElement(By.Id("Email")).SendKeys("employee@giftofthegivers.local");
        _driver.FindElement(By.Id("Password")).SendKeys("Employee@123");
        _driver.FindElement(By.CssSelector("button.auth-submit")).Click();

        _wait.Until(d => d.Url.Contains("EmployeeDashboard"));

        Assert.Contains("Employee Dashboard", _driver.PageSource);
        Assert.NotEmpty(_driver.FindElements(By.CssSelector("form.logout-form")));   // nav shows Logout when signed in
    }

    // Scenario 2: a newly registered donor is blocked from the employee-only area (role-based access).
    [Fact]
    public void DonorRegistration_IsDeniedAccessToEmployeeDashboard()
    {
        var email = $"donor{Guid.NewGuid():N}@example.com";

        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Register");
        _driver.FindElement(By.Id("FullName")).SendKeys("Selenium Donor");
        _driver.FindElement(By.Id("Email")).SendKeys(email);
        _driver.FindElement(By.Id("Password")).SendKeys("Donor@123");
        _driver.FindElement(By.Id("ConfirmPassword")).SendKeys("Donor@123");
        _driver.FindElement(By.CssSelector("button.auth-submit")).Click();

        _wait.Until(d => d.FindElements(By.CssSelector("form.logout-form")).Count > 0);   // registered + signed in

        _driver.Navigate().GoToUrl($"{_baseUrl}/EmployeeDashboard");
        _wait.Until(d => d.Url.Contains("AccessDenied"));

        Assert.Contains("Access denied", _driver.PageSource);
    }

    // Scenario 3: an invalid login shows an error and stays on the login page.
    [Fact]
    public void Login_WithWrongPassword_ShowsError()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Account/Login");

        _driver.FindElement(By.Id("Email")).SendKeys("employee@giftofthegivers.local");
        _driver.FindElement(By.Id("Password")).SendKeys("WrongPassword1");
        _driver.FindElement(By.CssSelector("button.auth-submit")).Click();

        _wait.Until(d => d.PageSource.Contains("Invalid login attempt"));

        Assert.Contains("/Account/Login", _driver.Url);
    }

    public void Dispose() => _driver.Quit();
}
