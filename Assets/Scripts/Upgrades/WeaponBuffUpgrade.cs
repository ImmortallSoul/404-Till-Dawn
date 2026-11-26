using System;

public sealed class WeaponBuffUpgrade : CombatUpgrade
{
    public override UpgradeKind Kind { get { return UpgradeKind.WeaponBuff; } }

    public WeaponBuffUpgrade(string title, string body, Action<PlayerCombat> apply,
        UpgradeRarity rarity = UpgradeRarity.Common)
        : base(title, body, apply, rarity)
    {
    }
}
