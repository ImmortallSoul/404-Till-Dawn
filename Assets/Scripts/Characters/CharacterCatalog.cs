using System;
using UnityEngine;

public class CharacterDefinition
{
    public CharacterId Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string SpecialName { get; private set; }
    public string SpecialDescription { get; private set; }
    public float SpecialCooldown { get; private set; }
    public float MaxHp { get; private set; }
    public Color Color { get; private set; }
    public Action<PlayerStats, PlayerCombat> Apply { get; private set; }

    public CharacterDefinition(CharacterId id, string name, string description, string specialName,
        string specialDescription, float cooldown, float hp, Color color,
        Action<PlayerStats, PlayerCombat> apply)
    {
        Id = id;
        Name = name;
        Description = description;
        SpecialName = specialName;
        SpecialDescription = specialDescription;
        SpecialCooldown = cooldown;
        MaxHp = hp;
        Color = color;
        Apply = apply;
    }
}

public static class CharacterCatalog
{
    public static readonly CharacterDefinition Shana = new CharacterDefinition(
        CharacterId.Shana, "Шана", "Пасивка: один перекид карт на кожному рівні, +10% до темпу стрілянини.",
        "Прокляття й перекид",
        "ПКМ: прокляття ворогів поруч, +2 перекиди карт і 0.5 HP. Перезарядка 14 с.",
        0f, 4f, new Color(0.95f, 0.85f, 0.55f),
        (p, c) =>
        {
            p.CanReroll = true;
            c.FireRateMultiplier *= 1.1f;
        });

    public static readonly CharacterDefinition Diamond = new CharacterDefinition(
        CharacterId.Diamond, "Даймонд", "Пасивка: 7 HP, витримка росте при лікуванні й уроні, менше шкоди на низькому HP.",
        "Мороз і зцілення",
        "ПКМ: заморозка ворогів на 2 с і +1 HP. Перезарядка 14 с.",
        0f, 7f, new Color(0.55f, 0.75f, 0.95f),
        (p, c) =>
        {
            p.DiamondBulkUp = true;
            p.DiamondBerserk = true;
            p.DiamondLowHp = true;
        });

    public static readonly CharacterDefinition Scarlett = new CharacterDefinition(
        CharacterId.Scarlett, "Скарлет", "Пасивка: кожен 3-й постріл випускає хвилю полум'я, +50% до підпалу.",
        "Вогняна хвиля",
        "ПКМ: хвиля полум'я з підпалом на 4 с. Перезарядка 14 с.",
        0f, 3f, new Color(1f, 0.42f, 0.2f),
        (p, c) =>
        {
            p.Scarlett = true;
            p.BurnMultiplier += 0.5f;
        });

    public static readonly CharacterDefinition Spark = new CharacterDefinition(
        CharacterId.Spark, "Спарк", "Пасивка: кулі з імовірністю 50% викликають блискавку.",
        "Блискавка",
        "ПКМ: блискавка по ворогах у радіусі 9 м. Перезарядка 14 с.",
        0f, 3f, new Color(0.5f, 0.9f, 1f),
        (p, c) =>
        {
            p.LightningChance = 0.5f;
            p.LightningDamage = 12f;
        });

    public static readonly CharacterDefinition Lilith = new CharacterDefinition(
        CharacterId.Lilith, "Ліліт", "Пасивка: після вбивства ворога з'являється дух, що атакує.",
        "Дух",
        "ПКМ: виклик духа на місці кожного ураженого ворога. Перезарядка 14 с.",
        0f, 4f, new Color(0.75f, 0.45f, 1f),
        (p, c) =>
        {
            p.LilithSpirit = true;
            p.SummonDamageMultiplier *= 1.2f;
        });

    public static readonly CharacterDefinition[] All =
    {
        Shana, Diamond, Scarlett, Spark, Lilith
    };

    public static CharacterDefinition Get(CharacterId id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
                return All[i];
        }
        return Shana;
    }
}
