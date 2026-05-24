using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.DTOs.Users;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settings;

    public SettingsController(ISettingsService settings) => _settings = settings;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _settings.GetAllAsync());

    [HttpPut("{key}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateSettingRequest request) =>
        Ok(await _settings.UpdateAsync(key, request.Value));
}
