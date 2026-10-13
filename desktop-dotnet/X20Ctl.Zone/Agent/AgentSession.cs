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
public sealed record AgentTurn(string Sentence, IReadOnlyList<ModelReply> Attempts, AgentVerdict Verdict, AgentOutcome Outcome, DriftReport? Measured = null, IReadOnlyList<DocHit>? Offered = null)
{
    /// <summary>The doc passages the model was given this turn, numbered from 1.</summary>
    public IReadOnlyList<DocHit> Docs => Offered ?? [];
    /// <summary>The passages the answer cites.</summary>
    public IEnumerable<DocHit> Cited => (Verdict.Proposal?.Sources ?? []).Where(n => n >= 1 && n <= Docs.Count).Select(n => Docs[n - 1]);
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
    // owner, 14 Oct 2026: a conversation remembers everything said and done in it, until a new one is started
    const int Remembered = 60;
    /// <summary>What this controller is, so a curve or a dead zone is chosen for it (sticks, triggers, motors, rate).</summary>
    public string ControllerFacts { get; set; } = "";
    public string SystemPromptOverride { get; set; } = "";
    /// <summary>Reads the real sticks when the model asks to measure drift. None: the model is told it can't measure.</summary>
    public IDriftMeter? Meter { get; set; }
    /// <summary>Finds the doc passages closest to the sentence (M5). None: no docs are offered.</summary>
    public IDocSearch? Docs { get; set; }
    public const int DocCount = 3;

    public async Task<AgentTurn> Ask(string sentence, PcModeProfile current, CancellationToken cancel = default)
    {
        var messages = new List<ChatMessage> { new("system", SystemPrompt(current)) };
        messages.AddRange(history.TakeLast(Remembered));
        // the docs: only passages close enough to be about this sentence, numbered for citing
        var offered = Docs == null ? [] : (await Docs.Search(sentence, DocCount, cancel)).Where(h => h.Score >= Docs.Enough).ToList();
        if (offered.Count > 0) messages.Add(new("system", AgentDocs.Prompt(offered)));
        messages.Add(new("user", sentence));
        var attempts = new List<ModelReply>();
        var verdict = await Answer(messages, attempts, null, offered.Count, cancel);
        // a reading first: the model asked to measure, so the app measures the real sticks and the model answers again
        // with the numbers. Once per turn; the dead zone it then picks is checked against what was measured
        DriftReport? measured = null;
        if (verdict.Valid && verdict.Proposal!.Actions.Where(a => a.Action == AgentVocabulary.MeasureDrift).Select(a => a.Target).Distinct().ToList() is { Count: > 0 } sticks)
        {
            measured = Meter == null ? new DriftReport([], "Measuring isn't available here: no controller is being read.") : await Meter.Measure(sticks, cancel);
            messages.Add(new("assistant", attempts[^1].Content));
            messages.Add(new("user", (measured.Problem == null ? "The app measured, with no hand on the sticks: " : "The app couldn't measure: ") + measured.Describe() +
                " Now answer with the changes (no measure_drift this time), and explain the numbers in the recap."));
            verdict = await Answer(messages, attempts, measured, offered.Count, cancel);
        }
        var outcome = AgentApplier.Apply(current, verdict);
        history.Add(new("user", sentence));
        // what was said, what the app really did, and what was asked back, so the next sentence is read in context
        string done = outcome.Changes.Count > 0 ? " [applied: " + string.Join("; ", outcome.Changes.Select(c => $"{c.What} {c.Before} -> {c.After}")) + "]"
            : verdict.Valid ? "" : " [rejected: nothing changed]";
        if (outcome.Mouse is { } mouse) done += $" [mouse mode {(mouse ? "on" : "off")}]";
        if (outcome.Page is { } page) done += $" [opened {page}]";
        history.Add(new("assistant", (verdict.Proposal?.Speech ?? "(no usable reply)") + done + (verdict.Proposal?.Question is { Length: > 0 } asked ? " " + asked : "")));
        return new(sentence, attempts, verdict, outcome, measured, offered);
    }

    /// <summary>One reply, checked; with one second chance: the model sees exactly what the validator objected to and
    /// answers again. Every attempt is kept and shown; if the second is still wrong, it stands, and nothing changes.</summary>
    async Task<AgentVerdict> Answer(List<ChatMessage> messages, List<ModelReply> attempts, DriftReport? measured, int offered, CancellationToken cancel)
    {
        attempts.Add(await model.Chat(messages, SchemaText, cancel));
        var verdict = ProposalValidator.Check(attempts[^1].Content, modelId, measured, offered);
        if (verdict.Valid) return verdict;
        messages.Add(new("assistant", attempts[^1].Content));
        messages.Add(new("user", "The app rejected that reply, so nothing changed: " + Problems(verdict) + " Answer again with only the JSON object. If what I asked isn't possible, say so with no actions."));
        attempts.Add(await model.Chat(messages, SchemaText, cancel));
        return ProposalValidator.Check(attempts[^1].Content, modelId, measured, offered);
    }

