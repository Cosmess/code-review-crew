# Security policy

## Supported versions

Security fixes are released for the latest `v1.x` version. The `v1` tag always points to it.

## Reporting a vulnerability

Please **do not open a public issue**. Report it privately through
[GitHub private vulnerability reporting](https://github.com/Cosmess/code-review-crew/security/advisories/new)
(*Security → Report a vulnerability*).

Include the version or commit, the steps to reproduce and the impact you expect. You will get an answer within
7 days, and a fix or a plan within 30 days for confirmed issues. Reporters are credited in the advisory unless they
prefer otherwise.

## What is in scope

Code Review Crew handles API keys and reads untrusted pull request content, so these reports are especially welcome:

- leaks of the API key or the GitHub token (logs, review comments, SARIF, container image);
- prompt injection in a diff that makes the crew post content it should not, or misuse its tools;
- escaping the read-only MCP tools (reading outside the repository, `.git`, secret files or symbolic links);
- script injection through the Action inputs.

Wrong or missing findings in a review are model quality issues, not vulnerabilities: please open a regular issue.

## Using the Action safely

- Keep the OpenAI key in a repository or organization secret, never in the workflow file.
- Grant only the permissions the workflow needs (`contents: read`, `pull-requests: write`, and `security-events: write`
  when uploading SARIF).
- Pin a version (`@v1`, `@v1.0.1` or a commit SHA).
- Pull requests from forks do not receive secrets. Do not work around it with `pull_request_target` plus a checkout of
  the fork's code.
