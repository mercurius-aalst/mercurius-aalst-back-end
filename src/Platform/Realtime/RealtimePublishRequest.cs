namespace Platform.Realtime;

public sealed record RealtimePublishRequest<TPayload>(
    string ClientMethod,
    TPayload Payload,
    IReadOnlyList<string> Groups);
