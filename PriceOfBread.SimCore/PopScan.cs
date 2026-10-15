namespace PriceOfBread.SimCore;

/// <summary>★州×POP の走査の名前のある入口（2026-10-15 (351)・案 A・結果を変えない）。
/// 旧＝州のループの中の POP のループを各所が書き写し、集合の定義が揃っていなかった（<c>State==Active</c> で絞る所と絞らない所・<see cref="World.AllPops"/> は絞らない）。
/// 今は集合が 2 つ・名前がある：
/// ・<see cref="World.AllPopsWithRegion"/>＝国家会計の集合（全ての州の全ての POP・州の並び→州の中の POP の並び・絞らない）＝人口保存・貯蓄の合計・国債の保有の台帳が読む集合。
/// ・<see cref="World.ActivePopsWithRegion"/>＝稼働中の州だけ（<c>State==Active</c>）＝州の価格水準・州の人口の目安が読む集合。
/// 本番の盤面（2026-08-26 に辺境を廃止）には稼働していない州が無い＝2 つは同じ列を返す。差が出るのは試験の盤面（辺境 <c>Locked</c>）だけ。
/// 兵士・規模 &gt; 1e-9・閉鎖階級（<c>Closed</c>）の除き方は呼び手ごとに違う＝この列挙には入れない（POP の述語 <c>IsSoldier</c>・<c>Closed</c> を呼び手が読む）。
/// 走査の順は旧の入れ子の <c>foreach</c> とまったく同じ＝和の順も変わらない。値型の列挙子＝割り当てなし。</summary>
public readonly struct PopScan
{
    private readonly List<Region> _regions;
    private readonly bool _activeOnly;
    internal PopScan(List<Region> regions, bool activeOnly) { _regions = regions; _activeOnly = activeOnly; }
    public Enumerator GetEnumerator() => new(_regions, _activeOnly);

    public struct Enumerator
    {
        private readonly List<Region> _regions;
        private readonly bool _activeOnly;
        private int _ri, _pi;
        private Region? _r;
        private List<Pop>? _pops;
        internal Enumerator(List<Region> regions, bool activeOnly) { _regions = regions; _activeOnly = activeOnly; _ri = -1; _pi = -1; }
        public bool MoveNext()
        {
            while (true)
            {
                if (_pops is not null && ++_pi < _pops.Count) return true;
                if (++_ri >= _regions.Count) { _pops = null; return false; }
                var r = _regions[_ri];
                if (_activeOnly && r.State != RegionState.Active) { _pops = null; continue; }
                _r = r; _pops = r.Pops; _pi = -1;
            }
        }
        public (Region Region, Pop Pop) Current => (_r!, _pops![_pi]);
    }
}

/// <summary>★窓の中だけの和の使い回し（2026-10-15 (351)・案 A・結果を変えない・作業領域＝セーブに載らない）。
/// 窓＝<see cref="World.PopSums"/> が非 null の間。書き手のいない区間の中で、同じ入力から同じ順で足した和を 1 回だけ求めて配る（前例＝<see cref="Simulation.CreditMonth"/>・<c>Diplomacy.SyncGdp</c>）。
/// 窓の外（単独の呼び・試験の直呼び）は毎回求める＝旧と同じ。置く窓は四半期首の「近代化投資〜政府の反映の終わり」だけ（<see cref="Simulation"/> の <c>QuarterStart</c>）。
/// ・窓に入れない段：AI の決定（<c>DecideQuarter</c>＝着工・払い下げ〔<c>SetStateTarget</c>〕が資本家の貯蓄を動かす）と議会（<c>Parliament.Step</c>＝総選挙の公示への AI の答え
///   <c>World.ResolveElection</c> が国庫→有権者の貯蓄を動かす）。議会を窓に入れた最初の木は canary（selfplay 1 200）で軌道が変わった＝<see cref="Verify"/> が止めた。
/// ・窓の中の段（近代化投資・政府の反映）が書くのは国の欄・州の欄・法の欄・闘争度ほか＝POP の規模・貯蓄・地代・所得・年齢の層は書かない（実装ログ (351) の読みと、
///   <see cref="Verify"/>＝環境変数 <c>POB_POPSCAN_VERIFY=1</c> で使い回すたびに求め直して 1 ビットも違わないことを確かめる番人）。
/// 使い回す和は 4 つ：総人口・兵士の数・利益集団の力の分母（<see cref="InterestGroups.Blocs"/> の第 1 走査）・政策の立場の土台の POP 由来の和（<see cref="PolicyPreferences.Build"/> の第 3 走査）。
/// 国の入力（法・係数・国庫）に依る第 2 走査（力の重み）は使い回さない＝政府の反映の途中で法が動く。</summary>
internal sealed class PopWindow
{
    /// <summary>番人の旗（環境変数 <c>POB_POPSCAN_VERIFY=1</c> で既定が真）。真なら窓の中の読みごとに求め直し、ビットが違えば例外。既定は偽＝実行ゼロ。試験は立てて回す（<c>PopScanTests</c>）。</summary>
    internal static bool Verify = System.Environment.GetEnvironmentVariable("POB_POPSCAN_VERIFY") == "1";

    /// <summary>このスレッドで開いている窓の数（番人 <see cref="Verify"/> が窓の中の POP への書き込みを止めるために数える）。窓は四半期首の 1 スレッドの中で開閉する。</summary>
    [System.ThreadStatic] internal static int Open;

    /// <summary>POP の規模・貯蓄の書き込み口（<c>Pop.Distribute／Take／Carry／SettleMonth／Blend／RebaseSavingsTo</c>・<c>World.KillPops</c>）の頭が呼ぶ番人。
    /// <see cref="Verify"/> が真で窓が開いている間の書き込みは例外（使い回しの前提が壊れる＝読みの照合を待たずに、書いた段で止める）。既定（偽）は旗を 1 回読むだけ。</summary>
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

    /// <summary>政策の立場の土台の POP 由来の和。呼び手が正規化で書き換えるので、呼び手へは**写し**を渡す。</summary>
    internal PolicyPreferences.BuildSums BuildSums(World w)
    {
        if (_build is null) _build = PolicyPreferences.ComputeBuildSums(w);
        else if (Verify) _build.CheckSame(PolicyPreferences.ComputeBuildSums(w));
        return _build.Copy();
    }

    internal static void Check(string what, double cached, double fresh)
    {
        if (System.BitConverter.DoubleToInt64Bits(cached) != System.BitConverter.DoubleToInt64Bits(fresh))
            throw new System.InvalidOperationException($"POP の窓：{what}の使い回しが求め直しと違う（使い回し {cached:R}・求め直し {fresh:R}）＝窓の中で POP が動いた");
    }
}
