using System.Security.Claims;
using Application.Services;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers
{
    [ApiController]
    [Authorize(Roles = "1,Administrador")]
    [Route("api/company-settings")]
    public sealed class CompanySettingsController : ControllerBase
    {
        private readonly CompanySettingsService _settingsService;

        public CompanySettingsController(CompanySettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<ActionResult<CompanySettingsDto>> GetCompanySettings()
        {
            return Ok(await _settingsService.GetCurrentCompanyAsync());
        }

        [HttpPut]
        public async Task<ActionResult<CompanySettingsDto>> UpdateCompanySettings(
            [FromBody] UpdateCompanySettingsRequest request)
        {
            try
            {
                return Ok(await _settingsService.UpdateCurrentCompanyAsync(request));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPost("upgrade-request")]
        public async Task<ActionResult<CompanySettingsDto>> RequestPlusUpgrade()
        {
            var requesterEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(requesterEmail))
                return Unauthorized();

            try
            {
                return Ok(await _settingsService.RequestPlusUpgradeAsync(requesterEmail));
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }
    }
}
