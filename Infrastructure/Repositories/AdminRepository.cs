using Domain.DTO;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class AdminRepository: IAdminRepository
{
    
    private readonly ApplicationDbContext _context;

    public AdminRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<UserProfileResponse>> GetUserListAsync(UserSerchRequest request)
    {
        var query = _context.Users.AsQueryable();

        // Фильтр по имени
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            query = query.Where(e => EF.Functions.ILike(e.FirstName, $"{request.FirstName}%"));
        }

        // Фильтр по фамилии
        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            query = query.Where(e => EF.Functions.ILike(e.LastName, $"{request.LastName}%"));
        }

        // Фильтр по Отчеству
        if (!string.IsNullOrWhiteSpace(request.MiddleName))
        {
            query = query.Where(e => EF.Functions.ILike(e.MiddleName, $"{request.MiddleName}%"));
        }

        // Фильтр по Городу
        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(e => EF.Functions.ILike(e.MiddleName, $"{request.MiddleName}%"));
        }

        query = query
            .Skip(request.Offset)
            .Take(request.Count);
        
        var result = await query.ToListAsync();
        var response = new List<UserProfileResponse>();
        foreach (var user in result)
        {
            response.Add(MapToResponse(user));
        }

        return response;
    }

    public async Task<User> UpdateProfileAsync(User userProfile)
    {
        _context.Users.Update(userProfile);
        await _context.SaveChangesAsync();
        
        return userProfile;
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(p => p.Id == userId);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task GivePrivelegeToUserAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(p => p.Id == userId);
        user.UserPrivilege = UserPrivilege.ORGANIZER;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UserIsAdmin(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(p => p.Id == userId);
        if (user.UserPrivilege == UserPrivilege.ADMIN)
        {
            return true;
        }
        return false;
    }
    
    private static UserProfileResponse MapToResponse(User model)
    {
        return new UserProfileResponse
        {
            Id = model.Id,
            LastName = model.LastName,
            FirstName = model.FirstName,
            MiddleName = model.MiddleName,
            PhoneNumber = model.PhoneNumber,
            Telegram = model.Telegram,
            City = model.City,
            UserPrivilege = model.UserPrivilege.ToString(),
            AvatarUrl = model.AvatarUrl
        };
    }
}