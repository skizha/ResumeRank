from pydantic import BaseModel, Field


class ParseRequest(BaseModel):
    file_path: str


class SuitableRole(BaseModel):
    """A job role with a suitability score."""
    role: str
    score: int = Field(ge=1, le=10, description="Suitability score from 1-10, where 10 is most suitable")


class ParsedResumeResponse(BaseModel):
    candidate_name: str
    skills: list[str]
    experience_level: str | None
    summary: str | None
    suitable_roles: list[SuitableRole]
