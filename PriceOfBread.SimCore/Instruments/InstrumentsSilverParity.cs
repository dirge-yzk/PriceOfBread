namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (775) 銀の平価の自己参照の表 silverpar_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsPortch.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・世界を書かない。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝事前登録 measurements/m775_20261102_prereg_ag.txt の L1・L2（銀本位国の E の貼り付き・b の貼り付き）・U1（E の ln と rel の ln の回帰）・U2（挟みを外した腕で E の幅）を同じ盤面で読む。
// 取り方＝①<see cref="SilverParityCapture"/>：MultiNationSim の銀の世界価格の注入の**直後**に呼ばれ、州を自分で歩いて relRecomputed（注入と式を共有しない再計算）・法ごとの州の局所の値段・b を捕捉して溜める。
//   ②<see cref="SilverParityTable"/>：Instruments.Quarter（四半期の末）から呼ばれ、溜めた捕捉と**その時の** E（注入の rel に月次で寄った後の値）・法・国の数を合わせて 1 四半期 3 行（S・G・F）を書く。
// 行＝四半期×法（S＝銀本位・G＝金本位・F＝不換）。世界の列は 3 行に同じ値。列（接尾辞：Index・Count・Share・Flag）：
//   世界：quarter・year・ratioOkFlag・oldRawIndex（Market.Price[銀]÷RefPrice＝RefPrice×[0.1,10] に挟んだ後・[ExchangeRateFloor,Ceil] に挟む前）・oldParityIndex（oldRawIndex を [Floor,Ceil] に挟んだ値＝旧の経路の平価の元）・
//         usedParityIndex（実際に各国へ注入された WorldSilverPrice÷RefPrice）・newParityIndex（rel＝比が不正なら nan）・relRecomputedIndex（計器が自分で取り直した rel・旗 0 の規則〔等重み〕での取り直し＝旗 SilverParityDemandWeighted が正でも重みづけない）・wAgShare（銀の局所価格が正の州のうち銀本位国の州の割合）。
//   法：law・nationCount・eMean・eMedian（その時の E・人口＞0 の国）・eAtCeilCount・eAtFloorCount・
//         provCount・mAgIndex・mAgEIndex・mAuIndex（その法の国の州のうち銀と金鉱の両方の市場が生きて価格が正の州。局所の銀の値段÷RefPrice・同×E・局所の金鉱の値段÷RefPrice の算術平均）・
//         agCount・mAgAllIndex・mAgEAllIndex（銀の局所価格が正の州〔Market.Step の平均価格の節と同じ集合に近い〕。不動点 E*＝(1−wAg)·mAgEAll(非S)/(1−wAg·mAgAll(S)) の確かめ用）。
// (786) 列 fiscalBAt3Count（b が上限に届いた国の数）は、上限 GoldDebtBurdenCap の削除で読む係数が無くなったため消した（旧の表を読む台本は scripts/instruments/c7ag・c8sv）。
// ⚠限界（計器が自己申告する）：州の集合は Market.Step の参加州（linked）の写しではなく〈Markets[銀] の局所価格が正の州〉＝座標なし・凍結港の州の扱いが違えば Market.Price の平均と一致しない／
//   州の法の振り分けは注入の時の法・E と国の数は四半期の末の法（法が四半期の中で替わった国は食い違う）／助走中の注入は溜めた値が上書きされるだけ（Quarter は呼ばれない）／
//   relRecomputed は注入の式と同じ定義の独立な再計算＝定義そのものの誤りは拾えない（U1 の突然変異で符号と平均の取り方だけ確かめる）。
public static partial class Instruments
{
    internal const string SilverparHeaderText = "quarter\tyear\tlaw\tratioOkFlag\toldRawIndex\toldParityIndex\tusedParityIndex\tnewParityIndex\trelRecomputedIndex\twAgShare\tnationCount\teMean\teMedian\teAtCeilCount\teAtFloorCount\tprovCount\tmAgIndex\tmAgEIndex\tmAuIndex\tagCount\tmAgAllIndex\tmAgEAllIndex";

    sealed class SilverparSink
    {
        public System.IO.StreamWriter? W; public bool Header;
        public bool Pending;                                   // 注入の直後に捕捉した値がある（Quarter で書いたら下ろす）
        public int RatioOkFlag; public double OldRaw, OldParity, Used, NewParity, RelRecomputed, WAg;
        public readonly int[] Prov = new int[3], AgCnt = new int[3];   // 添字＝法（0 不換・1 銀・2 金＝MonetaryStandardLaw の順）
        public readonly double[] SAg = new double[3], SAgE = new double[3], SAu = new double[3], SAgAll = new double[3], SAgEAll = new double[3];
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, SilverparSink> SilverparSinks = new();

