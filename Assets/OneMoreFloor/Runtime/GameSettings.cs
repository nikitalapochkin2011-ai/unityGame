using UnityEngine;
namespace OneMoreFloor
{
    [CreateAssetMenu(menuName = "Ещё один этаж/Настройки игры")]
    public sealed class GameSettings : ScriptableObject
    {
        public GameRules Rules = new GameRules();
        void OnValidate() { Rules.Validate(); }
    }
}
