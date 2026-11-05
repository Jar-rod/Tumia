#!/bin/bash
# Creates the local S3 bucket and SQS queues the API uses. Runs inside the LocalStack container on start-up.
# Names follow the AWS ones, with "local-" in place of the environment prefix ("prod-").
set -euo pipefail

BUCKET=local-tumiia-documents
QUEUE=local-document-analysis
DLQ=local-document-analysis-dlq

# Documents bucket: versioned, like the real one, so an original can never be silently replaced.
awslocal s3api create-bucket --bucket "$BUCKET" --create-bucket-configuration LocationConstraint=af-south-1
awslocal s3api put-bucket-versioning --bucket "$BUCKET" --versioning-configuration Status=Enabled

# The browser uploads and downloads directly with pre-signed URLs, so the Vite dev server's origin is allowed.
awslocal s3api put-bucket-cors --bucket "$BUCKET" --cors-configuration '{
  "CORSRules": [{
    "AllowedOrigins": ["http://localhost:5173"],
    "AllowedMethods": ["PUT", "GET"],
    "AllowedHeaders": ["*"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3000
  }]
}'

# Analysis queue with a dead-letter queue: a message that fails 3 times moves to the DLQ.
awslocal sqs create-queue --queue-name "$DLQ"
DLQ_ARN=$(awslocal sqs get-queue-attributes --queue-url "$(awslocal sqs get-queue-url --queue-name "$DLQ" --query QueueUrl --output text)" \
  --attribute-names QueueArn --query Attributes.QueueArn --output text)
awslocal sqs create-queue --queue-name "$QUEUE" --attributes "{\"VisibilityTimeout\":\"300\",\"RedrivePolicy\":\"{\\\"deadLetterTargetArn\\\":\\\"$DLQ_ARN\\\",\\\"maxReceiveCount\\\":\\\"3\\\"}\"}"

echo "Created bucket $BUCKET and queues $QUEUE, $DLQ"
