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
    const int Rounds = 3, PerRound = 25;
    // what a sentence for an intent has to contain, so the slots the agent lifts out are there
    static string Needs(string id) => id switch
    {
        "paddle_set" => "Every sentence names a rear paddle, M1 to M6.",
        "remap_swap" or "remap_to" => "Every sentence names two buttons (A, B, X, Y, LB, RB, the D-pad, Start, View).",
        "turbo_on" or "turbo_off" => "Every sentence names one button.",
        "reset_target" => "Every sentence names one stick, trigger or button.",
        "reset_all" => "No sentence names a single stick, trigger or button.",
        "stick_deadzone_set" or "stick_full_at_set" or "trigger_deadzone_set" or "trigger_full_at_set" => "Every sentence has a number.",
        _ when id.StartsWith("stick_") => "Talk about sticks, aim or the camera; never triggers, buttons or paddles.",
        _ when id.StartsWith("trigger_") => "Talk about triggers, shooting, throttle or brakes; never sticks, buttons or paddles.",
        _ => "",
    };
    // the requests closest to this one, which its sentences must not drift into
    static IEnumerable<OfflineIntent> Neighbours(OfflineIntent intent) =>
        OfflineAgent.Intents.Where(i => i != intent && i.Id.Split('_')[0] == intent.Id.Split('_')[0]);
    const string Schema = """{"type":"object","properties":{"sentences":{"type":"array","items":{"type":"string"}}},"required":["sentences"],"additionalProperties":false}""";

    public static async Task Run(string file, IReadOnlySet<string> heldOut)
    {
        string progress = file + ".progress.txt";
        void Note(string line) => File.AppendAllText(progress, $"{DateTime.Now:HH:mm:ss} {line}\n");
        File.WriteAllText(progress, "");
        var model = new OllamaClient { Temperature = 0.9, MaxTokens = 2048 };
        if (await model.Prepare((_, text) => Note(text)) != ModelState.Ready) { Note("stopped: model not ready"); return; }
        static string Key(string s) => new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        var book = new SortedDictionary<string, string[]>();
        foreach (var intent in OfflineAgent.Intents)
        {
            var taken = intent.Seeds.Select(Key).Concat(heldOut.Select(Key)).ToHashSet();
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
                    var reply = await model.Chat(messages, Schema, CancellationToken.None);
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
        File.WriteAllText(file + ".raw.json", JsonSerializer.Serialize(book, options) + "\n", new UTF8Encoding(false));
        var (cleaned, dropped) = PhrasebookCleaner.Clean(book);
        File.WriteAllText(file + ".dropped.json", JsonSerializer.Serialize(dropped, options) + "\n", new UTF8Encoding(false));
        Note($"cleaned: {cleaned.Values.Sum(v => v.Length)} kept, {dropped.Count} dropped");
        File.WriteAllText(file, JsonSerializer.Serialize(new SortedDictionary<string, string[]>(cleaned), options) + "\n", new UTF8Encoding(false));
        Note($"done · {book.Values.Sum(v => v.Length)} sentences");
    }
}

/// <summary>--agent-phrasebook-clean REPO: both phrasebooks Qwen wrote (docs/agent/phrasebook-raw.json and
/// phrasebook-v2-raw.json), each cleaned, merged into the one the agent embeds; what was dropped and why goes beside them.</summary>
internal static class AgentPhrasebookClean
{
    public static void Run(string repo)
    {
        string docs = Path.Combine(repo, "docs", "agent");
        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var merged = new SortedDictionary<string, List<string>>(); var dropped = new List<object>();
        foreach (var version in new[] { "phrasebook-raw.json", "phrasebook-v2-raw.json" })
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(Path.Combine(docs, version)))!;
            var (kept, gone) = PhrasebookCleaner.Clean(raw);
            foreach (var (intent, sentences) in kept) (merged.TryGetValue(intent, out var list) ? list : merged[intent] = new()).AddRange(sentences);
            dropped.AddRange(gone.Select(d => new { from = version, d.Intent, d.Sentence, d.Why }));
        }
        var book = merged.ToDictionary(e => e.Key, e => e.Value.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        File.WriteAllText(Path.Combine(repo, "desktop-dotnet", "X20Ctl.Zone", "Agent", "phrasebook.json"), JsonSerializer.Serialize(new SortedDictionary<string, string[]>(book), options) + "\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(docs, "phrasebook-dropped.json"), JsonSerializer.Serialize(dropped, options) + "\n", new UTF8Encoding(false));
    }
}
