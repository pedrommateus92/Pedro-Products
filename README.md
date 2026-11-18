# Products

A products management solution: a REST API built with ASP.NET Core on .NET 10, and an Angular web app.

> Work in progress: the API currently returns placeholder data. Persistence (SQL Server + EF Core) comes next.

## Solution structure

```
Products.slnx
├── Core
│   ├── src/Core/Products.Domain           Entities
│   ├── src/Core/Products.Application      DTOs and service contracts
│   └── src/Core/Products.Infrastructure   Service implementations, later data access
├── Web
│   ├── src/Web/Products.WebApi            ASP.NET Core API, OpenAPI, Swagger UI
│   └── src/Web/Products.WebApp            Angular app (Visual Studio JavaScript project, .esproj)
├── Tests
│   └── tests/Products.UnitTests           Unit tests (xUnit v3)
└── Other                                  Repo-root files: build settings, editor config, launch profile, README
```

The files under **Other** (`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.editorconfig`) stay at the repository root on disk because the build and editors look for them there.

| Area | Technology |
| --- | --- |
| API | .NET 10, ASP.NET Core controllers, built-in OpenAPI, Swagger UI |
| Web app | Angular 22, standalone components, signals, Angular Material |
| Quality | Nullable + .NET analyzers (warnings as errors), `dotnet format`, ESLint, Prettier |

## Prerequisites

- .NET 10 runtime and a .NET SDK 10 or newer
- Node.js 22 or newer and npm
- Visual Studio 2022 17.10+ or Visual Studio 2026 with the **ASP.NET and web development** workload (optional, for F5)

## Build

The backend and the frontend build separately. `dotnet build` compiles the .NET projects only, since the WebApp project does not run the Angular build.

Backend, from the repository root:

```powershell
dotnet build Products.slnx
```

Frontend, from `src/Web/Products.WebApp` (`npm install` is only needed the first time or after package changes):

```powershell
npm install
npm run build
```

The frontend output goes to `src/Web/Products.WebApp/dist/products-web/browser`. In Visual Studio, **Build > Build Solution** (Ctrl+Shift+B) builds the backend.

Stop a running API before building, otherwise Windows locks its output files.

## Run the solution

The API listens on http://localhost:5080 and the web app on http://localhost:4200. The Angular dev server proxies `/api` calls to the API (see `src/Web/Products.WebApp/proxy.conf.json`), so both must be running.

### Visual Studio

1. Open `Products.slnx`.
2. In the startup dropdown next to the Run button, pick **WebApi + WebApp** (defined in `Products.slnLaunch`).
3. Press F5. Visual Studio starts the API (Swagger opens at `/swagger`) and runs `npm start` for the web app, installing npm packages first if needed.
4. Open http://localhost:4200.

### Command line

Terminal 1, the API:

```powershell
dotnet run --project src/Web/Products.WebApi
```

Terminal 2, the web app (run `npm install` the first time):

```powershell
cd src/Web/Products.WebApp
npm install
npm start
```

Then open:

- Web app: http://localhost:4200
- Swagger UI: http://localhost:5080/swagger
- OpenAPI document: http://localhost:5080/openapi/v1.json

`src/Web/Products.WebApi/Products.WebApi.http` has a ready-made request for every endpoint.

## API endpoints

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

From the repository root:

```powershell
dotnet format --verify-no-changes
```

From `src/Web/Products.WebApp`:

```powershell
npm run lint
npm run format:check
```

.NET builds treat warnings as errors, with nullable reference types and the recommended .NET analyzers enabled.
