using System.Collections;
using UnityEngine;

namespace UBear.Leaderboard
{
    /// <summary>
    /// End-to-end sample showing the most common client flow:
    /// 1) ensure a guest session exists,
    /// 2) submit scores,
    /// 3) optionally claim the guest account.
    /// </summary>
    public class LeaderboardExample : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private LeaderboardService _service;

        [Header("Demo Settings")]
        [SerializeField] private string _gameMode = "classic";
        [SerializeField] private bool _fetchScoresOnStart = true;

        [Header("Claim Demo Inputs")]
        [SerializeField] private string _claimEmail = "player@example.com";
        [SerializeField] private string _claimPassword = "replace-with-real-password";

        private void Start()
        {
            StartCoroutine(BootstrapSession());
        }

        private IEnumerator BootstrapSession()
        {
            if (_service == null)
            {
                Debug.LogError("[Leaderboard] LeaderboardService reference is missing.");
                yield break;
            }

            bool done = false;
            ApiResult<bool> authResult = null;
            yield return _service.EnsureAuthenticated(result =>
            {
                authResult = result;
                done = true;
            });
            while (!done) yield return null;

            if (!authResult.Success)
            {
                Debug.LogWarning($"[Leaderboard] Authentication failed: {authResult.Error}");
                yield break;
            }

            Debug.Log($"[Leaderboard] Session ready. Username: {LeaderboardService.Username}");

            if (_fetchScoresOnStart)
            {
                StartCoroutine(_service.GetScores(_gameMode, OnScoresReceived));
            }
        }

        /// <summary>
        /// Submits a random score for the currently authenticated user.
        /// This is a typical game-over button handler.
        /// </summary>
        public void SubmitRandomScore()
        {
            long score = Random.Range(0, 100);
            StartCoroutine(_service.SubmitScore(score, _gameMode, OnScoreSubmitted));
        }

        /// <summary>
        /// Demonstrates upgrading a guest session to a claimed account.
        /// In production, call this with values from your account-claim UI.
        /// </summary>
        public void ClaimGuestAccountFromInspectorValues()
        {
            StartCoroutine(ClaimGuestAccount(_claimEmail, _claimPassword));
        }

        public IEnumerator ClaimGuestAccount(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("[Leaderboard] Claim requires a non-empty email and password.");
                yield break;
            }

            ApiResult<TokenResponse> claimResult = null;
            yield return _service.Claim(email, password, result => claimResult = result);

            if (!claimResult.Success)
            {
                Debug.LogWarning($"[Leaderboard] Claim failed: {claimResult.Error}");
                yield break;
            }

            Debug.Log($"[Leaderboard] Account claimed successfully. Username: {LeaderboardService.Username}");
        }

        private void OnScoresReceived(ApiResult<LeaderboardResponse> result)
        {
            if (!result.Success)
            {
                Debug.LogWarning($"[Leaderboard] Failed to load scores: {result.Error}");
                return;
            }

            Debug.Log($"[Leaderboard] Loaded {result.Data.Scores.Count} scores (total: {result.Data.TotalCount}).");
            foreach (ScoreResponse entry in result.Data.Scores)
            {
                Debug.Log($"{entry.Player}: {entry.Score} ({entry.GameMode})");
            }
        }

        private void OnScoreSubmitted(ApiResult<ScoreResponse> result)
        {
            if (!result.Success)
            {
                Debug.LogWarning($"[Leaderboard] Score submission failed: {result.Error}");
                return;
            }

            Debug.Log($"[Leaderboard] Score accepted. Current best: {result.Data.Score}");
        }
    }
}