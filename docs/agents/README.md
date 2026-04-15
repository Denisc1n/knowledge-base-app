# Agent System

This directory contains a client-agnostic agent orchestration starter pack for development workflows.

The goal is to define portable role and workflow specifications that can be used with different AI clients such as Codex, Claude, Cursor, or future tools.

## Design Principles

- Keep the core definitions client-agnostic
- Define roles and workflows in plain Markdown
- Use templates to make outputs more consistent across clients
- Keep client-specific behavior in thin adapter documents
- Treat this directory as the source of truth for agent collaboration rules

## Structure

```text
docs/agents/
  roles/
  workflows/
  templates/
  adapters/
```

## Usage Model

1. Choose a workflow.
2. Load the referenced role definitions.
3. Execute the roles in sequence in your AI client.
4. Pass the previous role's output into the next role.
5. Produce the final deliverable defined by the workflow.

## Where To Start

Use this quick guide when deciding which role should begin the workflow.

- Start with `planner` when you have a new feature, technical task, or implementation idea that needs to be broken down
- Start with `manager` when you want prioritization, a go or no-go recommendation, or a high-level decision summary
- Start with `improver` when you already have a plan or solution and want to make it simpler or stronger
- Start with `qa` when you mainly want to review an existing proposal for defects, risk, or missing tests

For most new feature requests, begin with `planner`.

## Current Starter Pack

- Roles:
  - planner
  - developer
  - qa
  - guard
  - manager
  - improver
- Workflow:
  - build-review
- Adapters:
  - codex
  - claude

## Future Extensions

- Add more workflows such as `plan-only` and `improve-existing`
- Introduce structured JSON output contracts if stronger consistency is needed
- Add client adapters for Cursor or other tools
- Add task intake templates and review report templates for repeatable sessions
