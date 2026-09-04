namespace JCMS_Mini_Monitoring.Models;

public sealed class StatusData
{
    private readonly Dictionary<string, decimal> _values = new(StringComparer.Ordinal);

    public void SetValue(string name, decimal value)
    {
        _values[name] = value;
    }

    public bool TryGetValue(string name, out decimal value)
    {
        return _values.TryGetValue(name, out value);
    }
}
