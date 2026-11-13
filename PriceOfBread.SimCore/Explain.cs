namespace PriceOfBread.SimCore;

public sealed class ExplainLedger
{
    public readonly record struct Term(string Reason, double Delta);

    private readonly Dictionary<string, List<Term>> _quantities = new();

    public void Reset(string quantity)
    {
        if (_quantities.TryGetValue(quantity, out var list)) list.Clear();
    }

    public void Add(string quantity, string reason, double delta)
    {
        if (!_quantities.TryGetValue(quantity, out var list))
        {
            list = new List<Term>();
            _quantities[quantity] = list;
        }
        list.Add(new Term(reason, delta));
    }

    public IReadOnlyList<Term> Get(string quantity)
        => _quantities.TryGetValue(quantity, out var list) ? list : System.Array.Empty<Term>();
}
