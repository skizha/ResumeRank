# ResumeRank.Web

ASP.NET Core 8 Razor Pages web application.

## Structure

- `Pages/` - Razor Pages (`.cshtml` + `.cshtml.cs` code-behind)
- `Pages/Shared/` - Layout and partial views
- `wwwroot/` - Static assets (CSS, JS, images)
- `Program.cs` - Application entry point and service configuration

## Build & Run

```
dotnet build
dotnet run
```

Default URL: https://localhost:5001

## Conventions

- Each page has a `.cshtml` view and `.cshtml.cs` PageModel
- Use dependency injection for services
- Static files go in `wwwroot/`
- Configuration in `appsettings.json` / `appsettings.Development.json`
