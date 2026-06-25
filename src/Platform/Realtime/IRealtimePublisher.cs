namespace Platform.Realtime;

public interface IRealtimePublisher
{
    Task PublishAsync<TPayload>(
        RealtimePublishRequest<TPayload> request,
        CancellationToken cancellationToken = default);
}
