using System.Security.Claims;
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
        var existingCategories = await _context.Categories
            .Where(c => request.Categories.Contains(c.Name))
            .ToListAsync();
        var newCategories = request.Categories
            .Where(name => !existingCategories.Any(c => c.Name == name))
            .Select(name => new Category { Name = name })
            .ToList();
        string? avatarPath = null;
        if (request.Avatar != null && request.Avatar.Length > 0)
        {
            var avatarsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "event-avatars"
            );
            if (!Directory.Exists(avatarsFolder))
                Directory.CreateDirectory(avatarsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.Avatar.FileName)}";
            var filePath = Path.Combine(avatarsFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Avatar.CopyToAsync(stream);
            }

            avatarPath = "/" + Path.Combine("event-avatars", fileName)
                .Replace("\\", "/");
        }
        
        var newEvent = new Event
        {
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            Format = request.Format,
            EventType = request.EventType,
            ResponsiblePersonId = request.ResponsiblePersonId,
            MaxParticipants = request.MaxParticipants ?? -1,
            Color = request.Color,
            Avatar = avatarPath,
        };
        

        _context.Categories.AddRange(newCategories);
        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();
        
        var newRoles = new List<Roles>();
        foreach (var role in request.Roles)
        {
            var newRole = new Roles
            {
                Name = role,
                EventId = newEvent.Id
            };
            newRoles.Add(newRole);
        }

        var organizatorRole = new Roles
        {
            Name = "Организатор",
            EventId = newEvent.Id
        };
        newRoles.Add(organizatorRole);
        var participantRole = new Roles
        {
            Name = "Участник",
            EventId = newEvent.Id
        };
        newRoles.Add(participantRole);
        

        _context.Roles.AddRange(newRoles);
        await _context.SaveChangesAsync();

        
        var eventCategories = existingCategories.Concat(newCategories)
            .Select(c => new EventCategory
            {
                EventId = newEvent.Id,
                CategoryId = c.Id
            })
            .ToList();

        Event newEventId = await _context.Events.FirstOrDefaultAsync(r => r == newEvent);
        var organizatorEventRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Организатор" && r.EventId == newEventId.Id);

        var eventOrganiztor = new EventRole
        {
            EventId = newEvent.Id,
            UserId = newEvent.ResponsiblePersonId,
            RoleId = organizatorEventRole.Id,
            IsContact = true
        };

        await _context.EventRoles.AddAsync(eventOrganiztor);
        _context.EventCategories.AddRange(eventCategories);
        await _context.SaveChangesAsync();

        return newEvent;
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
            .Include(e => e.Photos)
            .Include(e => e.EventRoles)
            .ThenInclude(er => er.User)
            .FirstOrDefaultAsync(e => e.Id == eventId);
    }

    public async Task<List<Event>> SearchEventsAsync(SearchRequest request)
    {
        var query = _context.Events.AsQueryable();

		// Фильтр по временному промежутку
		if (request.Start != null && request.End != null)
		{
			query = query.Where(e => e.EndDate >= request.Start && e.StartDate <= request.End);
		}

		// Фильтр по имени
		if (!string.IsNullOrWhiteSpace(request.Name))
		{
			query = query.Where(e => EF.Functions.ILike(e.Name, $"{request.Name}%"));
		}

		// Фильтр по организаторам
		if (request.Organizators != null && request.Organizators.Count > 0)
		{
			query = query.Where(e => request.Organizators.Contains(e.ResponsiblePersonId));
		}

		// Фильтр по формату
		if (!string.IsNullOrWhiteSpace(request.Format))
		{
			query = query.Where(e => e.Format == request.Format);
		}

		// Фильтр по свободным местам
		if (request.HasFreePlaces == true)
		{
			query = query.Where(e => e.EventRoles.Count() < e.MaxParticipants);
		}

		// Фильтр по категориям
		if (request.Categories != null && request.Categories.Count > 0)
		{
			query = query.Where(e => e.EventCategories.Any(c => request.Categories.Contains(c.Category.Name)));
		}

		query = query
            .OrderBy(e => e.StartDate)
            .Skip(request.Offset)
            .Take(request.Count);

        return await query
            .Include(e => e.EventCategories)
            .ThenInclude(ec => ec.Category)
            .ToListAsync();
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

        var roleIds = eventRoles.Select(r => r.RoleId).ToList();
        var categoryIds = eventCategories.Select(ec => ec.CategoryId).ToList();

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
        var unusedRoles = await _context.Roles
            .Where(r => r.EventId == eventToDelete.Id)
            .ToListAsync();

        _context.Roles.RemoveRange(unusedRoles);
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

    public async Task AddSuscriberAsync(Guid eventId, Guid userId)
    {
        var participantEventRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Участник" && r.EventId == eventId);

        var newSuscriber = new EventRole
        {
            EventId = eventId,
            UserId = userId,
            RoleId = participantEventRole.Id,
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

    public async Task<EventRole> AddRoleToUser(Guid eventId, Guid userId, Guid roleId)
    {
        var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.EventId == eventId);
        
        var oldData = await _context.EventRoles.FirstOrDefaultAsync(er =>
            er.EventId == eventId && er.UserId == userId);

        if (oldData != null)
        {
            _context.EventRoles.Remove(oldData);
            await _context.SaveChangesAsync();
        }

        var newUserInRole = new EventRole
        {
            EventId = eventId,
            UserId = userId,
            RoleId = roleEntity.Id
        };

        await _context.EventRoles.AddAsync(newUserInRole);
        await _context.SaveChangesAsync();

        return newUserInRole;
    }

    public async Task<List<Roles>> GetRolesByEvent(Guid eventId, int count, int offset)
    {
        var res = new List<Roles>();
        var roleInEvent = await _context.Roles.Where(r => r.EventId == eventId).Skip(offset)
            .Take(count).ToListAsync();
        foreach (var role in roleInEvent)
        {
            var roleName = new Roles
            {
                Id = role.Id,
                Name = role.Name,
                EventId = role.EventId,
            };
            res.Add(roleName);
        }
        return res;
    }

    public async Task<List<EventUserResponse>> GetAllSuscribersAsync(Guid eventId, string? name, int count, int offset)
    {
        List<EventRole> userRoles = await _context.EventRoles
            .Where(r => r.EventId == eventId)
            .Distinct().Skip(offset)
            .Take(count)
            .ToListAsync();
        List<EventUserResponse> usersRoles = new List<EventUserResponse>();
        foreach (EventRole userRole in userRoles)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => userRole.UserId == u.Id);
            if (!string.IsNullOrWhiteSpace(name))
            {
                string lowered = name.ToLower();
                string fullName = $"{user.LastName} {user.FirstName} {user.MiddleName}".ToLower();

                if (!fullName.Contains(lowered))
                    continue;
            }
            Roles role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == userRole.RoleId);
            EventUserResponse eventUser = new EventUserResponse
            {
                id = user.Id,
                Email = user.Email,
                Name = $"{user.LastName} {user.FirstName} {user.MiddleName}",
                PhoneNumber = user.PhoneNumber,
                Telegram = user.Telegram,
                City = user.City,
                AvatarUrl = user.AvatarUrl,
                Role = role.Name
            };
            usersRoles.Add(eventUser);
        }
        return usersRoles;
    }
    
    public async Task<List<EventUserResponse>> GetAllSuscribersWithoutOffsetAsync(Guid eventId, string? name)
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
                string fullName = $"{user.LastName} {user.FirstName} {user.MiddleName}".ToLower();

                if (!fullName.Contains(lowered))
                    continue;
            }
            Roles role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == userRole.RoleId);
            EventUserResponse eventUser = new EventUserResponse
            {
                id = user.Id,
                Email = user.Email,
                Name = $"{user.LastName} {user.FirstName} {user.MiddleName}",
                PhoneNumber = user.PhoneNumber,
                Telegram = user.Telegram,
                City = user.City,
                AvatarUrl = user.AvatarUrl,
                Role = role.Name
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
            .Include(u => u.Role)
            .ToListAsync();
        var contacts = new List<ContactResponse>();
        foreach (var userEventRole in eventRoles)
        {
            var user = new ContactResponse
            {
                Name = $"{userEventRole.User.LastName} {userEventRole.User.FirstName} {userEventRole.User.MiddleName}",
                Role = userEventRole.Role.Name,
                AvatarUrl = userEventRole.User.AvatarUrl
            };
            contacts.Add(user);
        }

        return contacts;
        
    }

    public async Task<Roles> GetRoleByName(string roleName)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
    }

    public async Task<Event> FinishEventAsync(Event @event)
    {
        _context.Events.Update(@event);
        await _context.SaveChangesAsync();
        
        return @event;
    }

    public async Task<Roles> CreateRoleAsync(Roles role)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<Roles> GetRoleByIdAsync(Guid eventId, Guid roleId)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.EventId == eventId);
        return role;
    }

    public async Task<Roles> UpdateRoleAsync(Roles role)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();
        
        return role;
    }

    public async Task DeleteRoleAsync(Guid eventId, Guid roleId)
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.EventId == eventId);
        var participantRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.Name == "Участник");
        var eventRoles = await _context.EventRoles
            .Where(er => er.EventId == eventId && er.RoleId == roleId)
            .ToListAsync();
        foreach (var er in eventRoles)
        {
            _context.EventRoles.Remove(er);

            var newEr = new EventRole
            {
                EventId = er.EventId,
                UserId = er.UserId,
                RoleId = participantRole.Id,
                IsContact = false
            };

            _context.EventRoles.Add(newEr);
        }
        
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
    }

    public async Task<string> GetEventPhotoByIdAsync(Guid eventId, Guid photoId)
    {
        var photo = await _context.EventPhotos.Where(p => p.EventId == eventId).FirstOrDefaultAsync(p => p.Id == photoId);
        return photo.FilePath;
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
}