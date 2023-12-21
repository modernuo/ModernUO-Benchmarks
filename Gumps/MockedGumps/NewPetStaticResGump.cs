using Server.Gumps;
using Server.Gumps.Static;

namespace Gumps.MockedGumps;

public sealed class NewPetStaticResurrectGump : StaticLayoutGump<NewPetStaticResurrectGump>
{
    public NewPetStaticResurrectGump() : base(50, 50)
    {
    }

    protected override void BuildStaticLayout(ref GumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(10, 10, 265, 140, 0x242C);

        builder.AddItem(205, 40, 0x4);
        builder.AddItem(227, 40, 0x5);

        builder.AddItem(180, 78, 0xCAE);
        builder.AddItem(195, 90, 0xCAD);
        builder.AddItem(218, 95, 0xCB0);

        builder.AddHtml(30, 30, 150, 75, "<div align=center>Wilt thou sanctify the resurrection of:</div>");
        builder.AddHtml(30, 70, 150, 25, "<CENTER>a horse</CENTER>", true);

        builder.AddButton(40, 105, 0x81A, 0x81B, 0x1); // Okay
        builder.AddButton(110, 105, 0x819, 0x818, 0x2); // Cancel
    }
}