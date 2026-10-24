using System.Linq;
using PriceOfBread.SimCore;
using Xunit;
using static PriceOfBread.Tests.StartWarmupShared;

namespace PriceOfBread.Tests;

/// <summary>
/// 開始前の助走（作者裁定2026-08-10「助走をプレイヤーの世界と自己対戦バッチで統一し、延長する」）の番人。
/// <para>守るべき性質は3つ：</para>
/// <para>①**暦が1四半期も進まない**＝助走を1年にしようが10年にしようが開始は 1783/Q1・Tick0 のまま。
///   ここが崩れると 1914年の忠実度参照断面（Q524）・全長 GameQuarters・技術の年ゲート（TechOnset）が丸ごとずれる
///   （2026-08-10以前は実際にずれていた＝助走4四半期でエンジン暦だけ1784年になり、表示暦1783と1年の乖離があった）。</para>
/// <para>②**時間の経過そのものである量を伴わせない**＝人口・識字は助走の長さに依存しない
///   （「10年ぶんの人口成長が時間経過なしに起きる」不整合を作らない）。</para>
/// <para>③**入口が1本**＝App も自己対戦バッチも <see cref="MultiNationSim.StartWarmup"/> だけを通り、同じ盤面から始まる。</para>
/// <para>⚠これは ON 経路のテスト（助走を実際に回して結果を見る）＝「何もしない機構は必ず緑」の轍を踏まない。</para>
/// </summary>
/// ★2026-10-17（開発の高速化）：番人を4つのクラスへ分けた（xUnit はクラスごとに並列）。共有の道具はここ。
internal static class StartWarmupShared
{
    internal static MultiNationSim Build(ulong seed = 1789UL)
        => new(WorldData.Nations(), seed, _ => new UtilityAgent());

    /// <summary>★(388) 開幕の国庫と国債の規則（world.json の treasuryMonths／govDebtToGdpYear）を外し、絶対額（国庫 20,000・英仏蘭は国債 500,000）を置いた盤面
    /// ＝規則のない国の旧の経路（助走が国庫と国債を動かさない）を、何もしない機構にならない非零の値で見る。規則のある国の置き方は OpeningFiscalRuleTests が見る。</summary>
    internal static MultiNationSim BuildRuleless(ulong seed = 1789UL)
    {
        var cfgs = WorldData.Nations().Select(c =>
        {
            var x = c.ShallowClone(); x.TreasuryMonths = 0; x.GovDebtToGdpYear = 0; x.Treasury = 20_000;
            x.GovDebt = c.Name is "イギリス" or "フランス" or "ネーデルラント" ? 500_000 : 0;
            return x;
        }).ToArray();
        return new(cfgs, seed, _ => new UtilityAgent());
    }
}

public class StartWarmupTests
{

    [Theory]
    [InlineData(4)]    // 既定＝1年
    [InlineData(20)]   // 5年
    [InlineData(40)]   // 10年
    public void 助走は暦を一切進めない(int quarters)
    {
        var sim = Build();
        sim.StartWarmup(null, quarters);
        foreach (var n in sim.Nations)
        {
            Assert.Equal(Calendar.StartYear, n.World.Year);   // 1783年のまま
            Assert.Equal(1, n.World.Quarter);                 // Q1のまま
            Assert.Equal(0L, n.World.Tick);                   // 経過日数0のまま
        }
    }

    [Fact]
    public void 助走の長さを変えても人口と識字は動かない()
    {
        // 助走で動いてよいのは価格・在庫・生産・財政＝実勢。人口と識字は「年月が経って積み上がる量」なので
        // 助走の長さに依存してはならない（依存したら、10年の助走で1793年の人口の国が1783年として始まる）。
        static (double Pop, double Lit) Board(int quarters)
        {
            var sim = Build();
            if (quarters > 0) sim.StartWarmup(null, quarters);
            double pop = 0, lit = 0;
            foreach (var n in sim.Nations)
            {
                pop += n.World.TotalPopulation;
                foreach (var r in n.World.Regions)
                    foreach (var p in r.Pops) lit += p.Literacy * p.Size;
            }
            return (pop, lit);
        }
        var w4 = Board(4);
        var w20 = Board(20);
        var w40 = Board(40);
        Assert.Equal(w4.Pop, w20.Pop, 6);
        Assert.Equal(w4.Pop, w40.Pop, 6);
        Assert.Equal(w4.Lit, w20.Lit, 6);
        Assert.Equal(w4.Lit, w40.Lit, 6);
    }

}

