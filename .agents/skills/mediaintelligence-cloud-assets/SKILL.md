---
name: mediaintelligence-cloud-assets
description: Use for cloud asset references, staging, S3/GCS selectors, lifecycle policy, assetSelectorJson and cross-cloud routing in DeValpo.MediaIntelligence.
---

# MediaIntelligence Cloud Assets

## Use When

- Working on `assetSelectorJson`, S3/GCS references or staging.
- Checking lifecycle-owned cleanup or cross-cloud staging behavior.

## Read

- `docs/context/AI_ONBOARDING.md`
- `docs/context/ACTIVE_DECISIONS.md`
- `docs/cloud-asset-references-and-staging.md`
- `docs/modules/cloud-assets.md`
- Relevant files under `src/...Application/CloudAssets`.

## Do Not Read

- `secrets/`
- `site/`
- Provider credential guides unless the task is setup-specific.

## Validate

```powershell
dotnet test DeValpo.MediaIntelligence.sln --filter CloudAsset
dotnet build DeValpo.MediaIntelligence.sln
```
