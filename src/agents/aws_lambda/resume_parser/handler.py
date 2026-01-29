"""AWS Lambda handler for Resume Parser."""

import json
import logging
import os

from parser import ResumeParser

# Configure logging
logger = logging.getLogger()
logger.setLevel(logging.INFO)


def lambda_handler(event: dict, context) -> dict:
    """AWS Lambda entry point for resume parsing.

    Handles both API Gateway proxy events and direct Lambda invocations.

    Args:
        event: Lambda event containing the request.
        context: Lambda context object.

    Returns:
        API Gateway response format with parsed resume data.
    """
    logger.info(f"Received event: {json.dumps(event)}")

    # Handle health check
    if event.get("rawPath") == "/parse/health" or event.get("path") == "/parse/health":
        return _response(200, {"status": "healthy", "service": "resume-parser"})

    try:
        # Parse request body from API Gateway or direct invocation
        body = _parse_request_body(event)

        if not body:
            return _response(400, {"error": "Request body is required"})

        file_path = body.get("file_path")
        if not file_path:
            return _response(400, {"error": "file_path is required"})

        logger.info(f"Parsing resume: {file_path}")

        # Parse the resume
        parser = ResumeParser()
        result = parser.parse(file_path)

        logger.info(f"Successfully parsed resume for: {result.get('candidate_name')}")
        return _response(200, result)

    except FileNotFoundError as e:
        logger.error(f"File not found: {e}")
        return _response(404, {"error": str(e)})
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
    if "file_path" in event:
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