/// <summary>★2026-10-17（開発の高速化）：xUnit はクラスごとに並列＝重い番人をクラスへ分けた（中身は変えない）。</summary>
public class StartWarmupTests2
{
    [Fact]
    public void 助走は資本を一切育てない()
    {
        // ★作者裁定2026-08-10 追加分「価格のみ試走することはできない？　問題は開始時の振動なので」。
        //   助走を延ばすと開始工業化が 0.31→1.5→2.3 と膨らんでいた＝価格が均衡から遠いせいで見かけの利潤が巨大化し
        //   投資が暴走した産物＝**取り除きたい振動そのものを開始盤面に焼き込んでいた**。よって助走中は資本形成を凍結する。
        //   物差しは「助走なし」の盤面と**厳密に同じ**であること（増えないだけでなく、減価で痩せもしない＝凍結は両側）。
        static (double Ind, double Built, double Grid) Capital(int quarters)
        {
            var sim = Build();
            if (quarters > 0) sim.StartWarmup(null, quarters);
            double ind = 0, built = 0, grid = 0;
            foreach (var n in sim.Nations)
                foreach (var r in n.World.Regions)
                {
                    ind += r.TotalIndustrialization; grid += r.GridCapacity;
                    foreach (var p in r.Producers) built += p.BuiltScale;
                }
            return (ind, built, grid);
        }
        var bareCap = Capital(0);
        foreach (int q in (int[])[4, 20, 40])
        {
            var w = Capital(q);
            Assert.Equal(bareCap.Ind, w.Ind, 6);      // 工業化度（新旧ヴィンテージ合計）
            Assert.Equal(bareCap.Built, w.Built, 6);  // 建った工場・作付面積の恒久容量
            Assert.Equal(bareCap.Grid, w.Grid, 6);    // 送電容量
        }
    }

