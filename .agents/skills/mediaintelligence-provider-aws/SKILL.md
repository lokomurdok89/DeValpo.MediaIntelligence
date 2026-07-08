---
name: mediaintelligence-provider-aws
description: Use for AWS Rekognition work in DeValpo.MediaIntelligence, including face search, image matching, provider readiness, temporary FaceId cleanup and AWS-specific validation.
---

# MediaIntelligence AWS Provider

## Use When

- Working on AWS Rekognition face search or image matching.
- Checking AWS readiness, credentials plumbing or cleanup behavior.

## Read

- `docs/context/AI_ONBOARDING.md`
- `docs/context/ACTIVE_DECISIONS.md`
- `docs/aws-rekognition-face-search.md`
- `docs/modules/face-search.md`
- Relevant files under `src/...Infrastructure/Providers/Aws/Rekognition`.

## Do Not Read

- `secrets/`
- Mounted AWS credentials.
- `site/`
- Unrelated Google provider docs unless comparing routing.

## Validate

```powershell
dotnet test DeValpo.MediaIntelligence.sln --filter AwsRekognition
powershell -ExecutionPolicy Bypass -File scripts\ci\secret-scan.ps1 -Root .
```
