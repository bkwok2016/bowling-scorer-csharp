using BowlingScorer;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BowlingWebApp.Pages;

public class IndexModel : PageModel
{
    public List<int>? Scores { get; set; }
    public int? Total { get; set; }
    public string? Error { get; set; }
    public string[] Values { get; set; } = new string[10];

    public void OnGet()
    {
        for (int i = 0; i < 10; i++)
            Values[i] = "";
    }

    public void OnPost()
    {
        var rawValues = new string[10];
        var frames = new List<List<string>>();

        try
        {
            for (int i = 0; i < 10; i++)
            {
                var raw = Request.Form[$"frame-{i}"].ToString().Trim();
                rawValues[i] = raw;

                if (string.IsNullOrEmpty(raw))
                    throw new InvalidGameException($"Frame {i + 1} is empty");

                var rolls = raw.Split(',')
                    .Select(r => r.Trim())
                    .Where(r => r.Length > 0)
                    .ToList();

                if (rolls.Count == 0)
                    throw new InvalidGameException($"Frame {i + 1} is empty");

                frames.Add(rolls);
            }

            Values = rawValues;
            Scores = BowlingGameScorer.ScoreGame(frames);
            Total = Scores[^1];
        }
        catch (InvalidGameException e)
        {
            Values = rawValues;
            Error = e.Message;
        }
    }
}
