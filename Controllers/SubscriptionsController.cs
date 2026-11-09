using System.Security.Claims;
using Application.Services;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers
{
    [ApiController]
    [Authorize(Roles = "platform_creator")]
    [Route("api/subscriptions")]
    public sealed class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionsController(SubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("tenants")]
        public async Task<ActionResult<List<SubscriptionTenantDto>>> GetTenants()
        {
            return Ok(await _subscriptionService.GetAllTenantsAsync());
        }

        [HttpPost("tenants/{tenantId:int}/initialize")]
        public async Task<IActionResult> InitializeSubscription(
            int tenantId,
            [FromBody] InitializeSubscriptionRequest request)
        {
            if (request.StartDate == default)
                return BadRequest(new { message = "La fecha de inicio es obligatoria." });

            var initialized = await _subscriptionService.InitializeSubscriptionAsync(tenantId, request.StartDate);
            if (!initialized)
                return Conflict(new { message = "No se pudo inicializar la suscripción. Verifica la empresa y si ya tiene obligaciones." });

            return Ok(new { message = "Se crearon las 12 obligaciones mensuales." });
        }

        [HttpPost("periods/{periodId:int}/payments")]
        public async Task<IActionResult> RegisterPayment(int periodId)
        {
            var creatorEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "creator";
            var registered = await _subscriptionService.RecordPaymentAsync(periodId, creatorEmail);
            if (!registered)
                return Conflict(new { message = "El periodo no existe o ya fue pagado." });

            return Ok(new { message = "Pago registrado correctamente." });
        }

        [HttpPost("tenants/{tenantId:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int tenantId)
        {
            var deactivated = await _subscriptionService.DeactivateTenantAsync(tenantId);
            if (!deactivated)
                return Conflict(new { message = "La empresa no existe o ya está inactiva." });

            return Ok(new { message = "Empresa desactivada correctamente." });
        }

        [HttpPost("tenants/{tenantId:int}/renew")]
        public async Task<IActionResult> Renew(int tenantId)
        {
            var renewed = await _subscriptionService.RenewSubscriptionAsync(tenantId);
            if (!renewed)
                return Conflict(new { message = "La renovación solo está disponible cuando los 12 meses del ciclo están pagados." });

            return Ok(new { message = "Se creó un nuevo ciclo de 12 meses." });
        }
    }
}
