using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;

namespace Benchmarks.BenchmarkUtilities;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
public class BenchmarkLocalizationInterpolation
{
    private ObjectPropertyList _opl;
    private OldObjectPropertyList _oldOPL;
    private AosAttributes _attrs;
    private string _crafter;
    private int _hits;
    
    [GlobalSetup]
    public void Setup()
    {
        _hits = 157;
        _crafter = "Some Guy";
        InitializeAttributes();
        _opl = new ObjectPropertyList(null);
        _oldOPL = new OldObjectPropertyList(null);
    }

    public void InitializeAttributes()
    {
        var attrs = _attrs = new AosAttributes(null);
        attrs.WeaponDamage = 10;
        attrs.DefendChance = 11;
        attrs.BonusDex = 15;
        attrs.EnhancePotions = 40;
        attrs.CastRecovery = 100;
        attrs.CastSpeed = 25;
        attrs.AttackChance = 35;
        attrs.BonusHits = 10;
        attrs.BonusDex = 15;
        attrs.BonusInt = 25;
        attrs.LowerManaCost = 100;
        attrs.LowerRegCost = 80;
        attrs.Luck = 50;
        attrs.BonusMana = 25;
        attrs.RegenMana = 28;
        attrs.NightSight = 1;
        attrs.ReflectPhysical = 30;
        attrs.RegenStam = 27;
        attrs.RegenHits = 38;
        attrs.SpellDamage = 29;
        attrs.BonusStam = 5;
        attrs.WeaponSpeed = 43;
    }

    [Benchmark]
    public void BenchmarkOldOPL()
    {
        GetOldProperties(_attrs, _oldOPL);
        _oldOPL.Reset();
    }

    [Benchmark]
    public void BenchmarkStringInterpolatedOPL()
    {
        GetNewProperties(_attrs, _opl);
        _opl.Reset();
    }
    
    public void GetOldProperties(AosAttributes attrs, OldObjectPropertyList list)
    {
        if (_crafter != null)
        {
            list.Add(1050043, _crafter); // crafted by ~1_NAME~
        }

        int prop;

        if ((prop = attrs.WeaponDamage) != 0)
        {
            list.Add(1060401, prop.ToString()); // damage increase ~1_val~%
        }

        if ((prop = attrs.DefendChance) != 0)
        {
            list.Add(1060408, prop.ToString()); // defense chance increase ~1_val~%
        }

        if ((prop = attrs.BonusDex) != 0)
        {
            list.Add(1060409, prop.ToString()); // dexterity bonus ~1_val~
        }

        if ((prop = attrs.EnhancePotions) != 0)
        {
            list.Add(1060411, prop.ToString()); // enhance potions ~1_val~%
        }

        if ((prop = attrs.CastRecovery) != 0)
        {
            list.Add(1060412, prop.ToString()); // faster cast recovery ~1_val~
        }

        if ((prop = attrs.CastSpeed) != 0)
        {
            list.Add(1060413, prop.ToString()); // faster casting ~1_val~
        }

        if ((prop = attrs.AttackChance) != 0)
        {
            list.Add(1060415, prop.ToString()); // hit chance increase ~1_val~%
        }

        if ((prop = attrs.BonusHits) != 0)
        {
            list.Add(1060431, prop.ToString()); // hit point increase ~1_val~
        }

        if ((prop = attrs.BonusInt) != 0)
        {
            list.Add(1060432, prop.ToString()); // intelligence bonus ~1_val~
        }

        if ((prop = attrs.LowerManaCost) != 0)
        {
            list.Add(1060433, prop.ToString()); // lower mana cost ~1_val~%
        }

        if ((prop = attrs.LowerRegCost) != 0)
        {
            list.Add(1060434, prop.ToString()); // lower reagent cost ~1_val~%
        }

        if ((prop = attrs.BonusMana) != 0)
        {
            list.Add(1060439, prop.ToString()); // mana increase ~1_val~
        }

        if ((prop = attrs.RegenMana) != 0)
        {
            list.Add(1060440, prop.ToString()); // mana regeneration ~1_val~
        }

        if ((prop = attrs.ReflectPhysical) != 0)
        {
            list.Add(1060442, prop.ToString()); // reflect physical damage ~1_val~%
        }

        if ((prop = attrs.RegenStam) != 0)
        {
            list.Add(1060443, prop.ToString()); // stamina regeneration ~1_val~
        }

        if ((prop = attrs.RegenHits) != 0)
        {
            list.Add(1060444, prop.ToString()); // hit point regeneration ~1_val~
        }

        if ((prop = attrs.SpellDamage) != 0)
        {
            list.Add(1060483, prop.ToString()); // spell damage increase ~1_val~%
        }

        if ((prop = attrs.BonusStam) != 0)
        {
            list.Add(1060484, prop.ToString()); // stamina increase ~1_val~
        }

        if ((prop = attrs.BonusStr) != 0)
        {
            list.Add(1060485, prop.ToString()); // strength bonus ~1_val~
        }

        if ((prop = attrs.WeaponSpeed) != 0)
        {
            list.Add(1060486, prop.ToString()); // swing speed increase ~1_val~%
        }

        if (_hits >= 0)
        {
            list.Add(1060639, string.Format("{0}\t{1}", _hits, _hits)); // durability ~1_val~ / ~2_val~
        }
    }
    
