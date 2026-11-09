using OpenQA.Selenium.Chrome;
namespace CloudQA.Core;
public static class Browser
{
    public static ChromeDriver Create(bool headless = true)
    {
        var options = new ChromeOptions();
        if (headless) options.AddArgument("--headless=new");
        options.AddArgument("--window-size=1440,1000");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--no-first-run");
        var binary = Environment.GetEnvironmentVariable("CHROME_BIN");
        if (!string.IsNullOrWhiteSpace(binary)) options.BinaryLocation = binary;
        var profile = Path.Combine(Path.GetTempPath(), "cloudqa-" + Guid.NewGuid());
        options.AddArgument("--user-data-dir=" + profile);
        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(45);
        return driver;
    }
}