using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UBear.Leaderboard
{
  #region Leaderboard Models

  /// <summary>
  /// Maps to the API's ScoreResponse shape.
  /// [JsonProperty] bridges C# naming conventions to the API's snake_case keys.
  /// rank and percentile are nullable — not all endpoints return them.
  /// </summary>
  [Serializable]
  public class ScoreResponse
  {
    // SERIAL in Postgres → int32. Widen to long if schema ever moves to BIGSERIAL.
    [JsonProperty("id")] public int Id { get; set; }
    [JsonProperty("player")] public string Player { get; set; }
    [JsonProperty("score")] public long Score { get; set; }
    [JsonProperty("game_mode")] public string GameMode { get; set; }
    [JsonProperty("period")] public TimePeriod? Period { get; set; }
    // Kept as string to avoid Newtonsoft's local-time DateTime conversion.
    // Parse with DateTimeStyles.RoundtripKind at the call site if needed.
    [JsonProperty("submitted_at")] public string SubmittedAt { get; set; }
    [JsonProperty("rank")] public int? Rank { get; set; }
    [JsonProperty("percentile")] public double? Percentile { get; set; }
    // Server-computed validation status. Raw and cumulative submissions return
    // Validated=false, ValidationTier=0; a score from a validated run sets them.
    // Non-breaking additions: older servers omit these keys and Newtonsoft
    // leaves the defaults (false / 0).
    [JsonProperty("validated")] public bool Validated { get; set; }
    [JsonProperty("validation_tier")] public int ValidationTier { get; set; }
  }

  /// <summary>
  /// Envelope returned by GET /api/leaderboard/scores.
  /// </summary>
  [Serializable]
  public class LeaderboardResponse
  {
    [JsonProperty("scores")] public System.Collections.Generic.List<ScoreResponse> Scores { get; set; }
    [JsonProperty("total_count")] public int TotalCount { get; set; }
  }

  /// <summary>
  /// Maps to the API's GameModeConfig shape.
  /// </summary>
  [Serializable]
  public class GameModeConfig
  {
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("sort_order")] public SortOrder SortOrdering { get; set; }
    [JsonProperty("label")] public string Label { get; set; }
    [JsonProperty("requires_claimed_account")] public bool RequiresClaimedAccount { get; set; }
    // Read-only mode metadata surfaced for clients that want it; the client does
    // not send these. RequiredTier 0 = raw via /scores (SubmitScore), >=1 = run
    // required via /runs (SubmitRun). ScoringStrategy is "best" or "cumulative"
    // (kept as string, not an enum, so a future strategy doesn't break
    // deserialization). GameKey is nullable forward-provisioning.
    [JsonProperty("required_tier")] public int RequiredTier { get; set; }
    [JsonProperty("scoring_strategy")] public string ScoringStrategy { get; set; }
    [JsonProperty("game_key")] public string GameKey { get; set; }
    // Per-mode score ceiling. Null inherits the global server cap; non-null caps
    // the canonical/raw score for this mode. long? because the global cap
    // (180,000,000,081) exceeds int32 range.
    [JsonProperty("max_score")] public long? MaxScore { get; set; }
  }

  /// <summary>
  /// Body for POST /api/leaderboard/scores.
  /// Player is not included — the server derives it from the Bearer token.
  /// </summary>
  [Serializable]
  public class ScoreSubmission
  {
    [JsonProperty("score")] public long Score { get; set; }
    [JsonProperty("game_mode")] public string GameMode { get; set; }
    // Required only for cumulative modes (the server returns 422 without it
    // there); leave null for raw/best modes. NullValueHandling.Ignore omits the
    // key entirely when unset, keeping raw submissions byte-identical to before.
    [JsonProperty("idempotency_key", NullValueHandling = NullValueHandling.Ignore)]
    public string IdempotencyKey { get; set; }
  }

  /// <summary>
  /// Body for POST /api/leaderboard/runs — a full run the server validates to
  /// produce a canonical score. Use for game modes the server reports with
  /// RequiredTier &gt;= 1; raw/best modes use ScoreSubmission via SubmitScore.
  ///
  /// The action log is opaque at the API boundary: the server validates Actions
  /// for transport bounds only (a list, capped element count), never internal
  /// semantics, and stores it gzipped. The element shape is pinned per
  /// ScenarioVersion by a (deferred) server-side validator — until then, tier-1
  /// modes validate against ClaimedScore plus a non-empty Actions list.
  /// </summary>
  [Serializable]
  public class RunSubmission
  {
    [JsonProperty("game_mode")] public string GameMode { get; set; }
    [JsonProperty("scenario_version")] public int ScenarioVersion { get; set; }
    // long: the server applies no numeric bound to the seed, and a 64-bit seed
    // is common. Newtonsoft serializes it as a plain JSON number.
    [JsonProperty("seed")] public long Seed { get; set; }
    // Opaque action log. The element shape is unpinned server-side this phase,
    // so it is typed as object[] and serialized as-is. Capped at 50,000
    // elements server-side (HTTP 422 beyond).
    [JsonProperty("actions")] public object[] Actions { get; set; }
    // The client's claimed score. Recorded but never authoritative — the server
    // computes the canonical score. Required for tier-1 modes (the claim becomes
    // canonical if plausible); omitted when null so a higher-tier recompute can
    // supply it. long? to match the server's score range.
    [JsonProperty("claimed_score", NullValueHandling = NullValueHandling.Ignore)]
    public long? ClaimedScore { get; set; }
    // Anti-replay / idempotency key. Resubmitting a run with the same value
    // returns the prior result without re-validating.
    [JsonProperty("client_run_id")] public string ClientRunId { get; set; }
    // The tier the client asserts its log supports. Recorded, never trusted;
    // when null, validation targets the mode's RequiredTier.
    [JsonProperty("claimed_tier", NullValueHandling = NullValueHandling.Ignore)]
    public int? ClaimedTier { get; set; }
  }

  #endregion
  #region Auth Models

  /// <summary>
  /// Response from any auth endpoint that issues tokens.
  /// </summary>
  [Serializable]
  public class TokenResponse
  {
    [JsonProperty("access_token")] public string AccessToken { get; set; }
    [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
    [JsonProperty("token_type")] public string TokenType { get; set; }
  }

  /// <summary>
  /// Body for POST /api/auth/register.
  /// </summary>
  [Serializable]
  public class RegisterRequest
  {
    [JsonProperty("username")] public string Username { get; set; }
    [JsonProperty("email")] public string Email { get; set; }
    [JsonProperty("password")] public string Password { get; set; }
  }

  /// <summary>
  /// Body for POST /api/auth/login.
  /// </summary>
  [Serializable]
  public class LoginRequest
  {
    [JsonProperty("username")] public string Username { get; set; }
    [JsonProperty("password")] public string Password { get; set; }
  }

  /// <summary>
  /// Body for POST /api/auth/refresh and /api/auth/logout.
  /// </summary>
  [Serializable]
  public class RefreshRequest
  {
    [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
  }

  /// <summary>
  /// Body for POST /api/auth/claim.
  /// </summary>
  [Serializable]
  public class ClaimRequest
  {
    [JsonProperty("email")] public string Email { get; set; }
    [JsonProperty("password")] public string Password { get; set; }
  }

  /// <summary>
  /// Body for POST /api/auth/rename.
  /// </summary>
  [Serializable]
  public class RenameRequest
  {
    [JsonProperty("username")] public string Username { get; set; }
  }

  #endregion
  #region Result wrapper

  /// <summary>
  /// Categorizes API failures by source so callers can branch on kind
  /// without parsing error message strings.
  /// </summary>
  public enum ApiErrorKind
  {
    None,           // Success — Error/StatusCode unset
    Network,        // Connection failed, DNS, timeout — no HTTP response
    BadRequest,     // 400 — malformed request
    Unauthorized,   // 401 — missing/invalid/expired token
    Forbidden,      // 403 — authenticated but not allowed (e.g. guest hitting requires_claimed_account mode)
    NotFound,       // 404
    Conflict,       // 409 — e.g. username taken
    WrongEndpoint,  // 409 + {code, submit_to} — submitted to the wrong endpoint
                    // for this mode's tier (raw to a run-required mode or vice
                    // versa). SubmitTo names the correct endpoint.
    Validation,     // 422 — Pydantic validation error
    RateLimited,    // 429
    Server,         // 5xx
    ParseError,     // Response received but couldn't deserialize
  }

  /// <summary>
  /// Wraps a successful result or a structured error.
  /// Callers check Success before reading Data.
  /// On failure, ErrorKind classifies the failure and StatusCode (if non-null)
  /// gives the raw HTTP status. Error is always populated with a human-readable
  /// message suitable for logging or UI display.
  /// </summary>
  public class ApiResult<T>
  {
    public bool Success { get; }
    public T Data { get; }
    public string Error { get; }
    public ApiErrorKind ErrorKind { get; }
    public int? StatusCode { get; }

    /// <summary>
    /// Machine-routable error code, populated only for coded server errors —
    /// today that means cross-route 409s, where it is "RUN_REQUIRED" or
    /// "RAW_ONLY". Null otherwise.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// For ErrorKind.WrongEndpoint, the endpoint path the server says the
    /// submission belongs to (e.g. "/api/leaderboard/runs"). Null otherwise.
    /// </summary>
    public string SubmitTo { get; }

    private ApiResult(bool success, T data, string error, ApiErrorKind kind, int? statusCode,
                      string errorCode = null, string submitTo = null)
    {
      Success = success;
      Data = data;
      Error = error;
      ErrorKind = kind;
      StatusCode = statusCode;
      ErrorCode = errorCode;
      SubmitTo = submitTo;
    }

    public static ApiResult<T> Ok(T data) =>
        new ApiResult<T>(true, data, null, ApiErrorKind.None, null);

    // Preserved for backward compatibility — defaults to ErrorKind.Network
    // since that's what existing pre-StatusCode callers were implicitly assuming.
    public static ApiResult<T> Fail(string message) =>
        new ApiResult<T>(false, default, message, ApiErrorKind.Network, null);

    public static ApiResult<T> Fail(string message, ApiErrorKind kind, int? statusCode = null,
                                    string errorCode = null, string submitTo = null) =>
        new ApiResult<T>(false, default, message, kind, statusCode, errorCode, submitTo);
  }
  #endregion
  #region Enums
  /// <summary>
  /// Time bucket for leaderboard queries.
  /// Wire format: lowercase string ("alltime", "daily", "weekly").
  /// Maintain against app/periods.py:PERIODS on the server.
  /// </summary>
  [JsonConverter(typeof(StringEnumConverter))]
  public enum TimePeriod
  {
    [System.Runtime.Serialization.EnumMember(Value = "alltime")] Alltime,
    [System.Runtime.Serialization.EnumMember(Value = "daily")] Daily,
    [System.Runtime.Serialization.EnumMember(Value = "weekly")] Weekly,
  }
  public static class TimePeriodExtensions
  {
    public static string ToWireValue(this TimePeriod period) =>
        period switch
        {
          TimePeriod.Alltime => "alltime",
          TimePeriod.Daily => "daily",
          TimePeriod.Weekly => "weekly",
          _ => throw new ArgumentOutOfRangeException(nameof(period), $"Unsupported TimePeriod: {period}")
        };
  }

  /// <summary>
  /// Direction a leaderboard is ranked.
  /// Wire format: uppercase string ("ASC" or "DESC"), constrained server-side
  /// by the regex ^(ASC|DESC)$ in app/models.py:GameModeCreate.
  /// Received as part of GameModeConfig; the client does not send it.
  /// </summary>
  [JsonConverter(typeof(StringEnumConverter))]
  public enum SortOrder
  {
    [System.Runtime.Serialization.EnumMember(Value = "ASC")] Asc,
    [System.Runtime.Serialization.EnumMember(Value = "DESC")] Desc,
  }
  #endregion
}
