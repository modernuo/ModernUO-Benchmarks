﻿using Server;
using Server.Gumps;
using static Server.Spells.Ninjitsu.AnimalForm;

namespace Gumps.MockedGumps;

public class OldAnimalFormGump : Gump
{
    public OldAnimalFormGump(int _ninjitsu, AnimalFormEntry[] entries) : base(50, 50)
    {
        AddPage(0);

        AddBackground(0, 0, 520, 404, 0x13BE);
        AddImageTiled(10, 10, 500, 20, 0xA40);
        AddImageTiled(10, 40, 500, 324, 0xA40);
        AddImageTiled(10, 374, 500, 20, 0xA40);
        AddAlphaRegion(10, 10, 500, 384);

        AddHtmlLocalized(14, 12, 500, 20, 1063394, 0x7FFF); // <center>Polymorph Selection Menu</center>

        AddButton(10, 374, 0xFB1, 0xFB2, 0);
        AddHtmlLocalized(45, 376, 450, 20, 1011012, 0x7FFF); // CANCEL

        var current = 0;

        for (var i = 0; i < entries.Length; ++i)
        {
            var enabled = _ninjitsu >= entries[i].ReqSkill; //&& BaseFormTalisman.EntryEnabled(caster, entries[i].Type);

            var page = current / 10 + 1;
            var pos = current % 10;

            if (pos == 0)
            {
                if (page > 1)
                {
                    AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, page);
                    AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next
                }

                AddPage(page);

                if (page > 1)
                {
                    AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 1);
                    AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
                }
            }

            if (!enabled)
            {
                continue;
            }

            var x = pos % 2 == 0 ? 14 : 264;
            var y = pos / 2 * 64 + 44;

            var b = new Rectangle2D(0, 0, 100, 100); //ItemBounds.Table[entries[i].ItemID];

            AddImageTiledButton(
                x,
                y,
                0x918,
                0x919,
                i + 1,
                GumpButtonType.Reply,
                0,
                entries[i].ItemID,
                entries[i].Hue,
                40 - b.Width / 2 - b.X,
                30 - b.Height / 2 - b.Y
            );
            AddTooltip(entries[i].Tooltip);
            AddHtmlLocalized(x + 84, y, 250, 60, entries[i].Name, 0x7FFF);

            current++;
        }
    }
}