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
    public void 土地税は貴族の行政で解禁される()
    {
        // 近世国家＝貴族が行政を担う（貴族の人口比×係数＝国庫不要の行政効率）。
        // よって貴族のいる国は行政費ゼロでも土地税（粗い検地＝年貢/タイユ）が立つ＝序盤の財政を支える（ユーザー要望）。
        // ※治安維持力は低く固定（人口比例の維持費で国庫を食わせない）。
        var sim = new Simulation(NationConfig.Japan().ToScenario(11), new NullAgent());
        sim.World.PoliticalLockEnabled = false;   // 税の解禁・歳入機構を検証＝政治ロック（地主の土地税反対）とは直交なので隔離
        sim.World.BaseCoercion = 100; sim.World.Policy.CoercionTarget = 100;
        sim.World.Policy.AdminSpendingTarget = 0.0;   // 官僚への投資ゼロでも…
        sim.World.Policy.LandTaxTarget = 1.0;
        sim.Run(120);
        Assert.True(sim.World.LandTaxUnlocked, $"貴族のいる国で土地税が解禁されない（行政力{sim.World.AdminPowerSmoothed:F2}）");
        // ★主張の書き直し（2026-09-20 段の点火）：土地税は段の法＝目標1.0は最上段（錨 Government.LandTaxTop＝0.4）へ落ちる。
        //   「解禁」は値を0へ書き潰す三項ではなく**最上段が押せる**こと（LawPrereq「検地台帳が前提」が開く）＝その門と、率が錨の半分を越えて登ることを見る。
        Assert.True(Government.LawPrereq(sim.World, PolicyAxis.LandTax, Government.TaxLawStages, sim.World.Coef).Ok, "解禁後も土地税の最上段が押せない");
        Assert.True(sim.World.LandTax > 0.5 * Government.LandTaxTop, $"解禁後に土地税が動いていない（{sim.World.LandTax:F3}）");
    }

    [Fact]
    public void 専売と人頭税は国庫を富ませ保存則を保つ()
    {
        var baseline = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        var taxed = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        baseline.World.GovDebt = taxed.World.GovDebt = 0;   // P5：仏は重債務開始＝利払いが両系の国庫を0まで枯渇させ税収差が埋もれる（課税0 vs 基準0）ため中立財政で税機構を隔離
        foreach (var w0 in new[] { baseline.World, taxed.World }) foreach (var rg in w0.Regions) foreach (var pp in rg.Pops) pp.BondHoldings = 0;   // ★2026-09-21：国債を手で消すなら持ち手の記録（保有台帳）も消す＝Σ保有＝国内元本の不変条件
        taxed.World.PoliticalLockEnabled = false;   // 税の歳入・保存機構を検証＝政治ロック（大衆の人頭税反対等）とは直交なので隔離
        // ★主張の書き直し（作者裁定2026-09-20＝率の廃止）：腕は率でなく**対象の広さ**（段2＝奢侈品＋役務）。率はエンジンの標準率1本。
        taxed.World.SetStateMonopolyTier(2);
        taxed.World.Policy.PollTaxTarget = 0.5;
        baseline.Run(80);
        var ex = Record.Exception(() => taxed.Run(80));   // 新税の金流（pop→国庫）が保存則（§11.6）を破らない
        Assert.Null(ex);
        Assert.True(taxed.World.Treasury > baseline.World.Treasury,
            $"課税しても国庫が増えない（課税{taxed.World.Treasury:F0} vs 基準{baseline.World.Treasury:F0}）");
    }

    [Fact]
    public void 人頭税は閉鎖階級を免除し大衆に重い()   // 特権＝貴族・聖職者は払わない（逆進性の設計）
    {
        // ★2026-09-30 (122) 盤面の直し：開幕の通貨は旧の不換（(119) で実国家の開幕は銀になった）。銀の盤面では、(117)(120)（支払いの段3-2・3-3）と
        //   合わせた木で無税の側の赤字の埋め方（借入と改鋳）が変わり、直接負担の対比が 6.9%→4.0% に縮む＝この番人が測る逆進性（直接負担）とは別の経路。
        Scenario FranceFiat() { var c = NationConfig.France().ShallowClone(); c.InitialMonetaryLaw = MonetaryStandardLaw.Fiat; return c.ToScenario(17); }
        var baseline = new Simulation(FranceFiat(), new NullAgent());
        var taxed = new Simulation(FranceFiat(), new NullAgent());
        baseline.World.GovDebt = taxed.World.GovDebt = 0;   // P5：仏は重債務開始＝切り下げが両系の貯蓄を毎月削り逆進性の対比が埋もれるため中立財政で隔離
        foreach (var w0 in new[] { baseline.World, taxed.World }) foreach (var rg in w0.Regions) foreach (var pp in rg.Pops) pp.BondHoldings = 0;   // ★2026-09-21：国債を手で消すなら持ち手の記録（保有台帳）も消す＝Σ保有＝国内元本の不変条件
        baseline.World.Policy.PollTaxTarget = 0;     // 既定で人頭税0.5が入る（近世財政）ため、清浄な対比（無税 vs 最大）にして逆進性の直接効果を測る
        taxed.World.Policy.PollTaxTarget = 1.0;
        // ★2026-09-20 段の点火：人頭税は段の法＝目標1.0は最上段（錨 Government.PollTaxTop＝0.5＝開始値そのもの）へ落ちる＝課税側は開始値に留まる。
        //   旧の対比（無税へ漸減 vs 1.0へ漸増）が縮むので、無税側は**開始時点から無税**にして同じ「無税 vs 最大」の対比を保つ。
        baseline.World.PollTax = 0;
        // ★2026-09-29 (109)：1783年の仏の人頭税は段2（タイユとカピタシオン＝NationConfig.PollTaxStage）＝課税側も**開始時点から最上段**に置いて同じ「無税 vs 最大」の対比を保つ。
        taxed.World.PollTax = Government.PollTaxTop;
        // 人頭税の逆進性（直接負担）の検証＝民族統一度とは直交。民族の一般均衡効果（行政効率・不満）が貴族の間接損を揺らして
        // 24Qでも逆転させるため、この税制テストでは民族系を無効化して直接負担だけを測る（24Q短期化と同じ趣旨の隔離）。
        baseline.World.EthnicEnabled = taxed.World.EthnicEnabled = false;
        baseline.World.ParliamentEnabled = taxed.World.ParliamentEnabled = false;   // 人頭税の逆進性（直接負担）を測る＝議会が税の綱領で人頭/消費を動かすのは別レイヤーなので isolate
        baseline.World.PoliticalLockEnabled = taxed.World.PoliticalLockEnabled = false;   // 逆進性の直接負担を測る＝大衆の人頭税反対による政治ロックとは直交なので隔離
        // 直接の税負担（農民が払い貴族は免除）を測る短い期間。長期（60Q）まで回すと一般均衡の間接効果
        // （景気収縮で貴族の地代・奢侈資産が目減り）が直接負担を上回り、逆進性の直接効果が埋もれるため24Qで測る。
        baseline.Run(24);
        taxed.Run(24);
        double PeasantPc(Simulation s) { var p = s.World.Region.Pops[1]; return p.Savings / System.Math.Max(1, p.Size); }
        double NoblePc(Simulation s) { var p = s.World.Region.Pops[0]; return p.Savings / System.Math.Max(1, p.Size); }
        // 大衆（農民）は人頭税で明確に貧しくなる。貴族の減り方は相対的に軽い（免除＝直接は払わない）。
        double peasantHit = 1 - PeasantPc(taxed) / System.Math.Max(1e-9, PeasantPc(baseline));
        double nobleHit = 1 - NoblePc(taxed) / System.Math.Max(1e-9, NoblePc(baseline));
        Assert.True(peasantHit > 0.05, $"人頭税が大衆に効いていない（農民の目減り {peasantHit:P1}）");
        Assert.True(nobleHit < peasantHit, $"免除のはずの貴族（{nobleHit:P1}）が農民（{peasantHit:P1}）より重く払っている");
    }

    [Fact]
    public void 商人は流通マージンで稼ぐ()   // 消費者の購入に小売の取り分が乗り、商業の規模＝商人の収入になる
    {
        var sim = new Simulation(NationConfig.Japan().ToScenario(31), new NullAgent());   // 商の国（商人800）
        sim.Run(40);
        var merchant = sim.World.Region.Pops.First(p => p.ClassId == 10);
        Assert.True(merchant.Earned > 0, "商人が流通マージンを得ていない");
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
        Assert.True(ruNoble.LandRent > 0, "農奴制の貴族が地代を受け取っていない");
        Assert.Equal(0.0, ruPeasant.LandRent);         // 農奴は地代を受け取らない（労働の売上のみ）
        Assert.True(usPeasant.LandRent > 0, "自作農が自分の農場の地代を受け取っていない");
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
        Assert.True(taxed.World.TaxRate > 0.25, $"所得税の率が上がっていない（{taxed.World.TaxRate:F3}）＝歳入を比べる前提が立たない");
        Assert.True(taxed.World.Treasury > baseline.World.Treasury,
            $"所得税が歳入を生んでいない（課税{taxed.World.Treasury:F0} vs 基準{baseline.World.Treasury:F0}）");
    }

    [Fact]
    public void 所得税の取り立ては行政力に応じて漏れる()   // (b) 2026-09-12 FIX1＝廃止した天井の置き換えが live であることの番人
    {
        // 旧・主張「所得税は行政力に応じて徐々に**上限**を得る」は廃止（率を削る天井＝二重計上）。
        // 新・主張＝行政力は**率**でなく**額**に効く：同じ TaxRate でも、行政が空の国は取り立てた金が国庫へ届かない
        //   （Fiscal の汚職漏れ＝revenue × CorruptionMax × EffectiveCorruption、EffectiveCorruption=(1−AdminEfficiency)(1−OversightV2)）。
        // 2本の腕は完全に同一の世界（同一決定論）で、行政の充足（Region.AdminFill）だけを変えて Fiscal を1回踏む＝漏れを隔離して測る。
        Simulation Build()
        {
            var s = new Simulation(NationConfig.Britain().ToScenario(19), new NullAgent());
            s.World.PoliticalLockEnabled = false;   // 富裕層の政治抵抗は漏れの検証と直交
            s.World.TaxRate = 0.10;   // ★2026-09-29 (109)：1783年の英に所得税は無い（1799年から）＝漏れを測る前提の率を盤面へ直接置く（旧の開始値）
            s.World.Policy.IncomeTaxTarget = 1.0;
            s.Run(12);
            return s;
        }
        var high = Build(); var low = Build();
        Assert.Equal(high.World.TaxRate, low.World.TaxRate);   // 分岐前は完全同一
        Assert.True(high.World.TaxRate > 0.1, $"前提が崩れた：所得税が立っていない＝漏れを測れない（{high.World.TaxRate:F3}）");
        foreach (var r in high.World.Regions) r.AdminFill = 1.0;   // 行政の行き届いた国
        foreach (var r in low.World.Regions) r.AdminFill = 0.0;    // 官僚の役務が買えていない国
        int cat = (int)TreasuryCat.Tax;   // 書き手は Fiscal の w.Pay(TreasuryCat.Tax, revenue) 1箇所だけ＝他の歳出入と混ざらない
        double hb = high.World.TreasuryLedger[cat], lb = low.World.TreasuryLedger[cat];
        Fiscal.Step(high.World, high.World.Coef);
        Fiscal.Step(low.World, low.World.Coef);
        double hTax = high.World.TreasuryLedger[cat] - hb, lTax = low.World.TreasuryLedger[cat] - lb;
        Assert.True(hTax > 0, $"前提が崩れた：税収が立っていない（{hTax:F0}）");
        Assert.True(lTax < hTax, $"行政が空の国でも同額が国庫に届いた＝行政力が取り立てに効いていない（行き届いた{hTax:F0} vs 空{lTax:F0}）");
        Assert.True(low.World.LastTaxLeak > high.World.LastTaxLeak,
            $"漏れが行政力で増えていない（行き届いた{high.World.LastTaxLeak:F0} vs 空{low.World.LastTaxLeak:F0}）");
        Assert.Equal(high.World.TaxRate, low.World.TaxRate);   // 率は1ミリも変わらない＝行政は率でなく額に効く（天井の不在）
    }
}
