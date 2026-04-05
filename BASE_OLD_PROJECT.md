# ProcureFlow - Codebase Reference (Không CMS)

> Tài liệu tổng hợp toàn bộ code base của dự án, bỏ CMS, chỉ giữ 3 project: **Core**, **Infrastructure**, **Web**.

---

## Cấu hình tạo dự án mới (RFP)

Mục này dùng để khởi tạo dự án RFP Procurement từ tài liệu nghiệp vụ và sơ đồ hiện có.

### Nguồn đầu vào

- Tài liệu nghiệp vụ và schema: `DATABASE.md`
- Sơ đồ dữ liệu: `RfpDiagram.png`, `RfpDiagram.drawio.xml`, `RfpDiagram.decoded.xml`
- Context dự án khởi tạo: `.planning/PROJECT.md`

### GSD Config (đang dùng)

```json
{
  "mode": "yolo",
  "granularity": "standard",
  "model_profile": "quality",
  "commit_docs": true,
  "parallelization": true,
  "workflow": {
    "research": true,
    "plan_check": true,
    "verifier": true,
    "nyquist_validation": true,
    "auto_advance": false
  },
  "git": {
    "branching_strategy": "none",
    "phase_branch_template": "gsd/phase-{phase}-{slug}",
    "milestone_branch_template": "gsd/{milestone}-{slug}"
  }
}
```

### Lệnh khởi tạo và tạo artifact

```bash
# Khởi tạo dự án theo GSD
/gsd-new-project

# Sau khi có PROJECT/REQUIREMENTS/ROADMAP
/gsd-plan-phase 1

# Thực thi phase đầu tiên
/gsd-execute-phase 1
```

### Đầu ra mong đợi sau khi khởi tạo

- `.planning/PROJECT.md`: mô tả bài toán Source-to-Contract
- `.planning/REQUIREMENTS.md`: yêu cầu v1 theo nhóm Company/RFP/Bid/Finalize/Contract
- `.planning/ROADMAP.md`: phase triển khai từ schema -> API -> workflow -> ký hợp đồng
- `.planning/STATE.md`: trạng thái tiến độ qua từng phase

### Ràng buộc nghiệp vụ bắt buộc

- Luồng chuẩn không đổi: `Create RFP -> Invite Vendor -> Submit Bid -> Finalize -> Generate Contract -> Sign`
- Bảo toàn quan hệ dữ liệu chính: `RFP -> RfpItem -> RfpItemSpec`, `RfpBid -> RfpBidItem -> RfpBidItemSpec`, `RfpFinalize -> RfpContract`
- Phân quyền rõ theo vai trò: Admin, Buyer, Vendor
- Tất cả bản ghi nghiệp vụ phải có trạng thái và dấu thời gian phục vụ audit

---

## 1. Tổng quan kiến trúc

- **Framework**: .NET 8, Blazor Web App (Interactive Server)
- **UI Library**: MudBlazor 8.15.0, Blazorise (RichTextEdit)
- **Database**: MySQL 8.0 via Pomelo.EntityFrameworkCore.MySql
- **Auth**: ASP.NET Identity (Cookie-based)
- **Pattern**: Clean Architecture 3-layer

```
ProcureFlow.Core (Domain)  ←  ProcureFlow.Infrastructure (Data/Services)  ←  ProcureFlow.Web (Presentation)
```

### Roles
- **SuperAdmin** / **Admin** — Full quyền quản trị
- **Staff** — Quyền theo Permission claims
- **Agent** — Đại lý (portal riêng)
- **User** — Khách hàng

---

## 2. Cấu trúc thư mục

