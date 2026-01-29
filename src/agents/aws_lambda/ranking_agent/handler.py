"""AWS Lambda handler for Ranking Agent."""

import json
import logging

from ranker import RankingAgent

# Configure logging
logger = logging.getLogger()
logger.setLevel(logging.INFO)


def lambda_handler(event: dict, context) -> dict:
    """AWS Lambda entry point for resume ranking.

    Handles both API Gateway proxy events and direct Lambda invocations.

    Args:
        event: Lambda event containing the request.
        context: Lambda context object.

    Returns:
        API Gateway response format with ranking results.
    """
    logger.info(f"Received event: {json.dumps(event)}")

    # Handle health check
    if event.get("rawPath") == "/rank/health" or event.get("path") == "/rank/health":
        return _response(200, {"status": "healthy", "service": "ranking-agent"})

    try:
        # Parse request body from API Gateway or direct invocation
        body = _parse_request_body(event)

        if not body:
            return _response(400, {"error": "Request body is required"})

        # Validate required fields
        resumes = body.get("resumes")
        job = body.get("job")

        if not resumes:
            return _response(400, {"error": "resumes field is required"})
        if not job:
            return _response(400, {"error": "job field is required"})

        # Validate resumes format
        if not isinstance(resumes, list) or len(resumes) == 0:
            return _response(400, {"error": "resumes must be a non-empty list"})

        for resume in resumes:
            if "resume_id" not in resume:
                return _response(400, {"error": "Each resume must have a resume_id"})
            if "candidate_name" not in resume:
                return _response(400, {"error": "Each resume must have a candidate_name"})

        # Validate job format
        required_job_fields = ["job_id", "title", "description", "required_skills", "preferred_skills", "experience_level"]
        for field in required_job_fields:
            if field not in job:
                return _response(400, {"error": f"job.{field} is required"})

        logger.info(f"Ranking {len(resumes)} resumes for job: {job.get('title')}")

        # Rank the resumes
        agent = RankingAgent()
        rankings = agent.rank(resumes, job)

        logger.info(f"Successfully ranked {len(rankings)} resumes")
        return _response(200, {"rankings": rankings})

    except ValueError as e:
        logger.error(f"Validation error: {e}")
        return _response(400, {"error": str(e)})
    except Exception as e:
        logger.exception(f"Unexpected error: {e}")
        return _response(500, {"error": f"Internal server error: {str(e)}"})


def _parse_request_body(event: dict) -> dict | None:
    """Parse the request body from various event formats.

    Args:
        event: Lambda event object.

    Returns:
        Parsed body as a dictionary, or None if no body.
    """
    # API Gateway HTTP API v2 format
    if "body" in event:
        body = event["body"]
        if body is None:
            return None
        if isinstance(body, str):
            # Check if base64 encoded
            if event.get("isBase64Encoded"):
                import base64
                body = base64.b64decode(body).decode("utf-8")
            return json.loads(body)
        return body

    # Direct Lambda invocation with payload
    if "resumes" in event and "job" in event:
        return event

    return None


def _response(status_code: int, body: dict) -> dict:
    """Create an API Gateway response.

    Args:
        status_code: HTTP status code.
        body: Response body dictionary.

    Returns:
        API Gateway response format.
    """
    return {
        "statusCode": status_code,
        "headers": {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
            "Access-Control-Allow-Headers": "Content-Type,Authorization",
        },
        "body": json.dumps(body),
    }
