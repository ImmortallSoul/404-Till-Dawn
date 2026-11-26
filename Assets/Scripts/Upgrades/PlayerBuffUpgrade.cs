using System;

public sealed class PlayerBuffUpgrade : StatsUpgrade
{
    public override UpgradeKind Kind { get { return UpgradeKind.PlayerBuff; } }

    public PlayerBuffUpgrade(string title, string body, Action<PlayerStats> apply,
        UpgradeRarity rarity = UpgradeRarity.Common)
        : base(title, body, apply, rarity)
    {
    }
}