    /// <summary>★助走は国債の残高を動かさない＋開幕の国債は1783年の史実（2026-09-29）。国債は「年月が経って積み上がる/減る量」（人口・資本と同じ）
    /// ＝助走の余剰で返さない・赤字で借りない・不履行へ数えない・利子の未払いを元本へ繰り入れない（旧＝仏の米独立戦争債務 年GDP の約0.76 が助走の5年の余剰で返され、開幕に0.04 へ痩せた）。
    /// ①全ての国の国債・対外債・保有の和が助走なしの盤面と同じ ②開幕（既定の助走の後）の国債÷年名目GDP＝英 0.9〜1.4（1783 約2.4億ポンド÷GDP 約2億）・
    /// 仏 0.45〜0.8（米独立戦争の後・1789 0.6〜0.8）・蘭 1.2〜2.2（ホラント州を主とする約6億ギルダー）。
    /// 突然変異：助走の減債の門を外す→①②が赤／英の初期国債を0に戻す→②が赤／助走の繰り入れの門を外す→①が赤（2026-09-29 (109)＝1783年の税の段で米の国債が助走で +0.023）。</summary>
    [Fact]
    public void 助走は国債を動かさず開幕の英の国債は年GDPの約1倍()
    {
        // ★(388) ①は規則のない盤面（旧の経路）で見る。規則のある既定の盤面では助走が歳入×月数と比×年GDP で置く（設計どおり）ので ② は既定の盤面で読む。
        var bare = BuildRuleless();
        var warmed = BuildRuleless();
        warmed.StartWarmup();
        static double Held(World w) { double h = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) h += p.BondHoldings.Value; return h; }
        for (int i = 0; i < bare.Nations.Count; i++)
        {
            var a = bare.Nations[i].World; var b = warmed.Nations[i].World;
            Assert.Equal(a.GovDebt.Value, b.GovDebt.Value);
            Assert.Equal(a.ForeignDebt, b.ForeignDebt);
            Assert.Equal(Held(a), Held(b), 6);
        }
        Assert.True(bare.Nations.Any(n => n.World.GovDebt.Value > 0), "前提が崩れた（動かさない主張が空洞＝国債が全て0）");
        warmed = Build();
        warmed.StartWarmup();
        var names = WorldData.Nations().Select(n => n.Name).ToList();
        double Ratio(string name) { var w = warmed.Nations[names.IndexOf(name)].World; return w.GovDebt.Value / Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value; }
        Assert.InRange(Ratio("イギリス"), 0.9, 1.4);
        Assert.InRange(Ratio("フランス"), 0.45, 0.8);
        Assert.InRange(Ratio("ネーデルラント"), 1.2, 2.2);
    }

    /// <summary>★助走は国庫を動かさず、開幕の国庫は史実の桁（2026-09-29）＝国庫も国債・人口・資本と同じ「年月が経って積み上がる量」。
    /// 旧＝開幕の歳入が請求の約2〜4倍（初期の人頭税の最上段と所得税10%）の余剰を助走の5年に積み、world.json の初期値（英 55万・普 19万ほか）と合わせて
    /// 開幕の国庫が年GDP の約0.8〜1.3（清 1.27・蘭 4.7）＝史実（年GDP の数%）の数十倍＝英がナポレオン戦争を蓄えで払い国債が史実の半分だった。
    /// ①全ての国で助走の後の国庫＝助走なしの盤面の国庫（1ビットも動かない）②開幕の国庫÷年の名目GDP＝英 1〜6%（手元は年の歳出の数か月分）・
    /// 仏 2% 未満（1788年8月の支払い停止）・普 20〜40%（フリードリヒ2世の戦時財宝 約5,000万ターラー＝年の歳入の2年分強）・清 2〜8%（乾隆末の戸部の銀庫 約7,000万両）・
    /// 普を除く全ての国で10% 以下。
    /// ★(392) 普・清の帯と〈普を除く 10% 以下〉は記録へ移した（史実の桁の固定＝計器か記録の層）。代わりに③全ての国で開幕の国庫が正（不変条件）。
    /// 突然変異：助走の四半期ごとの戻しを外す→①が赤／英の初期の国庫を旧の 550,000 へ→②が赤／RebaseOpeningFiscal の規則の枝を外す（全ての国が旧の戻し＝国庫 0）→③が赤（(392)）。</summary>
    [Fact]
    public void 助走は国庫を動かさず開幕の国庫は年GDPの数パーセント()
    {
        // ★(388) ①は規則のない盤面（旧の経路）で見る。規則のある既定の盤面では助走の終わりに歳入×月数で置く（設計どおり）ので ② は既定の盤面で読む。
        var bare = BuildRuleless();
        var warmed = BuildRuleless();
        warmed.StartWarmup();
        for (int i = 0; i < bare.Nations.Count; i++)
            Assert.Equal(bare.Nations[i].World.Treasury.Value, warmed.Nations[i].World.Treasury.Value);
        Assert.True(bare.Nations.All(n => n.World.Treasury.Value > 0), "前提が崩れた（動かさない主張が空洞＝国庫が0）");
        warmed = Build();
        warmed.StartWarmup();
        var names = WorldData.Nations().Select(n => n.Name).ToList();
        double Ratio(int i) { var w = warmed.Nations[i].World; return w.Treasury.Value / Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value; }
        double R(string name) => Ratio(names.IndexOf(name));
        // ★(516) 合流：英（1〜6%）・仏（2% 未満）の帯も記録へ移した＝史実の桁の固定（作者 2026-10-17＝史実の桁を固定したテストの赤は止める線ではない・テストは計器か記録の層）。
        //   合成の木（spi2＋spa2＋hz6＋wc0＋pv2〔仏の州アジャクシオ〕）で仏 0.0202 が帯 0.02 の外へ出た（measurements/m516_20261023_opening_treasury.txt）。値は R(…) で読める。
        // ★(392) 合流（反証役 bk〔bv〕中#1）：普・清の帯と〈普を除く全ての国で 10% 以下〉のループは記録へ移した（実装ログ (392)・measurements/m392_20261017_opening_treasury.txt）。
        //   (388) で下限を e7 の入力の月数（普 28・清 2.5）から逆算して広げた＝入力をそのまま検算する同語反復・史実の桁を固定したテストは計器か記録の層（作者 2026-10-17）。
        // 残す不変条件：既定の盤面では助走の後に全ての国の国庫が正（規則〈歳入×月数〉が歳入 0 の国で旧の戻し＝world.json の国庫 0 へ落ちる穴を見る＝反証役 bk〔bv〕低#4）。
        for (int i = 0; i < names.Count; i++)
            Assert.True(warmed.Nations[i].World.Treasury.Value > 0, $"{names[i]} の開幕の国庫が {warmed.Nations[i].World.Treasury.Value}（規則の月数の置き方が歳入 0 で旧の戻しへ落ちた）");
    }

    /// <summary>★1783年の税の段（2026-09-29 (109)）＝開幕の歳入の水準は史実の桁。
    /// 旧＝全61カ国が所得税10%（段の格子の外）と人頭税の最上段で始まり、開幕の歳入が年GDP の15〜37%（清 36.6%・蘭 21.3%）。
    /// 史実の1780年代の中央政府の歳入÷GDP＝英 約12%（関税と消費税が主・所得税は1799年から）・仏 約8%・蘭 約10%・西 約6〜7%・墺・普 約6〜8%・露 約5〜6%・清 約2〜3%。
    /// ①構築の盤面（助走の前）＝全ての国で所得税0（1783年に所得税を持つ国は無い＝英1799・普1811/20・墺1849・日1887）・人頭税と土地税は world.json の段どおり
    ///   （英・清は人頭税0＝英の人頭税は1698年が最後・清は1720年代の攤丁入畝で地丁銀へ畳んだ／露は土地税0＝1724年の人頭税が直接税の柱）。
    /// ②開幕（既定の助走の後）の歳入÷年名目GDP＝欧州の6カ国（英・仏・蘭・西・墺・普）が 4〜14%・全ての国で25% 以下。
    /// ③開幕の直接税（歳入−関税−専売）÷年名目GDP＝清 0.5〜3%（地丁銀 約3,000万両＝GDP の約1.5〜2%）。
    /// ⚠消費税（専売）は法の段でなく AI の生産の決断＝開幕は教義の広さ（段1〜2＝史実の物品税の財〔酒・煙草・茶・コーヒー・石鹸〕と奢侈品）で年GDP の2〜8%（役務を含まない GDP で割る＝アジアの国で大きい）＝②の上限はこの分を含む。
    /// ★蘭の上限を 14% へ戻した（2026-09-30 (127)＝歳入の構成を史実から）：(119)〜(122) は 18〜20% に緩めていた＝助走の中の払えない請求（助走は借りない）に AI の逼迫の応答が
    ///   専売を日用品の全てまで広げた（段3＝専売 年GDP の約14%）。新＝梯子は史実の物品税の財から・助走は教義の広さのまま（広げた記憶を積まない）＝開幕の蘭は段2。
    /// 突然変異：所得税を旧の10% へ→①が赤（①の所得税0 の行を外した写しで②も赤＝欧州の国が 14.9%）／world.json の段を読まない（全ての国を既定の段）→①が赤
    /// （③は清を既定の段にしても帯の内側＝段の取り違えは①が持つ・③は地丁銀の桁の錨）。</summary>
}