    public void GetNewProperties(AosAttributes attrs, ObjectPropertyList list)
    {
        if (_crafter != null)
        {
            list.Add(1050043, _crafter); // crafted by ~1_NAME~
        }

        if (attrs.WeaponDamage != 0)
        {
            list.Add(1060401, $"{attrs.WeaponDamage}"); // damage increase ~1_val~%
        }

        if (attrs.DefendChance != 0)
        {
            list.Add(1060408, $"{attrs.DefendChance}"); // defense chance increase ~1_val~%
        }

        if (attrs.BonusDex != 0)
        {
            list.Add(1060409, $"{attrs.BonusDex}"); // dexterity bonus ~1_val~
        }

        if (attrs.EnhancePotions != 0)
        {
            list.Add(1060411, $"{attrs.EnhancePotions}"); // enhance potions ~1_val~%
        }

        if (attrs.CastRecovery != 0)
        {
            list.Add(1060412, $"{attrs.CastRecovery}"); // faster cast recovery ~1_val~
        }

        if (attrs.CastSpeed != 0)
        {
            list.Add(1060413, $"{attrs.CastSpeed}"); // faster casting ~1_val~
        }

        if (attrs.AttackChance != 0)
        {
            list.Add(1060415, $"{attrs.AttackChance}"); // hit chance increase ~1_val~%
        }

        if (attrs.BonusHits != 0)
        {
            list.Add(1060431, $"{attrs.BonusHits}"); // hit point increase ~1_val~
        }

        if (attrs.BonusInt != 0)
        {
            list.Add(1060432, $"{attrs.BonusInt}"); // intelligence bonus ~1_val~
        }

        if (attrs.LowerManaCost != 0)
        {
            list.Add(1060433, $"{attrs.LowerManaCost}"); // lower mana cost ~1_val~%
        }

        if (attrs.LowerRegCost != 0)
        {
            list.Add(1060434, $"{attrs.LowerRegCost}"); // lower reagent cost ~1_val~%
        }

        if (attrs.BonusMana != 0)
        {
            list.Add(1060439, $"{attrs.BonusMana}"); // mana increase ~1_val~
        }

        if (attrs.RegenMana != 0)
        {
            list.Add(1060440, $"{attrs.RegenMana}"); // mana regeneration ~1_val~
        }

        if (attrs.ReflectPhysical != 0)
        {
            list.Add(1060442, $"{attrs.ReflectPhysical}"); // reflect physical damage ~1_val~%
        }

        if (attrs.RegenStam != 0)
        {
            list.Add(1060443, $"{attrs.RegenStam}"); // stamina regeneration ~1_val~
        }

        if (attrs.RegenHits != 0)
        {
            list.Add(1060444, $"{attrs.RegenHits}"); // hit point regeneration ~1_val~
        }

        if (attrs.SpellDamage != 0)
        {
            list.Add(1060483, $"{attrs.SpellDamage}"); // spell damage increase ~1_val~%
        }

        if (attrs.BonusStam != 0)
        {
            list.Add(1060484, $"{attrs.BonusStam}"); // stamina increase ~1_val~
        }

        if (attrs.BonusStr != 0)
        {
            list.Add(1060485, $"{attrs.BonusStr}"); // strength bonus ~1_val~
        }

        if (attrs.WeaponSpeed != 0)
        {
            list.Add(1060486, $"{attrs.WeaponSpeed}"); // swing speed increase ~1_val~%
        }

        if (_hits >= 0)
        {
            list.Add(1060639, $"{_hits}\t{_hits}"); // durability ~1_val~ / ~2_val~
        }
    }
}
