import json

from anthropic import Anthropic

from shared.config import ANTHROPIC_API_KEY

from .models import JobData, RankingScore, ResumeData
from .prompt import build_ranking_prompt


class RankingAgent:
    """Ranks resumes against job descriptions using Claude API."""

    def __init__(self):
        self._client = Anthropic(api_key=ANTHROPIC_API_KEY)

    def rank(self, resumes: list[ResumeData], job: JobData) -> list[RankingScore]:
        resumes_dict = [r.model_dump() for r in resumes]
        job_dict = job.model_dump()

        prompt = build_ranking_prompt(resumes_dict, job_dict)

        message = self._client.messages.create(
            model="claude-sonnet-4-20250514",
            max_tokens=4096,
            messages=[{"role": "user", "content": prompt}],
        )

        response_text = message.content[0].text

        try:
            parsed = json.loads(response_text)
        except json.JSONDecodeError:
            # Fallback: try to extract JSON from response
            start = response_text.find("{")
            end = response_text.rfind("}") + 1
            if start >= 0 and end > start:
                parsed = json.loads(response_text[start:end])
            else:
                raise ValueError("Failed to parse ranking response as JSON")

        rankings = []
        for item in parsed["rankings"]:
            rankings.append(
                RankingScore(
                    resume_id=item["resume_id"],
                    job_id=job.job_id,
                    skill_match_score=round(item["skill_match_score"], 1),
                    experience_match_score=round(item["experience_match_score"], 1),
                    overall_score=round(item["overall_score"], 1),
                    summary=item["summary"],
                )
            )

        return rankings
