
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

    public async Task<User?> GetProfileAsync(Guid userId)
    {
        var profile = await _profileRepository.GetByIdAsync(userId);
        return profile == null ? null : MapToModel(profile);
    }

    public async Task<User> UpdateProfileAsync(Guid userId, User updateModel)
    {
        // Проверяем существует ли профиль
        var exists = await _profileRepository.ProfileExistsAsync(userId);
        
        // Если профиль не существует, создаем его
        if (!exists)
        {
            return await CreateProfileIfNotExistsAsync(userId);
        }
        
        // Получаем текущий профиль
        var profile = await _profileRepository.GetByIdAsync(userId);
        
        // Обновляем поля профиля
        profile!.LastName = updateModel.LastName ?? profile.LastName;
        profile.FirstName = updateModel.FirstName ?? profile.FirstName;
        profile.MiddleName = updateModel.MiddleName ?? profile.MiddleName;
        profile.PhoneNumber = updateModel.PhoneNumber ?? profile.PhoneNumber;
        profile.Telegram = updateModel.Telegram ?? profile.Telegram;
        profile.City = updateModel.City ?? profile.City;
        profile.EducationalInstitution = updateModel.EducationalInstitution ?? profile.EducationalInstitution;
        profile.CourseNumber = updateModel.CourseNumber ?? profile.CourseNumber;
        profile.AvatarUrl = updateModel.AvatarUrl ?? profile.AvatarUrl;
        
        // Сохраняем изменения
        var updatedProfile = await _profileRepository.UpdateProfileAsync(profile);
        
        return MapToModel(updatedProfile);
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

    // Преобразование из модели данных в модель бизнес-логики
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
            EducationalInstitution = profile.EducationalInstitution,
            CourseNumber = profile.CourseNumber,
            AvatarUrl = profile.AvatarUrl
        };
    }
} 