```
ProcureFlow.sln
├── src/
│   ├── ProcureFlow.Core/
│   │   ├── Constants/Permissions.cs
│   │   ├── DTOs/
│   │   │   ├── DashboardDTOs.cs
│   │   │   ├── OrderDTOs.cs
│   │   │   └── EmployeeGoodsReceiptDTOs.cs
│   │   ├── Entities/ (23 files)
│   │   ├── Enums/AttributeType.cs
│   │   └── Utils/DateTimeHelper.cs
│   │
│   ├── ProcureFlow.Infrastructure/
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── DesignTimeDbContextFactory.cs
│   │   │   └── Migrations/
│   │   ├── Utils/Utils.cs
│   │   ├── DbInitializer.cs
│   │   └── DependencyInjection.cs
│   │
│   └── ProcureFlow.Web/
│       ├── Components/
│       │   ├── App.razor
│       │   ├── Routes.razor
│       │   ├── AdminComponentBase.cs
│       │   ├── _Imports.razor
│       │   ├── RedirectToLogin.razor
│       │   ├── Layout/ (9 files)
│       │   ├── Pages/
│       │   │   ├── Home.razor, SimStore.razor, Error.razor
│       │   │   ├── Admin/ (27 pages + Dialogs/, Blog/, Permissions/, Stock/)
│       │   │   ├── Agent/ (11 pages + Dialogs/)
│       │   │   ├── Public/ (11 pages)
│       │   │   ├── Account/ (Profile.razor, Register.razor)
│       │   │   └── Blog/ (BlogList.razor, BlogDetail.razor)
│       │   ├── Shared/Dialogs/
│       │   └── Chat/ChatBox
│       ├── Helpers/ImageHelper.cs
│       ├── Middleware/GlobalExceptionMiddleware.cs
│       ├── Pages/ (Login.cshtml/.cs, Logout.cshtml/.cs)
│       ├── Program.cs
│       ├── appsettings.json
│       └── wwwroot/ (app.css, js/app.js, uploads/, Templates/)
│
├── deploy/ (.env, build.sh, deploy.sh, nginx.conf)
└── Dockerfile
```

---

## 3. Project Files (.csproj)

### ProcureFlow.Core.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="9.0.0" />
  </ItemGroup>
</Project>
```

### ProcureFlow.Infrastructure.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\ProcureFlow.Core\ProcureFlow.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="MiniExcel" Version="1.42.0" />
    <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.2" />
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.12" />
  </ItemGroup>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

### ProcureFlow.Web.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <ProjectReference Include="..\ProcureFlow.Infrastructure\ProcureFlow.Infrastructure.csproj" />
    <ProjectReference Include="..\ProcureFlow.Core\ProcureFlow.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Blazored.LocalStorage" Version="4.3.0" />
    <PackageReference Include="Blazorise.Bootstrap5" Version="1.8.9" />
    <PackageReference Include="Blazorise.Icons.FontAwesome" Version="1.8.9" />
    <PackageReference Include="Blazorise.RichTextEdit" Version="1.8.9" />
    <PackageReference Include="ClosedXML" Version="0.102.3" />
    <PackageReference Include="Markdig" Version="0.44.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="8.0.6" />
    <PackageReference Include="MudBlazor" Version="8.15.0" />
  </ItemGroup>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

---

## 4. Entities (ProcureFlow.Core/Entities)

### ApplicationUser.cs
```csharp
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcureFlow.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int? AgentId { get; set; }
    [ForeignKey("AgentId")]
    public virtual Agent? ManagedByAgent { get; set; }
}
```

### Product.cs
```csharp
using System.ComponentModel.DataAnnotations.Schema;
namespace ProcureFlow.Core.Entities;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Slug { get; set; }
    public int? BrandId { get; set; }
    [ForeignKey("BrandId")] public virtual Brand? Brand { get; set; }
    public int DisplayOrder { get; set; }
    public string? Unit { get; set; }
    public string? Specifications { get; set; } // JSON
    public string? Details { get; set; } // HTML
    public int CategoryId { get; set; }
    [ForeignKey("CategoryId")] public virtual Category? Category { get; set; }
    public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public virtual ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
    public virtual ICollection<ProductMedia> Medias { get; set; } = new List<ProductMedia>();
    public virtual ICollection<ProductTag> Tags { get; set; } = new List<ProductTag>();
}
```

### ProductVariant.cs
```csharp
using System.ComponentModel.DataAnnotations.Schema;
namespace ProcureFlow.Core.Entities;

