using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>特権の表示（作者指示2026-09-07(7)(8)＝POPタブの各行の下に1行）が読む観測量の機構テスト。
/// (a) 税の本体（TaxAssessment／Fiscal.Step の十分の一税の率）から残高関数へ畳んだ重複が**ビット同一**であること＝較正帯が動かない証拠。
/// (b) 受益クラスの判定＝身分制免税だけは閉鎖階級すべて（貴族と聖職者）。
/// (c) 休眠ゲートで1銭も動かない特権（同業組合＝GuildLawGate=0）は「在る」と名乗らない。
/// (d) 初期状態が国家の初期法に一致する（新しい状態を作っていない証拠）。</summary>
public class PrivilegeDisplayTests
{
    private static World Warm(NationConfig cfg)
    {
        var sim = new Simulation(cfg.ToScenario(41), new NullAgent());
        sim.Run(1);
        return sim.World;
    }

    private static void AssertBitEqual(double expected, double actual, string what)
        => Assert.True(System.BitConverter.DoubleToInt64Bits(expected) == System.BitConverter.DoubleToInt64Bits(actual),
                       $"{what}：{expected:R} と {actual:R} がビット同一でない");

    [Fact]
    public void 身分制免税の残高への畳み込みが旧式とビット同一()
    {
        var w = Warm(NationConfig.France());
        Pop? noble = null, worker = null;
        foreach (var p in w.AllPops())
        {
            if (noble is null && p.Closed && !p.IsSoldier) noble = p;
            if (worker is null && !p.Closed) worker = p;
        }
        Assert.NotNull(noble); Assert.NotNull(worker);
        var c = w.Coef;
        foreach (var polity in System.Enum.GetValues<Polity>())
        {
            w.Polity = polity;
            foreach (double disc in new[] { 0.0, 0.3, 0.6 })
                foreach (double fromCrown in new[] { 0.0, 0.8 })
                    foreach (double share in new[] { 0.0, 0.4, 1.3 })
                    {
                        c.PrivilegeTaxDiscount = new Share(disc); c.PrivilegeFromCrownShare = new Share(fromCrown);
                        // 旧式（2026-09-07の畳み込み前・Privileges.cs の原文）
                        double old = disc > 0 && noble!.Closed && !noble.IsSoldier
                            ? (fromCrown > 0
                                ? 1.0 - disc * fromCrown * System.Math.Clamp(share, 0, 1)
                                : (polity is Polity.Absolutist or Polity.Despotic or Polity.Shogunate ? 1.0 - disc : 1.0))
                            : 1.0;
                        AssertBitEqual(old, Privileges.TaxAssessment(w, c, noble!, share), $"免税の課税査定（{polity}/{disc}/{fromCrown}/{share}）");
                        AssertBitEqual(1.0, Privileges.TaxAssessment(w, c, worker!, share), "非特権階級の課税査定は常に1.0");
                    }
        }
    }

    [Fact]
    public void 十分の一税の残高への畳み込みが旧式とビット同一()
    {
        var w = Warm(NationConfig.France());
        var c = w.Coef;
        foreach (var law in System.Enum.GetValues<StateReligionLaw>())
        {
            w.StateReligion = law;
            foreach (double gate in new[] { 0.0, 1.0 })
                foreach (double rate in new[] { 0.0, 0.05, 0.2 })
                {
                    c.StateReligionGate = gate; c.TitheRate = new Share(rate);
                    // 旧式（畳み込み前の2文）
                    double old = rate;
                    if (gate > 0)
                        old *= law switch { StateReligionLaw.InnerFreedom => c.InnerFreedomTithe, StateReligionLaw.ReligiousFreedom => c.ReligiousFreedomTithe, StateReligionLaw.StateAtheism => 0.0, _ => 1.0 };
                    // 畳み込み後（Fiscal.Step の十分の一税の率と同じ式）
                    AssertBitEqual(old, c.TitheRate.Value * Privileges.TitheDegree(w, c), $"十分の一税の実効率（{law}/{gate}/{rate}）");
                }
        }
    }

