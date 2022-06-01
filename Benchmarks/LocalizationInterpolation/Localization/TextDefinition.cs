namespace Server;

public class TextDefinition
{
    public TextDefinition(string text) : this(0, text)
    {
    }

    public TextDefinition(int number = 0, string text = null)
    {
        Number = number;
        String = text;
    }

    public int Number { get; }

    public string String { get; }

    public bool IsEmpty => Number <= 0 && String == null;

    public override string ToString() => Number > 0 ? $"#{Number}" : String ?? "";

    public string GetValue() => Number > 0 ? Number.ToString() : String ?? "";

    public static implicit operator TextDefinition(int v) => new(v);

    public static implicit operator TextDefinition(string s) => new(s);

    public static implicit operator int(TextDefinition m) => m?.Number ?? 0;

    public static implicit operator string(TextDefinition m) => m?.String;

    public static bool IsNullOrEmpty(TextDefinition def) => def?.IsEmpty != false;
}