public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    [ForeignKey("ProductId")] public virtual Product? Product { get; set; }
    public string? Sku { get; set; }
    public string Properties { get; set; } = "{}"; // JSON {"Color":"Red","Ram":"8GB"}
    public string Color { get; set; } = string.Empty;
    public string Storage { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public string Images { get; set; } = string.Empty;
    public decimal ImportPrice { get; set; }
    public decimal Price { get; set; }
    public virtual ICollection<ProductMedia> Medias { get; set; } = new List<ProductMedia>();
    public virtual ICollection<ProductItem> ProductItems { get; set; } = new List<ProductItem>();
}
```

### ProductItem.cs (Inventory - IMEI-based)
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProcureFlow.Core.Entities;

public enum ProductItemStatus { Available, Reserved, Sold, Defective, PendingTransfer }

public class ProductItem
{
    [Key] public required string IMEI { get; set; }
    public int VariantId { get; set; }
    [ForeignKey("VariantId")] public virtual ProductVariant? Variant { get; set; }
    public int? WarehouseId { get; set; }
    [ForeignKey("WarehouseId")] public virtual Warehouse? Warehouse { get; set; }
    public string Condition { get; set; } = string.Empty;
    public int BatteryHealth { get; set; }
    public string? OSVersion { get; set; }
    public string? PhysicalNote { get; set; }
    public string? ActivationLink { get; set; }
    public string? PhoneNumber { get; set; }
    public ProductItemStatus Status { get; set; } = ProductItemStatus.Available;
    public decimal ImportPrice { get; set; }
    public decimal SalePriceAdjustment { get; set; }
    public virtual ICollection<ProductItemImage> Images { get; set; } = new List<ProductItemImage>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

### Order.cs
```csharp
using System.ComponentModel.DataAnnotations.Schema;
namespace ProcureFlow.Core.Entities;

public enum PaymentMethod { Transfer, Cash, Shipping }
public enum CustomerType { Retail, Agency }
public enum ShippingType { None, Free, Daibiki, AgentPay, Letter }
public enum DeliveryPaymentOption
{
    ShipBithu_200y = 1, Daibiki_1500y = 2, ShipTakyubin_700y = 3,
    TienMat_0y = 4, KhachTraShip_0y = 5
}

