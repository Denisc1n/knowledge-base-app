# Build Review

## Goal

Take a development task from problem framing through implementation review and decision support.

## Sequence

1. `planner`
2. `developer`
3. `qa`
4. `guard`
5. `manager`

## Workflow Intent

Use this workflow when you want a structured multi-role review of a feature, refactor, architectural change, or implementation strategy.

## Input

- User request
- Relevant repository context
- Constraints such as cost, time, safety, or architecture rules

## Execution Instructions

- Run the roles in order
- Each role should receive the original request plus the outputs of prior roles
- Preserve section headers from each role's output
- If a role identifies a blocking issue, note it explicitly before continuing
- If a client cannot practically continue, stop and report the current state

## Default Deliverable

Produce a final `manager` summary that includes:

- the proposed path
- key risks
- whether the work is ready to proceed
- what should happen next

## Notes

- This workflow is intentionally human-supervised
- It is client-agnostic and should be usable in Codex, Claude, Cursor, or similar tools
- If stronger consistency is needed later, this workflow can be converted into a structured machine-readable format
