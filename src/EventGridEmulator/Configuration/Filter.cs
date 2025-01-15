namespace EventGridEmulator.Configuration;

/// <summary>
/// This Filter class is based on the event filtering in event grid:
/// https://learn.microsoft.com/en-us/azure/event-grid/event-filtering#event-type-filtering
/// </summary>
public class Filter
{
    public Filter(Filter other)
    {
        this.IncludedEventTypes = other.IncludedEventTypes?.ToArray();
    }

    public Filter() { }

    public string[]? IncludedEventTypes { get; set; }

    public override string ToString()
    {
        return $"Filter {{ IncludedEventTypes = [{string.Join(", ", this.IncludedEventTypes ?? [])}] }}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Filter other)
        {
            return false;
        }

        return (this.IncludedEventTypes, other.IncludedEventTypes) switch
        {
            (null, null) => true,
            (not null, not null) => this.IncludedEventTypes.SequenceEqual(other.IncludedEventTypes),
            _ => false,
        };
    }

    public override int GetHashCode()
    {
        return this.IncludedEventTypes == null ? 0 : this.IncludedEventTypes.GetHashCode();
    }
}
