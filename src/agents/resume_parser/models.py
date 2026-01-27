from pydantic import BaseModel


class ParseRequest(BaseModel):
    file_path: str


class ParsedResumeResponse(BaseModel):
    candidate_name: str
    skills: list[str]
    experience_level: str | None
    summary: str | None
    suitable_roles: list[str]
