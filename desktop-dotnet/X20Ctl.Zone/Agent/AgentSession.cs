using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using X20Ctl.Zone.PcMode;
namespace X20Ctl.Zone.Agent;

/// <summary>One reply from the language model, with the counts and times it reported (never estimated).</summary>
public sealed record ModelReply(string Content, int PromptTokens, int OutputTokens, double LoadMs, double TotalMs);
public sealed record ChatMessage(string Role, string Content);

/// <summary>The language model, as the agent sees it: messages and a JSON schema in, one reply out.</summary>
public interface IAgentModel
{
    string Name { get; }
    Task<ModelReply> Chat(IReadOnlyList<ChatMessage> messages, string schema, CancellationToken cancel);
}

/// <summary>One sentence handled end to end: every model attempt (a malformed first one is kept, not hidden), the verdict
/// on the last, and the outcome it leads to.</summary>
public sealed record AgentTurn(string Sentence, IReadOnlyList<ModelReply> Attempts, AgentVerdict Verdict, AgentOutcome Outcome)
{
    public ModelReply Reply => Attempts[^1];
}

/// <summary>
/// The agent's side of a conversation: the system prompt (the controller's voice and its action list), the schema the
/// model's output is held to, and one turn at a time through validation. It never stores anything: the caller decides
/// what to do with an outcome.
/// </summary>
public sealed class AgentSession(IAgentModel model, string modelId, string controllerName)
{
    readonly List<ChatMessage> history = new();
    const int Remembered = 6;
    public string SystemPromptOverride { get; set; } = "";

    public async Task<AgentTurn> Ask(string sentence, PcModeProfile current, CancellationToken cancel = default)
    {
        var messages = new List<ChatMessage> { new("system", SystemPrompt(current)) };
        messages.AddRange(history.TakeLast(Remembered));
        messages.Add(new("user", sentence));
        var attempts = new List<ModelReply> { await model.Chat(messages, Schema, cancel) };
        var verdict = ProposalValidator.Check(attempts[0].Content, modelId);
        // one second chance: the model sees exactly what the validator objected to and answers again. Both attempts are
        // kept and shown; if the second is still wrong, it stands, and nothing changes
        if (!verdict.Valid)
        {
            messages.Add(new("assistant", attempts[0].Content));
            messages.Add(new("user", "The app rejected that reply, so nothing changed: " + Problems(verdict) + " Answer again with only the JSON object. If what I asked isn't possible, say so with no actions."));
            attempts.Add(await model.Chat(messages, Schema, cancel));
            verdict = ProposalValidator.Check(attempts[1].Content, modelId);
        }
        var outcome = AgentApplier.Apply(current, verdict);
        history.Add(new("user", sentence));
        history.Add(new("assistant", verdict.Proposal?.Speech ?? "(no usable reply)"));
        return new(sentence, attempts, verdict, outcome);
    }

    public void Forget() => history.Clear();

    /// <summary>The validator's objections in one line, for the model's second attempt and for the app's own message.</summary>
    public static string Problems(AgentVerdict verdict) => verdict.StructureError ?? string.Join(" ", verdict.Checks.Where(c => c.State == CheckState.Rejected).Select(c => $"{c.Action.Action} on {c.Action.Target}: {c.Message}"));

    public string SystemPrompt(PcModeProfile current) => (SystemPromptOverride.Length > 0 ? SystemPromptOverride : DefaultPrompt(controllerName, modelId)) + "\n\nMy settings right now:\n" + Describe(current);

