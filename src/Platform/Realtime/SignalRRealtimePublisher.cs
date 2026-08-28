using Microsoft.AspNetCore.SignalR;

namespace Platform.Realtime;

public sealed class SignalRRealtimePublisher<THub> : IRealtimePublisher
    where THub : Hub
{
    private readonly IHubContext<THub> _hubContext;

    public SignalRRealtimePublisher(IHubContext<THub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishAsync<TPayload>(
        RealtimePublishRequest<TPayload> request,
        CancellationToken cancellationToken = default)
    {
        if (request.Groups.Count == 0)
            return Task.CompletedTask;

        var clients = request.Groups.Count == 1
            ? _hubContext.Clients.Group(request.Groups[0])
            : _hubContext.Clients.Groups(request.Groups);

        return clients.SendAsync(request.ClientMethod, request.Payload, cancellationToken);
    }
}
