"""S3 client wrapper for resume file operations."""

import os
from io import BytesIO
from typing import BinaryIO

import boto3
from botocore.config import Config
from botocore.exceptions import ClientError


class S3Client:
    """Wrapper for S3 operations on resume files."""

    def __init__(self, bucket_name: str | None = None, region: str | None = None):
        """Initialize the S3 client.

        Args:
            bucket_name: S3 bucket name. Defaults to S3_BUCKET_NAME env var.
            region: AWS region. Defaults to AWS_REGION env var or us-east-1.
        """
        self.bucket_name = bucket_name or os.environ.get("S3_BUCKET_NAME", "")
        self.region = region or os.environ.get("AWS_REGION", "us-east-1")

        if not self.bucket_name:
            raise ValueError("S3 bucket name is required")

        config = Config(retries={"max_attempts": 3, "mode": "adaptive"})

        self._client = boto3.client("s3", region_name=self.region, config=config)

    def download_file(self, s3_key: str) -> bytes:
        """Download a file from S3.

        Args:
            s3_key: The S3 object key (path within the bucket).

        Returns:
            The file contents as bytes.

        Raises:
            FileNotFoundError: If the file doesn't exist.
            RuntimeError: If the download fails.
        """
        try:
            response = self._client.get_object(Bucket=self.bucket_name, Key=s3_key)
            return response["Body"].read()
        except ClientError as e:
            error_code = e.response.get("Error", {}).get("Code", "")
            if error_code == "NoSuchKey":
                raise FileNotFoundError(f"File not found in S3: {s3_key}") from e
            raise RuntimeError(f"Failed to download from S3: {e}") from e

    def download_file_to_stream(self, s3_key: str) -> BinaryIO:
        """Download a file from S3 to a BytesIO stream.

        Args:
            s3_key: The S3 object key (path within the bucket).

        Returns:
            A BytesIO stream containing the file contents.

        Raises:
            FileNotFoundError: If the file doesn't exist.
            RuntimeError: If the download fails.
        """
        content = self.download_file(s3_key)
        return BytesIO(content)

    def upload_file(
        self, s3_key: str, content: bytes, content_type: str | None = None
    ) -> str:
        """Upload a file to S3.

        Args:
            s3_key: The S3 object key (path within the bucket).
            content: The file contents as bytes.
            content_type: Optional MIME type for the file.

        Returns:
            The S3 URI (s3://bucket/key).

        Raises:
            RuntimeError: If the upload fails.
        """
        try:
            extra_args = {}
            if content_type:
                extra_args["ContentType"] = content_type

            self._client.put_object(
                Bucket=self.bucket_name, Key=s3_key, Body=content, **extra_args
            )
            return f"s3://{self.bucket_name}/{s3_key}"
        except ClientError as e:
            raise RuntimeError(f"Failed to upload to S3: {e}") from e

    def delete_file(self, s3_key: str) -> None:
        """Delete a file from S3.

        Args:
            s3_key: The S3 object key (path within the bucket).

        Raises:
            RuntimeError: If the deletion fails.
        """
        try:
            self._client.delete_object(Bucket=self.bucket_name, Key=s3_key)
        except ClientError as e:
            raise RuntimeError(f"Failed to delete from S3: {e}") from e

    def file_exists(self, s3_key: str) -> bool:
        """Check if a file exists in S3.

        Args:
            s3_key: The S3 object key (path within the bucket).

        Returns:
            True if the file exists, False otherwise.
        """
        try:
            self._client.head_object(Bucket=self.bucket_name, Key=s3_key)
            return True
        except ClientError:
            return False

    def generate_presigned_url(
        self, s3_key: str, expiration: int = 3600, operation: str = "get_object"
    ) -> str:
        """Generate a presigned URL for S3 operations.

        Args:
            s3_key: The S3 object key (path within the bucket).
            expiration: URL expiration time in seconds (default: 1 hour).
            operation: S3 operation ('get_object' or 'put_object').

        Returns:
            The presigned URL.

        Raises:
            RuntimeError: If URL generation fails.
        """
        try:
            url = self._client.generate_presigned_url(
                ClientMethod=operation,
                Params={"Bucket": self.bucket_name, "Key": s3_key},
                ExpiresIn=expiration,
            )
            return url
        except ClientError as e:
            raise RuntimeError(f"Failed to generate presigned URL: {e}") from e

    @staticmethod
    def parse_s3_uri(s3_uri: str) -> tuple[str, str]:
        """Parse an S3 URI into bucket and key.

        Args:
            s3_uri: S3 URI in format s3://bucket/key.

        Returns:
            Tuple of (bucket_name, key).

        Raises:
            ValueError: If the URI format is invalid.
        """
        if not s3_uri.startswith("s3://"):
            raise ValueError(f"Invalid S3 URI format: {s3_uri}")

        path = s3_uri[5:]  # Remove 's3://'
        parts = path.split("/", 1)
        if len(parts) != 2:
            raise ValueError(f"Invalid S3 URI format: {s3_uri}")

        return parts[0], parts[1]
