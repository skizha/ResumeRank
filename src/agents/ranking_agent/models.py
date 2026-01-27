from pydantic import BaseModel


class ResumeData(BaseModel):
    resume_id: int
    candidate_name: str
    skills: list[str]
    experience_level: str | None
    summary: str | None


class JobData(BaseModel):
    job_id: str
    title: str
    description: str
    required_skills: list[str]
    preferred_skills: list[str]
    experience_level: str


class RankRequest(BaseModel):
    resumes: list[ResumeData]
    job: JobData


class RankingScore(BaseModel):
    resume_id: int
    job_id: str
    skill_match_score: float
    experience_match_score: float
    overall_score: float
    summary: str


class RankResponse(BaseModel):
    rankings: list[RankingScore]
