namespace PriceOfBread.SimCore;

/// <summary>
/// 不変条件（§11.6）が破れたときに投げる例外。「シミュ上の破綻」を経済的危機と
/// 分離して即停止させるための信号（§2.7）。価格暴落や欠品そのものは破綻ではない。
/// </summary>
public sealed class InvariantViolationException(string message) : Exception(message);

/// <summary>毎月検査する不変条件の共通形（§11.6）。</summary>
public interface IInvariant
{
    string Name { get; }
    void Check(World world);
}

/// <summary>
/// 貨幣保存（§11.6・§A-7）：保存量は基礎貨幣（金）＝広義マネー − 与信残高。与信で広義は増減するが基礎は内部移転で一定、
/// 交易（金流）・鋳造・不換発行でのみ増減する（Δ基礎貨幣＝純貿易流入＋鋳造＋不換発行）。
/// </summary>
public sealed class MoneyConservation(World world) : IInvariant
{
    // 基準線は「交易を差し引いた基礎貨幣」で持つ＝期待値 = _baseline + 累積貿易 が捕捉時の基礎貨幣に一致。
    // 構築時は累積貿易0なので従来と同値。開始盤面で国庫・与信を初期へ戻した直後に取り直しても整合する（相対基準）。
    private readonly double _baseline = world.BaseMoney - world.CumulativeTradeFlow - world.CumulativeGoldMinted - world.CumulativeFiatIssued;
    public string Name => "貨幣保存（基礎貨幣）";

    public void Check(World world)
    {
        double expected = _baseline + world.CumulativeTradeFlow + world.CumulativeGoldMinted + world.CumulativeFiatIssued;
        double bm = world.BaseMoney;   // 全pop走査＝1回だけ
        // 許容誤差は「合算した量の桁」に比例させる（残差は浮動小数の再結合のみ）。expected は基準−累積貿易の**大きな量の差**になり得る（資本逃避で基礎貨幣がほぼ0へ収縮＝激しい桁落ち）ので、
        // |expected| だけを基準にすると相対許容が過小になり float ノイズで誤検出する。基準・累積貿易・基礎貨幣の最大桁で規格化＝桁落ちに頑健（従来より緩むだけで厳しくはならない＝既存テスト不変）。
        double scale = Math.Max(Math.Abs(expected), Math.Max(Math.Abs(_baseline), Math.Max(Math.Abs(world.CumulativeTradeFlow), Math.Abs(bm))));
        // 許容1e-8（2026-07-08に1e-9から緩和）：604四半期=54,360日×数百万演算のfloat再結合ノイズは相対1e-9をわずかに超え得る
        // （実測の偽陽性：seed20江戸=2.5e-9・seed4タシュケント=1e-8）。実リーク（官僚Size≈0のDistribute消失=2e-7級）とは2桁の分離＝検出力は維持。
        double tol = 1e-8 * Math.Max(1.0, scale);
        if (Math.Abs(bm - expected) > tol)
            throw new InvariantViolationException(
                $"{Name}：[{world.Region.Name}・{world.Year}年] 基礎貨幣が想定 {expected}（基準{_baseline}＋累積貿易{world.CumulativeTradeFlow}＋鋳造{world.CumulativeGoldMinted}＋不換{world.CumulativeFiatIssued}）から {bm} へ乖離（ズレ{bm - expected:F6}）");
    }
}

/// <summary>
/// 人口保存（§11.6・修正版 §A-3）：Δ総pop = ±移民 ＋ PopulationDelta（出生−死亡−戦死）。
/// 移民は再ラベルで総和を保つ。出生・死亡・戦死だけが総人口を規定値から動かし、符号付き台帳で追跡（人口転換）。
/// </summary>
public sealed class PopulationConservation(World world) : IInvariant
{
    private readonly double _baseline = world.TotalPopulation - world.PopulationDelta;   // 相対基準（取り直し安全。構築時は増減0で従来と同値）
    public string Name => "人口保存";

