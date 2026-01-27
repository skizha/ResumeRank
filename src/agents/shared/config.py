import os

ANTHROPIC_API_KEY = os.environ.get("ANTHROPIC_API_KEY", "")
RESUME_PARSER_PORT = int(os.environ.get("RESUME_PARSER_PORT", "5100"))
RANKING_AGENT_PORT = int(os.environ.get("RANKING_AGENT_PORT", "5101"))
