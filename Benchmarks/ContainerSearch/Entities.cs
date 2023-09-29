using System.Collections.Generic;

namespace Server.Items;

public class TestItem1 : TestItem
{
}

public class TestItem2 : TestItem
{
}

public class TestItem
{
    internal List<TestItem> m_Items = new();
}

public partial class TestContainer : TestItem
{
}