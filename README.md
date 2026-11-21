# BizSpace.API

BizSpace is an ASP.NET Core 10 e-commerce API with products, categories, stores, orders, customer accounts, and product images.

## Requirements

- .NET 10 SDK
- SQL Server

## Local configuration

Keep private settings in a local `.env` file at the solution root. `.gitignore` excludes `.env`.

Set the values your setup needs:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key` and `Auth__CodeHmacKey`
- `SeedAdmin__Email` and `SeedAdmin__Password` to create the first admin account
- `Email__...` values to enable email verification and password recovery
- `Cloudinary__CloudName`, `Cloudinary__ApiKey`, and `Cloudinary__ApiSecret` for image uploads

The app also accepts these as environment variables; environment variables take precedence over `.env` values.

## Run

From the solution root:

```powershell
dotnet restore
dotnet run --project .\BizSpace.API\BizSpace.API.csproj --launch-profile https
```

Open Swagger at `/swagger`. When `Database:ApplyMigrations` is enabled, startup applies EF Core migrations and seeds the demo catalog, two store managers, and four stores. It does not seed customers or orders.

## Images

Product and category image uploads use Cloudinary. Configure the Cloudinary values above before uploading. Demo image references and photo credits are in `BizSpace.API/Data/DemoCatalogImages.cs`;

## Tests

Run the unit tests from the solution root:

```powershell
dotnet test .\BizSpace.Tests\BizSpace.Tests.csproj
```

The tests use temporary in-memory data; they do not need your SQL Server or Cloudinary account.

## Migrations

Run these commands from the solution root:

```powershell
dotnet ef migrations add DescribeYourChange --project .\BizSpace.API\BizSpace.API.csproj --startup-project .\BizSpace.API\BizSpace.API.csproj --output-dir Data\Migrations
dotnet ef database update --project .\BizSpace.API\BizSpace.API.csproj --startup-project .\BizSpace.API\BizSpace.API.csproj
```

To generate an idempotent SQL script from the migrations for review or submission:

```powershell
dotnet ef migrations script --idempotent --project .\BizSpace.API\BizSpace.API.csproj --startup-project .\BizSpace.API\BizSpace.API.csproj --output .\BizSpace.Migrations.sql
```

This creates `BizSpace.Migrations.sql` in the solution root; it does not execute the script against a database. It contains schema migrations, not the demo records inserted by `DatabaseSeeder`.
