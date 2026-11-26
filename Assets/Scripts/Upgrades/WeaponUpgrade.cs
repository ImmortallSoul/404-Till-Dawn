using System;

public sealed class WeaponUpgrade : CombatUpgrade
{
    public override UpgradeKind Kind { get { return UpgradeKind.Weapon; } }

    public WeaponUpgrade(string title, string body, Action<PlayerCombat> apply,
        UpgradeRarity rarity = UpgradeRarity.Common)
        : base(title, body, apply, rarity)
    {
    }
}
