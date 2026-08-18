// BowlingGame.cs
//
// A direct C# port of the Python bowling-scorer logic (already verified
// against 37 pytest cases in the original project). Same algorithm,
// same validation rules -- just translated syntax.

namespace BowlingScorer;

public class InvalidGameException : Exception
{
    public InvalidGameException(string message) : base(message) { }
}

public static class BowlingGameScorer
{
    private const int StrikePins = 10;
    private const int NumFrames = 10;

    private static int ParseRoll(string symbol, int? previousInFrame)
    {
        if (string.IsNullOrEmpty(symbol))
            throw new InvalidGameException($"Invalid roll symbol: '{symbol}'");

        if (symbol.ToUpperInvariant() == "X")
            return StrikePins;

        if (symbol == "/")
        {
            if (previousInFrame is null)
                throw new InvalidGameException("A spare ('/') cannot be the first roll of a frame");
            return StrikePins - previousInFrame.Value;
        }

        if (symbol.Length == 1 && char.IsDigit(symbol[0]))
            return int.Parse(symbol);

        throw new InvalidGameException($"Invalid roll symbol: '{symbol}'");
    }

    private static void ValidateFrame1To9(int frameNum, List<string> symbols, List<int> values)
    {
        if (symbols.Count == 1)
        {
            if (values[0] != StrikePins)
                throw new InvalidGameException($"Frame {frameNum}: a single-roll frame must be a strike");
        }
        else if (symbols.Count == 2)
        {
            int first = values[0], second = values[1];
            if (first == StrikePins)
                throw new InvalidGameException($"Frame {frameNum}: a strike frame cannot have a second roll");
            if (symbols[1] != "/" && (first + second) > StrikePins)
                throw new InvalidGameException($"Frame {frameNum}: total pins exceed 10 without a spare");
        }
        else
        {
            throw new InvalidGameException(
                $"Frame {frameNum}: must have 1 roll (strike) or 2 rolls, got {symbols.Count}");
        }
    }

    private static void ValidateFrame10(List<string> symbols, List<int> values)
    {
        int n = symbols.Count;
        if (n < 2 || n > 3)
            throw new InvalidGameException($"Frame 10: must have 2 or 3 rolls, got {n}");

        int first = values[0];
        bool isStrikeStart = first == StrikePins;
        bool isSparePair = !isStrikeStart && n >= 2 && (values[0] + values[1] == StrikePins);

        if (isStrikeStart)
        {
            if (n != 3)
                throw new InvalidGameException(
                    "Frame 10: a strike on the first roll must be followed by exactly two bonus rolls");

            string secondSym = symbols[1], thirdSym = symbols[2];
            if (secondSym == "/")
                throw new InvalidGameException("Frame 10: a spare cannot follow a strike with no roll between");

            if (secondSym.ToUpperInvariant() != "X" && thirdSym != "/")
            {
                int secondVal = values[1], thirdVal = values[2];
                if (secondVal + thirdVal > StrikePins)
                    throw new InvalidGameException(
                        "Frame 10: bonus rolls after the strike exceed 10 pins without a spare");
            }
        }
        else if (isSparePair)
        {
            if (symbols[1] != "/")
                throw new InvalidGameException(
                    "Frame 10: two rolls totaling 10 must use '/' notation for the second roll");
            if (n != 3)
                throw new InvalidGameException(
                    "Frame 10: a spare in the first two rolls must be followed by exactly one bonus roll");
        }
        else
        {
            if (n != 2)
                throw new InvalidGameException("Frame 10: no bonus roll is allowed after an open frame");
            if (values[0] + values[1] > StrikePins)
                throw new InvalidGameException("Frame 10: total pins exceed 10 without a spare");
        }
    }

    private static (List<int> flatValues, List<int> frameStarts) ValidateAndFlatten(List<List<string>> frames)
    {
        if (frames is null || frames.Count != NumFrames)
            throw new InvalidGameException($"A game must have exactly {NumFrames} frames");

        var flatValues = new List<int>();
        var frameStarts = new List<int>();

        for (int idx = 0; idx < frames.Count; idx++)
        {
            int frameNum = idx + 1;
            var symbols = frames[idx];

            if (symbols is null || symbols.Count == 0)
                throw new InvalidGameException($"Frame {frameNum}: must be a non-empty list of rolls");

            frameStarts.Add(flatValues.Count);

            var values = new List<int>();
            for (int rollIdx = 0; rollIdx < symbols.Count; rollIdx++)
            {
                int? previous = rollIdx > 0 ? values[rollIdx - 1] : null;
                values.Add(ParseRoll(symbols[rollIdx], previous));
            }

            if (frameNum < NumFrames)
                ValidateFrame1To9(frameNum, symbols, values);
            else
                ValidateFrame10(symbols, values);

            flatValues.AddRange(values);
        }

        return (flatValues, frameStarts);
    }

    /// <summary>
    /// Scores a complete, valid ten-pin bowling game.
    /// </summary>
    /// <param name="frames">Exactly 10 frames, each a list of roll symbols ("X", "/", or "0"-"9").</param>
    /// <returns>10 cumulative scores, one per frame.</returns>
    /// <exception cref="InvalidGameException">If the input does not represent a legal complete game.</exception>
    public static List<int> ScoreGame(List<List<string>> frames)
    {
        var (flatValues, frameStarts) = ValidateAndFlatten(frames);

        var cumulativeScores = new List<int>();
        int runningTotal = 0;

        for (int frameIdx = 0; frameIdx < NumFrames; frameIdx++)
        {
            int start = frameStarts[frameIdx];
            int first = flatValues[start];
            bool isLastFrame = frameIdx == NumFrames - 1;

            int frameScore;
            if (isLastFrame)
            {
                frameScore = 0;
                for (int i = start; i < flatValues.Count; i++)
                    frameScore += flatValues[i];
            }
            else if (first == StrikePins)
            {
                frameScore = StrikePins + flatValues[start + 1] + flatValues[start + 2];
            }
            else if (first + flatValues[start + 1] == StrikePins)
            {
                frameScore = StrikePins + flatValues[start + 2];
            }
            else
            {
                frameScore = first + flatValues[start + 1];
            }

            runningTotal += frameScore;
            cumulativeScores.Add(runningTotal);
        }

        return cumulativeScores;
    }
}

/// <summary>Thin object-oriented convenience wrapper around ScoreGame().</summary>
public class BowlingGame
{
    private readonly List<List<string>> _frames;

    public BowlingGame(List<List<string>> frames)
    {
        _frames = frames;
    }

    public List<int> GetScores() => BowlingGameScorer.ScoreGame(_frames);

    public int GetTotalScore() => GetScores()[^1];
}
