# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Overview

TechStock is a stock management system for a computer hardware retail shop in Sri Lanka. Products are purchased from Japan in JPY batches and resold locally in LKR. The core invariant is that **`BatchItem.RemainingQty` is the live stock counter** — every sale and adjustment mutates this field transactionally.

---

## Solution Structure

```
TechStock.sln
├── src/
│   ├── TechStock.Domain/          # Entities, enums — no external packages
│   ├── TechStock.Application/     # DTOs, service interfaces, AutoMapper profiles, FluentValidation
│   ├── TechStock.Infrastructure/  # EF Core DbContext, Excel/PDF services, repositories
│   ├── TechStock.API/             # ASP.NET Core Web API, controllers, middleware
│   └── TechStock.Blazor/          # Blazor WebAssembly frontend (MudBlazor)
└── tests/
    ├── TechStock.Application.Tests/
    └── TechStock.API.Tests/
```

---

## Common Commands

```bash
# Build entire solution
dotnet build TechStock.sln

# Run the API (https://localhost:7001)
dotnet run --project src/TechStock.API

# Run the Blazor frontend (https://localhost:7002)
dotnet run --project src/TechStock.Blazor

# EF Core migrations (run from solution root)
dotnet ef migrations add <MigrationName> --project src/TechStock.Infrastructure --startup-project src/TechStock.API
dotnet ef database update --project src/TechStock.Infrastructure --startup-project src/TechStock.API

# Run all tests
dotnet test

# Run a single test project
dotnet test tests/TechStock.Application.Tests
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| API | ASP.NET Core Web API |
| ORM | EF Core 8, SQL Server 2022, code-first |
| Auth | ASP.NET Core Identity + JWT Bearer (`IdentityUser<Guid>`) |
| Frontend | Blazor WebAssembly + MudBlazor 7.x |
| Excel | ClosedXML 0.102.2 |
| PDF | QuestPDF 2024.10.x (Community license — set in `Program.cs`) |
| Logging | Serilog (console + rolling file) |
| Mapping | AutoMapper |
| Validation | FluentValidation |
| Token storage | Blazored.LocalStorage |

---

## Architecture & Key Patterns

### Domain Layer
All entities inherit `BaseEntity` (`Id: Guid`, `CreatedAt`, `UpdatedAt`). `AppUser` extends `IdentityUser<Guid>` directly.

`ProductConfig` is the dynamic spec store: one row per spec per product (`ProductId + ConfigTypeId + Value`). `ConfigType` rows belong to a `ProductType` and define what fields appear in the product form. **There are no separate typed config tables** (no LaptopConfig, DesktopConfig, etc.).

### Stock Model
`BatchItem` is both the purchase record and the stock record. `RemainingQty` starts equal to `Quantity` and decrements on every sale. Stock across batches for a product is `SUM(BatchItem.RemainingQty WHERE ProductId = x)`.

### Currency
`UnitCostJPY` × `Batch.ExchangeRate` = `UnitCostLKR` (computed when items are added to a batch). All selling prices and reports are in LKR.

### Role-Based Price Visibility
Three roles: `Admin`, `Manager`, `Salesperson`. Purchase price (`UnitCostJPY`, `UnitCostLKR`) is stripped from API responses when the caller is a Salesperson — enforced in controllers, not just the UI. DTOs use nullable decimals for cost fields (`null` = not visible).

Authorization policies registered in `Program.cs`:
- `"AdminOnly"` — Admin
- `"AdminOrManager"` — Admin, Manager
- `"AllRoles"` — Admin, Manager, Salesperson

### Auto-generated Numbers
- **Batch:** `BT-YYYY-NNN` (sequential within the year)
- **Invoice:** `INV-YYYYMMDD-NNN` (sequential within the day)

### Sale Transactions
Stock deduction and sale creation must happen inside a `BeginTransactionAsync` block. Throw `BusinessException` if `RemainingQty < requested quantity` before committing.

### Soft Deletes
`Product`, `Brand`, and `ProductType` have an `IsActive` field. EF Core global query filters are applied to these three entities. Deleting via API sets `IsActive = false`.

### DbContext
`AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>`. Decimal precision and unique indexes are configured in `OnModelCreating`. Key unique constraints: `Brand.Name`, `ProductType.Name`, `Batch.BatchNumber`, `Sale.InvoiceNumber`.

---

## Blazor Frontend

- **Responsive layout:** `MainLayout.razor` uses `MudHidden` to show a persistent `MudDrawer` sidebar on `MdAndUp` and a bottom `MudBottomNavigation` on `SmAndDown`.
- **Auth:** `AppAuthStateProvider : AuthenticationStateProvider` parses the JWT from `Blazored.LocalStorage` to build `ClaimsPrincipal`. Token key: `"authToken"`.
- **Dynamic config form:** `ProductConfigForm.razor` fetches `ConfigType[]` for the selected `ProductTypeId` and renders one `MudTextField` per config type. Required fields are driven by `ConfigType.IsRequired`.
- **Price display:** Use the `<PriceDisplay>` shared component (renders `"—"` for Salesperson role).
- **API base URL:** Configured in `appsettings.json` under `ApiBaseUrl`.

---

## Seed Data

`DataSeeder.SeedAsync(app)` runs on API startup (idempotent — checks existence before inserting). It creates:
- 14 `ProductType` rows with their ordered `ConfigType` children
- 23 `Brand` rows
- 10 `AppSetting` key-value rows
- Default admin: `admin@techstock.lk` / `Admin@123!`

---

## Business Logic Constraints

- `BatchItem.RemainingQty` must never go below zero (enforced in sale service and stock adjustment service).
- `UnitCostLKR` is always derived from `UnitCostJPY × Batch.ExchangeRate` — never entered directly.
- `Batch.TotalCostLKR` = `SUM(item.UnitCostLKR × item.Quantity)` — recalculate whenever items change.
- `SaleItem.LineTotal` = `(UnitSellingPrice × Quantity) − Discount` — snapshot prices at time of sale.
- Salesperson can only view their own sales (`Sale.CreatedBy == currentUserId`).
- A `Batch` or `BatchItem` cannot be deleted if any `SaleItem` references it.

---

## Configuration

API runs on `https://localhost:7001`, Blazor on `https://localhost:7002`. CORS policy `"BlazorClient"` allows the Blazor origin. JWT key must be at least 32 characters (set in `appsettings.Development.json`, not committed).

QuestPDF license must be set before any PDF generation:
```csharp
QuestPDF.Settings.License = LicenseType.Community;
```
