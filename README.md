# ResumeRank

A resume ranking application that uses AI agents to parse and score resumes against job requirements.

## Features

- **Resume Parsing**: Upload PDF/DOCX resumes and extract structured data (skills, experience, summary)
- **Job Matching**: Define job descriptions with required and preferred skills
- **AI-Powered Ranking**: Score and rank candidates based on job requirements using LLM
- **Web Interface**: User-friendly Razor Pages frontend for managing jobs and resumes

## Architecture

The application consists of three components:

| Component | Technology | Port | Description |
|-----------|------------|------|-------------|
| Web App | .NET 8 ASP.NET Core | 5016 | Razor Pages frontend |
| Resume Parser Agent | Python FastAPI | 5100 | Extracts structured data from resumes |
| Ranking Agent | Python FastAPI | 5101 | Scores resumes against job criteria |

```
┌─────────────────────────────────────────────────────────────────┐
│                           Browser                               │
└─────────────────────────────┬───────────────────────────────────┘
                              │ HTTP
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    .NET Web Application                         │
│                   (ASP.NET Core - Port 5016)                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Razor Pages │  │  Services   │  │    SQLite Database      │  │
│  └─────────────┘  └──────┬──────┘  └─────────────────────────┘  │
└──────────────────────────┼──────────────────────────────────────┘
                           │ HTTP
              ┌────────────┴────────────┐
              ▼                         ▼
┌───────────────────────┐   ┌───────────────────────┐
│  Resume Parser Agent  │   │    Ranking Agent      │
│  (FastAPI - Port 5100)│   │  (FastAPI - Port 5101)│
│  ┌─────────────────┐  │   │  ┌─────────────────┐  │
│  │ PDF/DOCX Parser │  │   │  │   LLM Service   │  │
│  └─────────────────┘  │   │  └─────────────────┘  │
└───────────────────────┘   └───────────────────────┘
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Python 3.10+](https://www.python.org/downloads/)
- LLM API Key (e.g., Anthropic, OpenAI, or local LLM endpoint)

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/skizha/ResumeRank.git
cd ResumeRank
```

### 2. Install Python dependencies

```bash
pip install -r src/agents/requirements.txt
```

### 3. Configure environment

Set your LLM API key (example using Anthropic):

```bash
# Windows
set LLM_API_KEY=your-api-key-here

# Linux/Mac
export LLM_API_KEY=your-api-key-here
```

### 4. Build the .NET application

```bash
dotnet build
```

## Running the Application

You need to run all three components:

### Terminal 1 - Resume Parser Agent

```bash
cd src/agents
python -m uvicorn resume_parser.main:app --port 5100
```

### Terminal 2 - Ranking Agent

```bash
cd src/agents
python -m uvicorn ranking_agent.main:app --port 5101
```

### Terminal 3 - Web Application

```bash
dotnet run --project src/ResumeRank.Web
```

Open http://localhost:5016 in your browser.

## Project Structure

```
ResumeRankV1/
├── src/
│   ├── ResumeRank.Web/           # .NET 8 ASP.NET Core Razor Pages
│   │   ├── Pages/                # Razor Pages
│   │   ├── Models/               # Data models
│   │   ├── Services/             # Business logic and HTTP clients
│   │   └── Data/                 # EF Core DbContext
│   └── agents/                   # Python AI agents
│       ├── resume_parser/        # Resume parsing agent
│       ├── ranking_agent/        # Resume ranking agent
│       └── shared/               # Shared configuration
├── tests/
│   ├── ResumeRank.Web.Tests/     # xUnit tests
│   └── agents/                   # pytest tests
└── ResumeRank.sln                # .NET solution file
```

## Configuration

### Web Application

Edit `src/ResumeRank.Web/appsettings.json`:

```json
{
  "AgentServices": {
    "ResumeParserUrl": "http://localhost:5100",
    "RankingAgentUrl": "http://localhost:5101"
  }
}
```

### Python Agents

Environment variables:
- `LLM_API_KEY` - Your LLM API key (required for AI features)
- `RESUME_PARSER_PORT` - Resume parser port (default: 5100)
- `RANKING_AGENT_PORT` - Ranking agent port (default: 5101)

## API Endpoints

### Resume Parser Agent (port 5100)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/parse` | Parse a resume file |
| GET | `/health` | Health check |

### Ranking Agent (port 5101)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/rank` | Rank resumes against a job |
| GET | `/health` | Health check |

## Testing

### .NET Tests

```bash
dotnet test
```

### Python Tests

```bash
pytest tests/agents/ -v
```

## License

MIT
