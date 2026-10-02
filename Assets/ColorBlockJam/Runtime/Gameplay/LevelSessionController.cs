using System;
using System.Collections;
using ColorBlockJam.Levels;
using UnityEngine;

namespace ColorBlockJam.Gameplay
{
    /// <summary>Owns the rules and state of one playable level attempt.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelSessionController : MonoBehaviour
    {
        private LevelDefinition level;
        private BoardGridState board;
        private GameplayInputController input;
        private LevelCountdown countdown;
        private Coroutine pendingTimeoutResolution;
        private bool awaitingExitResolution;
        private bool hasResolved;
        private bool isPaused;
        private bool isInitialized;

        public bool IsActive => isInitialized && !hasResolved;
        public bool IsPaused => isPaused;

        public event Action<int> TimeChanged;
        public event Action Completed;
        public event Action Failed;
        public event Action<bool> PauseChanged;

        public void Initialize(LevelDefinition definition, LevelRuntimeContext runtime)
        {
            level = definition;
            board = runtime.Board;
            input = runtime.Input;
            countdown = runtime.Countdown;
            isInitialized = level != null && board != null && input != null && countdown != null;
        }

        public void Begin()
        {
            if (!isInitialized) return;

            board.RemainingBlockCountChanged += HandleRemainingBlockCountChanged;
            board.BlockExited += HandleBlockExited;
            countdown.TimeChanged += HandleTimeChanged;
            countdown.Expired += HandleTimerExpired;
            input.SetInputEnabled(true);
            countdown.StartCountdown(level.TimerSeconds);
        }

        public void Pause()
        {
            if (!IsActive || isPaused) return;
            SetPaused(true);
        }

        public void Resume()
        {
            if (!IsActive || !isPaused) return;
            SetPaused(false);
        }

        public void Shutdown()
        {
            CancelPendingTimeoutResolution();
            UnbindEvents();
            countdown?.StopCountdown();
            input?.SetInputEnabled(false);
            isInitialized = false;
            hasResolved = true;
            awaitingExitResolution = false;
            SetPaused(false);
        }

        private void OnDestroy() => Shutdown();

        private void HandleRemainingBlockCountChanged(int remainingBlockCount)
        {
            if (remainingBlockCount == 0) Complete();
        }

        private void HandleBlockExited(GridMovableBlock _)
        {
            if (!awaitingExitResolution || board == null || board.HasBlocksExiting) return;
            Fail();
        }

        private void HandleTimerExpired()
        {
            if (!IsActive || pendingTimeoutResolution != null) return;

            // Input release for this frame may still start a valid gate exit.
            pendingTimeoutResolution = StartCoroutine(ResolveTimerExpiryAtEndOfFrame());
        }

        private IEnumerator ResolveTimerExpiryAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            pendingTimeoutResolution = null;
            if (!IsActive) yield break;

            if (board.RemainingBlockCount == 0)
            {
                Complete();
                yield break;
            }

            input.SetInputEnabled(false);
            if (!board.HasBlocksExiting)
            {
                Fail();
                yield break;
            }

            awaitingExitResolution = true;
        }

        private void Complete()
        {
            if (!IsActive) return;
            Resolve(Completed);
        }

        private void Fail()
        {
            if (!IsActive) return;
            Resolve(Failed);
        }

        private void Resolve(Action result)
        {
            hasResolved = true;
            awaitingExitResolution = false;
            CancelPendingTimeoutResolution();
            countdown.StopCountdown();
            input.SetInputEnabled(false);
            result?.Invoke();
        }

        private void SetPaused(bool paused)
        {
            isPaused = paused && IsActive;
            Time.timeScale = isPaused ? 0f : 1f;
            countdown?.SetPaused(isPaused);
            input?.SetInputEnabled(!isPaused && IsActive);
            PauseChanged?.Invoke(isPaused);
        }

        private void HandleTimeChanged(int seconds) => TimeChanged?.Invoke(seconds);

        private void CancelPendingTimeoutResolution()
        {
            if (pendingTimeoutResolution == null) return;
            StopCoroutine(pendingTimeoutResolution);
            pendingTimeoutResolution = null;
        }

        private void UnbindEvents()
        {
            if (board != null)
            {
                board.RemainingBlockCountChanged -= HandleRemainingBlockCountChanged;
                board.BlockExited -= HandleBlockExited;
            }

            if (countdown != null)
            {
                countdown.TimeChanged -= HandleTimeChanged;
                countdown.Expired -= HandleTimerExpired;
            }
        }
    }
}
