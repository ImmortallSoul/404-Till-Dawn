public sealed class WeaponPickUpgrade : UpgradeDefinition
{
    public WeaponDefinition Weapon { get; private set; }

    public override UpgradeKind Kind { get { return UpgradeKind.Weapon; } }

    public WeaponPickUpgrade(WeaponDefinition weapon)
        : base(weapon.Title, weapon.BaseDescription + "\nЗамінити поточний ствол або підвищити його рівень", UpgradeRarity.Rare)
    {
        Weapon = weapon;
    }

    public override void Apply(PlayerStats player, PlayerCombat combat)
    {
        if (combat != null)
            combat.ReplaceOrLevelWeapon(Weapon);
    }
}