    public void Forget() => history.Clear();

    /// <summary>The validator's objections in one line, for the model's second attempt and for the app's own message.</summary>
    public static string Problems(AgentVerdict verdict) => verdict.StructureError ?? string.Join(" ", verdict.Checks.Where(c => c.State == CheckState.Rejected).Select(c => $"{c.Action.Action} on {c.Action.Target}: {c.Message}"));

    public string SystemPrompt(PcModeProfile current) => (SystemPromptOverride.Length > 0 ? SystemPromptOverride : DefaultPrompt(controllerName, modelId))
        + (ControllerFacts.Length > 0 ? "\n\nWhat I am (use it when choosing values):\n" + ControllerFacts : "") + "\n\nMy settings right now:\n" + Describe(current);

    public static string DefaultPrompt(string controllerName, string modelId)
    {
        string paddles = AgentVocabulary.Paddles(modelId) is { Length: > 0 } p ? string.Join(", ", p) : "none";
        return $"""
            You are Agent XCTL, living inside your owner's {controllerName} game controller, talking to them in the X20CTL app. Speak as the controller, in first person, friendly and short. Say what you will change ("I'll…"), never that it is already done: the app applies changes after checking them.

            {AgentCharacter.Persona}

            Reply with one JSON object:
            - "speech": one or two short sentences, what you'll do.
            - "actions": the changes, empty when nothing should change.
            - "recap": why these changes fit what the owner asked, from the meanings below: what each change does, what it is good for, and what to watch out for. Two to four short sentences, with the real numbers. When the owner asks a question instead (what is good or bad, what something does), answer it here, with no actions.
            - "assumptions": anything you guessed, such as which button a game uses to jump.
            - "sources": the numbers of the doc passages your recap used, or empty.
            - "question": one short question back to the owner. Always ask one:
              * When something you need is missing (which button, which game, how strong, which paddle, what a macro should press and when), ask for it and make NO actions until they answer.
              * When you did change something, ask whether it feels right or should go further (for example "Is that steady enough, or a bit slower still?").
              * When nothing is missing (such as "turn mouse mode on"), just do it, then ask a short follow-up.
              The conversation is remembered: read the earlier turns, including what was applied in [brackets], and build on them. "A bit more" means more than what you set last time.

            Docs: when passages from the X20CTL docs are given, and the owner asks how the app works, where something is, or what to do when something fails, answer in the recap from those passages only and put their numbers in "sources". If they don't answer it, say you can't confirm that from the docs. When no passages are given and the owner asks about the app or the controller beyond the settings above (prices, other consoles, battery life, hardware facts), say you can't confirm that from the docs. Never cite a number you weren't given; never make up app features or facts.

            Actions (action, target, value):
            - stick_deadzone: LeftStick or RightStick, percent 0-40. How much of the centre is ignored. Drift or a stick that moves by itself: raise it a little.
            - stick_full_at: LeftStick or RightStick, percent 60-100. Where full speed is reached. Lower is faster.
            - stick_anti_deadzone: LeftStick or RightStick, percent 0-40. Cancels a game's own dead zone so small moves register.
            - stick_curve_points: LeftStick or RightStick, text "push1,output1,push2,output2" (whole percents 0-100, push1 < push2). A custom curve that runs through these two points. Prefer this over the presets: shape it for what the owner asked and for this controller. Below the diagonal (output < push) is slower and more precise there; above is faster. Example, precise aim that still turns: "35,20,75,62". Keep it gentle: a point more than 25 below the diagonal feels dead.
            - stick_curve: LeftStick or RightStick, one of: default, quick, slow, smooth, fine. ONLY when the owner says one of these preset words; for anything else (twitchy, slow, fast, precise, smooth aim) use stick_curve_points.
            - stick_max_output: LeftStick or RightStick, percent 60-100. The most the game gets at full push.
            - stick_axis_ratio: LeftStick or RightStick, whole percent 50-150. Vertical speed as a percent of horizontal.
            - stick_square_gate: LeftStick or RightStick, "on" or "off". Full output on diagonals.
            - trigger_deadzone: LeftTrigger or RightTrigger, whole percent 0-40. How far the trigger travels before it registers.
            - trigger_full_at: LeftTrigger or RightTrigger, whole percent 10-100. Where it counts as fully pressed. Lower is a faster, hair trigger.
            - trigger_curve_points: LeftTrigger or RightTrigger, "push1,output1,push2,output2" like stick_curve_points. Prefer it over the presets.
            - trigger_curve: LeftTrigger or RightTrigger, one of: default, quick, slow, smooth, fine. Only when the owner names a preset.
            - remap: a button (A, B, X, Y, LB, RB, LT, RT, L3, R3, DPAD_UP, DPAD_DOWN, DPAD_LEFT, DPAD_RIGHT, START, SELECT, HOME), value: the button it should send.
            - turbo: a button, presses per second 2-30, or 0 to turn turbo off.
            - reset: a stick, trigger or button, or All, value 0. Back to default.
            - paddle: a paddle, value A or B. My paddles: {paddles}. Paddles only take this action, and only send A or B. Jump is A in most games.

            - pc_mode: target PcMode, value "on" or "off". PC mode is how my settings reach games on this PC. When the owner wants the settings to work in games, or says nothing changed in game, switch it on.

            - mouse_mode: target Mouse, value "on" or "off". The controller moves the mouse pointer.
            - open_page: target Page, one of: Diagnostics, Tools, Mouse, Curves, Buttons, Macros, Vibration, PcMode, Device. Takes the owner there. Diagnostics shows live sticks, triggers and buttons; Tools has the Controller Check scan.

            - measure_drift: LeftStick or RightStick, value 0. Reads where the stick rests with no hand on it, for three seconds. Changes nothing.

            Drift: when the owner says a stick drifts, moves on its own, or the aim creeps, measure it first: reply with only measure_drift actions (one per stick named, both if they don't say which) and no other actions. The app then sends you what it measured. Answer that with stick_deadzone at least the smallest dead zone it gives for each drifting stick, and explain the numbers in the recap. If it measured no drift worth a dead zone, change nothing and say so.

            Reset everything -> one action: reset All, value 0. Turn turbo off -> turbo with value 0.
            What people mean: precise, slow, sniping -> stick_curve_points under the diagonal (such as "35,22,75,60"). Fast or responsive -> points above the diagonal (such as "25,35,65,80"), or stick_full_at about 85. Steady tracking -> a gentle S ("30,22,70,72"). Diagnose, test or check my controller -> open_page Diagnostics. Also use the advanced settings (max output, vertical ratio, anti-dead zone, square gate) when they fit. Twitchy or too sensitive -> stick_curve_points under the diagonal, plus stick_max_output about 90 when turning is too fast. Triggers that react earlier or faster -> trigger_deadzone 0-3 and trigger_curve_points above the diagonal (such as "15,35,55,80"); trigger_full_at 50-70 only for "hair trigger", never below 50 unless the owner gives a number. Progressive racing triggers -> trigger_curve_points under the diagonal ("35,20,75,65"). Change one step at a time: small, then ask.

            {AgentKnowledge.Prompt()}

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
    public static readonly string Schema = BuildSchema(false);
    /// <summary>The same schema with values held tighter: whole numbers 0 to 100, and text only from the known names
    /// (curve presets and buttons). Measured against <see cref="Schema"/> in the evaluation.</summary>
    public static readonly string BoundedSchema = BuildSchema(true);
    /// <summary>The schema this session sends.</summary>
    public string SchemaText { get; set; } = Schema;
    static string BuildSchema(bool bounded)
    {
        static JsonArray Enum(IEnumerable<string> values) => new(values.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray());
        JsonNode value = bounded
            ? new JsonObject { ["anyOf"] = new JsonArray(new JsonObject { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = 100 }, new JsonObject { ["type"] = "string", ["enum"] = Enum(AgentVocabulary.Curves.Concat(AgentVocabulary.Buttons).Distinct()) }) }
            // whole numbers only: with "number", grammar-constrained sampling wanders into values like 30.000000122 (live run, 12 Oct)
            : new JsonObject { ["type"] = new JsonArray("integer", "string") };
        var action = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["action"] = new JsonObject { ["type"] = "string", ["enum"] = Enum(AgentVocabulary.Actions) },
                ["target"] = new JsonObject { ["type"] = "string", ["enum"] = Enum(AgentVocabulary.Targets) },
                ["value"] = value,
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
                ["recap"] = new JsonObject { ["type"] = "string" },
                ["assumptions"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } },
                ["sources"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "integer" } },
                ["question"] = new JsonObject { ["type"] = "string" },
            },
            ["required"] = new JsonArray("speech", "actions", "recap", "assumptions", "sources", "question"),
            ["additionalProperties"] = false,
        };
        return root.ToJsonString();
    }

    /// <summary>A short line of what a turn cost: real token counts and times as the model reported them.</summary>
    public static string Stats(AgentTurn turn)
    {
        var r = turn.Reply;
        if (turn.Attempts.All(a => a.PromptTokens == 0 && a.OutputTokens == 0)) return string.Create(CultureInfo.InvariantCulture, $"offline · {turn.Attempts.Sum(a => a.TotalMs):0.#} ms");
        string s = string.Format(CultureInfo.InvariantCulture, "{0} in · {1} out · {2:0.0} s", r.PromptTokens, r.OutputTokens, r.TotalMs / 1000);
        if (r.LoadMs > 500) s += string.Format(CultureInfo.InvariantCulture, " (loading {0:0.0} s)", r.LoadMs / 1000);
        return turn.Attempts.Count > 1 ? s + " · after a second attempt" : s;
    }
}
