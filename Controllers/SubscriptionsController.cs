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
        private readonly BranchManagementService _branchService;
        private readonly CompanySettingsService _companySettingsService;

        public SubscriptionsController(
            SubscriptionService subscriptionService,
            BranchManagementService branchService,
            CompanySettingsService companySettingsService)
        {
            _subscriptionService = subscriptionService;
            _branchService = branchService;
            _companySettingsService = companySettingsService;
        }

        [HttpGet("companies")]
        public async Task<ActionResult<List<SubscriptionCompanyDto>>> GetCompanies()
        {
            return Ok(await _subscriptionService.GetAllCompaniesAsync());
        }

        [HttpGet("plan-upgrade-requests")]
        public async Task<ActionResult<List<PlanUpgradeRequestDto>>> GetPendingPlanUpgradeRequests()
        {
            return Ok(await _companySettingsService.GetPendingUpgradeRequestsAsync());
        }

        [HttpPost("plan-upgrade-requests/{requestId:int}/approve")]
        public async Task<IActionResult> ApprovePlanUpgradeRequest(int requestId)
        {
            var approverEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "creator";
            var approved = await _companySettingsService.ApprovePlusUpgradeAsync(requestId, approverEmail);
            if (!approved)
                return Conflict(new { message = "La solicitud no existe o ya fue procesada." });

            return Ok(new { message = "Se aprobó la mejora al plan Plus." });
        }

        [HttpPost("plan-upgrade-requests/{requestId:int}/reject")]
        public async Task<IActionResult> RejectPlanUpgradeRequest(int requestId)
        {
            var reviewerEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "creator";
            var rejected = await _companySettingsService.RejectPlusUpgradeAsync(requestId, reviewerEmail);
            if (!rejected)
                return Conflict(new { message = "La solicitud no existe o ya fue procesada." });

            return Ok(new { message = "Se rechazó la solicitud de mejora a Plus." });
        }

        [HttpPost("companies")]
        public async Task<ActionResult<CompanyBranchDto>> CreateCompany([FromBody] CreateCompanyRequest request)
        {
            try
            {
                return Ok(await _branchService.CreateCompanyAsync(request));
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPost("companies/{companyId:int}/initialize")]
        public async Task<IActionResult> InitializeSubscription(
            int companyId,
            [FromBody] InitializeSubscriptionRequest request)
        {
            if (request.StartDate == default)
                return BadRequest(new { message = "La fecha de inicio es obligatoria." });

            var initialized = await _subscriptionService.InitializeSubscriptionAsync(companyId, request.StartDate);
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

        [HttpPost("companies/{companyId:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int companyId)
        {
            var deactivated = await _subscriptionService.DeactivateCompanyAsync(companyId);
            if (!deactivated)
                return Conflict(new { message = "La empresa no existe o ya está inactiva." });

            return Ok(new { message = "Empresa desactivada correctamente." });
        }

        [HttpPost("companies/{companyId:int}/activate")]
        public async Task<IActionResult> Activate(int companyId)
        {
            var activated = await _subscriptionService.ActivateCompanyAsync(companyId);
            if (!activated)
                return Conflict(new { message = "La empresa no existe o ya está activa." });

            return Ok(new { message = "Empresa activada correctamente." });
        }

        [HttpPost("companies/{companyId:int}/renew")]
        public async Task<IActionResult> Renew(int companyId)
        {
            var renewed = await _subscriptionService.RenewSubscriptionAsync(companyId);
            if (!renewed)
                return Conflict(new { message = "La renovación solo está disponible cuando los 12 meses del ciclo están pagados." });

            return Ok(new { message = "Se creó un nuevo ciclo de 12 meses." });
        }
    }
}
