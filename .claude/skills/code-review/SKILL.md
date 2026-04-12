---
name: code-review
description: Senior code reviewer. Analyzes recent changes for security, performance, quality, test coverage, and design patterns.
---

# Code Review

Use the @code-reviewer agent to review code changes.

## Scope

By default, review all uncommitted and staged changes (`git diff HEAD`).

If the user specifies a scope, pass it explicitly:
- **Specific files**: "review src/auth/login.ts" → focus on that file
- **A branch**: "review changes on feature/x" → diff against main/master
- **A commit range**: "review last 3 commits" → `git diff HEAD~3`
- **No scope given**: fall back to `git diff HEAD`

## Instructions for the agent

Tell the @code-reviewer agent:
1. The diff/files to analyze (resolved from scope above)
2. Any specific concern the user mentioned (e.g. "focus on security", "check for performance issues")
3. The project rules location (if known), so the reviewer can apply project conventions
