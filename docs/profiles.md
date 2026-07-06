# Profiles

A profile is a business configuration for a client or use case.

Initial profiles:

- `arenysmed-delivery-evidence-v1`
- `sports-event-clips-v1`
- `generic-document-extraction-v1`
- `invoice-extraction-v1`

Profiles define:

- `profileKey`
- `clientKey`
- supported asset types
- ordered skill names
- confidence threshold
- human review rule
- description

Human review is created when confidence is below the profile threshold or orchestration returns an error issue.
