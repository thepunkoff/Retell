namespace Retell.Elements;

public class DebugRenderToken : IEquatable<DebugRenderToken>
{
    private readonly DebugRenderTokenType _type;
    private readonly DebugRenderToken? _answerTo;

    public DebugRenderToken(DebugRenderTokenType type, DebugRenderToken? answerTo = null)
    {
        _type = type;
        _answerTo = answerTo;
    }

    public bool Equals(DebugRenderToken? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return _type == other._type && Equals(_answerTo, other._answerTo);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;

        return obj is DebugRenderToken debugRenderToken && Equals(debugRenderToken);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)_type, _answerTo);
    }

    public override string ToString()
    {
        return $"{_type.ToString()}{(_answerTo is not null ? $" (answer to '{_answerTo}')" : String.Empty)}";
    }
}