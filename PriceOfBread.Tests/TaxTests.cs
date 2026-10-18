using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 税制の拡張（土地税は検地台帳でアンロック）＋専売。
/// 人頭税＝行政不要・逆進的（閉鎖階級は免除）／土地税＝検地台帳（行政力で解禁）／
/// 所得税・累進＝率は自由に課せるが**取り立ては行政力に比例して漏れる**（2026-09-12 に天井とラッチを廃止）／
/// 専売＝奢侈品の購入に上乗せ（不完全な金持ち課税）。
/// </summary>
public class TaxTests
{
    [Fact]
    public void 土地税は最初から押せて立つ()
    {
        // ★(383) 作者 2026-10-16「行政効率→廃止」：旧は行政力の移動平均が閾値を越えて検地台帳の錠が開くまで土地税の最上段が押せなかった（貴族の統治の素地で開く）。
        //   錠ごと物理削除した＝土地税は最初から開いている。行政費ゼロでも土地税（粗い検地＝年貢/タイユ）が立つ＝序盤の財政を支える（ユーザー要望）。
        // ※治安維持力は低く固定（人口比例の維持費で国庫を食わせない）。
        var sim = new Simulation(NationConfig.Japan().ToScenario(11), new NullAgent());
        sim.World.PoliticalLockEnabled = false;   // 税の解禁・歳入機構を検証＝政治ロック（地主の土地税反対）とは直交なので隔離
        sim.World.BaseCoercion = 100; sim.World.Policy.CoercionTarget = 100;
        sim.World.Policy.AdminSpendingTarget = 0.0;   // 官僚への投資ゼロでも…
        sim.World.Policy.LandTaxTarget = 1.0;
        Assert.True(Government.LawPrereq(sim.World, PolicyAxis.LandTax, 1, sim.World.Coef).Ok, "開幕から土地税の段 1 が押せない");   // 錠は無い＝開幕で既に開いている
        sim.Run(120);
        // ★主張の書き直し（2026-09-20 段の点火）：土地税は段の法＝目標1.0は最上段（錨 Government.LandTaxTop＝0.4）へ落ちる。
        //   「解禁」は値を0へ書き潰す三項ではなく**最上段が押せる**こと＝その門と、率が錨の半分を越えて登ることを見る。
        Assert.True(Government.LawPrereq(sim.World, PolicyAxis.LandTax, Government.TaxLawStages, sim.World.Coef).Ok, "解禁後も土地税の最上段が押せない");
        Assert.True(sim.World.LandTax > 0.5 * Government.LandTaxTop, $"解禁後に土地税が動いていない（{sim.World.LandTax:F3}）");
    }

    [Fact]
    public void 専売と人頭税は国庫を富ませ保存則を保つ()
    {
        var baseline = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        var taxed = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        baseline.World.GovDebt = taxed.World.GovDebt = new MoneyStock(0);   // P5：仏は重債務開始＝利払いが両系の国庫を0まで枯渇させ税収差が埋もれる（課税0 vs 基準0）ため中立財政で税機構を隔離
        foreach (var w0 in new[] { baseline.World, taxed.World }) foreach (var rg in w0.Regions) foreach (var pp in rg.Pops) pp.BondHoldings = new MoneyStock(0);   // ★2026-09-21：国債を手で消すなら持ち手の記録（保有台帳）も消す＝Σ保有＝国内元本の不変条件
        taxed.World.PoliticalLockEnabled = false;   // 税の歳入・保存機構を検証＝政治ロック（大衆の人頭税反対等）とは直交なので隔離
        // ★主張の書き直し（作者裁定2026-09-20＝率の廃止）：腕は率でなく**対象の広さ**（段2＝史実の物品税の財＋奢侈品・2026-09-30 (127) の梯子）。率はエンジンの標準率1本。
        taxed.World.SetStateMonopolyTier(2);
        taxed.World.Policy.PollTaxTarget = 0.5;
        baseline.Run(80);
        var ex = Record.Exception(() => taxed.Run(80));   // 新税の金流（pop→国庫）が保存則（§11.6）を破らない
        Assert.Null(ex);
        Assert.True(taxed.World.Treasury.Value > baseline.World.Treasury.Value,
            $"課税しても国庫が増えない（課税{taxed.World.Treasury:F0} vs 基準{baseline.World.Treasury:F0}）");
    }

