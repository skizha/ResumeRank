from fastapi import FastAPI, HTTPException

from .agent import RankingAgent
from .models import RankRequest, RankResponse

app = FastAPI(title="Ranking Agent", version="1.0.0")
agent = RankingAgent()


@app.post("/rank", response_model=RankResponse)
async def rank_resumes(request: RankRequest) -> RankResponse:
    if not request.resumes:
        raise HTTPException(status_code=400, detail="No resumes provided")
    try:
        rankings = agent.rank(request.resumes, request.job)
        return RankResponse(rankings=rankings)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Ranking failed: {str(e)}")


@app.get("/health")
async def health():
    return {"status": "healthy", "service": "ranking_agent"}


if __name__ == "__main__":
    import uvicorn

    from shared.config import RANKING_AGENT_PORT

    uvicorn.run(
        "ranking_agent.main:app", host="0.0.0.0", port=RANKING_AGENT_PORT, reload=True
    )
