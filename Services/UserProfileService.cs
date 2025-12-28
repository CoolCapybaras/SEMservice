
using Domain;
using Domain.DTO;
using Microsoft.AspNetCore.Http;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IUserProfileRepository _profileRepository;

    public UserProfileService(IUserProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<ServiceResult<User>> GetProfileAsync(Guid userId)
    {
        var profile = await _profileRepository.GetByIdAsync(userId);

        return ServiceResult<User>.Ok(MapToModel(profile));
    }

    public async Task<ServiceResult<User>> UpdateProfileAsync(Guid userId, UpdateProfileRequest updateModel)
    {
        var profile = await _profileRepository.GetByIdAsync(userId);

        // === Обновление данных профиля ===
        profile.LastName = updateModel.LastName ?? profile.LastName;
        profile.FirstName = updateModel.FirstName ?? profile.FirstName;
        profile.Profession = updateModel.Profession ?? profile.Profession;
        profile.PhoneNumber = updateModel.PhoneNumber ?? profile.PhoneNumber;
        profile.Telegram = updateModel.Telegram ?? profile.Telegram;
        profile.City = updateModel.City ?? profile.City;

        // === Сохранение ===
        var updatedProfile = await _profileRepository.UpdateProfileAsync(profile);

        return ServiceResult<User>.Ok(MapToModel(updatedProfile));
    }

    public async Task<ServiceResult<String>> AddAvatarAsync(Guid userId, IFormFile? file)
    {
        var profile = await _profileRepository.GetByIdAsync(userId);
        
        if (file != null && file.Length > 0)
        {
            const long maxSize = 5 * 1024 * 1024;
            if (file.Length > maxSize)
                return ServiceResult<String>.Fail("Размер файла превышает 5 МБ.");
            
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
                return ServiceResult<String>.Fail("Ошибка при сохранении изображения.");
            }
        }
        var result = await _profileRepository.AddAvatarAsync(userId, profile.AvatarUrl);
        
        return ServiceResult<String>.Ok(profile.AvatarUrl);
    }

    public async Task<ServiceResult<List<Event>>> GetSubscribedEventsAsync(Guid userId)
    {
        var _event = await _profileRepository.GetSubscribedEventsAsync(userId);
        return ServiceResult<List<Event>>.Ok(_event);
    }

    public async Task<ServiceResult<List<User>>> GetOrganizersAsync()
    {
        var organizers = await _profileRepository.GetOrganizers();
        var result = new List<User>();
        foreach (var organizer in organizers)
        {
            result.Add(MapToModel(organizer));
        }

        return ServiceResult<List<User>>.Ok(result);
    }

    // Преобразование из модели данных в модель бизнес-логики
    private static User MapToModel(User profile)
    {
        return new User
        {
            Id = profile.Id,
            LastName = profile.LastName,
            FirstName = profile.FirstName,
            Profession = profile.Profession,
            PhoneNumber = profile.PhoneNumber,
            Telegram = profile.Telegram,
            City = profile.City,
            UserPrivilege = profile.UserPrivilege,
            AvatarUrl = profile.AvatarUrl
        };
    }
} 