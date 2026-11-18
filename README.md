# Products API

REST API for managing products, built with ASP.NET Core on .NET 10, with an Angular frontend.

> Work in progress: the endpoints currently return placeholder data. Persistence (SQL Server + EF Core) comes next.

## Structure

| Project | Purpose |
| --- | --- |
| `src/Products.Domain` | Entities |
| `src/Products.Application` | DTOs and service contracts |
| `src/Products.Infrastructure` | Service implementations, later data access |
| `src/Products.Api` | HTTP controllers, OpenAPI, Swagger UI |
| `tests/Products.UnitTests` | Unit tests (xUnit v3) |
| `frontend` | Angular app (standalone components, signals, Angular Material) |

## Requirements

- .NET 10 runtime and a .NET SDK 10 or newer
- Node.js 22 or newer and npm

## Run

Start the API:

```powershell
dotnet run --project src/Products.Api
```

Then open http://localhost:5080/swagger. The OpenAPI document is at http://localhost:5080/openapi/v1.json.

In a second terminal, start the frontend:

```powershell
cd frontend
npm install
npm start
```

Then open http://localhost:4200. The dev server proxies `/api` to the API on port 5080 (see `frontend/proxy.conf.json`).

## Endpoints

| Verb | Route |
| --- | --- |
| GET | `/api/products` |
| GET | `/api/products/{id}` |
| POST | `/api/products` |
| PUT | `/api/products/{id}` |
| DELETE | `/api/products/{id}` |
| POST | `/api/products/{id}/decrement-stock/{quantity}` |
| POST | `/api/products/{id}/add-to-stock/{quantity}` |
| GET | `/api/products/search?name=` |
| GET | `/api/products/stock-level?min=&max=` |

## Code quality

```powershell
dotnet build
dotnet format --verify-no-changes
```

In `frontend`:

```powershell
npm run lint
npm run format:check
```

.NET builds treat warnings as errors, with nullable reference types and the recommended .NET analyzers enabled.