    public void Check(World world)
    {
        // 規定値 ＋ 累積増減（出生−死亡−戦死）。これ以外の増減（意図しない人口漏れ）のみ検出する。
        double expected = _baseline + world.PopulationDelta;
        double tol = 1e-6 * Math.Max(1.0, Math.Abs(_baseline));
        if (Math.Abs(world.TotalPopulation - expected) > tol)
            throw new InvariantViolationException(
                $"{Name}：総人口が想定 {expected}（基準{_baseline}＋増減{world.PopulationDelta:F0}）から {world.TotalPopulation} へ乖離");
    }
}

/// <summary>
/// 境界条件（§11.6）：価格≥0・在庫≥0・貯蓄≥0、および非 NaN／∞（§2.7 のシミュ破綻検出）。
/// </summary>
public sealed class NonNegativeBounds : IInvariant
{
    public string Name => "非負境界";

    public void Check(World world)
    {
        foreach (var p in world.AllPops())
        {
            Require(p.Savings, $"貯蓄[{p.ClassName}]");
            Require(p.Size, "規模");
            if (double.IsNaN(p.Satisfaction) || double.IsInfinity(p.Satisfaction))
                throw new InvariantViolationException($"{Name}：充足度が NaN／∞");
            // ★年齢の層（2026-09-30 段4e 後半）：16本の割合は各々0以上・合計1（許容1e-9・否定形＝NaN も赤）
            double sa = 0; bool neg = p.Ages.Length != 2 * Pop.AgeBands;
            foreach (var x in p.Ages) { if (!(x >= 0)) neg = true; sa += x; }
            if (neg || !(Math.Abs(sa - 1.0) <= 1e-9))
                throw new InvariantViolationException($"{Name}：年齢の層の割合が壊れた[{p.ClassName}]（合計 {sa}）");
        }
        foreach (var r in world.Regions)
            foreach (var m in r.Markets)
            {
                Require(m.Price, "価格");
                Require(m.Stock, "在庫");
            }
        // 国庫・国債・基礎貨幣は負値が正当（債務）なので非負は課さず NaN／∞ のみ検出（Pay は有限性を確かめず加算するため）。
        if (double.IsNaN(world.Treasury) || double.IsInfinity(world.Treasury))
            throw new InvariantViolationException($"{Name}：国庫が NaN／∞");
        if (double.IsNaN(world.GovDebt) || double.IsInfinity(world.GovDebt))
            throw new InvariantViolationException($"{Name}：国債残高が NaN／∞");
        if (double.IsNaN(world.BaseMoney) || double.IsInfinity(world.BaseMoney))
            throw new InvariantViolationException($"{Name}：基礎貨幣が NaN／∞");
    }

    private void Require(double value, string label)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new InvariantViolationException($"{Name}：{label} が NaN／∞");
        if (value < 0)
            throw new InvariantViolationException($"{Name}：{label} が負（{value}）");
    }
}

/// <summary>
/// 国債の保有台帳（2026-09-21裁定）：国の全POP（Regions の全州＝Fiscal が利払い・償還を配る集合と同じ）の保有の和＝国内元本残高（GovDebt − ForeignDebt）。
/// 台帳は金を動かさない記録なので、両辺は別々の経路（Fiscal の起債・償還・デフォルト／Politics の否認／POPの移動と国外振替）で動く＝どこかで運び忘れると割れる。
/// 許容は相対1e-8（貨幣保存と同じ）。尺度は GovDebt・ForeignDebt・保有の和と、台帳を通った元本の流れ（起債と償還の累計）の最大＝国内元本が2つの大きな量の差になる国（対外債の多い国）の桁落ちと、
/// 大きかった国債を返し終えつつある国の丸めの残り（下の <see cref="Balanced"/>）に頑健。
/// 保有そのものも非負・有限であること（NaN は否定形で拾う）。状態を持たない＝基準線の取り直し不要。
/// </summary>
public sealed class BondLedger : IInvariant
{
    public string Name => "国債の保有台帳";

