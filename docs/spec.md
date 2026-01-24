# ResumeRank Web Application Specification

## Overview

A web application that displays pre-defined job descriptions and allows users to upload resumes to be parsed and ranked against those jobs using AI agents.

## Core Features

### 1. Job Descriptions

- 10 pre-defined job types loaded from a JSON configuration file (`jobs.json`)
- Displayed as a list on the home page
- Each entry shows: title, department, experience level, resume count
- No create/edit/delete — jobs are maintained in the config file

#### Pre-defined Jobs
1. Registered Nurse (Healthcare)
2. Marketing Manager (Marketing)
3. Financial Analyst (Finance)
4. Software Engineer (Technology)
5. Human Resources Generalist (Human Resources)
6. Project Manager (Operations)
7. Sales Representative (Sales)
8. Graphic Designer (Creative)
9. Data Analyst (Business Intelligence)
10. Operations Coordinator (Operations)

### 2. Resume Management

#### 2.1 Upload Resumes
- Upload one or more resume files to a specific job description
- Supported formats: PDF, DOCX
- Max file size: 10 MB per file
- On upload:
  1. File is stored.
  2. Resume parser agent is invoked to extract structured data. 
  3. Parsed data is saved alongside the file reference

#### 2.2 View Uploaded Resumes
- List all resumes uploaded to a job description
- Each entry shows: candidate name (extracted), file name, upload date, rank score (if ranked)
- Sort by: upload date, rank score, candidate name

#### 2.3 Delete Resume
- Remove a resume from a job description
- Confirmation prompt before deletion

### 3. Ranking

#### 3.1 Rank Resumes
- Trigger ranking for all uploaded resumes against the job description
- Invokes the ranking agent with parsed resume data and job requirements
- Displays progress indicator during ranking

#### 3.2 View Rankings
- Display ranked list of resumes for a job description
- Each entry shows:
  - Rank position
  - Candidate name
  - Overall score (0-100)
  - Skill match breakdown
  - Experience match indicator
- Sort by rank score (descending) by default
- Option to re-rank after uploading new resumes

## Pages

| Page | Route | Description |
|------|-------|-------------|
| Home | `/` | List of all pre-defined jobs with resume counts |
| Job Detail | `/Jobs/{id}` | View job details, uploaded resumes, and rankings |
| Resume Upload | `/Jobs/{id}/Upload` | Upload resumes to a job |

## Data Models

### JobDescription (loaded from `jobs.json`)
| Field | Type | Notes |
|-------|------|-------|
| Id | string | Unique identifier (slug) |
| Title | string | Job title |
| Department | string | Department name |
| Description | string | Full job description |
| RequiredSkills | string[] | List of required skills |
| PreferredSkills | string[] | List of preferred skills |
| ExperienceLevel | enum | Entry, Mid, Senior, Lead |
| Location | string | Optional |

### Resume
| Field | Type | Notes |
|-------|------|-------|
| Id | int | Primary key |
| JobId | string | References JobDescription.Id |
| FileName | string | Original file name |
| FilePath | string | Storage path |
| CandidateName | string | Extracted by parser agent |
| ParsedData | JSON | Structured data from parser |
| UploadedAt | DateTime | Auto-set |

### RankingResult
| Field | Type | Notes |
|-------|------|-------|
| Id | int | Primary key |
| ResumeId | int | Foreign key |
| JobId | string | References JobDescription.Id |
| OverallScore | decimal | 0-100 |
| SkillMatchScore | decimal | 0-100 |
| ExperienceMatchScore | decimal | 0-100 |
| Summary | string | AI-generated summary |
| RankedAt | DateTime | Auto-set |

## Agent Integration

### Resume Parser Agent
- **Input**: Resume file path
- **Output**: Structured JSON with candidate name, skills, experience, education
- **Trigger**: On resume upload

### Ranking Agent
- **Input**: List of parsed resumes, job description requirements
- **Output**: Scored and ranked list with breakdowns
- **Trigger**: User clicks "Rank Resumes" on job detail page

## Non-Functional Requirements

- Job descriptions loaded from `src/ResumeRank.Web/Data/jobs.json` at startup
- File uploads stored on local disk (configurable path)
- SQLite database for resumes and ranking results
- Agent communication via HTTP API or direct process invocation
- Responsive UI using Bootstrap (included with template)