    /// <summary>人頭税＝頭数に一律（行政不要・逆進的）・閉鎖階級（貴族・聖職）は免除・兵士は国が雇い主で対象外。
    /// ★2026-09-30（段4d・(122) の申し送り4＝番人の作り直し）：**1人あたりの額**で測る。旧＝24四半期の貯蓄の目減りの比（農民 vs 貴族）＝貴族は1人あたりの
    ///   貯蓄が大きく頭数の税の目減りの比が小さい＝貴族も払う突然変異（M1）でも緑だった（免除の有無を見分けなかった）。
    /// 盤面＝仏を1四半期温めた双子（人頭税 0 と最上段）・他の税と十分の一税と汚職の漏れと維持費と福祉と国債を止めて、月次の財政を1回＝双子の貯蓄の差が人頭税そのもの。
    /// ⚠突然変異「閉鎖階級も払う」「兵士も払う」「人頭税を取らない」で赤。</summary>
    [Fact]
    public void 人頭税は閉鎖階級を免除し大衆に重い()   // 特権＝貴族・聖職者は払わない（逆進性の設計）
    {
        World Board(double poll)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
            sim.Run(1);   // 前の月の所得（LastEarnedPerMonth）を立てる＝双子は同じ種で同じ軌道
            var w = sim.World;
            w.TaxRate = new Share(0); w.LandTax = 0; w.ProgressiveTax = 0; w.PollTax = poll;
            w.Coef.TitheRate = new Share(0); foreach (var rg0 in w.Regions) rg0.Rule = [1.0, 0.0, 0.0, 0.0];   // 国家が全域を治める（支配度の区分で目減りしない）
            w.SocialSecurity = 0; w.Pension = ReformLevel.None;
            w.GovDebt = new MoneyStock(0); w.ForeignDebt = new MoneyStock(0);
            foreach (var rg in w.Regions) foreach (var pp in rg.Pops) { pp.BondHoldings = new MoneyStock(0); pp.SetSavings(System.Math.Max(pp.Savings.Value, 0) + 1e3 * pp.Size); }   // 全員が払える＝率 r_p は1
            w.FiscalFundingRatio = new Share(1.0);
            Fiscal.Step(w, w.Coef);
            return w;
        }
        var none = Board(0);
        var taxed = Board(Government.PollTaxTop);
        double perHead = taxed.Coef.PollTaxRate * Government.PollTaxTop;
        Assert.True(perHead > 0, "前提が崩れた：人頭税の1人あたりの額が0");
        var np = new System.Collections.Generic.List<Pop>(none.AllPops()); var tp = new System.Collections.Generic.List<Pop>(taxed.AllPops());
        Assert.Equal(np.Count, tp.Count);
        double massPaid = 0, massIncome = 0, massSize = 0, elitePaid = 0, eliteIncome = 0, eliteSize = 0;
        int closed = 0, soldiers = 0, masses = 0;
        for (int i = 0; i < tp.Count; i++)
        {
            var p = tp[i];
            if (!(p.Size > 1)) continue;
            double paidPerHead = (np[i].Savings.Value - p.Savings.Value) / p.Size;
            if (p.Closed) { closed++; Assert.True(System.Math.Abs(paidPerHead) <= 1e-9 * perHead, $"免除のはずの閉鎖階級（{p.ClassName}）が1人あたり {paidPerHead} 払った"); continue; }
            if (p.IsSoldier) { soldiers++; Assert.True(System.Math.Abs(paidPerHead) <= 1e-9 * perHead, $"対象外のはずの兵士が1人あたり {paidPerHead} 払った"); continue; }
            Assert.True(System.Math.Abs(paidPerHead - perHead) <= 1e-9 * perHead, $"{p.ClassName} の1人あたりの人頭税 {paidPerHead} が一律の額 {perHead} でない");
            if (p.IsModernElite) { elitePaid += paidPerHead * p.Size; eliteIncome += System.Math.Max(0, p.LastEarnedPerMonth.Value); eliteSize += p.Size; }
            else { masses++; massPaid += paidPerHead * p.Size; massIncome += System.Math.Max(0, p.LastEarnedPerMonth.Value); massSize += p.Size; }
        }
        Assert.True(closed > 0 && masses > 0, $"前提が崩れた：閉鎖階級 {closed}・大衆 {masses}");
        // 逆進性＝同じ額が所得の低い大衆の所得に対して重い（負担率＝払った額÷前の月の所得）
        Assert.True(massIncome > 0 && eliteIncome > 0, "前提が崩れた：前の月の所得が立っていない");
        double massRate = massPaid / massIncome, eliteRate = elitePaid / eliteIncome;
        Assert.True(massRate > eliteRate, $"人頭税が大衆に重くない（大衆の負担率 {massRate:P2}・近代エリート {eliteRate:P2}）");
    }

    [Fact]
    public void 商人は流通マージンで稼ぐ()   // 消費者の購入に小売の取り分が乗り、商業の規模＝商人の収入になる
    {
        var sim = new Simulation(NationConfig.Japan().ToScenario(31), new NullAgent());   // 商の国（商人800）
        sim.Run(40);
        var merchant = sim.World.Region.Pops.First(p => p.ClassId == 10);
        Assert.True(merchant.EarnedPerMonth.Value > 0, "商人が流通マージンを得ていない");
        var ex = Record.Exception(() => sim.Run(40));   // 消費者→商人の金流が保存則を破らない
        Assert.Null(ex);
    }

    [Fact]
    public void 農制が地代の行き先を決める()   // 農奴制＝農場の残余は地主（貴族）へ／自作農＝農民が自分の農場の地代を受け取る
    {
        // B-中1（史実検証2026-07-22）の「米 farmLordShare 0→0.35」は2026-07-23に試験適用→**取り下げ**（このテストの米 Tech<1.5 失敗＝
        // 破壊経路の無いまま貴族IGを肥大させると米の近代化が死ぬ＝文書自身の警告どおり）。奴隷制の富の集中は将来の奴隷制機構とセットで回収する。
        // ※注意＝NationConfig.America() 等は WorldData._cache の**共有インスタンス**を返す。テスト内で FarmLordShare 等を書き換えると
        //   同プロセスの全テストを汚染する（2026-07-23に実証）＝設定の書き換えによる対照実験はここでは行わないこと。
        var russia = new Simulation(NationConfig.Russia().ToScenario(23), new NullAgent());    // 農奴制（FarmLordShare 1.0）
        var america = new Simulation(NationConfig.America().ToScenario(23), new NullAgent());  // 自作農（FarmLordShare 0.0）
        russia.Run(20);
        america.Run(20);
        var ruNoble = russia.World.Region.Pops[0];     // 貴族（地主）
        var ruPeasant = russia.World.Region.Pops[1];   // 農奴
        var usPeasant = america.World.Region.Pops[1];  // 自作農
        Assert.True(ruNoble.LandRentPerMonth.Value > 0, "農奴制の貴族が地代を受け取っていない");
        Assert.Equal(0.0, ruPeasant.LandRentPerMonth.Value);         // 農奴は地代を受け取らない（労働の売上のみ）
        Assert.True(usPeasant.LandRentPerMonth.Value > 0, "自作農が自分の農場の地代を受け取っていない");
    }

    [Fact]
    public void 所得税は歳入を生み保存則を保つ()
    {
        // ※蓄財を直接比べる検証は不適：国庫が太ると国家支出（軍需・行政調達）が売上に還流し蓄財はむしろ微増しうる。
        //   ここでは「歳入」と「保存」を仕様として固定する（解禁のラッチは2026-09-12に廃止＝下の (b) が行政力の担い手）。
        var baseline = new Simulation(NationConfig.Britain().ToScenario(19), new NullAgent());
        var taxed = new Simulation(NationConfig.Britain().ToScenario(19), new NullAgent());
        // 近世の土台（土地税）と官僚への投資で行政力を育てる＝漏れの少ない近代税務行政（率そのものは行政力に縛られない）
        foreach (var s in new[] { baseline, taxed })
        {
            s.World.PoliticalLockEnabled = false;   // 所得税の歳入・保存を検証＝政治ロック（富裕層の所得税反対）とは直交なので隔離
            // ★2026-08-23（配分＝付加価値×売れる量・幽霊生産なし）：閉じた英国は綿花が無く紙（行政の投入）が立たない＝多国世界の交易/植民地財の代替として紙を外生供給（検証対象は行政→所得税の梯子）
            s.World.Region.Markets[Catalog.PaperId].Stock = 1e9;   // 在庫は腐敗で指数減衰（1e6 は Q120 で尽きた実測）＝300Q を持たせる量
            s.World.Region.Markets[Catalog.Goods.First(g => g.Name == "書籍").Id].Stock = 1e9;   // 教育の投入（識字→官僚の補充）も同じ閉世界の栓
            s.World.Policy.LandTaxTarget = 1.0;
            s.World.Policy.AdminSpendingTarget = 0.8;
            // ★2026-09-07（PanicBuyWeight 点火 0.5・切り分け＝群11本の各1本戻しでこれだけが赤→緑）：交易の無い密閉単国世界は主食が慢性的に欠乏し（scarcity>0.75）
            //   買占めが常時発火して行政の成熟が0.50で止まる（多国世界では交易が欠乏を埋め硬性緑）。所得税の歳入・保存の検証と直交なので隔離（紙・書籍の栓と同じ作法）。
            s.World.Coef.PanicBuyWeight = 0;
        }
        baseline.World.Policy.IncomeTaxTarget = 0;
        taxed.World.Policy.IncomeTaxTarget = 1.0;
        // ★汚職V2点火（2026-08-15）で240→300Qへ延長：旧世界では汚職の漏れ（官僚のSavings直入）が官僚階級を養い行政の成熟を早めていた
        //   ＝倒錯した回路が較正の土台だった。V2の英は清廉（実効汚職0.05・監視0.78）＝官僚は俸給（行政財購入）だけで育つ＝
        //   行政の成熟が約10年後ろへ（当時の物差し＝所得税の解禁ゲート0.75。ゲートとラッチは2026-09-12に廃止＝300Qはその名残の長さ）。
        baseline.Run(300);   // イギリス＝行政力が育ち漏れが薄くなる（funding修正で行政は財政余剰から成熟＝ramp時間を確保）
        var ex = Record.Exception(() => taxed.Run(300));
        Assert.Null(ex);     // 所得への課税（pop→国庫）が保存則を破らない
        // 目標1.0へ率が実際に上がっている（天井の廃止後は Move の漸進と政治ロックの隔離だけが律速）＝下の歳入比較の前提。
        Assert.True(taxed.World.TaxRate.Value > 0.25, $"所得税の率が上がっていない（{taxed.World.TaxRate.Value:F3}）＝歳入を比べる前提が立たない");
        Assert.True(taxed.World.Treasury.Value > baseline.World.Treasury.Value,
            $"所得税が歳入を生んでいない（課税{taxed.World.Treasury:F0} vs 基準{baseline.World.Treasury:F0}）");
    }

    [Fact]
    public void 所得税の取り立ては支配度の区分に応じて国庫へ届く()   // (b) 2026-09-12 FIX1＝廃止した天井の置き換えが live であることの番人（(399) で汚職の漏れから支配度の区分へ）
    {
        // 旧・主張「所得税は行政力に応じて徐々に**上限**を得る」は廃止（率を削る天井＝二重計上）。
        // 新・主張＝支配は**率**でなく**額**に効く：同じ TaxRate でも、国家の区分が小さい国は取り立てた金が国庫へ届かない
        //   （Fiscal の課税ベース＝国家区分＋在地区分×届け率〔Politics.RuleDelivery〕。(399) で旧の汚職の財政の漏れ revenue × CorruptionMax × EffectiveCorruption は物理削除）。
        // 2本の腕は完全に同一の世界（同一決定論）で、州の4区分だけを変えて Fiscal を1回踏む＝届き方を隔離して測る。
        Simulation Build()
        {
            var s = new Simulation(NationConfig.Britain().ToScenario(19), new NullAgent());
            s.World.PoliticalLockEnabled = false;   // 富裕層の政治抵抗は漏れの検証と直交
            s.World.TaxRate = new Share(0.10);   // ★2026-09-29 (109)：1783年の英に所得税は無い（1799年から）＝漏れを測る前提の率を盤面へ直接置く（旧の開始値）
            s.World.Policy.IncomeTaxTarget = 1.0;
            s.Run(12);
            return s;
        }
        var high = Build(); var low = Build();
        Assert.Equal(high.World.TaxRate, low.World.TaxRate);   // 分岐前は完全同一
        Assert.True(high.World.TaxRate.Value > 0.1, $"前提が崩れた：所得税が立っていない＝漏れを測れない（{high.World.TaxRate.Value:F3}）");
        foreach (var r in high.World.Regions) r.Rule = [1.0, 0.0, 0.0, 0.0];   // 国家が全域を治める国
        foreach (var r in low.World.Regions) r.Rule = [0.2, 0.2, 0.1, 0.5];    // 国家の区分が小さい国（半分が無統治）
        int cat = (int)TreasuryCat.Tax;   // 書き手は Fiscal の徴税の移し方の1本（納税者→国庫・区分 Tax・2026-10-01 段4g＝旧 w.Pay(TreasuryCat.Tax, revenue)）1箇所だけ＝他の歳出入と混ざらない
        double hb = high.World.TreasuryLedger[cat], lb = low.World.TreasuryLedger[cat];
        Fiscal.Step(high.World, high.World.Coef);
        Fiscal.Step(low.World, low.World.Coef);
        double hTax = high.World.TreasuryLedger[cat] - hb, lTax = low.World.TreasuryLedger[cat] - lb;
        Assert.True(hTax > 0, $"前提が崩れた：税収が立っていない（{hTax:F0}）");
        Assert.True(lTax < hTax, $"国家の区分が小さい国でも同額が国庫に届いた＝区分が取り立てに効いていない（全域{hTax:F0} vs 小さい国{lTax:F0}）");
        Assert.Equal(high.World.TaxRate, low.World.TaxRate);   // 率は1ミリも変わらない＝支配は率でなく額に効く（天井の不在）
    }

    /// <summary>★所得税の**導入**は払えない請求が起こす（2026-09-30 (127)）＝所得税の無い国（率0）が段を上げられるのは国家の支払い率 r＜1 の時だけ・
    /// いったん課した国（率＞0）は政治の門だけ・下げる側は常に通る（天井型の前提は登る側だけ）。法律タブ（GateInfo）と AI の目標の整形（GateAllLawTargets）が同じ1本を読む。
    /// 旧＝前提なし＝AI の教義が開幕の最初の四半期から61カ国で所得税を置いた（仏 0.042・露 0.038＝史実の導入は英1799・普1811/20・墺1849・日1887）。
    /// 突然変異：門を外す→①が赤／率＞0 の免除を外す→③が赤／r の条件を外す（常に閉じる）→②が赤。</summary>
    [Fact]
    public void 所得税の導入は払えない請求の時だけ_導入の後は政治の門だけ()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(29), new NullAgent());
        sim.StepQuarter();
        var w = sim.World;
        Assert.Equal(0.0, w.TaxRate.Value);   // 前提＝1783年の所得税0（NationConfig・(109)）
        // ① 払えない請求が無い（r＝1）＝導入の段は法律タブでも押せない（理由つき）・AI の目標は現状（段0）へ畳まれる
        w.FiscalFundingRatio = new Share(1.0);
        var (ok1, _, why1) = Government.GateInfo(w, PolicyAxis.IncomeTax, increasing: true, 1);
        Assert.False(ok1, "払えない請求が無いのに所得税の導入の段が押せる");
        Assert.Equal("払えない請求が前提", why1);
        w.Policy.IncomeTaxTarget = Government.IncomeTaxTop;
        UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(0.0, w.Policy.IncomeTaxTarget);
        // ② 払えない請求がある（r＜1）＝同じ門が開く・AI の目標は次の1段
        w.FiscalFundingRatio = new Share(0.9);
        Assert.True(Government.LawPrereq(w, PolicyAxis.IncomeTax, 1, w.Coef).Ok, "払えない請求があるのに所得税を導入できない");
        w.Policy.IncomeTaxTarget = Government.IncomeTaxTop;
        UtilityAgent.GateAllLawTargets(w, w.Policy);
        Assert.Equal(Government.TaxValue(PolicyAxis.IncomeTax, 1), w.Policy.IncomeTaxTarget);
        // ③ いったん課した国（率＞0）は r＝1 でも上げられる（前提は導入だけ）・下げる側は常に通る
        w.FiscalFundingRatio = new Share(1.0);
        w.TaxRate = new Share(Government.TaxValue(PolicyAxis.IncomeTax, 1));
        Assert.True(Government.LawPrereq(w, PolicyAxis.IncomeTax, 2, w.Coef).Ok, "導入の後も払えない請求を前提にしている");
        Assert.True(Government.LawPrereq(w, PolicyAxis.IncomeTax, 0, w.Coef).Ok, "下げる側に前提が掛かっている（天井型は登る側だけ）");
    }
    /// <summary>(288)(291) 英の行 landTaxStage（既定の杖）＝段の間の開始値を置ける。整数の段は旧（int 引数）と全ビット一致。
    /// 期待値は本体の式から作らない：①整数の段は旧の式を int のまま打ち直した（型が違う別の計算）②LandTax の段1・2 と半段は手書きの数。
    /// 突然変異：Clamp の後ろで切り捨て（int 化）→半段が赤／NaN の否定形を外す→NaN の項が赤／最上段の分岐を外す→満額が赤。</summary>
    [Fact]
    public void 開幕の土地税は段の間の半段を置け整数の段は旧と全ビット一致する()
    {
        // ① 整数の段：旧の式（int の段・int*double/int）をそのまま打ち直して、全ての軸×段0〜3 で全ビット一致を見る（TaxValue は double の段）。
        foreach (var a in new[] { PolicyAxis.IncomeTax, PolicyAxis.ProgressiveTax, PolicyAxis.PollTax, PolicyAxis.LandTax, PolicyAxis.EducationSpending })
            for (int k = 0; k <= Government.TaxLawStages; k++)
            {
                double top = Government.TaxLawTop(a);
                double old = k == Government.TaxLawStages ? top : k * top / Government.TaxLawStages;   // 旧: int s の枝
                Assert.Equal(System.BitConverter.DoubleToInt64Bits(old),
                             System.BitConverter.DoubleToInt64Bits(Government.TaxValue(a, k)));
            }
        // ② 手書きの数（地租の錨 0.4）：段1＝0.4/3・段2＝0.8/3・満額＝0.4・半段 1.5＝0.2・1.275＝0.17（英の行）。
        Assert.Equal(0.4, Government.LandTaxTop);
        Assert.Equal(0.13333333333333333, Government.TaxValue(PolicyAxis.LandTax, 1));
        Assert.Equal(0.26666666666666666, Government.TaxValue(PolicyAxis.LandTax, 2));
        Assert.Equal(0.4, Government.TaxValue(PolicyAxis.LandTax, 3));
        Assert.Equal(0.2, Government.TaxValue(PolicyAxis.LandTax, 1.5), 12);
        Assert.Equal(0.17, Government.TaxValue(PolicyAxis.LandTax, 1.275), 12);
        // ③ 範囲外は端へ（旧の Clamp と同じ）・NaN は段0（課さない側）＝Math.Clamp は NaN を除かない
        Assert.Equal(0.0, Government.TaxValue(PolicyAxis.LandTax, -1.0));
        Assert.Equal(0.4, Government.TaxValue(PolicyAxis.LandTax, 3.5));
        Assert.Equal(0.0, Government.TaxValue(PolicyAxis.LandTax, double.NaN));
        // ④ 設定→開始の率：world.json と同じ読み口（小数の段も整数の段も読める）。共有の設定（キャッシュ）は書き換えない＝JSON から作る。
        var frac = System.Text.Json.JsonSerializer.Deserialize<NationConfig>("{\"landTaxStage\":1.275}", WorldData.Options)!;
        var whole = System.Text.Json.JsonSerializer.Deserialize<NationConfig>("{\"landTaxStage\":2}", WorldData.Options)!;
        Assert.Equal(0.17, frac.ToScenario(5).InitialLandTax, 12);
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(0.26666666666666666),
                     System.BitConverter.DoubleToInt64Bits(whole.ToScenario(5).InitialLandTax));   // 整数 2 は旧の段2 の率と全ビット一致
        // ⑤ 小数の率は段の格子の途中＝法の段は丸め（中点は常に上段）、目標は段2 の率へ畳まれる＝実効の率は目標へ這い寄る（記録の対象）。
        Assert.Equal(1, Government.TaxStage(PolicyAxis.LandTax, 0.17));
        Assert.Equal(2, Government.TaxStage(PolicyAxis.LandTax, 0.2));
        Assert.Equal(0.13333333333333333, Government.Q4(PolicyAxis.LandTax, 0.17));
    }
}
