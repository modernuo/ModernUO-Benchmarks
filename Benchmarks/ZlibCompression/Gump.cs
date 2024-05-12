using Server.Gumps;
using System.Runtime.CompilerServices;

namespace Gumps.MockedGumps;

public sealed class FakeGump : DynamicGump
{
    private readonly string _petName;

    public FakeGump(string petName) : base(50, 50)
    {
        _petName = petName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PublicBuildLayout(ref DynamicGumpBuilder builder)
    {
        BuildLayout(ref builder);
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        for(int i = 0; i < 10; i++)
        {
            AddRandomData(ref builder);
        }
    }

    private void AddRandomData(ref DynamicGumpBuilder builder)
    {
        builder.AddBackground(10, 10, 265, 140, 0x242C);

        builder.AddItem(205, 40, 0x4);
        builder.AddItem(227, 40, 0x5);

        builder.AddItem(180, 78, 0xCAE);
        builder.AddItem(195, 90, 0xCAD);
        builder.AddItem(218, 95, 0xCB0);

        builder.AddHtml(30, 30, 150, 75, "<div align=center>Wilt thou sanctify the resurrection of:</div>");
        builder.AddHtml(30, 70, 150, 25, $"<CENTER>{_petName}</CENTER>", true);

        builder.AddButton(40, 105, 0x81A, 0x81B, 0x1); // Okay
        builder.AddButton(110, 105, 0x819, 0x818, 0x2); // Cancel
    }
}