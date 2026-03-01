using DdoDatApi.Caching;
using DdoDatApi.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using VoK.Sdk.Ddo.Enums;
using VoK.Sdk.Enums;
using VoK.Sdk.Properties;

namespace DdoDatApi.Controllers;

/// <summary>
/// Returns HTML pages displaying item data from DbProperties.
/// </summary>
[Route("[controller]")]
public class ItemController : Controller
{
    private const uint WT_Weapon   = 0x00020081;
    private const uint WT_Shield   = 0x00010081;
    private const uint WT_Jewelry  = 0x00070081;
    private const uint WT_Clothing = 0x00030081;
    private const uint WT_Augment  = 0x000D0081;

    private const uint SetBonusMasterId = 0x7902F3F9;

    /// <summary>
    /// Returns an HTML page showing the item's stats on the left and a DDO tooltip mock on the right.
    /// </summary>
    /// <param name="id">DbProperties ID (hex with 0x prefix, or decimal). 0x70* IDs are converted to 0x79*.</param>
    [HttpGet("id/{id}")]
    public IActionResult GetById(string id)
    {
        if (!id.IsValid(out var datId, out var error))
            return error;

        if (datId >= 0x70000000 && datId < 0x71000000)
            datId += 0x09000000;

        if (datId < 0x78000000 || datId > 0x79FFFFFF)
            return new BadRequestObjectResult("ID not in valid range");

        var props = DatSource.PropertyMaster.GetPropertyCollection(datId);
        if (props == null) return NotFound();

        return View(BuildViewModel(datId, props));
    }

    // ── View model builder ────────────────────────────────────────────────

    private static ItemViewModel BuildViewModel(uint itemId, IPropertyCollection props)
    {
        var weenieType = props.GetWeenieType();
        var theme      = GetTheme(props);
        var effects    = GetEffectNames(props, out var clickie, out var effectCtx, out var boundLine);
        var augSlots   = GetAugmentSlots(props);
        var setBonus1  = GetSetBonusInfo(props, (uint)DdoProperty.Item_SetBonus_1);
        var setBonus2  = GetSetBonusInfo(props, (uint)DdoProperty.Item_SetBonus_2);

        // Prefer Usage_MinLevel from raw props; fall back to Treasure_BaseLevel set by effects
        var minLevel = props.GetInt32PropertyValue((uint)DdoProperty.Usage_MinLevel);
        if (minLevel == null || minLevel == 0) {
            var baseLevel = effectCtx.GetValue((uint)DdoProperty.Treasure_BaseLevel);
            if (baseLevel is > 0) minLevel = (int)baseLevel.Value;
        }

        // Durability = base + any bonus added by static mutator (Item Hardness)
        int? durability = null;
        string durabilityMaterial = null;
        int? durabilityHardness = null;
        if (weenieType != WT_Augment) {
            var durBase = props.GetInt32PropertyValue((uint)DdoProperty.MaxDurability_Base);
            if (durBase != null) {
                var durBonus = effectCtx.GetValue((uint)DdoProperty.MaxDurability_Effect);
                durability = durBase.Value + (int)(durBonus ?? 0f);
                durabilityMaterial = GetMaterialName(props);
                var h = effectCtx.GetValue((uint)DdoProperty.Durability_Hardness);
                if (h is > 0) durabilityHardness = (int)h.Value;
            }
        }

        var baseValueF = effectCtx.GetValue((uint)DdoProperty.Item_Value);
        int? baseValue = baseValueF is > 0 ? (int)(baseValueF.Value / 1000f) : null;

        var typeLine = weenieType switch {
            WT_Weapon                     => GetEnumDisplayName<WeaponType>(props, (uint)DdoProperty.Combat_WeaponType),
            WT_Shield                     => GetEnumDisplayName<ShieldType>(props, (uint)DdoProperty.Combat_ShieldType),
            WT_Jewelry or WT_Clothing     => GetEquipSlot(props),
            WT_Augment                    => GetBitFieldValues(props, (uint)DdoProperty.Augment_SlotTypes).FirstOrDefault(),
            _                             => null
        };

        var acceptsSentience = props.GetBytePropertyValue((uint)DdoProperty.AcceptsSentience) == 1;
        var binding          = GetBindingText(props, boundLine);
        var desc             = props.GetStringInfoProperty((uint)DdoProperty.Item_Description)
                                   ?.GetText(DatSource.PropertyMaster, null, null);
        var enc = props.GetInt32PropertyValue((uint)DdoProperty.Inventory_Encumbrance);

        string damageLine = null, hitDmgAbility = null;
        if (weenieType == WT_Weapon || weenieType == WT_Shield) {
            damageLine    = BuildDamageLine(props);
            hitDmgAbility = BuildHitDmgAbility(props);
        }

        int? shieldBonus = null, damageReduction = null;
        if (weenieType == WT_Shield) {
            shieldBonus     = props.GetInt32PropertyValue((uint)DdoProperty.Combat_ShieldBonus);
            var dr          = props.GetInt32PropertyValue((uint)DdoProperty.Combat_BlockingDamageReduction);
            damageReduction = dr is > 0 ? dr : null;
        }

        return new ItemViewModel {
            Title            = props.Name ?? "Item",
            Theme            = theme,
            WeenieType       = weenieType,
            TableRows        = BuildTableRows(props, weenieType, clickie, minLevel, durability, boundLine),
            Effects          = effects,
            AugmentSlots     = augSlots,
            SetBonus1        = setBonus1,
            SetBonus2        = setBonus2,
            ItemIconUrl      = $"/Image/Icon/0x{itemId:X8}",
            ItemName         = props.Name ?? "Unknown",
            TypeLine         = typeLine,
            AcceptsSentience = acceptsSentience,
            MinLevel         = minLevel is > 0 ? minLevel : null,
            Binding          = binding,
            ClickieText      = clickie != null ? BuildClickieText(clickie) : null,
            Description      = desc,
            DamageLine       = damageLine,
            HitDmgAbility    = hitDmgAbility,
            ShieldBonus      = shieldBonus,
            DamageReduction  = damageReduction,
            Durability         = durability,
            DurabilityMaterial = durabilityMaterial,
            DurabilityHardness = durabilityHardness,
            Weight             = enc != null ? $"{enc.Value / 100.0:0.00} lbs" : null,
            BaseValue          = baseValue,
            BoundLine          = boundLine,
        };
    }

