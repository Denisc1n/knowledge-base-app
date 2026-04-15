# Codex Adapter

## Purpose

Use the portable agent definitions in `docs/agents` from a Codex session without changing the source role and workflow files.

## Suggested Invocation Pattern

Tell Codex:

1. which workflow to run
2. what task or goal to analyze
3. that the workflow definitions live under `docs/agents`

Example:

```text
Run the `build-review` workflow from `docs/agents/workflows/build-review.md` for this task: <task>.
Use the role definitions under `docs/agents/roles`.
Follow the workflow in sequence and preserve each role's output sections.
```

## Guidance

- Keep the portable role and workflow documents as the source of truth
- Use Codex-specific capabilities only as execution help, not as part of the role definitions
- If Codex needs to summarize intermediate steps, preserve the role boundaries clearly

## Do Not

- Rewrite the core role files to depend on Codex-only features
- Assume Codex-exclusive tool semantics in the portable specs
