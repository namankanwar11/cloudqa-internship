using CloudQA.Core;
using OpenQA.Selenium;
var settings = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
{
    ["url"] = Environment.GetEnvironmentVariable("CLOUDQA_URL") ?? "https://app.cloudqa.io/home/AutomationPracticeForm",
    ["first-name"] = "Naman", ["gender"] = "Male", ["state"] = "India", ["headless"] = "true"
};
try
{
    for (int i=0;i<args.Length;i+=2)
    {
        if (!args[i].StartsWith("--") || i+1>=args.Length || !settings.ContainsKey(args[i][2..]))
            throw new ArgumentException("Use --url URL --first-name TEXT --gender Male --state TEXT --headless true|false");
        settings[args[i][2..]]=args[i+1];
    }
    var uri = new Uri(settings["url"], UriKind.Absolute);
    if (uri.Scheme is not ("http" or "https" or "file")) throw new ArgumentException("URL must use http, https or file.");
    var output = Path.Combine(Environment.CurrentDirectory,"artifacts",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
    Directory.CreateDirectory(output);
    using var writer = new StreamWriter(Path.Combine(output,"run.log")) { AutoFlush=true };
    void Log(string message) { var line=$"{DateTime.UtcNow:O} {message}"; Console.WriteLine(line); writer.WriteLine(line); }
    using var driver = Browser.Create(bool.Parse(settings["headless"]));
    try
    {
        Log($"NAVIGATE {uri}");
        driver.Navigate().GoToUrl(uri);
        new PracticeForm(driver,Log).Fill(settings["first-name"],settings["gender"],settings["state"]);
        Log("SUCCESS: all three actions verified. Form was not submitted.");
    }
    catch(Exception ex)
    {
        Log($"FAILURE {ex}");
        try { File.WriteAllText(Path.Combine(output,"failure.html"),driver.PageSource); }
        catch(Exception capture) { Log($"HTML unavailable: {capture.Message}"); }
        try { driver.GetScreenshot().SaveAsFile(Path.Combine(output,"failure.png")); }
        catch(Exception capture) { Log($"Screenshot unavailable: {capture.Message}"); }
        return 1;
    }
    return 0;
}
catch(Exception ex) { Console.Error.WriteLine(ex.Message); return 2; }