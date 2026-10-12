using System.Text.Json;
using X20Ctl.Zone.Agent;
using X20Ctl.Zone.PcMode;
namespace X20Ctl.Zone.Tests;

/// <summary>The Talk agent: what the model says is checked against fixed rules, and only a valid proposal changes settings.</summary>
public sealed class AgentTests
{
    static string Reply(params string[] actions) => $$"""{"speech":"I'll do it.","actions":[{{string.Join(",", actions)}}],"assumptions":[]}""";
    static string Act(string action, string target, string value) => $$"""{"action":"{{action}}","target":"{{target}}","value":{{value}},"reason":"asked"}""";

    [Fact]
    public void A_valid_proposal_changes_only_what_it_names()
    {
        var verdict = ProposalValidator.Check(Reply(Act("stick_deadzone", "RightStick", "6"), Act("trigger_full_at", "RightTrigger", "40")), "x20_pro");
        Assert.True(verdict.Valid);
        var before = new PcModeProfile();
        var outcome = AgentApplier.Apply(before, verdict);
        Assert.NotNull(outcome.Next);
        Assert.Equal(6, outcome.Next!.Sticks["RightStick"].Inner);
        Assert.Equal(40, outcome.Next.Curves["RightTrigger"].Outer);
        Assert.False(outcome.Next.Sticks.ContainsKey("LeftStick"));
        Assert.Empty(outcome.Next.Remap);
        Assert.Equal(["Right stick dead zone", "Right trigger full at"], outcome.Changes.Select(c => c.What));
        Assert.True(before.IsNeutral); // the current profile is never touched
    }

    [Fact]
    public void One_rejected_action_rejects_the_whole_proposal()
    {
        var verdict = ProposalValidator.Check(Reply(Act("stick_deadzone", "RightStick", "6"), Act("stick_deadzone", "LeftStick", "260")), "x20_pro");
        Assert.False(verdict.Valid);
        Assert.Equal(CheckState.Rejected, verdict.Checks[1].State);
        var outcome = AgentApplier.Apply(new PcModeProfile(), verdict);
        Assert.Null(outcome.Next);
        Assert.Empty(outcome.Changes);
    }

    [Fact]
    public void M5_exists_on_the_X20_Pro_but_not_on_the_X20()
    {
        string m5 = Reply(Act("paddle", "M5", "\"A\""));
        var pro = ProposalValidator.Check(m5, "x20_pro");
        Assert.True(pro.Valid);
        Assert.Equal(CheckState.Unavailable, pro.Checks[0].State); // valid, but paddles aren't written yet
        var x20 = ProposalValidator.Check(m5, "x20");
        Assert.False(x20.Valid);
        Assert.Contains("no M5", x20.Checks[0].Message);
    }

    [Fact]
    public void Paddles_only_send_A_or_B()
    {
        var verdict = ProposalValidator.Check(Reply(Act("paddle", "M1", "\"X\"")), "x20_pro");
        Assert.False(verdict.Valid);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("""{"speech":"hi","actions":[]}""")] // assumptions missing
    [InlineData("""{"speech":"hi","actions":[],"assumptions":[],"mood":"happy"}""")] // a field that isn't in the schema
    [InlineData("""{"speech":"hi","actions":[{"action":"turbo","target":"A","reason":"x"}],"assumptions":[]}""")] // value missing
    public void Malformed_replies_are_structure_errors(string json)
    {
        var verdict = ProposalValidator.Check(json, "x20_pro");
        Assert.NotNull(verdict.StructureError);
        Assert.False(verdict.Valid);
        Assert.Null(AgentApplier.Apply(new PcModeProfile(), verdict).Next);
    }

    [Theory]
    [InlineData("stick_deadzone", "RightStick", "\"6%\"")] // text where a number belongs (seen from the live model, 12 Oct)
    [InlineData("stick_full_at", "RightStick", "40")]      // below 60
    [InlineData("stick_deadzone", "LT", "5")]             // a button, not a stick
    [InlineData("trigger_deadzone", "LeftTrigger", "5.5")] // triggers take whole percents
    [InlineData("stick_curve", "LeftStick", "\"turbo\"")]  // not a preset
    [InlineData("remap", "M1", "\"A\"")]                  // paddles aren't remapped like buttons
    [InlineData("turbo", "A", "50")]                      // above 30
    [InlineData("fly", "A", "1")]                         // not an action
    public void Rule_breaks_are_rejected(string action, string target, string value)
    {
        var verdict = ProposalValidator.Check(Reply(Act(action, target, value)), "x20_pro");
        Assert.Null(verdict.StructureError);
        Assert.False(verdict.Valid);
    }

    [Fact]
    public void A_repeated_change_is_kept_once_and_a_conflicting_one_is_rejected()
    {
        var same = ProposalValidator.Check(Reply(Act("stick_deadzone", "RightStick", "6"), Act("stick_deadzone", "RightStick", "6")), "x20_pro");
        Assert.True(same.Valid);
        Assert.Single(AgentApplier.Apply(new PcModeProfile(), same).Changes);
        var clash = ProposalValidator.Check(Reply(Act("stick_deadzone", "RightStick", "6"), Act("stick_deadzone", "RightStick", "9")), "x20_pro");
        Assert.False(clash.Valid);
    }

    [Fact]
    public void A_result_the_profile_wont_hold_is_rejected_whole()
    {
        // each value is in range alone, but a dead zone past where full speed is reached isn't a stick setting
        var verdict = ProposalValidator.Check(Reply(Act("trigger_deadzone", "LeftTrigger", "40"), Act("trigger_full_at", "LeftTrigger", "30")), "x20_pro");
        Assert.True(verdict.Valid);
        var outcome = AgentApplier.Apply(new PcModeProfile(), verdict);
        Assert.Null(outcome.Next);
        Assert.NotNull(outcome.Error);
    }

