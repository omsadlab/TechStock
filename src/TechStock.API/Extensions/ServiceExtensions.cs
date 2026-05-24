using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TechStock.Application.Interfaces;
using TechStock.Application.Mappings;
using TechStock.Domain.Entities;
using TechStock.Infrastructure.Data;
using TechStock.Infrastructure.Excel;
using TechStock.Infrastructure.Pdf;
using TechStock.Infrastructure.Services;

namespace TechStock.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("Default")));

        services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly",      p => p.RequireRole("Admin"));
            options.AddPolicy("AdminOrManager", p => p.RequireRole("Admin", "Manager"));
            options.AddPolicy("AllRoles",       p => p.RequireRole("Admin", "Manager", "Salesperson"));
        });

        services.AddAutoMapper(typeof(MappingProfile));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductTypeService, ProductTypeService>();
        services.AddScoped<IConfigTypeService, ConfigTypeService>();
        services.AddScoped<IBatchService, BatchService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IStockAdjustmentService, StockAdjustmentService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();
        services.AddScoped<IReportPdfService, ReportPdfService>();

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options =>
            options.AddPolicy("BlazorClient", policy =>
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()));
        return services;
    }
}
