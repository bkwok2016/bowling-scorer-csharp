// UiTests.cs
//
// Selenium UI tests for the C# bowling scorer web app. Requires the web
// app (BowlingWebApp) to already be running at BASE_URL before these run.
// See README.md for exactly how to run this locally and in CI.

using BowlingScorer;
using BowlingWebApp.SeleniumTests.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace BowlingWebApp.SeleniumTests;

public class UiTests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;

    private static readonly List<List<string>> ExampleGame = new()
    {
        new() { "8", "/" }, new() { "5", "4" }, new() { "9", "0" }, new() { "X" }, new() { "X" },
        new() { "5", "/" }, new() { "5", "3" }, new() { "6", "3" }, new() { "9", "/" }, new() { "9", "/", "X" },
    };

    private static readonly List<List<string>> PerfectGame = BuildPerfectGame();
    private static readonly List<List<string>> AllOpenGame = BuildRepeatedFrame(new List<string> { "4", "3" }, 10);

    private static List<List<string>> BuildPerfectGame()
    {
        var frames = Enumerable.Range(0, 9).Select(_ => new List<string> { "X" }).ToList();
        frames.Add(new List<string> { "X", "X", "X" });
        return frames;
    }

    private static List<List<string>> BuildRepeatedFrame(List<string> frame, int count) =>
        Enumerable.Range(0, count).Select(_ => new List<string>(frame)).ToList();

    public UiTests()
    {
        var options = new ChromeOptions();
        var headlessEnv = Environment.GetEnvironmentVariable("HEADLESS") ?? "true";
        if (headlessEnv.Equals("true", StringComparison.OrdinalIgnoreCase))
            options.AddArgument("--headless=new");

        options.AddArgument("--window-size=1280,900");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        // Selenium 4.6+ ships with Selenium Manager, which auto-downloads
        // the matching chromedriver -- no manual driver setup needed.
        _driver = new ChromeDriver(options);
        _baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:5000";
    }

    public void Dispose() => _driver.Quit();

    // -------------------------------------------------------------------
    // Valid games -> correct score displayed
    // -------------------------------------------------------------------

    [Fact]
    public void ValidGame_ExampleGame_DisplaysCorrectTotal()
    {
        var page = new BowlingPage(_driver, _baseUrl);
        page.Load();
        page.EnterFrames(ExampleGame);
        page.Submit();

        var expectedTotal = BowlingGameScorer.ScoreGame(ExampleGame)[^1];
        Assert.Equal(expectedTotal.ToString(), page.GetFinalScore());
    }

    [Fact]
    public void ValidGame_PerfectGame_DisplaysCorrectTotal()
    {
        var page = new BowlingPage(_driver, _baseUrl);
        page.Load();
        page.EnterFrames(PerfectGame);
        page.Submit();

        var expectedTotal = BowlingGameScorer.ScoreGame(PerfectGame)[^1];
        Assert.Equal(expectedTotal.ToString(), page.GetFinalScore());
    }

    [Fact]
    public void ValidGame_AllOpenFrames_DisplaysCorrectTotal()
    {
        var page = new BowlingPage(_driver, _baseUrl);
        page.Load();
        page.EnterFrames(AllOpenGame);
        page.Submit();

        var expectedTotal = BowlingGameScorer.ScoreGame(AllOpenGame)[^1];
        Assert.Equal(expectedTotal.ToString(), page.GetFinalScore());
    }

    [Fact]
    public void ValidGame_DisplaysCorrectCumulativeScores()
    {
        var page = new BowlingPage(_driver, _baseUrl);
        page.Load();
        page.EnterFrames(ExampleGame);
        page.Submit();

        var expectedScores = BowlingGameScorer.ScoreGame(ExampleGame);
        for (int i = 0; i < expectedScores.Count; i++)
            Assert.Equal(expectedScores[i].ToString(), page.GetCumulativeScore(i));
    }

    // -------------------------------------------------------------------
    // Invalid input -> error message displayed, no results shown
    // -------------------------------------------------------------------

    [Fact]
    public void InvalidSymbol_ShowsErrorMessage()
    {
        var page = new BowlingPage(_driver, _baseUrl);
        page.Load();

        var frames = new List<List<string>> { new() { "Z", "3" } };
        frames.AddRange(Enumerable.Range(0, 9).Select(_ => new List<string> { "0", "0" }));
        page.EnterFrames(frames);
        page.Submit();

        var error = page.GetErrorMessage();
        Assert.NotNull(error);
        Assert.Contains("Invalid roll symbol", error);
    }

    [Fact]
    public void SpareAsFirstRoll_ShowsErrorMessage()
    {
        var page = new BowlingPage(_driver, _baseUrl);
        page.Load();

        var frames = new List<List<string>> { new() { "/", "5" } };
        frames.AddRange(Enumerable.Range(0, 9).Select(_ => new List<string> { "3", "4" }));
        page.EnterFrames(frames);
        page.Submit();

        var error = page.GetErrorMessage();
        Assert.NotNull(error);
        Assert.Contains("spare", error.ToLowerInvariant());
    }

    [Fact]
    public void EmptyFrame_ShowsErrorMessage()
    {
        var page = new BowlingPage(_driver, _baseUrl);
        page.Load();

        page.EnterFrame(0, "");
        for (int i = 1; i < 10; i++)
            page.EnterFrame(i, "3,4");
        page.Submit();

        var error = page.GetErrorMessage();
        Assert.NotNull(error);
        Assert.Contains("empty", error.ToLowerInvariant());
    }

    [Fact]
    public void TenthFrameMissingBonusRoll_ShowsErrorMessage()
    {
        var page = new BowlingPage(_driver, _baseUrl);
        page.Load();

        var frames = Enumerable.Range(0, 9).Select(_ => new List<string> { "3", "4" }).ToList();
        frames.Add(new List<string> { "X" }); // strike with no bonus rolls
        page.EnterFrames(frames);
        page.Submit();

        var error = page.GetErrorMessage();
        Assert.NotNull(error);
        Assert.Contains("must have 2 or 3 rolls", error.ToLowerInvariant());
    }

    // -------------------------------------------------------------------
    // Re-submission behavior
    // -------------------------------------------------------------------

    [Fact]
    public void ResubmittingCorrectedGame_ClearsPreviousError()
    {
        var page = new BowlingPage(_driver, _baseUrl);
        page.Load();

        // First submit something invalid
        var invalidFrames = new List<List<string>> { new() { "Z", "3" } };
        invalidFrames.AddRange(Enumerable.Range(0, 9).Select(_ => new List<string> { "0", "0" }));
        page.EnterFrames(invalidFrames);
        page.Submit();
        Assert.NotNull(page.GetErrorMessage());

        // Now correct frame 1 and resubmit the rest of a valid game
        page.EnterFrames(AllOpenGame);
        page.Submit();

        // Wait for the new page's success indicator FIRST -- this confirms
        // the browser has actually finished navigating before checking that
        // the old error message is gone. Checking absence too early can
        // false-positive-match the stale previous page (a real bug found
        // and fixed the hard way in the Python version of this project).
        var expectedTotal = BowlingGameScorer.ScoreGame(AllOpenGame)[^1];
        Assert.Equal(expectedTotal.ToString(), page.GetFinalScore());
        Assert.Null(page.GetErrorMessage());
    }
}
