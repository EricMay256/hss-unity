# Changelog

All notable changes to this project will be documented in this file.

## [0.2.0] - 2026-06-29

### Added

- `ScoreResponse.Validated` and `ScoreResponse.ValidationTier` — server-computed
  validation status. Raw and cumulative submissions report `false`/`0`; scores
  from a validated run carry the achieved tier.
- `GameModeConfig.RequiredTier`, `ScoringStrategy`, `GameKey`, and `MaxScore` —
  read-only mode metadata. `RequiredTier` 0 routes to `SubmitScore`, `>= 1` to
  `SubmitRun`; `ScoringStrategy` is `"best"` or `"cumulative"`.
- `ScoreSubmission.IdempotencyKey` and an optional `idempotencyKey` argument on
  `SubmitScore`, required for cumulative game modes (the server returns HTTP 422
  without it).
- `SubmitRun` and the `RunSubmission` model for `POST /api/leaderboard/runs`,
  the validated-run submission path for run-required modes.
- `ApiErrorKind.WrongEndpoint` plus `ApiResult.ErrorCode` / `ApiResult.SubmitTo`,
  surfacing the server's cross-route 409 (`RUN_REQUIRED` / `RAW_ONLY`) so callers
  can route a misdirected submission to the correct endpoint.

### Notes

- All changes are additive and non-breaking. Tracks High Score Server through its
  cumulative-scoring, run-validation, and per-mode score-bounding additions.

## [0.1.0] - 2026-05-25

### Added

- Initial offering of Unity SDK for [High Score Server](https://github.com/EricMay256/HighScoreServer) as a standalone repository and package - Formerly bundled as part of [UBear](https://github.com/EricMay256/UBear)
