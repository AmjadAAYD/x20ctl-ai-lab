using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using X20Ctl.Product.Agent;
using X20Ctl.Zone.Agent;
namespace X20Ctl.Product.Development;

/// <summary>
/// --agent-phrasebook FILE: offline mode's "training". For every intent the offline agent knows, Qwen is given what it
/// means and the hand-written examples, and writes many more ways an owner might say it. The sentences are kept as
/// written, apart from duplicates, empty ones, and any that repeat a seed or the held-out test sentences (so the
/// test stays honest). FILE is the phrasebook the Zone project embeds. Runs once, ahead of time; offline mode never
/// calls the model.
/// </summary>
internal static class AgentPhrasebook
{
    static int Rounds => AgentBench.EvaluationRepeats;
    const int PerRound = 25;
    // what a sentence for an intent has to contain, so the slots the agent lifts out are there
    static string Needs(string id) => id switch
    {
        "paddle_set" => "Every sentence names a rear paddle, M1 to M6.",
        "remap_swap" or "remap_to" => "Every sentence names two buttons (A, B, X, Y, LB, RB, the D-pad, Start, View).",
        "turbo_on" or "turbo_off" => "Every sentence names one button.",
        "reset_target" => "Every sentence names one stick, trigger or button.",
        "reset_all" => "No sentence names a single stick, trigger or button.",
        "stick_deadzone_set" or "stick_full_at_set" or "stick_max_output_set" or "stick_axis_ratio_set" or "trigger_deadzone_set" or "trigger_full_at_set" => "Every sentence has a number.",
        "led_color_set" => "Every sentence asks for RGB/LED/controller lights with a colour name, tint, shade or hex code.",
        "led_effect_set" => "Every sentence asks for a lighting effect (rainbow, breathe, cycle, moonlight pulse, hue sweep, strobe, lava shimmer, trigger glow, stick heat, button flash, or off).",
        "mouse_mode_on" or "mouse_mode_off" => "Every sentence explicitly mentions mouse mode or controlling the mouse cursor.",
        "open_page" => "Every sentence names an app page: Diagnostics, Tools, Mouse, Curves, Buttons, Macros, Vibration, PcMode, or Device.",
        _ when id.StartsWith("stick_") => "Talk about sticks, aim or the camera; never triggers, buttons or paddles.",
        _ when id.StartsWith("trigger_") => "Talk about triggers, shooting, throttle or brakes; never sticks, buttons or paddles.",
        _ => "",
    };
    // the requests closest to this one, which its sentences must not drift into
    static IEnumerable<OfflineIntent> Neighbours(OfflineIntent intent) =>
        OfflineAgent.Intents.Where(i => i != intent && i.Id.Split('_')[0] == intent.Id.Split('_')[0]);
    const string Schema = """{"type":"object","properties":{"sentences":{"type":"array","items":{"type":"string"}}},"required":["sentences"],"additionalProperties":false}""";

    public static async Task Run(string file, IReadOnlySet<string> heldOut, string? mergeFrom = null)
    {
        string sidecarBase = file;
        string zoneTail = Path.Combine("desktop-dotnet", "X20Ctl.Zone", "Agent", "phrasebook.json");
        if (Path.GetFullPath(file).EndsWith(zoneTail, StringComparison.OrdinalIgnoreCase))
        {
            string repoRoot = Path.GetFullPath(file)[..^zoneTail.Length];
            string docsDir = Path.Combine(repoRoot, "docs", "agent");
            if (Directory.Exists(docsDir)) sidecarBase = Path.Combine(docsDir, "phrasebook");
        }
        string progress = sidecarBase + ".progress.txt";
        void Note(string line) => File.AppendAllText(progress, $"{DateTime.Now:HH:mm:ss} {line}\n");
        File.WriteAllText(progress, "");
        static string Key(string s) => new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        var heldKeys = heldOut.Select(Key).ToHashSet();

        Dictionary<string, string[]>? existing = null;
        if (mergeFrom != null && File.Exists(mergeFrom))
        {
            existing = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(mergeFrom));
            Note($"loaded {existing?.Count ?? 0} existing intents from {mergeFrom}");
        }

        var missing = OfflineAgent.Intents.Where(i => existing == null || !existing.TryGetValue(i.Id, out var list) || list.Length == 0).ToList();
        OllamaClient? model = null;
        if (missing.Count > 0)
        {
            model = new OllamaClient { Temperature = 0.9, MaxTokens = 2048 };
            if (await model.Prepare((_, text) => Note(text)) != ModelState.Ready) { Note("stopped: model not ready"); return; }
        }