    public void Check(World world)
    {
        double held = 0;
        foreach (var r in world.Regions)
            foreach (var p in r.Pops)
            {
                if (!(p.BondHoldings >= 0) || double.IsInfinity(p.BondHoldings))
                    throw new InvariantViolationException($"{Name}：[{world.Region.Name}・{world.Year}年] {r.Name}/{p.ClassName} の保有が {p.BondHoldings}");
                held += p.BondHoldings;
            }
        if (!Balanced(world, held))
            throw new InvariantViolationException(
                $"{Name}：[{world.Region.Name}・{world.Year}年] 保有の和 {held} ≠ 国内元本 {world.GovDebt - world.ForeignDebt}（国債{world.GovDebt}−対外{world.ForeignDebt}）・ズレ{held - (world.GovDebt - world.ForeignDebt):G6}");
    }

    /// <summary>Σ保有と国内元本の一致（相対1e-8）。起票（<see cref="Fiscal.SeedBondHoldings"/>）も同じ判定を使う＝検査が割れる時だけ起票する（判定は1本）。
    /// ★尺度に台帳を通った元本の流れ（国庫の台帳の起債 Borrow と償還 DebtRepay の累計）を入れる（2026-09-28・支払いの一元化 段2-1 の合流で見つけた偽陽性）。
    ///   ズレは浮動小数の丸めで、国債が最大のときの桁で積もる（相対1e-15級）。比例の償還（Σ保有×keep と 国債−元本）はズレを**絶対値のまま**保つ
    ///   ＝国債を返して小さくなるほどズレの相対が膨らむ（seed1789 の清＝国債の最大 842万・ズレ −1.35e-8〔最大時の相対 1.6e-15〕が、償還で 1948年に
    ///   国債 1.7 まで下がった所で相対 7.7e-9＝旧の尺度〔今の残高〕の許容 1e-8×max(1,1.7) を越えて全長の番人が止まった）。
    ///   貨幣保存（<see cref="MoneyConservation"/>）が累積貿易を尺度に入れるのと同じ形＝台帳を通った量の桁で丸めの許容を測る（新しい状態は持たない＝既存の累計を読む）。
    ///   運び忘れ（保有の一部が消える・湧く）は流れの桁の1e-8 より何桁も大きい＝検出力は保つ。</summary>
    internal static bool Balanced(World world, double held)
    {
        double flow = Math.Abs(world.TreasuryLedger[(int)TreasuryCat.Borrow]) + Math.Abs(world.TreasuryLedger[(int)TreasuryCat.DebtRepay]);
        double scale = Math.Max(Math.Max(held, flow), Math.Max(Math.Abs(world.GovDebt), Math.Abs(world.ForeignDebt)));
        return Math.Abs(held - (world.GovDebt - world.ForeignDebt)) <= 1e-8 * Math.Max(1.0, scale);   // NaN は偽＝割れた側へ倒れる
    }
}

/// <summary>
/// 財収支（§11.6）：各財 stock = prev_stock + produced − sold（単一地域は純輸出0）。
/// 取引フェーズの帳尻が合っているかの恒等式チェック。
/// </summary>
public sealed class GoodsBalance : IInvariant
{
    public string Name => "財収支";

    public void Check(World world)
    {
        foreach (var r in world.Regions)
        {
            if (r.State != RegionState.Active) continue;   // locked 地域の市場は休眠（balance 検査の対象外）
            foreach (var m in r.Markets)
            {
                double expected = m.PrevStock + m.LastProduced + m.LastImport - m.LastSold - m.LastExport;
                if (Math.Abs(m.Stock - expected) > 1e-6)
                    throw new InvariantViolationException(
                        $"{Name}：在庫 {m.Stock} ≠ 前在庫{m.PrevStock}＋生産{m.LastProduced}＋輸入{m.LastImport}−販売{m.LastSold}−輸出{m.LastExport}");
            }
        }
    }
}
