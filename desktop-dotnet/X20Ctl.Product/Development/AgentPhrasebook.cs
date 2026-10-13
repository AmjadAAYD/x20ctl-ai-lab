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
    const int Rounds = 2, PerRound = 30;
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
        File.WriteAllText(file, JsonSerializer.Serialize(book, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n", new UTF8Encoding(false));
        Note($"done · {book.Values.Sum(v => v.Length)} sentences");
    }
}
