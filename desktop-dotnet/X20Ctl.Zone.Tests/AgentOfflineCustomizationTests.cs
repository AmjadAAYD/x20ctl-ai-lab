using X20Ctl.Zone.Agent;
using X20Ctl.Zone.PcMode;
namespace X20Ctl.Zone.Tests;

public sealed class AgentOfflineCustomizationTests
{
    [Theory]
    [InlineData("What does maximum output at 90 mean?")]
    [InlineData("Why use a vertical axis ratio of 120?")]
    public async Task A_numeric_question_does_not_change_settings(string question)
    {
        var turn = await new AgentSession(new OfflineAgent(), "x20_pro", "X20 Pro").Ask(question, new());
        Assert.Empty(turn.Outcome.Changes);
        Assert.Empty(turn.Verdict.Proposal!.Actions);
        Assert.NotEmpty(turn.Verdict.Proposal.Recap);
    }
    [Theory]
    [InlineData("My aim is twitchy", "stick_curve_points", "RightStick")]
    [InlineData("Slower for aiming please", "stick_curve_points", "RightStick")]
    [InlineData("Make my right stick more precise", "stick_curve_points", "RightStick")]
    [InlineData("Turn mouse mode on", "mouse_mode", "Mouse")]
    [InlineData("Switch mouse mode off", "mouse_mode", "Mouse")]
    [InlineData("Open diagnostics", "open_page", "Page")]
    [InlineData("Show curves", "open_page", "Page")]
    [InlineData("Take me to macros", "open_page", "Page")]
    [InlineData("Cap right stick output at 90 percent", "stick_max_output", "RightStick")]
    [InlineData("Set right stick vertical axis ratio to 120 percent", "stick_axis_ratio", "RightStick")]
    [InlineData("Turn square gate on for the left stick", "stick_square_gate", "LeftStick")]
    [InlineData("Give my right stick a custom curve through 30,25,70,65", "stick_curve_points", "RightStick")]
    public async Task New_requests_produce_real_valid_actions(string sentence, string action, string target)
    {
        var turn = await new AgentSession(new OfflineAgent(), "x20_pro", "X20 Pro").Ask(sentence, new());
        Assert.True(turn.Verdict.Valid, AgentSession.Problems(turn.Verdict));
        Assert.Contains(turn.Verdict.Applicable, a => a.Action == action && a.Target == target);
    }

    [Fact]
    public async Task Slight_adjustment_starts_from_the_current_custom_curve()
    {
        var session = new AgentSession(new OfflineAgent(), "x20_pro", "X20 Pro");
        var current = new PcModeProfile();
        var first = await session.Ask("Make my right stick slightly slower for aiming", current);
        Assert.True(first.Verdict.Valid);
        Assert.NotEmpty(first.Outcome.Changes);
        var second = await session.Ask("Make my right stick slightly slower for aiming", first.Outcome.Next!);
        Assert.True(second.Verdict.Valid);
        Assert.NotEmpty(second.Outcome.Changes);
        Assert.NotEqual(first.Outcome.Next!.Curves["RightStick"], second.Outcome.Next!.Curves["RightStick"]);
    }

    [Fact]
    public async Task Offline_clarification_uses_the_next_answer()
    {
        var session = new AgentSession(new OfflineAgent(), "x20_pro", "X20 Pro");
        var first = await session.Ask("Turn turbo on", new());
        Assert.Empty(first.Verdict.Proposal!.Actions);
        var next = await session.Ask("X at 12", new());
        Assert.Equal(12, next.Outcome.Next!.Turbo["X"]);
        session.Forget();
        var fresh = await session.Ask("X at 12", new());
        Assert.Empty(fresh.Verdict.Proposal!.Actions);
    }

    [Fact]
    public async Task Precision_is_below_the_diagonal_and_explicit_presets_are_preserved()
    {
        var session = new AgentSession(new OfflineAgent(), "x20_pro", "X20 Pro");
        var precise = await session.Ask("Make my right stick more precise", new());
        var curve = precise.Outcome.Next!.Curves["RightStick"];
        Assert.Equal("custom", curve.Preset);
        Assert.True(PadProcessor.Through(.5, curve.X1 / 255d, curve.Y1 / 255d, curve.X2 / 255d, curve.Y2 / 255d) < .5);
        var explicitPreset = await session.Ask("Set my right stick to the quick preset", new());
        Assert.Equal("quick", explicitPreset.Outcome.Next!.Curves["RightStick"].Preset);
    }

    [Fact]
    public async Task Offline_lighting_color_effect_and_clarification()
    {
        var session = new AgentSession(new OfflineAgent(), "x20_pro", "X20 Pro");
        var colorTurn = await session.Ask("can u set the rgbs to light blue", new());
        Assert.True(colorTurn.Verdict.Valid);
        var colorAction = Assert.Single(colorTurn.Verdict.Proposal!.Actions);
        Assert.Equal(AgentVocabulary.LedColor, colorAction.Action);
        Assert.True(AgentColors.TryParse(colorAction.ValueText, out var parsedColor));
        Assert.Equal("light blue", colorAction.ValueText, ignoreCase: true);
        Assert.True(parsedColor.B > parsedColor.R);

        var rainbowTurn = await session.Ask("put rainbow mode on my leds", new());
        Assert.True(rainbowTurn.Verdict.Valid);
        var rainbowAction = Assert.Single(rainbowTurn.Verdict.Proposal!.Actions);
        Assert.Equal(AgentVocabulary.LedEffect, rainbowAction.Action);
        Assert.Contains("rainbow", rainbowAction.ValueText, StringComparison.OrdinalIgnoreCase);

        var offTurn = await session.Ask("turn off my rgb", new());
        Assert.True(offTurn.Verdict.Valid);
        var offAction = Assert.Single(offTurn.Verdict.Proposal!.Actions);
        Assert.Equal(AgentVocabulary.LedEffect, offAction.Action);
        Assert.Equal("off", offAction.ValueText, ignoreCase: true);

        var vagueTurn = await session.Ask("change the lights", new());
        Assert.True(vagueTurn.Verdict.Valid);
        Assert.Empty(vagueTurn.Verdict.Proposal!.Actions);
        Assert.NotEmpty(vagueTurn.Verdict.Proposal!.Question);

        var explainTurn = await session.Ask("what does rainbow do?", new());
        Assert.True(explainTurn.Verdict.Valid);
        Assert.Empty(explainTurn.Verdict.Proposal!.Actions);
        Assert.Contains("rainbow", explainTurn.Verdict.Proposal!.Speech + " " + explainTurn.Verdict.Proposal!.Recap, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Direct_literal_rejects_out_of_range_values()
    {
        var session = new AgentSession(new OfflineAgent(), "x20_pro", "X20 Pro");
        var turn = await session.Ask("set the right stick full speed to 40", new());
        Assert.True(turn.Verdict.Valid);
        Assert.Empty(turn.Verdict.Proposal!.Actions);
        Assert.Empty(turn.Outcome.Changes);
    }

    [Fact]
    public void PhrasebookCleaner_rejects_cross_intent_noise_and_preserves_game_mentions()
    {
        Assert.NotNull(PhrasebookCleaner.Rule("greeting", "game modes show"));
        Assert.NotNull(PhrasebookCleaner.Rule("trigger_curve_set", "Change my right button response curve"));
        Assert.Null(PhrasebookCleaner.Rule("trigger_fast", "Make triggers faster for Rainbow Six"));
    }
}
