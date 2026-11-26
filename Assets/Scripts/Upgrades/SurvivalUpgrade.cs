using System;

public sealed class SurvivalUpgrade : StatsUpgrade
{
    public override UpgradeKind Kind { get { return UpgradeKind.Survival; } }

    public SurvivalUpgrade(string title, string body, Action<PlayerStats> apply,
        UpgradeRarity rarity = UpgradeRarity.Common)
        : base(title, body, apply, rarity)
    {
    }
}