    private static List<TableRow> BuildTableRows(
        IPropertyCollection props, uint weenieType, ClickieData clickie, int? minLevel, int? durability, string boundLine)
    {
        var rows = new List<TableRow>();

        rows.Add(new TableRow("Name", props.Name ?? "Unknown"));

        AppendTypeFieldsAfterName(props, weenieType, rows);

        if (minLevel is > 0)
            rows.Add(new TableRow("Minimum Level", minLevel.Value.ToString()));

        var binding = GetBindingText(props, boundLine);
        if (binding != null)
            rows.Add(new TableRow(weenieType == WT_Augment ? "Bind Status" : "Binding", binding));

        if (weenieType == WT_Weapon && props.GetBytePropertyValue((uint)DdoProperty.AcceptsSentience) == 1)
            rows.Add(new TableRow("Accepts Sentience", "Yes"));

        if (clickie != null)
            rows.Add(new TableRow("Clickie", BuildClickieText(clickie)));

        var desc = props.GetStringInfoProperty((uint)DdoProperty.Item_Description)
            ?.GetText(DatSource.PropertyMaster, null, null);
        if (!string.IsNullOrWhiteSpace(desc))
            rows.Add(new TableRow("Description", desc));

        if (weenieType != WT_Augment) {
            var mat = GetMaterialName(props);
            if (mat != null) rows.Add(new TableRow("Material", mat));

            if (durability != null) rows.Add(new TableRow("Durability", durability.Value.ToString()));
        }

        var enc = props.GetInt32PropertyValue((uint)DdoProperty.Inventory_Encumbrance);
        if (enc != null)
            rows.Add(new TableRow("Weight", $"{enc.Value / 100.0:0.00} lbs"));

        return rows;
    }

