namespace X20Ctl.Zone.Agent;

/// <summary>
/// Fast, deterministic Gemini / AI Studio-style inline ghost autocomplete for the agent chat bar.
/// Returns only the remaining suffix to render in grey after the user's current text (Tab to accept).
/// </summary>
public static class AgentAutocomplete
{
    static readonly string[] RGB_EFFECTS =
    [
        "ambient",
        "ambient mode",
        "battery mode",
        "moonlight pulse",
        "hue sweep",
        "palette jump",
        "multi-color fade",
        "rainbow",
        "breathe cyan",
        "lava ember",
        "dual strobe",
        "trigger glow",
        "stick heat",
        "button flash",
        "idle nightlight",
        "game auto",
        "cyberpunk fade",
        "off",
    ];

    static readonly (string Prefix, string Completion)[] PhraseCompletions =
    [
        // RGB / LEDs / Effects Studio
        ("change my rgb to ", "ambient"),
        ("change my rgb ", "to ambient"),
        ("change rgb to ", "ambient"),
        ("change rgb ", "to ambient"),
        ("change my led to ", "ambient"),
        ("change my leds to ", "ambient"),
        ("change my lights to ", "ambient"),
        ("change lights to ", "ambient"),
        ("set my rgb to ", "ambient"),
        ("set rgb to ", "ambient"),
        ("set my leds to ", "ambient"),
        ("set my lights to ", "ambient"),
        ("set lights to ", "ambient"),
        ("switch my rgb to ", "ambient"),
        ("switch lights to ", "ambient"),
        ("turn my rgb to ", "ambient"),
        ("turn my lights ", "to ambient"),
        ("enable ", "ambient mode"),
        ("turn on ", "ambient mode"),
        ("turn off ", "my lights"),

        // Sticks, curves & drift
        ("my right stick ", "drifts"),
        ("my left stick ", "drifts"),
        ("right stick ", "drifts"),
        ("left stick ", "drifts"),
        ("fix stick ", "drift"),
        ("make my aim ", "more precise"),
        ("make aim ", "more precise"),
        ("make my triggers ", "hair trigger"),
        ("make triggers ", "hair trigger"),

        // Buttons, remaps & modes
        ("remap ", "A to B"),
        ("swap ", "A and B"),
        ("open ", "diagnostics"),
        ("reset ", "everything"),
        ("how do i ", "use ambient mode?"),
        ("what is ", "ambient mode?"),
    ];

    static readonly string[] FullTemplates =
    [
        "Change my RGB to ambient",
        "Change my LEDs to moonlight pulse",
        "Set lights to battery mode",
        "Set lights to hue sweep",
        "Set lights to palette jump",
        "Set lights to trigger glow",
        "Set lights to stick heat",
        "Set lights to button flash",
        "Set lights to lava ember",
        "Set lights to dual strobe",
        "Set lights to game auto",
        "Turn off my lights",
        "Enable ambient mode",
        "Enable PC mode",
        "Enable mouse mode",
        "My right stick drifts",
        "Make my aim more precise",
        "Make my triggers hair trigger",
        "Open diagnostics",
        "Reset everything",
    ];

    public static string Suggest(string? typed)
    {
        if (string.IsNullOrWhiteSpace(typed) || typed.Length > 160 || typed.Contains('\n') || typed.Contains('\r'))
            return "";

        // 1. Exact phrase prefix match (e.g. "Change my RGB to " -> "ambient", "Change my RGB " -> "to ambient")
        foreach (var (prefix, completion) in PhraseCompletions)
        {
            if (typed.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                return completion;
        }

        // 2. Partial full-template match (e.g. "Change my R" -> "GB to ambient", "Enable a" -> "mbient mode")
        foreach (string template in FullTemplates)
        {
            if (template.Length > typed.Length && template.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
                return template[typed.Length..];
        }

        // 3. RGB / light command followed by partial effect name (e.g. "Change my RGB to amb" -> "ient", "set lights to moo" -> "nlight pulse")
        string? afterTo = TryAfterLightVerb(typed);
        if (afterTo != null)
        {
            if (afterTo.Length == 0) return "ambient";
            foreach (string effect in RGB_EFFECTS)
            {
                if (effect.Length > afterTo.Length && effect.StartsWith(afterTo, StringComparison.OrdinalIgnoreCase))
                    return effect[afterTo.Length..];
            }
        }

        // 4. General trailing partial-word match when user has typed >= 3 chars of a phrase prefix
        foreach (var (prefix, completion) in PhraseCompletions)
        {
            string full = prefix + completion;
            if (full.Length > typed.Length && full.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
                return full[typed.Length..];
        }

        return "";
    }

    static string? TryAfterLightVerb(string typed)
    {
        string lower = typed.ToLowerInvariant();
        string[] markers =
        [
            "rgb to ", "leds to ", "led to ", "lights to ", "lighting to ",
            "rgb ", "leds ", "lights ",
            "enable ", "turn on ", "switch to ",
        ];
        int bestEnd = -1;
        foreach (string m in markers)
        {
            int idx = lower.LastIndexOf(m, StringComparison.Ordinal);
            if (idx >= 0)
            {
                int end = idx + m.Length;
                if (end > bestEnd) bestEnd = end;
            }
        }
        if (bestEnd < 0) return null;
        string tail = typed[bestEnd..];
        // Avoid triggering on multi-clause sentences after the effect has already been typed
        if (tail.Contains(" and ", StringComparison.OrdinalIgnoreCase)) return null;
        return tail;
    }
}
