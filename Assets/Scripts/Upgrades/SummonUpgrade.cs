using UnityEngine;

public sealed class SummonUpgrade : StatsUpgrade
{
    public SummonKind Minion { get; private set; }
    public override UpgradeKind Kind { get { return UpgradeKind.Summon; } }

    public SummonUpgrade(SummonKind minion, string title, string body)
        : base(title, body, p => SummonManager.Instance?.AddOrLevel(minion), UpgradeRarity.Epic)
    {
        Minion = minion;
    }
}
