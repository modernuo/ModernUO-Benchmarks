using System.Runtime.CompilerServices;
using Server.Gumps;

namespace Server.Engines.Craft.Tests;

public class DynamicLayoutCraftGump : DynamicGump
{
    public enum CraftPage
    {
        None,
        PickResource,
        PickResource2
    }

    private const int LabelHue = 0x480;
    private const int LabelColor = 0x7FFF;
    private const int FontColor = 0xFFFFFF;
    private readonly CraftSystem m_CraftSystem;
    private readonly Mobile m_From;

    private readonly CraftPage m_Page;
    private readonly TextDefinition _notice;

    public DynamicLayoutCraftGump(
        Mobile from, CraftSystem craftSystem, TextDefinition notice, CraftPage page = CraftPage.None
    ) : base(40, 40)
    {
        m_From = from;
        m_CraftSystem = craftSystem;
        m_Page = page;
        _notice = notice;
    }
    
    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var context = m_CraftSystem.GetContext(m_From);

        builder.AddPage();

        builder.AddBackground(0, 0, 530, 437, 5054);
        builder.AddImageTiled(10, 10, 510, 22, 2624);
        builder.AddImageTiled(10, 292, 150, 45, 2624);
        builder.AddImageTiled(165, 292, 355, 45, 2624);
        builder.AddImageTiled(10, 342, 510, 85, 2624);
        builder.AddImageTiled(10, 37, 200, 250, 2624);
        builder.AddImageTiled(215, 37, 305, 250, 2624);
        builder.AddAlphaRegion(10, 10, 510, 417);

        if (m_CraftSystem.GumpTitle.Number > 0)
        {
            builder.AddHtmlLocalized(10, 12, 510, 20, m_CraftSystem.GumpTitle.Number, LabelColor);
        }
        else
        {
            builder.AddHtml(10, 12, 510, 20, m_CraftSystem.GumpTitle.String);
        }

        builder.AddHtmlLocalized(10, 37, 200, 22, 1044010, LabelColor);  // <CENTER>CATEGORIES</CENTER>
        builder.AddHtmlLocalized(215, 37, 305, 22, 1044011, LabelColor); // <CENTER>SELECTIONS</CENTER>
        builder.AddHtmlLocalized(10, 302, 150, 25, 1044012, LabelColor); // <CENTER>NOTICES</CENTER>

        builder.AddButton(15, 402, 4017, 4019, 0);
        builder.AddHtmlLocalized(50, 405, 150, 18, 1011441, LabelColor); // EXIT

        builder.AddButton(270, 402, 4005, 4007, GetButtonID(6, 2));
        builder.AddHtmlLocalized(305, 405, 150, 18, 1044013, LabelColor); // MAKE LAST

        // Mark option
        if (m_CraftSystem.MarkOption)
        {
            builder.AddButton(270, 362, 4005, 4007, GetButtonID(6, 6));
            builder.AddHtmlLocalized(
                305,
                365,
                150,
                18,
                1044017 + (int)(context?.MarkOption ?? CraftMarkOption.MarkItem), // MARK ITEM
                LabelColor
            );
        }
        // ****************************************

        // Resmelt option
        if (m_CraftSystem.Resmelt)
        {
            builder.AddButton(15, 342, 4005, 4007, GetButtonID(6, 1));
            builder.AddHtmlLocalized(50, 345, 150, 18, 1044259, LabelColor); // SMELT ITEM
        }
        // ****************************************

        // Repair option
        if (m_CraftSystem.Repair)
        {
            builder.AddButton(270, 342, 4005, 4007, GetButtonID(6, 5));
            builder.AddHtmlLocalized(305, 345, 150, 18, 1044260, LabelColor); // REPAIR ITEM
        }
        // ****************************************

        // Enhance option
        if (m_CraftSystem.CanEnhance)
        {
            builder.AddButton(270, 382, 4005, 4007, GetButtonID(6, 8));
            builder.AddHtmlLocalized(305, 385, 150, 18, 1061001, LabelColor); // ENHANCE ITEM
        }
        // ****************************************

        if (_notice != null)
        {
            if (_notice.Number > 0)
            {
                builder.AddHtmlLocalized(170, 295, 350, 40, _notice.Number, LabelColor);
            }
            else
            {
                builder.AddHtml(170, 295, 350, 40, $"<BASEFONT COLOR=#{FontColor:X6}>{_notice.String}</BASEFONT>");
            }
        }

