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
            RolesNames = request.Roles
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

        var organizatorEventRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Организатор");

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

        var roleIds = eventRoles.Select(r => r.RoleId).ToList();
        var categoryIds = eventCategories.Select(ec => ec.CategoryId).ToList();

        _context.EventCategories.RemoveRange(eventCategories);
        _context.EventRoles.RemoveRange(eventRoles);
        _context.Events.Remove(eventToDelete);
        await _context.SaveChangesAsync();

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

    public async Task<EventRole> AddRoleToUser(Guid eventId, Guid userId,string roleName)
    {
        var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        
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

    public async Task<List<RolesResponse>> GetRolesByEvent(Guid eventId)
    {
        var res = new List<RolesResponse>();
        var roleInEvent = await _context.Roles.Where(r => r.EventId == eventId).ToListAsync();
        foreach (var role in roleInEvent)
        {
            var roleName = new RolesResponse
            {
                Name = role.Name
            };
            res.Add(roleName);
        }
        return res;
    }

    public async Task<List<User>> GetAllSuscribersAsync(Guid eventId)
    {
        var userIds = await _context.EventRoles
            .Where(r => r.EventId == eventId)
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync();
        
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        return users;
    }

    public async Task<Event> UpdateEventAsync(Event @event)
    {
        _context.Events.Update(@event);
        await _context.SaveChangesAsync();
        
        return @event;
    }

    public async Task<List<string>> GetEventPhotosAsync(Guid eventId)
    {
        return await _context.EventPhotos
            .Where(p => p.EventId == eventId)
            .Select(p => p.FilePath)
            .ToListAsync();
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
                Name = $"{userEventRole.User.LastName} {userEventRole.User.FirstName} {userEventRole.User.LastName}",
                Role = userEventRole.Role.Name,
                PhotoUrl = userEventRole.User.AvatarUrl
            };
            contacts.Add(user);
        }

        return contacts;
    }

    public async Task<Roles> GetRoleByName(string roleName)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
    }
    
    public async Task<List<User>> Get10UsersByName(string userName)
    {
        return await _context.Users
            .Where(u => EF.Functions.ILike($"{u.LastName} {u.FirstName} {u.MiddleName}", $"%{userName}%"))
            .Take(10)
            .ToListAsync();
    }
}