# ADR 0007: English code and prompts, localized reviews and docs

**Status:** accepted

## Context

The project started in Portuguese (prompts, CLI messages and docs). To be published on the GitHub Marketplace it needs
to be usable by a global audience, while still serving Portuguese and Chinese-speaking teams.

## Decision

- Code, comments, agent instructions, CLI messages and errors are in **English**.
- The review language is a setting (`language` in `.crc.yml`, `--language`, the Action input) with `en` as the default,
  plus `pt-BR` and `zh-CN`. The agents keep English instructions and receive one extra sentence asking them to write
  in the chosen language.
- Every text the crew writes itself is translated in `ReviewStrings`.
- Documentation exists in English, Portuguese and Chinese with the same structure (`docs/en`, `docs/pt-BR`,
  `docs/zh-CN`) and a language switcher on every page. ADRs and `AGENTS.md` stay in English only.
- The evaluation dataset uses keywords in the three languages so it can score reviews in any of them.

## Consequences

- One set of prompts to maintain and evaluate, instead of one per language.
- Adding a language means a `ReviewLanguage`, its `ReviewStrings` and a translation of the docs.
- The three documentation versions must be kept in sync when behavior changes.
