namespace PriceOfBread.SimCore;

internal sealed class RootSupply
{
    const int MaxDepth = 6;
    readonly World _w;
    readonly sbyte[] _st;
    readonly sbyte[] _flow;
    readonly int[] _leaf;
    int _lastLeaf = -1;
    List<(Region R, Producer P)>[]? _byGood;

    internal RootSupply(World w)
    {
        _w = w;
        int n = Catalog.Goods.Length;
        _st = new sbyte[n];
        _flow = new sbyte[n];
        _leaf = new int[n];
    }

    internal int Blocker => _lastLeaf;

    internal bool Of(Producer pr) { _lastLeaf = -1; return InputsOk(pr, 0); }

    bool InputsOk(Producer p, int depth)
    {
        foreach (var inp in p.Inputs)
        {
            int g = inp.GoodId;
            if (g < 0 || g >= _st.Length) continue;
            if (!(inp.QtyPerOutput > 1e-9)) continue;
            if (Catalog.Goods[g].Tier == GoodTier.Service) continue;
            if (!Obtainable(g, depth)) return false;
        }
        return true;
    }

    bool Obtainable(int g, int depth)
    {
        if (_st[g] == 1 || _st[g] == 3) return true;
        if (_st[g] == 2) { _lastLeaf = _leaf[g]; return false; }
        if (depth >= MaxDepth) return true;
        if (FlowsNow(g)) { _st[g] = 1; return true; }
        _st[g] = 3;
        bool ok = false, anyOpen = false;
        foreach (var (r, p) in ProducersOf(g))
        {
            if (!(Discovery.Gate(r, p) > 0)) continue;
            anyOpen = true;
            if (InputsOk(p, depth + 1)) { ok = true; break; }
        }
        _st[g] = (sbyte)(ok ? 1 : 2);
        if (!ok) { if (!anyOpen) _lastLeaf = g; _leaf[g] = _lastLeaf; }
        return ok;
    }

    bool FlowsNow(int g)
    {
        if (_flow[g] != 0) return _flow[g] == 1;
        double sum = 0;
        foreach (var r in _w.Regions)
        {
            if (g >= r.Markets.Length) continue;
            var m = r.Markets[g];
            sum += m.LastProduced + Math.Max(m.LastImport, m.PrevImport);
        }
        bool flows = double.IsNaN(sum) || sum > _w.Coef.TargetStockFloor;
        if (!flows)
            flows = !(_w.WorldGoodsProduced is { } wp && g < wp.Length) || double.IsNaN(wp[g]) || wp[g] > _w.Coef.TargetStockFloor;
        _flow[g] = (sbyte)(flows ? 1 : 2);
        return _flow[g] == 1;
    }

    List<(Region R, Producer P)> ProducersOf(int g)
    {
        if (_byGood is null)
        {
            _byGood = new List<(Region, Producer)>[_st.Length];
            foreach (var r in _w.Regions)
            {
                foreach (var p in r.Producers)
                {
                    if (p.GoodId < 0 || p.GoodId >= _byGood.Length) continue;
                    if (p.RecipeName == Simulation.HouseholdRecipeName) continue;
                    (_byGood[p.GoodId] ??= []).Add((r, p));
                }
            }
        }
        return _byGood[g] ?? s_none;
    }
    static readonly List<(Region R, Producer P)> s_none = [];
}
