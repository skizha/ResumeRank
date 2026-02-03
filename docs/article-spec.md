# LinkedIn Article Spec: ResumeRank

## Meta

| Field | Value |
|-------|-------|
| **Target Audience** | Developers, Recruiters |
| **Angle** | Tech deep-dive |
| **Tone** | Conversational |
| **Length** | Medium (1000-1200 words) |
| **Status** | Draft |
| **Style** | It should be written like human |


---

## Hook / Opening Story

<!-- Your past experience without LLM - fill this in -->

| Field | Your Input |
|-------|------------|
| **Year** | 2015 |
| **Technology/Approach Used** | NLTK, Vectorizing, Nueral Network, Knowledge Graph, Chunking |
| **How It Worked** | We had company that scraps job descriptions(JDs). Which is used to create a Nueral network of knowldge base of JDs. This is used to rank resumes agianst a job description.
The project used chunking, NLTK and grammar. It took 6 months to create it. A week worth JDs took, 2 weeks to update the language model |
| **Pain Points** | Long time in training. Cleaning the jds, appropriate chunking, biases if many jds are same |

---

## Problem Statement

- Manual resume screening is time-consuming
- Keyword matching misses context
- Hard to identify transferable skills
- Scaling is difficult

---

## Solution Overview

### Agent 1: Resume Parser
- Input: PDF/DOCX resume
- Output: Structured data (skills, experience, education)
- Bonus: Identifies suitable roles with suitability scores (1-10)

### Agent 2: Ranking Agent
- Input: Parsed resumes + job requirements
- Output: Ranked candidates with scores
- Scoring: Skill match, experience relevance, overall fit

---

## Technical Details

### Architecture
```
Resume (PDF/DOCX) → Parser Agent → Structured Data (S3)
                                          ↓
Job Description ──────→ Ranking Agent → Ranked Results
```

### Tech Stack
| Component | Technology |
|-----------|------------|
| Frontend | .NET 8 Razor Pages |
| AI Agents | Python + AWS Bedrock (Claude) |
| Compute | AWS Lambda |
| Storage | S3 |
| API | API Gateway |
| IaC | Terraform |

### Why Serverless/Bedrock
- No model hosting
- Pay-per-use scaling
- No GPU provisioning

---

## Key Insights / Surprises

<!-- What unexpected results or learnings did you have? -->

- Suitable roles feature emerged organically
- AI identifies cross-functional fit (e.g., Backend Dev → DevOps)
- Prompt engineering was the hardest part

---

## Value Proposition

### For Recruiters
- First-pass automation
- Time savings: Days of scanning the resumes → minutes in filtering (adjust with your numbers)
- Talent pooling via suitable roles

### For Developers
- Replicable architecture
- Key challenge: consistent structured output
- Tips: JSON mode, few-shot examples

---

## Call to Action

<!-- Choose one or more -->

- [*] Check out GitHub repo: [URL]
- [ ] Share your experience with AI in hiring
- [ ] Connect for discussion
- [ ] Other:

---

## Assets to Include

- [*] Screenshot of ranking results
- [*] Screenshot of suitable roles
- [*] Architecture diagram
- [*] Code snippet (optional)

---

## Your Notes
Add a note that the code is written using AI
<!-- Any specific anecdotes, phrases, or points you want included -->




---

## Review Checklist

- [ ] Hook story filled in
- [ ] Metrics/numbers added (if available)
- [ ] CTA decided
- [ ] Assets prepared
- [ ] Ready for final draft
