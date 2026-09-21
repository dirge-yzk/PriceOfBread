using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>権力の取り分＝**階級 × 機関の重み表**（docs/202608/20260818_権力の取り分を同一スケールへ_部分完了.md 第1段）の番人。
/// ★本段は**観測のみ**（どの挙動にも配線されていない）ので、ここで固定するのは「器の性質」＝
///   ①表の合計が厳密に1・全セル[0,1] ②取り分＝列の合計・貴族率＝行の比 ③退化状態で発散もNaNも出ない ④決定論・純関数。
/// ★★★そして**設計の芯**＝
///   ⑤**「王そのもの」の行が残る**（社会的支持が全部剥がれても執行権はゼロにならない＝1787-88仏の極限）
///   ⑥**階級の固定対応を書いていない**（執行権が特権に手を付ければ貴族は議会側／恩顧を供給すれば資本家は執行権側）
///   ⑦**2階級で閉じない**（1870年以降の議会は大衆の器＝北欧農民党・独中央党/社民党・英労働党）
///   ⑧**参政権拡大は単調でない**（保守的な新規有権者は執行権の基盤を増やす＝ビスマルク1867）
///   ⑨**軍部＝常在項＋漏れ**（執行権が強い国でも軍が消えない＝軍は執行権の道具であり道具が主人を食う）
///   ⑩**軍部は将校と兵卒に分かれる**（1スカラーだと打ち消し合って両方消える＝1917露）
///   ⑪**金だけでは軍を買えない**（正統性 × 実質＝1917は金庫に金があってもパンが無かった）。</summary>
public class PowerSharesTests
{
    private static double TableSum(double[][] t)
    {
        double s = 0;
        foreach (var row in t) foreach (double v in row) s += v;
        return s;
    }

    private static void AssertTableWellFormed(World w, string who)
    {
        var t = PowerShares.Table(w);
        Assert.Equal(PowerShares.ClassCount, t.Length);
        foreach (var row in t)
        {
            Assert.Equal(PowerShares.OrganCount, row.Length);
            foreach (double v in row)
            {
                Assert.False(double.IsNaN(v), $"{who}：表に NaN が出た");
                Assert.False(double.IsInfinity(v), $"{who}：表に無限大が出た");
                Assert.InRange(v, 0.0, 1.0);
            }
        }
        Assert.Equal(1.0, TableSum(t), 9);
        var s = PowerShares.Shares(w);
        Assert.Equal(PowerShares.HolderCount, s.Length);
        foreach (double v in s) { Assert.False(double.IsNaN(v), $"{who}：取り分に NaN"); Assert.InRange(v, 0.0, 1.0); }
        Assert.Equal(1.0, s[0] + s[1] + s[2] + s[3], 9);
    }

    private static Pop First(World w, Func<Pop, bool> pred)
    {
        foreach (var p in w.AllPops()) if (pred(p)) return p;
        throw new InvalidOperationException("該当する POP が見つからない＝テストの前提が崩れた");
    }

    private static void SetIdeology(Pop p, int index)
    {
        for (int i = 0; i < p.IdeologyShares.Length; i++) p.IdeologyShares[i] = 0;
        p.IdeologyShares[index] = 1.0;
    }

    [Fact]
    public void 全参照国_表の合計が1で全セルが0から1()
    {
        int n = 0; bool anyBaseline = false;
        foreach (var cfg in WorldData.Nations())
        {
            var w = new Simulation(cfg.ToScenario(7), new NullAgent()).World;
            AssertTableWellFormed(w, cfg.Name);
            Assert.InRange(PowerShares.Leak(w), 0.0, 1.0);
            Assert.InRange(PowerShares.ExecutiveWeight(w), 0.0, 1.0);
            Assert.InRange(PowerShares.PrivilegeIntact(w), 0.0, 1.0);
            Assert.InRange(PowerShares.StatePatronage(w), 0.0, 1.0);
            Assert.InRange(PowerShares.StateCapacity(w), 0.0, 1.0);
            Assert.InRange(PowerShares.VetoPower(w), 0.0, 1.0);
            if (PowerShares.ArmyBaseline(w) > 0) anyBaseline = true;
            n++;
        }
        Assert.True(n >= 50, $"参照国が少なすぎる（{n}）＝WorldData の読み込み前提が崩れた");
        Assert.True(anyBaseline, "どの国も軍部の常在項が0＝常備軍の対人口比が観測できていない");
    }

