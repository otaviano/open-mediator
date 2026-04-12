---
name: pull-request
description: Prepares and creates a pull request following conventional commits, running tests and code review first.
---

# Pull Request Preparation Checklist

Before creating a PR, execute these steps:

1. Create a new branch for changes, in case of be at main or master branches. Never commit against these branchs
2. Run tests: /test-runner. If tests fail, fix them before proceeding.
3. Run code-review: /code-review
4. Stage changes based on atomic files, avoid large single commit: `git add .`
5. Create commits messages following conventional commits:
   - `fix:` for bug fixes
   - `feat:` for new features
   - `docs:` for documentation
   - `refactor:` for code restructuring
   - `test:` for test additions
   - `chore:` for maintenance

6. Resolve the repository owner by running `gh repo view --json owner --jq '.owner.login'` and store it as `$REPO_OWNER`.

7. If exists, use template at .github/pull_request_template.md. Replace the literal `<!-- replace with the repository owner -->` placeholder with `$REPO_OWNER`. Otherwise generate PR summary including:
    ## Description

    A clear and concise description of the PR.

    Use this section for review hints, explanations or discussion points/todos.

    - What changed
    - Why it changed
    - Testing performed
    - Potential impacts
    - Additional context

    ## Screenshots

    Screenshots or a screen recording of the visual changes associated with this PR.
    (Feel free to delete this section for non-visual changes.)

    ## Docs

    Add any notes that help to document the feature/changes. Doesn't need to be your best writing, just a few words and/or code snippets.

    ## Ready?

    Did you do any of the following?
    - [ ] Documented what's new at README.md
    - [ ] Wrote tests for new components/features
    - [ ] Created a demo

    ## Contributor License Agreement

    I give $REPO_OWNER permission to license my contributions on any terms they like. I am giving them this license in order to make it possible for them to accept my contributions into their project.

    **_As far as the law allows, my contributions come as is, without any warranty or condition, and I will not be liable to anyone for any damages related to this software or this license, under any kind of legal claim._**
