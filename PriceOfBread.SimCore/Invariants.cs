namespace PriceOfBread.SimCore;

public sealed class InvariantViolationException(string message) : Exception(message);

public interface IInvariant
{
    string Name { get; }
    void Check(World world);
}

public sealed class MoneyConservation(World world) : IInvariant
{
    private readonly MoneyStock _baseline = world.BaseMoney - world.CumulativeTradeFlow - world.CumulativeGoldMinted - world.CumulativeFiatIssued;
    public string Name => "貨幣保存（基礎貨幣）";

    public void Check(World world)
    {
        double expected = (_baseline + world.CumulativeTradeFlow + world.CumulativeGoldMinted + world.CumulativeFiatIssued).Value;
        double bm = world.BaseMoney.Value;
        double scale = Math.Max(Math.Abs(expected), Math.Max(Math.Abs(_baseline.Value), Math.Max(Math.Abs(world.CumulativeTradeFlow.Value), Math.Abs(bm))));
        double tol = 1e-8 * Math.Max(1.0, scale);
        if (Math.Abs(bm - expected) > tol)
            throw new InvariantViolationException(
                $"{Name}：[{world.Region.Name}・{world.Year}年] 基礎貨幣が想定 {expected}（基準{_baseline}＋累積貿易{world.CumulativeTradeFlow}＋鋳造{world.CumulativeGoldMinted}＋不換{world.CumulativeFiatIssued}）から {bm} へ乖離（ズレ{bm - expected:F6}）");
    }
}

public sealed class PopulationConservation(World world) : IInvariant
{
    private readonly double _baseline = world.TotalPopulation - world.PopulationDelta;
    public string Name => "人口保存";

    public void Check(World world)
    {
        double expected = _baseline + world.PopulationDelta;
        double tol = 1e-6 * Math.Max(1.0, Math.Abs(_baseline));
        if (Math.Abs(world.TotalPopulation - expected) > tol)
            throw new InvariantViolationException(
                $"{Name}：総人口が想定 {expected}（基準{_baseline}＋増減{world.PopulationDelta:F0}）から {world.TotalPopulation} へ乖離");
    }
}

public sealed class NonNegativeBounds : IInvariant
{
    public string Name => "非負境界";

    public void Check(World world)
    {
        foreach (var p in world.AllPops())
        {
            Require(p.Savings.Value, $"貯蓄[{p.ClassName}]");
            Require(p.Size, "規模");
            if (double.IsNaN(p.Satisfaction) || double.IsInfinity(p.Satisfaction))
                throw new InvariantViolationException($"{Name}：充足度が NaN／∞");
            double sa = 0; bool neg = p.Ages.Length != 2 * Pop.Layout.Bands;
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
        if (double.IsNaN(world.Treasury.Value) || double.IsInfinity(world.Treasury.Value))
            throw new InvariantViolationException($"{Name}：国庫が NaN／∞");
        if (double.IsNaN(world.GovDebt.Value) || double.IsInfinity(world.GovDebt.Value))
            throw new InvariantViolationException($"{Name}：国債残高が NaN／∞");
        if (double.IsNaN(world.BaseMoney.Value) || double.IsInfinity(world.BaseMoney.Value))
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

public sealed class BondLedger : IInvariant
{
    public string Name => "国債の保有台帳";

    public void Check(World world)
    {
        MoneyStock held = default;
        foreach (var r in world.Regions)
            foreach (var p in r.Pops)
            {
                if (!(p.BondHoldings.Value >= 0) || double.IsInfinity(p.BondHoldings.Value))
                    throw new InvariantViolationException($"{Name}：[{world.Region.Name}・{world.Year}年] {r.Name}/{p.ClassName} の保有が {p.BondHoldings}");
                held += p.BondHoldings;
            }
        if (!Balanced(world, held))
            throw new InvariantViolationException(
                $"{Name}：[{world.Region.Name}・{world.Year}年] 保有の和 {held} ≠ 国内元本 {world.GovDebt - world.ForeignDebt}（国債{world.GovDebt}−対外{world.ForeignDebt}）・ズレ{held - (world.GovDebt - world.ForeignDebt):G6}");
    }

    internal static bool Balanced(World world, MoneyStock held)
    {
        double flow = Math.Abs(world.TreasuryLedger[(int)TreasuryCat.Borrow]) + Math.Abs(world.TreasuryLedger[(int)TreasuryCat.DebtRepay]);
        double scale = Math.Max(Math.Max(held.Value, flow), Math.Max(Math.Abs(world.GovDebt.Value), Math.Abs(world.ForeignDebt.Value)));
        return Math.Abs((held - (world.GovDebt - world.ForeignDebt)).Value) <= 1e-8 * Math.Max(1.0, scale);
    }
}

public sealed class GoodsBalance : IInvariant
{
    public string Name => "財収支";

    public void Check(World world)
    {
        foreach (var r in world.Regions)
        {
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