    [Fact]
    public void 取り分は表の列の合計であり_貴族率は行の比である()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(3), new NullAgent()).World;
        var t = PowerShares.Table(w);
        var s = PowerShares.Shares(w);
        double exec = 0, parl = 0, army = 0, rev = 0;
        for (int c = 0; c < PowerShares.ClassCount; c++)
        {
            exec += t[c][(int)PowerShares.Organ.Executive];
            parl += t[c][(int)PowerShares.Organ.Parliament];
            army += t[c][(int)PowerShares.Organ.Officers] + t[c][(int)PowerShares.Organ.Ranks];
            rev += t[c][(int)PowerShares.Organ.Revolution];
        }
        Assert.Equal(exec, s[(int)PowerShares.Holder.Executive], 9);
        Assert.Equal(parl, s[(int)PowerShares.Holder.Parliament], 9);
        Assert.Equal(army, s[(int)PowerShares.Holder.Army], 9);
        Assert.Equal(rev, s[(int)PowerShares.Holder.Revolution], 9);

        // 貴族率＝行の比（列の合計が0なら算出不能＝NaN。勝手に0で埋めない）
        double pn = PowerShares.ParliamentNobility(w);
        if (parl > 1e-12) Assert.Equal(t[(int)PowerShares.Cls.OldRuling][(int)PowerShares.Organ.Parliament] / parl, pn, 9);
        else Assert.True(double.IsNaN(pn));
    }

    [Fact]
    public void 王そのものの行が残る_社会的支持が全部剥がれても執行権はゼロにならない()
    {
        // ★1787-88のフランスの極限＝「王そのもの」だけが残り、社会の重みが執行権から離れた状態。
        var w = new Simulation(NationConfig.France().ToScenario(1), new NullAgent()).World;
        foreach (var p in w.AllPops()) { p.Savings = 0; p.LandRent = 0; }   // 誰も資産で支えない
        w.ReformistPower = 0; w.RadicalPower = 0;
        AssertTableWellFormed(w, "支持ゼロ");
        var t = PowerShares.Table(w);
        var s = PowerShares.Shares(w);
        Assert.True(s[(int)PowerShares.Holder.Executive] > 0, "支持が剥がれても執行権はゼロにならない");
        Assert.True(t[(int)PowerShares.Cls.Sovereign][(int)PowerShares.Organ.Executive] > 0, "残っているのは「王そのもの」の行である");
        Assert.Equal(0.0, s[(int)PowerShares.Holder.Parliament], 9);   // 支え手のいない議会は取り分を持たない
    }

    [Fact]
    public void 貴族の陣営は内生_執行権が特権に手を付けると議会側へ反転する()
    {
        // ★史学反証：1787-88仏の名士会・高等法院／ヨーゼフ2世／シュタイン改革／1861露農奴解放／1871-76明治の廃藩置県・秩禄処分。
        var w = new Simulation(NationConfig.France().ToScenario(1), new NullAgent()).World;
        var noble = First(w, p => p.Closed);
        SetIdeology(noble, 0);   // 思想は伝統に固定＝差が「特権」だけから出ることを保証

        w.SerfEmancipation = ReformLevel.None; w.LandTax = 0; w.ProgressiveTax = 0; w.Bureaucracy = BureaucracyLaw.Spoils;
        Assert.Equal(1.0, PowerShares.PrivilegeIntact(w), 9);
        double kept = PowerShares.PopExecutiveLean(noble, w);
        Assert.Equal(1.0, kept, 9);

        w.SerfEmancipation = ReformLevel.Full; w.LandTax = 0.6; w.ProgressiveTax = 0.4; w.Bureaucracy = BureaucracyLaw.Professional;
        Assert.Equal(0.0, PowerShares.PrivilegeIntact(w), 9);
        double stripped = PowerShares.PopExecutiveLean(noble, w);
        Assert.True(stripped < kept, "特権に手を付ければ貴族は議会/反乱側へ回る");
        Assert.Equal(0.5, stripped, 9);   // 思想1.0 と 特権0.0 の平均

        w.SerfEmancipation = ReformLevel.Limited; w.LandTax = 0.25; w.ProgressiveTax = 0.25; w.Bureaucracy = BureaucracyLaw.MeritExam;
        Assert.InRange(PowerShares.PopExecutiveLean(noble, w), stripped, kept);   // 中間段も連続に効く
    }

    [Fact]
    public void 資本家の陣営も内生_国家が関税と発注と秩序を供給すれば執行権側へ回る()
    {
        // ★史学反証：第二帝政のクレディ・モビリエ／1879独「鉄とライ麦」／明治の政商／露十月党。
        var w = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent()).World;
        var cap = First(w, p => p.IsModernElite);
        SetIdeology(cap, 2);   // 思想は自由に固定＝差が「恩顧」だけから出ることを保証

        w.Tariff = 0; w.ArmyUpkeep = 0; w.NavyUpkeep = 0; w.PublicOrder = 0;
        Assert.Equal(0.0, PowerShares.StatePatronage(w), 9);
        Assert.Equal(0.0, PowerShares.PopExecutiveLean(cap, w), 9);

        w.Tariff = 1.0; w.ArmyUpkeep = 1.0; w.NavyUpkeep = 0; w.PublicOrder = 1.0;
        Assert.Equal(1.0, PowerShares.StatePatronage(w), 9);
        Assert.Equal(0.5, PowerShares.PopExecutiveLean(cap, w), 9);   // 思想0.0 と 恩顧1.0 の平均
    }

    [Fact]
    public void 議会は近代エリートの器とは限らない_大衆の行が立つ()
    {
        // ★史学反証：1870年以降の議会は近代エリートの器ではない（スウェーデン農民身分・デンマーク/ノルウェー農民党・
        //   独中央党/社民党・英労働党/アイルランド党・仏急進社会党・ベルギーカトリック党＝この帯では多数派）。
        //   ⚠2階級（貴族／近代エリート）で閉じると期間の3分の1で系統的に誤る。
        var c = new Coefficients { PolityLabelFromAxes = 1.0 };
        var w = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent()).World;
        CrownParliament.ApplyRegime(w, c, Polity.Republic);              // 統制力1.0
        w.Suffrage = Suffrage.Universal;                                 // 普通選挙＝大衆に発言権
        w.ReformistPower = 0; w.RadicalPower = 0;
        w.TaxRate = 1.0; w.ProgressiveTax = 0; w.PollTax = 0; w.LandTax = 0; w.SetStateMonopolyTier(0);   // 年次議決の歳入だけ＝掌握を立てる（専売は材を1つも選ばない＝対象0・2026-09-20）
        // 旧支配層と資本家は**完全に執行権側**へ固定する（特権も恩顧も満額）＝議会の列に残るのは大衆だけになる
        w.SerfEmancipation = ReformLevel.None; w.Bureaucracy = BureaucracyLaw.Spoils;   // 特権保全＝1
        w.Tariff = 1.0; w.ArmyUpkeep = 1.0; w.NavyUpkeep = 0; w.PublicOrder = 1.0;      // 恩顧＝1
        Assert.Equal(1.0, PowerShares.PrivilegeIntact(w), 9);
        Assert.Equal(1.0, PowerShares.StatePatronage(w), 9);
        foreach (var p in w.AllPops())
        {
            if (p.Closed || p.IsModernElite) { SetIdeology(p, 0); continue; }   // 旧支配層と資本家は保守＝執行権側
            p.Savings = 1000;                                            // 資産を持たせて「資産×発言権」に乗せる
            SetIdeology(p, 5);                                           // 社会族＝議会側
        }
        var t = PowerShares.Table(w);
        Assert.True(t[(int)PowerShares.Cls.Mass][(int)PowerShares.Organ.Parliament] > 0, "大衆が議会を支えている行が立たない＝2階級で閉じている");
        double massRatio = PowerShares.ClassRatio(w, PowerShares.Organ.Parliament, PowerShares.Cls.Mass);
        Assert.InRange(massRatio, 0.0, 1.0);
        Assert.True(massRatio > PowerShares.ClassRatio(w, PowerShares.Organ.Parliament, PowerShares.Cls.OldRuling),
            "普通選挙×社会族なら議会の大衆率が貴族率を上回る");
    }

    [Fact]
    public void 参政権の拡大は単調でない_保守的な新規有権者は執行権の基盤を増やす()
    {
        // ★史学反証：1867ビスマルクの北ドイツ連邦（農村保守票で自由主義議会を無力化）・ナポレオン3世の人民投票・1907墺。
        var w = new Simulation(NationConfig.Prussia().ToScenario(1), new NullAgent()).World;
        foreach (var p in w.AllPops())
        {
            if (p.Closed || p.IsModernElite) { SetIdeology(p, 2); continue; }   // 既存の有権者＝自由派
            SetIdeology(p, 0);                                                   // 新規に入る大衆＝保守（農村票）
            p.Savings = 1000;
        }
        double ExecBase()
        {
            var sc = PowerShares.ClassScan(w);
            double e = 0;
            for (int i = 0; i < PowerShares.ClassCount; i++) e += sc.Exec[i];
            return e;
        }
        w.Suffrage = Suffrage.Census;
        double before = ExecBase();
        w.Suffrage = Suffrage.AdultMale;
        double after = ExecBase();
        Assert.True(after > before, $"保守的な新規有権者を入れたのに執行権の基盤が増えない（{before:F3}→{after:F3}）＝単調性の罠");

        foreach (var p in w.AllPops()) if (!p.Closed && !p.IsModernElite) SetIdeology(p, 2);   // 新規有権者が自由派なら
        w.Suffrage = Suffrage.Census;
        double before2 = ExecBase();
        w.Suffrage = Suffrage.AdultMale;
        Assert.True(ExecBase() < before2, "自由派の新規有権者なら執行権の基盤は減る＝拡大の効果は構成で決まる");
    }

    [Fact]
    public void 軍部は常在項プラス漏れ_執行権が強い国でも軍は消えない()
    {
        // ★史学検証2：露の近衛が皇帝を殺す(1801)／独の軍事内閣・最高陸軍指揮部(1916-18)／1930年代日本＝
        //   執行権が最強のときに軍の取り分も最大だった。「1−執行権−議会」だけだと符号が逆になる。
        var c = new Coefficients { PolityLabelFromAxes = 1.0 };
        var w = new Simulation(NationConfig.Prussia().ToScenario(1), new NullAgent()).World;
        CrownParliament.ApplyRegime(w, c, Polity.Absolutist);   // 統制力0＝執行権が最強
        w.ReformistPower = 0; w.RadicalPower = 0;               // 革命の口を閉じる
        w.ArmyUpkeep = 0.3; w.NavyUpkeep = 0;
        w.FiscalFundingRatio = 1.0; w.MilitaryReadiness = 1.0;
        double baseline = PowerShares.ArmyBaseline(w);
        var s = PowerShares.Shares(w);
        Assert.True(s[(int)PowerShares.Holder.Army] >= baseline - 1e-9, "軍部の取り分が常在項を下回る＝常在項が効いていない");
        Assert.Equal(1.0, s[0] + s[1] + s[2] + s[3], 9);
    }

    [Fact]
    public void 将校と兵卒は別の入力から動く_兵卒は物質_将校は制度()
    {
        // ★史学検証3：**兵卒・下士官の反乱はほぼ例外なく物質的苦情を伴う**（ラ・グランハ1836の給与3か月未払／
        //   ブラジル1910シバータ／チリ1931コキンボの30%減俸／キューバ1933／エルサルバドル1931）。
        //   **対して将校の反乱は賃金では動かない**（伯1883-87の年金拠出義務化と文民大臣への従属／西1923ピカソ調書／ボリビア1934）。
        //   ⚠同じ「軍事費」から両方を引くとこの分岐が消える。
        var w = new Simulation(NationConfig.Russia().ToScenario(2), new NullAgent()).World;
        w.ArmyUpkeep = 0.4; w.NavyUpkeep = 0;
        w.FiscalFundingRatio = 1.0; w.MilitaryReadiness = 1.0;
        w.ReformistPower = 0; w.RadicalPower = 0;
        w.ArmyAllegiance = 1.0; w.Legitimacy = 1.0;
        foreach (var p in w.AllPops()) if (p.IsSoldier) p.Satisfaction = 1.0;

        // ① 物質（給与・満足）を落とす＝**兵卒の漏れ**だけが増え、将校の漏れは動かない
        double ranks0 = PowerShares.RanksLeak(w), officers0 = PowerShares.OfficerLeak(w);
        foreach (var p in w.AllPops()) if (p.IsSoldier) p.Satisfaction = 0;   // 給与3か月未払・給食の悪化
        if (PowerShares.SoldierWelfare(w) < 1.0)
        {
            Assert.True(PowerShares.RanksLeak(w) > ranks0, "物質的苦情で兵卒の漏れが増えない");
            Assert.Equal(officers0, PowerShares.OfficerLeak(w), 12);   // 将校は賃金では動かない
        }

        // ② 制度（文民統制・身分の剥奪）を動かす＝**将校の漏れ**だけが増え、兵卒の漏れは動かない
        foreach (var p in w.AllPops()) if (p.IsSoldier) p.Satisfaction = 1.0;   // 実質は満額＝兵卒の物質チャネルを閉じる
        w.ParliamentControl = 0; w.Bureaucracy = BureaucracyLaw.Spoils;
        w.SerfEmancipation = ReformLevel.None; w.LandTax = 0; w.ProgressiveTax = 0;
        double grievanceA = PowerShares.OfficerGrievance(w);
        Assert.Equal(0.0, PowerShares.RanksLeak(w), 12);                // 実質満額＝兵卒の漏れは制度をどう動かしても0
        w.ParliamentControl = 1.0;                                      // 文民大臣への従属＝自律の喪失
        w.SerfEmancipation = ReformLevel.Full; w.LandTax = 1.0; w.ProgressiveTax = 1.0;   // 身分の剥奪
        Assert.True(PowerShares.OfficerGrievance(w) > grievanceA, "制度を動かしても将校の不満が増えない");
        Assert.Equal(0.0, PowerShares.RanksLeak(w), 12);                // ⚠兵卒は制度では動かない（物質だけ）
    }

    [Fact]
    public void 将校団の貴族率は資格任用で下がるが徴兵法では下がらない()
    {
        // ⚠プロイセン（皆兵＋ユンカー将校団＋当時最も近代的な軍）が矛盾なく出るように、
        //   徴兵法は将校団の開放度に**入れない**。⚠軍事的な有効性はここから一切下げない。
        var w = new Simulation(NationConfig.Prussia().ToScenario(2), new NullAgent()).World;
        w.Bureaucracy = BureaucracyLaw.Spoils;
        double closed = PowerShares.ArmyNobility(w);
        w.ConscriptionLaw = ConscriptionLaw.Universal;
        Assert.Equal(closed, PowerShares.ArmyNobility(w), 12);         // 皆兵にしても将校団の性格は変わらない
        w.Bureaucracy = BureaucracyLaw.Professional;
        Assert.True(PowerShares.ArmyNobility(w) <= closed, "資格任用が開いても将校団の貴族率が下がらない");
    }

    [Fact]
    public void 軍部の常在項は国別の定数でなく文民制度の厚みで決まる()
    {
        // ★史学検証3の修正④：純軍政の割合は普遍でなく文民制度の厚みで決まる（アンデス三国約70% vs スペイン14%・ブラジル8%）。
        //   ⚠国別の定数を1つも置かない＝地域差は**既存の制度の値の差**として出る。
        var w = new Simulation(NationConfig.Prussia().ToScenario(2), new NullAgent()).World;
        w.ParliamentControl = 0; w.Bureaucracy = BureaucracyLaw.Spoils; w.Suffrage = Suffrage.None; w.Legitimacy = 0;
        double thin = PowerShares.CivilianInstitutions(w);
        double baselineThin = PowerShares.ArmyBaseline(w);
        w.ParliamentControl = 1.0; w.Bureaucracy = BureaucracyLaw.Professional; w.Suffrage = Suffrage.Universal; w.Legitimacy = 1.0;
        w.TaxRate = 1.0; w.ProgressiveTax = 0; w.PollTax = 0; w.LandTax = 0; w.SetStateMonopolyTier(0); w.Tariff = 0;   // 歳入の掌握＝1（専売は対象0・2026-09-20）
        // ⚠PolityLeverRanges は撤去済み（2026-08-19）＝政体由来の床が無くなったので、年次審議の外の歳出は既定で0。
        double thick = PowerShares.CivilianInstitutions(w);
        double baselineThick = PowerShares.ArmyBaseline(w);
        Assert.True(thick > thin, "議会・官僚・政党の3本を厚くしても文民制度の厚みが増えない");
        Assert.True(baselineThick <= baselineThin, "文民制度が厚くなっても軍部の常在項が減らない");
        Assert.InRange(thick, 0.0, 1.0);
        Assert.InRange(thin, 0.0, 1.0);
        Assert.True(thick > 0.9, $"4本とも満点に近いはず（{thick:F3}）");   // 硬性条件の国が構造的に満たす向き
        Assert.True(baselineThick < 0.02, $"厚い文民制度の下では常在項がほぼ消えるはず（{baselineThick:F4}）");
    }

    [Fact]
    public void 金だけでは軍を買えない_正統性と実質のどちらが消えても束縛はゼロになる()
    {
        // ★1917ペトログラード守備隊：給与は払われていたが皇帝を見捨てた（金庫に金があってもパンが無い）。
        var w = new Simulation(NationConfig.Russia().ToScenario(2), new NullAgent()).World;
        w.ArmyUpkeep = 0.4; w.NavyUpkeep = 0;
        w.FiscalFundingRatio = 1.0; w.MilitaryReadiness = 1.0;
        w.ReformistPower = 0; w.RadicalPower = 0;
        w.ArmyAllegiance = 1.0; w.Legitimacy = 1.0;
        double bound = PowerShares.ArmyBinding(w);
        Assert.True(bound > 0);

        w.Legitimacy = 0.0;                                   // 正統性の崩壊＝命令を出す主権者の名分が消える
        Assert.Equal(0.0, PowerShares.ArmyBinding(w), 9);
        // ⚠2026-08-18：**「金＝帰属」は取り下げた**（同一の給与で3週間後に符号が反転＝ライヒスヴェーア1920年3月と4月）。
        //   帰属は 人事系譜 × 主権者の正統性 だけで決まり、給与は**兵卒の物質的苦情**の経路にしか残っていない。
        w.Legitimacy = 1.0;
        double ranksFed = PowerShares.RanksLeak(w);
        foreach (var p in w.AllPops()) if (p.IsSoldier) p.Satisfaction = 0;   // 実質＝パンが無い（1917）
        if (PowerShares.SoldierWelfare(w) < 1.0)
        {
            Assert.True(PowerShares.RanksLeak(w) > ranksFed, "兵卒の実質が落ちても兵卒の漏れが増えない");
            Assert.Equal(bound, PowerShares.ArmyBinding(w), 12);   // ⚠帰属は給与では動かない
        }
    }

    [Fact]
    public void 軍事独裁は相対条件_組む相手がいないときだけ単独支配を選ぶ()
    {
        var w = new Simulation(NationConfig.France().ToScenario(6), new NullAgent()).World;
        w.ReformistPower = 0; w.RadicalPower = 0;
        w.FiscalFundingRatio = 1.0; w.MilitaryReadiness = 1.0;
        w.ArmyAllegiance = 0; w.Legitimacy = 0.5;
        w.ArmyUpkeep = 0.1; w.NavyUpkeep = 0;
        var s = PowerShares.Shares(w);
        double other = Math.Max(s[0], Math.Max(s[1], s[3]));
        // 相手が成立している（≥0.5）なら、軍が最大でも単独支配は選ばない
        if (other >= PowerShares.PartnerViableShare) Assert.False(PowerShares.SeeksMilitaryRule(w));
        // 軍が最大でどの相手も成立しない状態を作る＝単独支配
        w.ArmyUpkeep = 0.9; w.NavyUpkeep = 0;
        var s2 = PowerShares.Shares(w);
        double other2 = Math.Max(s2[0], Math.Max(s2[1], s2[3]));
        if (s2[(int)PowerShares.Holder.Army] > other2 && other2 < PowerShares.PartnerViableShare)
        {
            Assert.True(PowerShares.SeeksMilitaryRule(w));
            Assert.Equal(PowerShares.Holder.Army, PowerShares.Ally(w));
        }
        Assert.Equal(1.0, s2[0] + s2[1] + s2[2] + s2[3], 9);
    }

    [Fact]
    public void 退化世界_発散せずNaNも無限大も出ない()
    {
        var w = new Simulation(new Scenario { Seed = 1 }, new NullAgent()).World;   // 最小世界＝兵士も資産も発言権も無い
        AssertTableWellFormed(w, "退化");
        Assert.Equal(0.0, PowerShares.Leak(w), 9);

        var w2 = new Simulation(NationConfig.France().ToScenario(5), new NullAgent()).World;
        w2.Legitimacy = 0;
        AssertTableWellFormed(w2, "正統性0");

        // NaN の注入（Math.Clamp は NaN を除去しない＝否定形ガードの番人）
        w2.Legitimacy = double.NaN;
        w2.ArmyAllegiance = double.NaN;
        w2.ParliamentControl = double.NaN;
        w2.FiscalFundingRatio = double.NaN;
        w2.MilitaryReadiness = double.NaN;
        w2.ArmyUpkeep = double.NaN;
        w2.Coercion = double.NaN;
        w2.Tariff = double.NaN;
        w2.PublicOrder = double.NaN;
        w2.LandTax = double.NaN;
        w2.ProgressiveTax = double.NaN;
        foreach (var p in w2.AllPops()) { p.Savings = double.NaN; p.LandRent = double.NaN; p.Satisfaction = double.NaN; }
        AssertTableWellFormed(w2, "NaN");
        Assert.False(double.IsNaN(PowerShares.ExecutiveWeight(w2)));
        Assert.False(double.IsNaN(PowerShares.Leak(w2)));
        Assert.False(double.IsNaN(PowerShares.PrivilegeIntact(w2)));
        Assert.False(double.IsNaN(PowerShares.StatePatronage(w2)));
        Assert.False(double.IsNaN(PowerShares.StateCapacity(w2)));
        Assert.False(double.IsNaN(PowerShares.VetoPower(w2)));

        w2.Legitimacy = double.PositiveInfinity;
        w2.ArmyAllegiance = double.NegativeInfinity;
        w2.ParliamentControl = double.PositiveInfinity;
        w2.ArmyUpkeep = double.PositiveInfinity;
        foreach (var p in w2.AllPops()) { p.Savings = double.PositiveInfinity; p.LandRent = double.PositiveInfinity; }
        AssertTableWellFormed(w2, "∞");
    }

    [Fact]
    public void 統制力が上がるほど執行権の取り分は下がる()
    {
        var c = new Coefficients { PolityLabelFromAxes = 1.0 };
        var abs = new Simulation(NationConfig.France().ToScenario(1), new NullAgent()).World;
        CrownParliament.ApplyRegime(abs, c, Polity.Absolutist);        // 統制力0
        var rep = new Simulation(NationConfig.France().ToScenario(1), new NullAgent()).World;
        CrownParliament.ApplyRegime(rep, c, Polity.Republic);          // 統制力1.0
        Assert.Equal(1.0, PowerShares.ExecutiveWeight(abs), 9);        // 統制ゼロの議会は基盤を権力に変換できない
        Assert.True(PowerShares.Shares(abs)[(int)PowerShares.Holder.Executive] > PowerShares.Shares(rep)[(int)PowerShares.Holder.Executive]);
        Assert.True(PowerShares.Shares(rep)[(int)PowerShares.Holder.Parliament] > PowerShares.Shares(abs)[(int)PowerShares.Holder.Parliament]);

        // 反転の制御実験＝自由派の基盤 × 高い統制力 × 財政破綻＝議会が握る（1787仏の逆向き）
        foreach (var p in rep.AllPops()) SetIdeology(p, 2);
        rep.SerfEmancipation = ReformLevel.Full; rep.Bureaucracy = BureaucracyLaw.Professional;
        // ★歳入の掌握＝年次議決に載る税（所得税・累進）だけにする＝第109条の逃げ道を塞いだ状態
        rep.TaxRate = 1.0; rep.ProgressiveTax = 1.0; rep.PollTax = 0; rep.LandTax = 0; rep.SetStateMonopolyTier(0);
        rep.Tariff = 0; rep.ArmyUpkeep = 0; rep.NavyUpkeep = 0; rep.PublicOrder = 0;
        rep.FiscalFundingRatio = 0; rep.ReformistPower = 0; rep.RadicalPower = 0;
        Assert.Equal(1.0, PowerShares.ParliamentGrip(rep), 9);
        var r = PowerShares.Shares(rep);
        AssertTableWellFormed(rep, "自由派共和");
        Assert.True(r[(int)PowerShares.Holder.Parliament] > r[(int)PowerShares.Holder.Executive]);
    }

    [Fact]
    public void 防衛反応型の第2の引き金_取り分の削減率()
    {
        // 1936スペイン・1973チリ・1961トルコ＝「一方が強すぎて軍自身が脅かされた」型。
        Assert.Equal(0.0, PowerShares.CutSeverity(0.40, 0.40), 12);
        Assert.Equal(0.5, PowerShares.CutSeverity(0.40, 0.20), 12);
        Assert.Equal(1.0, PowerShares.CutSeverity(0.40, 0.00), 12);
        Assert.Equal(0.0, PowerShares.CutSeverity(0.20, 0.40), 12);   // 増えた＝脅威ではない
        Assert.Equal(0.0, PowerShares.CutSeverity(0.00, 0.00), 12);   // 前が0＝削られようがない（倒し先）
        Assert.Equal(0.0, PowerShares.CutSeverity(double.NaN, 0.10), 12);
        Assert.Equal(0.0, PowerShares.CutSeverity(0.40, double.NaN), 12);
        Assert.InRange(PowerShares.CutSeverity(0.40, -1.0), 0.0, 1.0);
    }

    [Fact]
    public void 決定論_同じ状態から2回呼ぶとビット一致し_Worldを1ビットも変えない()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(11), new NullAgent()).World;
        double leg = w.Legitimacy, pc = w.ParliamentControl, aa = w.ArmyAllegiance;
        double au = w.ArmyUpkeep, nu = w.NavyUpkeep, ffr = w.FiscalFundingRatio, mr = w.MilitaryReadiness;
        double rp = w.ReformistPower, rd = w.RadicalPower, co = w.Coercion, tar = w.Tariff, ord = w.PublicOrder;
        var polity = w.Polity; var suffrage = w.Suffrage; var serf = w.SerfEmancipation; var consc = w.ConscriptionLaw;

        var t1 = PowerShares.Table(w); var ally1 = PowerShares.Ally(w);
        var t2 = PowerShares.Table(w); var ally2 = PowerShares.Ally(w);
        for (int c = 0; c < PowerShares.ClassCount; c++)
            for (int o = 0; o < PowerShares.OrganCount; o++)
                Assert.Equal(t1[c][o], t2[c][o]);   // 精度指定なし＝厳密一致
        Assert.Equal(ally1, ally2);

        // 純関数＝観測は世界を書き換えない（第1段の遵守事項＝挙動非接触）
        Assert.Equal(leg, w.Legitimacy);
        Assert.Equal(pc, w.ParliamentControl);
        Assert.Equal(aa, w.ArmyAllegiance);
        Assert.Equal(au, w.ArmyUpkeep);
        Assert.Equal(nu, w.NavyUpkeep);
        Assert.Equal(ffr, w.FiscalFundingRatio);
        Assert.Equal(mr, w.MilitaryReadiness);
        Assert.Equal(rp, w.ReformistPower);
        Assert.Equal(rd, w.RadicalPower);
        Assert.Equal(co, w.Coercion);
        Assert.Equal(tar, w.Tariff);
        Assert.Equal(ord, w.PublicOrder);
        Assert.Equal(polity, w.Polity);
        Assert.Equal(suffrage, w.Suffrage);
        Assert.Equal(serf, w.SerfEmancipation);
        Assert.Equal(consc, w.ConscriptionLaw);
    }
}