    private static void AppendTypeFieldsAfterName(IPropertyCollection props, uint weenieType, List<TableRow> rows)
    {
        if (weenieType == WT_Weapon) {
            var wt = GetEnumDisplayName<WeaponType>(props, (uint)DdoProperty.Combat_WeaponType);
            if (wt != null) rows.Add(new TableRow("Weapon Type", wt));

            var dmg = BuildDamageLine(props);
            if (dmg != null) rows.Add(new TableRow("Damage", dmg));

            var hd = BuildHitDmgAbility(props);
            if (hd != null) rows.Add(new TableRow("Hit/Dmg Ability", hd));

            if (GetBitFieldValues(props, (uint)DdoProperty.Inventory_PrecludedSlot).Contains("Weapon2"))
                rows.Add(new TableRow("Handedness", "Two-handed"));
        }
        else if (weenieType == WT_Shield) {
            var st = GetEnumDisplayName<ShieldType>(props, (uint)DdoProperty.Combat_ShieldType);
            if (st != null) rows.Add(new TableRow("Shield Type", st));

            var shBonus = props.GetInt32PropertyValue((uint)DdoProperty.Combat_ShieldBonus);
            if (shBonus != null) rows.Add(new TableRow("Shield Bonus", shBonus.Value.ToString()));

            var maxDex = props.GetInt32PropertyValue((uint)DdoProperty.Combat_MaxDexBonus);
            if (maxDex is < 99) rows.Add(new TableRow("Max Dex Bonus", maxDex.Value.ToString()));

            var dr = props.GetInt32PropertyValue((uint)DdoProperty.Combat_BlockingDamageReduction);
            if (dr is > 0) rows.Add(new TableRow("Damage Reduction", dr.Value.ToString()));

            var acp = props.GetInt32PropertyValue((uint)DdoProperty.Combat_SkillCheckPenalty);
            if (acp is < 0) rows.Add(new TableRow("Armor Check Penalty", acp.Value.ToString()));
            else if (acp is > 0) rows.Add(new TableRow("Armor Check Penalty", $"-{acp.Value}"));

            var sf = props.GetInt32PropertyValue((uint)DdoProperty.Spell_SpellFailureChance);
            if (sf is > 0) rows.Add(new TableRow("Arcane Spell Failure", $"{sf}%"));

            var dmg = BuildDamageLine(props);
            if (dmg != null) rows.Add(new TableRow("Damage", dmg));

            var hd = BuildHitDmgAbility(props);
            if (hd != null) rows.Add(new TableRow("Hit/Dmg Ability", hd));
        }
        else if (weenieType == WT_Jewelry || weenieType == WT_Clothing) {
            var slot = GetEquipSlot(props);
            if (slot != null) rows.Add(new TableRow("Slot", slot));
        }
        else if (weenieType == WT_Augment) {
            var augType = GetBitFieldValues(props, (uint)DdoProperty.Augment_SlotTypes).FirstOrDefault();
            if (augType != null) rows.Add(new TableRow("Augment Type", augType));
        }
    }

    // ── Property helpers ──────────────────────────────────────────────────

    private static string GetTheme(IPropertyCollection props)
    {
        var tt = props.GetEnumProperty((uint)DdoProperty.Treasure_Type)?.UInt32Value;
        return tt switch {
            2 => "gold",      // Named
            3 => "platinum",  // Raid
            1 => "silver",    // Random
            _ => "copper"
        };
    }

    private static string BuildDamageLine(IPropertyCollection props)
    {
        var diceStr = props.GetStringProperty((uint)DdoProperty.DamageValue)?.StringValue;
        if (diceStr == null) return null;

        var modifier  = props.GetFloatPropertyValue((uint)DdoProperty.BaseWeaponDamageDiceModifier) ?? 1.0f;
        var critRange = props.GetInt32PropertyValue((uint)DdoProperty.Combat_CriticalHitRange) ?? 0;
        var critMod   = props.GetInt32PropertyValue((uint)DdoProperty.Combat_CriticalHitMod) ?? 2;

        var damageType = GetBitFieldValues(props, (uint)DdoProperty.DamageFlags)
            .FirstOrDefault(f => f != "Undef" && !f.Contains("SpellPoint")) ?? "";

        var diceDisplay = Math.Abs(modifier - 1.0f) < 0.0001f
            ? diceStr
            : $"{modifier:0.##}[{diceStr}]";

        return $"{diceDisplay} {damageType} {21 - critRange}-20x{critMod}".Trim();
    }

