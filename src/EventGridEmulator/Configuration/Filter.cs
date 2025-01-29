using System.Diagnostics.CodeAnalysis;

namespace EventGridEmulator.Configuration;

/// <summary>
/// This Filter class is based on the event filtering in event grid:
/// https://learn.microsoft.com/en-us/azure/event-grid/event-filtering#event-type-filtering
/// </summary>
public class Filter
{
    public required string Subscription { get; set; }
    public string[]? IncludedEventTypes { get; set; }

    public Filter() { }

    [SetsRequiredMembers]
    public Filter(Filter other)
    {
        this.Subscription = other.Subscription;
        this.IncludedEventTypes = other.IncludedEventTypes?.ToArray();
    }

    public override string ToString()
    {
        return $"Filter {{ Subscription = {this.Subscription}, IncludedEventTypes = [{string.Join(", ", this.IncludedEventTypes ?? [])}] }}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Filter other)
        {
            return false;
        }

        if (this.Subscription != other.Subscription)
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
        var hashCode = new HashCode();
        hashCode.Add(this.Subscription);
        hashCode.Add(this.IncludedEventTypes == null ? 0 : this.IncludedEventTypes.GetHashCode());
        return hashCode.ToHashCode();
    }
}
