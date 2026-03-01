using System.Collections.Generic;

namespace DdoDatApi.Models;

public record SetBonusInfo(string Name, List<string> Descriptions);
public record EffectInfo(string Name, string? Description);
public record TableRow(string Field, string Value);

public class ItemViewModel
{
    private const uint WT_Weapon   = 0x00020081;
    private const uint WT_Shield   = 0x00010081;
    private const uint WT_Augment  = 0x000D0081;
    private const uint WT_Jewelry  = 0x00070081;
    private const uint WT_Clothing = 0x00030081;

    public string Title      { get; set; }
    public string Theme      { get; set; }
    public uint   WeenieType { get; set; }

    // Text-side table
    public List<TableRow>   TableRows    { get; set; } = new();
    public List<EffectInfo> Effects      { get; set; } = new();
    public List<string>     AugmentSlots { get; set; } = new();
    public SetBonusInfo SetBonus1 { get; set; }
    public SetBonusInfo SetBonus2 { get; set; }

    // Mock-side (DDO tooltip panel)
    public string ItemIconUrl        { get; set; }
    public string ItemName           { get; set; }
    public string TypeLine           { get; set; }
    public bool   AcceptsSentience   { get; set; }
    public int?   MinLevel           { get; set; }
    public string Binding            { get; set; }
    public string ClickieText        { get; set; }
    public string Description        { get; set; }
    public string DamageLine         { get; set; }
    public string HitDmgAbility      { get; set; }
    public float  BaseDamageRating   { get; set; }
    public int?   ShieldBonus        { get; set; }
    public int?   DamageReduction    { get; set; }
    public int?   Durability         { get; set; }
    public string DurabilityMaterial { get; set; }
    public int?   DurabilityHardness { get; set; }
    public string Weight             { get; set; }
    public int?   BaseValue          { get; set; }
    public string BoundLine          { get; set; }

    public bool IsWeapon    => WeenieType == WT_Weapon;
    public bool IsShield    => WeenieType == WT_Shield;
    public bool IsAugment   => WeenieType == WT_Augment;
    public bool IsEquipSlot => WeenieType == WT_Jewelry || WeenieType == WT_Clothing;
    public bool IsPlatinum  => Theme == "platinum";
}