    [Fact]
    public void 身分制免税は貴族と聖職の両方が受ける()
    {
        Assert.True(Privileges.HeldBy(Privileges.TaxExemption, PopClass.Noble));
        Assert.True(Privileges.HeldBy(Privileges.TaxExemption, PopClass.Clergy));   // 第一身分の免税＝Beneficiary（代表1クラス）では落ちる
        Assert.False(Privileges.HeldBy(Privileges.TaxExemption, PopClass.Worker));
        Assert.True(Privileges.HeldBy(Privileges.Tithe, PopClass.Clergy));
        Assert.True(Privileges.HeldBy(Privileges.Seigneurial, PopClass.Noble));
        Assert.True(Privileges.HeldBy(Privileges.VestedOffice, PopClass.Bureaucrat));
        Assert.True(Privileges.HeldBy(Privileges.Monopoly, PopClass.Capitalist));
    }

    [Fact]
    public void 休眠ゲートの特権は表に出さない()
    {
        var c = new Coefficients();
        Assert.False(Privileges.Enabled(c, Privileges.Guild));        // GuildLawGate=0／GuildRentPremium=0＝1銭も動かない
        Assert.True(Privileges.Enabled(c, Privileges.Tithe));
        Assert.True(Privileges.Enabled(c, Privileges.TaxExemption));
        Assert.True(Privileges.Enabled(c, Privileges.Monopoly));
        c.GuildLawGate = 1.0; c.GuildRentPremium = 0.1;
        Assert.True(Privileges.Enabled(c, Privileges.Guild));         // 点火すれば出る（表示は法の現物に従う）
    }

    [Fact]
    public void 初期の特権は国家の初期法に一致する()
    {
        var fr = Warm(NationConfig.France());
        Assert.Equal(Polity.Absolutist, fr.Polity);
        Assert.True(Privileges.Degree(fr, fr.Coef, Privileges.TaxExemption) > 0, "絶対君主制の仏で身分制免税が立たない");
        Assert.True(Privileges.Degree(fr, fr.Coef, Privileges.Seigneurial) > 0, "農奴解放が全面でない仏で領主権が立たない");

        var gb = Warm(NationConfig.Britain());
        Assert.Equal(ReformLevel.Full, gb.SerfEmancipation);
        Assert.Equal(0.0, Privileges.Degree(gb, gb.Coef, Privileges.Seigneurial));           // 農奴解放が全面＝領主権は無い
        Assert.Equal(0.0, Privileges.Degree(gb, gb.Coef, Privileges.TaxExemption));          // 立憲君主制＝身分制免税は無い
        Assert.True(Privileges.Degree(gb, gb.Coef, Privileges.Tithe) > 0, "国教のイギリスで十分の一税が立たない");
    }

    // ★(383) 作者 2026-10-16「行政効率→廃止」：特権の見返り 段2（領主権の在地が貴族の行政の素地を変調する機構 NobleAdminFromSeigneurial）は行政効率の 4 因子ごと物理削除した。その番人 2 本（係数 0 で恒等・点火で線形で床を残す／在地は土地の保有と農奴の緊縛の論理和）も消した。

