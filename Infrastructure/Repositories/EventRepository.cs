using Domain.DTO;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Event> AddEventAsync(EventRequest request)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var eventId = Guid.NewGuid();
            var tagNames = request.Categories ?? new List<string>();
            var existingCategories = await _context.Categories
                .Where(c => tagNames.Contains(c.Name))
                .ToListAsync();
            var newCategories = tagNames
                .Where(name => !existingCategories.Any(c => c.Name == name))
                .Select(name => new Category { Id = Guid.NewGuid(), Name = name })
                .ToList();

            var newEvent = new Event
            {
                Id = eventId,
                Name = request.Name,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Location = request.Location,
                Auditorium = request.Auditorium,
                VenueFormat = request.VenueFormat,
                LifecycleState = request.Publish ? EventLifecycleState.Published : EventLifecycleState.Draft,
                IsCancelled = false,
                CancelledAt = null,
                BufferDays = request.BufferDays ?? 14,
                ResponsiblePersonId = request.ResponsiblePersonId,
                MaxParticipants = request.MaxParticipants ?? -1,
                Color = request.Color,
            };

            _context.Categories.AddRange(newCategories);
            _context.Events.Add(newEvent);

            foreach (var kind in request.Types.Distinct())
                _context.EventSelectedTypes.Add(new EventSelectedType { EventId = eventId, TypeKind = kind });

            await _context.SaveChangesAsync();

            var allCategories = existingCategories.Concat(newCategories).ToList();
            var eventCategories = allCategories
                .Where(c => tagNames.Contains(c.Name))
                .Select(c => new EventCategory { EventId = eventId, CategoryId = c.Id })
                .ToList();
            _context.EventCategories.AddRange(eventCategories);

            var roleByUser = new Dictionary<Guid, ParticipantRoleKind>();
            foreach (var p in request.Participants ?? new List<EventParticipantAssignmentDto>())
                roleByUser[p.UserId] = p.Role;
            roleByUser[request.ResponsiblePersonId] = ParticipantRoleKind.Organizer;

            foreach (var (userId, kind) in roleByUser)
            {
                await _context.EventRoles.AddAsync(new EventRole
                {
                    EventId = eventId,
                    UserId = userId,
                    ParticipantRole = kind,
                    IsContact = kind == ParticipantRoleKind.Organizer && userId == request.ResponsiblePersonId,
                    AddedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return newEvent;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        return await _context.Events.ToListAsync();
    }

    public async Task<Event?> GetEventByIdAsync(Guid eventId)
    {
        return await _context.Events
            .Include(e => e.EventCategories)
            .ThenInclude(ec => ec.Category)
            .Include(e => e.SelectedTypes)
            .Include(e => e.Photos)
            .Include(e => e.EventRoles)
            .ThenInclude(er => er.User)
            .FirstOrDefaultAsync(e => e.Id == eventId);
    }

    public async Task<List<Event>> SearchEventsAsync(SearchRequest request)
    {
        var query = ApplySearchFilters(_context.Events.Where(e => e.LifecycleState != EventLifecycleState.Archived), request)
            .OrderBy(e => e.StartDate)
            .Skip(request.Offset)
            .Take(request.Count);

        return await query
            .Include(e => e.EventCategories)
            .ThenInclude(ec => ec.Category)
            .Include(e => e.SelectedTypes)
            .Include(e => e.EventRoles)
            .ToListAsync();
    }
    
    public async Task<List<Event>> SearchArchivedEventsAsync(SearchRequest request, Guid userId)
    {
        var query = ApplySearchFilters(_context.Events.Where(e => e.LifecycleState == EventLifecycleState.Archived), request)
            .OrderBy(e => e.StartDate)
            .Skip(request.Offset)
            .Take(request.Count);

        return await query
            .Include(e => e.EventCategories)
            .ThenInclude(ec => ec.Category)
            .Include(e => e.SelectedTypes)
            .Include(e => e.EventRoles)
            .ToListAsync();
    }

    private static IQueryable<Event> ApplySearchFilters(IQueryable<Event> query, SearchRequest request)
    {
        if (request.Start != null && request.End != null)
        {
            query = query.Where(e =>
                (!e.EndDate.HasValue || e.EndDate >= request.Start) &&
                e.StartDate <= request.End);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(e => e.Name != null && EF.Functions.ILike(e.Name, $"{request.Name}%"));
        }

        if (request.Organizators != null && request.Organizators.Count > 0)
        {
            query = query.Where(e => request.Organizators.Contains(e.ResponsiblePersonId));
        }

        if (request.VenueFormat.HasValue)
            query = query.Where(e => e.VenueFormat == request.VenueFormat.Value);
        else if (!string.IsNullOrWhiteSpace(request.Format))
            query = query.Where(e => e.VenueFormat == VenueFormatParser.Parse(request.Format));

        if (request.HasFreePlaces == true)
        {
            query = query.Where(e => e.MaxParticipants == null || e.MaxParticipants < 0 || e.EventRoles.Count() < e.MaxParticipants);
        }

        if (request.Categories != null && request.Categories.Count > 0)
        {
            query = query.Where(e => e.EventCategories.Any(c => request.Categories.Contains(c.Category.Name)));
        }

        if (request.Types != null && request.Types.Count > 0)
        {
            query = query.Where(e => e.SelectedTypes.Any(t => request.Types.Contains(t.TypeKind)));
        }

        return query;
    }

    public async Task<List<Event>> GetMyEventsAsync(Guid userId)
    {
        return await _context.Events.Where(e => e.ResponsiblePersonId == userId).ToListAsync();
    }

    public async Task AddCategoryAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories.ToListAsync();
    }
    
    public async Task<List<CategoryResponse>> GetEventCategoriesAsync(Guid eventId)
    {
        var categories = await _context.EventCategories
            .Where(ec => ec.EventId == eventId)
            .Select(ec => new CategoryResponse
            {
                Id = ec.Category.Id,
                Name = ec.Category.Name
            })
            .ToListAsync();

        return categories;
    }

    public async Task DeleteEventAndUnusedCategoriesAsync(Event eventToDelete)
    {
        var eventCategories = await _context.EventCategories
            .Where(ec => ec.EventId == eventToDelete.Id)
            .ToListAsync();
        var eventRoles = await _context.EventRoles
            .Where(r => r.EventId == eventToDelete.Id)
            .ToListAsync();
        var eventPhotos = await _context.EventPhotos
            .Where(p => p.EventId == eventToDelete.Id)
            .ToListAsync();

        var categoryIds = eventCategories.Select(ec => ec.CategoryId).ToList();
        
        if (!string.IsNullOrEmpty(eventToDelete.Avatar))
        {
            var avatarPath = eventToDelete.Avatar.TrimStart('/');
            var avatarFullpath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", avatarPath);
            if (File.Exists(avatarFullpath))
                File.Delete(avatarFullpath);
        }

        var eventSelectedTypes = await _context.EventSelectedTypes
            .Where(t => t.EventId == eventToDelete.Id)
            .ToListAsync();
        _context.EventSelectedTypes.RemoveRange(eventSelectedTypes);
        _context.EventCategories.RemoveRange(eventCategories);
        _context.EventRoles.RemoveRange(eventRoles);
        _context.EventPhotos.RemoveRange(eventPhotos);
        _context.Events.Remove(eventToDelete);
        await _context.SaveChangesAsync();
        
        foreach (var photo in eventPhotos)
        {
            var relativePath = photo.FilePath.TrimStart('/'); 
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

            if (File.Exists(fullPath))
            { 
                File.Delete(fullPath);
            }
        }

        var unusedCategories = await _context.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .Where(c => !_context.EventCategories.Any(ec => ec.CategoryId == c.Id))
            .ToListAsync();
        _context.Categories.RemoveRange(unusedCategories);
        await _context.SaveChangesAsync();
    }

    public async Task AddEventCategoryConnAsync(Guid newEventId, Guid categoryId)
    {
        var eventCategory = new EventCategory
        {
            EventId = newEventId,
            CategoryId = categoryId
        };

        await _context.EventCategories.AddAsync(eventCategory);
        await _context.SaveChangesAsync();
    }

    public async Task<EventCategory> AddCategoryToEventAsync(Guid eventId, string categoryName)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == categoryName.ToLower());

        if (category == null)
        {
            category = new Category
            {
                Id = Guid.NewGuid(),
                Name = categoryName
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }
        
        var existingLink = await _context.EventCategories
            .FirstOrDefaultAsync(ec => ec.EventId == eventId && ec.CategoryId == category.Id);

        if (existingLink != null)
        {
            return existingLink;
        }
        
        var eventCategory = new EventCategory
        {
            EventId = eventId,
            CategoryId = category.Id
        };

        _context.EventCategories.Add(eventCategory);
        await _context.SaveChangesAsync();

        return eventCategory;
    }
    
    public async Task DeleteCategoryInEventAsync(Guid eventId, Guid categoryId)
    {
        var eventCategory = await _context.EventCategories
            .FirstOrDefaultAsync(ec => ec.EventId == eventId && ec.CategoryId == categoryId);

        if (eventCategory == null)
            throw new Exception("Связка ивента с категорией не найдена.");
        
        var categoryUsageCount = await _context.EventCategories
            .CountAsync(ec => ec.CategoryId == categoryId);
        
        _context.EventCategories.Remove(eventCategory);
        
        if (categoryUsageCount <= 1)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);
            if (category != null)
                _context.Categories.Remove(category);
        }

        await _context.SaveChangesAsync();
    }
    
    public async Task AddSuscriberAsync(Guid eventId, Guid userId)
    {
        var newSuscriber = new EventRole
        {
            EventId = eventId,
            UserId = userId,
            ParticipantRole = ParticipantRoleKind.Observer,
            AddedAt = DateTime.UtcNow
        };

        await _context.EventRoles.AddAsync(newSuscriber);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSuscriber(Guid eventId, Guid userId)
    {
        var participantToDelete = await _context.EventRoles
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

        _context.EventRoles.Remove(participantToDelete);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteSuscriberByOrganizer(Guid eventId, Guid userId)
    {
        var participantToDelete = await _context.EventRoles
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

        _context.EventRoles.Remove(participantToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<EventRole> SetParticipantRoleForUser(Guid eventId, Guid userId, ParticipantRoleKind role)
    {
        var evt = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId)
                  ?? throw new InvalidOperationException("Мероприятие не найдено");

        var existing = await _context.EventRoles.FirstOrDefaultAsync(er =>
            er.EventId == eventId && er.UserId == userId);

        var isContact = role == ParticipantRoleKind.Organizer && userId == evt.ResponsiblePersonId;

        if (existing != null)
        {
            existing.ParticipantRole = role;
            existing.IsContact = isContact;
            await _context.SaveChangesAsync();
            return existing;
        }

        var row = new EventRole
        {
            EventId = eventId,
            UserId = userId,
            ParticipantRole = role,
            IsContact = isContact,
            AddedAt = DateTime.UtcNow
        };
        _context.EventRoles.Add(row);
        await _context.SaveChangesAsync();
        return row;
    }

    public async Task<List<EventFixedRoleInfoDto>> GetFixedRolesForEventAsync(Guid eventId, int count, int offset)
    {
        if (!await _context.Events.AnyAsync(e => e.Id == eventId))
            return new List<EventFixedRoleInfoDto>();

        var all = Enum.GetValues<ParticipantRoleKind>()
            .Cast<ParticipantRoleKind>()
            .Select(k => new EventFixedRoleInfoDto
            {
                Id = (int)k,
                ParticipantRole = k,
                Name = EventRoleTemplates.NameFor(k)
            })
            .ToList();

        return await Task.FromResult(all.Skip(offset).Take(count).ToList());
    }

    public async Task<EventUserAndCountResponse> GetAllSuscribersAsync(Guid eventId, string? name, string? roleFil, int count, int offset)
    {
        List<EventRole> userRoles = await _context.EventRoles
            .Where(r => r.EventId == eventId)
            .Distinct().Skip(offset)
            .Take(count)
            .ToListAsync();
        
        int totalCount = userRoles.Count;
        List<EventUserResponse> usersRoles = new List<EventUserResponse>();
        foreach (EventRole userRole in userRoles)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => userRole.UserId == u.Id);
            if (!string.IsNullOrWhiteSpace(name))
            {
                string lowered = name.ToLower();
                string fullName = $"{user.LastName} {user.FirstName}".ToLower();

                if (!fullName.Contains(lowered))
                    continue;
            }
            
            var roleDisplay = EventRoleTemplates.NameFor(userRole.ParticipantRole);
            if (!string.IsNullOrWhiteSpace(roleFil))
            {
                if (!roleDisplay.Equals(roleFil, StringComparison.OrdinalIgnoreCase) &&
                    !userRole.ParticipantRole.ToString().Equals(roleFil, StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            EventUserResponse eventUser = new EventUserResponse
            {
                id = user.Id,
                Email = user.Email,
                Name = $"{user.LastName} {user.FirstName}",
                Profession = user.Profession,
                PhoneNumber = user.PhoneNumber,
                Telegram = user.Telegram,
                City = user.City,
                AvatarUrl = user.AvatarUrl,
                Role = roleDisplay,
                IsContact = userRole.IsContact,
                AddedAt = userRole.AddedAt
            };
            usersRoles.Add(eventUser);
        }

        return new EventUserAndCountResponse
        {
            Users = usersRoles,
            TotalCount = totalCount
        };
    }
    
    public async Task<List<EventUserResponse>> GetAllSuscribersWithoutOffsetAsync(Guid eventId, string? name, string? roleFil)
    {
        List<EventRole> userRoles = await _context.EventRoles
            .Where(r => r.EventId == eventId)
            .Distinct()
            .ToListAsync();
        List<EventUserResponse> usersRoles = new List<EventUserResponse>();
        foreach (EventRole userRole in userRoles)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => userRole.UserId == u.Id);
            if (!string.IsNullOrWhiteSpace(name))
            {
                string lowered = name.ToLower();
                string fullName = $"{user.LastName} {user.FirstName}".ToLower();

                if (!fullName.Contains(lowered))
                    continue;
            }
            var roleDisplay = EventRoleTemplates.NameFor(userRole.ParticipantRole);
            if (!string.IsNullOrWhiteSpace(roleFil))
            {
                if (!roleDisplay.Equals(roleFil, StringComparison.OrdinalIgnoreCase) &&
                    !userRole.ParticipantRole.ToString().Equals(roleFil, StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            EventUserResponse eventUser = new EventUserResponse
            {
                id = user.Id,
                Email = user.Email,
                Name = $"{user.LastName} {user.FirstName}",
                Profession = user.Profession,
                PhoneNumber = user.PhoneNumber,
                Telegram = user.Telegram,
                City = user.City,
                AvatarUrl = user.AvatarUrl,
                Role = roleDisplay,
                AddedAt = userRole.AddedAt
            };
            usersRoles.Add(eventUser);
        }
        return usersRoles;
    }

    public async Task<Event> UpdateEventAsync(Event @event)
    {
        _context.Events.Update(@event);
        await _context.SaveChangesAsync();
        
        return @event;
    }

    public async Task ReplaceEventTypesAsync(Guid eventId, IReadOnlyList<EventTypeKind> types)
    {
        var existing = await _context.EventSelectedTypes.Where(x => x.EventId == eventId).ToListAsync();
        _context.EventSelectedTypes.RemoveRange(existing);
        foreach (var k in types.Distinct())
            _context.EventSelectedTypes.Add(new EventSelectedType { EventId = eventId, TypeKind = k });
        await _context.SaveChangesAsync();
    }

    public async Task<List<PhotoResponse>> GetEventPhotosAsync(Guid eventId, int count, int offset)
    {
        var results = await _context.EventPhotos
            .Where(p => p.EventId == eventId)
            .Skip(offset)
            .Take(count)
            .ToListAsync();
        var response = new List<PhotoResponse>();
        foreach (var photo in results)
        {
            var photoData = new PhotoResponse
            {
                Id = photo.Id,
                FilePath = photo.FilePath,
            };
            response.Add(photoData);
        }
        return response;
    }

    public async Task AddEventPhotoAsync(EventPhoto photo)
    {
        _context.EventPhotos.Add(photo);
        await _context.SaveChangesAsync();
    }

    public async Task AddContact(Guid eventId, Guid userId)
    {
        var user = await _context.EventRoles
            .FirstOrDefaultAsync(er => er.EventId == eventId && er.UserId == userId);
        
        user.IsContact = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<ContactResponse>> GetContacts(Guid eventId)
    {
        var eventRoles = await _context.EventRoles
            .Where(u => u.EventId == eventId && u.IsContact == true)
            .Include(u => u.User)
            .ToListAsync();
        var contacts = new List<ContactResponse>();
        foreach (var userEventRole in eventRoles)
        {
            var user = new ContactResponse
            {
                Name = $"{userEventRole.User.LastName} {userEventRole.User.FirstName}",
                Profession = userEventRole.User.Profession,
                Role = EventRoleTemplates.NameFor(userEventRole.ParticipantRole),
                AvatarUrl = userEventRole.User.AvatarUrl
            };
            contacts.Add(user);
        }

        return contacts;
        
    }

    public async Task<Event> FinishEventAsync(Event @event)
    {
        _context.Events.Update(@event);
        await _context.SaveChangesAsync();
        
        return @event;
    }

    public async Task<string> GetEventPhotoByIdAsync(Guid eventId, Guid photoId)
    {
        var photo = await _context.EventPhotos.Where(p => p.EventId == eventId).FirstOrDefaultAsync(p => p.Id == photoId);
        return photo.FilePath;
    }
    
    public async Task<EventPhoto?> GetEventPhotoEntityAsync(Guid eventId, Guid photoId)
    {
        return await _context.EventPhotos
            .Where(p => p.EventId == eventId)
            .FirstOrDefaultAsync(p => p.Id == photoId);
    }
    
    public async Task<List<EventPhoto>> GetEventPhotoEntitiesAsync(Guid eventId, IReadOnlyCollection<Guid> photoIds)
    {
        if (photoIds.Count == 0)
            return new List<EventPhoto>();

        return await _context.EventPhotos
            .Where(p => p.EventId == eventId && photoIds.Contains(p.Id))
            .ToListAsync();
    }

    public async Task DeleteEventPhotoAsync(Guid eventId, Guid photoId)
    {
        var photo = await _context.EventPhotos.Where(p => p.EventId == eventId).FirstOrDefaultAsync(p => p.Id == photoId);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", Path.GetFileName(photo.FilePath));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        _context.EventPhotos.Remove(photo);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteContact(Guid eventId, Guid userId)
    {
        var user = await _context.EventRoles
            .FirstOrDefaultAsync(er => er.EventId == eventId && er.UserId == userId);
        
        user.IsContact = false;
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteEventPhotosAsync(Guid eventId, List<Guid> photoIds)
    {
        var photos = await _context.EventPhotos
            .Where(p => p.EventId == eventId && photoIds.Contains(p.Id))
            .ToListAsync();

        if (!photos.Any())
            return;
        foreach (var photo in photos)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", Path.GetFileName(photo.FilePath));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        _context.EventPhotos.RemoveRange(photos);
        await _context.SaveChangesAsync();
    }
    
    public async Task<Event> UpdateAvatarEventAsync(Event entity)
    {
        _context.Events.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Event> CloneArchivedEventAsTemplateAsync(Event sourceEvent, Guid newOwnerId, string newName)
    {
        var source = await _context.Events
            .Include(e => e.EventCategories)
                .ThenInclude(ec => ec.Category)
            .Include(e => e.SelectedTypes)
            .Include(e => e.EventRoles)
            .Include(e => e.Attachments)
            .Include(e => e.Photos)
            .Include(e => e.Notes)
            .FirstOrDefaultAsync(e => e.Id == sourceEvent.Id)
            ?? throw new InvalidOperationException("Мероприятие не найдено");

        var sourceColumns = await _context.BoardColumn
            .Where(c => c.EventId == source.Id)
            .Include(c => c.Tasks)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var newEventId = Guid.NewGuid();
        var startDate = now.AddDays(7);
        var endDate = source.EndDate.HasValue ? startDate.Add(source.EndDate.Value - source.StartDate) : startDate.AddDays(1);

        var clonedEvent = new Event
        {
            Id = newEventId,
            Name = newName,
            Description = source.Description,
            StartDate = startDate,
            EndDate = endDate,
            Location = source.Location,
            Auditorium = source.Auditorium,
            VenueFormat = source.VenueFormat,
            LifecycleState = EventLifecycleState.Draft,
            IsCancelled = false,
            CancelledAt = null,
            BufferDays = source.BufferDays,
            ResponsiblePersonId = newOwnerId,
            MaxParticipants = source.MaxParticipants,
            Color = source.Color,
            Avatar = null
        };

        _context.Events.Add(clonedEvent);
        await _context.SaveChangesAsync();

        foreach (var sourceType in source.SelectedTypes.DistinctBy(t => t.TypeKind))
        {
            _context.EventSelectedTypes.Add(new EventSelectedType
            {
                EventId = newEventId,
                TypeKind = sourceType.TypeKind
            });
        }

        foreach (var sourceCategory in source.EventCategories)
        {
            _context.EventCategories.Add(new EventCategory
            {
                EventId = newEventId,
                CategoryId = sourceCategory.CategoryId
            });
        }

        _context.EventRoles.Add(new EventRole
        {
            EventId = newEventId,
            UserId = newOwnerId,
            ParticipantRole = ParticipantRoleKind.Organizer,
            IsContact = true,
            AddedAt = now
        });

        foreach (var sourceAttachment in source.Attachments.Where(a => a.Kind == EventAttachmentKind.Link))
        {
            _context.EventAttachments.Add(new EventAttachment
            {
                Id = Guid.NewGuid(),
                EventId = newEventId,
                AuthorId = newOwnerId,
                Kind = EventAttachmentKind.Link,
                Title = sourceAttachment.Title,
                Resource = sourceAttachment.Resource,
                OriginalFileName = null,
                ContentType = null,
                Size = null,
                CreatedAt = now
            });
        }

        var columnIdMap = new Dictionary<Guid, Guid>();
        foreach (var sourceColumn in sourceColumns.OrderBy(c => c.Order))
        {
            var newColumnId = Guid.NewGuid();
            columnIdMap[sourceColumn.Id] = newColumnId;
            _context.BoardColumn.Add(new BoardColumn
            {
                Id = newColumnId,
                EventId = newEventId,
                Name = sourceColumn.Name,
                Order = sourceColumn.Order
            });
        }

        foreach (var sourceColumn in sourceColumns)
        {
            var newColumnId = columnIdMap[sourceColumn.Id];
            foreach (var sourceTask in sourceColumn.Tasks.OrderBy(t => t.Order))
            {
                _context.BoardTasks.Add(new BoardTask
                {
                    Id = Guid.NewGuid(),
                    ColumnId = newColumnId,
                    Title = sourceTask.Title,
                    Description = sourceTask.Description,
                    AssignedUserId = null,
                    CreatorId = newOwnerId,
                    DueDate = null,
                    Priority = sourceTask.Priority,
                    DeadlineReminderSentAt = null,
                    OverdueNotificationSentAt = null,
                    Order = sourceTask.Order,
                    CreatedAt = now,
                    UpdatedAt = null
                });
            }
        }

        await _context.SaveChangesAsync();
        return clonedEvent;
    }

    public async Task<List<Guid>> GetPublicationSubscribersAsync(Guid organizerId, IReadOnlyCollection<string> categoryNames, Guid eventId)
    {
        var normalizedCategories = categoryNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLower())
            .ToList();

        var byOrganizer = await _context.EventRoles
            .Where(r => r.Event.ResponsiblePersonId == organizerId)
            .Select(r => r.UserId)
            .ToListAsync();

        var byCategories = normalizedCategories.Count == 0
            ? new List<Guid>()
            : await _context.EventRoles
                .Where(r => r.Event.EventCategories.Any(ec => normalizedCategories.Contains(ec.Category.Name.ToLower())))
                .Select(r => r.UserId)
                .ToListAsync();

        var participants = await _context.EventRoles
            .Where(r => r.EventId == eventId)
            .Select(r => r.UserId)
            .ToListAsync();

        var recipients = new HashSet<Guid>(byOrganizer.Concat(byCategories));
        recipients.ExceptWith(participants);
        recipients.Remove(organizerId);
        return recipients.ToList();
    }
}