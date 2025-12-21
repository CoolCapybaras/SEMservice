using Domain;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class AdminService: IAdminService
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUserProfileRepository _profileRepository;

    public AdminService(IAdminRepository adminRepository, IUserProfileRepository profileRepository)
    {
        _adminRepository = adminRepository;
        _profileRepository = profileRepository;
    }
    
    public async Task<ServiceResult<List<UserProfileResponse>>> GetUserListAsync(UserSerchRequest request, Guid userId)
    {
        var isAdmin = await UserIsAdmin(userId);
        if (!isAdmin)
            return ServiceResult<List<UserProfileResponse>>.Fail("Вы не обладаете правами администратора");
        var users = await _adminRepository.GetUserListAsync(request);
        return ServiceResult<List<UserProfileResponse>>.Ok(users);
    }

    public async Task<ServiceResult<User>> UpdateProfileAsync(Guid userId, UpdateProfileRequest updateModel, IFormFile? file, Guid adminId)
    {
        var isAdmin = await UserIsAdmin(userId);
        if (!isAdmin)
            return ServiceResult<User>.Fail("Вы не обладаете правами администратора");
        
        var profile = await _profileRepository.GetByIdAsync(userId);
        if (profile == null)
        {
            var createdProfile = await CreateProfileIfNotExistsAsync(userId);
            profile = createdProfile;
        }

        // === Обработка аватара ===
        if (file != null && file.Length > 0)
        {
            // Проверка размера файла (например, не более 5 МБ)
            const long maxSize = 5 * 1024 * 1024;
            if (file.Length > maxSize)
                return ServiceResult<User>.Fail("Размер файла превышает 5 МБ.");
            
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            try
            {
                var uploadsFolder = Path.Combine("wwwroot", "avatars");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                profile.AvatarUrl = $"/avatars/{fileName}";
            }
            catch (Exception)
            {
                return ServiceResult<User>.Fail("Ошибка при сохранении изображения.");
            }
        }

        // === Обновление данных профиля ===
        profile.LastName = updateModel.LastName ?? profile.LastName;
        profile.FirstName = updateModel.FirstName ?? profile.FirstName;
        profile.MiddleName = updateModel.MiddleName ?? profile.MiddleName;
        profile.PhoneNumber = updateModel.PhoneNumber ?? profile.PhoneNumber;
        profile.Telegram = updateModel.Telegram ?? profile.Telegram;
        profile.City = updateModel.City ?? profile.City;

        // === Сохранение ===
        var updatedProfile = await _profileRepository.UpdateProfileAsync(profile);

        return ServiceResult<User>.Ok(MapToModel(updatedProfile));
    }

    public async Task<ServiceResult<bool>> DeleteUserAsync(Guid userId, Guid adminId)
    {
        var isAdmin = await UserIsAdmin(userId);
        if (!isAdmin)
            return ServiceResult<bool>.Fail("Вы не обладаете правами администратора");
        await _adminRepository.DeleteUserAsync(userId);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> GivePrivelegeToUserAsync(Guid userId, Guid adminId)
    {
        var isAdmin = await UserIsAdmin(adminId);
        if (!isAdmin)
            return ServiceResult<bool>.Fail("Вы не обладаете правами администратора");
        var user = await _profileRepository.GetByIdAsync(userId);
        if (user.UserPrivilege == UserPrivilege.ORGANIZER)
            return ServiceResult<bool>.Fail("Пользователь уже обладает правом создания мероприятий");
        await _adminRepository.GivePrivelegeToUserAsync(userId);
        return ServiceResult<bool>.Ok(true);
    }
    
    public async Task<User> CreateProfileIfNotExistsAsync(Guid userId)
    {
        // Проверяем существует ли профиль
        var exists = await _profileRepository.ProfileExistsAsync(userId);
        
        if (exists)
        {
            var profile = await _profileRepository.GetByIdAsync(userId);
            return MapToModel(profile!);
        }
        
        // Создаем новый профиль
        var newProfile = await _profileRepository.CreateProfileAsync(userId);
        
        return MapToModel(newProfile);
    }

    public async Task<bool> UserIsAdmin(Guid userId)
    {
        var user = await _adminRepository.UserIsAdmin(userId);
        return user;
    }
    
    private static User MapToModel(User profile)
    {
        return new User
        {
            Id = profile.Id,
            LastName = profile.LastName,
            FirstName = profile.FirstName,
            MiddleName = profile.MiddleName,
            PhoneNumber = profile.PhoneNumber,
            Telegram = profile.Telegram,
            City = profile.City,
            UserPrivilege = profile.UserPrivilege,
            AvatarUrl = profile.AvatarUrl
        };
    }
    
    
}