/// <summary>★2026-10-17（開発の高速化）：xUnit はクラスごとに並列＝重い番人をクラスへ分けた（中身は変えない）。</summary>
public class StartWarmupTests3
{
    [Fact]
    public void 開幕の歳入は所得税なしの直接税の段で年GDPの史実の桁()
    {
        var cfgs = WorldData.Nations();
        var names = cfgs.Select(n => n.Name).ToList();
        var bare = Build();
        for (int i = 0; i < names.Count; i++)
        {
            var w = bare.Nations[i].World;
            Assert.Equal(0.0, w.TaxRate.Value);
            Assert.Equal(Government.TaxValue(PolicyAxis.PollTax, cfgs[i].PollTaxStage), w.PollTax);
            Assert.Equal(Government.TaxValue(PolicyAxis.LandTax, cfgs[i].LandTaxStage), w.LandTax);
        }
        World B(string nm) => bare.Nations[names.IndexOf(nm)].World;
        Assert.Equal(0.0, B("イギリス").PollTax); Assert.Equal(0.0, B("清").PollTax); Assert.Equal(0.0, B("ロシア").LandTax);
        Assert.True(B("フランス").PollTax > 0 && B("フランス").LandTax > 0, "仏のタイユ・カピタシオンとヴァンティエームが無い");

        var sim = Build();
        sim.StartWarmup();
        double Rev(int i) { var w = sim.Nations[i].World; return w.LastRevenuePerMonth / w.NominalGdpPerMonth; }   // 月次÷月次＝年の比
        double Direct(int i)
        {
            var w = sim.Nations[i].World;
            double mon = 0; foreach (var r in w.Regions) if (r.State == RegionState.Active) mon -= r.StateSpent[(int)TreasuryCat.StateMonopoly];
            double tar = System.Math.Max(0, w.LastTariffRevenuePerClearing) / w.Coef.MonthsPerClearing;
            return (w.LastRevenuePerMonth.Value - mon - tar) / w.NominalGdpPerMonth.Value;
        }
        // ★(400) 2026-10-18：英・蘭は開幕の専売の段をデータで持つ（OpeningStateMonopolyTier＝3・旗 OpeningFiscalAlign は (405) で焼き込み済み）＝専売益が歳入に入り、開幕の歳入が年GDP の 0.14 を超えうる
        //   （手元の出典の幅は約 6〜12%・2〜4 倍は記録で NG ではない）。史実の桁を固定した細い帯は止める線ではない（README 2026-10-17）＝専売の段をデータで持つ国は上限を 0.25（下の全国の上限と同じ）へ。他の 4 国は従来の 0.14。
        var six = new[] { "イギリス", "フランス", "ネーデルラント", "スペイン", "オーストリア", "プロイセン" };
        var revs = string.Join("・", six.Select(nm => $"{nm} {Rev(names.IndexOf(nm)):P1}"));
        foreach (var nm in six)
        {
            int ix = names.IndexOf(nm);
            double hi = cfgs[ix].OpeningStateMonopolyTier > 0 ? 0.25 : 0.14;   // ★蘭も 0.14 だった（2026-09-30 (127)・(119)(122) の 0.18→0.20 を戻した＝上の注記）
            // ★(542) 合流（反証役 ivf〔bv〕低〜中）：6 国の下限の帯 4% は記録へ移した（(536) は 2% へ緩めていた＝緩めるのは記録へ移すのと違う・(392)(516) と同じ扱い）。
            //   F1・F3 の点火でスペインが 3.9%（ivf の木）。値は revs と実装ログ (542) の記録に残す。残す不変条件＝開幕の歳入が正。
            Assert.True(Rev(ix) > 0 && Rev(ix) <= hi, $"{nm} の開幕の歳入が年GDP の {Rev(ix):P1}（上限 {hi:P0}）：{revs}");
        }
        for (int i = 0; i < names.Count; i++)
            Assert.True(Rev(i) <= 0.25, $"{names[i]} の開幕の歳入が年GDP の {Rev(i):P1}（旧＝清 36.6%）");
        Assert.InRange(Direct(names.IndexOf("清")), 0.005, 0.03);
    }

