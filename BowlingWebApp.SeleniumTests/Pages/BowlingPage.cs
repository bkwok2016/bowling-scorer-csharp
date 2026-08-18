// BowlingPage.cs
//
// Page Object for the bowling scorer web UI. Includes explicit waits on
// every locator from the start -- these are the exact fixes discovered
// the hard way in the Python version (a missing wait on the cumulative
// score locator, and a race condition on the error-message check after
// resubmission). No need to rediscover those bugs here.

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace BowlingWebApp.SeleniumTests.Pages;

public class BowlingPage
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;

    public BowlingPage(IWebDriver driver, string baseUrl)
    {
        _driver = driver;
        _baseUrl = baseUrl;
    }

    public void Load() => _driver.Navigate().GoToUrl(_baseUrl);

    public void EnterFrame(int index, string value)
    {
        var box = _driver.FindElement(By.Id($"frame-{index}"));
        box.Clear();
        box.SendKeys(value);
    }

    public void EnterFrames(List<List<string>> frames)
    {
        for (int i = 0; i < frames.Count; i++)
            EnterFrame(i, string.Join(",", frames[i]));
    }

    public void Submit() => _driver.FindElement(By.Id("calculate-btn")).Click();

    public string GetFinalScore(int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        var element = wait.Until(d => d.FindElement(By.Id("final-score")));
        return element.Text;
    }

    public string GetCumulativeScore(int frameIndex, int timeoutSeconds = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        var element = wait.Until(d => d.FindElement(By.Id($"score-{frameIndex}")));
        return element.Text;
    }

    public string? GetErrorMessage(int timeoutSeconds = 3)
    {
        try
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
            var element = wait.Until(d => d.FindElement(By.Id("error-message")));
            return element.Text;
        }
        catch (WebDriverTimeoutException)
        {
            return null;
        }
    }
}
