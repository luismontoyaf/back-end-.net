using System.Security.Claims;
using Application.Services;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/dashboard/")]
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly TenantProvider _tenantProvider;

        public DashboardController(DashboardService dashboardService, TenantProvider tenantProvider)
        {
            _dashboardService = dashboardService;
            _tenantProvider = tenantProvider;
        }

        [HttpGet("getDashboardInfo")]
        [Authorize]
        public async Task<IActionResult> GetDashboardInfo()
        {
            var result = await _dashboardService.GetDashboardInfo();

            return Ok(result);
        }
    }
}
