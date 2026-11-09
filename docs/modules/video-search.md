# Video Search

Video search finds target numbers and generic visual evidence in videos.

## Provider

- Google Video Intelligence for current video target search.
- AWS Rekognition is used by the separate face-search flow.

## Inputs

- Multipart videos for compatibility.
- Cloud references through `assetSelectorJson` for productive flows.

## Output

Results are grouped by `targetKey` and include video time segments.

## Full reference

- [Video target search](../video-target-search.md)
- [Google video configuration](../video-target-search.md)
