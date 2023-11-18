using System.Collections.Generic;

namespace Server.Items;

public class TestItem1 : TestItem
{
}

public class TestItem2 : TestItem
{
    public int Hue;

    public TestItem2()
    {
    }

    public TestItem2(int hue) => Hue = hue;
}

public class TestItem
{
    internal List<TestItem> m_Items = new();
}

public partial class TestContainer : TestItem
{
}