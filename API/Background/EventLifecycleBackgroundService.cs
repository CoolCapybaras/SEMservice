using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.API.Background;

public class EventLifecycleBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventLifecycleBackgroundService> _logger;

    public EventLifecycleBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EventLifecycleBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTransitionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process event lifecycle transitions");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task ProcessTransitionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;

        var changed = false;

        var cancelledToComplete = await db.Events
            .Where(e => e.LifecycleState == EventLifecycleState.Cancelled
                        && e.CancelledAt.HasValue
                        && e.CancelledAt.Value <= now.AddDays(-7))
            .ToListAsync(cancellationToken);
        foreach (var evt in cancelledToComplete)
        {
            evt.LifecycleState = EventLifecycleState.Completed;
            evt.IsCancelled = false;
            changed = true;
        }

        var publishedToComplete = await db.Events
            .Where(e => e.LifecycleState == EventLifecycleState.Published
                        && e.EndDate.HasValue
                        && e.EndDate.Value <= now)
            .ToListAsync(cancellationToken);
        foreach (var evt in publishedToComplete)
        {
            evt.LifecycleState = EventLifecycleState.Completed;
            changed = true;
        }

        var startSoon = await db.Events
            .Where(e => e.LifecycleState == EventLifecycleState.Published
                        && e.StartDate > now
                        && e.StartDate <= now.AddHours(24)
                        && e.EventStart24hNotificationSentAt == null)
            .Select(e => new { Event = e, e.Id, e.Name, e.StartDate, e.ResponsiblePersonId })
            .ToListAsync(cancellationToken);
        foreach (var item in startSoon)
        {
            var participants = await db.EventRoles.Where(r => r.EventId == item.Id).Select(r => r.UserId).ToListAsync(cancellationToken);
            participants.Add(item.ResponsiblePersonId);
            foreach (var userId in participants.Distinct())
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = "EventStart",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        event_id = item.Id,
                        event_name = item.Name,
                        start_at = item.StartDate
                    }),
                    IsRead = false,
                    CreatedAt = now
                };
                await notificationService.AddNotificationIfEnabledAsync(notification);
            }
            item.Event.EventStart24hNotificationSentAt = now;
        }

        var startVerySoon = await db.Events
            .Where(e => e.LifecycleState == EventLifecycleState.Published
                        && e.StartDate > now
                        && e.StartDate <= now.AddHours(1)
                        && e.EventStart1hNotificationSentAt == null)
            .Select(e => new { Event = e, e.Id, e.Name, e.StartDate, e.ResponsiblePersonId })
            .ToListAsync(cancellationToken);
        foreach (var item in startVerySoon)
        {
            var participants = await db.EventRoles.Where(r => r.EventId == item.Id).Select(r => r.UserId).ToListAsync(cancellationToken);
            participants.Add(item.ResponsiblePersonId);
            foreach (var userId in participants.Distinct())
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = "EventStart",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        event_id = item.Id,
                        event_name = item.Name,
                        start_at = item.StartDate,
                        kind = "start_1h"
                    }),
                    IsRead = false,
                    CreatedAt = now
                };
                await notificationService.AddNotificationIfEnabledAsync(notification);
            }
            item.Event.EventStart1hNotificationSentAt = now;
        }

        if (startSoon.Count > 0 || startVerySoon.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        var completeToArchive = await db.Events
            .Where(e => e.LifecycleState == EventLifecycleState.Completed
                        && e.EndDate.HasValue
                        && e.EndDate.Value.AddDays(e.BufferDays) <= now)
            .ToListAsync(cancellationToken);
        foreach (var evt in completeToArchive)
        {
            evt.LifecycleState = EventLifecycleState.Archived;
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken);

        await ProcessBufferEndingNotificationsAsync(db, notificationService, now, cancellationToken);
        await ProcessTaskDeadlineNotificationsAsync(db, notificationService, now, cancellationToken);
    }

    private static async Task ProcessBufferEndingNotificationsAsync(
        ApplicationDbContext db,
        INotificationService notificationService,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var from = now.AddDays(3);
        var to = now.AddDays(3).AddMinutes(30);

        var nearBufferEnd = await db.Events
            .Where(e => e.LifecycleState == EventLifecycleState.Completed
                        && e.EndDate.HasValue
                        && e.EndDate.Value.AddDays(e.BufferDays) >= from
                        && e.EndDate.Value.AddDays(e.BufferDays) <= to
                        && e.BufferEnding3dNotificationSentAt == null)
            .Select(e => new { Event = e, e.Id, e.Name, e.ResponsiblePersonId, ArchiveAt = e.EndDate!.Value.AddDays(e.BufferDays) })
            .ToListAsync(cancellationToken);

        foreach (var item in nearBufferEnd)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = item.ResponsiblePersonId,
                Type = "BufferEndingSoon",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    event_id = item.Id,
                    event_name = item.Name,
                    archive_at = item.ArchiveAt,
                    days_left = 3
                }),
                IsRead = false,
                CreatedAt = now
            };
            await notificationService.AddNotificationIfEnabledAsync(notification);
            item.Event.BufferEnding3dNotificationSentAt = now;
        }

        if (nearBufferEnd.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task ProcessTaskDeadlineNotificationsAsync(
        ApplicationDbContext db,
        INotificationService notificationService,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var remindUntil = now.AddHours(24);

        var dueSoon = await db.BoardTasks
            .Where(t => t.AssignedUserId.HasValue
                        && t.DueDate.HasValue
                        && t.DueDate.Value > now
                        && t.DueDate.Value <= remindUntil
                        && t.DeadlineReminderSentAt == null)
            .Select(t => new
            {
                Task = t,
                EventState = t.Column.Event.LifecycleState
            })
            .ToListAsync(cancellationToken);

        foreach (var item in dueSoon)
        {
            if (item.EventState is EventLifecycleState.Archived or EventLifecycleState.Completed or EventLifecycleState.Cancelled)
                continue;

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = item.Task.AssignedUserId!.Value,
                Type = "TaskDeadline",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    task_id = item.Task.Id,
                    task_title = item.Task.Title,
                    due_at = item.Task.DueDate,
                    kind = "due_24h"
                }),
                IsRead = false,
                CreatedAt = now
            };
            await notificationService.AddNotificationIfEnabledAsync(notification);
            item.Task.DeadlineReminderSentAt = now;
        }

        var overdue = await db.BoardTasks
            .Where(t => t.AssignedUserId.HasValue
                        && t.DueDate.HasValue
                        && t.DueDate.Value <= now
                        && t.OverdueNotificationSentAt == null)
            .Select(t => new
            {
                Task = t,
                EventState = t.Column.Event.LifecycleState
            })
            .ToListAsync(cancellationToken);

        foreach (var item in overdue)
        {
            if (item.EventState is EventLifecycleState.Archived or EventLifecycleState.Completed or EventLifecycleState.Cancelled)
                continue;

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = item.Task.AssignedUserId!.Value,
                Type = "TaskDeadline",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    task_id = item.Task.Id,
                    task_title = item.Task.Title,
                    due_at = item.Task.DueDate,
                    kind = "overdue"
                }),
                IsRead = false,
                CreatedAt = now
            };
            await notificationService.AddNotificationIfEnabledAsync(notification);
            item.Task.OverdueNotificationSentAt = now;
        }

        if (dueSoon.Count > 0 || overdue.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