        // If the system has more than one resource
        if (m_CraftSystem.CraftSubRes.Init)
        {
            var nameString = m_CraftSystem.CraftSubRes.Name.String;
            var nameNumber = m_CraftSystem.CraftSubRes.Name.Number;

            var resIndex = context?.LastResourceIndex ?? -1;

            var resourceType = m_CraftSystem.CraftSubRes.ResType;

            if (resIndex > -1)
            {
                var subResource = m_CraftSystem.CraftSubRes.GetAt(resIndex);

                nameString = subResource.Name.String;
                nameNumber = subResource.Name.Number;
                resourceType = subResource.ItemType;
            }

            var resourceCount = 0;

            if (m_From.Backpack != null)
            {
                foreach (var item in m_From.Backpack.FindItems())
                {
                    if (resourceType.IsInstanceOfType(item))
                    {
                        resourceCount += item.Amount;
                    }
                }
            }

            builder.AddButton(15, 362, 4005, 4007, GetButtonID(6, 0));

            if (nameNumber > 0)
            {
                builder.AddHtmlLocalized(50, 365, 250, 18, nameNumber, $"{resourceCount}", LabelColor);
            }
            else
            {
                builder.AddLabel(50, 362, LabelHue, $"{nameString} ({resourceCount} Available)");
            }
        }
        // ****************************************

        // For dragon scales
        if (m_CraftSystem.CraftSubRes2.Init)
        {
            var nameString = m_CraftSystem.CraftSubRes2.Name.String;
            var nameNumber = m_CraftSystem.CraftSubRes2.Name.Number;

            var resIndex = context?.LastResourceIndex2 ?? -1;

            var resourceType = m_CraftSystem.CraftSubRes2.ResType;

            if (resIndex > -1)
            {
                var subResource = m_CraftSystem.CraftSubRes2.GetAt(resIndex);

                nameString = subResource.Name.String;
                nameNumber = subResource.Name.Number;
                resourceType = subResource.ItemType;
            }

            var resourceCount = 0;

            if (m_From.Backpack != null)
            {
                foreach (var item in m_From.Backpack.FindItems())
                {
                    if (resourceType.IsInstanceOfType(item))
                    {
                        resourceCount += item.Amount;
                    }
                }
            }

            builder.AddButton(15, 382, 4005, 4007, GetButtonID(6, 7));

            if (nameNumber > 0)
            {
                builder.AddHtmlLocalized(50, 385, 250, 18, nameNumber, $"{resourceCount}", LabelColor);
            }
            else
            {
                builder.AddLabel(50, 385, LabelHue, $"{nameString} ({resourceCount} Available)");
            }
        }
        // ****************************************

        CreateGroupList(ref builder);