    /// <summary>★助走は法を動かさない（2026-09-30 (119)・(109) の申し送り3）＝開幕の法は world.json と史実の初期値のまま始まる。
    /// 法の施行は「年月をかけて起きること」（国庫・国債・人口・資本と同じ門）＝施行（<see cref="Government.Step"/>）は助走で走らない。
    /// 旧＝政治だけを止め AI の目標と施行は回っていた＝5年の助走で所得税 0→0.02〜0.05・土地税の最上段 0.4→0.32・金利 0.25→0.02〜0.24・関税・官僚制・秘密投票・
    /// 徴兵法・教育法・穀物法・国教・通貨（不換→銀）が動いた（seed 1789 で61カ国の全て）。
    /// ①全ての国で、法の値（段の法の表・税と支出の段の表・陸海の維持費・関税・金利・行政費・治安維持力・福祉水準・年金・法に追従する実労働時間と賃金保持と児童労働の合法度）が
    ///   助走の前後で1ビットも変わらない（表を読む＝軸を足しても番人から漏れない）。
    /// ②開幕の通貨は英＝金・他の国＝銀（1783年の銀の世界＝データ。旧は不換で始まり、助走の最初の四半期に AI が銀へ上げていた）。
    /// ③導出量は助走でも導出する＝英（国債 年GDP の約1.1倍）の緊縮の強度と全ての国の政治資本の最大量が開幕に0でない（施行と一緒に導出まで止めると構築の0のまま）。
    /// 突然変異：施行の門を外す→①が赤／通貨の既定を不換へ戻す→②が赤／門を導出の前へ上げる→③が赤。</summary>
    [Fact]
    public void 助走は法を動かさない()
    {
        static System.Collections.Generic.List<(string Name, long Bits)> Laws(World w)
        {
            var v = new System.Collections.Generic.List<(string, long)>();
            for (int k = 0; k < Government.LevelLaws.Length; k++) v.Add(($"{Government.LevelLaws[k].Axis}#{k}", Government.LevelLaws[k].Get(w)));
            foreach (var law in Government.RateLaws) v.Add((law.Axis.ToString(), System.BitConverter.DoubleToInt64Bits(law.Get(w))));
            void D(string name, double x) => v.Add((name, System.BitConverter.DoubleToInt64Bits(x)));
            // ★(231) 陸海の維持費の比は外す＝助走の直後に開幕の軍の確定（MultiNationSim.OpenForces：艦の播種〔OpeningNavalShares が陸海の比を置く〕）が置く（助走の動きではなく開幕の置き方・OpeningForcesTests が式と突き合わせる）。治安維持力は (233) で戻した（開幕の導出 C を差し戻した）
            D("関税", w.Tariff); D("金利", w.InterestRate); D("行政費", w.AdminSpending);
            D("治安維持力", w.BaseCoercion); D("福祉水準", w.SocialSecurity); D("実労働時間", w.WorkHoursValue); D("賃金保持", w.WomenWageRetention); D("児童労働の合法度", w.ChildLaborLegal);
            v.Add(("年金", (long)w.Pension));
            return v;
        }
        var sim = Build();
        var before = sim.Nations.Select(n => Laws(n.World)).ToArray();
        sim.StartWarmup();
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            var after = Laws(w);
            for (int k = 0; k < after.Count; k++)
                Assert.True(before[i][k].Bits == after[k].Bits, $"{w.SelfPower?.Name} の {after[k].Name} が助走で動いた（{before[i][k].Bits}→{after[k].Bits}）");
            Assert.Equal(w.SelfPower?.Name == "イギリス" ? MonetaryStandardLaw.Gold : MonetaryStandardLaw.Silver, w.MonetaryLaw);
            Assert.True(w.PoliticalCapitalMax > 0, $"{w.SelfPower?.Name} の政治資本の最大量が助走で導出されていない");
        }
        var uk = sim.Nations.First(n => n.World.SelfPower?.Name == "イギリス").World;
        Assert.True(uk.AusterityIntensity > 0.5, $"英の緊縮の強度が助走で導出されていない（{uk.AusterityIntensity}）");
    }

    [Fact]
    public void 助走は建材の需要を殺さない()
    {
        // 資本形成を止めるときに「建てる需要と支払い」まで止めると、鋼・機械・セメント・製材の4財だけ需要が抜けて
        // 均衡が下振れし、助走が**間違った価格**へ収束する（作者の懸念）。需要が生きている＝価格が床へ落ちないことで確かめる。
        var matCoef = new Coefficients();
        var sim = Build();
        sim.StartWarmup(null, 20);
        foreach (int gid in (int[])[matCoef.SteelGoodId, matCoef.MachineGoodId, matCoef.CementGoodId, matCoef.LumberGoodId])
        {
            int alive = 0;
            foreach (var n in sim.Nations)
            {
                var r = n.World.Region;
                if (gid >= r.Markets.Length) continue;
                double rp = 0; foreach (var g in r.Goods) if (g.Id == gid) { rp = g.RefPrice; break; }
                if (rp > 1e-9 && r.Markets[gid].Price / rp > 0.3) alive++;   // 参照価格の3割超＝需要が生きている
            }
            Assert.True(alive > sim.Nations.Count / 2,
                $"{Catalog.Goods[gid].Name}の建設需要が助走で消えている（{alive}/{sim.Nations.Count}カ国のみ生存）");
        }
    }

    [Fact]
    public void 助走ゼロは素の構築直後と厳密恒等()
    {
        // 係数を0にしたら助走は丸ごと走らない（技術の助走も基準線の取り直しもしない）＝
        // 2026-08-10以前のバッチへ戻れる逃げ道。帯が動いたときの二分探索の基線なので、恒等でなければ意味がない。
        var bare = Build();
        var warmed = Build();
        warmed.StartWarmup(null, 0);
        for (int i = 0; i < bare.Nations.Count; i++)
        {
            var a = bare.Nations[i].World; var b = warmed.Nations[i].World;
            Assert.Equal(a.Year, b.Year);
            Assert.Equal(a.Quarter, b.Quarter);
            Assert.Equal(a.Tick, b.Tick);
            Assert.Equal(a.TotalPopulation, b.TotalPopulation, 9);
            Assert.Equal(a.TechLevel, b.TechLevel, 12);
            for (int g = 0; g < a.Region.Markets.Length; g++)
                Assert.Equal(a.Region.Markets[g].Price, b.Region.Markets[g].Price, 12);
        }
        // 助走ゼロの世界を本編で走らせても、素の世界とビット単位で同じ軌道を描く（凍結フラグが状態を汚していない）。
        for (int q = 0; q < 4; q++) { bare.StepQuarter(); warmed.StepQuarter(); }
        for (int i = 0; i < bare.Nations.Count; i++)
        {
            Assert.Equal(bare.Nations[i].World.GdpPerMonth.Value, warmed.Nations[i].World.GdpPerMonth.Value, 12);
            Assert.Equal(bare.Nations[i].World.TotalPopulation, warmed.Nations[i].World.TotalPopulation, 9);
        }
    }

}

