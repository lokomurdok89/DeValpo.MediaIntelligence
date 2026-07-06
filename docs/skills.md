# Skills

A skill implements `IMediaAnalysisSkill` and is independent from HTTP, EF Core and client systems.

Product skills:

- `GoogleDocumentAiDocumentOcrSkill`

Mock/demo skills are registered only when `Skills:EnableMockSkills=true`:

- `MockDocumentOcrSkill`
- `MockHandwrittenIdExtractionSkill`
- `MockStampDetectionSkill`
- `MockImageObjectDetectionSkill`
- `MockSportsBibDetectionSkill`
- `MockFaceDetectionSkill`
- `MockVideoClipGenerationSkill`
- `MockLiveVideoBufferAnalysisSkill`

Rules:

- Skill names must be unique.
- Skills declare supported asset types.
- Skills declare required capabilities.
- Skills return normalized fields, labels, entities, clips and issues.
- Real providers stay behind Infrastructure implementations.
- Production configuration must not depend on mock skills.

To add a skill, implement `IMediaAnalysisSkill`, register it in DI, add it to a profile, and test the profile behavior.
