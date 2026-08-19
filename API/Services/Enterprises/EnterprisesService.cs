using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Enterprises
{
    public class EnterprisesService(ApplicationDbContext context) : IEnterprisesService
    {
        private readonly ApplicationDbContext context = context;

        public async Task<Enterprise?> GetEnterpriseAsync()
        {
            return await context.Enterprises.FirstOrDefaultAsync();
        }

        public async Task<Enterprise> CreateEnterpriseAsync(Enterprise enterprise)
        {
            context.Enterprises.Add(enterprise);
            await context.SaveChangesAsync();
            return enterprise;
        }

        public async Task<Enterprise?> UpdateEnterpriseAsync(int enterpriseId, Enterprise enterprise)
        {
            var existing = await context.Enterprises.FirstOrDefaultAsync(e => e.enterprise_id == enterpriseId);
            if (existing == null) return null;

            existing.Logo = enterprise.Logo;
            existing.LogoContentType = enterprise.LogoContentType;
            existing.Codigo_Empresa = enterprise.Codigo_Empresa;
            existing.Nombre_Empresa = enterprise.Nombre_Empresa;
            existing.Tipo_Empresa = enterprise.Tipo_Empresa;
            existing.Registro_Fiscal = enterprise.Registro_Fiscal;
            existing.Direccion = enterprise.Direccion;
            existing.Telefono = enterprise.Telefono;
            existing.Correo = enterprise.Correo;
            existing.Latitud = enterprise.Latitud;
            existing.Longitud = enterprise.Longitud;
            existing.Persona_Contacto = enterprise.Persona_Contacto;

            await context.SaveChangesAsync();
            return existing;
        }
    }
}