    /// <summary>特権の取り分は pop→pop の移転＝ゼロ和（国庫を通さず貨幣を作らない）。
    /// 十分の一税＝特権そのものが金を動かす現物の経路（★2026-09-30 段3-4＝Fiscal.Step の税の走査で税と同じ1本の率で集める＝旧 Privileges.CollectTithe は削除）。
    /// 盤面＝税・汚職の漏れ・福祉・年金・国債を止めた月次の財政1回＝動く金は十分の一税だけ（連合維持費は 2026-09-30 (135) に撤去）。</summary>
    [Fact]
    public void 十分の一税の取り分は聖職者への移転でゼロ和()
    {
        var w = Warm(NationConfig.France());
        var c = w.Coef;
        w.StateReligion = StateReligionLaw.StateChurch;
        c.TitheRate = new Share(0.05); c.StateReligionGate = 0; foreach (var rg0 in w.Regions) rg0.Rule = [1.0, 0.0, 0.0, 0.0];   // 国家が全域を治める（支配度の区分で目減りしない）
        w.TaxRate = new Share(0); w.PollTax = 0; w.LandTax = 0; w.ProgressiveTax = 0;
        w.SocialSecurity = 0; w.Pension = ReformLevel.None;
        w.GovDebt = new MoneyStock(0); w.ForeignDebt = new MoneyStock(0);
        var pops = new System.Collections.Generic.List<Pop>(w.AllPops());
        foreach (var p in pops) p.BondHoldings = new MoneyStock(0);
        double clergySize = 0; foreach (var p in pops) if (p.ClassId == PopClass.Clergy) clergySize += p.Size;
        Assert.True(clergySize > 1e-9, "聖職者のいない世界では移転を測れない");
        double expected = 0;
        foreach (var p in pops)
        {
            p.LastEarnedPerMonth = new MoneyPerMonth(0);
            if (!p.Closed && !p.IsModernElite && !p.IsSoldier) { p.LastEarnedPerMonth = new MoneyPerMonth(100); p.SetSavings(100); expected += 0.05 * 100; }   // 払い手に確実に原資を持たせる
        }

        double Sum(System.Func<Pop, bool> pick) { double s = 0; foreach (var p in pops) if (pick(p)) s += p.Savings.Value; return s; }
        double before = Sum(_ => true), clergyBefore = Sum(p => p.ClassId == PopClass.Clergy), t0 = w.Treasury.Value;
        w.FiscalFundingRatio = new Share(1.0);
        Fiscal.Step(w, c);
        double after = Sum(_ => true), clergyAfter = Sum(p => p.ClassId == PopClass.Clergy);

        Assert.True(expected > 0, "十分の一税の請求が立っていない＝移転を測れていない");
        Assert.True(System.Math.Abs(after - before) <= 1e-9 * System.Math.Max(1, before), $"貯蓄の総和が動いた＝移転でなく生成/消滅（{before:R}→{after:R}）");
        Assert.Equal(t0, w.Treasury.Value);   // 国庫を通さない
        Assert.True(System.Math.Abs((clergyAfter - clergyBefore) - expected) <= 1e-9 * System.Math.Max(1, expected), $"請求の額と聖職者の受取が一致しない（{expected:R}/{clergyAfter - clergyBefore:R}）");
    }

    // ==== 剥離の怒り＝特権 段3（二量分離の設計メモ §7-6・2026-09-09）====================================================
    //   ★これは**インパルス**であって積分ではない（史実16件はすべて日付のある法令が引き金・中央値2か月）。
    //   ⚠測るのは3つ＝(a) 落ちた月に1度だけ立つ（翌月は積まない）(b) 受益層の判定が Privileges.HeldBy の1本
    //     （身分制免税は貴族と聖職の**両方**＝取り分を記帳する人口と怒る人口が一致する）(c) 係数0で1ビットも動かない。

    /// <summary>特権 k を「1か月活きて、翌月に法令で消える」形で回し、各月の受益層/非受益層の闘争度を返す。</summary>
    private static (double[] Held, double[] Other) 剥離を1回踏む(int kind, double rate)
    {
        var w = Warm(NationConfig.France());
        var c = w.Coef;
        c.PrivilegeLedgerEnabled = 1.0; c.PrivilegeStripMilitancy = rate;
        foreach (var p in w.AllPops()) p.Militancy = 0;
        var held = new double[3]; var other = new double[3];
        for (int m = 0; m < 3; m++)
        {
            if (m == 0) Privileges.Record(w, kind, 100.0);   // 1か月目だけ取り分が立つ＝2か月目が「落ちた月」
            Privileges.StepMonth(w, c);
            double hs = 0, hp = 0, os = 0, op = 0;
            foreach (var p in w.AllPops())
            {
                if (p.IsSoldier || p.Size <= 1e-9) continue;
                if (Privileges.HeldBy(kind, p.ClassId)) { hs += p.Militancy * p.Size; hp += p.Size; }
                else { os += p.Militancy * p.Size; op += p.Size; }
            }
            Assert.True(hp > 0 && op > 0, $"受益層/非受益層のどちらかが空＝対照が取れない（特権{Privileges.Names[kind]}）");
            held[m] = hs / hp; other[m] = os / op;
        }
        return (held, other);
    }

