import os

from fastapi import FastAPI, HTTPException

from .agent import ResumeParserAgent
from .models import ParseRequest, ParsedResumeResponse

app = FastAPI(title="Resume Parser Agent", version="1.0.0")
agent = ResumeParserAgent()


@app.post("/parse", response_model=ParsedResumeResponse)
async def parse_resume(request: ParseRequest) -> ParsedResumeResponse:
    if not os.path.exists(request.file_path):
        raise HTTPException(
            status_code=404, detail=f"File not found: {request.file_path}"
        )
    try:
        return agent.parse(request.file_path)
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Parsing failed: {str(e)}")


@app.get("/health")
async def health():
    return {"status": "healthy", "service": "resume_parser"}


if __name__ == "__main__":
    import uvicorn

    from shared.config import RESUME_PARSER_PORT

    uvicorn.run(
        "resume_parser.main:app", host="0.0.0.0", port=RESUME_PARSER_PORT, reload=True
    )
