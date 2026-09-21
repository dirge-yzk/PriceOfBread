using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 史実参照表（1914年・新較正基準 2026-07-09）の整合検査。
/// 参照表（HistoricalReference.Table）は world.json の53カ国と1:1でなければならない（国追加・改名時のドリフト検知）。
/// 判定帯（◎7割帯/固着/逸脱過多）の境界も固定＝バッチ出力の意味が変わらないことを保証。
/// </summary>
public class HistoricalReferenceTests
{
    [Fact]
    public void 参照表はworldjsonの全国家と一対一()
    {
        var nations = WorldData.Nations();
        Assert.Equal(nations.Length, HistoricalReference.Table.Length);
        foreach (var cfg in nations)
        {
            var e = Array.Find(HistoricalReference.Table, t => t.Nation == cfg.Name);
            Assert.True(e is not null, $"参照表に「{cfg.Name}」がない（world.json に国を追加したら docs/史実参照表_1914.md と HistoricalReference.cs を更新）");
            Assert.True(cfg.Provinces.Length > 0 && e!.Capital == cfg.Provinces[0].City,
                $"「{cfg.Name}」の首都名が不一致：参照表={e!.Capital} world.json={(cfg.Provinces.Length > 0 ? cfg.Provinces[0].City : "(州なし)")}");
        }
    }

    [Fact]
    public void 参照表のexactは非空でAnarchyを含まない()   // Anarchy＝一過性（秩序崩壊）＝1914年の史実解として不正
    {
        foreach (var e in HistoricalReference.Table)
        {
            Assert.True(e.Exact.Length > 0, $"「{e.Nation}」の exact が空");
            Assert.DoesNotContain(Polity.Anarchy, e.Exact);
            Assert.DoesNotContain(Polity.Anarchy, e.Near);
            foreach (var p in e.Near) Assert.True(Array.IndexOf(e.Exact, p) < 0, $"「{e.Nation}」の near に exact と重複：{p}");
        }
    }

    [Fact]
    public void 判定帯の境界が仕様どおり()   // ◎7割帯=near55-85% / 固着>90% / 逸脱過多<50% / 残り=△境界
    {
        Assert.Equal("逸脱過多", HistoricalReference.VerdictOf(0.49));
        Assert.Equal("△境界", HistoricalReference.VerdictOf(0.52));
        Assert.Equal("◎7割帯", HistoricalReference.VerdictOf(0.55));
        Assert.Equal("◎7割帯", HistoricalReference.VerdictOf(0.70));
        Assert.Equal("◎7割帯", HistoricalReference.VerdictOf(0.85));
        Assert.Equal("△境界", HistoricalReference.VerdictOf(0.88));
        Assert.Equal("固着", HistoricalReference.VerdictOf(0.95));
        Assert.Equal("固着", HistoricalReference.VerdictOf(1.00));
    }

    [Fact]
    public void 法一致の採点は許容集合どおり()
    {
        var uk = Array.Find(HistoricalReference.Table, t => t.Nation == "イギリス")!;
        // 英の5軸（参政権・教育・農奴解放・穀物法・土地改革）が全一致するケース
        // ★土地改革は2026-08-02に追加した軸（それ以前は忠実度に1ビットも寄与しない死んだ軸だった）。英は地主貴族が
        //   農地の大半を保有したまま1914を迎える＝なし。
        var hit = new NationStanding("ロンドン", 0, 0, 0, 0, 0, 0, 0, 0, Polity.ConstitutionalMonarchy, 0, 0,
            Suffrage: Suffrage.AdultMale, Education: EducationLaw.Compulsory, Serf: ReformLevel.Full, CornLaws: ReformLevel.None,
            LandReform: ReformLevel.None, Nation: "イギリス");
        Assert.Equal((5, 5), HistoricalReference.LawScore(uk, hit));
        // 穀物法Full（保護残存）だけ外すと4/5
        var miss = hit with { CornLaws = ReformLevel.Full };
        Assert.Equal((4, 5), HistoricalReference.LawScore(uk, miss));
        // 土地改革Full（地主貴族の解体）だけ外しても4/5＝新軸が実際に採点されている
        var missLand = hit with { LandReform = ReformLevel.Full };
        Assert.Equal((4, 5), HistoricalReference.LawScore(uk, missLand));
        Assert.Equal(5, HistoricalReference.AxesOf(uk));
    }

