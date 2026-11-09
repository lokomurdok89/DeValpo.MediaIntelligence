# Configurable Image Search — Later Bib Pilot

## Direction

Make the search target configurable rather than tying the product to sports bibs.
Keep facial recognition as a separate supported capability. The first product
experience is [facial video search](s3-video-face-search-map.md); bib photos come
after it. Words and codes remain proposed extensions, not implemented query types.

## Existing Foundation

- Image batch search supports visible numbers and face-reference workflows.
- Google Vision provides image OCR; AWS Rekognition provides identity-oriented
  face matching. Generic Google face detection does not identify a person.
- Productive inputs use structured S3/GCS references. `targetKey` remains the
  canonical target identifier and provider SDK models stay behind adapters.
- Keep the existing API, Application, Domain, Contracts and Infrastructure layers.

## Proposed Search Behavior

| Search | Intended behavior | Status |
| --- | --- | --- |
| Numbers / bibs | Find the requested visible number in media | Existing number-search foundation; bib-specific experience still to develop |
| Words | Find configured text with explicit normalization | Proposed extension |
| Codes | Match a configured alphanumeric identifier without losing meaningful zeros or separators | Proposed extension |
| Reference face | Return similarity-based candidates for an authorized reference | Existing AWS capability |

A completed search without findings returns an empty match collection. Preserve
errors/issues separately. No generic search-engine refactor or new DTO is approved
by this document; extend existing behavior only when the concrete journey needs it.

## Retained Sequence Diagrams

Repository-local HTML diagrams and their JSON sources remain under `.archify/`:

- `.archify/sequence-busqueda-configurable-20261109-124422/alta/alta.html`
- `.archify/sequence-busqueda-configurable-20261109-124422/procesamiento/procesamiento.html`
- `.archify/sequence-busqueda-configurable-20261109-124422/facial/facial.html`

They describe proposed admission/OCR behavior and the image facial flow, not a
recorded cloud execution. The image facial flow indexes source-image faces;
the video facial flow indexes references. Some annotations reflect earlier
planning constraints, not current demo prerequisites. Superseded phase plans
and earlier bib-only diagram versions have been removed; only these current
design references are retained.

No phase schedule, QA matrix, certification gate or benchmark campaign is part
of this scope. Reuse [saved evidence](../examples/video-result-evidence.md);
do not generate new cloud runs merely to demonstrate existing results.
