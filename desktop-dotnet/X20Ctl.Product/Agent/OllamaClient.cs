using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using X20Ctl.Zone.Agent;
namespace X20Ctl.Product.Agent;

public enum ModelState { Starting, Loading, Ready, NotInstalled, Missing, Failed }

/// <summary>
/// The only file that knows about Ollama: the local model server on this PC. It starts the server quietly when it isn't
/// running, checks the model is there, and sends chats with a JSON schema and thinking switched off. Nothing leaves the PC.
/// </summary>
public sealed class OllamaClient : IAgentModel
{
    public const string DefaultModel = "qwen3.5:9b";
    static readonly Uri Server = new("http://127.0.0.1:11434/");
    static readonly HttpClient http = new() { BaseAddress = Server, Timeout = TimeSpan.FromMinutes(4) };
    public string Name { get; }
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 600;
    public int Context { get; set; } = 4096;
    /// <summary>Hold replies to the JSON schema (Ollama's "format"). Off only to measure what the schema is worth.</summary>
    public bool UseSchema { get; set; } = true;
    public OllamaClient(string model = DefaultModel) { Name = model; }

    /// <summary>Gets the server up and the model loaded, reporting each step. Returns the final state.</summary>
    public async Task<ModelState> Prepare(Action<ModelState, string> report, CancellationToken cancel = default)
    {
        if (!await Running(cancel))
        {
            if (Executable() is not { } exe) { report(ModelState.NotInstalled, "Ollama isn't installed"); return ModelState.NotInstalled; }
            report(ModelState.Starting, "Starting Ollama…");
            try { Process.Start(new ProcessStartInfo(exe, "serve") { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden }); }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException) { report(ModelState.Failed, "Ollama couldn't start"); return ModelState.Failed; }
            for (int i = 0; i < 40 && !await Running(cancel); i++) await Task.Delay(250, cancel);
            if (!await Running(cancel)) { report(ModelState.Failed, "Ollama didn't start"); return ModelState.Failed; }
        }
        if (!await Installed(cancel)) { report(ModelState.Missing, $"{Name} isn't downloaded · ollama pull {Name}"); return ModelState.Missing; }
        report(ModelState.Loading, $"Loading {Name}…");
        try
        {
            // an empty chat loads the model into memory and keeps it there, so the first real answer is quick
            using var warm = await http.PostAsync("api/chat", Json(new JsonObject { ["model"] = Name, ["messages"] = new JsonArray(), ["keep_alive"] = "30m" }), cancel);
            warm.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException) { report(ModelState.Failed, $"{Name} couldn't load"); return ModelState.Failed; }
        report(ModelState.Ready, $"{Name} ready");
        return ModelState.Ready;
    }

    public async Task<ModelReply> Chat(IReadOnlyList<ChatMessage> messages, string schema, CancellationToken cancel)
    {
        var body = new JsonObject
        {
            ["model"] = Name, ["stream"] = false, ["think"] = false, ["keep_alive"] = "30m",
            ["options"] = new JsonObject { ["temperature"] = Temperature, ["num_predict"] = MaxTokens, ["num_ctx"] = Context },
            ["messages"] = new JsonArray(messages.Select(m => (JsonNode)new JsonObject { ["role"] = m.Role, ["content"] = m.Content }).ToArray()),
        };
        if (UseSchema) body["format"] = JsonNode.Parse(schema);
        using var response = await http.PostAsync("api/chat", Json(body), cancel);
        string text = await response.Content.ReadAsStringAsync(cancel);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException(Error(text) ?? $"Ollama answered {(int)response.StatusCode}");
        using var reply = JsonDocument.Parse(text);
        var r = reply.RootElement;
        static long N(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.TryGetInt64(out long n) ? n : 0;
        string content = r.GetProperty("message").GetProperty("content").GetString() ?? "";
        return new(content, (int)N(r, "prompt_eval_count"), (int)N(r, "eval_count"), N(r, "load_duration") / 1e6, N(r, "total_duration") / 1e6);
    }

    static async Task<bool> Running(CancellationToken cancel)
    {
        try { using var r = await http.GetAsync("api/version", new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token); return r.IsSuccessStatusCode; }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { cancel.ThrowIfCancellationRequested(); return false; }
    }

    async Task<bool> Installed(CancellationToken cancel)
    {
        using var tags = JsonDocument.Parse(await http.GetStringAsync("api/tags", cancel));
        string wanted = Name.Contains(':') ? Name : Name + ":latest";
        return tags.RootElement.GetProperty("models").EnumerateArray().Any(m => m.GetProperty("name").GetString() == wanted);
    }

    /// <summary>ollama.exe where the Windows installer puts it, or on PATH.</summary>
    static string? Executable()
    {
        string local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe");
        if (File.Exists(local)) return local;
        return (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator).Select(d => Path.Combine(d.Trim(), "ollama.exe")).FirstOrDefault(File.Exists);
    }

    static StringContent Json(JsonNode body) => new(body.ToJsonString(), Encoding.UTF8, "application/json");
    static string? Error(string text) { try { return JsonNode.Parse(text)?["error"]?.GetValue<string>(); } catch (JsonException) { return null; } }
}
