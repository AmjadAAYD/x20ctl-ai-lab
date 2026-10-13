using X20Ctl.Zone.Agent;
using Xunit;
namespace X20Ctl.Zone.Tests;

public class AgentIdeasTests
{
    [Fact]
    public void Nothing_typed_is_the_help_list_first()
    {
        var ideas = AgentIdeas.Search("");
        Assert.Equal(AgentHelp.Situations[0].Say, ideas[0].Say);
        Assert.All(AgentIdeas.All, i => Assert.NotEqual("", i.Intent));
        Assert.Equal(AgentIdeas.All.Count, AgentIdeas.All.Select(i => i.Say).Distinct().Count());
    }

    [Theory]
    [InlineData("sticks", "stick_")]
    [InlineData("Sticks", "stick_")]
    [InlineData("sti", "stick_")]
    [InlineData("triggers", "trigger_")]
    [InlineData("turbo", "turbo_")]
    [InlineData("tur", "turbo_")]
    [InlineData("paddle", "paddle_")]
    [InlineData("pc mode", "pc_mode_")]
    public void Typing_narrows_to_the_topic(string query, string prefix)
    {
        var ideas = AgentIdeas.Search(query);
        Assert.NotEmpty(ideas);
        Assert.StartsWith(prefix, ideas[0].Intent);
    }

    [Fact]
    public void Sticks_lists_only_stick_ideas_at_the_top()
    {
        var ideas = AgentIdeas.Search("sticks");
        Assert.True(ideas.Count >= 5);
        Assert.All(ideas.Take(5), i => Assert.StartsWith("stick_", i.Intent));
    }

    [Fact]
    public void Nonsense_finds_nothing() => Assert.Empty(AgentIdeas.Search("banana"));

    [Theory]
    [InlineData("Sticks", "Sticks")]
    [InlineData("sticks", "Sticks")]
    [InlineData("dead zone", "Dead zone")]
    [InlineData("my triggers?", "My triggers")]
    [InlineData("turbo", "Turbo")]
    public void A_topic_alone_is_a_search(string sentence, string topic) => Assert.Equal(topic, AgentIdeas.Topic(sentence));

    [Theory]
    [InlineData("Turbo on X")]
    [InlineData("Swap A and B")]
    [InlineData("Make my sticks smooth")]
    [InlineData("Reset everything")]
    [InlineData("HELP")]
    [InlineData("hello")]
    public void A_request_is_not_a_search(string sentence) => Assert.Null(AgentIdeas.Topic(sentence));

    [Fact]
    public void The_reply_says_what_was_searched() =>
        Assert.Equal("I can see that you searched about Sticks, do you need any help?", AgentIdeas.TopicReply("Sticks"));
}
