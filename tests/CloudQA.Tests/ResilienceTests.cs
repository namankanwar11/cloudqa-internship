using CloudQA.Core;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;
namespace CloudQA.Tests;
public sealed class ResilienceTests(ITestOutputHelper output) : IDisposable
{
    private readonly OpenQA.Selenium.Chrome.ChromeDriver driver = Browser.Create();
    private void Open(string file) => driver.Navigate().GoToUrl(new Uri(Path.Combine(AppContext.BaseDirectory,"Fixtures",file)).AbsoluteUri);
    [Theory]
    [InlineData("baseline.html")]
    [InlineData("live-pattern.html")]
    [InlineData("mutated.html")]
    [InlineData("accessible.html")]
    [InlineData("delayed.html")]
    public void Fills_and_verifies_across_dom_variants(string fixture)
    {
        Open(fixture);
        new PracticeForm(driver,output.WriteLine,TimeSpan.FromSeconds(3)).Fill("Ada","Male","India");
        var finder = new SemanticDiscovery(driver,output.WriteLine);
        Assert.Equal("Ada",finder.Find(new("text","First Name")).GetDomProperty("value"));
        finder.Scope = (IWebElement?)((IJavaScriptExecutor)driver).ExecuteScript("return arguments[0].closest('form');", finder.Find(new("text","First Name")));
        Assert.True(finder.Find(new("radio","Male")).Selected);
        Assert.Equal("India",new OpenQA.Selenium.Support.UI.SelectElement(finder.Find(new("select","State"))).SelectedOption.Text);
        if(fixture=="mutated.html")
        {
            Assert.Equal("untouched",driver.FindElements(By.TagName("input")).Single(e=>e.GetDomAttribute("aria-label")=="First Name confirmation").GetDomProperty("value"));
        }
    }
    [Fact]
    public void Duplicate_semantic_names_fail_without_mutation()
    {
        Open("ambiguous.html");
        Assert.Throws<AmbiguousElementException>(()=>new SemanticDiscovery(driver,output.WriteLine).Find(new("text","First Name")));
        Assert.All(driver.FindElements(By.TagName("input")),e=>Assert.Equal("",e.GetDomProperty("value")));
    }
    [Fact]
    public void Missing_target_times_out_with_diagnostics()
    {
        Open("baseline.html");
        var error=Assert.Throws<WebDriverTimeoutException>(()=>new SemanticDiscovery(driver,output.WriteLine,TimeSpan.FromMilliseconds(400)).Find(new("text","Unknown")));
        Assert.Contains("Unknown",error.Message);
    }
    [Fact]
    public void Missing_option_fails_explicitly()
    {
        Open("baseline.html");
        Assert.Throws<InvalidOperationException>(()=>new PracticeForm(driver,output.WriteLine).Fill("Ada","Male","Atlantis"));
    }
    [Fact]
    public void Equal_radio_names_in_different_groups_require_context()
    {
        driver.Navigate().GoToUrl("data:text/html,<fieldset><legend>Gender</legend><label>Male<input type=radio></label></fieldset><fieldset><legend>Other</legend><label>Male<input type=radio></label></fieldset>");
        var finder=new SemanticDiscovery(driver,output.WriteLine);
        Assert.Throws<AmbiguousElementException>(()=>finder.Find(new("radio","Male")));
        Assert.NotNull(finder.Find(new("radio","Male","Gender")));
    }
    public void Dispose()=>driver.Dispose();
}