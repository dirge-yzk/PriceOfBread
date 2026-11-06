using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(889) 線 c75cp：議会の統制力の項ごとの寄与の表 <c>crownpc_s</c>（読むだけ）の番人＝計器の不変条件。事前登録 measurements/m889_20261106_prereg_cp.txt の T0〜T6b。
/// 期待値は計器と別の出所から作る：係数の既定値から手で書いた数（T1〜T4）・エンジンを直に Step させた後の <see cref="World.ParliamentControl"/>（T0・T1・T2・T4・T5）・fm_s が四半期の頭に写した値（T6a）。
/// 突然変異（使い捨ての木・measurements/m889_20261106/mutation.txt）：M1 召集の係数を 2 倍・M2 Clamp01 を外す・M3 ① の保持力の門を外す・M4 巻き返しの王冠の守りを外す・
/// M5 前の断面を貯めず今の値で数える・M6 交差の門の許しを 1 へ緩める。</summary>
[Collection("Instruments")]
public class CrownPcInstrumentTests
{
    private static World Nation(NationConfig cfg)
    {
        var w = new Simulation(cfg.ToScenario(5), new NullAgent()).World;
        Assert.True(w.PoliticsEnabled, "前提：政治が動く世界（Politics.Step が CrownParliament.Step を呼ぶ世界）");
        return w;
    }

    /// <summary>①②②′ を閉じる（率を 0 へ）＝召集と巻き返しだけが動く盤面。</summary>
    private static void CloseContinuous(Coefficients c) { c.ParliamentPressureRate = 0; c.ArmyPcPressure = 0; }

    /// <summary>T0：前の断面を取り、Step を直に呼んだ直後に数える（共有層を挟まない）＝事象の無い四半期の残差は 0。仏・英・米・日・露を 8 四半期。</summary>
    [Fact]
    public void Stepの直後に数えれば残差は0()
    {
        int rows = 0, active = 0;
        foreach (var cfg in new[] { NationConfig.France(), NationConfig.Britain(), NationConfig.America(), NationConfig.Japan(), NationConfig.Russia() })
        {
            var w = Nation(cfg); var c = w.Coef;
            if (w.Polity != Polity.Republic) w.ParliamentControl = 0.3;   // ② が動く高さ（王権の支持が代議制を上回る国で）
            for (int q = 0; q < 8; q++)
            {
                w.AusterityIntensity = q % 2 == 0 ? 0.9 : 0.2;
                var prev = Instruments.CrownSnap(w);
                CrownParliament.Step(w, c);
                var row = Instruments.CrownCompute(w, prev);
                Assert.False(row.RegimeEvent);   // Step は ApplyRegime を呼ばない
                if (row.Relabel) continue;       // 再ラベルは Step の中で参政権を動かしうる（① の cur が読む）＝自己申告の限界
                rows++;
                if (row.Run.Summon != 0 || row.Run.Term1 != 0 || row.Run.Term2 != 0 || row.Run.Term2p != 0) active++;
                Assert.True(row.Explain is 0 or 7, $"{Instruments.NameOf(w)} q{q} explain={row.Explain} residual={row.Residual}");
                if (row.Explain == 0) Assert.Equal(0.0, row.Residual);
            }
        }
        Assert.True(rows >= 30, $"前提：再ラベルの無い行が十分ある rows={rows}");
        Assert.True(active >= 10, $"前提：項が動いた行がある active={active}");
    }

    /// <summary>T1：召集だけが効く盤面（Austerity 0.8・①②②′ を閉じる・巻き返しの門を閉じる）。summonTerm＝0.02×(0.8−0.5)×2＝0.012。</summary>
    [Fact]
    public void 召集だけの盤面で召集の項は式の値で残差は0()
    {
        var w = Nation(NationConfig.France()); var c = w.Coef;
        Assert.Equal(0.02, c.SummonParliamentRate);   // 手の数の前提
        CloseContinuous(c);
        w.ParliamentControl = 0.2; w.CoopParliamentCrown = 0; w.AusterityIntensity = 0.8;
        var prev = Instruments.CrownSnap(w);
        CrownParliament.Step(w, c);
        var row = Instruments.CrownCompute(w, prev);
        Assert.True(Math.Abs(row.Run.Summon - 0.012) <= 1e-15, $"summon={row.Run.Summon}");
        Assert.True(Math.Abs(w.ParliamentControl - 0.2 - 0.012) <= 1e-15);
        Assert.False(row.Run.SummonClamp);
        Assert.Equal(0.0, row.Run.Rollback); Assert.Equal(0.0, row.Run.Term1); Assert.Equal(0.0, row.Run.Term2); Assert.Equal(0.0, row.Run.Term2p);
        Assert.Equal(0.0, row.Residual);
        Assert.Equal(0, row.Explain);
    }

