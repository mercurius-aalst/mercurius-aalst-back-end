namespace Platform.Eventing;

public sealed class ModuleEventingOptions
{
    public const string SectionName = "ModuleEventing";

    public int DispatchBatchSize { get; set; } = 50;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxAttempts { get; set; } = 5;
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SuccessfulRetention { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan DeadLetterRetention { get; set; } = TimeSpan.FromDays(30);
    public int CleanupBatchSize { get; set; } = 100;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    internal TimeSpan LeaseRenewalInterval => TimeSpan.FromTicks(LeaseDuration.Ticks / 3);
}
