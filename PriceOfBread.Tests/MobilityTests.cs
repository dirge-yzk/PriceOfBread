using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// POPの転職（昇格・転落）：蓄財した中層→資本家／困窮した上層→中層（最も実入りの良い職へ）／
/// 食えない中層→労働者。転落は闘争度を上げる。聖職者は世俗化で没落するが人口比の下限で消えない。
/// 閾値は国民平均の1人あたり貯蓄に対する倍率＝経済規模で自動スケール。
/// </summary>
public class MobilityTests
{
    [Fact]
    public void 転職があっても人口と貨幣は保存される()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        double p0 = sim.World.TotalPopulation;
        var ex = Record.Exception(() => sim.Run(200));   // §11.6 が全ティックで保持（規模・貯蓄の比例移動＝保存）
        Assert.Null(ex);
        Assert.True(System.Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "転職で人口保存が破れた");
    }

    [Fact]
    public void 蓄財した中層が資本家に昇格する()   // ③中流→資本家（設計仕様）。昇格機構を直接・決定論的に検証＝人口動態(衛生/疫病等)のノイズに頑健（旧・240年の人口比テストは均衡近傍で脆弱だった）
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(43), new NullAgent());
        sim.Run(20);   // 経済を落ち着かせる
        var r = sim.World.Region;
        Assert.True(r.InvestorPopIndex >= 0 && r.MeritToIndex >= 0, "英に階級移動の階級が無い");
        var mid = r.Pops[r.MeritToIndex];          // 中流（専門職・技師）
        var cap = r.Pops[r.InvestorPopIndex];      // 資本家
        double totalSav = 0, total = 0;
        foreach (var p in r.Pops) { totalSav += System.Math.Max(0, p.Savings.Value); total += p.Size; }
        double avgPerCap = totalSav / total;
        // 好況で中層が資金を貯めた状態を再現：中層の1人あたり貯蓄を平均の20倍へ（閾値8倍 CapitalRiseThreshold を確実に超える）。
        // 財源は最富裕の資本家から移す（pop間移転＝貨幣保存＝保存則を破らない）。
        double want = 20.0 * avgPerCap * mid.Size;
        double take = System.Math.Min(System.Math.Max(0, want - System.Math.Max(0, mid.Savings.Value)), System.Math.Max(0, cap.Savings.Value));
        cap.SetSavings(cap.Savings.Value - (take)); mid.SetSavings(mid.Savings.Value + (take));
        Assert.True(mid.Savings.Value / mid.Size > sim.World.Coef.CapitalRiseThreshold * avgPerCap, "セットアップ不足：中層が昇格閾値に達していない");
        // ★盤面の直し（2026-10-01 (143)）：旧＝資本家の規模が8回の後に増えたか（cap1 > cap0）で見た。だが財源を資本家から移すので資本家の1人あたりの資産が転落の線
        //   （上流の転落 UpperFallThreshold 0.8×平均）を割り、④の転落が同じ8回の中で起きる（1人あたり 4,217→417）＝昇格（③）と盤面が起こした転落の差を見ていた（(144) 反証の読み＝差の大半は昇格が運ぶ貯蓄で転落を避けた分）
        //   ＝(143) で最初の20四半期の軌道が少し動いた（資本家 459→475・1人あたり 4,386→4,217）だけで転落が1回から2回へ増えて赤になった。
        //   直し＝**同じ移転の双子で昇格の閾値だけ無限大**（③だけを止める）と比べる＝盤面の転落は両方に同じだけ起きる＝差が③の昇格。主張（蓄財した中層は資本家へ昇格する）は同じ。
        var ctl = new Simulation(NationConfig.Britain().ToScenario(43), new NullAgent());
        ctl.Run(20);
        var rc = ctl.World.Region;
        var midC = rc.Pops[rc.MeritToIndex]; var capC = rc.Pops[rc.InvestorPopIndex];
        capC.SetSavings(capC.Savings.Value - (take)); midC.SetSavings(midC.Savings.Value + (take));   // 同じ種・同じ20四半期＝同じ盤面へ同じ移転（盤面の書き込みは Board.SetSavings＝(139) の init・(144) 合流）
        ctl.World.Coef.CapitalRiseThreshold = double.PositiveInfinity;   // ③（中流→資本家）を止める（閾値は rank1 の商人・職人→資本家と共有＝下で両方の腕の rank1 を止める）
        foreach (var wd in new[] { sim.World, ctl.World }) foreach (int mi in new[] { wd.Region.MerchantPopIndex, wd.Region.ArtisanPopIndex }) if (mi >= 0) wd.Region.Pops[mi].SetSavings(0);   // ★c132kx：旗 MerchantArtisanCapitalRise（rank1 を止める）を焼いた＝商人・職人の蓄財を両方の腕で 0 にして rank1 の昇格の条件を満たさせない＝差が③だけになる
        double cap0 = cap.Size;
        Assert.Equal(cap0, capC.Size);   // 前提：双子は同じ盤面（同じ種・同じ20四半期＝移転の前の資本家の規模が一致）
        for (int i = 0; i < 8; i++) { Meritocracy.Step(sim.World, sim.World.Coef); Meritocracy.Step(ctl.World, ctl.World.Coef); }   // 昇格機構のみを回す（人口動態を挟まない＝衛生等の人口ノイズと隔離）
        double cap1 = r.Pops[r.InvestorPopIndex].Size, capCtl = rc.Pops[rc.InvestorPopIndex].Size;
        Assert.True(cap1 > capCtl, $"蓄財した中層が資本家に昇格しない（資本家 {cap0:F1}→{cap1:F1}・昇格を止めた双子 {capCtl:F1}）");
    }

    [Fact]
    public void 蓄財した商人が資本家に昇格する()   // rank1 商人・職人→資本家（旗 MerchantArtisanCapitalRise を 1 に焼いて消した＝(1281)）。同じ移転の双子で昇格の閾値だけ無限大＝差が rank1 の昇格
    {
        static (Simulation Sim, double Take) Build()
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(43), new NullAgent());
            sim.Run(20);
            var r = sim.World.Region;
            Assert.True(r.InvestorPopIndex >= 0 && r.MerchantPopIndex >= 0, "英に資本家か商人が無い");
            var mer = r.Pops[r.MerchantPopIndex]; var cap = r.Pops[r.InvestorPopIndex];
            double totalSav = 0, total = 0;
            foreach (var p in r.Pops) { totalSav += System.Math.Max(0, p.Savings.Value); total += p.Size; }
            double want = 20.0 * (totalSav / total) * mer.Size;
            double take = System.Math.Min(System.Math.Max(0, want - System.Math.Max(0, mer.Savings.Value)), System.Math.Max(0, cap.Savings.Value));
            cap.SetSavings(cap.Savings.Value - take); mer.SetSavings(mer.Savings.Value + take);   // 財源は資本家から（貨幣保存）
            return (sim, take);
        }
        var (sim, take) = Build(); var (ctl, take2) = Build();
        Assert.True(take > 0 && take == take2, "セットアップ不足：商人へ移せていない");
        ctl.World.Coef.CapitalRiseThreshold = double.PositiveInfinity;   // 昇格（専門職・商人・職人→資本家）を止める双子
        var r = sim.World.Region; var rc = ctl.World.Region;
        double cap0 = r.Pops[r.InvestorPopIndex].Size;
        for (int i = 0; i < 8; i++) { Meritocracy.Step(sim.World, sim.World.Coef); Meritocracy.Step(ctl.World, ctl.World.Coef); }
        double cap1 = r.Pops[r.InvestorPopIndex].Size, capCtl = rc.Pops[rc.InvestorPopIndex].Size;
        Assert.True(cap1 > capCtl, $"蓄財した商人が資本家に昇格しない（資本家 {cap0:F1}→{cap1:F1}・昇格を止めた双子 {capCtl:F1}）");
    }

    [Fact]
    public void 食えない中層は労働者に転落し不満を持ち込む()   // 宗教国の失業教師＝収入ゼロの中層は消える（受け皿＝下層）
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(47), new NullAgent());   // 宗教法＝教師に職がない
        var r = sim.World.Region;
        double teacher0 = r.Pops[r.TeacherPopIndex].Size;
        sim.Run(120);
        Assert.True(r.Pops[r.TeacherPopIndex].Size < teacher0 * 0.7,
            $"職のない教師が転落していない（{teacher0:F0}→{r.Pops[r.TeacherPopIndex].Size:F0}）");
    }

    [Fact]
    public void 教育市場は子供の就学需要で維持され教師が枯渇しない()   // 教師崩壊の回帰（2件の真因を監視）：
    {
        // ①人口動態：純増だけを子供に加えると成熟社会で子供が払底（0.3→0.04を観測）→学校需要が消える。
        // ②昇格：給与差駆動で、教育が売れる限り教師の給与が立ち志望者が補充される（枠の凍結事故は構造ごと廃止）。
        var sim = new Simulation(NationConfig.Britain().ToScenario(59), new NullAgent());   // イギリス＝最初から公教育
        var r = sim.World.Region;
        r.Markets[Catalog.PaperId].Stock = 1e6;   // ★2026-08-23：閉じた英国は綿花なし→紙（教育の投入）が立たない＝多国世界の交易/植民地財の代替として紙を外生供給（検証対象は就学需要→教師の補充）
        r.Markets[Catalog.Goods.First(g => g.Name == "書籍").Id].Stock = 1e6;   // 教育の投入は技術で紙→書籍へ切り替わる＝書籍も外生供給
        sim.Run(240);
        double childShare = r.Pops[2].AgeShare(0, 15);   // 工場労働者の15歳未満の割合（粗出生で補充され続ける・年齢の層）
        Assert.True(childShare > 0.2, $"成熟社会で子供が払底した（子供比率 {childShare:P0}）＝粗出生の補充が機能していない");
        var edu = r.Markets[Catalog.EducationId];
        Assert.True(edu.LastProduced > 50, $"教育の生産が崩壊した（{edu.LastProduced:F1}）＝就学需要に教師の供給が応えていない");
        Assert.True(r.Pops[r.TeacherPopIndex].Size > 8, $"教師が枯渇した（{r.Pops[r.TeacherPopIndex].Size:F0}人）＝給与シグナルが教師に志望者を呼べていない");
    }

    [Fact]
    public void 失業プールは失業者を同階級の稼げる職へ流し資格が無ければ降格する()   // ①''（2026-08-15作者指示・休眠JobSearchRatePerQuarter）の点火経路
    {
        // 双子シム（同シード＝決定論で同一状態）：ON側だけプールが動く＝差分がプールの効果そのもの。
        var off = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var on = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        off.Run(20); on.Run(20);
        on.World.Coef.JobSearchRatePerQuarter = new RatePerQuarter(0.10);
        foreach (var sim in new[] { off, on })
        {
            var r = sim.World.Region;
            var farmer = r.Pops[1]; var worker = r.Pops[r.MeritFromIndex];
            // 農民の半分が失業・労働者の1人あたり所得が農民を大きく上回る世界（工場が生産性で勝つ）を直接セット＝決定論
            farmer.UnemploymentRate = new Share(0.5); farmer.UnemploymentEma = new Share(0.5);   // JobSearchUsesEma点火（2026-08-16）＝プールは慢性失業（EMA）を読む
            farmer.EarnedPerMonth = new MoneyPerMonth(0.5 * farmer.Size); worker.EarnedPerMonth = new MoneyPerMonth(10.0 * worker.Size);
            // 職人は識字0（資格なし）＋半分失業：同階級に入れる職が無い→下層の最良職（労働者）へ降格
            var artisan = r.Pops[r.ArtisanPopIndex];
            artisan.UnemploymentRate = new Share(0.5); artisan.UnemploymentEma = new Share(0.5); artisan.Literacy = 0.0; artisan.EarnedPerMonth = new MoneyPerMonth(0);
            double pop0 = 0; foreach (var p in r.Pops) pop0 += p.Size;
            Meritocracy.Step(sim.World, sim.World.Coef);
            double pop1 = 0; foreach (var p in r.Pops) pop1 += p.Size;
            Assert.True(System.Math.Abs(pop1 - pop0) < 1e-6 * pop0, "失業プールで人口保存が破れた");
        }
        var ro = off.World.Region; var rn = on.World.Region;
        Assert.True(rn.Pops[1].Size < ro.Pops[1].Size,
            $"失業した農民が稼げる職へ流れない（OFF {ro.Pops[1].Size:F1} / ON {rn.Pops[1].Size:F1}）");
        Assert.True(rn.Pops[rn.ArtisanPopIndex].Size < ro.Pops[ro.ArtisanPopIndex].Size,
            $"資格の無い失業職人が降格しない（OFF {ro.Pops[ro.ArtisanPopIndex].Size:F1} / ON {rn.Pops[rn.ArtisanPopIndex].Size:F1}）");
        Assert.True(rn.Pops[rn.MeritFromIndex].Size > ro.Pops[ro.MeritFromIndex].Size,
            "受け皿（下層の最良職＝労働者）が増えていない");
    }

    [Fact]
    public void 失業プールの着地はsoftmax按分でも動き低温の極限を超えない()   // JobPoolTemperature の経路（(1200) の後は softmax の 1 本・旧の総取りの枝は無い）
    {
        var wta = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());   // 低温の極限（T=1e-9＝ほぼ最良の 1 つへ寄る）
        var soft = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());  // softmax（T=0.15）
        wta.Run(20); soft.Run(20);
        wta.World.Coef.JobSearchRatePerQuarter = new RatePerQuarter(0.10);
        soft.World.Coef.JobSearchRatePerQuarter = new RatePerQuarter(0.10); soft.World.Coef.JobPoolTemperature = 0.15;
        wta.World.Coef.JobPoolTemperature = 1e-9;   // (1206) 比べる対の直し：以前は温度を置かず既定の 0.15 どうしの比べだった
        double before = 0;
        foreach (var sim in new[] { wta, soft })
        {
            var r = sim.World.Region;
            var farmer = r.Pops[1]; var worker = r.Pops[r.MeritFromIndex];
            farmer.UnemploymentRate = new Share(0.5); farmer.UnemploymentEma = new Share(0.5);   // EMA点火に追随
            farmer.EarnedPerMonth = new MoneyPerMonth(0.5 * farmer.Size); worker.EarnedPerMonth = new MoneyPerMonth(10.0 * worker.Size);
            before = farmer.Size;
            Meritocracy.Step(sim.World, sim.World.Coef);
        }
        double movedWta = before - wta.World.Region.Pops[1].Size;
        double movedSoft = before - soft.World.Region.Pops[1].Size;
        Assert.True(movedSoft > 0, "softmax按分で失業農民が全く動かない");
        Assert.True(movedSoft <= movedWta + 1e-9, $"softmax按分が低温の極限より多く流した（低温 {movedWta:F2} / softmax {movedSoft:F2}）");
    }

    [Fact]
    public void 聖職者は世俗化で没落するが消えない()   // 設計仕様：明確に減少するが0にはならない
    {
        var cfg = NationConfig.Japan();
        var sim = new Simulation(cfg.ToScenario(53), new UtilityAgent());   // 統治AI＝いずれ公教育へ世俗化→聖職者は教育収入を失う
        sim.World.ParliamentEnabled = false;   // 聖職の世俗化没落＋下限（Meritocracy）の機構を検証＝議会の教育加速は別レイヤーなので isolate
        sim.World.PoliticalLockEnabled = false;   // 法の政治ゲートも isolate（所得税テストと同じ標準手法）＝土地の政治力で旧支配層が世俗化を票で止めるのは正しい挙動だが、このテストの主題ではない

        var r = sim.World.Region;
        double clergy0 = r.Pops[6].Size, low = double.MaxValue;
        // ★2026-09-28 (93)：旧＝400四半期目の1点で比べた＝世俗化の後の聖職者は山（約800）→谷（約100）→再びの増加を通る創発の軌道で、
        //   最後の点は軌道の小さな揺れで入れ替わる（06055b5 でも種63 で赤・(93) の変更の後は種53 で赤・10種で8〜9が緑＝分布は同じ）。
        //   ⇒ 世俗化の後の最小で見る＝「明確に減少する（開始を割り込む）が0にはならない（最小でも絶滅しない）」をそのまま測る。
        for (int q = 0; q < 400; q++)
        {
            sim.Run(1);
            if (sim.World.EducationLaw >= EducationLaw.Public) low = System.Math.Min(low, r.Pops[6].Size);
        }
        Assert.True(sim.World.EducationLaw >= EducationLaw.Public, "AIが公教育へ世俗化していない（前提が崩れた）");
        Assert.True(low > 0.1 && r.Pops[6].Size > 0.1, $"聖職者が絶滅した（下限が効いていない・最小 {low:F2}）");
        Assert.True(low < clergy0, $"世俗化でも聖職者が減らない（{clergy0:F0}→世俗化の後の最小 {low:F0}）");
    }

    [Fact]
    public void 転落物差し改革で所得に見合う資産の貴族は没落しない()   // rank7（点火0.3）のON経路＝テストは効果を明示するため1.0で検証：アンカーを「他人の平均」から「自分の年所得」へ
    {
        // ⚠2026-09-26：旧名「結晶資産持ち貴族は没落しない」は rank7(a)（結晶資産 IlliquidWealth 込みの判定資産）を見ていたが、
        //   結晶資産は休眠の枝だけが書き実走では常に0だった＝欄ごと削除（dorm-24）。残る生きた部分＝(b) のアンカーを見る番人へ置き換えた。
        // 双子シム（同シード＝決定論で同一状態）：ON側だけ物差しが変わる＝差分が改革の効果そのもの。
        var off = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var on = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        off.Run(20); on.Run(20);
        off.World.Coef.FallAnchorReform = 0;    // OFF側を明示ピン（2026-08-16に既定0→0.5で点火済み＝対照は明示しないと番人が空洞化する）
        on.World.Coef.FallAnchorReform = 1.0;   // ON側は効果を可視化する全強度（実運用の既定は0.3）
        double[] size0 = new double[2], size1 = new double[2];
        int si = 0;
        foreach (var sim in new[] { off, on })
        {
            var r = sim.World.Region;
            int nobleIdx = -1;   // 貴族＝聖職者でない閉鎖階級
            for (int i = 0; i < r.Pops.Count; i++) if (r.Pops[i].Closed && i != r.ClergyPopIndex) { nobleIdx = i; break; }
            Assert.True(nobleIdx >= 0, "閉鎖階級（貴族）が見つからない");
            var noble = r.Pops[nobleIdx];
            // 「資産は平均の半分・年所得も平均資産の半分」の貴族を直接セット（決定論）。
            // 旧物差し：Savings/Size=0.5×avg < 0.8×avg ＝転落圏。新物差し：資産 0.5×avg ≥ 0.8×年所得(=0.5×avg) ＝安泰（所得に見合う資産）。
            for (int it = 0; it < 6; it++)   // Savingsを書くと平均自身が動く＝数回の反復で不動点へ
            {
                double totalSav = 0, total = 0;
                foreach (var p in r.Pops) { totalSav += System.Math.Max(0, p.Savings.Value); total += p.Size; }
                noble.SetSavings(0.5 * (totalSav / total) * noble.Size);
            }
            double avgF; { double totalSav = 0, total = 0; foreach (var p in r.Pops) { totalSav += System.Math.Max(0, p.Savings.Value); total += p.Size; } avgF = totalSav / total; }
            noble.EarnedPerMonth = new MoneyPerMonth(Calendar.PerYearToPerMonth(0.5 * avgF * noble.Size));   // 年所得/人 ＝ 平均貯蓄/人の半分（アンカーの言語）
            noble.LastEarnedPerMonth = noble.EarnedPerMonth;
            Assert.True(noble.Savings.Value / noble.Size < sim.World.Coef.UpperFallThreshold * avgF, "セットアップ不足：旧物差しの転落圏に居ない");
            size0[si] = noble.Size;
            Meritocracy.Step(sim.World, sim.World.Coef);
            size1[si] = r.Pops[nobleIdx].Size;
            si++;
        }
        Assert.True(size1[0] < size0[0] - 1e-9, $"旧物差しで貧窮貴族が転落しない（{size0[0]:F2}→{size1[0]:F2}）＝前提が崩れた");
        Assert.True(size1[1] > size0[1] - 1e-9, $"改革ONで所得に見合う資産の貴族が幽霊転落した（{size0[1]:F2}→{size1[1]:F2}）");
    }

    [Fact]
    public void 性別運搬は閉じた専門職へ女性を運ばず教師へは運ぶ()   // rank12（点火1.0・2026-08-16）のON経路
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World; var c = w.Coef; var r = w.Region;
        w.WomenRights = WomenRightsLaw.None;   // 権利の梯子なし＝専門職（技師/官僚/医者）の access=0
        w.WomenWageRetention = 0.0;
        // ①昇格だけを残して隔離（他の移動機構は率0）
        c.JobSearchRatePerQuarter = new RatePerQuarter(0); c.PettyEntryRatePerQuarter = new RatePerQuarter(0); c.ArtisanDeclineRatePerQuarter = new RatePerQuarter(0);
        c.MiddleFallRatePerQuarter = new RatePerQuarter(0); c.UpperFallRatePerQuarter = new RatePerQuarter(0); c.BankruptcyFallRatePerQuarter = new RatePerQuarter(0); c.CapitalRiseRatePerQuarter = new RatePerQuarter(0);
        var worker = r.Pops[r.MeritFromIndex];
        var eng = r.Pops[r.MeritToIndex];
        Assert.True(r.TeacherPopIndex >= 0, "教師popが無い");
        var teacher = r.Pops[r.TeacherPopIndex];
        worker.Literacy = 1.0; worker.EarnedPerMonth = new MoneyPerMonth(1.0 * worker.Size); worker.SetAdultFemaleShare(0.6);
        eng.SetAdultFemaleShare(0.02); teacher.SetAdultFemaleShare(0.25);   // 初期符号化と同じ性別分離（技師0.02・教師0.25＝20歳以上の層）
        static double Fem(Pop p) => p.AgeShare(Pop.Layout.AdultAge, double.PositiveInfinity, 1) / p.AgeShare(Pop.Layout.AdultAge, double.PositiveInfinity);   // 大人の境（8層は20歳・11層は21歳＝性別の門 SetAdultFemaleShare と同じ層の境）
        foreach (var p in r.Pops) if (p.IsModernElite) { p.EducationFill = 1.0; }
        eng.EarnedPerMonth = new MoneyPerMonth(10.0 * eng.Size); teacher.EarnedPerMonth = new MoneyPerMonth(10.0 * teacher.Size);
        double engSize0 = eng.Size, teaSize0 = teacher.Size;
        // 保存されるのは女性の人数（年齢の層の女の層×規模・2026-09-30 段4e 後半＝移し方は層×性ごとの人数を足す）
        double femMass0 = 0; foreach (var p in r.Pops) femMass0 += p.AgeShare(0, double.PositiveInfinity, 1) * p.Size;
        Meritocracy.Step(w, c);
        Assert.True(eng.Size > engSize0 + 1e-9, "技師への昇格が起きない（前提が崩れた）");
        Assert.True(teacher.Size > teaSize0 + 1e-9, "教師への昇格が起きない（前提が崩れた）");
        // 技師行きコホートは女性0（access=0）＝技師の女性比は流入で薄まる。教師行き（access=1.0）は従来どおりsrc平均0.6を運ぶ＝教師の女性比は上がる。
        Assert.True(Fem(eng) < 0.02, $"閉じた専門職の女性比が薄まらない（技師 {Fem(eng):F4}）");
        Assert.True(Fem(teacher) > 0.25, $"教師行きコホートの女性比が運ばれていない（教師 {Fem(teacher):F4}）");
        double femMass1 = 0; foreach (var p in r.Pops) femMass1 += p.AgeShare(0, double.PositiveInfinity, 1) * p.Size;
        Assert.True(System.Math.Abs(femMass1 - femMass0) < 1e-6 * System.Math.Max(1.0, femMass0),
            $"性別の選抜運搬で女性の総量が保存されない（{femMass0:F2}→{femMass1:F2}）");
    }

    /// <summary>★人の移し方の1本（2026-09-30 処理の一元化 段4e I・<c>Pop.Blend</c>）：女性比は「成人のうちの女性の割合」＝移る組と受け手を**成人数**で混ぜる
    /// （成人の女性の数が保存される）。旧＝5つの写し（州の中・社会移動・州の間・越境・招聘）が子供込みの規模で重み付けた＝子供比の違う組では女性の数がずれた
    /// （Military の G11#2 の直しが5つに届いていなかった）。盤面＝子供の多い村（子供比 0.5・女性比 0.5）から子供のいない POP（女性比 0.2）へ40人。
    /// ⚠突然変異「規模で重み付ける」で赤。</summary>
    [Theory]
    [InlineData(8)]
    [InlineData(11)]
    public void 人の移し方は女性比を成人数で混ぜ成人の女性の数を保存する(int bands)
    {
        // ★年齢の層（2026-09-30 段4e 後半）：性は層の中＝移し方は層×性の人数を足すだけ＝成人数で混ぜる形が構造で出る
        // ★(490) 配列は層の器（配列の長さから引く）で組む＝8層でも11層でも同じ主張（旧は 8 層の 16 本を直書きし、11 層の既定のプロセスでも 8 層の算術を叩いていた）。環境変数に依らず 2 つの器を並べて回す
        var lay = bands == 8 ? AgeLayout.Bands8 : AgeLayout.Bands11;
        double[] Mix(double kids, double adultMale, double adultFemale)   // 子供（大人に入る年齢より下の層）に kids を層の数で均等・大人の最初の層に男女を置く（和 = kids + adultMale + adultFemale）
        {
            var a = new double[2 * lay.Bands];
            for (int b = 0; b < lay.AdultFirstBand; b++) { a[2 * b] = kids / lay.AdultFirstBand / 2.0; a[2 * b + 1] = kids / lay.AdultFirstBand / 2.0; }
            a[2 * lay.AdultFirstBand] = adultMale; a[2 * lay.AdultFirstBand + 1] = adultFemale;
            return a;
        }
        double FemaleAdults(Pop p) => Pop.AgeShareOf(p.Ages, lay.AdultAge, double.PositiveInfinity, 1) * p.Size;
        double Fem(Pop p) => Pop.AgeShareOf(p.Ages, lay.AdultAge, double.PositiveInfinity, 1) / Pop.AgeShareOf(p.Ages, lay.AdultAge, double.PositiveInfinity, -1);   // 大人の境（8層は20歳・11層は21歳＝性別の門 SetAdultFemaleShare と同じ層の境）
        var from = new Pop { Size = 100, Ages = Mix(0.5, 0.25, 0.25) };   // 子供が半分・大人は男女半々
        var to = new Pop { Size = 100, Ages = Mix(0.0, 0.8, 0.2) };   // 子供がいない・大人の女性比 0.2
        double fem0 = FemaleAdults(from) + FemaleAdults(to);
        Pop.Blend(from, to, 40);
        // 受け手＝大人 100＋20（移る40人のうち大人20）・女性 20＋10 → 30/120＝0.25（規模で重み付けると (0.2×100＋0.5×40)/140＝0.2857）
        Assert.Equal(0.25, Fem(to), 12);
        Assert.Equal(20.0 / 140.0, Pop.AgeShareOf(to.Ages, 0, lay.AdultAge, -1), 12);
        Assert.Equal(0.5, Fem(from), 12);   // 選抜の無い移動は送り手の構成を1ビットも動かさない
        Assert.Equal(fem0, FemaleAdults(from) + FemaleAdults(to), 9);
    }

    /// <summary>★国内の移動も政治意識・国民意識・思想の分布を人数で運ぶ（作者裁定 2026-09-28 §14-2 の6「国内の移動でも運ぶ＝1本の Blend で全部」・段4e I）。
    /// 旧＝国民意識を運ぶのは越境移民だけ・政治意識と思想の分布はどの経路も運ばなかった（移った人の意識が着地の POP の値に化けた）。
    /// 国民意識の未捕捉（−1）は重み0（書き手が触らない POP は読み手も見ない）。⚠突然変異「政治意識を運ばない」「未捕捉を重みに入れる」「思想を運ばない」で赤。</summary>
    [Fact]
    public void 国内の移動も政治意識と国民意識と思想の分布を人数で運ぶ()
    {
        var a = new Pop { Size = 30, Consciousness = 0.9 };
        var b = new Pop { Size = 90, Consciousness = 0.1 };
        a.IdeologyShares[2] = 1.0; b.IdeologyShares[0] = 1.0;
        Territory.MoveBetweenRegions(a, b, 10, new Coefficients());
        Assert.Equal((0.1 * 90 + 0.9 * 10) / 100, b.Consciousness, 12);
        Assert.Equal(0.9, b.IdeologyShares[0], 12); Assert.Equal(0.1, b.IdeologyShares[2], 12);
        Assert.Equal(0.9, a.Consciousness, 12);   // 送り手の1人あたりは動かない（国民意識の運びは (1193) で国民意識ごと消した）
    }
}
