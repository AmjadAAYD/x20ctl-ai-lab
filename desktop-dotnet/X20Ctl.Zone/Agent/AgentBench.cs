using System.Globalization;
using System.Text;
using System.Text.Json;
namespace X20Ctl.Zone.Agent;

/// <summary>
/// One sentence the agent is measured on, and what a right answer contains. Each group in <see cref="Needs"/> must be met by
/// at least one action; a group lists its alternatives as "action:target" or "action:target=value". No groups means the
/// right answer is to decline: a valid reply with no actions.
/// </summary>
public sealed record BenchCase(string Sentence, string ModelId, params string[][] Needs)
{
    public bool Decline => Needs.Length == 0;
    /// <summary>A question ("What…", "Why…"): right means no actions and an answer in the recap.</summary>
    public bool Question => Decline && (Sentence.StartsWith("What") || Sentence.StartsWith("Why"));
    public string Expected => Question ? "answer, no actions" : Decline ? "decline" : string.Join(" + ", Needs.Select(g => string.Join(" or ", g)));
}

/// <summary>How a variant of the model's settings is run: temperature, and whether the reply is held to the JSON schema.</summary>
/// <summary>The bench's drift reading: no controller, the same fixed numbers every run (right stick wandering to 6.4%, left
/// to 4.2%), so a drift sentence is scored on whether the dead zone covers what was measured.</summary>
public sealed class BenchMeter : IDriftMeter
{
    public static readonly DriftReport Reading = new([new("LeftStick", 1.8, 0.9, 4.2, 3000), new("RightStick", 3.1, -2.4, 6.4, 3000)], null);
    public Task<DriftReport> Measure(IReadOnlyList<string> sticks, CancellationToken cancel) =>
        Task.FromResult(new DriftReport(Reading.Sticks.Where(d => sticks.Contains(d.Stick)).ToList(), null));
}

public sealed record BenchVariant(string Name, double Temperature, bool Schema, bool Bounded = false);

/// <summary>One sentence, one run: what the first attempt was worth, and where the whole turn (with its repair) ended.</summary>
public sealed record BenchRun(BenchVariant Variant, BenchCase Case, int Run, bool FirstJson, bool FirstStructure, bool FirstRules, bool FinalRules, bool Correct, int Attempts, double TotalMs, int OutputTokens, int PromptTokens, string FirstOutput)
{
    public static BenchRun Of(BenchVariant variant, BenchCase c, int run, AgentTurn turn)
    {
        var first = turn.Attempts[0];
        var firstVerdict = ProposalValidator.Check(first.Content, c.ModelId);
        return new(variant, c, run, AgentBench.IsJson(first.Content), firstVerdict.StructureError == null, firstVerdict.Valid, turn.Verdict.Valid, AgentBench.Matches(c, turn.Verdict) && (!AgentBench.DriftCases.Contains(c) || turn.Measured is { Problem: null }),
            turn.Attempts.Count, turn.Attempts.Sum(a => a.TotalMs), turn.Attempts.Sum(a => a.OutputTokens), turn.Attempts.Sum(a => a.PromptTokens), first.Content);
    }
}

/// <summary>
/// The agent's evaluation (class project, M2): a fixed set of sentences, each with its right answer, run through the real
/// session (prompt, model, validator, one repair) under each variant. The scoring is the app's own validator plus the
/// expected actions; nothing is judged by another model.
/// </summary>
public static class AgentBench
{
    /// <summary>The drift sentences (M3), scored with <see cref="BenchMeter"/>: right means it measured first and the dead zone
    /// covers what was measured (the validator refuses less), on the stick the sentence names, or both when it names neither.</summary>
    public static readonly BenchCase[] DriftCases =
    [
        new("My right stick drifts a little", "x20_pro", ["stick_deadzone:RightStick"]),
        new("My left stick moves on its own", "x20_pro", ["stick_deadzone:LeftStick"]),
        new("Fix my stick drift", "x20_pro", ["stick_deadzone:LeftStick"], ["stick_deadzone:RightStick"]),
        new("My camera keeps slowly turning by itself", "x20_pro", ["stick_deadzone:RightStick"]),
        new("Check my sticks for drift", "x20_pro", ["stick_deadzone:LeftStick"], ["stick_deadzone:RightStick"]),
    ];

