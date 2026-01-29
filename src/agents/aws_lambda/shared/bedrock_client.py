"""AWS Bedrock client wrapper for Claude model invocation."""

import json
import os
from typing import Any

import boto3
from botocore.config import Config


class BedrockClient:
    """Wrapper for AWS Bedrock Runtime to invoke Claude models."""

    def __init__(self, model_id: str | None = None, region: str | None = None):
        """Initialize the Bedrock client.

        Args:
            model_id: Bedrock model ID. Defaults to BEDROCK_MODEL_ID env var.
            region: AWS region. Defaults to AWS_REGION env var or us-east-1.
        """
        self.model_id = model_id or os.environ.get(
            "BEDROCK_MODEL_ID", "anthropic.claude-3-sonnet-20240229-v1:0"
        )
        self.region = region or os.environ.get("AWS_REGION", "us-east-1")

        # Configure retry behavior
        config = Config(
            retries={"max_attempts": 3, "mode": "adaptive"},
            read_timeout=120,
            connect_timeout=10,
        )

        self._client = boto3.client(
            "bedrock-runtime", region_name=self.region, config=config
        )

    def invoke(
        self,
        prompt: str,
        max_tokens: int = 4096,
        temperature: float = 0.0,
        system: str | None = None,
    ) -> str:
        """Invoke the Claude model with a prompt.

        Args:
            prompt: The user message/prompt to send.
            max_tokens: Maximum tokens in the response.
            temperature: Sampling temperature (0.0-1.0).
            system: Optional system prompt.

        Returns:
            The model's response text.

        Raises:
            RuntimeError: If the model invocation fails.
        """
        messages = [{"role": "user", "content": prompt}]

        body = {
            "anthropic_version": "bedrock-2023-05-31",
            "max_tokens": max_tokens,
            "messages": messages,
            "temperature": temperature,
        }

        if system:
            body["system"] = system

        try:
            response = self._client.invoke_model(
                modelId=self.model_id,
                body=json.dumps(body),
                contentType="application/json",
                accept="application/json",
            )

            response_body = json.loads(response["body"].read())

            # Extract text from the response
            if "content" in response_body and len(response_body["content"]) > 0:
                return response_body["content"][0]["text"]
            else:
                raise RuntimeError("No content in Bedrock response")

        except self._client.exceptions.ThrottlingException as e:
            raise RuntimeError(f"Bedrock rate limit exceeded: {e}") from e
        except self._client.exceptions.ModelTimeoutException as e:
            raise RuntimeError(f"Bedrock model timeout: {e}") from e
        except Exception as e:
            raise RuntimeError(f"Bedrock invocation failed: {e}") from e

    def invoke_json(
        self,
        prompt: str,
        max_tokens: int = 4096,
        temperature: float = 0.0,
        system: str | None = None,
    ) -> dict[str, Any]:
        """Invoke the model and parse the response as JSON.

        Args:
            prompt: The user message/prompt to send.
            max_tokens: Maximum tokens in the response.
            temperature: Sampling temperature (0.0-1.0).
            system: Optional system prompt.

        Returns:
            The parsed JSON response.

        Raises:
            ValueError: If the response cannot be parsed as JSON.
            RuntimeError: If the model invocation fails.
        """
        response_text = self.invoke(prompt, max_tokens, temperature, system)

        try:
            return json.loads(response_text)
        except json.JSONDecodeError:
            # Try to extract JSON from the response
            start = response_text.find("{")
            end = response_text.rfind("}") + 1
            if start >= 0 and end > start:
                return json.loads(response_text[start:end])
            raise ValueError(f"Failed to parse response as JSON: {response_text[:200]}")
