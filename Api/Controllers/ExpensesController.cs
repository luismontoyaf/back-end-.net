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
    [Route("api/expenses/")]
    public class ExpensesController : Controller
    {
        private readonly ExpensesService _expensesService;

        public ExpensesController(ExpensesService expensesService)
        {
            _expensesService = expensesService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExpenseDto egreso)
        {
            await _expensesService.CreateAsync(egreso);
            return Ok(new { message = "Egreso registrado correctamente" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _expensesService.GetByTenantAsync();
            return Ok(result);
        }

        [HttpGet("expenses-types")]
        public async Task<IActionResult> GetAllExpensesTypes()
        {
            var result = await _expensesService.GetAllExpensesTypesAsync();
            return Ok(result);
        }
    }
}