    public static readonly BenchCase[] Cases =
    [
        new("Make my sticks smooth", "x20_pro", ["stick_curve:LeftStick=smooth"], ["stick_curve:RightStick=smooth"]),
        new("Make my right stick more precise for sniping", "x20_pro", ["stick_curve:RightStick=slow"]),
        new("My aim feels too twitchy", "x20_pro", ["stick_curve:RightStick=slow", "stick_curve:RightStick=smooth"]),
        new("My right stick drifts a little", "x20_pro", ["stick_deadzone:RightStick"]),
        new("My left stick moves on its own", "x20_pro", ["stick_deadzone:LeftStick"]),
        new("Set my left stick dead zone to 8", "x20_pro", ["stick_deadzone:LeftStick=8"]),
        new("Right stick full speed at 85", "x20_pro", ["stick_full_at:RightStick=85"]),
        new("Small stick movements don't register in my game", "x20_pro", ["stick_anti_deadzone:LeftStick", "stick_anti_deadzone:RightStick"]),
        new("I want faster triggers for shooting", "x20_pro", ["trigger_full_at:RightTrigger", "trigger_deadzone:RightTrigger", "trigger_curve:RightTrigger=quick"]),
        new("Hair triggers for Call of Duty", "x20_pro", ["trigger_full_at:RightTrigger", "trigger_deadzone:RightTrigger"]),
        new("Slow, progressive triggers for racing", "x20_pro", ["trigger_curve:RightTrigger=slow", "trigger_curve:LeftTrigger=slow"]),
        new("Remove the dead zone on both triggers", "x20_pro", ["trigger_deadzone:LeftTrigger=0"], ["trigger_deadzone:RightTrigger=0"]),
        new("Turbo on X, 12 presses a second", "x20_pro", ["turbo:X=12"]),
        new("Turn off turbo on X", "x20_pro", ["turbo:X=0", "reset:X"]),
        new("Swap A and B", "x20_pro", ["remap:A=B"], ["remap:B=A"]),
        new("Make LB act like RB", "x20_pro", ["remap:LB=RB"]),
        new("Put jump on M1", "x20_pro", ["paddle:M1=A"]),
        new("Put jump on M5", "x20_pro", ["paddle:M5=A"]),
        new("Reset my right trigger", "x20_pro", ["reset:RightTrigger"]),
        new("Reset everything", "x20_pro", ["reset:All"]),
        new("Put jump on M5", "x20"),
        new("Set my left stick dead zone to 260", "x20_pro"),
        new("Turbo on RB at 50 a second", "x20_pro"),
        new("Can you change my RGB to red?", "x20_pro"),
        new("Make the vibration stronger", "x20_pro"),
        new("What's good and bad about hair triggers?", "x20_pro"),
        new("Why would I use an anti-dead zone?", "x20_pro"),
    ];

    public static readonly BenchVariant[] Variants =
    [
        new("T 0.2 · schema", 0.2, true),
        new("T 0.8 · schema", 0.8, true),
        new("T 0.2 · bounded schema", 0.2, true, true),
        new("T 0.8 · bounded schema", 0.8, true, true),
        new("T 0.2 · no schema", 0.2, false),
    ];

    public static bool IsJson(string text)
    {
        try { using var d = JsonDocument.Parse(text); return d.RootElement.ValueKind == JsonValueKind.Object; }
        catch (JsonException) { return false; }
    }