    private static string BuildHitDmgAbility(IPropertyCollection props)
    {
        var hitFlags = GetBitFieldValues(props, (uint)DdoProperty.Combat_HitAbilityMod_Multiple).ToList();
        var dmgFlags = GetBitFieldValues(props, (uint)DdoProperty.Combat_DamageAbilityMod_Multiple).ToList();
        if (!hitFlags.Any() && !dmgFlags.Any()) return null;

        static string Abbrev(string s) => s?.Length >= 3 ? s.Substring(0, 3) : s ?? "?";
        return $"{Abbrev(hitFlags.FirstOrDefault())}/{Abbrev(dmgFlags.FirstOrDefault())}";
    }

    private static readonly uint[] StaticMutatorIds = [
        0x79029FD6, // Minimum Level
        0x7902B139, // Item Hardness / Durability
        0x7902B13A, // Item Value
    ];

    private static void ApplyStaticMutators(EffectContext ctx)
    {
        foreach (var id in StaticMutatorIds) {
            var mutProps = DatSource.PropertyMaster.GetPropertyCollection(id);
            if (mutProps != null) EffectResolver.ApplyMods(mutProps, ctx);
        }
    }

    private static List<EffectInfo> GetEffectNames(IPropertyCollection props, out ClickieData clickie, out EffectContext effectCtx, out string boundLine)
    {
        clickie = null;
        boundLine = null;
        var effectsArray = props.GetArrayProperty((uint)DdoProperty.Effect_OnCreationEffects);
        if (effectsArray == null) { effectCtx = new EffectContext(props); return new List<EffectInfo>(); }

        var canBeUsed = props.GetBytePropertyValue((uint)DdoProperty.Usage_CanBeUsed) == 1;
        var ctx       = new EffectContext(props);

        // Collect all effect entries up front
        var entries = new List<EffectEntry>();
        foreach (var entry in effectsArray.Properties) {
            if (entry.PropertyId != (uint)DdoProperty.Effect_Entry) continue;
            if (entry is not IArrayProperty entryArr) continue;

            var effectProp = entryArr.GetInt32Property((uint)DdoProperty.Effect);
            if (effectProp == null) continue;

            var did = (uint)effectProp.Int32Value;
            if (did == 0) continue;

            var effectId    = EffectResolver.ConvertId(did);
            var effectProps = DatSource.PropertyMaster.GetPropertyCollection(effectId);

            string fallback = null;
            if (effectProps == null) {
                fallback = effectProp.ReferencedObject as string;
                if (string.IsNullOrEmpty(fallback))
                    DatCache.Index.NameLookup.TryGetValue(effectId, out fallback);
            }

            entries.Add(new EffectEntry(effectId, effectProps, fallback));
        }

        // Pass 1: apply all mods from all effects into context before resolving any names.
        foreach (var entry in entries) {
            if (entry.Props != null) EffectResolver.ApplyMods(entry.Props, ctx);
        }

        // Apply static mutators now that Treasure_BaseLevel is established.
        ApplyStaticMutators(ctx);

        effectCtx = ctx;

        // Pass 2: resolve each effect's display name using the fully-populated context
        var names       = new List<EffectInfo>();
        string spellName    = null;
        int?   spellCl      = null;
        string maxCharges   = null;
        string rechargeText = null;

        foreach (var entry in entries) {
            // Skip system/internal effects that carry a mutation type flag
            if (entry.Props != null) {
                var mutType = entry.Props.GetBitFieldProperty((uint)DdoProperty.Effect_MutationType);
                if (mutType?.Values?.Any() == true) continue;
            }

            EffectInfo resolved;
            if (entry.Props != null) {
                resolved = EffectResolver.ResolveName(entry.Props, ctx);
            } else {
                resolved = new EffectInfo(entry.FallbackName, null);
            }

            if (string.IsNullOrEmpty(resolved.Name)) continue;

            // "Bound to Account" is shown as a field, not in the effects list
            if (resolved.Name.Equals("Bound to Account", StringComparison.OrdinalIgnoreCase)) {
                boundLine = resolved.Name;
                continue;
            }

            if (canBeUsed) {
                if (resolved.Name.StartsWith("Spell: ", StringComparison.OrdinalIgnoreCase)) {
                    spellName = resolved.Name.Substring("Spell: ".Length);
                    spellCl   = TryGetCasterLevel(entry.EffectId);
                    continue;
                }
                if (resolved.Name.Contains("Max Charges", StringComparison.OrdinalIgnoreCase)) {
                    maxCharges = resolved.Name;
                    continue;
                }
                if (resolved.Name.StartsWith("Recharge", StringComparison.OrdinalIgnoreCase)) {
                    rechargeText = resolved.Name;
                    continue;
                }
            }

            names.Add(resolved);
        }

        if (spellName != null)
            clickie = new ClickieData(spellName, spellCl, maxCharges, rechargeText);

        return names;
    }