public class Order
{
    public int Id { get; set; }
    public string? ApplicationUserId { get; set; }
    [ForeignKey("ApplicationUserId")] public virtual ApplicationUser? User { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public PaymentMethod PaymentMethod { get; set; }
    public CustomerType CustomerType { get; set; }
    public string? ShippingCode { get; set; }
    public string? ShippingAddress { get; set; }
    public decimal ShippingFee { get; set; }
    public ShippingType ShippingType { get; set; }
    public decimal AmountPaid { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public string? CreatedById { get; set; }
    [ForeignKey("CreatedById")] public virtual ApplicationUser? CreatedBy { get; set; }
    public int? AgentId { get; set; }
    [ForeignKey("AgentId")] public virtual Agent? Agent { get; set; }
    public enum AgentProfitAction { CreditBalance, Payout }
    public decimal AgentProfit { get; set; }
    public AgentProfitAction ProfitAction { get; set; }
    public string? Note { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

### OrderItem.cs
```csharp
using System.ComponentModel.DataAnnotations.Schema;
namespace ProcureFlow.Core.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    [ForeignKey("OrderId")] public virtual Order? Order { get; set; }
    public int? ProductId { get; set; }
    [ForeignKey("ProductId")] public virtual Product? Product { get; set; }
    public string? ProductItemId { get; set; } // IMEI
    [ForeignKey("ProductItemId")] public virtual ProductItem? ProductItem { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
```

### Category.cs
```csharp
using System.ComponentModel.DataAnnotations.Schema;
namespace ProcureFlow.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? ImageUrl { get; set; }
    public int? ParentId { get; set; }
    [ForeignKey("ParentId")] public virtual Category? Parent { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
```

### Agent.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProcureFlow.Core.Entities;

public class Agent
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")] public virtual ApplicationUser? User { get; set; }
    public required string ShopName { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public int? WarehouseId { get; set; }
    [ForeignKey("WarehouseId")] public virtual Warehouse? Warehouse { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentDebt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public virtual ICollection<AgentTransaction> Transactions { get; set; } = new List<AgentTransaction>();
}
```

### AgentTransaction.cs
```csharp
using System.ComponentModel.DataAnnotations.Schema;
namespace ProcureFlow.Core.Entities;

public enum TransactionType { Purchase, Payment, Refund, Adjustment }

public class AgentTransaction
{
    public int Id { get; set; }
    public int AgentId { get; set; }
    [ForeignKey("AgentId")] public virtual Agent? Agent { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal DebtBefore { get; set; }
    public decimal DebtAfter { get; set; }
    public string? ReferenceId { get; set; }
    public int? OrderId { get; set; }
    [ForeignKey("OrderId")] public virtual Order? Order { get; set; }
    public string? Note { get; set; }
    public string? ProofImageUrl { get; set; }
}
```

### Các Entity khác (tóm tắt)

| Entity | Mô tả |
|--------|--------|
| `Brand` | Id, Name, Description, LogoUrl, IsActive, DisplayOrder |
| `Attribute` | Id, Name, Code, Description, IsActive, Type (Variant/Specification) |
| `ProductAttribute` | ProductId, AttributeId, Values (JSON array) |
| `ProductMedia` | ProductId, VariantId?, ImageUrl, IsMain |
| `ProductTag` | Id, Name, Color, Icon, DisplayOrder, IsVisible, Products (M2M) |
| `ProductItemImage` | IMEI, ImageUrl, ImageType (Front/Back/Scratch/Other) |
| `Warehouse` | Id, Name, Address |
| `GoodsReceipt` | Id (string), WarehouseId, Status (Draft/WaitingForPricing/Completed/Cancelled), Items |
| `GoodsReceiptItem` | ProductVariantId, Quantity, UnitPrice, ImeiList, ItemDetailsJson |
| `Banner` | Id, Title, Subtitle, ButtonText, LinkUrl, ImageUrl, IsActive, DisplayOrder |
| `BlogPost` | Id, Title, Slug, Excerpt, Content, ImageUrl, IsPublished, IsFeatured, SEO fields |
| `AuditLog` | EntityName, EntityId, Action, UserId, Timestamp, Details |
| `DebtPaymentRequest` | AgentId, Amount, ImageUrl, Status (Pending/Approved/Rejected) |
| `AgentSupportRequest` | AgentId, Title, Content, Status (Pending/Processing/Completed) |

---

## 5. Enums

```csharp
// ProcureFlow.Core/Enums/AttributeType.cs
namespace ProcureFlow.Core.Enums;
public enum AttributeType { Variant = 0, Specification = 1 }
```

---

## 6. Constants

### Permissions.cs
```csharp
namespace ProcureFlow.Core.Constants;

public static class Permissions
{
    public static class Dashboard { public const string View = "Permissions.Dashboard.View"; /* ViewSales, ViewInventory */ }
    public static class Products { /* View, Create, Edit, Delete */ }
    public static class Categories { /* View, Create, Edit, Delete */ }
    public static class Brands { /* View, Create, Edit, Delete */ }
    public static class Orders { /* View, Create, Edit, Delete */ }
    public static class Users { /* View, Create, Edit, Delete, Manage */ }
    public static class Roles { /* View, Create, Edit, Delete, Manage */ }
    public static class Warehouse { /* View, Create, Edit, Delete, Manage */ }
    public static class Agents { /* View, Create, Edit, Delete */ }
    public static class GoodsReceipts { /* View, Create, Edit, Delete, Approve */ }
    public static class Inventory { /* View, Edit, Transfer, Export */ }
    public static class Finance { /* View, Manage */ }
    public static class Blog { /* View, Create, Edit, Delete */ }

    public static List<string> GetAll() { /* reflection over nested types */ }
}
```

## 7. Web Layer

### Program.cs
```csharp
// Key setup:
builder.Services.AddMudServices();
builder.Services.AddRazorPages();
builder.Services.AddBlazorise() + Bootstrap5 + FontAwesome + RichTextEdit;
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddDataProtection().PersistKeysToFileSystem();
// Identity options: no complex password, min 6 chars
// Cookie: LoginPath = "/login"
// Seed data on startup
// Middleware: GlobalExceptionMiddleware
// UseAuthentication, UseAuthorization, UseAntiforgery
// MapRazorPages + MapRazorComponents<App>().AddInteractiveServerRenderMode()
```

### App.razor
- HTML shell with CSS/JS references (MudBlazor, Blazorise, Bootstrap5, Google Fonts Inter/Montserrat/Playfair Display)
- Auto-reload on reconnection failure
- BlazorDownloadFile JS helper

### Routes.razor
```razor
<Router AppAssembly="typeof(Program).Assembly">
    <Found> — Route by path prefix: /admin → AdminLayout, else → PublicLayout
    <NotFound> → PublicLayout + NotFoundPage
</Router>
```

### Layouts

| Layout | Mô tả |
|--------|--------|
| `AdminLayout` | MudAppBar + MudDrawer (NavMenu) + Auth check (Admin/Staff) |
| `AgentLayout` | MudAppBar + Agent NavMenu + Bottom mobile nav + Auth check (Agent) |
| `PublicLayout` | Desktop header (search, cart, auth menu) + Mobile header + Footer + Bottom nav |
| `LoginLayout` | Centered gradient background |
| `PrintLayout` | Clean print-friendly layout |
| `MainLayout` | Legacy sidebar layout |

### NavMenu.razor
- Permission-based menu rendering using `HasPermission()` + `AuthorizeView`
- Sections: Dashboard, Sản phẩm, Người dùng, Kho, Bán hàng, CMS, Blog, Finance, Audit

### AdminComponentBase.cs
```csharp
public abstract class AdminComponentBase : ComponentBase
{
    // Injects AuthenticationStateProvider
    // Provides User property and HasPermission(string) method
    // SuperAdmin/Admin bypass all permission checks
}
```

### Pages Structure

**Admin**

**Agent** 

**Public**

**Account**: Profile, Register


**Root**: Home, Error

### Authentication (Razor Pages)
- `Login.cshtml/.cs` — Email/Password → redirect by role
- `Logout.cshtml/.cs` — SignOut → redirect "/"


### Helpers
- `ImageHelper.cs` — GetFirstImageUrl from comma-separated URLs

### Middleware
- `GlobalExceptionMiddleware.cs` — Catches UriFormatException + general exceptions → JSON

### _Imports.razor
```razor
@using System.Net.Http / .Json
@using Microsoft.AspNetCore.Components / .Forms / .Routing / .Web / .Authorization
@using Microsoft.JSInterop
@using ProcureFlow.Web / .Components
@using MudBlazor
@using ProcureFlow.Core.Entities / .Interfaces
@using Microsoft.AspNetCore.Identity / Microsoft.EntityFrameworkCore
@using ProcureFlow.Web.Components.Layout / .Pages.Public
```

---

## 8. Configuration

### appsettings.json
```json
{
  "Gemini": {
    "ApiKey": "...",
    "Model": "gemini-3-flash-preview",
    "Enabled": true,
    "Configs": [...]
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=ProcureFlow;..."
  }
}
```

---

## 9. Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ProcureFlow.Web/ProcureFlow.Web.csproj", "src/ProcureFlow.Web/"]
COPY ["src/ProcureFlow.Core/ProcureFlow.Core.csproj", "src/ProcureFlow.Core/"]
COPY ["src/ProcureFlow.Infrastructure/ProcureFlow.Infrastructure.csproj", "src/ProcureFlow.Infrastructure/"]
RUN dotnet restore "src/ProcureFlow.Web/ProcureFlow.Web.csproj"
COPY . .
WORKDIR "/src/src/ProcureFlow.Web"
RUN dotnet build "ProcureFlow.Web.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "ProcureFlow.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "ProcureFlow.Web.dll"]
```

---

