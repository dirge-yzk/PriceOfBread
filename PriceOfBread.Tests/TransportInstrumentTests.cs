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
/// ⑥(385) 陸脚の売り手側・買い手側：和が元の陸脚の列に合う（3 つのファイルで）・海経由は〈売り手側の換算 km ＝ 売り手側の実 km × 地形の係数 × 陸÷海の比〉（別の列から再構成）・
///   陸直行は売り手側÷買い手側 ＝ 両端の（地形×比）の比・国の表の〈国の中の陸脚の運賃〉の和 ＝ 帯の表の陸脚の運賃の和（別のファイル）・〈戻る額〉は受け取った運賃を超えない。
///   対照＝越境の海経由で売り手側の陸脚の運賃が正の行がある。⑦(385) 州の面積の行が断面の年ごとに出て、その年の G 行の州の集合に合う。
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
                // (385) 陸脚の売り手側＋買い手側 ＝ 元の陸脚の列（実 km・換算 km・運賃）
                Near(rl, Col(bh, r, "weightKmRealLandSellerSidePerYear") + Col(bh, r, "weightKmRealLandBuyerSidePerYear"), 1e-9);
                Near(Col(bh, r, "weightKmEqLandPerYear"), Col(bh, r, "weightKmEqLandSellerSidePerYear") + Col(bh, r, "weightKmEqLandBuyerSidePerYear"), 1e-9);
                Near(Col(bh, r, "freightLandPartGoldPerYear"), Col(bh, r, "freightLandSellerSidePartGoldPerYear") + Col(bh, r, "freightLandBuyerSidePartGoldPerYear"), 1e-9);
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
            int checkedPairs = 0, withLandLeg = 0, seaPairs = 0, landPairs = 0, crossSellerLeg = 0;
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
                // (385) 陸脚の売り手側・買い手側：和が合う・海経由の（最後の成約の）売り手側の換算 km ＝ 売り手側の実 km × 地形の係数 × 陸÷海の比（別の列から再構成）
                double rls = Col(rh, r, "realLandSellerSideKm"), rlb = Col(rh, r, "realLandBuyerSideKm"), els = Col(rh, r, "eqLandSellerSideKm"), elb = Col(rh, r, "eqLandBuyerSideKm");
                double ts = Col(rh, r, "terrainCostSeller"), tb = Col(rh, r, "terrainCostBuyer"), ks = Col(rh, r, "landSeaRatioSeller"), kb = Col(rh, r, "landSeaRatioBuyer");
                Near(Col(rh, r, "realLandKm"), rls + rlb, 1e-9); Near(eqLand, els + elb, 1e-9);
                Near(frL, Col(rh, r, "freightLandSellerSidePartGoldPerYear") + Col(rh, r, "freightLandBuyerSidePartGoldPerYear"), 1e-6);
                Near(vl, Col(rh, r, "valueTimesEqLandSellerSideKmGoldPerYear") + Col(rh, r, "valueTimesEqLandBuyerSideKmGoldPerYear"), 1e-9);
                if ((int)Col(rh, r, "routeCode") == 1) { Near(rls * ts * ks, els, 1e-9); Near(rlb * tb * kb, elb, 1e-9); }
                else { Near(rls, rlb, 1e-9); Near(rls * ts * ks, els, 1e-9); Near(rlb * tb * kb, elb, 1e-9); }   // 陸直行：実 km は半分ずつ・換算は各端の係数
                if ((int)Col(rh, r, "routeCode") == 1 && Col(rh, r, "valueSeaRouteGoldPerYear") >= Col(rh, r, "valueGoldPerYear") * (1 - 1e-9))
                {   // 全ての成約が海経由の組：売り手側÷買い手側の運賃 ＝ 両端の（実 km×地形×比）の比（売り手側の分だけが正の組もある＝0 除け）
                    double fs = Col(rh, r, "freightLandSellerSidePartGoldPerYear"), fb = Col(rh, r, "freightLandBuyerSidePartGoldPerYear");
                    if (els > 0 && elb > 0 && fb > 0) Near(els / elb, fs / fb, 1e-6);
                    if (fs > 0 && Str(rh, r, "sellerNation") != Str(rh, r, "buyerNation")) crossSellerLeg++;
                }
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
            Assert.True(crossSellerLeg > 0, "越境の海経由で売り手側の陸脚の運賃が正の組が 1 つも無い（対照）");

            // ---- ⑥ (385) 国の表：国の中の陸脚の運賃（売り手側は売り手の国・買い手側は買い手の国）の和 ＝ 帯の表の陸脚の運賃の和（別のファイル・年ごと）。戻る額は国の中の陸脚の運賃・受け取った運賃を超えない。
            var inNat = new Dictionary<string, double>(); var landBin = new Dictionary<string, double>(); double toOwnAll = 0, inAll = 0, recvAll = 0;
            foreach (var r in nr)
            {
                string y = Str(nh, r, "year");
                double sIn = Col(nh, r, "freightLandSellerSideInNationGoldPerYear"), bIn = Col(nh, r, "freightLandBuyerSideInNationGoldPerYear"), own = Col(nh, r, "freightLandInNationToOwnCarriersGoldPerYear");
                inNat[y] = inNat.GetValueOrDefault(y) + sIn + bIn;
                Assert.True(own <= sIn + bIn + 1e-9 * Math.Max(1.0, sIn + bIn), "戻る額が国の中の陸脚の運賃を超えた");
                Assert.True(own <= Col(nh, r, "freightReceivedGoldPerYear") * (1 + 1e-9) + 1e-12, "戻る額が受け取った運賃を超えた（戻る額は受け取りの一部のはず）");
                Assert.True(Col(nh, r, "freightPaidLandPartDomesticGoldPerYear") <= sIn + bIn + 1e-9 * Math.Max(1.0, sIn + bIn), "国内の成約の陸脚の運賃は国の中の陸脚の運賃に含まれる");
                toOwnAll += own; inAll += sIn + bIn; recvAll += Col(nh, r, "freightReceivedGoldPerYear");
            }
            foreach (var r in br) landBin[Str(bh, r, "year")] = landBin.GetValueOrDefault(Str(bh, r, "year")) + Col(bh, r, "freightLandPartGoldPerYear");
            foreach (var y in inNat.Keys) Near(landBin.GetValueOrDefault(y), inNat[y], 1e-6);
            Assert.True(toOwnAll > 0 && toOwnAll < inAll, $"戻る額 {toOwnAll}・国の中の陸脚の運賃 {inAll}（4 国の試験では 0 < 戻る額 < 国の中の運賃）");
            Assert.True(toOwnAll < recvAll, "戻る額は受け取った運賃の和より小さい");
            // 国ごと：〈戻る額÷国の中の陸脚の運賃〉は、その国の船腹の比（受け取った運賃÷世界の受け取り）の時間平均に近い＝売り手側にも買い手側にも〈その国自身の〉比を掛けた証拠
            //   （売り手と買い手の比を取り違えると、国の中の運賃に相手国の比が掛かって、この国の比から外れる）。2 つの量は別の経路（成約の位置の積／清算の終わりの配り）で、重みが違うので完全には一致しない。
            var inBy = new Dictionary<string, double>(); var ownBy = new Dictionary<string, double>(); var recvBy = new Dictionary<string, double>();
            foreach (var r in nr)
            {
                string nm = Str(nh, r, "nation");
                inBy[nm] = inBy.GetValueOrDefault(nm) + Col(nh, r, "freightLandSellerSideInNationGoldPerYear") + Col(nh, r, "freightLandBuyerSideInNationGoldPerYear");
                ownBy[nm] = ownBy.GetValueOrDefault(nm) + Col(nh, r, "freightLandInNationToOwnCarriersGoldPerYear");
                recvBy[nm] = recvBy.GetValueOrDefault(nm) + Col(nh, r, "freightReceivedGoldPerYear");
            }
            double recvTot = recvBy.Values.Sum(); int shareChecked = 0, shareOff = 0; var shareMsg = new List<string>();
            foreach (var nm in inBy.Keys)
            {
                double sh0 = recvBy[nm] / recvTot;
                if (!(inBy[nm] > 0) || sh0 < 0.03) continue;
                double ratio = ownBy[nm] / inBy[nm]; shareMsg.Add($"{nm} 戻る/国の中={ratio:F4}・受取の割合={sh0:F4}");
                if (Math.Abs(ratio - sh0) > 0.05 * sh0) shareOff++;   // (390) 幅を 35%→5% へ：正常な木の差は 0.5〜1.3%（オーストリア 1.2・フランス 0.5・プロイセン 1.3）・M3（比の添字の取り違え）は 16〜53%（フランス 16・オーストリア 25・プロイセン 53＝(390) の木での実測）＝3 国全てで赤。挙動を変える線の合流の木で正常な差が 5% を超えたら、まず差の原因（船腹の比の時間平均と成約ごとの積の重みの違い）を読んでから幅を決める
                shareChecked++;
            }
            Assert.True(shareChecked >= 2, "船腹の比が 3% 以上で陸脚の運賃のある国が 2 つ未満＝比の突き合わせが空: " + string.Join(" ／ ", shareMsg));
            Assert.True(shareOff == 0, "戻る額÷国の中の陸脚の運賃が、その国の船腹の比から外れた国がある：" + string.Join(" ／ ", shareMsg));
        }
        finally { restore(); }
    }

    /// <summary>★2026-10-17（開発の高速化）：州の面積の近傍探索（緯度の順に並べて外へ広げる）が、旧の総当たり（番号の順に狭義の &lt; で最初の最小）とビットまで同じ面積を出す。
    /// 点は同じ緯度の組（距離の同点）・同じ座標・日付変更線の両側・(0,0) を混ぜる。</summary>
    [Fact]
    public void 州の面積の近傍探索は総当たりと同じ面積を出す()
    {
        ulong x = 0x9E3779B97F4A7C15UL;
        double Next() { x ^= x << 13; x ^= x >> 7; x ^= x << 17; return (x >> 11) * (1.0 / (1UL << 53)); }
        var pts = new List<(double Lat, double Lon)>();
        for (int i = 0; i < 220; i++) pts.Add((Math.Round(-55 + 130 * Next(), 1), Math.Round(-180 + 360 * Next(), 1)));
        for (int i = 0; i < 30; i++) { var q = pts[i * 5]; pts.Add((q.Lat, Math.Round(q.Lon + 2.0 * (Next() - 0.5), 1))); }   // 同じ緯度
        pts.Add(pts[3]); pts.Add(pts[17]);                    // 同じ座標
        pts.Add((10.1, 179.9)); pts.Add((10.1, -179.9));      // 日付変更線の両側
        pts.Add((0.0, 0.0));
        var arr = pts.ToArray();
        var (all, near) = Instruments.TrAreas(arr);
        var (rAll, rNear) = BruteAreas(arr);
        for (int i = 0; i < arr.Length; i++)
        {
            Assert.True(BitConverter.DoubleToInt64Bits(all[i]) == BitConverter.DoubleToInt64Bits(rAll[i]), $"面積が総当たりと違う：{i} {arr[i]} {all[i]} ≠ {rAll[i]}");
            Assert.True(BitConverter.DoubleToInt64Bits(near[i]) == BitConverter.DoubleToInt64Bits(rNear[i]), $"1000 km 以内の面積が総当たりと違う：{i} {arr[i]} {near[i]} ≠ {rNear[i]}");
        }
    }

    /// <summary>旧の総当たり（2026-10-17 以前の Instruments.TrAreas の本体そのまま・計器の定数も同じ値）。</summary>
    static (double[] All, double[] Near) BruteAreas((double Lat, double Lon)[] pts)
    {
        const double step = 0.2, nearKm = 1000.0, latMin = -60.0, latMax = 80.0, kmPerDeg = 111.195;
        var uniq = new List<(double Lat, double Lon)>(); var uid = new int[pts.Length]; var members = new List<int>();
        var seen = new Dictionary<(double, double), int>();
        for (int i = 0; i < pts.Length; i++)
        {
            if (pts[i].Lat == 0 && pts[i].Lon == 0) { uid[i] = -1; continue; }
            if (!seen.TryGetValue(pts[i], out int u)) { u = uniq.Count; seen[pts[i]] = u; uniq.Add(pts[i]); members.Add(0); }
            uid[i] = u; members[u]++;
        }
        var ua = new double[uniq.Count]; var un = new double[uniq.Count];
        int nx = (int)Math.Round(360.0 / step), ny = (int)Math.Round(180.0 / step);
        for (int yi = 0; yi < ny; yi++)
        {
            double lat = 90.0 - (yi + 0.5) * step;
            if (lat < latMin || lat > latMax) continue;
            double cosLat = Math.Cos(lat * Math.PI / 180.0);
            double cellKm2 = step * kmPerDeg * step * kmPerDeg * cosLat;
            for (int xi = 0; xi < nx; xi++)
            {
                double lon = -180.0 + (xi + 0.5) * step;
                if (!Landmask.IsLandForRoads(lat, lon)) continue;
                int best = -1; double bd = double.MaxValue;
                for (int u = 0; u < uniq.Count; u++)
                {
                    double dl = Math.Abs(lon - uniq[u].Lon); if (dl > 180.0) dl = 360.0 - dl;
                    double dy = lat - uniq[u].Lat, dx = dl * cosLat;
                    double d2 = dy * dy + dx * dx;
                    if (d2 < bd) { bd = d2; best = u; }
                }
                if (best < 0) continue;
                ua[best] += cellKm2;
                if (Math.Sqrt(bd) * kmPerDeg <= nearKm) un[best] += cellKm2;
            }
        }
        var all = new double[pts.Length]; var near = new double[pts.Length];
        for (int i = 0; i < pts.Length; i++)
        {
            if (uid[i] < 0) { all[i] = near[i] = double.NaN; continue; }
            all[i] = ua[uid[i]] / members[uid[i]]; near[i] = un[uid[i]] / members[uid[i]];
        }
        return (all, near);
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
            int aRows = 0; var areaSumByYear = new Dictionary<string, double>(); var aSet = new Dictionary<string, HashSet<string>>(); var gSet = new Dictionary<string, HashSet<string>>();
            foreach (var r in sr.Where(r => Str(sh, r, "scope") == "A"))
            {
                aRows++;
                string ay = Str(sh, r, "year");
                if (!aSet.TryGetValue(ay, out var asx)) aSet[ay] = asx = new HashSet<string>();
                asx.Add(Str(sh, r, "nation") + "/" + Str(sh, r, "region"));
                double a = Col(sh, r, "areaKm2"), n = Col(sh, r, "areaWithin1000KmKm2");
                if (!double.IsNaN(a))
                {
                    areaSumByYear[ay] = areaSumByYear.GetValueOrDefault(ay) + a;
                    Assert.True(n <= a * (1 + 1e-12));
                    Near(2.0 / 3.0 * Math.Sqrt(a / Math.PI), Col(sh, r, "meanHaulKm"), 1e-8);
                }
            }
            Assert.True(aRows >= 50, $"面積の行が少ない {aRows}");
            foreach (var kv in areaSumByYear) Assert.InRange(kv.Value, 80e6, 150e6);   // 年ごと（旧は全部の和＝年を数えなかった）
            // (385) 面積の行が断面の年（G 行のある年）ごとに出て、その年の G 行の州の集合に合う（A がその年の州に従う）。開幕の 1 回の年も 1 組ある。
            foreach (var r in sr.Where(r => Str(sh, r, "scope") == "G"))
            {
                string gy = Str(sh, r, "year");
                if (!gSet.TryGetValue(gy, out var gsx)) gSet[gy] = gsx = new HashSet<string>();
                gsx.Add(Str(sh, r, "nation") + "/" + Str(sh, r, "region"));
            }
            Assert.True(gSet.Count >= 5, $"断面の年が少ない {gSet.Count}");
            foreach (var kv in gSet) { Assert.True(aSet.ContainsKey(kv.Key), $"断面の年 {kv.Key} に面積の行が無い"); Assert.True(kv.Value.SetEquals(aSet[kv.Key]), $"断面の年 {kv.Key} の面積の行の州の集合が G 行の州の集合と違う"); }
            Assert.True(aSet.Count == gSet.Count + 1, $"面積の行の年 {aSet.Count}（断面の年 {gSet.Count} ＋ 開幕の 1 回のはず）");
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
