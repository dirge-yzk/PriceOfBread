namespace PriceOfBread.SimCore;

public readonly struct PopScan
{
    private readonly List<Region> _regions;
    internal PopScan(List<Region> regions) { _regions = regions; }
    public Enumerator GetEnumerator() => new(_regions);

    public struct Enumerator
    {
        private readonly List<Region> _regions;
        private int _ri, _pi, _regionCount, _popCount;
        private Region? _r;
        private List<Pop>? _pops;
        internal Enumerator(List<Region> regions) { _regions = regions; _ri = -1; _pi = -1; _regionCount = regions.Count; _popCount = 0; }
        public bool MoveNext()
        {
            while (true)
            {
                if (_regions.Count != _regionCount || (_pops is not null && _pops.Count != _popCount)) throw new System.InvalidOperationException("POP の走査：走査の途中で州または POP の一覧の数が変わった");
                if (_pops is not null && ++_pi < _pops.Count) return true;
                if (++_ri >= _regions.Count) { _pops = null; return false; }
                var r = _regions[_ri];
                _r = r; _pops = r.Pops; _popCount = r.Pops.Count; _pi = -1;
            }
        }
        public (Region Region, Pop Pop) Current => (_r!, _pops![_pi]);
    }
}

internal sealed class PopWindow
{
    internal static bool Verify = InitVerify();

    private static bool InitVerify()
    {
        bool on = System.Environment.GetEnvironmentVariable("POB_POPSCAN_VERIFY") == "1";
        if (on) System.AppDomain.CurrentDomain.ProcessExit += (_, _) => System.Console.Error.WriteLine($"[POP の窓] 番人 POB_POPSCAN_VERIFY=1：照合 {System.Threading.Interlocked.Read(ref Checks)} 回・窓を開いた回数 {System.Threading.Interlocked.Read(ref Opened)}");
        return on;
    }

    internal static long Checks, Opened;

    [System.ThreadStatic] internal static int Open;

    internal static void GuardWrite(string what)
    {
        if (Open > 0) throw new System.InvalidOperationException($"POP の窓：窓の中で POP への書き込み（{what}）＝使い回している和の前提が崩れる（窓の外へ出す）");
    }

    private double _total, _soldier;
    private bool _hasTotal, _hasSoldier;
    private (double Wealth, double Pop)? _landed;
    private PolicyPreferences.BuildSums? _build;

    internal double TotalPopulation(World w)
    {
        if (!_hasTotal) { _total = w.ComputeTotalPopulation(); _hasTotal = true; }
        else if (Verify) Check("総人口", _total, w.ComputeTotalPopulation());
        return _total;
    }

    internal double SoldierSizeK(World w)
    {
        if (!_hasSoldier) { _soldier = w.ComputeSoldierSizeK(); _hasSoldier = true; }
        else if (Verify) Check("兵士の数", _soldier, w.ComputeSoldierSizeK());
        return _soldier;
    }

    internal (double Wealth, double Pop) LandedTotals(World w)
    {
        if (_landed is not { } v) { v = InterestGroups.ComputeLandedTotals(w); _landed = v; }
        else if (Verify) { var f = InterestGroups.ComputeLandedTotals(w); Check("利益集団の富の分母", v.Wealth, f.Wealth); Check("利益集団の人口の分母", v.Pop, f.Pop); }
        return v;
    }

    internal PolicyPreferences.BuildSums BuildSums(World w)
    {
        if (_build is null) _build = PolicyPreferences.ComputeBuildSums(w);
        else if (Verify) _build.CheckSame(PolicyPreferences.ComputeBuildSums(w));
        return _build.Copy();
    }

    internal static void Check(string what, double cached, double fresh)
    {
        if (Verify) System.Threading.Interlocked.Increment(ref Checks);
        if (System.BitConverter.DoubleToInt64Bits(cached) != System.BitConverter.DoubleToInt64Bits(fresh))
            throw new System.InvalidOperationException($"POP の窓：{what}の使い回しが求め直しと違う（使い回し {cached:R}・求め直し {fresh:R}）＝窓の中で POP が動いた");
    }
}
