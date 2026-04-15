# Claude Adapter

## Purpose

Use the portable agent definitions in `docs/agents` from a Claude session without changing the source role and workflow files.

## Suggested Invocation Pattern

Tell Claude:

1. which workflow to run
2. where the workflow and roles are stored
3. that the outputs should preserve the role-defined structure

Example:

```text
Run the `build-review` workflow from `docs/agents/workflows/build-review.md` for this task: <task>.
Use the role definitions under `docs/agents/roles`.
Execute the roles in order and preserve each role's required output sections.
```

## Guidance

- Keep the role and workflow documents portable
- Use Claude-specific strengths during execution, but do not encode them into the shared spec
- If Claude compresses context, make sure each role's core conclusions are still preserved

## Do Not

- Depend on Claude-only features in the shared role definitions
- Let execution convenience drift the shared workflow away from portability
