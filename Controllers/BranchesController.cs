using Application.Services;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers
{
    [ApiController]
    [Authorize(Roles = "1,Administrador")]
    [Route("api/branches")]
    public sealed class BranchesController : ControllerBase
    {
        private readonly BranchManagementService _branchService;

        public BranchesController(BranchManagementService branchService)
        {
            _branchService = branchService;
        }

        [HttpGet]
        public async Task<ActionResult<List<CompanyBranchDto>>> GetBranches()
        {
            return Ok(await _branchService.GetBranchesAsync());
        }

        [HttpPost]
        public async Task<ActionResult<CompanyBranchDto>> CreateBranch([FromBody] CreateBranchRequest request)
        {
            try
            {
                return Ok(await _branchService.CreateBranchAsync(request));
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

        [HttpPut("{branchId:int}")]
        public async Task<ActionResult<CompanyBranchDto>> UpdateBranch(int branchId, [FromBody] CreateBranchRequest request)
        {
            try
            {
                var branch = await _branchService.UpdateBranchAsync(branchId, request);
                return branch is null ? NotFound() : Ok(branch);
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
    }
}
