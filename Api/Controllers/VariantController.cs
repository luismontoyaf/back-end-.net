using Application.Services;
using Azure.Core;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VariantController : Controller
    {
        private readonly VariantRepository _repository;
        private readonly IVariantRepository _IvariantRepository;
        private readonly VariantService _variantService;
        private readonly TenantProvider _tenantProvider;

        public VariantController(
            VariantService variantService,
            IVariantRepository IvariantRepository,
            AppDbContext context,
            TenantProvider tenantProvider)
        {
            _repository = new VariantRepository(context, tenantProvider);
            _variantService = variantService;
            _IvariantRepository = IvariantRepository;
            _tenantProvider = tenantProvider;
        }

        [HttpPost("createVariant")]
        public async Task<IActionResult> CreateVariant([FromBody] VariantDto variantDto)
        {
            try
            {
                if (variantDto == null || string.IsNullOrWhiteSpace(variantDto.Name))
                    return BadRequest(new { message = "Los datos enviados no son válidos." });

                await _variantService.SaveVariantAsync(variantDto);

                return Ok(new { message = "Variante guardada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
            }
        }

        [HttpGet("getVariants")]
        public async Task<IActionResult> GetVariants()
        {
            try
            {
                var result = await _variantService.GetVariants();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
            }
        }

        [HttpGet("getVariantById/{id}")]
        public async Task<IActionResult> GetVariantById(int id)
        {
            try
            {
                var variant = await _IvariantRepository.GetVariantById(id);

                if (variant == null)
                    return NotFound();

                return Ok(variant);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
            }
        }

        [HttpPatch("changeStatusVariant/{id}")]
        public async Task<IActionResult> ChangeStatusVariant(int id)
        {
            try
            {
                await _variantService.changeStatusVariant(id);

                return Ok(new { message = "Estado modificado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateVariant(int id, [FromBody] VariantDto dto)
        {
            try
            {
                await _variantService.UpdateVariantAsync(id, dto);

                return Ok(new { message = "Variante actualizada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            try
            {
                await _variantService.DeleteVariant(id);

                return Ok(new { message = "Variante eliminada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
            }
        }
    }
}
