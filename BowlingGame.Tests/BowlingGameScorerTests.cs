// BowlingGameScorerTests.cs
//
// xUnit port of the Python test suite, covering the same 10 categories.
// Not a literal 1:1 count of all 37 Python tests, but every category has
// direct representation, including the two corrected assertions (all-spares
// = 150 for 5/ frames, complex mixed game = 168) verified previously.

using BowlingScorer;
using Xunit;

namespace BowlingGame.Tests;

public class BowlingGameScorerTests
{
    // =========================================================================
    // 1. Basic Valid Game Scenarios
    // =========================================================================

    [Fact]
    public void AllGutters_ReturnsZero()
    {
        var frames = Enumerable.Range(0, 10).Select(_ => new List<string> { "0", "0" }).ToList();
        Assert.Equal(0, BowlingGameScorer.ScoreGame(frames)[^1]);
    }

    [Fact]
    public void AllOpenFrames_ReturnsSeventy()
    {
        var frames = Enumerable.Range(0, 10).Select(_ => new List<string> { "3", "4" }).ToList();
        Assert.Equal(70, BowlingGameScorer.ScoreGame(frames)[^1]);
    }

    [Fact]
    public void ExampleGameFromPrompt_ReturnsExpectedScores()
    {
        var frames = new List<List<string>>
        {
            new() { "8", "/" }, new() { "5", "4" }, new() { "9", "0" }, new() { "X" }, new() { "X" },
            new() { "5", "/" }, new() { "5", "3" }, new() { "6", "3" }, new() { "9", "/" }, new() { "9", "/", "X" },
        };
        var expected = new List<int> { 15, 24, 33, 58, 78, 93, 101, 110, 129, 149 };
        Assert.Equal(expected, BowlingGameScorer.ScoreGame(frames));
    }

    // =========================================================================
    // 2. Strike/Spare Behavior (Frames 1-9)
    // =========================================================================

    [Fact]
    public void SingleStrike_FollowedByOpenFrame()
    {
        var frames = new List<List<string>> { new() { "X" }, new() { "3", "4" } };
        frames.AddRange(Enumerable.Range(0, 8).Select(_ => new List<string> { "0", "0" }));

        var scores = BowlingGameScorer.ScoreGame(frames);
        Assert.Equal(17, scores[0]);
        Assert.Equal(24, scores[1]);
    }

    [Fact]
    public void SingleSpare_FollowedByOpenFrame()
    {
        var frames = new List<List<string>> { new() { "7", "/" }, new() { "3", "4" } };
        frames.AddRange(Enumerable.Range(0, 8).Select(_ => new List<string> { "0", "0" }));

        var scores = BowlingGameScorer.ScoreGame(frames);
        Assert.Equal(13, scores[0]);
        Assert.Equal(20, scores[1]);
    }

    // =========================================================================
    // 3. Invalid Frame Structures
    // =========================================================================