    private static int? TryGetCasterLevel(uint effectDid)
    {
        try {
            var convertedId = effectDid >= 0x70000000 && effectDid < 0x71000000
                ? effectDid + 0x09000000 : effectDid;
            var ep = DatSource.PropertyMaster.GetPropertyCollection(convertedId);
            if (ep == null) return null;

            var modArray = ep.GetArrayProperty((uint)DdoProperty.Mod_Array);
            if (modArray == null) return null;

            foreach (var mod in modArray.Properties) {
                if (mod is not IArrayProperty modArr) continue;
                var channel = modArr.GetEnumProperty((uint)DdoProperty.Mod_Channel);
                if (channel == null) continue;
                if (Enum.GetName(typeof(EffectChannel), channel.UInt32Value) == "CasterLevel") {
                    var floatCl = modArr.GetFloatPropertyValue((uint)DdoProperty.Spell_CasterLevel);
                    if (floatCl != null) return (int)floatCl.Value;
                    var intCl = modArr.GetInt32PropertyValue((uint)DdoProperty.Spell_CasterLevel);
                    if (intCl != null) return intCl.Value;
                }
            }
        }
        catch { }
        return null;
    }

    private static string BuildClickieText(ClickieData c)
    {
        var sb = new System.Text.StringBuilder(c.SpellName);
        if (c.CasterLevel != null) sb.Append($" (Caster Level: {c.CasterLevel})");
        sb.Append(" \u2014 ");
        if (c.MaxChargesText != null) sb.Append(c.MaxChargesText);
        if (c.RechargeText != null) {
            if (c.MaxChargesText != null) sb.Append(", ");
            sb.Append(c.RechargeText);
        }
        return sb.ToString();
    }

    private static List<string> GetAugmentSlots(IPropertyCollection props)
    {
        var arr = props.GetArrayProperty((uint)DdoProperty.Augment_SlotArray);
        if (arr == null) return new List<string>();

        var slots = new List<string>();
        foreach (var entry in arr.Properties) {
            if (entry.PropertyId != (uint)DdoProperty.Augment_SlotEntry) continue;
            if (entry is not IArrayProperty entryArr) continue;

            var nameProp = entryArr.GetStringInfoProperty((uint)DdoProperty.Augment_SlotName);
            if (nameProp == null || nameProp.Key == 0) continue;

            var text = nameProp.GetText(DatSource.PropertyMaster, null, null);
            if (string.IsNullOrEmpty(text)) continue;

            slots.Add(text);
        }
        return slots;
    }

    private static string GetBindingText(IPropertyCollection props, string boundLine = null)
    {
        var onAcquire = props.GetBytePropertyValue((uint)DdoProperty.Inventory_IsBoundOnAcquire);
        var onEquip   = props.GetBytePropertyValue((uint)DdoProperty.Inventory_IsBoundOnEquip);
        var toAccount = props.GetBytePropertyValue((uint)DdoProperty.Inventory_BoundToAccount);

        // Account-binding can come from the property OR from the effects pipeline (boundLine)
        var isAccountBound = toAccount == 1 ||
            (boundLine != null && boundLine.IndexOf("Account", StringComparison.OrdinalIgnoreCase) >= 0);

        var scope = isAccountBound ? "Account" : "Character";
        if (onAcquire == 1) return $"Bound to {scope} (from Acquisition)";
        if (onEquip   == 1) return $"Bound to {scope} (from Equipping)";
        return null;
    }