    static string SilverparR(double v) => double.IsNaN(v) ? "nan" : double.IsPositiveInfinity(v) ? "inf" : double.IsNegativeInfinity(v) ? "-inf" : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>銀の世界価格の注入の直後（MultiNationSim）：州を自分で歩いて旧と新の平価・州の局所の値段・b を溜める。<paramref name="silverWorld"/>＝注入した値・<paramref name="rel"/>＝注入側の rel・
    /// <paramref name="ratioOk"/>＝比が正当か。助走中は呼ばれない（呼び手が門）。</summary>
    internal static void SilverParityCapture(MultiNationSim sim, double silverWorld, double rel, bool ratioOk)
    {
        if (sim.Nations.Count == 0) return;
        var sk = SilverparSinks.GetValue(sim, _ => new SilverparSink());
        double refAg = Catalog.RefPrices[Catalog.SilverId], refAu = Catalog.RefPrices[Catalog.GoldOreId];
        var c0 = sim.Nations[0].World.Coef;
        sk.RatioOkFlag = ratioOk ? 1 : 0;
        sk.OldRaw = refAg > 1e-9 ? sim.Market.Price[Catalog.SilverId] / refAg : double.NaN;
        sk.OldParity = Math.Clamp(sk.OldRaw, c0.ExchangeRateFloor, c0.ExchangeRateCeil);
        sk.Used = refAg > 1e-9 ? silverWorld / refAg : double.NaN;
        sk.NewParity = ratioOk ? rel : double.NaN;
        Array.Clear(sk.Prov); Array.Clear(sk.AgCnt); Array.Clear(sk.SAg); Array.Clear(sk.SAgE); Array.Clear(sk.SAu); Array.Clear(sk.SAgAll); Array.Clear(sk.SAgEAll);
        double lnAg = 0, lnAu = 0; int cnt = 0, agTotal = 0, agSilver = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; int law = (int)w.MonetaryLaw; double e = w.FxRate;
            foreach (var r in w.Regions)
            {
                if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
                var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
                double pAg = mAg.Price, pAu = mAu.Price;
                if (pAg > 1e-9)
                {
                    agTotal++; if (law == 1) agSilver++;
                    sk.AgCnt[law]++; sk.SAgAll[law] += pAg / refAg; sk.SAgEAll[law] += pAg * e / refAg;
                }
                bool live = (mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) && (mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9);
                if (!live || !(pAg > 1e-9) || !(pAu > 1e-9)) continue;
                lnAg += Math.Log(pAg); lnAu += Math.Log(pAu); cnt++;
                sk.Prov[law]++; sk.SAg[law] += pAg / refAg; sk.SAgE[law] += pAg * e / refAg; sk.SAu[law] += pAu / refAu;
            }
        }
        sk.RelRecomputed = cnt > 0 && refAu > 1e-9 && refAg > 1e-9 ? Math.Exp((lnAg - lnAu) / cnt - Math.Log(refAg / refAu)) : double.NaN;
        sk.WAg = agTotal > 0 ? (double)agSilver / agTotal : double.NaN;
        sk.Pending = true;
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：溜めた捕捉と今の E・法を合わせて 3 行（S・G・F）を書く。捕捉が無い四半期（助走・銀の財が無い）は何も書かない。</summary>
    static void SilverParityTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!SilverparSinks.TryGetValue(sim, out var sk) || !sk.Pending) return;
        sk.Pending = false;
        var es = new List<double>[3]; for (int i = 0; i < 3; i++) es[i] = [];
        var c0 = sim.Nations[0].World.Coef; int yr = sim.Nations[0].World.Year;
        int[] atCeil = new int[3], atFloor = new int[3];
        foreach (var n in sim.Nations)
        {
            var w = n.World; if (!(w.TotalPopulation > 0)) continue;
            int law = (int)w.MonetaryLaw; double e = w.ExchangeRate;
            es[law].Add(e);
            if (e >= c0.ExchangeRateCeil - 1e-9) atCeil[law]++;
            if (e <= c0.ExchangeRateFloor + 1e-9) atFloor[law]++;
        }
        sk.W ??= Open("silverpar", seed);
        if (!sk.Header) { sk.W.WriteLine(SilverparHeaderText); sk.Header = true; }
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string[] lawName = ["F", "S", "G"]; int[] order = [1, 2, 0];
        foreach (int law in order)
        {
            var list = es[law]; list.Sort();
            double med = list.Count == 0 ? double.NaN : (list.Count % 2 == 1 ? list[list.Count / 2] : 0.5 * (list[list.Count / 2 - 1] + list[list.Count / 2]));
            double mean = list.Count == 0 ? double.NaN : list.Sum() / list.Count;
            int pc = sk.Prov[law], ac = sk.AgCnt[law];
            sk.W.WriteLine(string.Join('\t', q.ToString(inv), yr.ToString(inv), lawName[law], sk.RatioOkFlag.ToString(inv),
                SilverparR(sk.OldRaw), SilverparR(sk.OldParity), SilverparR(sk.Used), SilverparR(sk.NewParity), SilverparR(sk.RelRecomputed), SilverparR(sk.WAg),
                list.Count.ToString(inv), SilverparR(mean), SilverparR(med), atCeil[law].ToString(inv), atFloor[law].ToString(inv),
                pc.ToString(inv), SilverparR(pc > 0 ? sk.SAg[law] / pc : double.NaN), SilverparR(pc > 0 ? sk.SAgE[law] / pc : double.NaN), SilverparR(pc > 0 ? sk.SAu[law] / pc : double.NaN),
                ac.ToString(inv), SilverparR(ac > 0 ? sk.SAgAll[law] / ac : double.NaN), SilverparR(ac > 0 ? sk.SAgEAll[law] / ac : double.NaN)));
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void SilverParityClose(MultiNationSim sim)
    {
        if (SilverparSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
