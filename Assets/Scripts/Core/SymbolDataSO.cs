using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// ScriptableObject defining metadata, visual presentation, and RNG weight for a single slot symbol.
    /// Follows OOPS encapsulation and allows easy tuning in Unity Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "SymbolData", menuName = "SlotGame/Symbol Data")]
    public class SymbolDataSO : ScriptableObject
    {
        [SerializeField] private SymbolType symbolType;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite symbolSprite;
        [SerializeField] private Color themeColor = Color.white;
        [Tooltip("Relative weight for RNG random selection. Higher = more frequent.")]
        [SerializeField] [Range(1, 100)] private int rngWeight = 10;

        public SymbolType SymbolType => symbolType;
        public string DisplayName => displayName;
        public Sprite SymbolSprite => symbolSprite;
        public Color ThemeColor => themeColor;
        public int RngWeight => rngWeight;

        public void Initialize(SymbolType type, string name, Sprite sprite, Color color, int weight)
        {
            symbolType = type;
            displayName = name;
            symbolSprite = sprite;
            themeColor = color;
            rngWeight = weight;
        }
    }
}