    /// <summary>★親軍法の撤去（2026-08-18）：軍国の法軸は「法の有無」でなく**軍の政治的優位の閾値**で照合する。
    /// 旧テスト相当（親軍法のbool照合）をこの形へ書き直したもの＝軸数（分母）は不変で、判定だけが連続量の閾値になる。
    /// ⚠この物差しなら英1913（国防費が中央歳出の43%）でも文民制度が厚いので偽になる＝金額では測らない。</summary>
    [Fact]
    public void 軍国の法軸は軍の政治的優位の閾値で採点される()
    {
        var prussia = Array.Find(HistoricalReference.Table, t => t.Nation == "プロイセン")!;   // MilitarismOk: true（軍国）
        var usa = Array.Find(HistoricalReference.Table, t => t.Nation == "アメリカ")!;          // MilitarismOk: false（非軍国）
        const double eps = 1e-9;
        double ok = HistoricalReference.MilitarismDegreeOk;

        // 閾値ちょうどは「軍国」側（>= で判定）＝普は一致・米は不一致
        var hot = new NationStanding("ベルリン", 0, 0, 0, 0, 0, 0, 0, 0, Polity.ConstitutionalMonarchy, 0, 0, Militarism: ok);
        var cold = hot with { Militarism = ok - eps };
        Assert.Equal(1, HistoricalReference.LawScore(prussia, hot).Matched - HistoricalReference.LawScore(prussia, cold).Matched);
        Assert.Equal(1, HistoricalReference.LawScore(usa, cold).Matched - HistoricalReference.LawScore(usa, hot).Matched);

        // 軸数（法一致の分母）は値に依らず一定＝物差しの分母を壊していない
        Assert.Equal(HistoricalReference.AxesOf(prussia), HistoricalReference.LawScore(prussia, hot).Axes);
        Assert.Equal(HistoricalReference.AxesOf(usa), HistoricalReference.LawScore(usa, cold).Axes);
    }

