namespace StickyBoard.Api.Common.Notifications;

public sealed class NoOpNotificationBus : INotificationBus
{
    public Task NotifyMentionAsync(Guid u, Guid e, string t) => Task.CompletedTask;
    public Task NotifyReplyAsync(Guid u, Guid e, string t) => Task.CompletedTask;
    public Task NotifyAssignmentAsync(Guid u, Guid c) => Task.CompletedTask;
    public Task NotifyDirectMessageAsync(Guid u, Guid m) => Task.CompletedTask;
    public Task NotifyInviteAsync(Guid u, Guid i) => Task.CompletedTask;
    public Task PushAsync(Guid u, string title, string body, object? data = null) => Task.CompletedTask;
    public Task NotifyPresenceAsync(Guid u, string channel, string status) => Task.CompletedTask;
    public Task PublishAsync(string topic, object payload) => Task.CompletedTask;
}
