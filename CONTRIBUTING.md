# Contributing

Thanks for your interest in Code Review Crew. Bug reports, new evaluation cases, translations and code are all welcome.

## Before you start

- For a bug or an idea, open an [issue](https://github.com/Cosmess/code-review-crew/issues/new/choose) first, so we
  can agree on the approach before you write code.
- For a security issue, follow the [security policy](SECURITY.md) instead.
- Read [`AGENTS.md`](AGENTS.md): it lists the project rules for humans and AI assistants alike. The
  [architecture](docs/en/architecture.md) and [coding standards](docs/en/coding-standards.md) explain the details.

## Development

Requirements: the [.NET 10 SDK](https://dotnet.microsoft.com/download). An OpenAI API key is only needed for real
reviews, integration tests and the evaluation.

```bash
dotnet build
dotnet test tests/CodeReviewCrew.Tests      # unit tests, no network
dotnet run --project src/CodeReviewCrew.Cli -- samples/vulnerable.diff   # needs OPENAI_API_KEY
```

## Pull requests

- Keep each pull request focused on one change and add tests for it. Unit tests never call an LLM or the network.
- If you change a prompt or an agent, run the evaluation (`dotnet run --project src/CodeReviewCrew.Evals`) and put
  the precision and recall before and after in the description.
- If you change visible behavior, update the README and the docs in the three languages. If you cannot write one of
  them, say so in the pull request and we will help.
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`...).
  The maintainer writes them in Brazilian Portuguese, but English is fine in contributions.

## Releasing (maintainers)

1. Move the changes from *Unreleased* to the new version in [`CHANGELOG.md`](CHANGELOG.md).
2. Push the version tag: `git tag v1.2.3 && git push origin v1.2.3`. The
   [Container image](.github/workflows/container.yml) workflow publishes `ghcr.io/cosmess/code-review-crew:v1.2.3`.
3. Create the GitHub release from that tag and check *Publish this Action to the GitHub Marketplace*.
4. Move the major tag: `git tag -f v1 v1.2.3 && git push -f origin v1`. This republishes the `:v1` image too.