/// <summary>★2026-10-17（開発の高速化）：xUnit はクラスごとに並列＝重い番人をクラスへ分けた（中身は変えない）。</summary>
public class StartWarmupTests4
{
    [Fact]
    public void 助走は二度目の呼び出しで何もしない()
    {
        // 入口が1本である以上、誤って2度呼ばれても盤面が二重に助走してはならない（ロード経路・診断からの再入）。
        var once = Build();
        once.StartWarmup();
        var twice = Build();
        twice.StartWarmup();
        twice.StartWarmup();
        for (int i = 0; i < once.Nations.Count; i++)
        {
            var a = once.Nations[i].World; var b = twice.Nations[i].World;
            for (int g = 0; g < a.Region.Markets.Length; g++)
                Assert.Equal(a.Region.Markets[g].Price, b.Region.Markets[g].Price, 12);
        }
    }

    [Fact]
    public void 助走は決定論的で助走後の本編も再現する()
    {
        // 同じ種なら助走→本編まで完全に一致する（助走で凍結/解凍するフラグが状態を汚していない証拠）。
        static double[] Run(ulong seed)
        {
            var sim = Build(seed);
            sim.StartWarmup();
            for (int q = 0; q < 8; q++) sim.StepQuarter();
            return [.. sim.Nations.Select(n => n.World.GdpPerMonth.Value)];
        }
        Assert.Equal(Run(7UL), Run(7UL));
    }

