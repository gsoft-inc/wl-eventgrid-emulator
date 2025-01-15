using EventGridEmulator.Configuration;

namespace EventGridEmulator.Tests;

public class TopicOptionsTests
{
    [Fact]
    public void Equals_Returns_True_When_Options_Are_Equal()
    {
        var optionsA = new TopicOptions
        {
            Topics = new Dictionary<string, string[]> { ["a"] = new[] { "b", "c", } },
        };

        var optionsB = new TopicOptions
        {
            Topics = new Dictionary<string, string[]> { ["a"] = new[] { "b", "c", } },
        };

        var optionsC = new TopicOptions(optionsB);

        Assert.Equal(optionsA, optionsB);
        Assert.Equal(optionsB, optionsC);
        Assert.Equal(optionsA, optionsC);
    }

    [Fact]
    public void Equals_Returns_True_When_Options_Are_Equal_With_Filters()
    {
        var optionsA = new TopicOptions
        {
            Topics = new Dictionary<string, string[]> { ["a"] = new[] { "b", "c", } },
            Filters = new Dictionary<string, Filter> { ["b"] = new Filter() { IncludedEventTypes = ["f"] } },
        };

        var optionsB = new TopicOptions
        {
            Topics = new Dictionary<string, string[]> { ["a"] = new[] { "b", "c", } },
            Filters = new Dictionary<string, Filter> { ["b"] = new Filter() { IncludedEventTypes = ["f"] } },
        };

        var optionsC = new TopicOptions(optionsB);

        Assert.Equal(optionsA, optionsB);
        Assert.Equal(optionsB, optionsC);
        Assert.Equal(optionsA, optionsC);
    }

    [Fact]
    public void Equals_Returns_False_When_Options_Are_Not_Equal()
    {
        var optionsA = new TopicOptions();

        var optionsB = new TopicOptions
        {
            Topics = new Dictionary<string, string[]> { ["a"] = new[] { "e", "f", } },
        };

        var optionsC = new TopicOptions(optionsB);
        optionsC.Topics.Add("b", ["c", "d"]);

        Assert.NotEqual(optionsA, optionsB);
        Assert.NotEqual(optionsB, optionsC);
        Assert.NotEqual(optionsA, optionsC);
    }
}
