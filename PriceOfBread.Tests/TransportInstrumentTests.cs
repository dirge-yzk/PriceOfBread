using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(380) 輸送の段0 の計器（<c>trbin_s</c>・<c>trroute_s</c>・<c>trnation_s</c>・<c>trstat_s</c>・読むだけ）の番人。
/// ①運賃の払い手と受け手が閉じる：買い手の国が払った運賃の和（成約の位置で足した）＝世界の船腹の比で受け取った運賃の和（清算の終わりの位置で足した）＝別の経路で同じ量を数える。
/// ②分解が閉じる：経路ごとに〈海脚＋陸脚の換算 km ＝ 運賃の計算に使われた換算 km〉・〈運賃＝荷の値打ち×運賃率×換算 km÷1万 km〉（係数 FreightRatePer10Mm は Z の行から）・
///   〈運賃の陸脚の分÷運賃＝陸脚の換算 km ÷ 換算 km〉（陸直行は全部が陸脚）。期待値は列の別の組み合わせから書く（同じ式の写しにしない）。
/// ③帯が閉じる：距離の帯の中の重さ加重の平均の実 km は帯の境（テストが自分で持つ境）の内側・陸脚＋海脚＝合計・運賃＝陸脚の分＋海脚の分。
/// ④州の面積：全ての州の面積の和が陸地の面積の桁・同じ座標の州は等分・平均の運び距離＝(2/3)√(A/π)。州の断面の産出の和＝国の GdpPerMonth（別の経路）。
/// ⑤計器を切っている間は蓄えが作られない（hook が 1 命令も走らない）。
/// 何もしない番人は必ず緑になる＝陸直行と海経由の両方・越境と国内の両方・陸脚の運賃が正の行が 1 つ以上あることを対照にする。軌道が計器で変わらないことは InstrumentsTests と成績表の全行一致が見る。
/// </summary>
[Collection("Instruments")]
public class TransportInstrumentTests
{
    private static MultiNationSim Make() => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());

    private static (string[] Header, List<string[]> Rows) Table(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }
    private static double Col(string[] h, string[] r, string name)
    {
        int i = Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name);
        return r[i] switch { "nan" => double.NaN, "" => double.NaN, "inf" => double.PositiveInfinity, "-inf" => double.NegativeInfinity, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) };
    }
    private static string Str(string[] h, string[] r, string name) { int i = Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name); return r[i]; }
    private static void Near(double expect, double actual, double rel = 1e-8) => Assert.True(Math.Abs(expect - actual) <= rel * Math.Max(1.0, Math.Abs(expect)), $"期待 {expect}・実際 {actual}");

    /// <summary>6 年（24 四半期）回して表を書く。経路と州の断面の行は毎年の終わりに出す（既定は 10 年ごと）。</summary>
    private static (string dir, MultiNationSim sim, Action restore) Run(int quarters = 24)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tr_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        int snap = Instruments.TrSnapEveryYears; Instruments.TrSnapEveryYears = 1;
        var sim = Make();
        Instruments.Phase(sim, 1789UL, "open");
        sim.StartWarmup();
        Instruments.Phase(sim, 1789UL, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
        finally { Instruments.Close(sim); }
        return (dir, sim, () => { Instruments.TrSnapEveryYears = snap; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } });
    }

    [Fact]
    public void 運賃の払い手と受け手が別の経路で閉じ_分解と帯が列の別の組み合わせと合う()
    {
        var (dir, sim, restore) = Run();
        try
        {
            string P(string f) => System.IO.Path.Combine(dir, f + "_s1789.tsv");
            var (bh, br) = Table(P("trbin"));
            var (rh, rr) = Table(P("trroute"));
            var (nh, nr) = Table(P("trnation"));
            var (sh, sr) = Table(P("trstat"));
            Assert.All(br, r => Assert.Equal(bh.Length, r.Length));
            Assert.All(rr, r => Assert.Equal(rh.Length, r.Length));
            Assert.All(nr, r => Assert.Equal(nh.Length, r.Length));
            Assert.All(sr, r => Assert.Equal(sh.Length, r.Length));

            // ---- ① 払い手の和＝受け手の和（年ごと）。成約の位置で足した額と、清算の終わりに船腹の比で配った額は別の経路。
            var paid = new Dictionary<string, double>(); var recv = new Dictionary<string, double>();
            foreach (var r in nr)
            {
                string y = Str(nh, r, "year");
                paid[y] = paid.GetValueOrDefault(y) + Col(nh, r, "freightPaidGoldPerYear"); recv[y] = recv.GetValueOrDefault(y) + Col(nh, r, "freightReceivedGoldPerYear");
                double p = Col(nh, r, "freightPaidGoldPerYear");
                Assert.True(Col(nh, r, "freightPaidLandPartGoldPerYear") <= p * (1 + 1e-9) + 1e-12 && Col(nh, r, "freightPaidDomesticGoldPerYear") <= p * (1 + 1e-9) + 1e-12);
                Assert.True(Col(nh, r, "freightPaidLandPartDomesticGoldPerYear") <= Col(nh, r, "freightPaidDomesticGoldPerYear") * (1 + 1e-9) + 1e-12);
            }
            Assert.True(paid.Count >= 5, $"年の行が少ない {paid.Count}");
            double paidAll = 0, landAll = 0;
            foreach (var y in paid.Keys) { Near(paid[y], recv[y], 1e-6); paidAll += paid[y]; }
            Assert.True(paidAll > 0, "運賃が 1 件も払われていない＝計器が空");
            // 陸脚の運賃（世界の船の持ち主へ行った分）が正＝§2-7 の不具合の大きさ
            foreach (var r in nr) landAll += Col(nh, r, "freightPaidLandPartGoldPerYear");
            Assert.True(landAll > 0, "陸脚の運賃が 0＝陸脚の分け方が空");

            // ---- ③ 帯：陸脚＋海脚＝合計・運賃の2つの分け方・距離の帯の中の平均の実 km
            double[] lo = [0, 50, 150, 400, 1000, 2500, 6000, 12000], hi = [50, 150, 400, 1000, 2500, 6000, 12000, double.PositiveInfinity];   // テスト自身の持つ境
            int landRows = 0, seaRows = 0, crossRows = 0, domRows = 0, bands = 0; var bandSeen = new HashSet<int>();
            foreach (var r in br)
            {
                int route = (int)Col(bh, r, "routeCode"), cross = (int)Col(bh, r, "crossBorderFlag"), db = (int)Col(bh, r, "distanceBandCode");
                double w = Col(bh, r, "weightUnitsPerYear"), real = Col(bh, r, "weightKmRealPerYear"), rl = Col(bh, r, "weightKmLandRealPerYear"), rs = Col(bh, r, "weightKmSeaRealPerYear");
                Near(real, rl + rs, 1e-9);
                Near(Col(bh, r, "weightKmEqPerYear"), Col(bh, r, "weightKmEqLandPerYear") + Col(bh, r, "weightKmEqSeaPerYear"), 1e-9);
                Near(Col(bh, r, "freightGoldPerYear"), Col(bh, r, "freightLandPartGoldPerYear") + Col(bh, r, "freightSeaPartGoldPerYear"), 1e-9);
                if (route == 0)
                {   // 陸直行＝海脚が無く運賃は全部が陸脚
                    Assert.Equal(0.0, rs, 9); Assert.Equal(0.0, Col(bh, r, "weightKmEqSeaPerYear"), 9); Near(Col(bh, r, "freightGoldPerYear"), Col(bh, r, "freightLandPartGoldPerYear"), 1e-9); landRows++;
                }
                else { Assert.True(rs > 0, "海経由の行の海脚が 0"); seaRows++; }
                if (cross == 1) crossRows++; else domRows++;
                if (w > 0)
                {
                    double mean = real / w;
                    Assert.True(mean >= lo[db] * (1 - 1e-9) && mean < hi[db] * (1 + 1e-9), $"帯 {db} の重さ加重の平均の実 km {mean} が帯 [{lo[db]}, {hi[db]}) の外");
                    bandSeen.Add(db);
                }
            }
            bands = bandSeen.Count;
            Assert.True(landRows > 0 && seaRows > 0, $"陸直行の行 {landRows}・海経由の行 {seaRows}（対照＝両方が要る）");
            Assert.True(crossRows > 0 && domRows > 0, $"越境の行 {crossRows}・国内の行 {domRows}（対照＝両方が要る）");
            Assert.True(bands >= 3, $"距離の帯が {bands} 種しか出ていない");

            // ---- ② 経路ごと：分解が運賃の計算に使われた換算 km に合う・運賃＝値打ち×率×換算 km÷1万 km・陸脚の分の比
            double rate = 0, norm = 0;
            foreach (var r in sr) if (Str(sh, r, "scope") == "Z") { string nm = Str(sh, r, "name"); if (nm == "FreightRatePer10Mm") rate = Col(sh, r, "value"); if (nm == "DistanceNormKm") norm = Col(sh, r, "value"); }
            Assert.True(rate > 0 && norm > 0, "Z の行から運賃率と距離の物差しが読めない");
            int checkedPairs = 0, withLandLeg = 0, seaPairs = 0, landPairs = 0;
            foreach (var r in rr)
            {
                if (Str(rh, r, "sellerRegion").StartsWith("(")) continue;
                double eqSea = Col(rh, r, "eqSeaKm"), eqLand = Col(rh, r, "eqLandKm"), book = Col(rh, r, "eqBookKm");
                Near(book, eqSea + eqLand, 1e-9);                     // 最後の成約の経路：海脚＋陸脚の換算 km ＝ 運賃の計算に使われた換算 km
                double fr = Col(rh, r, "freightGoldPerYear"), frL = Col(rh, r, "freightLandPartGoldPerYear");
                double vb = Col(rh, r, "valueTimesEqBookKmGoldPerYear"), vs = Col(rh, r, "valueTimesEqSeaKmGoldPerYear"), vl = Col(rh, r, "valueTimesEqLandKmGoldPerYear");
                Near(vb, vs + vl, 1e-9);                              // 全ての成約で（額の重み付き）：海脚＋陸脚の換算 km ＝ 運賃の計算に使われた換算 km
                Near(rate * vb / norm, fr, 1e-6);                     // 運賃率×（荷の値打ち×換算 km）÷距離の物差し＝払われた運賃
                Near(rate * vl / norm, frL, 1e-6);                    // 陸脚の分＝運賃率×（荷の値打ち×陸脚の換算 km）÷距離の物差し
                if ((int)Col(rh, r, "routeCode") == 0) { Assert.Equal(0.0, eqSea, 9); landPairs++; } else { Assert.True(Col(rh, r, "realSeaKm") > 0); seaPairs++; }
                if (frL > 0) withLandLeg++;
                // 陸脚の換算 km ≧ 陸脚の実 km ×（地形の係数の最小 0.15）×（陸÷海の比の最小）
                Assert.True(eqLand + 1e-9 >= Col(rh, r, "realLandKm") * 0.15 * Math.Min(Col(rh, r, "landSeaRatioSeller"), Col(rh, r, "landSeaRatioBuyer")) * (1 - 1e-9) || Col(rh, r, "realLandKm") == 0);
                checkedPairs++;
            }
            // 越境の印：経路の行の売り手の国と買い手の国が違う組の運賃の和 ＝ 距離の帯の行の越境の印が 1 の運賃の和（年ごと・別のファイルの別の列から）
            var crossPair = new Dictionary<string, double>(); var crossBin = new Dictionary<string, double>(); var domPair = new Dictionary<string, double>(); var domBin = new Dictionary<string, double>();
            foreach (var r in rr)
            {
                if (Str(rh, r, "sellerRegion").StartsWith("(")) { Assert.Fail("上位 3000 組の外の行が出た＝4 国の試験では起きないはず（越境の突き合わせの前提が崩れた）"); }
                string y = Str(rh, r, "year"); double f = Col(rh, r, "freightGoldPerYear");
                if (Str(rh, r, "sellerNation") != Str(rh, r, "buyerNation")) crossPair[y] = crossPair.GetValueOrDefault(y) + f; else domPair[y] = domPair.GetValueOrDefault(y) + f;
            }
            foreach (var r in br)
            {
                string y = Str(bh, r, "year"); double f = Col(bh, r, "freightGoldPerYear");
                if ((int)Col(bh, r, "crossBorderFlag") == 1) crossBin[y] = crossBin.GetValueOrDefault(y) + f; else domBin[y] = domBin.GetValueOrDefault(y) + f;
            }
            Assert.True(crossPair.Count >= 5 && domPair.Count >= 5);
            foreach (var y in crossPair.Keys) Near(crossPair[y], crossBin.GetValueOrDefault(y), 1e-6);
            foreach (var y in domPair.Keys) Near(domPair[y], domBin.GetValueOrDefault(y), 1e-6);
            Assert.True(checkedPairs >= 50, $"経路の行が少ない {checkedPairs}");
            Assert.True(landPairs > 0 && seaPairs > 0 && withLandLeg > 0, $"陸直行 {landPairs}・海経由 {seaPairs}・陸脚の運賃が正 {withLandLeg}");
        }
        finally { restore(); }
    }

    [Fact]
    public void 州の面積は陸地を分け合い平均の運び距離は幾何の式に合い_州の断面の産出が国のGDPに合う()
    {
        // ---- 面積：1 点なら帯の陸地の全部・同じ座標の 2 点は等分・遠い 2 点は合計が変わらない（分け合う）
        var one = Instruments.TrAreas([(48.85, 2.35)]);
        double land = one.All[0];
        Assert.InRange(land, 80e6, 150e6);   // 緯度 −60〜80 度の陸地（km²）の桁
        var dup = Instruments.TrAreas([(48.85, 2.35), (48.85, 2.35)]);
        Near(land / 2, dup.All[0], 1e-9); Near(land / 2, dup.All[1], 1e-9);
        var two = Instruments.TrAreas([(48.85, 2.35), (35.68, 139.69)]);
        Near(land, two.All[0] + two.All[1], 1e-9);
        Assert.True(two.All[0] > 10e6 && two.All[1] > 10e6, "遠い 2 点のどちらかの面積が小さすぎる");
        Assert.True(two.Near[0] < two.All[0] && two.Near[0] > 0, "1000 km 以内の面積は全体の内側");
        var none = Instruments.TrAreas([(0.0, 0.0), (48.85, 2.35)]);
        Assert.True(double.IsNaN(none.All[0]), "座標が (0,0) の州は NaN");
        Near(land, none.All[1], 1e-9);

        var (dir, sim, restore) = Run();
        try
        {
            string P(string f) => System.IO.Path.Combine(dir, f + "_s1789.tsv");
            var (sh, sr) = Table(P("trstat"));
            int aRows = 0; double areaSum = 0;
            foreach (var r in sr.Where(r => Str(sh, r, "scope") == "A"))
            {
                aRows++;
                double a = Col(sh, r, "areaKm2"), n = Col(sh, r, "areaWithin1000KmKm2");
                if (!double.IsNaN(a))
                {
                    areaSum += a;
                    Assert.True(n <= a * (1 + 1e-12));
                    Near(2.0 / 3.0 * Math.Sqrt(a / Math.PI), Col(sh, r, "meanHaulKm"), 1e-8);
                }
            }
            Assert.True(aRows >= 50, $"面積の行が少ない {aRows}");
            Assert.InRange(areaSum, 80e6, 150e6);
            // 州の断面：国ごとの（工場＋採取場）の産出の和 ＝ World.GdpPerMonth（別の経路）・都市人口 ≦ 人口
            var perNation = new Dictionary<string, double>(); string lastYear = "";
            foreach (var r in sr.Where(r => Str(sh, r, "scope") == "G")) lastYear = Str(sh, r, "year");
            Assert.True(lastYear.Length > 0, "州の断面の行が無い");
            int gRows = 0, mountain = 0, plain = 0;
            foreach (var r in sr.Where(r => Str(sh, r, "scope") == "G" && Str(sh, r, "year") == lastYear))
            {
                gRows++;
                string nat = Str(sh, r, "nation");
                perNation[nat] = perNation.GetValueOrDefault(nat) + Col(sh, r, "factoryOutputValuePerMonth") + Col(sh, r, "gatherOutputValuePerMonth");
                Assert.True(Col(sh, r, "urbanPopulationK") <= Col(sh, r, "populationK") * (1 + 1e-9) + 1e-12);
                int terr = (int)Col(sh, r, "terrainCode"); if (terr == (int)Terrain.Mountain) mountain++; if (terr == (int)Terrain.Plain) plain++;
            }
            Assert.True(gRows >= 30 && plain > 0, $"断面の州 {gRows}・平地 {plain}");
            foreach (var n in sim.Nations)
            {
                string name = Instruments.NameOf(n.World);
                if (!perNation.TryGetValue(name, out double sum)) continue;
                double gdp = n.World.GdpPerMonth.Value;
                Assert.True(Math.Abs(sum - gdp) <= 1e-6 * Math.Max(1.0, gdp), $"{name}：州の断面の産出の和 {sum} が GdpPerMonth {gdp} に合わない");
            }
            Assert.True(perNation.Count >= 3);
            // 世界の値段：各財の行が年ごとにある・値が正・参照価格が正
            var prices = sr.Where(r => Str(sh, r, "scope") == "P").ToList();
            Assert.True(prices.Count >= 14 * 5, $"値段の行が少ない {prices.Count}");
            foreach (var r in prices.Where(r => Str(sh, r, "name") is "馬" or "労働力")) Assert.True(Col(sh, r, "value") > 0 && Col(sh, r, "refValue") > 0, "馬か労働の値段が正でない");
        }
        finally { restore(); }
    }

    [Fact]
    public void 計器を切っている間は蓄えが作られない()
    {
        Instruments.Configure(null, null);
        Assert.False(Instruments.On);
        var sim = Make();
        sim.StartWarmup();
        for (int q = 0; q < 4; q++) sim.StepQuarter();
        Assert.False(Instruments.TrAccExists(sim), "計器を切っているのに蓄えが作られた＝hook が On の外で走っている");
    }
}
