using System.Collections.Generic;
using UnityEngine;

namespace ColorBlockJam.Levels
{
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Color Block Jam/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

        public int Count => levels.Count;
        public IReadOnlyList<LevelDefinition> Levels => levels;

        public bool TryGet(int index, out LevelDefinition level)
        {
            if (index >= 0 && index < levels.Count && levels[index] != null)
            {
                level = levels[index];
                return true;
            }

            level = null;
            return false;
        }

        public int ClampIndex(int index) => Mathf.Clamp(index, 0, Mathf.Max(0, levels.Count - 1));

        public bool SetLevels(IReadOnlyList<LevelDefinition> source)
        {
            if (source == null) return false;

            bool changed = levels.Count != source.Count;
            if (!changed)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    if (levels[i] == source[i]) continue;
                    changed = true;
                    break;
                }
            }

            if (!changed) return false;
            levels.Clear();
            for (int i = 0; i < source.Count; i++)
                if (source[i] != null) levels.Add(source[i]);
            return true;
        }
    }
}
