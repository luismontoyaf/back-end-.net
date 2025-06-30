using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Core.Models;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Services
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public InvoiceRepository(string connectionString, AppDbContext context)
        {
            _connectionString = connectionString;
            _context = context;
        }

        public Client GetUserInfoByDocument(string cedula)
        {
            var client = _context.Clientes
                .Where(u => u.numDocumento == cedula)
                .Select(u => new Client
                {
                    Id = u.Id,
                    nombre = u.nombre,
                    apellidos = u.apellidos,
                    tipoDocumento = u.tipoDocumento,
                    numDocumento = u.numDocumento,
                    fechaNacimiento = (DateTime)u.fechaNacimiento,
                    direccion = u.direccion,
                    correo = u.correo,
                    contrasena = u.contrasena,
                    celular = u.celular
                })
                .FirstOrDefault();

            return client;
        }

        public async Task<List<Sale>> GetAllInvoices ()
        {
            return await _context.Ventas.ToListAsync();
        }

    }
}