    [Fact]
    public void ThreeRollsInNormalFrame_Throws()
    {
        var frames = new List<List<string>> { new() { "3", "4", "2" } };
        frames.AddRange(Enumerable.Range(0, 9).Select(_ => new List<string> { "0", "0" }));
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    [Fact]
    public void StrikeWithTwoRolls_Throws()
    {
        var frames = new List<List<string>> { new() { "X", "3" } };
        frames.AddRange(Enumerable.Range(0, 9).Select(_ => new List<string> { "0", "0" }));
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    [Fact]
    public void SpareAsFirstRoll_Throws()
    {
        var frames = new List<List<string>> { new() { "/", "5" } };
        frames.AddRange(Enumerable.Range(0, 9).Select(_ => new List<string> { "3", "4" }));
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    [Fact]
    public void WrongNumberOfFrames_Throws()
    {
        var frames = Enumerable.Range(0, 9).Select(_ => new List<string> { "3", "4" }).ToList();
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    [Fact]
    public void ExtraFrameAfterCompletion_Throws()
    {
        var frames = Enumerable.Range(0, 11).Select(_ => new List<string> { "3", "4" }).ToList();
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    [Fact]
    public void FrameGivenAsNullList_Throws()
    {
        var frames = new List<List<string>> { null! };
        frames.AddRange(Enumerable.Range(0, 9).Select(_ => new List<string> { "0", "0" }));
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    // =========================================================================
    // 4. Invalid Pin Counts
    // =========================================================================

    [Fact]
    public void PinCountExceeds10WithoutSpare_Throws()
    {
        var frames = new List<List<string>> { new() { "6", "6" } };
        frames.AddRange(Enumerable.Range(0, 9).Select(_ => new List<string> { "3", "4" }));
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    [Fact]
    public void NegativePinValue_Throws()
    {
        var frames = new List<List<string>> { new() { "-1", "5" } };
        frames.AddRange(Enumerable.Range(0, 9).Select(_ => new List<string> { "0", "0" }));
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    [Fact]
    public void InvalidSymbol_Throws()
    {
        var frames = new List<List<string>> { new() { "A", "3" } };
        frames.AddRange(Enumerable.Range(0, 9).Select(_ => new List<string> { "0", "0" }));
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    // =========================================================================
    // 5. 10th Frame Valid Patterns
    // =========================================================================

    [Theory]
    [MemberData(nameof(TenthFrameEndings))]
    public void VariousTenthFrameEndings(List<string> tenthFrame, int expectedBonus)
    {
        var frames = Enumerable.Range(0, 9).Select(_ => new List<string> { "3", "4" }).ToList();
        frames.Add(tenthFrame);

        var scores = BowlingGameScorer.ScoreGame(frames);
        Assert.Equal(scores[8] + expectedBonus, scores[9]);
    }

    public static IEnumerable<object[]> TenthFrameEndings()
    {
        yield return new object[] { new List<string> { "X", "X", "X" }, 30 };
        yield return new object[] { new List<string> { "X", "9", "/" }, 20 };
        yield return new object[] { new List<string> { "7", "/", "3" }, 13 };
        yield return new object[] { new List<string> { "0", "0" }, 0 };
    }

    // =========================================================================
    // 6. 10th Frame Invalid Patterns
    // =========================================================================

    [Fact]
    public void TenthFrameStrikeMissingBonusRolls_Throws()
    {
        var frames = Enumerable.Range(0, 9).Select(_ => new List<string> { "3", "4" }).ToList();
        frames.Add(new List<string> { "X" });
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    [Fact]
    public void TenthFrameSpareMissingBonusRoll_Throws()
    {
        var frames = Enumerable.Range(0, 9).Select(_ => new List<string> { "3", "4" }).ToList();
        frames.Add(new List<string> { "7", "/" });
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    [Fact]
    public void TenthFrameTooManyRolls_Throws()
    {
        var frames = Enumerable.Range(0, 9).Select(_ => new List<string> { "3", "4" }).ToList();
        frames.Add(new List<string> { "X", "X", "X", "X" });
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    [Fact]
    public void TenthFrameInvalidPinSum_Throws()
    {
        var frames = Enumerable.Range(0, 9).Select(_ => new List<string> { "3", "4" }).ToList();
        frames.Add(new List<string> { "9", "9" });
        Assert.Throws<InvalidGameException>(() => BowlingGameScorer.ScoreGame(frames));
    }

    // =========================================================================
    // 7. Perfect Game
    // =========================================================================

    [Fact]
    public void PerfectGame_Returns300()
    {
        var frames = Enumerable.Range(0, 9).Select(_ => new List<string> { "X" }).ToList();
        frames.Add(new List<string> { "X", "X", "X" });

        var scores = BowlingGameScorer.ScoreGame(frames);
        Assert.Equal(300, scores[^1]);
        Assert.Equal(new List<int> { 30, 60, 90, 120, 150, 180, 210, 240, 270, 300 }, scores);
    }

    // =========================================================================
    // 8. All Spares
    // =========================================================================

    [Fact]
    public void AllSpares5_Returns150()
    {
        var frames = Enumerable.Range(0, 9).Select(_ => new List<string> { "5", "/" }).ToList();
        frames.Add(new List<string> { "5", "/", "5" });
        Assert.Equal(150, BowlingGameScorer.ScoreGame(frames)[^1]);
    }

    // =========================================================================
    // 9. Mixed Complex Game (Regression Test)
    // =========================================================================

    [Fact]
    public void ComplexMixedGame_Returns168()
    {
        var frames = new List<List<string>>
        {
            new() { "X" },
            new() { "7", "/" },
            new() { "9", "0" },
            new() { "X" },
            new() { "0", "8" },
            new() { "8", "/" },
            new() { "X" },
            new() { "X" },
            new() { "7", "2" },
            new() { "X", "9", "0" },
        };
        Assert.Equal(168, BowlingGameScorer.ScoreGame(frames)[^1]);
    }

    // =========================================================================
    // 10. BowlingGame class API
    // =========================================================================

    [Fact]
    public void BowlingGameClass_GetScoresAndGetTotalScore()
    {
        var frames = new List<List<string>>
        {
            new() { "8", "/" }, new() { "5", "4" }, new() { "9", "0" }, new() { "X" }, new() { "X" },
            new() { "5", "/" }, new() { "5", "3" }, new() { "6", "3" }, new() { "9", "/" }, new() { "9", "/", "X" },
        };
        var game = new BowlingScorer.BowlingGame(frames);

        Assert.Equal(new List<int> { 15, 24, 33, 58, 78, 93, 101, 110, 129, 149 }, game.GetScores());
        Assert.Equal(149, game.GetTotalScore());
    }
}