    [Fact]
    public void 段3_特権の剥離は落ちた月に1度だけ怒りを立てる()
    {
        Assert.Equal(0.0, new Coefficients().PrivilegeStripMilitancy);   // ★既定は休眠（点火したら本アサートも直す）
        const double rate = 0.5;   // ★点火値＝RegionalRevoltThreshold と同じ0.50（満期の特権を1度に剥がすと門に届く）
        var (held, other) = 剥離を1回踏む(Privileges.Tithe, rate);

        Assert.Equal(0.0, held[0]);                       // 取り分が立っている月＝怒りなし
        Assert.True(held[1] > 0, "剥離の月に怒りが立っていない");
        Assert.Equal(held[1], held[2], 12);               // ★翌月は**積まない**＝インパルス（積分ならここで倍になる）
        foreach (double v in other) Assert.Equal(0.0, v); // 受益層でない層は1ビットも動かない

        // 強度に比例する＝1783年から在る特権は**既に世代長ぶん既得権化している**（Privileges.StepMonth の初期化）＝強度1＝怒りは係数そのもの
        Assert.Equal(rate, held[1], 9);

        var (zero, _) = 剥離を1回踏む(Privileges.Tithe, 0.0);   // 係数0＝1ビットも動かない
        foreach (double v in zero) Assert.Equal(0.0, v);
    }

    [Fact]
    public void 段3_身分制免税の剥離は貴族と聖職の両方を怒らせる()
    {
        // ⚠旧は Beneficiary[k]（台帳の代表1クラス＝貴族）と直接比べていた＝**取り分を記帳する人口（閉鎖階級すべて）と
        //   怒る人口（貴族だけ）が違う**＝分子と分母の非対称。判定は Privileges.HeldBy の1本に畳んだ（2026-09-09）。
        Assert.True(Privileges.HeldBy(Privileges.TaxExemption, PopClass.Noble));
        Assert.True(Privileges.HeldBy(Privileges.TaxExemption, PopClass.Clergy));
        var (held, other) = 剥離を1回踏む(Privileges.TaxExemption, 0.5);
        Assert.True(held[1] > 0, "身分制免税の剥離で閉鎖階級が怒っていない");
        foreach (double v in other) Assert.Equal(0.0, v);

        // 聖職者だけを見ても実際に上がっている（＝「貴族の平均に紛れて見えているだけ」ではない）
        var w = Warm(NationConfig.France());
        var c = w.Coef; c.PrivilegeLedgerEnabled = 1.0; c.PrivilegeStripMilitancy = 0.5;
        foreach (var p in w.AllPops()) p.Militancy = 0;
        Privileges.Record(w, Privileges.TaxExemption, 100.0); Privileges.StepMonth(w, c);
        Privileges.StepMonth(w, c);
        double clergy = 0, clergySize = 0;
        foreach (var p in w.AllPops()) if (p.ClassId == PopClass.Clergy && !p.IsSoldier) { clergy += p.Militancy * p.Size; clergySize += p.Size; }
        Assert.True(clergySize > 1e-9, "聖職者のいない世界＝この検査は空洞");
        Assert.True(clergy / clergySize > 0, "聖職者が身分制免税の剥離で怒っていない（旧＝Beneficiary の代表1クラスだけを見ていた欠陥）");
    }
}
