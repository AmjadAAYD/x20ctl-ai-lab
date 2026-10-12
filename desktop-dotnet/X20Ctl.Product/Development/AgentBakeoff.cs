using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using X20Ctl.Product.Agent;
using X20Ctl.Zone.Agent;
using X20Ctl.Zone.PcMode;
namespace X20Ctl.Product.Development;

/// <summary>
/// --agent-bakeoff DIRECTORY: the agent's evaluation against the real local model (class project, M2). Every sentence in
/// <see cref="AgentBench.Cases"/>, three times, under each variant, each in a fresh conversation with default settings.
/// Writes report.md (the tables), runs.jsonl (every turn with the model's first output) and progress.txt as it goes.
/// Nothing is applied anywhere: the outcomes are only scored.
/// </summary>
internal static class AgentBakeoff
{
    const int Repeats = 3;

    public static async Task Run(string directory)
    {
        Directory.CreateDirectory(directory);
        string progress = Path.Combine(directory, "progress.txt");
        File.WriteAllText(progress, "");
        void Note(string line) => File.AppendAllText(progress, $"{DateTime.Now:HH:mm:ss} {line}\n");
        var model = new OllamaClient();
        var state = await model.Prepare((_, text) => Note(text));
        if (state != ModelState.Ready) { Note("stopped: model not ready"); return; }

        var runs = new List<BenchRun>();
        using var jsonl = new StreamWriter(Path.Combine(directory, "runs.jsonl"), false, new UTF8Encoding(false));
        int total = AgentBench.Variants.Length * Repeats * AgentBench.Cases.Length, done = 0;
        foreach (var variant in AgentBench.Variants)
        {
            model.Temperature = variant.Temperature; model.UseSchema = variant.Schema;
            for (int run = 1; run <= Repeats; run++)
                foreach (var c in AgentBench.Cases)
                {
                    var session = new AgentSession(model, c.ModelId, c.ModelId == "x20" ? "X20" : "X20 Pro") { SchemaText = variant.Bounded ? AgentSession.BoundedSchema : AgentSession.Schema };
                    BenchRun result;
                    try { result = BenchRun.Of(variant, c, run, await session.Ask(c.Sentence, new PcModeProfile())); }
                    catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { Note($"{variant.Name} · {c.Sentence}: {error.Message}"); continue; }
                    runs.Add(result); done++;
                    jsonl.WriteLine(JsonSerializer.Serialize(new
                    {
                        variant = variant.Name, sentence = c.Sentence, pad = c.ModelId, run, expected = c.Expected,
                        result.FirstJson, result.FirstStructure, result.FirstRules, result.FinalRules, result.Correct, result.Attempts,
                        ms = Math.Round(result.TotalMs), tokensOut = result.OutputTokens, tokensIn = result.PromptTokens, first = result.FirstOutput,
                    }));
                    jsonl.Flush();
                    Note($"{done}/{total} {variant.Name} · run {run} · {c.Sentence} · {(result.Correct ? "right" : "wrong")} · {result.TotalMs / 1000:0.0} s");
                }
        }
        File.WriteAllText(Path.Combine(directory, "report.md"), AgentBench.Report(runs, model.Name, Repeats));
        Note("done");
    }
}
