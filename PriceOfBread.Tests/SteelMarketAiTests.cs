using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 鋼市場AI側の5フラグ（2026-08-16・診断=docs/202608/20260816_鋼市場診断_部分完了.md／実装節=同docの末尾）のON経路。
/// #1 AiIndustrialCeilingFrontier／#2 AiWorldPriceEntry／#3 AiBuildGuardRelax／#4 AiSupplyChainEyes／#5 AiProtectiveTariff。
/// ⚠5本とも2026-08-16の点火梯子で**点火済み**（既定0→#1は0.6・他4本は1.0）＝既定はもう休眠ではない。
/// ここでは**既定値の番人**（＝いま live である値の宣言）と**ON側の挙動**、および各フラグのOFF側（旧挙動）の対照を見る。
/// ⚠対照実験はOFF側もON側も明示的にピンする（pob-ignition-comment-sweep の教訓＝既定に寄りかかった番人は点火で空洞化する）。
/// </summary>
public class SteelMarketAiTests
{
    private static MultiNationSim FourNations()
        => new([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);

    /// <summary>着工の余地を作る（判断そのものは変えない）。TryBuildFacility の**金の律速3本**は全て財政バッファ
    /// （＝月次義務の5か月分）を分母に持つので、Last*義務が未計上のQ0（バッファ=床の1.0）では
    /// 身の丈ゲート（工場費用≤1.0×バッファ）に**どの工場も通らない**＝ON/OFFの差が観測できない。
    /// 行政請求だけを立ててバッファを作り、国庫を <paramref name="treasuryOverBuffer"/> 倍に置く
    /// （1.0以上＝財政逼迫0＝既定ガードも通る／1.0未満＝逼迫>0＝既定ガードで止まる＝#3(b)の対照）。</summary>
    private static void GiveBuildRoom(World w, double treasuryOverBuffer)
    {
        w.LastAdminClaimPerMonth = new MoneyPerMonth(1_000_000);                     // バッファ＝FiscalStressBufferMonths(5)×これ
        w.SetTreasuryRaw(treasuryOverBuffer * UtilityAgent.FiscalBufferOf(w));
    }

    /// <summary>国営工場カウンターに意志が立っている生産者の数（着工の観測窓＝AIもプレイヤーも同じ World.SetStateTarget を通る）。</summary>
    private static int WilledCount(World w)
    {
        int n = 0;
        foreach (var r in w.Regions)
            if (r.State == RegionState.Active)
                foreach (var p in r.Producers) if (p.StateTarget > 0) n++;
        return n;
    }

    /// <summary>その州で「エンジンの動詞が掴む個体」（World.FindFactory＝SetStateTarget と同じ1本）である非役務の生産者。
    /// 代替レシピで同じ財の個体が複数ある州でカウンターが噛み合わない事故を避ける。</summary>
    private static Producer PickBuildable(Region rg)
        => rg.Producers.First(p => Catalog.Goods[p.GoodId].Tier != GoodTier.Service
                                   && !p.Bankrupt && p.ConstructionLeft <= 0
                                   && ReferenceEquals(World.FindFactory(rg, p.GoodId), p));

    /// <summary>その州の参照価格建ての費用錨（TryBuildFacility の分母 rp と同一式）。</summary>
    private static double RefAnchor(Region rg, int goodId) => Catalog.RefPrices[goodId] * Math.Max(rg.PriceLevel, 1e-9);

    // ============ (a) 既定値の番人（5フラグとも点火済み＝いまこの値で live） ============

    [Fact]
    public void 鋼市場AI側の5係数は点火済みの既定値である()
    {
        // 2026-08-16 点火梯子で 0→この値へ点火（合成comp4）。うっかり値が動いたらここが赤になる。
        // ⚠POB_COEF で明示的に上書きした測定走行では見ない（点火A/Bを同一バイナリで取るため）。
        var c = new Coefficients();
        string ov = Coefficients.OverrideSummary();
        if (!ov.Contains("AiSupplyChainEyes", StringComparison.Ordinal)) Assert.Equal(1.0, c.AiSupplyChainEyes);
        if (!ov.Contains("AiWorldPriceEntry", StringComparison.Ordinal)) Assert.Equal(1.0, c.AiWorldPriceEntry);
        if (!ov.Contains("AiIndustrialCeilingFrontier", StringComparison.Ordinal)) Assert.Equal(0.6, c.AiIndustrialCeilingFrontier);
        if (!ov.Contains("AiProtectiveTariff", StringComparison.Ordinal)) Assert.Equal(1.0, c.AiProtectiveTariff);
        if (!ov.Contains("AiBuildGuardRelax", StringComparison.Ordinal)) Assert.Equal(1.0, c.AiBuildGuardRelax);
    }

    [Fact]
    public void 世界価格の注入はフラグが握る()
    {
        // OFF側を明示ピン（点火前の旧挙動＝配列すら確保しない＝World側の読み手も常にnull枝＝恒等）
        var off = FourNations();
        foreach (var n in off.Nations) n.World.Coef.AiWorldPriceEntry = 0.0;
        off.Coef.AiWorldPriceEntry = 0.0;   // 注入は MultiNationSim 側の係数が握る（四半期末ブロック）
        off.Run(2);
        foreach (var n in off.Nations) Assert.Null(n.World.WorldGoodsPrices);

        // ON側も明示ピン（既定は点火1.0だが、番人は既定に寄りかからない）
        var on = FourNations();
        foreach (var n in on.Nations) n.World.Coef.AiWorldPriceEntry = 1.0;
        on.Coef.AiWorldPriceEntry = 1.0;
        on.Run(2);
        Assert.All(on.Nations, n => Assert.NotNull(n.World.WorldGoodsPrices));
    }

    // ============ (b) #1 AiIndustrialCeilingFrontier＝自主停止2.5の世界前線連動 ============

    [Fact]
    public void 前線連動ONで近代化投資の自主停止が世界前線まで開く()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(7), new NullAgent());
        var w = sim.World;
        foreach (var r in w.Regions) r.Industrialization = 3.0;   // 定数包絡2.5を**超えた**世界＝旧実装は投資を完全に止める
        Assert.True(w.MeanIndustrialization >= 2.5,
            $"テスト前提が崩れた：平均工業化度が{w.MeanIndustrialization:F2}（2.5以上が前提＝飽和側で測る）");
        // 旧＝定数2.5＝飽和＝投資ゼロ（プレイヤーは州上限5.0まで踏める非対称＝診断B-4）
        Assert.Equal(0.0, UtilityAgent.AutoModernizationFrac(w, 0));
        w.Coef.AiIndustrialCeilingFrontier = 1.0;
        w.IndustrialFrontier = 2.0;   // 前線が定数2.5に届かない世界＝包絡は定数のまま＝**開幕は挙動不変**（暦年を読まずに時代が創発）
        Assert.Equal(0.0, UtilityAgent.AutoModernizationFrac(w, 0));
        w.IndustrialFrontier = 10.0;  // 前線が伸びた世界＝包絡が開く＝第二幕（追随投資）が始まる
        Assert.True(UtilityAgent.AutoModernizationFrac(w, 0) > 0,
            "前線10でも投資が0＝EffectiveIndCeiling が世界前線を読んでいない");
        w.Coef.AiIndustrialCeilingFrontier = 0;   // 番人：αを戻せば前線が高くても定数2.5＝恒等
        Assert.Equal(0.0, UtilityAgent.AutoModernizationFrac(w, 0));
    }

    // ============ (c) #2 AiWorldPriceEntry＝逆インセンティブの反転 ============

    /// <summary>「安い輸入で国内価格が潰れ、採算も赤字」＝旧AIが**絶対に建てない**状態を作り、世界価格だけを高く置く。
    /// 史実の型＝安い英国鋼が入ってくるからこそ独米は製鉄所を建てた（旧実装はその真逆＝診断B-3）。</summary>
    [Fact]
    public void 世界価格が高ければ自国価格が潰れていても参入する()
    {
        foreach (bool on in new[] { false, true })
        {
            var w = FourNations().Nations[0].World;   // フランス
            GiveBuildRoom(w, 1.5);
            Assert.Equal(0.0, UtilityAgent.FiscalStressOf(w));   // 前提：既定の財政ガード（逼迫0必須）は通る
            Assert.Equal(0, WilledCount(w));                     // 前提：未達の意志ゼロ（旧ガードの全国 return を踏まない）
            var rg = w.Regions[0];
            var pr = PickBuildable(rg);
            double rp = RefAnchor(rg, pr.GoodId);
            rg.Markets[pr.GoodId].Price = 0.5 * rp;   // 自国 relExcess = −0.5 ＜ 参入閾値 0.05
            pr.SmoothedMargin = -0.5;                 // 採算も赤字＝国内チャネルは二重に閉じている
            var wgp = new double[Catalog.Goods.Length];
            wgp[pr.GoodId] = 100.0 * rp;              // 世界の着値だけが高い（他財は0＝世界チャネルは開かない）
            w.WorldGoodsPrices = wgp;
            w.Coef.AiWorldPriceEntry = on ? 1.0 : 0.0;

            new UtilityAgent().DecideQuarter(w);

            if (on) Assert.Equal(1, pr.StateTarget);   // 世界へ売れるレントを読んで参入＝反転
            else Assert.Equal(0, pr.StateTarget);      // 旧＝局所価格しか読まない＝安い輸入ほど建てない
        }
    }

    // ============ (d) #3 AiBuildGuardRelax＝三重ガードの緩和 ============

    /// <summary>(b)財政ガード＝借入と同じ1行（国家の支払い率 r＜1 で止まる・2026-09-28 段2-1）。
    /// 旧＝緩和なしは「国庫がバッファ満額でないと建てない」・緩和ありは「維持費＋利払いを国庫で賄える限り」＝どちらも r の1本になり、財政の門では緩和の旗が効かない
    /// （旗は並行着工の側＝下の番人に残る）。国庫がバッファの半分でも r＝1 なら建ち、r＜1 なら国中どこにも1件も立たない。</summary>
    [Fact]
    public void 財政ガードは国家の支払い率が1未満の月だけ着工を止める()
    {
        foreach (double r in new[] { 0.9, 1.0 })
        {
            var w = FourNations().Nations[0].World;
            GiveBuildRoom(w, 0.5);   // 国庫＝バッファの半分（旧の緩和なしのガードなら止まる額）
            w.FiscalFundingRatio = r;
            var rg = w.Regions[0];
            var pr = PickBuildable(rg);
            rg.Markets[pr.GoodId].Price = 2.0 * RefAnchor(rg, pr.GoodId);   // relExcess=1.0＝不足の深い財
            pr.SmoothedMargin = 0.5;                                        // 採算も黒字＝#2に頼らず旧判断だけで建つ候補

            new UtilityAgent().DecideQuarter(w);

            if (r >= 1) Assert.Equal(1, pr.StateTarget);
            else Assert.Equal(0, WilledCount(w));   // r＜1＝国中どこにも1件も立たない
        }
    }

    /// <summary>(a)並行着工：旧は「全国のどこかに未達の意志があれば**全国** return」＝AI国は世界で同時1件（年4棟）。
    /// 緩和後は意志の残る州だけ静止し、他州は並行して着工する（金の天井＝月次出費の律速は残る）。</summary>
    [Fact]
    public void ガード緩和ONなら州ごとに並行着工する()
    {
        foreach (bool on in new[] { false, true })
        {
            var w = FourNations().Nations[0].World;
            GiveBuildRoom(w, 1.5);
            var active = w.Regions.Where(r => r.State == RegionState.Active).ToList();
            Assert.True(active.Count >= 2, "テスト前提が崩れた：並行着工は多州国でしか観測できない");
            foreach (var rg in active)   // 全州に「不足の深い黒字財」を1つずつ置く＝州ごとの最良が必ず存在する
            {
                var pr = PickBuildable(rg);
                rg.Markets[pr.GoodId].Price = 2.0 * RefAnchor(rg, pr.GoodId);
                pr.SmoothedMargin = 0.5;
            }
            w.Coef.AiBuildGuardRelax = on ? 1.0 : 0.0;

            new UtilityAgent().DecideQuarter(w);

            if (on) Assert.True(WilledCount(w) >= 2, $"並行着工が起きていない（意志の立った生産者={WilledCount(w)}）");
            else Assert.Equal(1, WilledCount(w));   // 旧＝全国で同時1件
        }
    }

    // ============ (e) #4 AiSupplyChainEyes＝連鎖の律速を読む目 ============

    /// <summary>下流の財が「不足していて儲かる」のに、その材料が配給されている（Fill が低い）とき、
    /// 旧AIは下流を建てて詰まりを深くした。目を持つAIは**上流から**建てる（生産タブでプレイヤーが読むのと同じ「供給:n%」）。</summary>
    [Fact]
    public void 連鎖の目ONなら上流の律速財を先に建てる()
    {
        foreach (bool on in new[] { false, true })
        {
            var w = FourNations().Nations[0].World;
            GiveBuildRoom(w, 1.5);
            var rg = w.Regions[0];
            var (down, up) = FindChainPair(w, rg);
            rg.Markets[down.GoodId].Price = 100.0 * RefAnchor(rg, down.GoodId);   // 下流＝全国で突出した不足（eff≈99）
            down.SmoothedMargin = 1.0;                                            // 旧判断でも建つ＝OFF側の対照が成立する
            rg.Markets[up.GoodId].LastRation = 0.0;                               // 上流＝配給0％＝連鎖の律速（Fill=0）
            // 係数5.0＝上流の優先（1+5×(1−Fill)=6）が下流の失業重み上限（1+2×1=3）を必ず上回る＝argmax が決定論
            w.Coef.AiSupplyChainEyes = on ? 5.0 : 0.0;

            new UtilityAgent().DecideQuarter(w);

            if (on)
            {
                Assert.Equal(1, up.StateTarget);     // 律速点から建てる
                Assert.Equal(0, down.StateTarget);   // 詰まった下流は後回し（1四半期1件のまま）
            }
            else
            {
                Assert.Equal(1, down.StateTarget);   // 旧＝価格の高い下流を建てて詰まりを深くする
                Assert.Equal(0, up.StateTarget);
            }
        }
    }

    /// <summary>その州で「下流＝非労働の材を実際に使う生産者」と「その材の生産者」の組を探す（財カタログの変更に追随）。</summary>
    private static (Producer Down, Producer Up) FindChainPair(World w, Region rg)
    {
        foreach (var d in rg.Producers)
        {
            if (Catalog.Goods[d.GoodId].Tier == GoodTier.Service || d.Bankrupt || d.ConstructionLeft > 0) continue;
            if (!ReferenceEquals(World.FindFactory(rg, d.GoodId), d)) continue;
            foreach (var it in ProductionDiag.ExplainCost(w, rg, d.GoodId).Items)
            {
                if (it.IsLabor || !(it.Qty > 1e-9)) continue;
                if (it.GoodId < 0 || it.GoodId >= Catalog.Goods.Length) continue;
                if (Catalog.Goods[it.GoodId].Tier == GoodTier.Service) continue;
                var u = World.FindFactory(rg, it.GoodId);
                if (u is null || u.Bankrupt || u.ConstructionLeft > 0 || ReferenceEquals(u, d)) continue;
                if (Catalog.FactorySpec(it.GoodId).Cost > UtilityAgent.FiscalBufferOf(w)) continue;   // 身の丈ゲートに通る上流だけ
                return (d, u);
            }
        }
        throw new InvalidOperationException("テスト前提が崩れた：首都州に『材料を国内で作れる下流財』が1つも無い");
    }

    // ============ (e2) 眠った生産者の採算は着工の門で読まない（(187) 反証役 vb2#1） ============

    /// <summary>★(187) 眠った生産者（<see cref="EconomyStep.SleepState"/>＝1＝発見の門が0 で操業規模0・産出0）は (186) の眠りの間に採算の平滑が止まる
    /// ＝退場したレシピは退場した月の値で凍る。AI の着工の門（採算&gt;0）はその凍った値を読まない＝動いている個体が赤字の財を、凍った正の採算で建てない。
    /// 対照＝同じ盤面で産出をわずかに残す（眠りの条件から外れる＝今のまま採算を読む）と建つ＝門が読んでいるのは眠りの判定そのもの。
    /// 世界の着値の経路（採算を読まない）と連鎖の目（上流を選ぶ）は外してピンする。</summary>
    [Fact]
    public void 眠った生産者の凍った採算では着工しない()
    {
        foreach (bool asleep in new[] { true, false })
        {
            var w = FourNations().Nations[0].World;   // フランス
            GiveBuildRoom(w, 1.5);
            Assert.Equal(0.0, UtilityAgent.FiscalStressOf(w));   // 前提：財政の門は通る
            Assert.Equal(0, WilledCount(w));                     // 前提：未達の意志ゼロ
            w.Coef.AiSupplyChainEyes = 0.0;                      // 連鎖の目は外す（上流を選ぶと下流の着工が見えない）
            w.WorldGoodsPrices = null;                           // 世界の着値の経路は閉じる（採算の門を通る候補だけを見る）
            Region? rg = null; Producer? pr = null;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var p in r.Producers)
                {
                    if (p.GoodId < 0 || p.GoodId >= Catalog.Goods.Length || Catalog.Goods[p.GoodId].Tier == GoodTier.Service) continue;
                    if (p.Bankrupt || p.ConstructionLeft > 0 || p.BuiltScale > 1e-9 || p.SiteRow >= 0 || Discovery.Gate(r, p) > 0) continue;
                    if (!(Catalog.RefPrices[p.GoodId] > 0) || Catalog.FactorySpec(p.GoodId).Cost > UtilityAgent.FiscalBufferOf(w)) continue;
                    rg = r; pr = p; break;
                }
                if (pr is not null) break;
            }
            Assert.True(pr is not null && rg is not null, "テスト前提が崩れた：発見の門が0 の工場が1つも無い");
            pr!.Scale = 0; pr.Output = asleep ? 0 : 1e-6;   // 眠った個体／対照＝産出を残す（眠りの条件から外れる）
            Assert.Equal(asleep ? 1 : 0, EconomyStep.SleepState(rg!, pr, w.Coef));
            rg!.Markets[pr.GoodId].Price = 100.0 * RefAnchor(rg, pr.GoodId);   // 不足の深さは突出（他の財に負けない）
            pr.SmoothedMargin = 0.5;                                             // 凍った正の採算
            foreach (var q in rg.Producers) if (q.GoodId == pr.GoodId && !ReferenceEquals(q, pr)) q.SmoothedMargin = -0.5;   // 動いている後継は赤字
            new UtilityAgent().DecideQuarter(w);
            int willed = rg.Producers.Where(q => q.GoodId == pr.GoodId).Sum(q => q.StateTarget);
            if (asleep) Assert.Equal(0, willed);                               // 凍った採算を読まない
            else Assert.True(willed >= 1, $"対照で建たない（{willed}）＝盤面が着工できない＝番人が空回りしている");
        }
    }

    // ============ (f) #5 AiProtectiveTariff＝保護関税の反射 ============

    /// <summary>「自国産業が赤字」×「その財の輸入が急増」＝壁を作る（1879年独の鉄と穀物・1861年米モリル関税の型）。
    /// EMAはAI個体の記憶なので**同じエージェント**を2四半期続けて呼ぶ（1回目＝記憶の初期化・2回目＝前年比の判定）。</summary>
    [Fact]
    public void 輸入急増と自国財の赤字で関税目標が上がる()
    {
        foreach (bool on in new[] { false, true })
        {
            var w = FourNations().Nations[0].World;
            w.Tariff = 0.5;                     // 現在の関税＝反射の基点（目標は現在＋TariffReactionMax×α＝0.75）
            w.Coef.AiProtectiveTariff = on ? 1.0 : 0.0;
            int gid = PickBuildable(w.Regions[0]).GoodId;
            foreach (var r in w.Regions)        // 自国産業が押されている（生産規模で加重した平均採算が赤字）
                foreach (var p in r.Producers)
                    if (p.GoodId == gid) { p.SmoothedMargin = -0.5; p.Scale = Math.Max(p.Scale, 1.0); p.Bankrupt = false; }
            double basis = Math.Max(1.0, (0.01 * w.NominalGdpPerMonth).Value);   // 規模の床（名目GDP×0.001）の10倍＝雑音でない輸入
            var imp = new double[Catalog.Goods.Length];
            imp[gid] = basis;
            w.ImportsByGood = imp;

            var agent = new UtilityAgent();
            agent.DecideQuarter(w);                                  // 1回目＝記憶を現在値で初期化（いきなり急増と誤認しない）
            Assert.True(w.Policy.TariffTarget < 0.75 - 1e-9,
                $"記憶の初期化だけで関税が跳ねている（目標={w.Policy.TariffTarget:F3}）");
            imp[gid] = basis * 5.0;                                  // 対EMA比5倍＝急増線1.5を超える
            agent.DecideQuarter(w);                                  // 2回目＝判定

            if (on) Assert.True(w.Policy.TariffTarget >= 0.75 - 1e-9,
                        $"輸入急増×自国赤字なのに壁を作らない（目標={w.Policy.TariffTarget:F3}）");
            else Assert.True(w.Policy.TariffTarget < 0.75 - 1e-9,
                        $"休眠のはずの反射が動いている（目標={w.Policy.TariffTarget:F3}）");
        }
    }

    /// <summary>壁は「自国財」にだけ立てる＝国内に産業の無い財（原料・熱帯産品）の輸入が増えても関税は動かさない。
    /// ＝#5が「輸入急増」単独の反射でないことの番人（貿易/GDP帯を無意味に潰さない）。</summary>
    [Fact]
    public void 自国産業が黒字なら輸入が急増しても関税は動かない()
    {
        var w = FourNations().Nations[0].World;
        w.Tariff = 0.5;
        w.Coef.AiProtectiveTariff = 1.0;
        int gid = PickBuildable(w.Regions[0]).GoodId;
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
                if (p.GoodId == gid) { p.SmoothedMargin = 0.5; p.Scale = Math.Max(p.Scale, 1.0); }   // 黒字＝押されていない
        double basis = Math.Max(1.0, (0.01 * w.NominalGdpPerMonth).Value);
        var imp = new double[Catalog.Goods.Length];
        imp[gid] = basis;
        w.ImportsByGood = imp;

        var agent = new UtilityAgent();
        agent.DecideQuarter(w);
        imp[gid] = basis * 5.0;
        agent.DecideQuarter(w);

        Assert.True(w.Policy.TariffTarget < 0.75 - 1e-9,
            $"自国産業が黒字なのに壁を作った（目標={w.Policy.TariffTarget:F3}）");
    }
}
