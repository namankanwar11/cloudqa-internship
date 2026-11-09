using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CloudQA.Core;

public sealed class AmbiguousElementException(string message) : Exception(message);
public sealed record Target(string Kind, string Name, string? Context = null);
public sealed record Candidate(IWebElement Element, int Score, string Evidence);

public sealed class SemanticDiscovery(IWebDriver driver, Action<string> log, TimeSpan? timeout = null)
{
    private static readonly string Script = ReadScript();
    private static string ReadScript()
    {
        using var stream = typeof(SemanticDiscovery).Assembly.GetManifestResourceStream("CloudQA.Core.Discovery.js")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    public static string Normalize(string value) =>
        Regex.Replace(value.ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ").Trim();

    public IWebElement? Scope { get; set; }
    public IWebElement Find(Target target)
    {
        var wait = new WebDriverWait(driver, timeout ?? TimeSpan.FromSeconds(12))
        { PollingInterval = TimeSpan.FromMilliseconds(200) };
        string last = "No eligible candidates";
        try
        {
            return wait.Until(_ =>
            {
                try
                {
                    var raw = (IEnumerable<object>)((IJavaScriptExecutor)driver).ExecuteScript(Script, target.Kind, Scope!)!;
                    var ranked = raw.Select(item =>
                    {
                        var row = (IDictionary<string, object>)item;
                        var element = (IWebElement)row["element"];
                        if (!element.Displayed || !element.Enabled || element.GetDomAttribute("readonly") != null)
                            return (Candidate?)null;
                        string Value(string key) => row[key]?.ToString() ?? "";
                        int Match(string key, int weight) => Normalize(Value(key)) == Normalize(target.Name) ? weight : 0;
                        // Exact normalized names avoid "First name confirmation" and Male/Female collisions.
                        var score = new[] { Match("labels", 100), Match("accessible", 100),
                            Normalize(Value("accessible")) == Normalize(target.Name) ? 100 : 0,
                            Match("placeholder", 70), Match("nearby", 60) }.Max();
                        if (target.Context != null)
                        {
                            if (Normalize(Value("context")) != Normalize(target.Context)) score = 0;
                            else if (score > 0) score += 20;
                        }
                        return new Candidate(element, score,
                            $"label='{Value("labels")}', accessible='{Value("accessible")}', nearby='{Value("nearby")}', group='{Value("context")}'");
                    }).Where(c => c != null && c.Score >= 60).Cast<Candidate>()
                      .OrderByDescending(c => c.Score).ToArray();
                    last = string.Join(" | ", ranked.Select(c => $"{c.Score}: {c.Evidence}"));
                    if (ranked.Length == 0) return null;
                    if (ranked.Length > 1 && ranked[0].Score - ranked[1].Score < 15)
                        throw new AmbiguousElementException($"Ambiguous {target}: {last}");
                    log($"DISCOVER {target}: {last}");
                    return ranked[0].Element;
                }
                catch (StaleElementReferenceException) { return null; }
            })!;
        }
        catch (WebDriverTimeoutException ex)
        {
            throw new WebDriverTimeoutException($"Cannot discover {target} within {wait.Timeout}. Candidates: {last}", ex);
        }
    }
}