    [Fact]
    public void 助走の後も政治と外交の凍結は解けている()
    {
        // 助走は PoliticsEnabled と DiplomacyFrozen を一時的に触る＝必ず元へ戻す（戻し忘れると本編の政治が死ぬ）。
        var sim = Build();
        var before = sim.Nations.Select(n => n.World.PoliticsEnabled).ToArray();
        sim.StartWarmup(null, 20);
        Assert.False(sim.DiplomacyFrozen);
        for (int i = 0; i < sim.Nations.Count; i++)
            Assert.Equal(before[i], sim.Nations[i].World.PoliticsEnabled);
        foreach (var n in sim.Nations) Assert.False(n.World.WarmupFrozen);
    }

    [Fact]
    public void 助走は価格を実勢へ寄せる()
    {
        // 助走の目的そのもの＝開幕直後の価格の荒れが、助走なしより小さいこと。
        // 物差し＝首都市場の実勢財について、本編8四半期のあいだの |ln(価格の四半期変化)| の平均。
        static double Turbulence(int warm)
        {
            var sim = Build();
            if (warm > 0) sim.StartWarmup(null, warm);
            var r = sim.Nations[0].World.Region;
            var ids = r.Goods.Where(g => g.Tier != GoodTier.Service && r.Markets[g.Id].Price > 1e-9)
                             .Select(g => g.Id).ToArray();
            var prev = ids.Select(g => r.Markets[g].Price).ToArray();
            double sum = 0; int n = 0;
            for (int q = 0; q < 8; q++)
            {
                sim.StepQuarter();
                for (int k = 0; k < ids.Length; k++)
                {
                    double now = r.Markets[ids[k]].Price;
                    if (now > 1e-9 && prev[k] > 1e-9) { sum += System.Math.Abs(System.Math.Log(now / prev[k])); n++; }
                    prev[k] = now;
                }
            }
            return n > 0 ? sum / n : 0;
        }
        Assert.True(Turbulence(4) < Turbulence(0), "助走は開幕の価格の荒れを減らすはず");
    }

