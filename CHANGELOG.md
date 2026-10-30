# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.0.2] - 2026-10-30

### Fixed

- The timeout message of an agent call was still in Portuguese; it is now in English, like every other message.

## [1.0.1] - 2026-10-29

### Changed

- The Action uses a prebuilt container image on Linux runners, published to
  `ghcr.io/cosmess/code-review-crew` for every version. The review starts in about 5 seconds, without building the
  tool. Other runners, and refs without an image, still build from source.

### Fixed

- The Action no longer fails when the reviewed repository has a `global.json` pinning an older .NET SDK.

### Added

- Security policy, contribution guide, changelog and issue and pull request templates.

## [1.0.0] - 2026-10-29

First public release, available on the GitHub Marketplace.

- Security, OWASP Top 10:2025, performance and style agents, plus a synthesizer that merges findings and decides the
  verdict.
- Reviews posted on the pull request with inline comments, and SARIF output for GitHub code scanning.
- Read-only MCP tools so the agents can read the repository.
- Reviews in English, Brazilian Portuguese and Simplified Chinese.
- Timeouts, retries, large diff chunking, token usage and cost estimates.
- OpenTelemetry traces and a .NET Aspire dashboard.
- Evaluation suite with labeled pull requests.

[Unreleased]: https://github.com/Cosmess/code-review-crew/compare/v1.0.2...main
[1.0.2]: https://github.com/Cosmess/code-review-crew/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/Cosmess/code-review-crew/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Cosmess/code-review-crew/releases/tag/v1.0.0
