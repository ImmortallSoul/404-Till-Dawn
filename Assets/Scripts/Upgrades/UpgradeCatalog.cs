using System.Collections.Generic;
using UnityEngine;

public static class UpgradeCatalog
{
    public static readonly UpgradeDefinition[] All =
    {
        // Зброя
        new WeaponPickUpgrade(WeaponCatalog.Revolver),
        new WeaponPickUpgrade(WeaponCatalog.Shotgun),
        new WeaponPickUpgrade(WeaponCatalog.Crossbow),
        new WeaponPickUpgrade(WeaponCatalog.DualSmgs),
        new WeaponPickUpgrade(WeaponCatalog.Batgun),
        new WeaponPickUpgrade(WeaponCatalog.FlameCannon),
        new WeaponPickUpgrade(WeaponCatalog.GrenadeLauncher),
        new WeaponPickUpgrade(WeaponCatalog.Staff),
        new WeaponPickUpgrade(WeaponCatalog.SalvoKnife),
        new WeaponPickUpgrade(WeaponCatalog.WateringGun),
        new WeaponPickUpgrade(WeaponCatalog.CycloneSword),

        // Боєздатність зброї
        new WeaponBuffUpgrade("Гостре залізо", "+20% до урону всієї зброї", c => c.DamageMultiplier *= 1.2f),
        new WeaponBuffUpgrade("Швидкий спуск", "+15% до темпу стрільби", c => c.FireRateMultiplier *= 1.15f),
        new WeaponBuffUpgrade("Спритні руки", "+20% до швидкості перезарядки", c => c.ReloadRateMultiplier *= 1.2f),
        new WeaponBuffUpgrade("Важкі кулі", "+20% до швидкості снарядів", c => c.BulletSpeedMultiplier *= 1.2f, UpgradeRarity.Rare),
        new WeaponBuffUpgrade("Пробивні набої", "+1 пробиття для всіх снарядів", c => c.Pierce++, UpgradeRarity.Rare),
        new WeaponBuffUpgrade("Роздвоєння", "+1 снаряд до всієї зброї", c => c.ExtraProjectiles++, UpgradeRarity.Epic),
        new WeaponBuffUpgrade("Відбивання", "+1 відскок від стін", c => c.Bounces++, UpgradeRarity.Rare),
        new WeaponBuffUpgrade("Широка хвиля", "+2 до розльоту снарядів", c => c.Spread += 2f),

        // Стихійні ефекти
        new WeaponBuffUpgrade("Кремінь", "+12% шанс підпалити ворога", c => c.GetComponent<PlayerStats>().BurnChance = Mathf.Min(1f, c.GetComponent<PlayerStats>().BurnChance + 0.12f)),
        new WeaponBuffUpgrade("Жар полум'я", "+35% до урону горіння", c => c.GetComponent<PlayerStats>().BurnMultiplier *= 1.35f, UpgradeRarity.Rare),
        new WeaponBuffUpgrade("Мороз", "+10% шанс заморозити ворога", c => c.GetComponent<PlayerStats>().FreezeChance = Mathf.Min(1f, c.GetComponent<PlayerStats>().FreezeChance + 0.1f)),
        new WeaponBuffUpgrade("Розряд", "+12% шанс викликати блискавку", c => c.GetComponent<PlayerStats>().LightningChance = Mathf.Min(1f, c.GetComponent<PlayerStats>().LightningChance + 0.12f)),
        new WeaponBuffUpgrade("Сильний розряд", "+8 до урону блискавки", c => c.GetComponent<PlayerStats>().LightningDamage += 8f, UpgradeRarity.Rare),
        new WeaponBuffUpgrade("Прокляття", "+10% шанс накласти прокляття", c => c.GetComponent<PlayerStats>().CurseChance = Mathf.Min(1f, c.GetComponent<PlayerStats>().CurseChance + 0.1f)),
        new WeaponBuffUpgrade("Смертельний удар", "+8% шанс миттєвої кари", c => c.GetComponent<PlayerStats>().SmiteChance = Mathf.Min(0.6f, c.GetComponent<PlayerStats>().SmiteChance + 0.08f), UpgradeRarity.Epic),
        new WeaponBuffUpgrade("Кара небесна", "+10 до урону кари", c => c.GetComponent<PlayerStats>().SmiteDamage += 10f, UpgradeRarity.Rare),
        new WeaponBuffUpgrade("Щепки", "Вбивство може розлітатися уламками", c => c.GetComponent<PlayerStats>().Splinter = true, UpgradeRarity.Rare),
        new WeaponBuffUpgrade("Асасин", "Перший удар по ворогу завдає +60% урону", c => c.GetComponent<PlayerStats>().Assassin = true, UpgradeRarity.Epic),

        // Критичні удари
        new WeaponBuffUpgrade("Точний приціл", "+8% шанс критичного удару", c => c.GetComponent<PlayerStats>().CritChance = Mathf.Min(0.85f, c.GetComponent<PlayerStats>().CritChance + 0.08f)),
        new WeaponBuffUpgrade("Смертельний постріл", "+0.5x до множника криту", c => c.GetComponent<PlayerStats>().CritMultiplier += 0.5f, UpgradeRarity.Rare),

        // Герой
        new PlayerBuffUpgrade("Швидкі ноги", "+12% до швидкості пересування", p => p.MoveSpeedMultiplier *= 1.12f),
        new PlayerBuffUpgrade("Магніт", "+1.5 до радіусу збору досвіду", p => p.PickupRadius += 1.5f),
        new PlayerBuffUpgrade("Жадібність", "+15% до отриманого досвіду", p => p.XpGainMultiplier *= 1.15f, UpgradeRarity.Rare),
        new PlayerBuffUpgrade("Дальший світло", "+2 до радіусу ліхтаря", p => p.VisionRange += 2f),
        new PlayerBuffUpgrade("Великий калібр", "+15% до розміру снарядів", p => p.BulletSizeMultiplier *= 1.15f),
        new PlayerBuffUpgrade("Місткий магазин", "+2 патрони до всієї зброї", p => p.MaxAmmoBonus += 2),
        new PlayerBuffUpgrade("Проворні духи", "+20% до швидкості призивів", p => p.SummonSpeedMultiplier *= 1.2f),
        new PlayerBuffUpgrade("Сильні духи", "+20% до урону призивів", p => p.SummonDamageMultiplier *= 1.2f, UpgradeRarity.Rare),
        new PlayerBuffUpgrade("Дзеркало заднього виду", "Вивчає скіл: задній огляд у куті екрана. [Q] — увімкнути/вимкнути. Повтор — ширший кут.", p => p.RearViewMirrorLevel++, UpgradeRarity.Rare),
        new PlayerBuffUpgrade("Часовий зсув", "Вивчає скіл Titanfall: [E] — перехід у паралельний час. Невразливість, потім вибух повернення.", p => p.TimeDisplacementLevel++, UpgradeRarity.Epic),

        // Виживання
        new SurvivalUpgrade("Міцне тіло", "+1 максимальне HP", p => p.AddMaxHp(1f, true)),
        new SurvivalUpgrade("Перев'язка", "Відновлює 1 HP", p => p.Heal(1f)),
        new SurvivalUpgrade("Регенерація", "+0.05 HP на секунду", p => p.Regen += 0.05f, UpgradeRarity.Rare),
        new SurvivalUpgrade("Броня", "-12% до вхідного урону", p => p.DamageReduction = Mathf.Min(0.8f, p.DamageReduction + 0.12f), UpgradeRarity.Rare),

        // Призиви
        new SummonUpgrade(SummonKind.Spirit, "Дух помсти", "Дух полює на ворогів після вбивства"),
        new SummonUpgrade(SummonKind.Scythe, "Коса", "Обертається навколо героя й розсікає ворогів"),
        new SummonUpgrade(SummonKind.Dragon, "Дракон", "Вогняний супутник, що атакує все навколо"),
        new SummonUpgrade(SummonKind.Butterfly, "Метелик", "Заморожує ворогів при контакті"),
        new SummonUpgrade(SummonKind.FrenzyBat, "Скажений кажан", "Дуже швидкий, але менш сильний супутник")
    };

    public static void FillRandomDistinct(UpgradeDefinition[] dest, PlayerCombat combat)
    {
        if (dest == null || dest.Length == 0)
            return;

        var bag = new List<UpgradeDefinition>();
        for (int i = 0; i < All.Length; i++)
        {
            var card = All[i];
            bag.Add(card);
        }

        for (int i = 0; i < dest.Length; i++)
        {
            if (bag.Count == 0)
            {
                dest[i] = null;
                continue;
            }

            int totalWeight = 0;
            for (int j = 0; j < bag.Count; j++)
                totalWeight += Weight(bag[j]);

            int roll = Random.Range(0, Mathf.Max(1, totalWeight));
            int picked = bag.Count - 1;
            for (int j = 0; j < bag.Count; j++)
            {
                roll -= Weight(bag[j]);
                if (roll < 0)
                {
                    picked = j;
                    break;
                }
            }

            dest[i] = bag[picked];
            bag.RemoveAt(picked);
        }
    }

    static int Weight(UpgradeDefinition card)
    {
        switch (card.Rarity)
        {
            case UpgradeRarity.Rare: return 3;
            case UpgradeRarity.Epic: return 1;
            default: return 6;
        }
    }
}