    /// <summary>★(223) 助走は時間が経たない四半期＝年次の乱数（豊凶・作物の病害・疫病の発生と感染窓の減衰）も引かない。
    /// 旧＝助走の Tick は 0 のまま進まないのに MonthSettle の年次ブロックの年（(Tick−30)/360 の整数除算＝0）が _lastHarvestYear(−1) と違い、助走の最初の月決算で1度発火した
    /// （全61カ国の開幕の豊凶が 1.0 でない＝0.73〜1.19・疫病の死者が助走で出る国が1種あたり1〜4国・ライン諸邦 −4.8% ほか）。初回のロールは設計どおり最初の実月（Tick=30）。
    /// 盤面＝seed 1789 の全61カ国。助走の前後で豊凶が 1.0 のまま・粗死亡（DiagDeaths）が 0・総人口が助走なしと一致、30日目に初回ロール（豊凶が 1.0 でなくなる）。
    /// ⚠突然変異「年次ブロックの WarmupFrozen の門を外す」で赤。</summary>
    [Fact]
    public void 助走は豊凶と疫病を引かず初回のロールは最初の実月に走る()
    {
        var sim = Build();
        double[] pop0 = sim.Nations.Select(n => n.World.TotalPopulation).ToArray();
        sim.StartWarmup(null, 4);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            string nm = w.SelfPower?.BaseName ?? "?";
            Assert.True(w.HarvestFactor == 1.0, $"{nm} の開幕の豊凶 {w.HarvestFactor}（助走で引いている）");
            Assert.True(w.DiagDeaths == 0, $"{nm} が助走で疫病の死者 {w.DiagDeaths}");
            Assert.Equal(pop0[i], w.TotalPopulation, 9);
            Assert.Equal(0, w.EpidemicActiveYears);
        }
        for (int d = 0; d < Calendar.DaysPerMonth - 1; d++) sim.StepDay();
        Assert.All(sim.Nations, n => Assert.True(n.World.HarvestFactor == 1.0, "29日目までは豊凶を引かない"));
        sim.StepDay();   // 30日目＝最初の月決算＝初回ロール（Tick=30）
        Assert.Equal(Calendar.DaysPerMonth, (int)sim.Nations[0].World.Tick);
        Assert.True(sim.Nations.Count(n => n.World.HarvestFactor != 1.0) >= sim.Nations.Count - 1, "初回のロールが最初の実月に走らない");
    }
}
