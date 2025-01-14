namespace EventGridEmulator.Configuration;

/// <summary>
/// This Filter class is based on the event filtering in event grid:
/// https://learn.microsoft.com/en-us/azure/event-grid/event-filtering#event-type-filtering
/// </summary>
public record Filter
{
    public string[]? IncludedEventTypes { get; set; }

    public Filter(Filter other)
    {
        this.IncludedEventTypes = other.IncludedEventTypes?.ToArray();
    }

    public override string ToString()
    {
        return $"Filter {{ IncludedEventTypes = [{string.Join(", ", this.IncludedEventTypes ?? [])}] }}";
    }
}
