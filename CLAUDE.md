# ResumeRank

Resume ranking application that uses AI agents to parse and score resumes against job requirements.

## Project Structure

```
ResumeRankV1/
├── src/
│   ├── ResumeRank.Web/       # .NET 8 ASP.NET Core Razor Pages frontend
│   └── agents/               # Python AI agents
│       ├── resume_parser/    # Parses resume documents into structured data
│       ├── ranking_agent/    # Scores and ranks resumes against job criteria
│       └── shared/           # Shared utilities and configuration
├── tests/
│   ├── ResumeRank.Web.Tests/ # xUnit tests for the web project
│   └── agents/               # pytest tests for Python agents
└── ResumeRank.sln            # .NET solution file
```

## Build & Run

### Web Application (.NET)
- Build: `dotnet build`
- Run: `dotnet run --project src/ResumeRank.Web`
- Test: `dotnet test`

### Agents (Python)
- Install: `pip install -r src/agents/requirements.txt`
- Test: `pytest tests/agents/`

## Conventions

- .NET code follows standard C# naming conventions (PascalCase for public members, camelCase for locals)
- Python code follows PEP 8 (snake_case for functions/variables, PascalCase for classes)
- Each agent is a self-contained module under `src/agents/`
- Shared code between agents goes in `src/agents/shared/`
- All tests mirror the source structure under `tests/`
