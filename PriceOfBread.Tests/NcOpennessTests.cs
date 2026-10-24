using System;
using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>P12残渣（原典2026-07-17 P12節・実装2026-08-09）の番人。
/// ①開放度導出（旧の旗 CivicOpennessEnabled は 2026-10-18 に焼き込み）＝民族抵抗を速度drag/coreから原典式の天井「1−民族距離×(1−開放度)」へ一本化。
/// ②移民NC運搬（旧 MigrantCarriesConsciousness＝2026-09-30 段4e で焼き込み＝人の移し方の1本 Pop.Blend）＝越境移民が出身popの国民意識を持参しSize加重平均で混合（総量保存）。
/// ★2026-08-09に点火（両係数とも既定1.0・ign_p12）。旧の旗は 2026-10-18 までに両方とも焼き込み。</summary>
public class NcOpennessTests(ITestOutputHelper output)
{
    // ── 残渣①：開放度の天井 ─────────────────────────────────────────────

    /// <summary>合成世界＝仏の実データ構造で全popのNCと民族シェアをピンし、蓄積1回の増分を式と突き合わせる。
    /// minority＝Size>0 の最初のpop（民族距離1）・core＝その次のSize>0のpop（民族距離0）。</summary>
    private static (World w, Pop minority, Pop core) OpennessWorld()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
        var w = sim.World;
        foreach (var p in w.AllPops()) { p.NationalConsciousness = 0.5; p.CoreEthnicShare = new Share(1.0); }
        Pop? minority = null, core = null;
        foreach (var p in w.Regions[0].Pops)
        {
            if (!(p.Size > 1e-9)) continue;   // 蓄積ループが飛ばすpopは対象にしない
            if (minority is null) { minority = p; continue; }
            core = p; break;
        }
        minority!.CoreEthnicShare = new Share(0.0);   // 民族距離1＝完全な非コアpop（天井式の感度が最大）
        return (w, minority, core!);
    }

    private static Coefficients OpennessCoef() => new()
    {
        NationConsciousnessReachDecay = 0,   // reach=1.0＝距離を消す（天井だけを単離）
    };

    // 期待値（reach=1）：天井 = ceiling×(1−民族距離×(1−開放度))
    private static double ExpectedOn(double nc0, double coreShare, double openness, double drive, double ceiling, double decay)
    {
        double ceil = ceiling * (1.0 - (1.0 - coreShare) * (1.0 - openness));
        double head = Math.Max(0, ceil - nc0);
        return Math.Clamp(nc0 * (1 - decay) + drive * head, 0, 1);
    }

    [Fact]
    public void ON経路_少数派popの天井が開放度の式どおり動く()
    {
        const double drive = 0.5, ceiling = 0.9, decay = 0;
        // 低開放度（0.1）：非コアpopの天井=0.9×(1−1×0.9)=0.09＜現在値0.5＝headroom 0＝成長停止（独のポーランド人が天井で止まる）
        var (wLo, minLo, coreLo) = OpennessWorld();
        CrownParliament.AccumulatePopConsciousness(wLo, OpennessCoef(), drive, ceiling, decay, 0.1);
        Assert.Equal(ExpectedOn(0.5, 0.0, 0.1, drive, ceiling, decay), minLo.NationalConsciousness, 12);
        Assert.Equal(0.5, minLo.NationalConsciousness, 12);   // 式の帰結＝止まる（低開放度の非コアは国民化しない）
        // 高開放度（0.9）：天井=0.9×(1−1×0.1)=0.81＞0.5＝成長する（米のるつぼ＝移民popが国民化）
        var (wHi, minHi, coreHi) = OpennessWorld();
        CrownParliament.AccumulatePopConsciousness(wHi, OpennessCoef(), drive, ceiling, decay, 0.9);
        Assert.Equal(ExpectedOn(0.5, 0.0, 0.9, drive, ceiling, decay), minHi.NationalConsciousness, 12);
        Assert.True(minHi.NationalConsciousness > minLo.NationalConsciousness, "開放度が高いほど非コアpopの天井が開くはず");
        // コアpop（民族距離0）は開放度に無反応＝天井は ceiling のまま（式：1−0×(1−開放度)=1）
        Assert.Equal(ExpectedOn(0.5, 1.0, 0.1, drive, ceiling, decay), coreLo.NationalConsciousness, 12);
        Assert.Equal(coreLo.NationalConsciousness, coreHi.NationalConsciousness, 12);
    }

    [Fact]
    public void 開放度の導出_共和経験と参政権と自由度の等分()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
        var w = sim.World;
        // 専制・参政権なし・低自由＝旧体制（1783仏の素の形）
        w.Polity = Polity.Absolutist; w.Suffrage = Suffrage.None; w.Freedom = 0.2;
        Assert.Equal((0.0 + 0.0 + 0.2) / 3.0, CrownParliament.CivicOpenness(w), 12);
        // 共和×男女普通選挙×自由報道＝最大帯（革命→共和のjus soli）
        w.Polity = Polity.Republic; w.Suffrage = Suffrage.Universal; w.Freedom = 0.9;
        Assert.Equal((1.0 + 0.95 + 0.9) / 3.0, CrownParliament.CivicOpenness(w), 12);
        // 「あった履歴」＝専制へ後退しても議会制系の実績ストック正部（RegimeTrackRecord[1]）が開放度を支える（新規保存状態なし）
        w.Polity = Polity.Absolutist; w.Suffrage = Suffrage.None; w.Freedom = 0.2;
        w.RegimeTrackRecord[1] = 0.5;
        Assert.Equal((0.5 + 0.0 + 0.2) / 3.0, CrownParliament.CivicOpenness(w), 12);
        // 負の実績（失望）は開放度を下げない（正部のみ＝failed republicが血統国民を深めることはしない）
        w.RegimeTrackRecord[1] = -0.8;
        Assert.Equal((0.0 + 0.0 + 0.2) / 3.0, CrownParliament.CivicOpenness(w), 12);
    }

    [Fact]
    public void 実世界1783_代表国の開放度は米が最高で専制諸国は低い()
    {
        var cfgs = WorldData.Nations();
        var sim = new MultiNationSim(cfgs, 1789UL);
        double Find(string name)
        {
            for (int i = 0; i < cfgs.Length; i++)
                if (cfgs[i].Name == name) return CrownParliament.CivicOpenness(sim.Nations[i].World);
            throw new InvalidOperationException($"国が見つからない：{name}");
        }
        string[] names = ["フランス", "イギリス", "アメリカ", "ロシア", "清", "オスマン"];
        foreach (var n in names) output.WriteLine($"{n} : 開放度 {Find(n):F4}");
        Assert.True(Find("アメリカ") > 0.8, "米＝共和×自由報道×男子普通（floor-raise）＝最高帯のはず");
        Assert.InRange(Find("イギリス"), 0.5, 0.7);   // 英＝立憲×制限選挙×検閲付報道＝中間帯
        foreach (var n in new[] { "フランス", "ロシア", "清", "オスマン" })
            Assert.True(Find(n) < 0.15, $"{n}＝専制/絶対王政×参政権なし＝低帯のはず（実測 {Find(n):F4}）");
        Assert.True(Find("アメリカ") > Find("イギリス") && Find("イギリス") > Find("フランス"), "米＞英＞仏の序列");
    }

    // ── 残渣②：移民NC運搬 ─────────────────────────────────────────────

    /// <summary>2国世界＝スイス→ジェノヴァ。大衆pop（農民・労働者）のNCをピンして越境転送1回を単離。</summary>
    private static (MultiNationSim sim, World src, World dst, Region srcR, Region dstR) MigrationWorld()
    {
        var sim = new MultiNationSim([WorldData.ByName("スイス"), WorldData.ByName("ジェノヴァ")], seed: 17);
        var src = sim.Nations[0].World; var dst = sim.Nations[1].World;
        var srcR = src.Regions[0]; var dstR = dst.Regions[0];
        foreach (int cls in new[] { PopClass.Peasant, PopClass.Worker })
        {
            var sp = srcR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier);
            var dp = dstR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier);
            if (sp is not null) sp.NationalConsciousness = 0.8;   // 出発国＝高NC
            if (dp is not null) dp.NationalConsciousness = 0.2;   // 到着国＝低NC
        }
        return (sim, src, dst, srcR, dstR);
    }

    /// <summary>NC×Size の総量（初期化済みpopのみ・両国合算）＝移動で保存されるべき量。</summary>
    private static double NcMass(params World[] worlds)
    {
        double m = 0;
        foreach (var w in worlds)
            foreach (var p in w.AllPops())
                if (p.NationalConsciousness >= 0) m += p.NationalConsciousness * p.Size;
        return m;
    }

    [Fact]
    public void ON経路_移民は出身NCを持参しSize加重平均で混ざり総量が保存される()
    {
        var (sim, src, dst, srcR, dstR) = MigrationWorld();
        var before = new System.Collections.Generic.Dictionary<int, (double spSize, double dpSize, double dpNc)>();
        foreach (int cls in new[] { PopClass.Peasant, PopClass.Worker })
        {
            var sp = srcR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier);
            var dp = dstR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier);
            if (sp is not null && dp is not null) before[cls] = (sp.Size, dp.Size, dp.NationalConsciousness);
        }
        double massBefore = NcMass(src, dst);
        foreach (var (cls, b) in before) sim.MoveMigrants(src, dst, srcR, dstR, cls, sim.Coef.EmigrationMaxPerQuarter.Value * b.spSize);   // ★回1-2：流量は呼び手が決める（1階級ずつ）
        int movedClasses = 0;
        foreach (var (cls, b) in before)
        {
            var sp = srcR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier)!;
            var dp = dstR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier)!;
            double flow = b.spSize - sp.Size;
            if (!(flow > 1e-9)) continue;
            movedClasses++;
            // 出発pop の1人当たりNCは不変（人が減っただけ）・到着popはSize加重平均
            Assert.Equal(0.8, sp.NationalConsciousness, 12);
            Assert.Equal((b.dpNc * b.dpSize + 0.8 * flow) / (b.dpSize + flow), dp.NationalConsciousness, 12);
            Assert.True(dp.NationalConsciousness > b.dpNc, "高NC移民の流入で到着popのNCは上がるはず");
        }
        Assert.True(movedClasses > 0, "1階級も移動していない＝テスト前提の破れ");
        Assert.Equal(massBefore, NcMass(src, dst), 6);   // 総量保存＝NC×Sizeは湧かず消えず（移転のみ）
    }

    // 【削除2026-09-30 段4e I】休眠_移民のNCは運ばれず到着国の値を纏う（OFF経路の回帰ピン）＝旗 MigrantCarriesConsciousness を焼き込んだ＝経路ごと無い。

    // ── 段4（2026-09-09）：§7-7の実バグ2件 ─────────────────────────────
    // a. 同化が速すぎる（到達年で判定）／b. 国民意識が国内経路で保存しない（裁定5の導出化で定義ごと消す）

    /// <summary>同化の速度は**到達年から逆算**した値である（勘で置いていないことの番人）。
    /// 式は「毎四半期 民族差 ×(1−a)」なので、民族差が半分になる年数 = ln2 / (−ln(1−a)) / 4。</summary>
    [Fact]
    public void 段4_同化の速度は半減期100年と1000年から逆算されている()
    {
        var c = new Coefficients();
        double HalfLifeYears(double a) => Math.Log(0.5) / Math.Log(1.0 - a) / Calendar.QuartersPerYear;
        // 公教育なし＝半減期1000年（墺の独語話者シェアが4回の国勢調査で横這い）
        Assert.Equal(1000.0, HalfLifeYears(c.EthnicAssimilation), 1);
        // 公教育あり＝半減期100年（仏＝革命から農民をフランス人にするのにほぼ1世紀）
        double edu = c.EthnicAssimilation + c.AssimilationEducationBonus;
        Assert.Equal(100.0, HalfLifeYears(edu), 1);
        // 検算＝81年後（1789→1870）に閉じるのは半分未満＝ブルターニュ/オクシタニアはまだフランス語の多数派でない
        Assert.True(1.0 - Math.Pow(1.0 - edu, 81 * Calendar.QuartersPerYear) < 0.5);
        // 検算＝1914断面（主指標・Q524）までに公教育なしでは1割に届かない＝学校を持たない帝国は大戦まで多民族（墺洪の国勢調査の横這い）。
        //   ★2026-09-22：旧「ゲーム全長で1割未満」は長さの主張だった（604Qで9.9%・1950延長の668Qで10.9%）＝史実の主張の断面へ書き直した
        Assert.True(1.0 - Math.Pow(1.0 - c.EthnicAssimilation, Coefficients.FidelityQuarters) < 0.10);
    }

    /// <summary>同化の式を単離する2国世界（同化の段だけを呼ぶ＝CoreEthnicShare を動かす経路は同化1本）。</summary>
    private static (MultiNationSim sim, Pop pop) AssimWorld(double revival, double literacy, double core)
    {
        var sim = new MultiNationSim([WorldData.ByName("スイス"), WorldData.ByName("ジェノヴァ")], seed: 17);
        sim.Coef.AssimilationRevival = revival;
        var w = sim.Nations[0].World;
        w.EthnicEnabled = true;
        foreach (var p in w.AllPops()) { p.Literacy = literacy; p.CoreEthnicShare = new Share(core); }
        var pop = w.Regions[0].Pops.Find(x => x.Size > 1e-9)!;
        return (sim, pop);
    }

    [Fact]
    public void 段4_逆流は既定で起きない_床が同化を止めるだけ()
    {
        var (sim, pop) = AssimWorld(revival: 0, literacy: 1.0, core: 0.4);   // 識字1.0＝抵抗が最大の帯
        double before = pop.CoreEthnicShare.Value;
        sim.StepAssimilation();
        var c = sim.Coef;
        double assim = c.EthnicAssimilation + (sim.Nations[0].World.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory ? c.AssimilationEducationBonus : 0);
        double resist = Math.Max(0.0, 1.0 - c.AssimilationLiteracyResist * 1.0);
        Assert.Equal(before + assim * resist * (1.0 - before), pop.CoreEthnicShare.Value, 12);
        Assert.True(pop.CoreEthnicShare.Value > before, "既定（床あり）では識字がいくら高くても同化は止まるだけで逆流しない");
    }

    [Fact]
    public void 段4_点火すると識字の高い少数派で同化が逆流する()
    {
        const double ign = 1.0 / 0.95;   // 点火値＝反転点をボヘミア1900の識字0.95に置く
        // 識字0.99＞0.95＝逆流（自民族語の学校網と出版市場を持てた少数派）
        var (simHi, popHi) = AssimWorld(revival: ign, literacy: 0.99, core: 0.4);
        double beforeHi = popHi.CoreEthnicShare.Value;
        simHi.StepAssimilation();
        Assert.True(popHi.CoreEthnicShare.Value < beforeHi, $"識字0.99では中核度が下がるはず（{beforeHi:F6}→{popHi.CoreEthnicShare.Value:F6}）");
        // 識字0.58＜0.95＝なお同化（ハンガリーのマジャール化の反例が壊れないこと）
        var (simLo, popLo) = AssimWorld(revival: ign, literacy: 0.58, core: 0.4);
        double beforeLo = popLo.CoreEthnicShare.Value;
        simLo.StepAssimilation();
        Assert.True(popLo.CoreEthnicShare.Value > beforeLo, "識字0.58では同化が進むはず");
        // 反転点ちょうど（識字0.95）では1ビットも動かない＝符号が連続に反転する
        var (simMid, popMid) = AssimWorld(revival: ign, literacy: 0.95, core: 0.4);
        double beforeMid = popMid.CoreEthnicShare.Value;
        simMid.StepAssimilation();
        Assert.Equal(beforeMid, popMid.CoreEthnicShare.Value, 15);
    }

    /// <summary>逆流の安全弁＝中核度1.0のPOPは1ビットも動かない（単一文化国の自国民が自分から離反する暴走を構造で閉じる）。</summary>
    [Fact]
    public void 段4_逆流の核は中核度1のPOPを動かさない()
    {
        var (sim, pop) = AssimWorld(revival: 1.0 / 0.95, literacy: 1.0, core: 1.0);
        sim.StepAssimilation();
        Assert.Equal(1.0, pop.CoreEthnicShare.Value, 15);
    }

    /// <summary>裁定5＝国民意識は「盤面から読める量」＝**記憶を持たない**。同じ盤面なら何度積んでも同じ値に落ち、
    /// 値を壊してもその場で元へ戻る（ストックなら戻らない）。⚠これは機構の検査であって創発の主張ではない。</summary>
    [Fact]
    public void 段4_点火すると国民意識は記憶を持たない()
    {
        const double drive = 0.5, ceiling = 0.9, decay = 0.0025;
        var cOn = OpennessCoef(); cOn.NationConsciousnessDerived = 1.0;
        var (w, minority, core) = OpennessWorld();
        CrownParliament.AccumulatePopConsciousness(w, cOn, drive, ceiling, decay, 0.5);
        double a1 = minority.NationalConsciousness, b1 = core.NationalConsciousness;
        CrownParliament.AccumulatePopConsciousness(w, cOn, drive, ceiling, decay, 0.5);   // 同じ盤面でもう1回
        Assert.Equal(a1, minority.NationalConsciousness, 15);
        Assert.Equal(b1, core.NationalConsciousness, 15);
        // 盤面を壊す（外科的に0へ）→ 次の1回で同じ値へ戻る＝履歴を1ビットも持っていない
        minority.NationalConsciousness = 0; core.NationalConsciousness = 0;
        CrownParliament.AccumulatePopConsciousness(w, cOn, drive, ceiling, decay, 0.5);
        Assert.Equal(a1, minority.NationalConsciousness, 15);
        Assert.Equal(b1, core.NationalConsciousness, 15);
        // 式＝同じ漸化式の不動点 NC* = 天井 × d/(decay+d)（reach=1）
        double ceilMin = ceiling * (1.0 - (1.0 - 0.0) * (1.0 - 0.5));
        Assert.Equal(ceilMin * drive / (decay + drive), a1, 12);
        // 休眠側（既定0）は同じ呼び出しで積み上がる＝記憶を持つ
        var (w2, min2, _) = OpennessWorld();
        var cOff = OpennessCoef();
        Assert.Equal(0.0, cOff.NationConsciousnessDerived);   // 既定が休眠であることも同時にピン
        CrownParliament.AccumulatePopConsciousness(w2, cOff, drive, ceiling, decay, 0.5);
        double s1 = min2.NationalConsciousness;
        CrownParliament.AccumulatePopConsciousness(w2, cOff, drive, ceiling, decay, 0.5);
        // ⚠この盤面では少数派popの天井(0.45)が現在値(0.5)より低いので動く向きは**下**＝「増える」ではなく「動き続ける」が記憶の徴候
        Assert.True(Math.Abs(min2.NationalConsciousness - s1) > 1e-9, "休眠側はストック＝2回目も前期の値から動くはず（記憶がある）");
    }

    /// <summary>導出化の狙いの番人＝§7-7バグb「国民意識が国内経路（階級昇格・徴兵/復員・職業移動・州内移住）で
    /// 運搬されず保存しない」。導出量には運ぶべき状態が無いので、**人の移動でPOPの値が汚れても次の1期で消える**。
    /// ⚠保存則の検査ではない（保存の対象でなくなったことの検査である）。</summary>
    [Fact]
    public void 段4_点火すると国内経路の運搬漏れが定義ごと消える()
    {
        const double drive = 0.5, ceiling = 0.9, decay = 0.0025;
        var cOn = OpennessCoef(); cOn.NationConsciousnessDerived = 1.0;
        var (w, minority, _) = OpennessWorld();
        CrownParliament.AccumulatePopConsciousness(w, cOn, drive, ceiling, decay, 0.5);
        double settled = minority.NationalConsciousness;
        // 「運搬漏れ」の再現＝人だけ動いてNCが混ざらない（旧経路が実際にやっていたこと）
        minority.Size *= 2.0;
        minority.NationalConsciousness = 0.99;
        CrownParliament.AccumulatePopConsciousness(w, cOn, drive, ceiling, decay, 0.5);
        Assert.Equal(settled, minority.NationalConsciousness, 15);   // 盤面（民族距離・州・国の駆動）が同じなら同じ値
    }
}
