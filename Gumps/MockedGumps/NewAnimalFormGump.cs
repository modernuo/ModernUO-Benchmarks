using Server;
using Server.Gumps;
using Server.Gumps.Dynamic;
using static Server.Spells.Ninjitsu.AnimalForm;

namespace Gumps.MockedGumps;

public class NewAnimalFormGump : DynamicLayoutGump
{
    private readonly int _ninjitsu;
    private readonly AnimalFormEntry[] _entries;

    public NewAnimalFormGump(int ninjitsu, AnimalFormEntry[] entries) : base(50, 50)
    {
        _ninjitsu = ninjitsu;
        _entries = entries;
    }

    protected override void BuildLayout(ref GumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 520, 404, 0x13BE);
        builder.AddImageTiled(10, 10, 500, 20, 0xA40);
        builder.AddImageTiled(10, 40, 500, 324, 0xA40);
        builder.AddImageTiled(10, 374, 500, 20, 0xA40);
        builder.AddAlphaRegion(10, 10, 500, 384);

        builder.AddHtmlLocalized(14, 12, 500, 20, 1063394, 0x7FFF); // <center>Polymorph Selection Menu</center>

        builder.AddButton(10, 374, 0xFB1, 0xFB2, 0);
        builder.AddHtmlLocalized(45, 376, 450, 20, 1011012, 0x7FFF); // CANCEL

        var current = 0;

        for (var i = 0; i < _entries.Length; ++i)
        {
            var enabled = _ninjitsu >= _entries[i].ReqSkill; //&& BaseFormTalisman.EntryEnabled(_caster, _entries[i].Type);

            var page = current / 10 + 1;
            var pos = current % 10;

            if (pos == 0)
            {
                if (page > 1)
                {
                    builder.AddButton(400, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, page);
                    builder.AddHtmlLocalized(440, 376, 60, 20, 1043353, 0x7FFF); // Next
                }

                builder.AddPage(page);

                if (page > 1)
                {
                    builder.AddButton(300, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 1);
                    builder.AddHtmlLocalized(340, 376, 60, 20, 1011393, 0x7FFF); // Back
                }
            }

            if (!enabled)
            {
                continue;
            }

            var entry = _entries[i];

            var x = pos % 2 == 0 ? 14 : 264;
            var y = pos / 2 * 64 + 44;
            Rectangle2D b = new(0, 0, 100, 100); //ItemBounds.Table[entry.ItemID];

            builder.AddImageTiledButton(x, y, 0x918, 0x919, i + 1, GumpButtonType.Reply, 0, entry.ItemID,
                entry.Hue, 40 - b.Width / 2 - b.X, 30 - b.Height / 2 - b.Y, entry.Tooltip);

            builder.AddHtmlLocalized(x + 84, y, 250, 60, entry.Name, 0x7FFF);

            current++;
        }
    }
}