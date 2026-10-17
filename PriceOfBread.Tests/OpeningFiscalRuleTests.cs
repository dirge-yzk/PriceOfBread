using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(332) 初期値の整備 段6 の採り直し＝開幕の国庫と国債を比から導く規則（<see cref="World.RebaseOpeningFiscal"/>）の番人。
/// 規則は <see cref="NationConfig.TreasuryMonths"/>／<see cref="NationConfig.GovDebtToGdpYear"/> が正の国だけ（既定の world.json は全て 0＝従来どおり絶対額の国庫・国債）。
/// 番人は機構（式と台帳）だけを見る＝帯の合否は入れない（置いた瞬間に決まる量・出典の表は measurements/e7_20261014_values.tsv）。
/// 期待値は式の出力から作らず、World の外から読んだ量（直近の月の歳入・年名目GDP・Pop の保有を直接走査した和）から組む。
/// 突然変異：保有の起票し直し（<c>SeedBondHoldings</c>）の行を外す→Σ保有が 0 で台帳の検査が赤／対外債を引かず GovDebt を比×GDP に置く→対外債のある国で国内の元本が負になり赤／
/// 国庫を 歳入×月数 でなく 月数 だけにする→国庫の式が赤／歳入が正でないのに置く守りを外す→NaN が国庫に入り赤。
/// </summary>
public class OpeningFiscalRuleTests
{
    private static NationConfig With(string name, double months, double ratio)
    {
        var c = WorldData.ByName(name).ShallowClone();   // 共有の世界データ（WorldData.Nations）は書き換えない
        c.TreasuryMonths = months; c.GovDebtToGdpYear = ratio;
        return c;
    }

