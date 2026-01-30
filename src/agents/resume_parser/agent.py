import json
from pathlib import Path

from anthropic import Anthropic

from shared.config import ANTHROPIC_API_KEY

from .models import ParsedResumeResponse, SuitableRole


class ResumeParserAgent:
    """Parses resume files (PDF, DOCX) into structured data using LLM."""

    def __init__(self):
        self._client = Anthropic(api_key=ANTHROPIC_API_KEY)

    def parse(self, file_path: str) -> ParsedResumeResponse:
        ext = Path(file_path).suffix.lower()
        if ext == ".pdf":
            text = self._extract_pdf(file_path)
        elif ext == ".docx":
            text = self._extract_docx(file_path)
        else:
            raise ValueError(f"Unsupported file type: {ext}")

        return self._extract_structured_data(text, file_path)

    def _extract_pdf(self, file_path: str) -> str:
        import pdfplumber

        text_parts = []
        with pdfplumber.open(file_path) as pdf:
            for page in pdf.pages:
                page_text = page.extract_text()
                if page_text:
                    text_parts.append(page_text)
        return "\n".join(text_parts)

    def _extract_docx(self, file_path: str) -> str:
        from docx import Document

        doc = Document(file_path)
        return "\n".join(para.text for para in doc.paragraphs if para.text.strip())

    def _extract_structured_data(
        self, text: str, file_path: str
    ) -> ParsedResumeResponse:
        if not text.strip():
            return ParsedResumeResponse(
                candidate_name=Path(file_path).stem.replace("_", " "),
                skills=[],
                experience_level="Unknown",
                summary=None,
                suitable_roles=[],
            )

        prompt = f"""Analyze the following resume text and extract structured information.

## Resume Text
{text[:3000]}

## Instructions
Extract the following and respond ONLY with valid JSON:
{{
  "candidate_name": "<full name of the candidate>",
  "skills": ["<list of technical and professional skills mentioned>"],
  "experience_level": "<one of: Junior, Mid, Senior, based on years of experience and role titles>",
  "summary": "<2-3 sentence professional summary of the candidate>",
  "suitable_roles": [
    {{"role": "<job title>", "score": <1-10>}},
    ...
  ]
}}

Rules:
- For candidate_name: Extract the person's full name. It's usually at the top of the resume.
- For skills: List all specific technical skills, tools, frameworks, certifications, and professional competencies mentioned. Be thorough but only include skills explicitly stated.
- For experience_level: Junior = 0-2 years or entry-level titles, Mid = 3-6 years or mid-level titles, Senior = 7+ years or senior/lead/principal titles.
- For summary: Write a brief professional summary based on the resume content.
- For suitable_roles: Based on the candidate's skills and experience, suggest 3-6 job titles/roles they could realistically perform with a suitability score:
  - Score 9-10: Excellent fit - candidate's primary expertise matches this role
  - Score 7-8: Good fit - candidate has strong relevant skills
  - Score 5-6: Moderate fit - candidate could transition with some upskilling
  - Score 3-4: Stretch role - significant skill gaps but transferable experience
  - Score 1-2: Weak fit - minimal alignment
  - Sort roles by score (highest first)

Respond ONLY with the JSON object, no other text."""

        message = self._client.messages.create(
            model="claude-sonnet-4-20250514",
            max_tokens=1024,
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
                raise ValueError("Failed to parse LLM response as JSON")

        # Parse suitable_roles - handle both old (list[str]) and new (list[dict]) formats
        raw_roles = parsed.get("suitable_roles", [])
        suitable_roles = []
        for item in raw_roles:
            if isinstance(item, dict):
                suitable_roles.append(SuitableRole(
                    role=item.get("role", "Unknown"),
                    score=item.get("score", 5)
                ))
            elif isinstance(item, str):
                # Backward compatibility: string roles get a default score
                suitable_roles.append(SuitableRole(role=item, score=5))

        return ParsedResumeResponse(
            candidate_name=parsed.get("candidate_name", Path(file_path).stem),
            skills=parsed.get("skills", []),
            experience_level=parsed.get("experience_level", "Unknown"),
            summary=parsed.get("summary"),
            suitable_roles=suitable_roles,
        )
