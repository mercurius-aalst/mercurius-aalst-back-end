namespace Platform.Eventing.Persistence;

public sealed class InboxMessage
{
    public string ConsumerName { get; set; } = string.Empty;
    public Guid MessageId { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
