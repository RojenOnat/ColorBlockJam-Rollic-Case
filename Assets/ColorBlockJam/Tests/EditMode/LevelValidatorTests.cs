using ColorBlockJam.Levels;
using NUnit.Framework;
using UnityEngine;

namespace ColorBlockJam.Tests
{
    public sealed class LevelValidatorTests
    {
        [Test]
        public void Validate_ReportsBlockWithoutMatchingGate()
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.AddBlock(new BlockDefinition("RedBlock", BlockColor.Red, BlockShape.Single,
                new Vector2Int(1, 1)));

            var issues = LevelValidator.Validate(level);

            Assert.That(issues.Exists(issue => issue.Severity == ValidationSeverity.Error &&
                                                     issue.Message.Contains("no matching door")), Is.True);
            Object.DestroyImmediate(level);
        }

        [Test]
        public void Validate_AcceptsMatchingBlockAndEdgeDoor()
        {
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.AddBlock(new BlockDefinition("RedBlock", BlockColor.Red, BlockShape.Single,
                new Vector2Int(1, 1)));
            level.AddDoor(new DoorDefinition("RedDoor", BlockColor.Red, BoardEdge.Top, 1));

            var issues = LevelValidator.Validate(level);

            Assert.That(issues.Exists(issue => issue.Severity == ValidationSeverity.Error), Is.False);
            Object.DestroyImmediate(level);
        }
    }
}
