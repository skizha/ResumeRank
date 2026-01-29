"""Core ranking logic for AWS Lambda."""

from shared.bedrock_client import BedrockClient


class RankingAgent:
    """Ranks resumes against job descriptions using AWS Bedrock."""

    def __init__(self, bedrock_client: BedrockClient | None = None):
        """Initialize the ranking agent.

        Args:
            bedrock_client: Bedrock client instance. Created from env vars if not provided.
        """
        self._bedrock = bedrock_client or BedrockClient()

    def rank(self, resumes: list[dict], job: dict) -> list[dict]:
        """Rank resumes against a job description.

        Args:
            resumes: List of resume data dictionaries with keys:
                - resume_id: int
                - candidate_name: str
                - skills: list[str]
                - experience_level: str | None
                - summary: str | None
            job: Job description dictionary with keys:
                - job_id: str
                - title: str
                - description: str
                - required_skills: list[str]
                - preferred_skills: list[str]
                - experience_level: str

        Returns:
            List of ranking dictionaries with keys:
                - resume_id: int
                - job_id: str
                - skill_match_score: float
                - experience_match_score: float
                - overall_score: float
                - summary: str
        """
        prompt = self._build_ranking_prompt(resumes, job)
        parsed = self._bedrock.invoke_json(prompt, max_tokens=4096)

        rankings = []
        for item in parsed.get("rankings", []):
            rankings.append({
                "resume_id": item["resume_id"],
                "job_id": job["job_id"],
                "skill_match_score": round(item["skill_match_score"], 1),
                "experience_match_score": round(item["experience_match_score"], 1),
                "overall_score": round(item["overall_score"], 1),
                "summary": item["summary"],
            })

        return rankings

    def _build_ranking_prompt(self, resumes: list[dict], job: dict) -> str:
        """Build the ranking prompt for the LLM.

        Args:
            resumes: List of resume data dictionaries.
            job: Job description dictionary.

        Returns:
            Formatted prompt string.
        """
        return f"""You are an expert technical recruiter AI. Analyze the following resumes against the job description and provide structured scoring.

## Job Description
- **Title**: {job['title']}
- **Description**: {job['description']}
- **Required Skills**: {', '.join(job['required_skills'])}
- **Preferred Skills**: {', '.join(job['preferred_skills'])}
- **Experience Level Required**: {job['experience_level']}

## Candidates to Evaluate
{self._format_resumes(resumes)}

## Scoring Instructions
For each candidate, provide:
1. **skill_match_score** (0-100): How well the candidate's skills match the required and preferred skills. Weight required skills more heavily (70%) than preferred skills (30%).
2. **experience_match_score** (0-100): How well the candidate's experience level matches the job requirement. Consider:
   - Exact match = 90-100
   - One level above = 80-90
   - One level below = 50-70
   - Two+ levels off = 20-50
3. **overall_score** (0-100): Weighted combination: 60% skill_match + 40% experience_match
4. **summary** (1-2 sentences): Brief assessment of fit, noting key strengths and gaps.

## Response Format
Respond ONLY with valid JSON in this exact structure:
{{
  "rankings": [
    {{
      "resume_id": <integer>,
      "skill_match_score": <float 0-100>,
      "experience_match_score": <float 0-100>,
      "overall_score": <float 0-100>,
      "summary": "<string>"
    }}
  ]
}}

Do not include any text outside the JSON object. Ensure all resume_ids from the input are represented in the output."""

    def _format_resumes(self, resumes: list[dict]) -> str:
        """Format resumes for the prompt.

        Args:
            resumes: List of resume data dictionaries.

        Returns:
            Formatted string of resume information.
        """
        parts = []
        for i, r in enumerate(resumes, 1):
            skills = r.get("skills", [])
            skills_str = ", ".join(skills) if skills else "Not specified"
            summary = r.get("summary", "No summary available") or "No summary available"

            parts.append(
                f"""### Candidate {i} (resume_id: {r['resume_id']})
- **Name**: {r['candidate_name']}
- **Skills**: {skills_str}
- **Experience Level**: {r.get('experience_level', 'Unknown')}
- **Summary**: {summary[:300]}
"""
            )
        return "\n".join(parts)