    public static string DefaultPrompt(string controllerName, string modelId)
    {
        string paddles = AgentVocabulary.Paddles(modelId) is { Length: > 0 } p ? string.Join(", ", p) : "none";
        return $"""
            You are {controllerName}, a game controller, talking to your owner inside the X20CTL app. Speak as the controller, in first person, friendly and short (one or two sentences). Say what you will change ("I'll…"), never that it is already done: the app applies changes after checking them.

            Reply with one JSON object: "speech" (what you say), "actions" (the changes, empty when nothing should change), "assumptions" (anything you guessed, such as which button a game uses to jump).

            Actions (action, target, value):
            - stick_deadzone: LeftStick or RightStick, percent 0-40. How much of the centre is ignored. Drift or a stick that moves by itself: raise it a little.
            - stick_full_at: LeftStick or RightStick, percent 60-100. Where full speed is reached. Lower is faster.
            - stick_anti_deadzone: LeftStick or RightStick, percent 0-40. Cancels a game's own dead zone so small moves register.
            - stick_curve: LeftStick or RightStick, one of: default, quick, slow, smooth, fine.
            - trigger_deadzone: LeftTrigger or RightTrigger, whole percent 0-40. How far the trigger travels before it registers.
            - trigger_full_at: LeftTrigger or RightTrigger, whole percent 10-100. Where it counts as fully pressed. Lower is a faster, hair trigger.
            - trigger_curve: LeftTrigger or RightTrigger, one of: default, quick, slow, smooth, fine.
            - remap: a button (A, B, X, Y, LB, RB, LT, RT, L3, R3, DPAD_UP, DPAD_DOWN, DPAD_LEFT, DPAD_RIGHT, START, SELECT, HOME), value: the button it should send.
            - turbo: a button, presses per second 2-30, or 0 to turn turbo off.
            - reset: a stick, trigger or button, or All, value 0. Back to default.
            - paddle: a paddle, value A or B. My paddles: {paddles}. Paddles only take this action, and only send A or B. Jump is A in most games.

            What people mean: smooth -> stick_curve smooth. Fast or responsive sticks -> stick_curve quick, or stick_full_at about 80. Precise, slow, sniping -> stick_curve slow or fine. Fast triggers -> trigger_full_at 30-50 and trigger_deadzone 0. Slow, progressive triggers -> trigger_curve slow.
            Numbers are plain whole numbers (6, not "6%"). Use each action and target once.
            If the owner asks for a number outside its range, don't pick another number for them: tell them the range, with no actions.
            If the owner asks for something that isn't in this list, say you can't do that yet, with no actions.
            """;
    }

    /// <summary>The current settings in a few lines, so "a bit faster" has something to be faster than.</summary>
    public static string Describe(PcModeProfile p)
    {
        var lines = AgentApplier.Diff(new PcModeProfile(), p).Select(c => $"- {c.What}: {c.After}").ToList();
        return lines.Count == 0 ? "- Everything is at its default." : string.Join("\n", lines);
    }

    /// <summary>The JSON Schema the model's output is constrained to (Ollama's "format"), built from the vocabulary so the
    /// two can't drift apart.</summary>
    public static readonly string Schema = BuildSchema();
    static string BuildSchema()
    {
        static JsonArray Enum(IEnumerable<string> values) => new(values.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray());
        var action = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["action"] = new JsonObject { ["type"] = "string", ["enum"] = Enum(AgentVocabulary.Actions) },
                ["target"] = new JsonObject { ["type"] = "string", ["enum"] = Enum(AgentVocabulary.Targets) },
                // whole numbers only: with "number", grammar-constrained sampling wanders into values like 30.000000122 (live run, 12 Oct)
                ["value"] = new JsonObject { ["type"] = new JsonArray("integer", "string") },
                ["reason"] = new JsonObject { ["type"] = "string" },
            },
            ["required"] = new JsonArray("action", "target", "value", "reason"),
            ["additionalProperties"] = false,
        };
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["speech"] = new JsonObject { ["type"] = "string" },
                ["actions"] = new JsonObject { ["type"] = "array", ["items"] = action, ["maxItems"] = AgentVocabulary.MaxActions },
                ["assumptions"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } },
            },
            ["required"] = new JsonArray("speech", "actions", "assumptions"),
            ["additionalProperties"] = false,
        };
        return root.ToJsonString();
    }

    /// <summary>A short line of what a turn cost: real token counts and times as the model reported them.</summary>
    public static string Stats(AgentTurn turn)
    {
        var r = turn.Reply; string s = string.Format(CultureInfo.InvariantCulture, "{0} in · {1} out · {2:0.0} s", r.PromptTokens, r.OutputTokens, r.TotalMs / 1000);
        if (r.LoadMs > 500) s += string.Format(CultureInfo.InvariantCulture, " (loading {0:0.0} s)", r.LoadMs / 1000);
        return turn.Attempts.Count > 1 ? s + " · after a second attempt" : s;
    }
}
