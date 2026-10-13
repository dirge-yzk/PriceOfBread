using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class FranceTests
{
    // 所得税は19世紀の発明＝行政力でロック。序盤は人頭税が国庫を支える（近世財政）。
    // pollTax 既定0.5＝旧体制の中庸／国営工場テストは国費調達のため最大課税（1.0）で国庫を厚くする。
    private static Scenario Fr(ulong s, double pollTax = 0.5, double treasury = 200_000) => new()
    {
        Seed = s,
        StartYear = 1789,
        Kind = ScenarioKind.France,
        PoliticsEnabled = true,
        InitialPolity = Polity.Absolutist,
        FiscalEnabled = true,
        TaxRate = 0.10,
        InitialPollTax = pollTax,
        InitialTreasury = treasury,
        BaseCoercion = 400,   // 人口正規化後＝人口あたり強度（旧1500は絶対値時代）
        EducationLaw = EducationLaw.Religious,
        ExternalPressure = 0.1,
    };

    [Fact]
    public void 旧体制フランスは100年放置で破綻せず基礎貨幣を保存する()
    {
        var sim = new Simulation(Fr(1789), new NullAgent());
        double base0 = sim.World.BaseMoney.Value;
        var ex = Record.Exception(() => sim.Run(400));   // §11.6（保存・非負・財収支）が全ティックで保持
        Assert.Null(ex);
        // 基礎貨幣の変化＝累積貿易流入（植民地輸入の対外流出を含む）
        Assert.True(Math.Abs(sim.World.BaseMoney.Value - base0 - sim.World.CumulativeTradeFlow.Value - sim.World.CumulativeGoldMinted.Value - sim.World.CumulativeFiatIssued.Value) < 1e-6 * base0, "基礎貨幣保存の破れ");
    }

    [Fact]
    public void 特権身分は移民で流入流出しない()   // 閉鎖階級（世襲）＝移民の対象外。ただし自然増減で人口とともに増減する（子は生まれ死ぬ）。
    {
        var sim = new Simulation(Fr(1789), new NullAgent());
        var r = sim.World.Regions[0];
        double n0 = r.Pops[0].Size, share0 = n0 / r.SumPopulation();   // 貴族・聖職
        sim.Run(200);
        double n1 = r.Pops[0].Size, share1 = n1 / r.SumPopulation();
        Assert.NotEqual(n0, n1);   // 自然増減で人口とともに変化する（世襲固定ではない＝聖職者も増減する）
        Assert.True(System.Math.Abs(share1 - share0) < 0.01,   // 移民で流入流出しないので人口比はほぼ一定（再ラベルで膨張/縮小しない）
            $"特権身分の人口比が移民で動いた（{share0:P2}→{share1:P2}）");
    }

    [Fact]
    public void 国営工場の建設と売却で基礎貨幣を保存し民営化される()   // 建設（国庫→工員）・売却（資本家→国庫）はどちらも移転＝保存
    {
        var sim = new Simulation(Fr(2025, 1.0, 1_000_000), new NullAgent());   // 国費で重工業を建てる絶対王政＝人頭税を最大に引き、国庫も軍資金級に厚く積む（重工業の建設費は素材＝高騰中の鋼/セメントに依存し、立ち上げ期は薄い国庫だと建設中に枯渇して停滞するため）
        sim.World.BaseCoercion = 80; sim.World.Policy.CoercionTarget = 80;   // 産業重視＝警察・軍需を抑え国庫を建設へ回す（治安維持力の人口比例維持費＋軍需 floor が建設を圧迫しないよう低く）
        sim.World.Coef.InfraBudgetScale = 0;   // 建設＋保存の機構を検証＝上下水道(#4)の素材競合（鋼/セメントを奪う）とは直交なので隔離
        sim.World.Coef.ElecInfraBudgetScale = 0;   // 同上＝電化インフラ（送電網/電化製品/鋼を奪う）の素材競合も隔離
        double base0 = sim.World.BaseMoney.Value;
        sim.Run(40);                                   // 助走（経済が回り資本家に貯蓄が貯まる）
        Assert.True(sim.World.BuildFactory(0, Catalog.SteelId), "国営工場（鋼）を着工できない");
        var p = World.FindFactory(sim.World.Regions[0], Catalog.SteelId)!;   // エンジンの着工が掴むのと同じ1本（鋼は坩堝鋼/ベッセマーの2本立て＝World.FindFactory）
        Assert.True(p.ConstructionLeft > 0, "着工後に建設中（ConstructionLeft>0）にならない");
        Assert.Equal(0.0, p.BuiltScale);               // 着工直後はまだ立たない（時間制＝一瞬でない）
        sim.Run(60);                                   // 重工業は最短8四半期だが、建設費が素材価格依存になり、品不足の鋼を建てる間は鋼が高騰し、
                                                       // 一定倍率を超えると建設が一時停止して鋼の供給を待つため、立ち上げ期は完成まで長引く
                                                       // （汽船tier2/装甲艦の素材競合＋RGO効率化の火薬が石炭を奪い鋼/セメントを押し上げる競合で更に伸びる）。
        Assert.True(p.BuiltScale > 0, "建設完了後に BuiltScale が増えない");
        sim.Run(40);                                   // 10年運営（黒字化＋資本家の資金蓄積）
        var sale = sim.World.CanSellFactory(0, Catalog.SteelId);    // 売却可否の判定
        bool sold = sim.World.SellFactory(0, Catalog.SteelId);
        Assert.Equal(sale.Ok, sold);                   // CanSell の判定と実売却が一致
        if (sold) Assert.Equal(0.0, p.BuiltScale);     // 売却＝民営化（BuiltScale=0・以後は市場が伸縮）
        var ex = Record.Exception(() => sim.Run(20));  // §11.6 が全ティック保持
        Assert.Null(ex);
        Assert.True(Math.Abs(sim.World.BaseMoney.Value - base0 - sim.World.CumulativeTradeFlow.Value - sim.World.CumulativeGoldMinted.Value - sim.World.CumulativeFiatIssued.Value) < 1e-6 * base0, "建設・売却で基礎貨幣保存が破れた");
    }

    [Fact]
    public void 国家工場建設は基礎貨幣を保存する()   // 国庫→建設の束（生産者）で保存・工業化度が上がる
    {
        // ★2026-09-26：旧＝1棟単位の手動建設 World.BuildIndustry（テストと PlayHarness だけの動詞）で予約していた。
        //   動詞を削除し、UI・AI 共通の口＝近代化投資のレバー（ApplyModernizationInvestment が毎四半期 StateBuildQueue へ配る）で予約する。
        var sim = new Simulation(Fr(1789), new NullAgent());
        double base0 = sim.World.BaseMoney.Value, ind0 = sim.World.Region.TotalIndustrialization;   // ヴィンテージ2026-07-10：資本ストックは新旧合計（旧単層と同一動学）で測る
        sim.World.ModernizationFrac = 0.5;   // NullAgent は上書きしない＝単一国では毎四半期この率で国庫から建設を予約する
        var ex = Record.Exception(() => sim.Run(20));   // EconomyStep が束を買い決済（§11.6 が全ティック保持）
        Assert.Null(ex);
        Assert.True(sim.World.ModernizationBudget() > 0, "近代化投資の予算が立たない（国庫不足＝建設を予約できていない）");
        Assert.True(System.Math.Abs(sim.World.BaseMoney.Value - base0 - sim.World.CumulativeTradeFlow.Value - sim.World.CumulativeGoldMinted.Value - sim.World.CumulativeFiatIssued.Value) < 1e-6 * base0, "国家建設で基礎貨幣保存が破れた");
        Assert.True(sim.World.Region.TotalIndustrialization > ind0, "国家建設で工業化度が上がっていない");
    }

    [Fact]
    public void 工場補助金は基礎貨幣を保存する()   // 国庫が赤字を肩代わり＝国庫→所有者で保存
    {
        var sim = new Simulation(Fr(1789), new NullAgent());
        double base0 = sim.World.BaseMoney.Value;
        foreach (var p in sim.World.Region.Producers) p.SubsidyRate = new Share(1.0);   // 全工場に満額補助金（赤字工場の肩代わりを発火・2026-07-19率化）
        var ex = Record.Exception(() => sim.Run(20));
        Assert.Null(ex);
        Assert.True(System.Math.Abs(sim.World.BaseMoney.Value - base0 - sim.World.CumulativeTradeFlow.Value - sim.World.CumulativeGoldMinted.Value - sim.World.CumulativeFiatIssued.Value) < 1e-6 * base0, "工場補助金で基礎貨幣保存が破れた");
    }

    [Fact]
    public void 工業化はゆるやかに進むが都市化は史実的な範囲に縛られる()
    {
        var sim = new Simulation(Fr(1789), new NullAgent());
        sim.Run(400);
        var r = sim.World.Regions[0];
        double urban = r.SumUrbanPopulation() / r.SumPopulation();
        Assert.True(r.TotalIndustrialization > 0.3, "工業化が進んでいない");   // ヴィンテージ2026-07-10：新旧合計（旧単層と同一動学＝基準は据え置き）
        Assert.True(urban < 0.6, $"都市化が過剰（{urban:P0}）＝工業化に縛られていない（資本ゲートの破れ）");
    }

    [Fact]
    public void 同一シードでフランスは決定論的()
    {
        var a = new Simulation(Fr(7), new NullAgent());
        var b = new Simulation(Fr(7), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.Equal(a.World.Regions[0].Industrialization, b.World.Regions[0].Industrialization);
        Assert.Equal(a.World.TotalMoney, b.World.TotalMoney);
    }
}