        var book = new SortedDictionary<string, string[]>();
        foreach (var intent in OfflineAgent.Intents)
        {
            if (existing != null && existing.TryGetValue(intent.Id, out var oldSentences) && oldSentences.Length > 0)
            {
                var seen = intent.Seeds.Select(Key).Concat(heldKeys).ToHashSet();
                var keptOld = new List<string>();
                foreach (var s in oldSentences.Select(s => s.Trim()).Where(s => s.Length is > 1 and < 160))
                    if (seen.Add(Key(s))) keptOld.Add(s);
                book[intent.Id] = keptOld.ToArray();
                Note($"{intent.Id}: preserved {keptOld.Count} existing sentences");
                continue;
            }
            var taken = intent.Seeds.Select(Key).Concat(heldKeys).ToHashSet();
            var kept = new List<string>();
            for (int round = 1; round <= Rounds; round++)
            {
                var messages = new List<ChatMessage>
                {
                    new("system", "You write example sentences for training a game controller app's assistant. Reply with JSON only."),
                    new("user", $"""
                        What the owner wants: {intent.Meaning}.
                        Examples: {string.Join(" | ", intent.Seeds)}
                        Write {PerRound} different things a gamer might type to the assistant that mean exactly this and nothing else.
                        {Needs(intent.Id)} Requests only, never questions.
                        These are different requests, so no sentence may mean any of them: {string.Join(" | ", Neighbours(intent).Select(i => i.Meaning))}
                        Vary them: short and long, casual and polite, slang, the odd typo, games named, with and without numbers.
                        Where the examples name a stick, trigger, button, paddle or number, use other ones. Don't repeat the examples.
                        {(round > 1 ? "Different wording from these: " + string.Join(" | ", kept.Take(20)) : "")}
                        """),
                };
                try
                {
                    var reply = await model!.Chat(messages, Schema, CancellationToken.None);
                    using var doc = JsonDocument.Parse(reply.Content);
                    foreach (var s in doc.RootElement.GetProperty("sentences").EnumerateArray().Select(e => (e.GetString() ?? "").Trim()))
                        if (s.Length is > 1 and < 160 && taken.Add(Key(s))) kept.Add(s);
                    Note($"{intent.Id} round {round}: {kept.Count} kept · {reply.TotalMs / 1000:0.0} s");
                }
                catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or System.Net.Http.HttpRequestException or TaskCanceledException) { Note($"{intent.Id} round {round}: {error.Message}"); }
            }
            book[intent.Id] = kept.ToArray();
        }
        // as Qwen wrote it, then cleaned: the agent embeds the cleaned one; both, and what was dropped, are kept
        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        File.WriteAllText(sidecarBase + ".raw.json", JsonSerializer.Serialize(book, options) + "\n", new UTF8Encoding(false));
        var (cleaned, dropped) = PhrasebookCleaner.Clean(book);
        File.WriteAllText(sidecarBase + ".dropped.json", JsonSerializer.Serialize(dropped, options) + "\n", new UTF8Encoding(false));
        Note($"cleaned: {cleaned.Values.Sum(v => v.Length)} kept, {dropped.Count} dropped");
        File.WriteAllText(file, JsonSerializer.Serialize(new SortedDictionary<string, string[]>(cleaned), options) + "\n", new UTF8Encoding(false));
        Note($"done · {cleaned.Values.Sum(v => v.Length)} sentences across {cleaned.Count} intents");
    }
}

/// <summary>--agent-phrasebook-clean REPO: cleans docs/agent/phrasebook-raw.json and phrasebook-v2-raw.json if present,
/// or cleans desktop-dotnet/X20Ctl.Zone/Agent/phrasebook.json directly when raw exports are absent.</summary>
internal static class AgentPhrasebookClean
{
    public static void Run(string repo)
    {
        string docs = Path.Combine(repo, "docs", "agent");
        string phrasebookPath = Path.Combine(repo, "desktop-dotnet", "X20Ctl.Zone", "Agent", "phrasebook.json");
        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var merged = new SortedDictionary<string, List<string>>(); var dropped = new List<object>();
        var rawFiles = new[] { "phrasebook-raw.json", "phrasebook-v2-raw.json" }
            .Select(name => (Name: name, Path: Path.Combine(docs, name)))
            .Where(f => File.Exists(f.Path))
            .ToArray();
        if (rawFiles.Length > 0)
        {
            foreach (var f in rawFiles)
            {
                var raw = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(f.Path))!;
                var (kept, gone) = PhrasebookCleaner.Clean(raw);
                foreach (var (intent, sentences) in kept) (merged.TryGetValue(intent, out var list) ? list : merged[intent] = new()).AddRange(sentences);
                dropped.AddRange(gone.Select(d => new { from = f.Name, d.Intent, d.Sentence, d.Why }));
            }
        }
        else if (File.Exists(phrasebookPath))
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(phrasebookPath))!;
            var (kept, gone) = PhrasebookCleaner.Clean(raw);
            foreach (var (intent, sentences) in kept) merged[intent] = sentences.ToList();
            dropped.AddRange(gone.Select(d => new { from = "phrasebook.json", d.Intent, d.Sentence, d.Why }));
        }
        var book = merged.ToDictionary(e => e.Key, e => e.Value.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        File.WriteAllText(phrasebookPath, JsonSerializer.Serialize(new SortedDictionary<string, string[]>(book), options) + "\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(docs, "phrasebook-dropped.json"), JsonSerializer.Serialize(dropped, options) + "\n", new UTF8Encoding(false));
    }
}