    /// <summary>Whether the reply did what the sentence asked: valid, and every group met (or, for a decline, no actions).</summary>
    public static bool Matches(BenchCase c, AgentVerdict verdict)
    {
        if (!verdict.Valid || verdict.Proposal == null) return false;
        var actions = verdict.Proposal.Actions;
        if (c.Decline) return actions.Count == 0 && (!c.Question || verdict.Proposal.Recap.Trim().Length > 0);
        return c.Needs.All(group => group.Any(need => actions.Any(a => Fits(need, a))));
        static bool Fits(string need, AgentAction a)
        {
            var (head, value) = need.Split('=') is [var h, var v] ? (h, v) : (need, null);
            return head == a.Action + ":" + a.Target && (value == null || string.Equals(value, a.ValueText, StringComparison.OrdinalIgnoreCase));
        }
    }

    static double Median(IEnumerable<double> values)
    {
        var v = values.OrderBy(x => x).ToArray();
        return v.Length == 0 ? 0 : v.Length % 2 == 1 ? v[v.Length / 2] : (v[v.Length / 2 - 1] + v[v.Length / 2]) / 2;
    }

    /// <summary>The variant to make the default: most right answers, then most valid first attempts, then the quickest.</summary>
    public static BenchVariant Best(IReadOnlyList<BenchRun> runs) =>
        runs.GroupBy(r => r.Variant).OrderByDescending(g => g.Count(r => r.Correct)).ThenByDescending(g => g.Count(r => r.FirstRules)).ThenBy(g => Median(g.Select(r => r.TotalMs))).First().Key;

    /// <summary>The results as markdown: one row per variant, then each sentence's right answers per variant.</summary>
    public static string Report(IReadOnlyList<BenchRun> runs, string model, int repeats)
    {
        var c = CultureInfo.InvariantCulture;
        var variants = runs.Select(r => r.Variant).Distinct().ToList();
        string Pct(IEnumerable<BenchRun> g, Func<BenchRun, bool> f) { var a = g.ToList(); return a.Count == 0 ? "-" : $"{100.0 * a.Count(f) / a.Count:0}%"; }
        var s = new StringBuilder();
        s.AppendLine($"# Agent evaluation · {model}");
        s.AppendLine();
        s.AppendLine($"{runs.Select(r => r.Case).Distinct().Count()} sentences × {repeats} runs × {variants.Count} variants = {runs.Count} turns, run {DateTime.Now.ToString("d MMM yyyy HH:mm", c)}. Each turn goes through the app's real prompt, validator and one repair attempt. \"First\" columns score the model's first reply alone; \"Right answer\" scores the whole turn against what the sentence asked for.");
        s.AppendLine();
        s.AppendLine("| Variant | First: JSON | First: schema | First: rules | Repaired | Valid after repair | Right answer | Median time | Median tokens out |");
        s.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var v in variants)
        {
            var g = runs.Where(r => r.Variant == v).ToList();
            s.AppendLine($"| {v.Name} | {Pct(g, r => r.FirstJson)} | {Pct(g, r => r.FirstStructure)} | {Pct(g, r => r.FirstRules)} | {Pct(g, r => r.Attempts > 1)} | {Pct(g, r => r.FinalRules)} | **{Pct(g, r => r.Correct)}** | {(Median(g.Select(r => r.TotalMs)) / 1000).ToString("0.0", c)} s | {Median(g.Select(r => (double)r.OutputTokens)).ToString("0", c)} |");
        }
        var best = Best(runs);
        s.AppendLine();
        s.AppendLine($"**Top score: {best.Name}** (most right answers, then most valid first replies, then quickest). A one-turn lead is within run-to-run noise; read the failures before choosing a default.");
        s.AppendLine();
        s.AppendLine("## Right answers per sentence");
        s.AppendLine();
        s.AppendLine("| Sentence | Pad | Expected | " + string.Join(" | ", variants.Select(v => v.Name)) + " |");
        s.AppendLine("|---|---|---|" + string.Concat(variants.Select(_ => "---|")));
        foreach (var bc in runs.Select(r => r.Case).Distinct())
            s.AppendLine($"| {bc.Sentence} | {bc.ModelId} | `{bc.Expected}` | " + string.Join(" | ", variants.Select(v => { var g = runs.Where(r => r.Case == bc && r.Variant == v).ToList(); return $"{g.Count(r => r.Correct)}/{g.Count}"; })) + " |");
        return s.ToString();
    }
}
