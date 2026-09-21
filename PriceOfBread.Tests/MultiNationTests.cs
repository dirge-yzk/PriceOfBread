using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 複数国家の並列シミュレーション（§6.1 国際層）の検証。4国家モデル（仏・日・英・米）が
/// 独立した94財経済を回しつつ共有外交で相互作用する。各国の保存則・決定論・差別化・戦争の作動を確かめる。
/// </summary>
// ⚠2026-08-28：戦闘ログ書式・覇権緊張の2件は ColoniesAreRegions=0 で環境固定＝検証対象は機構（ログ書式・緊張の集中）であって
//   植民地の政治ではない。昇格を入れると4カ国世界の戦争軌道が変わり、200四半期で戦闘が1件も起きない世界線に入る（実測）。
public class MultiNationTests
{
    private static NationConfig[] FourNations() =>
        [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()];

    // ★器 SilverParityFromRatio の ON/OFF 経路（2026-09-07）：注入される WorldSilverPrice が
    //   ON＝「同一市場内の 銀/金鉱 比の生きた Active 州の対数平均 ÷ RefPrice 比 × RefPrices[銀]」の手計算と一致（1e-9）し、
    //   既定0＝旧統計 Market.Price[銀] と同一であること。⚠この係数は sim 側 _coef の読み手＝コンストラクタ coef: で渡すのが正しい
    //   （World.Coef は国ごと別インスタンスで、ここでは読まれない）。
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 銀平価の器は同一市場内の銀金比から世界銀価格を作る(double gate)
    {
        var coef = new Coefficients { SilverParityFromRatio = gate };
        Assert.True(coef.SilverStandardGate > 0, "前提＝SilverStandardGate が live（0なら注入自体が無い）");
        var sim = new MultiNationSim(FourNations(), seed: 1789, coef: coef);
        sim.Run(3);
        double refAg = Catalog.RefPrices[Catalog.SilverId], refAu = Catalog.RefPrices[Catalog.GoldOreId];
        double sum = 0; int cnt = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                if (r.State != RegionState.Active) continue;
                if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
                var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
                if (!(mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) || !(mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9)) continue;   // 器と同じ＝死んだ市場は除外
                double pAg = mAg.Price, pAu = mAu.Price;
                if (!(pAg > 1e-9) || !(pAu > 1e-9)) continue;
                sum += System.Math.Log(pAg / pAu); cnt++;   // 器と同じ＝対数平均
            }
        Assert.True(cnt > 0, "銀と金鉱の両方に価格のある Active 州が無い（世界データが変わった）");
        double refRatio = refAg / refAu;
        double expected = gate > 0 ? System.Math.Exp(sum / cnt) / refRatio * refAg : sim.Market.Price[Catalog.SilverId];
        foreach (var n in sim.Nations)
            Assert.True(System.Math.Abs(n.World.WorldSilverPrice - expected) <= 1e-9,
                $"{n.World.SelfPower?.Name}：WorldSilverPrice={n.World.WorldSilverPrice} 期待={expected}（gate={gate}）");
    }

    [Fact]
    public void 検証ハーネスが指標を算出し合格判定を返す()
    {
        // ハーネス自体のスモークテスト（合格帯は課さない＝経済の較正前でも緑）。算出が壊れないことだけ保証。
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        sim.Run(100);
        var m = Metrics.Compute(sim);
        Assert.True(m.Nations.Count >= 4, $"国が減った（{m.Nations.Count}）");   // ★2026-09-06：E-06 同時独立（SecessionAsWar）／内戦一元化で試合中に国が**増える**のは正常＝下限だけ見る（旧＝厳密4）
        Assert.All(m.Nations, n => Assert.InRange(n.PriceMidFrac, 0.0, 1.0));
        Assert.All(m.Nations, n => Assert.InRange(n.MeanSatisfaction, 0.0, 1.0));
        var checks = Validation.Evaluate(m);
        Assert.NotEmpty(checks);
        Assert.Equal(checks.Count, Validation.Count(checks, Verdict.Pass) + Validation.Count(checks, Verdict.Warn) + Validation.Count(checks, Verdict.Fail));
    }

    [Fact]
    public void マクロ層の健全性が回帰しない()
    {
        // 健全なマクロ層を固定（回帰防止）。価格ミクロの二極化＝コブウェブ×損失吸収の脆さ は既知の構造課題で別扱い。
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        sim.Run(400);
        var m = Metrics.Compute(sim);
        Assert.True(m.IndustrializationSpread >= 0.5, $"工業化の差別化が消えた（{m.IndustrializationSpread:F2}）");
        Assert.True(m.MeanSat >= 0.40, $"満足度が崩壊（平均{m.MeanSat:F2}）");
        Assert.True(m.Wars >= 1, "戦争が起きていない");
        Assert.True(m.TradeVolume > 0.01, "交易が成立していない");
        Assert.True(m.WorldPricesMoved > 10, $"世界価格が動かない（{m.WorldPricesMoved}）");
    }

    [Fact]
    public void 四国は100年放置で破綻せず各国が基礎貨幣を保存する()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        double[] base0 = [.. sim.Nations.Select(n => n.World.BaseMoney)];

        // §11.6 が全国・全ティックで保持（戦費還流・賠償の対外移転を含む）
        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);

        // 第6段6-8（2026-07-28）：断裂の窓の導入で4カ国世界にも分離建国が出るようになった。分離の親は州・貨幣の物理移転（割譲と同型の
        // 両建て記帳）で per-nation の単純恒等式が成立しない＝NationModelTests と同作法で除外（世界全体の番人は leakhunt＝53カ国・分離込みで清潔）。
        for (int i = 0; i < base0.Length; i++)
        {
            var w = sim.Nations[i].World;
            if (w.HadSecession) continue;
            double drift = w.BaseMoney - base0[i] - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued;
            Assert.True(Math.Abs(drift) < 1e-6 * base0[i], $"{w.Region.Name} の保存破れ（ズレ{drift:N2}）");
        }
    }

    [Fact]
    public void 賠償は国家間で相殺し世界全体の基礎貨幣は貿易流入の総和だけ動く()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        double[] base0 = [.. sim.Nations.Select(n => n.World.BaseMoney)];
        double worldBase0 = base0.Sum();
        sim.Run(400);
        // 賠償は一方の CumulativeTradeFlow を減らし他方を増やす＝世界合計では相殺。
        // 残る世界全体の動きは植民地採取費（対外流出）＋鋳造/不換発行（新規貨幣の創造＝GoldMintFraction起動 2026-07-10）＝Σ(累積貿易流入＋鋳造＋発行)に一致する。
        // 第6段6-8（2026-07-28）：4カ国世界にも分離建国が出るようになった。分離の親子は両建て記帳（親側のみ台帳・子の初期基礎貨幣は
        // 自分の基準線）で「開始4カ国のΣ」との単純比較が成立しない＝親子を除外した各国恒等式のΣで検算（世界全体の番人は leakhunt）。
        // 賠償の相殺は残存国どうしの ±CumulativeTradeFlow が各国恒等式に入ることで引き続き検証される。
        double drift = 0;
        for (int i = 0; i < base0.Length; i++)
        {
            var w = sim.Nations[i].World;
            if (w.HadSecession) continue;
            drift += w.BaseMoney - base0[i] - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued;
        }
        Assert.True(Math.Abs(drift) < 1e-3 * worldBase0, "世界全体の保存破れ（分離の親子を除く）");
    }

    [Fact]
    public void 同一シードで多国シミュは決定論的()
    {
        var a = new MultiNationSim(FourNations(), seed: 42);
        var b = new MultiNationSim(FourNations(), seed: 42);
        a.Run(200);
        b.Run(200);
        for (int i = 0; i < a.Nations.Count; i++)
        {
            Assert.Equal(a.Nations[i].World.Region.Industrialization, b.Nations[i].World.Region.Industrialization);
            Assert.Equal(a.Nations[i].World.TotalMoney, b.Nations[i].World.TotalMoney);
        }
        Assert.Equal(a.Diplomacy.Log.Count, b.Diplomacy.Log.Count);
    }

    [Fact]
    public void 各国は独立した経済として産出を持つ()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        sim.Run(200);
        foreach (var n in sim.Nations)
        {
            int producing = n.World.Region.Producers.Count(p => p.Output > 0.5);
            // 期待値再導出（#17・2026-08-08作者許可）：床30は軌道ピン化していた（ナショ0.01加算・植民地州昇格などの
            // 機構的に正しい変更のたびに割れる）。仕様の実体＝「各国が機能する独立経済を持つ」＝破綻検出の床（>12）へ純化
            //（実測の真の崩壊は7＝結晶化の需要収縮で観測済み。軌道ゆらぎの谷は17前後）。
            Assert.True(producing > 12, $"{n.World.Region.Name} の産出財が乏しい（{producing}）");
        }
    }

    [Fact]
    public void 国家モデルは構成が差別化されている()   // 同じカタログでも初期状態が異なる
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        var japan = sim.Nations[1].World.Region;
        var britain = sim.Nations[2].World.Region;
        // 日本＝農業国（前工業・農民多数）、イギリス＝工業先進（初期工業化度あり・労働者多数）
        Assert.Equal(0.0, japan.Industrialization);
        Assert.True(britain.Industrialization > 0.0, "イギリスは工業先進のはず");
        double japanPeasantShare = japan.Pops[1].Size / japan.SumPopulation();
        double britainWorkerShare = britain.Pops[2].Size / britain.SumPopulation();
        Assert.True(japanPeasantShare > 0.6, "日本は農民が多数のはず");
        Assert.True(britainWorkerShare > britain.Pops[1].Size / britain.SumPopulation(), "イギリスは農民より労働者が多いはず");
    }

    [Fact]
    public void 緊張は戦争を生み講和で植民地と賠償が動く()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        int coloniesBefore = sim.Diplomacy.Powers.Sum(p => p.Colonies);   // 清を含む全勢力（列強は清からも奪うため sim 国だけでは増える）
        sim.Run(400);
        Assert.NotEmpty(sim.Diplomacy.Log);   // 英仏のライバル構図＋覇権緊張で戦争が起きる
        Assert.Contains(sim.Diplomacy.Log, l => l.Contains("開戦"));
        // 植民地の総数は保存（戦争は移転のみ）。ただし**独立**で離反した分は帝国の手を離れる（消滅でなく解放）。
        // 戦争割譲は本国**州**を World 間で移すだけ（植民地数は不変＝"州移転"・植民地化しない）。
        int independents = sim.Diplomacy.Log.Count(l => l.Contains("独立"));
        int promoted = sim.Diplomacy.Log.Count(l => l.Contains("州へ昇格"));   // 植民地州昇格（2026-08-09）＝植民地が本国州になる出口（消滅でなく編入）
        int coloniesAfter = sim.Diplomacy.Powers.Sum(p => p.Colonies);
        Assert.Equal(coloniesBefore, coloniesAfter + independents + promoted);
    }

    /// <summary>★戦闘ログの書式の番人（2026-08-10・作者指定「yyyy/mm/dd『イギリスがポルトガルに陸戦で快勝（戦勝点+n）』」）。
    ///
    /// ⚠要点は**本文に日付を書かない**こと：出来事パネルは配信日（情報が届いた日 yyyy/mm/dd）を自分で付けるので、
    /// 本文が年月を持つと日付が2つ並ぶ（旧 "1787/05 陸戦 - イギリスがポルトガルに辛勝（戦況…・10ヶ月目）"）。
    /// 先頭の**年**トークンだけは他の全ログと同じ作法で置く＝UI側（GameViewModel.Refresh.cs）が整数トークンとして剥がすので
    /// 画面には配信日が1つだけ出て、Runner/プローブでは年が読める。旧書式は "1787/05" ＝整数として読めず剥がれなかった。</summary>
    [Fact]
    public void 戦闘ログは日付が二重にならない書式で出る()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789, coef: new Coefficients { ColoniesAreRegions = 0 });
        sim.Run(200);
        var battles = sim.Diplomacy.Log.Where(l => l.Contains("陸戦で") || l.Contains("海戦で")).ToList();
        Assert.NotEmpty(battles);
        foreach (var l in battles)
        {
            Assert.DoesNotContain("/", l);        // 本文に年月を書かない＝配信日と二重にしない
            Assert.DoesNotContain("ヶ月目", l);    // 経過月は出さない（作者指定）
            Assert.DoesNotContain("戦況", l);      // 「戦況 ○○優勢0.02」は廃止＝その月に動いた戦勝点で語る
            Assert.Contains("（戦勝点+", l);       // 増分は勝った側から見て常に正
            int sp = l.IndexOf(' ');
            Assert.True(sp > 0 && int.TryParse(l[..sp], out int y) && y >= 1783, $"先頭が年トークンでない: {l}");
        }
    }

    [Fact]
    public void 国際市場で交易が成立し世界価格が初期値から動く()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        sim.Run(200);
        Assert.Contains(sim.Market.Volume, v => v > 0.01);   // 直近四半期に取引が成立
        int moved = 0;                                        // 価格発見：初期1.0から動いた財
        for (int g = 0; g < sim.Market.Price.Count; g++)
            if (Math.Abs(sim.Market.Price[g] - 1.0) > 0.05) moved++;
        Assert.True(moved > 10, $"世界価格がほとんど動いていない（{moved}財）");
    }

    [Fact]
    public void 国際市場ありでも各国と世界全体が保存する()
    {
        var sim = new MultiNationSim(FourNations(), seed: 7);
        double[] base0 = [.. sim.Nations.Select(n => n.World.BaseMoney)];
        int nOrig = base0.Length;
        var ex = Record.Exception(() => sim.Run(300));
        Assert.Null(ex);   // ランタイムの per-nation 不変量プローブ（毎サブステップ・全国）が保存則を厳密に監視＝分離独立込みでも例外なし＝保存の一次保証
        // 明示的な集計式(per-nation/世界)の検査は**分離独立で国が増えない安定軌道のみ**（1934拡張E4で遺恨/POP調整により seed7が不安定化し分離独立が発生）。
        // 分離独立は親→子への基礎貨幣の内部移転＝プローブは子を birth BaseMoney で再基線化するので保存は閉じる（親−(S+T)/子baseline+S+T）が、
        // ここの集計式は base0(t=0の原国のみ)を初期とするため**子の birth baseline を勘定できず** per-nation も世界も spawn 時は成り立たない
        // （＝式の非対応でありコードの leak ではない。上の Assert.Null が spawn 込みの保存を担保）。安定軌道では従来どおり両式を厳密検査。
        if (sim.Nations.Count == nOrig)
        {
            for (int i = 0; i < nOrig; i++)
            {
                var w = sim.Nations[i].World;
                Assert.True(Math.Abs(w.BaseMoney - base0[i] - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued) < 1e-6 * base0[i], $"{w.Region.Name} 保存破れ");
            }
            // 交易は国家間で相殺＝世界全体の基礎貨幣の動きは累積貿易流入＋鋳造/不換発行（新規貨幣＝GoldMintFraction起動 2026-07-10）の総和に一致（残るは植民地採取費）
            double worldBase1 = sim.Nations.Sum(n => n.World.BaseMoney);
            double worldFlow = sim.Nations.Sum(n => n.World.CumulativeTradeFlow + n.World.CumulativeGoldMinted + n.World.CumulativeFiatIssued);
            Assert.True(Math.Abs(worldBase1 - base0.Sum() - worldFlow) < 1e-3 * base0.Sum(), "世界保存破れ");
        }
    }

    [Fact]
    public void 国際市場ありでも決定論的()
    {
        var a = new MultiNationSim(FourNations(), seed: 99);
        var b = new MultiNationSim(FourNations(), seed: 99);
        a.Run(150);
        b.Run(150);
        for (int g = 0; g < a.Market.Price.Count; g++)
            Assert.Equal(a.Market.Price[g], b.Market.Price[g]);
    }

    [Fact]
    public void 覇権国は孤立国より多く戦争に巻き込まれる()   // §6.1：覇権防止＝大国は周囲の緊張を集め孤立
    {
        // 裁定2026-07-09：マンデート余白で英親軍法廃止（自由党綱領が余白で通過＝歴史的に妥当）→戦争減の新挙動を受理。
        // 旧計測は国名を含む**全ログ行**の比較＝実態は凶作/疫病/鉱山ノイズが大半（46:45の1件差ナイフエッジ）で、余白導入の戦争減により反転していた。
        // 戦争への巻き込まれ（開戦/参戦/勝利/講和の行）だけを数える計測へ更新＝ガード意図（覇権の英＞孤立の日本）は戦争の実数で保持。
        // 2026-07-10更新：収束是正後の軌道では日本が明治化して能動的に戦争へ出る（新基準では望ましい挙動）＝
        // 「孤立国」の前提を保つため日本の議会を無効化（教育・軍拡の綱領駆動を封じる＝TechInstitutionTestsと同じ分離手法）。
        // 機能5（2026-07-28）：屈服判定の陣営戦力化（抑止＝覇権は戦わずに引かせる）＋参戦拒否の緊張代償で、開戦数は
        // 「覇権の対立中心性」の物差しとして反転しうる（英の splendid isolation・二番手どうしの戦争）。ガードの本義＝§6.1
        // ThreatTension（大国であること自体が周囲の緊張を集める）を**累積緊張の積分**で直接測る形へ更新（3度目の物差し是正）。
        var sim = new MultiNationSim(FourNations(), seed: 1789, coef: new Coefficients { ColoniesAreRegions = 0 });
        sim.Nations[1].World.ParliamentEnabled = false;
        double britTension = 0, japanTension = 0;
        var brit = sim.Diplomacy.Powers.First(p => p.BaseName == "イギリス");
        var japan = sim.Diplomacy.Powers.First(p => p.BaseName == "日本");
        for (int q = 0; q < 400; q++)
        {
            sim.StepQuarter();
            foreach (var p in sim.Diplomacy.Powers)
            {
                if (p != brit) britTension += sim.Diplomacy.Tension(brit, p);
                if (p != japan) japanTension += sim.Diplomacy.Tension(japan, p);
            }
        }
        Assert.True(britTension > japanTension, $"覇権の英（累積緊張{britTension:F0}）が孤立の日本（{japanTension:F0}）より周囲の緊張を集めるはず");
    }

    // ===== 近代化投資の裁量（2026-08-10 是正の番人）=====
    // 背景：Simulation.QuarterStart が**プレイヤー国を含む全国**へ毎四半期 AutoModernizationFrac を代入していた。
    //       AI国は直後の DecideQuarter が自分の値で上書きするので無害だったが、NullAgent のプレイヤー国には上書きが来ない＝
    //       財政タブのスライダーで動かしても次の四半期に必ず自動値へ戻る＝レバーが構造的に効かなかった。
    // 判定は Power.IsPlayer（UI と Runner の warmtest だけが立てる）＝自己対戦・較正・テストは一度も立てない＝AI挙動はビット不変。
    [Fact]
    public void 近代化投資はプレイヤー国だけ毎期の自動値に上書きされない()
    {
        var cfgs = FourNations();
        var playerCfg = cfgs[0];
        var sim = new MultiNationSim(cfgs, 1789UL, cfg => ReferenceEquals(cfg, playerCfg) ? new NullAgent() : new UtilityAgent());
        sim.Diplomacy.Powers[0].IsPlayer = true;   // UI（GameViewModel）と同じ印

        var player = sim.Nations[0].World;
        var ai = sim.Nations[1].World;
        const double Lever = 0.4242;   // UIのスライダーで置いた値。AIが取りうる範囲（≤0.15）と衝突しない目印
        player.ModernizationFrac = Lever;
        ai.ModernizationFrac = Lever;

        sim.Run(8);   // QuarterStart を8回通る

        Assert.Equal(Lever, player.ModernizationFrac, 9);    // プレイヤーの裁量が残る（是正の本体）
        Assert.NotEqual(Lever, ai.ModernizationFrac, 9);     // AI国は従来どおり毎期自分の値で上書きされる
        Assert.InRange(ai.ModernizationFrac, 0.0, 0.15);     // 上書き先はAIのスタンス別の率（0.08/0.15）の範囲＝自動設定が生きている
    }

    [Fact]
    public void プレイヤー印の無い世界では近代化投資が全国自動のまま()
    {
        // AI挙動不変の直接確認：IsPlayer を**一度も立てない**（＝自己対戦・較正と同じ条件）なら、
        // NullAgent の国も含めて従来どおり毎期 AutoModernizationFrac に上書きされる。
        var cfgs = FourNations();
        var nullCfg = cfgs[0];
        var sim = new MultiNationSim(cfgs, 1789UL, cfg => ReferenceEquals(cfg, nullCfg) ? new NullAgent() : new UtilityAgent());

        var nullAgentNation = sim.Nations[0].World;
        const double Lever = 0.4242;
        nullAgentNation.ModernizationFrac = Lever;

        sim.Run(8);

        Assert.NotEqual(Lever, nullAgentNation.ModernizationFrac, 9);   // 印が無い＝除外されない＝旧挙動のまま
    }

    /// <summary>#15（2026-09-11）：国力の豊かさ項は WealthParts（正典が内訳を公開）と同順の左結合和＝内訳の計器が「/12000・×9・×12」を書き写さない。</summary>
    [Fact]
    public void 国力の豊かさは内訳の和と厳密に一致する()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        sim.Run(2);
        foreach (var n in sim.Nations)
        {
            var (gdp, ind, sat, col) = NationalScore.WealthParts(n.World);
            Assert.Equal(gdp + ind + sat + col, NationalScore.Wealth(n.World));
            Assert.True(gdp >= 0 && ind >= 0 && sat >= 0 && col >= 0, $"{n.World.SelfPower?.Name}：内訳に負がある（{gdp}/{ind}/{sat}/{col}）");
        }
    }
}