    /// <summary>T2：Clamp01 に当たる盤面（pcPrev 0.995・Austerity 1.0＝素の和 1.015）＝summonClampHitFlag・summonTerm＝1−0.995。</summary>
    [Fact]
    public void 召集が1で切られた四半期に切りの印が立つ()
    {
        var w = Nation(NationConfig.France()); var c = w.Coef;
        Assert.Equal(0.02, c.SummonParliamentRate);
        CloseContinuous(c);
        w.ParliamentControl = 0.995; w.CoopParliamentCrown = 0; w.AusterityIntensity = 1.0;
        var prev = Instruments.CrownSnap(w);
        CrownParliament.Step(w, c);
        Assert.Equal(1.0, w.ParliamentControl);   // 前提：エンジンが 1 で切った
        var row = Instruments.CrownCompute(w, prev);
        Assert.True(row.Run.SummonClamp);
        Assert.True(Math.Abs(row.Run.Summon - 0.005) <= 1e-12, $"summon={row.Run.Summon}");
        Assert.Equal(0.0, row.Residual);
        Assert.Equal(0, row.Explain);
    }

    /// <summary>T3：① の門（純粋な関数に手の入力）。rep 0.6・cur 0.3・inertia 0.1・動員 2・保持力 1 → term1＝0.04×0.3。門の片方を閉じると 0。</summary>
    [Fact]
    public void 議会の圧力は門が2つとも開いたときだけ動く()
    {
        var c = new Coefficients();
        Assert.Equal(0.04, c.ParliamentPressureRate);
        var x = new Instruments.CrownInputs { Rep = 0.6, Inertia = 0.1, Mobilization = 2, Coerce = 1, Legitimacy = 0.5 };
        Instruments.CrownRun Run(in Instruments.CrownInputs v) => Instruments.CrownApply(v, c, 0.2, Polity.Absolutist, CrownForm.Absolutist, SeizureForm.None, 0, 0, 0.3);
        var open = Run(x);
        Assert.True(open.T1Support && open.T1Coerce);
        Assert.True(Math.Abs(open.Term1 - 0.012) <= 1e-15, $"term1={open.Term1}");
        var noCoerce = x; noCoerce.Mobilization = 0.5;   // 動員＜保持力
        var r2 = Run(noCoerce);
        Assert.False(r2.T1Coerce); Assert.Equal(0.0, r2.Term1);
        var noSupport = x; noSupport.Inertia = 0.4;      // 支持の差 0.3＜慣性 0.4
        var r3 = Run(noSupport);
        Assert.False(r3.T1Support); Assert.Equal(0.0, r3.Term1);
        var seized = Instruments.CrownApply(x, c, 0.2, Polity.MilitaryDictatorship, CrownForm.Absolutist, SeizureForm.Military, 0, 0, 0.3);   // 奪取中は塊ごと閉じる
        Assert.False(seized.BlockOpen); Assert.Equal(0.0, seized.Term1);
    }

    /// <summary>T4：巻き返し。王冠あり・前の協力度 −0.8・正統性 0.9・前の軍の帰属 0.5 → rollbackTerm＝−0.02。王冠なし（共和）→ 0 と門の因子の列。</summary>
    [Fact]
    public void 巻き返しは前の断面の協力度と軍の帰属で門を読み王冠の無い国では0()
    {
        var w = Nation(NationConfig.France()); var c = w.Coef;
        Assert.Equal(0.02, c.SummonParliamentRate);
        CloseContinuous(c);
        w.ParliamentControl = 0.3; w.AusterityIntensity = 0; w.CoopParliamentCrown = -0.8; w.Legitimacy = 0.9; w.ArmyAllegiance = 0.5;
        var prev = Instruments.CrownSnap(w);
        CrownParliament.Step(w, c);
        Assert.True(w.CoopParliamentCrown >= -0.5 || w.ArmyAllegiance <= 0.3, "前提：Step の後の値なら門が閉じる（今の値で数える誤りを落とせる）");
        var row = Instruments.CrownCompute(w, prev);
        Assert.True(row.Run.RbCrown && row.Run.RbCoop && row.Run.RbLeg && row.Run.RbArmy);
        Assert.True(Math.Abs(row.Run.Rollback + 0.02) <= 1e-15, $"rollback={row.Run.Rollback}");
        Assert.True(Math.Abs(w.ParliamentControl - 0.28) <= 1e-15);
        Assert.Equal(0.0, row.Residual);

        var a = Nation(NationConfig.America()); var ca = a.Coef;
        Assert.Equal(CrownForm.None, a.Crown);   // 前提：王冠の無い国
        CloseContinuous(ca);
        a.AusterityIntensity = 0; a.CoopParliamentCrown = -0.8; a.Legitimacy = 0.9; a.ArmyAllegiance = 0.5;
        var prevA = Instruments.CrownSnap(a);
        CrownParliament.Step(a, ca);
        var rowA = Instruments.CrownCompute(a, prevA);
        Assert.False(rowA.Run.RbCrown);
        Assert.True(rowA.Run.RbCoop && rowA.Run.RbLeg && rowA.Run.RbArmy);   // 他の門の因子は値を出す
        Assert.Equal(0.0, rowA.Run.Rollback);
        Assert.Equal(-0.8, rowA.Prev!.Coop); Assert.Equal(0.5, rowA.Prev!.Army);
        Assert.Equal(0.0, rowA.Residual);
    }

