using AutoMapper;
using TechStock.Application.DTOs.Batches;
using TechStock.Application.DTOs.Brands;
using TechStock.Application.DTOs.Products;
using TechStock.Application.DTOs.Sales;
using TechStock.Application.DTOs.Users;
using TechStock.Domain.Entities;

namespace TechStock.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Brand, BrandDto>();

        CreateMap<ProductType, ProductTypeDto>();

        CreateMap<ConfigType, ConfigTypeDto>();

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand.Name))
            .ForMember(d => d.ProductTypeName, o => o.MapFrom(s => s.ProductType.Name))
            .ForMember(d => d.TotalStock, o => o.Ignore())
            .ForMember(d => d.SellingPriceLKR, o => o.Ignore())
            .ForMember(d => d.UnitCostLKR, o => o.Ignore())
            .ForMember(d => d.UnitCostJPY, o => o.Ignore());

        CreateMap<ProductConfig, ProductConfigDto>()
            .ForMember(d => d.ConfigTypeName, o => o.MapFrom(s => s.ConfigType.Name));

        CreateMap<Batch, BatchDto>()
            .ForMember(d => d.PurchaseCurrency, o => o.MapFrom(s => s.Currency))
            .ForMember(d => d.TotalCost, o => o.MapFrom(s => s.TotalCostLKR));

        CreateMap<BatchItemWarrantyOption, WarrantyOptionDto>();

        CreateMap<BatchItem, BatchItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Product.Brand.Name))
            .ForMember(d => d.ProductTypeName, o => o.MapFrom(s => s.Product.ProductType.Name))
            .ForMember(d => d.UnitCostPurchase, o => o.MapFrom(s => s.UnitCostJPY))
            .ForMember(d => d.UnitCostLocal, o => o.MapFrom(s => s.UnitCostLKR))
            .ForMember(d => d.SellingPrice, o => o.MapFrom(s => s.SellingPriceLKR))
            .ForMember(d => d.WarrantyOptions, o => o.MapFrom(s => s.WarrantyOptions));

        CreateMap<Sale, SaleDto>()
            .ForMember(d => d.CreatedByName, o => o.Ignore());

        CreateMap<SaleItem, SaleItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.BatchItem.Product.Name))
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.BatchItem.Product.Brand.Name))
            .ForMember(d => d.Barcode, o => o.MapFrom(s => s.BatchItem.Barcode));

        CreateMap<AppUser, AppUserDto>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));

        CreateMap<AppSetting, AppSettingDto>();
    }
}
