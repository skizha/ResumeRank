# Python Agents

AI agents for resume parsing and ranking.

## Structure

- `resume_parser/` - Extracts structured data from resume files (PDF, DOCX)
- `ranking_agent/` - Scores resumes against job requirements
- `shared/` - Common configuration and utilities

## Setup

```
python -m venv .venv
source .venv/bin/activate  # Linux/Mac
.venv\Scripts\activate     # Windows
pip install -r requirements.txt
```

## Testing

```
pytest tests/agents/ -v
```

## Conventions

- Each agent is a Python package with an `agent.py` entry point
- Agent classes expose a single public method for their core operation
- Use `shared/config.py` for cross-agent configuration
- Type hints required on all public methods
- Tests use pytest and live in `tests/agents/`
