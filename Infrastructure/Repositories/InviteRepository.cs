using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class InviteRepository : IInviteRepository
{
    private readonly ApplicationDbContext _context;

    public InviteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Invites> AddInviteAsync(Invites invite)
    {
        await _context.Invites.AddAsync(invite);
        await _context.SaveChangesAsync();
        return invite;
    }

    public async Task<Invites?> GetByIdAsync(Guid inviteId)
    {
        return await _context.Invites.FirstOrDefaultAsync(i => i.Id == inviteId);
    }

    public async Task<List<Invites>> GetUserInvitesAsync(Guid userId)
    {
        return await _context.Invites
            .Where(i => i.InvitedUserId == userId)
            .ToListAsync();
    }

    public async Task AcceptInviteAsync(Guid inviteId)
    {
        var invite = await _context.Invites.FirstOrDefaultAsync(i => i.Id == inviteId);
        if (invite != null)
        {
            invite.Status = InviteStatus.Accepted;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeclineInviteAsync(Guid inviteId)
    {
        var invite = await _context.Invites.FirstOrDefaultAsync(i => i.Id == inviteId);
        if (invite != null)
        {
            invite.Status = InviteStatus.Declined;
            await _context.SaveChangesAsync();
        }
    }
}