    /// <summary>T5：政変（ApplyRegime）。Step の前なら regimeEventFlag と explainCode 4・Step の後なら explainCode 3。</summary>
    [Fact]
    public void 政変の四半期は印と説明の区分で残差が説明される()
    {
        var w = Nation(NationConfig.France()); var c = w.Coef;
        CloseContinuous(c);
        w.ParliamentControl = 0.0; w.CoopParliamentCrown = 0; w.AusterityIntensity = 0.8;
        var prev = Instruments.CrownSnap(w);
        CrownParliament.ApplyRegime(w, c, Polity.ConstitutionalMonarchy);   // Transition＝Step の前
        CrownParliament.Step(w, c);
        var row = Instruments.CrownCompute(w, prev);
        Assert.True(row.RegimeEvent);
        Assert.NotEqual(0.0, row.Residual);   // 前の断面から始めると説明できない
        Assert.Equal(0.0, row.ResidualAlt);
        Assert.Equal(4, row.Explain);

        var w2 = Nation(NationConfig.France()); var c2 = w2.Coef;
        CloseContinuous(c2);
        w2.ParliamentControl = 0.0; w2.CoopParliamentCrown = 0; w2.AusterityIntensity = 0.8;
        var prev2 = Instruments.CrownSnap(w2);
        CrownParliament.Step(w2, c2);
        CrownParliament.ApplyRegime(w2, c2, Polity.Republic);   // 講和の矯正・内戦の決着＝Step の後
        var row2 = Instruments.CrownCompute(w2, prev2);
        Assert.True(row2.RegimeEvent);
        Assert.Equal(3, row2.Explain);
        Assert.Equal(1.0, row2.PcNow);
    }

    // ---- 交差の門（fm_s の ParlControl は四半期の頭の写し＝crownpc_s の pcPrevShare と一致） ----
    internal const double CrossTol = 1e-12;

    /// <summary>fm_s の netIncreasingFlag が空でない行ごとに、同じ (quarter, nationUid) の crownpc_s の pcPrevShare（＋shift）と parliamentControlShare を比べる。</summary>
    internal static (int Matched, int Mismatched, int Missing) Cross(List<Dictionary<string, string>> crown, List<Dictionary<string, string>> fm, double shift)
    {
        var map = new Dictionary<(string, string), double>(); var first = new HashSet<(string, string)>();
        foreach (var r in crown)
            if (r["firstRowFlag"] == "0") map[(r["quarter"], r["nationUid"])] = SubstInstrumentTests.D(r, "pcPrevShare") + shift;
            else first.Add((r["quarter"], r["nationUid"]));   // 最初の行（前の断面なし）＝照合しない
        int ok = 0, bad = 0, miss = 0;
        foreach (var f in fm)
        {
            if (f["netIncreasingFlag"].Length == 0 || first.Contains((f["quarter"], f["nationUid"]))) continue;
            if (!map.TryGetValue((f["quarter"], f["nationUid"]), out double pc)) { miss++; continue; }
            double v = SubstInstrumentTests.D(f, "parliamentControlShare");
            if (Math.Abs(v - pc) <= CrossTol) ok++; else bad++;
        }
        return (ok, bad, miss);
    }

    private static (List<Dictionary<string, string>> Crown, List<Dictionary<string, string>> Fm) RunTwo(int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_crownpc_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1791UL) { DiplomacyFrozen = true };
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1791UL, q); } }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var crown = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "crownpc_s1791.tsv"), Instruments.CrownHeaderText);
        var fm = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "fm_s1791.tsv"), null);
        return (crown, fm);
    }

    /// <summary>T6a：多国シムの 6 四半期で、fm_s が四半期の頭に写した統制力と crownpc_s の前の四半期の末の断面が一致する。行は国×四半期で全てある。</summary>
    [Fact]
    public void 軍事費の施行が写した統制力は前の四半期の末の断面に一致する()
    {
        var (crown, fm) = RunTwo(6);
        Assert.Equal(2 * 6, crown.Count);   // 全ての国・毎四半期
        var (ok, bad, miss) = Cross(crown, fm, 0);
        Assert.True(ok >= 1, $"前提：照合できる fm_s の行がある ok={ok} miss={miss}");
        Assert.Equal(0, bad);
        Assert.Equal(0, miss);
        foreach (var r in crown.Where(r => r["firstRowFlag"] == "0"))
        {
            var before = crown.Single(p => p["nationUid"] == r["nationUid"] && p["quarter"] == (int.Parse(r["quarter"]) - 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(before["pcNowShare"], r["pcPrevShare"]);   // 断面は前の行の pcNow
        }
    }

    /// <summary>T6b：交差の門の門＝pcPrev を 1e-6 ずらすと不一致が出る（許しを緩めると赤）。</summary>
    [Fact]
    public void 交差の門はずらした統制力を落とす()
    {
        var (crown, fm) = RunTwo(4);
        var (ok, bad, _) = Cross(crown, fm, 1e-6);
        Assert.True(bad >= 1, $"ずらした値を落とせない ok={ok} bad={bad}");
        Assert.Equal(0, ok);
    }
}
