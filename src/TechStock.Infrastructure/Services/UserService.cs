using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TechStock.Application.DTOs.Users;
using TechStock.Application.Exceptions;
using TechStock.Application.Interfaces;
using TechStock.Domain.Entities;
using TechStock.Domain.Enums;
using TechStock.Infrastructure.Data;

namespace TechStock.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IMapper _mapper;

    public UserService(UserManager<AppUser> userManager, IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<List<AppUserDto>> GetAllAsync() =>
        _mapper.Map<List<AppUserDto>>(await _userManager.Users.OrderBy(u => u.FullName).ToListAsync());

    public async Task<AppUserDto?> GetByIdAsync(Guid id) =>
        _mapper.Map<AppUserDto>(await _userManager.FindByIdAsync(id.ToString()));

    public async Task<AppUserDto> CreateAsync(CreateUserRequest request)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new BusinessException($"Invalid role: {request.Role}");

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Role = role,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new BusinessException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, role.ToString());
        return _mapper.Map<AppUserDto>(user);
    }

    public async Task<AppUserDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException($"User {id} not found.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new BusinessException($"Invalid role: {request.Role}");

        var oldRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, oldRoles);
        await _userManager.AddToRoleAsync(user, role.ToString());

        user.FullName = request.FullName;
        user.Role = role;
        user.IsActive = request.IsActive;

        await _userManager.UpdateAsync(user);
        return _mapper.Map<AppUserDto>(user);
    }

    public async Task ResetPasswordAsync(Guid id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException($"User {id} not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
            throw new BusinessException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundException($"User {id} not found.");
        user.IsActive = false;
        await _userManager.UpdateAsync(user);
    }
}

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public SettingsService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<AppSettingDto>> GetAllAsync() =>
        _mapper.Map<List<AppSettingDto>>(await _db.AppSettings.OrderBy(s => s.Key).ToListAsync());

    public async Task<AppSettingDto?> GetByKeyAsync(string key) =>
        _mapper.Map<AppSettingDto>(await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key));

    public async Task<AppSettingDto> UpdateAsync(string key, string value)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key)
            ?? throw new NotFoundException($"Setting '{key}' not found.");
        setting.Value = value;
        await _db.SaveChangesAsync();
        return _mapper.Map<AppSettingDto>(setting);
    }
}