        if (m_Page == CraftPage.PickResource)
        {
            CreateResList(ref builder, false, m_From);
        }
        else if (m_Page == CraftPage.PickResource2)
        {
            CreateResList(ref builder, true, m_From);
        }
        else if (context?.LastGroupIndex > -1)
        {
            CreateItemList(ref builder, context.LastGroupIndex);
        }
    }

    public void CreateResList(ref DynamicGumpBuilder builder, bool opt, Mobile from)
    {
        var res = opt ? m_CraftSystem.CraftSubRes2 : m_CraftSystem.CraftSubRes;

        for (var i = 0; i < res.Count; ++i)
        {
            var index = i % 10;

            var subResource = res[i];

            if (index == 0)
            {
                if (i > 0)
                {
                    builder.AddButton(485, 260, 4005, 4007, 0, GumpButtonType.Page, i / 10 + 1);
                }

                builder.AddPage(i / 10 + 1);

                if (i > 0)
                {
                    builder.AddButton(455, 260, 4014, 4015, 0, GumpButtonType.Page, i / 10);
                }

                var context = m_CraftSystem.GetContext(m_From);

                builder.AddButton(220, 260, 4005, 4007, GetButtonID(6, 4));
                builder.AddHtmlLocalized(
                    255,
                    263,
                    200,
                    18,
                    context?.DoNotColor != true ? 1061591 : 1061590,
                    LabelColor
                );
            }

            var resourceCount = 0;

            if (from.Backpack != null)
            {
                var type = subResource.ItemType;
                foreach (var item in from.Backpack.FindItems())
                {
                    if (type.IsInstanceOfType(item))
                    {
                        resourceCount += item.Amount;
                    }
                }
            }

            builder.AddButton(220, 60 + index * 20, 4005, 4007, GetButtonID(5, i));

            if (subResource.Name.Number > 0)
            {
                builder.AddHtmlLocalized(
                    255,
                    63 + index * 20,
                    250,
                    18,
                    subResource.Name.Number,
                    $"{resourceCount}",
                    LabelColor
                );
            }
            else
            {
                builder.AddLabel(255, 60 + index * 20, LabelHue, $"{subResource.Name.String} ({resourceCount})");
            }
        }
    }

    public void CreateMakeLastList(ref DynamicGumpBuilder builder)
    {
        var context = m_CraftSystem.GetContext(m_From);

        if (context == null)
        {
            return;
        }

        var items = context.Items;

        if (items.Count > 0)
        {
            for (var i = 0; i < items.Count; ++i)
            {
                var index = i % 10;

                var craftItem = items[i];

                if (index == 0)
                {
                    if (i > 0)
                    {
                        builder.AddButton(370, 260, 4005, 4007, 0, GumpButtonType.Page, i / 10 + 1);
                        builder.AddHtmlLocalized(405, 263, 100, 18, 1044045, LabelColor); // NEXT PAGE
                    }

                    builder.AddPage(i / 10 + 1);

                    if (i > 0)
                    {
                        builder.AddButton(220, 260, 4014, 4015, 0, GumpButtonType.Page, i / 10);
                        builder.AddHtmlLocalized(255, 263, 100, 18, 1044044, LabelColor); // PREV PAGE
                    }
                }

                builder.AddButton(220, 60 + index * 20, 4005, 4007, GetButtonID(3, i));

                if (craftItem.NameNumber > 0)
                {
                    builder.AddHtmlLocalized(255, 63 + index * 20, 220, 18, craftItem.NameNumber, LabelColor);
                }
                else
                {
                    builder.AddLabel(255, 60 + index * 20, LabelHue, craftItem.NameString);
                }

                builder.AddButton(480, 60 + index * 20, 4011, 4012, GetButtonID(4, i));
            }
        }
        else
        {
            builder.AddHtmlLocalized(230, 62, 200, 22, 1044165, LabelColor); // You haven't made anything yet.
        }
    }

    public void CreateItemList(ref DynamicGumpBuilder builder, int selectedGroup)
    {
        if (selectedGroup == 501) // 501 : Last 10
        {
            CreateMakeLastList(ref builder);
            return;
        }

        var craftGroup = m_CraftSystem.CraftGroups[selectedGroup];
        var craftItemCol = craftGroup.CraftItems;

        for (var i = 0; i < craftItemCol.Count; ++i)
        {
            var index = i % 10;

            var craftItem = craftItemCol[i];

            if (index == 0)
            {
                if (i > 0)
                {
                    builder.AddButton(370, 260, 4005, 4007, 0, GumpButtonType.Page, i / 10 + 1);
                    builder.AddHtmlLocalized(405, 263, 100, 18, 1044045, LabelColor); // NEXT PAGE
                }

                builder.AddPage(i / 10 + 1);

                if (i > 0)
                {
                    builder.AddButton(220, 260, 4014, 4015, 0, GumpButtonType.Page, i / 10);
                    builder.AddHtmlLocalized(255, 263, 100, 18, 1044044, LabelColor); // PREV PAGE
                }
            }

            builder.AddButton(220, 60 + index * 20, 4005, 4007, GetButtonID(1, i));

            if (craftItem.NameNumber > 0)
            {
                builder.AddHtmlLocalized(255, 63 + index * 20, 220, 18, craftItem.NameNumber, LabelColor);
            }
            else
            {
                builder.AddLabel(255, 60 + index * 20, LabelHue, craftItem.NameString);
            }

            builder.AddButton(480, 60 + index * 20, 4011, 4012, GetButtonID(2, i));
        }
    }

    public int CreateGroupList(ref DynamicGumpBuilder builder)
    {
        var craftGroupCol = m_CraftSystem.CraftGroups;

        builder.AddButton(15, 60, 4005, 4007, GetButtonID(6, 3));
        builder.AddHtmlLocalized(50, 63, 150, 18, 1044014, LabelColor); // LAST TEN

        for (var i = 0; i < craftGroupCol.Count; i++)
        {
            var craftGroup = craftGroupCol[i];

            builder.AddButton(15, 80 + i * 20, 4005, 4007, GetButtonID(0, i));

            if (craftGroup.NameNumber > 0)
            {
                builder.AddHtmlLocalized(50, 83 + i * 20, 150, 18, craftGroup.NameNumber, LabelColor);
            }
            else
            {
                builder.AddLabel(50, 80 + i * 20, LabelHue, craftGroup.NameString);
            }
        }

        return craftGroupCol.Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetButtonID(int type, int index) => 1 + type + index * 7;

    public override int Switches { get; }
    public override int TextEntries { get; }
}