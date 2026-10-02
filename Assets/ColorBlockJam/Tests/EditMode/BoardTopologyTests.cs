using ColorBlockJam.Levels;
using NUnit.Framework;
using UnityEngine;

namespace ColorBlockJam.Tests
{
    public sealed class BoardTopologyTests
    {
        private LevelDefinition level;

        [SetUp]
        public void SetUp()
        {
            level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.SetBoardSize(5, 5);
            AddBoundaryRing(level);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(level);

        [Test]
        public void FindEnclosedCells_ReturnsOnlyCellsInsideClosedBoundary()
        {
            var enclosed = BoardTopology.FindEnclosedCells(level);

            Assert.That(enclosed.Count, Is.EqualTo(9));
            Assert.That(enclosed.Contains(new Vector2Int(2, 2)), Is.True);
            Assert.That(enclosed.Contains(new Vector2Int(0, 0)), Is.False);
        }

        [Test]
        public void CornerAndRotation_AreSharedDeterministicRules()
        {
            var bottomLeft = new Vector2Int(0, 0);

            Assert.That(BoardTopology.IsCorner(level, bottomLeft), Is.True);
            Assert.That(BoardTopology.GetAutomaticRotation(level, bottomLeft), Is.EqualTo(270));
        }

        [Test]
        public void BlockLookup_RecognizesEveryCellOfAShapedBlock()
        {
            var block = new BlockDefinition("Wide", BlockColor.Blue, BlockShape.Horizontal2,
                new Vector2Int(1, 1));
            level.AddBlock(block);

            Assert.That(BlockTopology.GetBlockAt(level, new Vector2Int(2, 1)), Is.SameAs(block));
        }

        private static void AddBoundaryRing(LevelDefinition target)
        {
            for (int x = 0; x < 5; x++)
            {
                target.AddWall(new WallDefinition($"Bottom_{x}", new Vector2Int(x, 0)));
                target.AddWall(new WallDefinition($"Top_{x}", new Vector2Int(x, 4)));
            }

            for (int y = 1; y < 4; y++)
            {
                target.AddWall(new WallDefinition($"Left_{y}", new Vector2Int(0, y)));
                target.AddWall(new WallDefinition($"Right_{y}", new Vector2Int(4, y)));
            }
        }
    }
}
