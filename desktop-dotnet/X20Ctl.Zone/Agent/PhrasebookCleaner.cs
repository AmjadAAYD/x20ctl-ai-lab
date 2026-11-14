using System.Text.RegularExpressions;
namespace X20Ctl.Zone.Agent;

/// <summary>A phrasebook sentence that was left out, and why.</summary>
public sealed record DroppedSentence(string Intent, string Sentence, string Why);

/// <summary>
/// Cleans the phrasebook (13 Oct 2026): some of its sentences belong to another intent ("Reset the right stick
/// to the quick profile" was filed under curves). Two passes, nothing hand-picked:
/// 1. Rules: a sentence must not name what contradicts its intent (a trigger in a stick request, a stick in a trigger
///    one), must name what its intent needs (a paddle, two buttons, a number), and must not be a question (questions are
///    answered from the meanings, never matched).
/// 2. Leave-one-out vote: each remaining sentence is matched against all the others; if its nearest examples vote
///    clearly for another intent (at least twice its own), it is left out.
/// </summary>
public static class PhrasebookCleaner
{
    public const double Margin = 2;

    static readonly HashSet<string> StickIntents = ["stick_sniping", "stick_twitchy", "stick_smooth", "stick_fast", "stick_curve_set", "stick_drift", "stick_deadzone_set", "stick_deadzone_remove", "stick_full_at_set", "stick_small_moves", "stick_max_output_set", "stick_axis_ratio_set", "stick_square_gate_on", "stick_square_gate_off"];
    static readonly HashSet<string> TriggerIntents = ["trigger_fast", "trigger_slow", "trigger_curve_set", "trigger_deadzone_set", "trigger_deadzone_remove", "trigger_full_at_set"];
    static readonly HashSet<string> NeedNumber = ["stick_deadzone_set", "stick_full_at_set", "stick_max_output_set", "stick_axis_ratio_set", "trigger_deadzone_set", "trigger_full_at_set"];

    /// <summary>Why a sentence breaks the rules for its intent, or null when it doesn't.</summary>
    public static string? Rule(string intent, string sentence)
    {
        var s = OfflineAgent.Slots(sentence);
        string lower = s.Text;
        bool namedStick = Regex.IsMatch(lower, @"\b(left|right|l|r)[\s-]*(analog|thumb ?stick|joystick|stick)s?\b|\b(ls|rs)\b|\b(sticks|analogs|joysticks|thumb ?sticks)\b");
        bool namedPaddle = s.Paddles.Count > 0 || Regex.IsMatch(lower, @"\bpaddles?\b");
        bool namedLighting = Regex.IsMatch(lower, @"\b(rgb|leds?|lights|lighting|backlights?|rainbow(?!\s*(?:six|6|r6))|strobe|moonlight)\b");
        if (intent != "greeting" && OfflineAgent.IsQuestion(lower)) return "a question";
        if (namedPaddle && intent != "paddle_set") return "names a paddle";
        if (namedLighting && !intent.StartsWith("led_", StringComparison.Ordinal)) return "names lighting";
        if (StickIntents.Contains(intent) && s.Triggers.Count > 0) return "names a trigger";
        if (TriggerIntents.Contains(intent) && namedStick) return "names a stick";
        if (TriggerIntents.Contains(intent) && Regex.IsMatch(lower, @"\bbuttons?\b")) return "names a button";
        if (NeedNumber.Contains(intent) && s.Numbers.Count == 0) return "has no number";
        if (intent.EndsWith("_remove") && s.Numbers.Any(n => n != 0)) return "names a number";
        return intent switch
        {
            "greeting" when Regex.IsMatch(lower, @"\b(sticks?|triggers?|buttons?|paddles?|d-pad|reset|game modes?|connect|sign in|log on|open dialogue|initiate action|showing you)\b") => "off-intent greeting noise",
            "paddle_set" when s.Paddles.Count == 0 => "names no paddle",
            "remap_to" when s.Buttons.Count < 2 => "names fewer than two buttons",
            "remap_swap" when s.Buttons.Count < 2 && !lower.Contains("bumpers") => "names fewer than two buttons",
            "reset_all" when s.Sticks.Count > 0 || s.Triggers.Count > 0 || s.Buttons.Count > 0 => "names one control",
            "reset_target" when s.Sticks.Count + s.Triggers.Count + s.Buttons.Count == 0 => "names no control",
            "turbo_on" when Regex.IsMatch(lower, @"\b(off|disable|stop|remove|no more)\b") => "turns turbo off",
            "pc_mode_on" when Regex.IsMatch(lower, @"\b(off|disable|stop)\b") => "turns PC mode off",
            "pc_mode_off" when Regex.IsMatch(lower, @"\b(enable|turn on|switch on)\b") => "turns PC mode on",
            "mouse_mode_on" when Regex.IsMatch(lower, @"\b(off|disable|stop)\b") => "turns mouse mode off",
            "mouse_mode_off" when Regex.IsMatch(lower, @"\b(enable|turn on|switch on)\b") => "turns mouse mode on",
            "stick_square_gate_on" when Regex.IsMatch(lower, @"\b(off|disable|stop)\b") => "turns square gate off",
            "stick_square_gate_off" when Regex.IsMatch(lower, @"\b(enable|turn on|switch on)\b") => "turns square gate on",
            "led_color_set" when AgentColors.Asked(sentence) == null => "names no colour",
            "led_effect_set" when AgentLightingEffects.Asked(sentence) == null => "names no lighting effect",
            _ => null,
        };
    }

    public static (Dictionary<string, string[]> Kept, List<DroppedSentence> Dropped) Clean(IReadOnlyDictionary<string, string[]> raw)
    {
        var dropped = new List<DroppedSentence>();
        var ruled = raw.ToDictionary(e => e.Key, e => e.Value.Where(sentence =>
        {
            if (Rule(e.Key, sentence) is not { } why) return true;
            dropped.Add(new(e.Key, sentence, why)); return false;
        }).ToArray());

        var examples = OfflineAgent.Examples(ruled);
        var index = new OfflineAgent.Index(examples);
        var noisy = new HashSet<int>();
        for (int row = 0; row < examples.Count; row++)
        {
            if (examples[row].Weight == OfflineAgent.SeedWeight) continue; // the hand-written examples are the reference
            var votes = index.VotesWithout(row);
            double own = votes.GetValueOrDefault(examples[row].Intent);
            if (votes.Count > 0 && votes.MaxBy(v => v.Value) is var best && best.Key != examples[row].Intent && best.Value >= Margin * own)
            {
                noisy.Add(row); dropped.Add(new(examples[row].Intent, examples[row].Sentence, $"closer to {best.Key}"));
            }
        }
        var kept = new Dictionary<string, List<string>>();
        for (int row = 0; row < examples.Count; row++)
            if (examples[row].Weight != OfflineAgent.SeedWeight && !noisy.Contains(row))
                (kept.TryGetValue(examples[row].Intent, out var list) ? list : kept[examples[row].Intent] = new()).Add(examples[row].Sentence);
        return (kept.ToDictionary(e => e.Key, e => e.Value.ToArray()), dropped);
    }
}
