using ColorBlockJam.Gameplay;
using ColorBlockJam.Levels;
using NUnit.Framework;
using UnityEngine;

namespace ColorBlockJam.Tests
{
    public sealed class LevelSessionControllerTests
    {
        private GameObject root;
        private LevelDefinition level;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("SessionTestRoot");
            level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.SetTimer(30);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(level);
            Time.timeScale = 1f;
        }

        [Test]
        public void PauseAndResume_UpdateSessionSystemsTogether()
        {
            BoardGridState board = root.AddComponent<BoardGridState>();
            GameplayInputController input = root.AddComponent<GameplayInputController>();
            LevelCountdown countdown = root.AddComponent<LevelCountdown>();
            var runtime = new LevelRuntimeContext(root, board, input, countdown);
            LevelSessionController session = root.AddComponent<LevelSessionController>();
            session.Initialize(level, runtime);
            session.Begin();

            session.Pause();

            Assert.That(session.IsPaused, Is.True);
            Assert.That(countdown.IsPaused, Is.True);
            Assert.That(input.IsInputEnabled, Is.False);
            Assert.That(Time.timeScale, Is.Zero);

            session.Resume();

            Assert.That(session.IsPaused, Is.False);
            Assert.That(countdown.IsPaused, Is.False);
            Assert.That(input.IsInputEnabled, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
    }
}
