# Evaluation

**English** | [Português](README.pt-BR.md) | [简体中文](README.zh-CN.md)

A set of labeled pull requests to measure review quality. Each case has a diff and the issues a good review must find.

## Running

```bash
export OPENAI_API_KEY=sk-...
dotnet run --project src/CodeReviewCrew.Evals                              # every case, reviews in English
dotnet run --project src/CodeReviewCrew.Evals -- --filter sql              # only cases with "sql" in the name
dotnet run --project src/CodeReviewCrew.Evals -- --language pt-BR          # reviews in Portuguese
dotnet run --project src/CodeReviewCrew.Evals -- --output report.md --min-recall 0.8
```

On GitHub, run the **Evaluation** workflow (*Actions → Evaluation → Run workflow*). The report appears in the run
summary and as an artifact.

## Metrics

| Metric | How it is computed |
|---|---|
| **Recall** | Expected issues found / expected issues |
| **Precision** | Matches / (matches + false positives) |
| **F1** | Harmonic mean of precision and recall |
| **Correct verdicts** | `RequestChanges` when the case should block, another verdict when it should not |
| **Underrated severity** | Issues found with a severity below the expected minimum |

A finding **matches** an expected issue when it is in the same file, at most `tolerance` lines away, and mentions one
of the `keywords` (case- and accent-insensitive) or one of the `owasp` categories in its title (e.g. `[A05:2025 ...]`).
Keywords are given in English, Portuguese and Chinese so the evaluation works in every review language.
Each finding counts for at most one expected issue.

Findings that match nothing only count as **false positives** when they are `Medium` or more severe.
Extra `Low`/`Info` findings (style, naming) are treated as acceptable noise.

## Cases

| Case | What it tests |
|---|---|
| `sql-injection` | SQL built by concatenation |
| `hardcoded-secret` | Production API key in the code |
| `sync-over-async` | `.Result` in a controller |
| `n-plus-one-query` | Database query inside a loop |
| `path-traversal` | File name from the URL used without validation |
| `insecure-deserialization` | `TypeNameHandling.All` |
| `weak-password-hash` | MD5 for passwords |
| `idor-missing-ownership-check` | Resource returned without checking its owner |
| `swallowed-exception` | Empty `catch` |
| `sql-injection-and-password-logging` | Two issues in the same file |
| `clean-parameterized-query` | Safe change: must not raise alarms |
| `clean-documentation` | Renames and docs: must not raise alarms |

## Adding a case

1. Create `evals/cases/<case-name>/`.
2. Generate `diff.patch` with git, so the hunk headers are correct:
   ```bash
   git diff main -- path/to/File.cs > evals/cases/<case-name>/diff.patch
   ```
3. Write `expected.json`:
   ```json
   {
     "description": "What the case tests.",
     "shouldBlock": true,
     "findings": [
       {
         "file": "src/Api/Controller.cs",
         "line": 14,
         "tolerance": 2,
         "keywords": ["traversal", "path", "caminho", "路径"],
         "owasp": ["A01"],
         "minSeverity": "High"
       }
     ]
   }
   ```
   `line` is the line in the **new** version of the file. `shouldBlock` must be `true` if and only if some issue
   has a `minSeverity` of `High` or `Critical`.
4. Run `dotnet test`: `Every_labeled_case_in_the_dataset_is_consistent` checks that the file and line exist in the diff.
