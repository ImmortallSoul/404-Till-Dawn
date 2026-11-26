using System;

public abstract class UpgradeDefinition
{
    public string Title { get; private set; }
    public string Body { get; private set; }
    public UpgradeRarity Rarity { get; private set; }
    public abstract UpgradeKind Kind { get; }

    protected UpgradeDefinition(string title, string body, UpgradeRarity rarity = UpgradeRarity.Common)
    {
        Title = title;
        Body = body;
        Rarity = rarity;
    }

    public abstract void Apply(PlayerStats player, PlayerCombat combat);
}

public abstract class CombatUpgrade : UpgradeDefinition
{
    readonly Action<PlayerCombat> _apply;

    protected CombatUpgrade(string title, string body, Action<PlayerCombat> apply,
        UpgradeRarity rarity = UpgradeRarity.Common) : base(title, body, rarity)
    {
        _apply = apply;
    }

    public override void Apply(PlayerStats player, PlayerCombat combat)
    {
        if (_apply != null && combat != null)
            _apply(combat);
    }
}

public abstract class StatsUpgrade : UpgradeDefinition
{
    readonly Action<PlayerStats> _apply;

    protected StatsUpgrade(string title, string body, Action<PlayerStats> apply,
        UpgradeRarity rarity = UpgradeRarity.Common) : base(title, body, rarity)
    {
        _apply = apply;
    }

    public override void Apply(PlayerStats player, PlayerCombat combat)
    {
        if (_apply != null && player != null)
            _apply(player);
    }
}
