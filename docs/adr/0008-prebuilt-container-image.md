# ADR 0008: Prebuilt container image for the Action, with a build-from-source fallback

**Status:** accepted

## Context

The first version of the Action installed the .NET SDK and ran `dotnet run` on every pull request. That added a build
(about 15 seconds on GitHub-hosted runners, more when the SDK has to be downloaded) before every review, and because the build ran from the reviewed repository's folder, a `global.json`
pinning an older SDK there made the Action fail.

A Docker container action (`runs.using: docker`) would start fast, but it only runs on Linux runners and needs the image
to exist before the version can be used.

## Decision

- The Action stays a **composite** action.
- A `Dockerfile` builds a framework-dependent image (amd64 and arm64). The `Container image` workflow publishes it to
  `ghcr.io/cosmess/code-review-crew` with the name of every version tag, including the moving `v1` tag.
- On Linux runners the Action pulls the image matching the ref it was used with (`github.action_ref`) and runs the CLI
  in it, mounting the workspace and the runner's temp folder at the same paths.
- When there is no image (Windows, macOS, a branch or a commit SHA, or this repository's own `uses: ./`), the Action
  installs the SDK and publishes the CLI from the Action's own folder, so the reviewed repository's `global.json` does
  not apply.

## Consequences

- Linux reviews start in about 5 seconds; other runners keep working, only slower.
- This repository's own workflow keeps reviewing with the code of the pull request, because `uses: ./` builds from
  source.
- Every release must push its version tag so the image is published (see [CONTRIBUTING.md](../../CONTRIBUTING.md)).
- The GHCR package must be public, or other repositories fall back to building from source.
