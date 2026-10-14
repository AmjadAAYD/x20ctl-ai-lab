using System.IO;
using System.Text;
using X20Ctl.Product.Agent;
using X20Ctl.Zone.Agent;
using X20Ctl.Zone.PcMode;
namespace X20Ctl.Product.Development;

/// <summary>
/// --agent-m5 DIRECTORY: M5's evaluation against the local models only. Retrieval first, by words and by meaning: is an
/// answering passage among the three offered, and how similar are the best passages for questions the docs answer and
/// for ones they don't (that gap sets the cut-off). Then Qwen answers every question three times with the passages
/// found by meaning, scored by <see cref="AgentDocsBench.Right"/>. Writes report.md and progress.txt.
/// </summary>
internal static class AgentDocsBakeoff
{
    static int Repeats => AgentBench.EvaluationRepeats;

    public static async Task Run(string directory)
    {
        Directory.CreateDirectory(directory);
        string progress = Path.Combine(directory, "progress.txt");
        File.WriteAllText(progress, "");
        void Note(string line) => File.AppendAllText(progress, $"{DateTime.Now:HH:mm:ss} {line}\n");
        var chunks = AgentDocs.Load(Path.Combine(AppContext.BaseDirectory, "Help"));
        var report = new StringBuilder($"# M5: answers from the docs\n\n{chunks.Count} passages from {string.Join(", ", AgentDocs.Files)}.\n\n## Retrieval\n\n| Method | Answering passage in the top 3 | Best score, docs answer it (lowest) | Best score, docs don't (highest) |\n|---|---|---|---|\n");
        var searches = new IDocSearch[] { new KeywordSearch(chunks), new EmbeddingSearch(chunks, new OllamaEmbedder(), Path.Combine(directory, "doc-embeddings.json")) };
        foreach (var search in searches)
        {
            int found = 0; double lowIn = 1, highOut = 0;
            foreach (var c in AgentDocsBench.Cases)
            {
                var hits = await search.Search(c.Question, AgentSession.DocCount, CancellationToken.None);
                if (c.OutOfDocs) highOut = Math.Max(highOut, hits[0].Score);
                else { lowIn = Math.Min(lowIn, hits[0].Score); if (c.Found(hits)) found++; else Note($"{search.Method} missed: {c.Question} -> {string.Join(" | ", hits.Select(h => $"{h.Chunk.Where} {h.Score:0.00}"))}"); }
                Note($"{search.Method} · {c.Question} · best {hits[0].Score:0.00} {hits[0].Chunk.Where}");
            }
            int inDocs = AgentDocsBench.Cases.Count(c => !c.OutOfDocs);
            report.AppendLine($"| {search.Method} | {found}/{inDocs} | {lowIn:0.00} | {highOut:0.00} |");
        }

        var model = new OllamaClient();
        if (await model.Prepare((_, text) => Note(text)) != ModelState.Ready) { Note("stopped: model not ready"); File.WriteAllText(Path.Combine(directory, "report.md"), report.ToString()); return; }
        report.AppendLine($"\n## Answers ({model.Name}, passages found by meaning, cut-off {searches[1].Enough:0.00}, {Repeats} runs)\n\n| Question | Right | Cited |\n|---|---|---|");
        int right = 0, total = 0;
        foreach (var c in AgentDocsBench.Cases)
        {
            int ok = 0; var cited = new List<string>();
            for (int run = 1; run <= Repeats; run++)
            {
                var session = new AgentSession(model, "x20_pro", "X20 Pro") { Docs = searches[1] };
                var turn = await session.Ask(c.Question, new PcModeProfile());
                bool good = AgentDocsBench.Right(c, turn); ok += good ? 1 : 0; right += good ? 1 : 0; total++;
                cited.AddRange(turn.Cited.Select(h => h.Chunk.Section));
                Note($"{c.Question} · run {run} · {(good ? "right" : "wrong")} · offered {turn.Docs.Count} · cited {string.Join(", ", turn.Cited.Select(h => h.Chunk.Section))} · {turn.Verdict.StructureError ?? ""} · {turn.Verdict.Proposal?.Recap.ReplaceLineEndings(" ")}");
            }
            report.AppendLine($"| {c.Question} | {ok}/{Repeats} | {string.Join(", ", cited.Distinct())} |");
        }
        report.AppendLine($"\n**{right}/{total} right.**");
        File.WriteAllText(Path.Combine(directory, "report.md"), report.ToString());
        Note("done");
    }
}
