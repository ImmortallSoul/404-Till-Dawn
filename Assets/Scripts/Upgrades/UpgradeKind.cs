public enum UpgradeKind
{
    Weapon,
    WeaponBuff,
    PlayerBuff,
    Survival,
    Summon
}

public enum UpgradeRarity
{
    Common,
    Rare,
    Epic
}

public static class UpgradeKindNames
{
    public static string Display(UpgradeKind kind)
    {
        switch (kind)
        {
            case UpgradeKind.Weapon: return "ЗБРОЯ";
            case UpgradeKind.WeaponBuff: return "БАФФ ЗБРОЇ";
            case UpgradeKind.PlayerBuff: return "БАФФ ГЕРОЯ";
            case UpgradeKind.Survival: return "ВИЖИВАННЯ";
            case UpgradeKind.Summon: return "ПРИЗИВ";
            default: return kind.ToString();
        }
    }
}