    [Fact]
    public void Presets_remaps_turbo_and_reset_land_where_PC_mode_reads_them()
    {
        var verdict = ProposalValidator.Check(Reply(Act("stick_curve", "LeftStick", "\"smooth\""), Act("remap", "A", "\"B\""), Act("turbo", "X", "12")), "x20");
        var next = AgentApplier.Apply(new PcModeProfile(), verdict).Next!;
        Assert.Equal("smooth", next.Curves["LeftStick"].Preset);
        Assert.Equal((142, 142), (next.Curves["LeftStick"].X1, next.Curves["LeftStick"].Y1));
        Assert.Equal("B", next.Remap["A"]);
        Assert.Equal(12, next.Turbo["X"]);
        var reset = AgentApplier.Apply(next, ProposalValidator.Check(Reply(Act("reset", "All", "0")), "x20")).Next!;
        Assert.True(reset.IsNeutral);
    }

    [Fact]
    public void The_schema_offers_exactly_the_vocabulary()
    {
        using var schema = JsonDocument.Parse(AgentSession.Schema);
        var item = schema.RootElement.GetProperty("properties").GetProperty("actions").GetProperty("items").GetProperty("properties");
        Assert.Equal(AgentVocabulary.Actions, item.GetProperty("action").GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(AgentVocabulary.Targets, item.GetProperty("target").GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Paddles_match_the_controller_catalog()
    {
        using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data/catalog.json")));
        foreach (var model in catalog.RootElement.EnumerateArray())
        {
            string id = model.GetProperty("id").GetString()!;
            var slots = model.TryGetProperty("macroSlots", out var s) ? s.EnumerateArray().Select(e => e.GetString()!).ToArray() : [];
            Assert.True(slots.SequenceEqual(AgentVocabulary.Paddles(id)), $"{id}: catalog {string.Join(",", slots)} vs agent {string.Join(",", AgentVocabulary.Paddles(id))}");
        }
    }

    sealed class ScriptedModel(params string[] replies) : IAgentModel
    {
        int next;
        public string Name => "scripted";
        public List<IReadOnlyList<ChatMessage>> Seen { get; } = new();
        public Task<ModelReply> Chat(IReadOnlyList<ChatMessage> messages, string schema, CancellationToken cancel)
        { Seen.Add(messages.ToList()); return Task.FromResult(new ModelReply(replies[next++], 10, 20, 0, 100)); }
    }

    [Fact]
    public async Task Broken_JSON_gets_one_retry_and_both_attempts_are_kept()
    {
        var model = new ScriptedModel("{oops", Reply(Act("stick_deadzone", "LeftStick", "4")));
        var turn = await new AgentSession(model, "x20_pro", "X20 Pro").Ask("less drift please", new PcModeProfile());
        Assert.Equal(2, turn.Attempts.Count);
        Assert.True(turn.Verdict.Valid);
        Assert.Equal(4, turn.Outcome.Next!.Sticks["LeftStick"].Inner);
    }

    [Fact]
    public async Task A_rejected_reply_gets_one_second_chance_with_the_validators_reasons()
    {
        var model = new ScriptedModel(Reply(Act("paddle", "M5", "\"A\"")), Reply());
        var turn = await new AgentSession(model, "x20", "X20").Ask("jump on M5", new PcModeProfile());
        Assert.Equal(2, turn.Attempts.Count);
        Assert.Contains("This controller has no M5", model.Seen[1][^1].Content);
        Assert.True(turn.Verdict.Valid); // the second answer changes nothing, which is allowed
        Assert.Empty(turn.Outcome.Changes);
    }

    [Fact]
    public async Task A_second_wrong_answer_stands_and_changes_nothing()
    {
        string bad = Reply(Act("paddle", "M5", "\"A\""));
        var turn = await new AgentSession(new ScriptedModel(bad, bad), "x20", "X20").Ask("jump on M5", new PcModeProfile());
        Assert.Equal(2, turn.Attempts.Count);
        Assert.False(turn.Verdict.Valid);
        Assert.Null(turn.Outcome.Next);
    }

    [Fact]
    public void A_paddle_this_model_lacks_is_named_whatever_the_action()
    {
        var verdict = ProposalValidator.Check(Reply(Act("remap", "M5", "\"DPAD_UP\"")), "x20"); // seen live, 12 Oct
        Assert.Contains("no M5", verdict.Checks[0].Message);
        verdict = ProposalValidator.Check(Reply(Act("remap", "DPAD_UP", "\"M5\"")), "x20"); // the other way round, also live
        Assert.Contains("no M5", verdict.Checks[0].Message);
        verdict = ProposalValidator.Check(Reply(Act("remap", "M5", "\"A\"")), "x20_pro");
        Assert.Contains("\"action\":\"paddle\"", verdict.Checks[0].Message);
    }

    [Fact]
    public void The_schema_keeps_numbers_whole()
    {
        using var schema = JsonDocument.Parse(AgentSession.Schema);
        var value = schema.RootElement.GetProperty("properties").GetProperty("actions").GetProperty("items").GetProperty("properties").GetProperty("value");
        Assert.Equal(["integer", "string"], value.GetProperty("type").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task The_prompt_carries_the_current_settings_and_recent_conversation()
    {
        var model = new ScriptedModel(Reply(), Reply());
        var session = new AgentSession(model, "x20_pro", "X20 Pro");
        var current = new PcModeProfile { Turbo = { ["X"] = 10 } };
        await session.Ask("first", current); await session.Ask("second", current);
        var second = model.Seen[1];
        Assert.Contains("X turbo: 10/s", second[0].Content);
        Assert.Equal(["system", "user", "assistant", "user"], second.Select(m => m.Role));
    }
}
