using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechStock.Application.DTOs.Claims;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize(Policy = "AllRoles")]
public class WarrantyClaimsController : ControllerBase
{
    private readonly IWarrantyClaimService _service;

    public WarrantyClaimsController(IWarrantyClaimService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ClaimQueryParams query) =>
        Ok(await _service.GetClaimsAsync(query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var claim = await _service.GetByIdAsync(id);
        return claim == null ? NotFound() : Ok(claim);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarrantyClaimRequest request)
    {
        var userId = GetUserId();
        var claim = await _service.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateClaimStatusRequest request)
    {
        var userId = GetUserId();
        return Ok(await _service.UpdateStatusAsync(id, request, userId));
    }

    [HttpGet("replacement-candidates")]
    public async Task<IActionResult> ReplacementCandidates([FromQuery] string? search) =>
        Ok(await _service.GetReplacementCandidatesAsync(search));

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
