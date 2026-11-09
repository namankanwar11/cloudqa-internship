using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
namespace CloudQA.Core;

public sealed class PracticeForm(IWebDriver driver, Action<string> log, TimeSpan? timeout = null)
{
    private readonly SemanticDiscovery discovery = new(driver, log, timeout);
    public void Fill(string firstName, string gender, string state)
    {
        Act(new("text", "First Name"), e => { e.Clear(); e.SendKeys(firstName); },
            e => e.GetDomProperty("value") == firstName, "First Name");
        discovery.Scope = (IWebElement?)((IJavaScriptExecutor)driver).ExecuteScript("return arguments[0].closest('form');", discovery.Find(new("text", "First Name")));
        // Option label is an accessible name. Group context is optional because the live form
        // may not expose a fieldset; duplicate option names fail safely as ambiguous.
        Act(new("radio", gender), e => { if (!e.Selected) e.Click(); },
            e => e.Selected, $"Gender={gender}");
        Act(new("select", "State"), e =>
        {
            var select = new SelectElement(e);
            var matches = select.Options.Where(o => SemanticDiscovery.Normalize(o.Text) ==
                SemanticDiscovery.Normalize(state) && o.Enabled).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException($"Expected one enabled State option '{state}', found {matches.Length}");
            matches[0].Click();
        }, e => SemanticDiscovery.Normalize(new SelectElement(e).SelectedOption.Text) ==
            SemanticDiscovery.Normalize(state), $"State={state}");
    }
    private void Act(Target target, Action<IWebElement> action, Func<IWebElement, bool> verify, string description)
    {
        var wait = new WebDriverWait(driver, timeout ?? TimeSpan.FromSeconds(12));
        wait.PollingInterval = TimeSpan.FromMilliseconds(200);
        wait.Until(_ =>
        {
            try
            {
                var element = discovery.Find(target);
                if (!verify(element)) action(element);
                return verify(discovery.Find(target));
            }
            catch (StaleElementReferenceException) { log($"RETRY stale element: {description}"); return false; }
            catch (ElementClickInterceptedException) { log($"RETRY intercepted: {description}"); return false; }
            catch (ElementNotInteractableException) { log($"RETRY not interactable: {description}"); return false; }
        });
        log($"VERIFIED {description}");
    }
}