    private static string GetEquipSlot(IPropertyCollection props)
    {
        var slots = GetBitFieldValues(props, (uint)DdoProperty.Inventory_CompatibleSlot)
            .Where(f => f != "Equipment" && f != "Backpack")
            .Select(MapSlotName)
            .ToList();
        return slots.Count > 0 ? string.Join(", ", slots) : null;
    }

    private static string MapSlotName(string slot) => slot switch {
        "Finger1" => "First Finger",
        "Finger2" => "Second Finger",
        "Head"    => "Head",
        "Neck"    => "Neck",
        "Trinket" => "Trinket",
        "Cloak"   => "Back",
        "Arms"    => "Wrists",
        "Hands"   => "Gloves",
        "Chest"   => "Body",
        "Legs"    => "Legs",
        "Feet"    => "Boots",
        "Belt"    => "Waist",
        _         => slot
    };

    private static string GetMaterialName(IPropertyCollection props)
    {
        var did = props.GetInt32PropertyValue((uint)DdoProperty.Material);
        if (did == null || did == 0) return null;

        var mp = DatSource.PropertyMaster.GetPropertyCollection((uint)did.Value);
        if (mp == null) return null;

        return mp.GetStringInfoProperty((uint)DdoProperty.Material_Name)
                   ?.GetText(DatSource.PropertyMaster, null, null)
               ?? mp.Name;
    }

    private static readonly uint[] SetBonusDescriptionIds = [
        (uint)DdoProperty.SetBonus_TwoPiece_Description,
        (uint)DdoProperty.SetBonus_ThreePiece_Description,
        (uint)DdoProperty.SetBonus_FourPiece_Description,
        (uint)DdoProperty.SetBonus_FivePiece_Description,
    ];

    private static SetBonusInfo GetSetBonusInfo(IPropertyCollection props, uint propertyId)
    {
        var setBonusProp = props.GetEnumProperty(propertyId);
        if (setBonusProp == null) return null;

        var setId = setBonusProp.UInt32Value;
        try {
            var master = DatSource.PropertyMaster.GetPropertyCollection(SetBonusMasterId);
            var table  = master?.GetArrayProperty((uint)DdoProperty.SetBonus_Table);
            if (table == null) return null;

            foreach (var entry in table.Properties) {
                if (entry.PropertyId != (uint)DdoProperty.SetBonus_Entry) continue;
                if (entry is not IArrayProperty ea) continue;
                if (ea.GetUInt32PropertyValue((uint)DdoProperty.SetBonus_ID) != setId) continue;

                var name = ea.GetStringInfoProperty((uint)DdoProperty.SetBonus_Name)
                              ?.GetText(DatSource.PropertyMaster, null, null);
                if (string.IsNullOrEmpty(name)) return null;

                var descs = new List<string>();
                foreach (var descId in SetBonusDescriptionIds) {
                    var text = ea.GetStringInfoProperty(descId)
                                 ?.GetText(DatSource.PropertyMaster, null, null);
                    if (!string.IsNullOrEmpty(text))
                        descs.Add(text);
                }

                return new SetBonusInfo(name, descs);
            }
        }
        catch { }
        return null;
    }

    private static string GetEnumDisplayName<TEnum>(IPropertyCollection props, uint propertyId) where TEnum : struct, Enum
    {
        var prop = props.GetEnumProperty(propertyId);
        if (prop == null) return null;
        return Enum.IsDefined(typeof(TEnum), prop.UInt32Value)
            ? Enum.GetName(typeof(TEnum), prop.UInt32Value)
            : null;
    }

    private static IEnumerable<string> GetBitFieldValues(IPropertyCollection props, uint propertyId)
    {
        var prop = props.GetBitFieldProperty(propertyId);
        if (prop == null) return Enumerable.Empty<string>();
        return prop.Values ?? Enumerable.Empty<string>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private record ClickieData(string SpellName, int? CasterLevel, string MaxChargesText, string RechargeText);
    private record EffectEntry(uint EffectId, IPropertyCollection Props, string FallbackName);
}