    private static double Held(World w) { double h = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) h += p.BondHoldings.Value; return h; }
    private static bool AnyHolder(World w) { foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.BondHoldings.Value > 0) return true; return false; }

    [Fact]
    public void 既定の世界データは規則を持つ_388で採用()
    {
        // ★(388) 腕 B を採った＝world.json は 61 国の国庫を歳入×月数・18 国の国債を比×年の名目GDP で置く（出典＝measurements/e7_20261014_values.tsv）。
        // 数は表から数えた物（61・18・英 1.34・英の月数 2.5）で、式の出力から作らない。規則が0の国の旧の経路（RebaseTreasury の1文）は下の With(…, 0, 0) の国が見る。
        var nations = WorldData.Nations();
        Assert.Equal(61, nations.Length);
        Assert.Equal(61, nations.Count(c => c.TreasuryMonths > 0));
        Assert.Equal(18, nations.Count(c => c.GovDebtToGdpYear > 0));
        Assert.Equal(1.34, nations.Single(c => c.Name == "イギリス").GovDebtToGdpYear);
        Assert.Equal(2.5, nations.Single(c => c.Name == "イギリス").TreasuryMonths);
    }

    [Fact]
    public void 規則の国は助走の各四半期の終わりに国庫と国債を式どおりに置き台帳が閉じる()
    {
        // 規則のある2国（月数と比を国で変える＝取り違えを捕まえる）と、規則のない1国（旧のまま絶対額へ戻す）。
        var uk = With("イギリス", 3.0, 1.34); var fr = With("フランス", 1.5, 0.56); var pr = With("プロイセン", 0, 0);
        double prInitial = pr.Treasury;
        var sim = new MultiNationSim([uk, fr, pr], 1789UL, _ => new UtilityAgent());
        var bad = new List<string>(); int calls = 0;
        sim.StartWarmup(() =>
        {
            calls++;
            foreach (var n in sim.Nations)
            {
                var w = n.World; string who = $"{w.SelfPower?.Name}・助走{calls}";
                if (w.OpeningTreasuryMonths > 0)
                {
                    double revenue = w.LastRevenuePerMonth.Value;
                    if (!(revenue > 0)) { bad.Add($"{who}：歳入が正でない（{revenue}）＝試験の盤面が成り立たない"); continue; }
                    double wantTreasury = revenue * w.OpeningTreasuryMonths;
                    if (!(System.Math.Abs(w.Treasury.Value - wantTreasury) <= 1e-9 * System.Math.Max(1.0, wantTreasury))) bad.Add($"{who}：国庫 {w.Treasury.Value} ≠ 歳入×月数 {wantTreasury}");
                    double wantDebt = System.Math.Max(w.OpeningGovDebtToGdpYear * Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value, w.ForeignDebt.Value);
                    if (!(System.Math.Abs(w.GovDebt.Value - wantDebt) <= 1e-9 * System.Math.Max(1.0, wantDebt))) bad.Add($"{who}：国債 {w.GovDebt.Value} ≠ 比×年名目GDP {wantDebt}");
                    double dom = w.GovDebt.Value - w.ForeignDebt.Value;
                    if (!(dom >= 0)) bad.Add($"{who}：国内の元本が負 {dom}");
                    if (!(System.Math.Abs(Held(w) - dom) <= 1e-8 * System.Math.Max(1.0, w.GovDebt.Value))) bad.Add($"{who}：Σ保有 {Held(w)} ≠ 国内の元本 {dom}");
                    if (dom > 0 && !AnyHolder(w)) bad.Add($"{who}：国内の元本があるのに持ち手がいない");
                }
                else if (System.Math.Abs(w.Treasury.Value - prInitial) > 1e-9 * System.Math.Max(1.0, prInitial)) bad.Add($"{who}：規則のない国の国庫 {w.Treasury.Value} が旧の戻し {prInitial} と違う");
            }
        }, 3);
        Assert.Equal(3, calls);
        Assert.True(bad.Count == 0, string.Join("\n", bad));
        // 台帳の不変条件（国債の保有＝BondLedger）が助走の後の盤面でも割れない
        foreach (var n in sim.Nations) Assert.True(BondLedger.Balanced(n.World, new MoneyStock(Held(n.World))), $"{n.World.SelfPower?.Name}：保有の和が国内の元本と合わない");
    }

    [Fact]
    public void 対外債のある国は国内の元本を負にしない_対外債は触らない()
    {
        var uk = With("イギリス", 3.0, 0.5);
        var sim = new MultiNationSim([uk], 1789UL, _ => new UtilityAgent());
        var w = sim.Nations[0].World;
        double big = 1e12;   // 比×年名目GDP より桁違いに大きい対外債
        w.ForeignDebt = new MoneyStock(big); w.GovDebt = new MoneyStock(big);
        w.RebaseOpeningFiscal(7.0);
        Assert.Equal(big, w.ForeignDebt.Value);            // 対外債には触れない（貸し手の国の債権と引き落としの予約が相手側に立っている）
        Assert.Equal(big, w.GovDebt.Value);                // 比×GDP が対外債より小さいときは対外債が下限
        Assert.Equal(0.0, Held(w));                        // 国内の元本 0 ＝ 持ち手なし
        Assert.True(BondLedger.Balanced(w, new MoneyStock(Held(w))));
    }

    [Fact]
    public void 歳入が正でない四半期は国庫を置かず旧の戻しへ落ちる_NaNも通さない()
    {
        var c = With("イギリス", 3.0, 0.0);   // 国債の比は 0＝国庫の規則だけ
        var sim = new MultiNationSim([c], 1789UL, _ => new UtilityAgent());
        var w = sim.Nations[0].World;
        double debt0 = w.GovDebt.Value;
        w.LastRevenuePerMonth = new MoneyPerMonth(double.NaN);
        w.RebaseOpeningFiscal(123.0);
        Assert.Equal(123.0, w.Treasury.Value);
        w.LastRevenuePerMonth = new MoneyPerMonth(0.0);
        w.RebaseOpeningFiscal(456.0);
        Assert.Equal(456.0, w.Treasury.Value);
        w.LastRevenuePerMonth = new MoneyPerMonth(10.0);
        w.RebaseOpeningFiscal(789.0);
        Assert.Equal(30.0, w.Treasury.Value, 9);   // 歳入 10 × 月数 3
        Assert.Equal(debt0, w.GovDebt.Value);       // 比 0 ＝国債に触れない
    }
}

/// <summary>
/// ★(332) 助走なしの器のテスト用＝開幕の国庫と国債を絶対額で持つ国の設定（<see cref="NationConfig.Treasury"/>・<see cref="NationConfig.GovDebt"/>）を返す。
/// 開幕の国庫と国債を比から導く規則（腕 B＝world.json が国庫 0・国債 0 の絶対額を持たない）の世界でも、助走を通さずに <c>new Simulation</c>／<c>new MultiNationSim</c> で組む番人が
/// 〈国庫が正〉〈仏に国債がある〉を前提にできるようにする（主張は変えない）。絶対額は r331 の world.json の値（手で書いた表＝world.json から作らない）。
/// 既定の world.json（絶対額あり）では、設定が既に正の値を持つので何も変えない（クローンを返すだけ）。
/// </summary>
internal static class OpeningFixture
{
    private static readonly System.Collections.Generic.Dictionary<string, (double Treasury, double GovDebt)> Absolute = new()
    {
        ["イギリス"] = (21000, 750000), ["フランス"] = (3700, 470000), ["プロイセン"] = (46000, 0),
    };

    public static NationConfig Named(string name)
    {
        var c = WorldData.ByName(name).ShallowClone();   // 共有の世界データは書き換えない
        if (Absolute.TryGetValue(name, out var a))
        {
            if (!(c.Treasury > 0)) c.Treasury = a.Treasury;
            if (!(c.GovDebt > 0)) c.GovDebt = a.GovDebt;
        }
        return c;
    }
}
