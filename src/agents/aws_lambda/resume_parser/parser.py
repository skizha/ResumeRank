"""Core resume parsing logic for AWS Lambda."""

import json
import os
import tempfile
from pathlib import Path
from typing import BinaryIO

from shared.bedrock_client import BedrockClient
from shared.s3_client import S3Client


class ResumeParser:
    """Parses resume files from S3 into structured data using AWS Bedrock."""

    def __init__(self, s3_client: S3Client | None = None, bedrock_client: BedrockClient | None = None):
        """Initialize the parser.

        Args:
            s3_client: S3 client instance. Created from env vars if not provided.
            bedrock_client: Bedrock client instance. Created from env vars if not provided.
        """
        self._s3 = s3_client or S3Client()
        self._bedrock = bedrock_client or BedrockClient()

    def parse(self, s3_key: str) -> dict:
        """Parse a resume file from S3.

        Args:
            s3_key: S3 object key or full S3 URI (s3://bucket/key).

        Returns:
            Parsed resume data as a dictionary.

        Raises:
            ValueError: If the file type is unsupported.
            FileNotFoundError: If the file doesn't exist in S3.
        """
        # Handle both S3 URI and plain key formats
        if s3_key.startswith("s3://"):
            _, s3_key = S3Client.parse_s3_uri(s3_key)

        ext = Path(s3_key).suffix.lower()
        if ext not in (".pdf", ".docx"):
            raise ValueError(f"Unsupported file type: {ext}")

        # Download file from S3
        file_stream = self._s3.download_file_to_stream(s3_key)

        # Extract text based on file type
        if ext == ".pdf":
            text = self._extract_pdf(file_stream)
        else:
            text = self._extract_docx(file_stream)

        # Use LLM to extract structured data
        return self._extract_structured_data(text, s3_key)

    def _extract_pdf(self, file_stream: BinaryIO) -> str:
        """Extract text from a PDF file stream.

        Args:
            file_stream: PDF file as a binary stream.

        Returns:
            Extracted text content.
        """
        import pdfplumber

        text_parts = []
        with pdfplumber.open(file_stream) as pdf:
            for page in pdf.pages:
                page_text = page.extract_text()
                if page_text:
                    text_parts.append(page_text)
        return "\n".join(text_parts)

    def _extract_docx(self, file_stream: BinaryIO) -> str:
        """Extract text from a DOCX file stream.

        Args:
            file_stream: DOCX file as a binary stream.

        Returns:
            Extracted text content.
        """
        from docx import Document

        doc = Document(file_stream)
        return "\n".join(para.text for para in doc.paragraphs if para.text.strip())

    def _extract_structured_data(self, text: str, file_path: str) -> dict:
        """Use LLM to extract structured resume data.

        Args:
            text: Raw text extracted from the resume.
            file_path: Original file path (for fallback name extraction).

        Returns:
            Parsed resume data dictionary.
        """
        if not text.strip():
            return {
                "candidate_name": Path(file_path).stem.replace("_", " "),
                "skills": [],
                "experience_level": "Unknown",
                "summary": None,
                "suitable_roles": [],
            }

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
  "suitable_roles": ["<list of job roles this candidate could perform based on their skills and experience>"]
}}

Rules:
- For candidate_name: Extract the person's full name. It's usually at the top of the resume.
- For skills: List all specific technical skills, tools, frameworks, certifications, and professional competencies mentioned. Be thorough but only include skills explicitly stated.
- For experience_level: Junior = 0-2 years or entry-level titles, Mid = 3-6 years or mid-level titles, Senior = 7+ years or senior/lead/principal titles.
- For summary: Write a brief professional summary based on the resume content.
- For suitable_roles: Based on the candidate's skills and experience, suggest 3-6 job titles/roles they could realistically perform. Consider adjacent roles (e.g., someone with AWS + Docker experience could be a Cloud Engineer, DevOps Engineer, or Infrastructure Engineer). Include both their current role type and transferable roles.

Respond ONLY with the JSON object, no other text."""

        parsed = self._bedrock.invoke_json(prompt, max_tokens=1024)

        return {
            "candidate_name": parsed.get("candidate_name", Path(file_path).stem),
            "skills": parsed.get("skills", []),
            "experience_level": parsed.get("experience_level", "Unknown"),
            "summary": parsed.get("summary"),
            "suitable_roles": parsed.get("suitable_roles", []),
        }
