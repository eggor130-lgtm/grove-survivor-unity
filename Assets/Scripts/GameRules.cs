using System;
using UnityEngine;

namespace GroveSurvivor
{
    public enum GameScreen { Home, Selection, Settings, Help, Playing, Paused, Upgrade, Results }
    public enum Difficulty { Wanderer, Survivor, Nightmare }
    public enum UpgradeKind { Damage, AttackSpeed, Health }

    [Serializable]
    public struct DifficultyRules
    {
        public string Name;
        public float Health, SpawnInterval, Damage;
        public int WaveSize;
        public DifficultyRules(string name, float health, float interval, float damage, int count)
        { Name = name; Health = health; SpawnInterval = interval; Damage = damage; WaveSize = count; }
        public static DifficultyRules Get(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Wanderer: return new DifficultyRules("Путник", .75f, 1.45f, .7f, 1);
                case Difficulty.Nightmare: return new DifficultyRules("Кошмар", 1.65f, .65f, 1.35f, 3);
                default: return new DifficultyRules("Выживший", 1, 1, 1, 2);
            }
        }
    }

    public struct UpgradeChoice
    {
        public UpgradeKind Kind;
        public int Value;
        public string Name => Kind == UpgradeKind.Damage ? "Закалённая сталь" :
            Kind == UpgradeKind.AttackSpeed ? "Быстрый клинок" : "Сердце леса";
        public string Description => Kind == UpgradeKind.Damage ? "Урон каждого удара" :
            Kind == UpgradeKind.AttackSpeed ? "Скорость атаки" : "Максимальное и текущее здоровье";
        public string Bonus => "+" + Value + (Kind == UpgradeKind.Health ? " HP" : "%");
        public static UpgradeChoice[] Roll()
        {
            var choices = new[] {
                new UpgradeChoice { Kind = UpgradeKind.Damage, Value = 20 + UnityEngine.Random.Range(0, 3) * 5 },
                new UpgradeChoice { Kind = UpgradeKind.AttackSpeed, Value = 12 + UnityEngine.Random.Range(0, 3) * 3 },
                new UpgradeChoice { Kind = UpgradeKind.Health, Value = 20 + UnityEngine.Random.Range(0, 3) * 5 }
            };
            for (int i = choices.Length - 1; i > 0; i--)
            { int j = UnityEngine.Random.Range(0, i + 1); var temp = choices[i]; choices[i] = choices[j]; choices[j] = temp; }
            return choices;
        }
    }

    public sealed class HeroStats
    {
        public float Health = 100, MaxHealth = 100, Damage = 24, AttackInterval = .85f;
        public const float MoveSpeed = 6.5f, AttackRange = 3.6f;
        public int Level = 1, Experience, RequiredExperience = 10;
        public void Apply(UpgradeChoice upgrade)
        {
            switch (upgrade.Kind)
            {
                case UpgradeKind.Damage: Damage *= 1 + upgrade.Value / 100f; break;
                case UpgradeKind.AttackSpeed: AttackInterval = Mathf.Max(.16f, AttackInterval / (1 + upgrade.Value / 100f)); break;
                case UpgradeKind.Health: MaxHealth += upgrade.Value; Health = Mathf.Min(MaxHealth, Health + upgrade.Value); break;
            }
        }
        public bool TryLevelUp()
        {
            if (Experience < RequiredExperience) return false;
            Experience -= RequiredExperience;
            Level++;
            RequiredExperience = Mathf.RoundToInt(10 + Level * 5 + Mathf.Pow(Level, 1.25f));
            return true;
        }
    }
}
