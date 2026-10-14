using System.Windows;
using System.Windows.Controls;
using X20Ctl.Zone.Agent;
namespace X20Ctl.Product.Agent;

/// <summary>Teaching controls, kept out of the conversation until expanded.</summary>
internal static class AgentLab
{
    public static Expander Create(AgentSession session, OllamaClient model, string modelId, string controllerName,
        Action<bool> demoChanged, Func<bool> busy)
    {
        var controls = new StackPanel { Margin = new(8) };
        TextBlock Text(string text) => new() { Text = text, Foreground = DS.Brush("DS.TextSoft"), TextWrapping = TextWrapping.Wrap, Margin = new(0, 4, 0, 4) };
        controls.Children.Add(Text("Local chat model"));
        controls.Children.Add(new ComboBox { ItemsSource = new[] { OllamaClient.DefaultModel }, SelectedIndex = 0,
            ToolTip = "Only installed chat model: qwen3.5:9b. No downloads." });
        var temperatureLabel = Text($"Temperature · {model.Temperature:0.00}");
        var temperature = new Slider { Minimum = 0, Maximum = 2, Value = model.Temperature, TickFrequency = .1 };
        temperature.ValueChanged += (_, _) => { model.Temperature = temperature.Value; temperatureLabel.Text = $"Temperature · {model.Temperature:0.00}"; };
        controls.Children.Add(temperatureLabel); controls.Children.Add(temperature);
        controls.Children.Add(Text("System prompt · Qwen only; current settings and controller facts are appended"));
        var prompt = new TextBox { Text = session.SystemPromptOverride.Length > 0 ? session.SystemPromptOverride : AgentSession.DefaultPrompt(controllerName, modelId),
            AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 100, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        prompt.TextChanged += (_, _) => session.SystemPromptOverride = prompt.Text;
        controls.Children.Add(prompt);
        var reset = new Button { Content = "Reset system prompt", Margin = new(0, 4, 0, 4) };
        reset.Click += (_, _) => { prompt.Text = AgentSession.DefaultPrompt(controllerName, modelId); session.SystemPromptOverride = ""; };
        controls.Children.Add(reset);
        var demo = new CheckBox { Content = "Canned demo · deterministic · preview only", Margin = new(0, 6, 0, 6) };
        demo.Click += (_, _) => { if (busy()) { demo.IsChecked = demo.IsChecked != true; return; } demoChanged(demo.IsChecked == true); };
        controls.Children.Add(demo);
        controls.Children.Add(Text("Demo phrases: " + string.Join(" / ", AgentDemo.Prompts)));
        controls.Children.Add(Text("Bad-output playground · real validator · never applies"));
        var output = new TextBox { Text = AgentDemo.Output("demo bad output"), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
            Height = 85, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var result = Text("");
        var validate = new Button { Content = "Validate output", Margin = new(0, 4, 0, 4) };
        validate.Click += (_, _) =>
        {
            var verdict = ProposalValidator.Check(output.Text, modelId);
            result.Text = (verdict.Valid ? "Valid · nothing applied" : "Rejected · nothing applied · " + AgentSession.Problems(verdict))
                + string.Concat(verdict.Checks.Select(c => $"\n{c.State} · {c.Action.Action} · {c.Message}"));
        };
        controls.Children.Add(output); controls.Children.Add(validate); controls.Children.Add(result);
        controls.Children.Add(Text("Agent prompts, replies and document lookup stay on this PC. Qwen can hallucinate. Validation checks structure, targets and ranges; it does not prove advice is correct. Live mode can save PC-mode settings; Undo restores settings. Hardware paddle writes are unsupported. Offline is a phrasebook; canned demo is a separate preview with no model or measurements."));
        return new Expander { Header = "Lab / Safety", Foreground = DS.Brush("DS.Text"), IsExpanded = false, Margin = new(0, 0, 0, 6),
            Content = new ScrollViewer { MaxHeight = 200, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = controls } };
    }
}
