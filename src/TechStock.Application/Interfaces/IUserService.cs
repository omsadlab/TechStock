using TechStock.Application.DTOs.Users;

namespace TechStock.Application.Interfaces;

public interface IUserService
{
    Task<List<AppUserDto>> GetAllAsync();
    Task<AppUserDto?> GetByIdAsync(Guid id);
    Task<AppUserDto> CreateAsync(CreateUserRequest request);
    Task<AppUserDto> UpdateAsync(Guid id, UpdateUserRequest request);
    Task ResetPasswordAsync(Guid id, string newPassword);
    Task DeleteAsync(Guid id);
}

public interface ISettingsService
{
    Task<List<AppSettingDto>> GetAllAsync();
    Task<AppSettingDto?> GetByKeyAsync(string key);
    Task<AppSettingDto> UpdateAsync(string key, string value);
}
