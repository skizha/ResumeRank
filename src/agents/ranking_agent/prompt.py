def build_ranking_prompt(resumes: list[dict], job: dict) -> str:
    return f"""You are an expert technical recruiter AI. Analyze the following resumes against the job description and provide structured scoring.

## Job Description
- **Title**: {job['title']}
- **Description**: {job['description']}
- **Required Skills**: {', '.join(job['required_skills'])}
- **Preferred Skills**: {', '.join(job['preferred_skills'])}
- **Experience Level Required**: {job['experience_level']}

## Candidates to Evaluate
{_format_resumes(resumes)}

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


def _format_resumes(resumes: list[dict]) -> str:
    parts = []
    for i, r in enumerate(resumes, 1):
        parts.append(
            f"""### Candidate {i} (resume_id: {r['resume_id']})
- **Name**: {r['candidate_name']}
- **Skills**: {', '.join(r['skills']) if r['skills'] else 'Not specified'}
- **Experience Level**: {r.get('experience_level', 'Unknown')}
- **Summary**: {r.get('summary', 'No summary available')[:300]}
"""
        )
    return "\n".join(parts)