    /// <summary>★4実体への分解（2026-08-18）：軍国主義は単一スカラーではない。
    /// ①軍の練度は識字・組織・勤務年数から／②軍の政治的優位は**給与を1度も読まない**（西1931アサーニャ改革は満額俸給付き退役で最大のクーデタ）／
    /// ④将校の任免権は符号を持つ（君主＋／内閣−）。棄却された「軍事費×徴兵法の深さ」が復活していないことも同時に守る。</summary>
    [Fact]
    public void 軍国主義の4実体はそれぞれ別の量から出る()
    {
        var sim = new Simulation(NationConfig.Prussia().ToScenario(1), new NullAgent());
        var w = sim.World;

        // ① 練度＝識字×組織（専門官僚制・鉄道）×勤務年数。志願＝長期勤務が最高・皆兵＝短期が最低（英の長期志願兵 対 普仏の短期兵）
        Assert.True(PoliticalQuantities.ServiceLength(ConscriptionLaw.Volunteer)
                  > PoliticalQuantities.ServiceLength(ConscriptionLaw.Selective));
        Assert.True(PoliticalQuantities.ServiceLength(ConscriptionLaw.Selective)
                  > PoliticalQuantities.ServiceLength(ConscriptionLaw.Universal));
        w.Bureaucracy = BureaucracyLaw.Spoils; w.Railways = false;
        Assert.Equal(0.0, PoliticalQuantities.ArmyProfessionalism(w));   // 組織がなければ練度は立たない（識字だけでは軍にならない）

        // ② 軍の政治的優位は**給与を1度も読まない**（＝「金を積めば将校は黙る」が成り立たない）。
        //    軍事費が効く経路は2つだけで、どちらも**水準そのものではない**：
        //      (i) 兵数＝組織の大きさ（払うほど軍は大きく強い政治主体になる＝黙らせる向きではない・中南米の歳出68%国家の型）
        //      (ii) 現在値と目標の差＝**解隊の脅威**（ノスケ1920／アサーニャ1931）
        //    ⇒ 目標が現在値に追随している均衡では、水準をどこへ動かしても値は変わらない。
        //    ⚠水準は**段の上で**動かす（2026-09-20 段の点火＝R2）：軍事費の目標は4段（0.25/0.5/0.75/戦時1.0）で、平時の上限は段3（0.75）。
        //      旧の 0.95／0.05 は段の世界では「目標に置けない水準」＝現在値がそこに居ること自体が解隊の脅威（戦後の復員＝ノスケ1920）になり、
        //      この検査が測りたい「均衡では水準を読まない」から外れる。段の上の3点（0.25/0.5/0.75）で同じ主張を測る。
        void Level(double v) { w.SetMilitarySpending(v, 0.0); w.Policy.ArmyUpkeepTarget = v; w.Policy.NavyUpkeepTarget = 0.0; }
        Level(0.50); double mid = PoliticalQuantities.MilitaryPoliticalRatio(w);
        Level(0.75); Assert.Equal(mid, PoliticalQuantities.MilitaryPoliticalRatio(w), 12);
        Level(0.25); Assert.Equal(mid, PoliticalQuantities.MilitaryPoliticalRatio(w), 12);
        // ⇒ 一方、**目標だけを下げる（＝解隊を命じる）と跳ね上がる**＝「軍事費を削れば軍は安全」が塞がっている。
        Level(0.50); w.Policy.ArmyUpkeepTarget = 0.05;
        Assert.True(PoliticalQuantities.MilitaryPoliticalRatio(w) > mid, "解隊の脅威は軍の政治的優位を上げるべき");
        // ② 粛清・人事介入の記憶（文民が反対を押し切った履歴）は圧力を**上げる**＝アサーニャ1931型（満額俸給付き退役でも軍は怒る）。
        //    ⚠先に解隊の脅威を戻す（push は [0,1] クランプなので、解隊で飽和した状態では粛清の寄与が見えない）。
        //    ★書き直し（2026-09-21 ㉖＝N2/S1 の焼き込み）：S は門で b（≈0.2）の近傍に縛られる＝旧の 1.0 は世界では到達不能。
        //      主張は**単調性**（記憶が厚いほど圧力が高い）＝値を b の射程（実測の最大 0.2399＝計器 i3 seed1）へ書き直した。
        Level(0.50);
        w.MandateSpent = 0.0; double calm = PoliticalQuantities.MilitaryPoliticalRatio(w);
        w.MandateSpent = 0.2; Assert.True(PoliticalQuantities.MilitaryPoliticalRatio(w) > calm, "粛清・人事介入の記憶は軍の政治的優位を上げるべき");
        w.MandateSpent = 0.0;

        // ② 文民制度の弁が厚いほど軍の政治的優位は下がる（Finer/Huntington）
        w.ParliamentControl = 0.0; w.Suffrage = Suffrage.None; w.Bureaucracy = BureaucracyLaw.Spoils;
        double weakCivil = PoliticalQuantities.MilitaryPoliticalRatio(w);
        w.ParliamentControl = 1.0; w.Suffrage = Suffrage.Universal; w.Bureaucracy = BureaucracyLaw.Professional;
        double strongCivil = PoliticalQuantities.MilitaryPoliticalRatio(w);
        Assert.True(weakCivil > strongCivil, "文民制度が厚いほど軍の政治的優位は下がるべき");

        // ④ 任免権は符号を持つ：君主がいて議会が弱い＝＋（普の軍事内閣）／議会が強い＝−（英のシビリアンコントロール）
        w.Crown = CrownForm.Absolutist; w.ParliamentControl = 0.0;
        Assert.True(PoliticalQuantities.OfficerAppointmentCrownward(w) > 0);
        w.ParliamentControl = 1.0;
        Assert.True(PoliticalQuantities.OfficerAppointmentCrownward(w) < 0);
        w.Crown = CrownForm.None; w.ParliamentControl = 0.0;
        Assert.Equal(0.0, PoliticalQuantities.OfficerAppointmentCrownward(w));   // 王も議会もいない＝軍が自分の主
    }
}
