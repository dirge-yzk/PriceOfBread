namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (681) 急進の運動体の表 prdf_s（読むだけ・恒等・事前登録 measurements/prd_671_20261030_prereg_rev3.txt [R8][R9]） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsIdn.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない。INS=1 のときだけ動く。
// 行＝国×四半期（Factions.Step が走った国だけ＝議会の世界で FactionRng を持つ国）。Factions.Step の終わり（結束の更新と合流の後）の断面。観測クラスタは Factions.Step が求めた物をそのまま読む（第二の束ね方を作らない）。
//   運動体 ＝ 地域×束×主軸のセルのうち全国人口の 1%（FactionSnapshot.MinSizeFraction）以上の物（Factions.Step の足切りと同じ）。
//   nBody ＝ 運動体の数／nBodyCom ＝ うち主軸が共産主義（Ideology 6）／nBodySoc ＝ うち主軸が社会主義（Ideology 5）。
//   popShareCom ＝ 主軸（POP の思想シェアの ArgMax）が共産主義の POP の人数割合（州の POP の Size 合計比・兵士も含む）。
//   popShareComSub ＝ 主軸が共産主義の POP のうち、そのセルが運動体になれなかった POP（参加の述語が 0 で束に入らない POP も含む）の人数割合。
//   popShareSoc ＝ 主軸が社会主義の POP の人数割合（比べる側）。
//   nCellCom ＝ 主軸が共産主義のセルの数（足切りの前）／maxCellFracCom ＝ そのうち最大のセルの規模÷全国人口（参加で重みづけた規模・足切りは 0.01）／maxCellFracSoc ＝ 社会主義の同じ量。
//   maxCohCom ＝ 運動体になっている共産主義のセルの Cohesion の最大（無ければ 0）。
//   revoltTarget ＝ 当四半期に TriggerFactionRevolt が発火した最後の目標政体のコード（(int)Polity・プロ独裁は 6・無ければ -1）／revoltN ＝ 当四半期の発火の回数。
//   track0〜track3 ＝ RegimeTrackRecord の系統別（0 君主・1 議会制・2 強権・3 急進）の当四半期の値（最小は種内の集計で出す）。
//   manifestoCom ＝ 公約の思想（ManifestoIdeology）が共産主義か（1／0）／knownCom ＝ 共産主義の行を発見済みか（1／0）／polityCode ＝ (int)w.Polity。
// ⚠限界（計器が自己申告する）：議会の無い世界（Factions.Step が走らない国）は行が出ない＝分母は〈行のある国×四半期〉／セルの規模は参加で重みづけた量（POP の人数ではない）／
//   popShare の分母は州の POP の Size の和（w.TotalPopulation と同じ集合だが足す順が違う）／revoltTarget は発火した時点の目標で RaiseRevolt の成否は見ない。
public static partial class Instruments
{
    internal const string PrdfHeaderText = "quarter\tyear\tnation\tpolityCode\tknownCom\tmanifestoCom\tnBody\tnBodyCom\tnBodySoc\tpopShareCom\tpopShareComSub\tpopShareSoc"
        + "\tnCellCom\tmaxCellFracCom\tmaxCellFracSoc\tmaxCohCom\trevoltTarget\trevoltN\ttrack0\ttrack1\ttrack2\ttrack3";

    sealed class PrdfRow { public string Head = ""; public int Q; public int RevoltTarget = -1, RevoltN; public string Tail = ""; }

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, List<PrdfRow>> PrdfPending = new();
    sealed class PrdfSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PrdfSink> PrdfSinks = new();

    /// <summary><see cref="Factions.Step"/> の終わり（計器だけ）：この国のこの四半期の行を溜める。<paramref name="observed"/> は Factions.Step が求めたクラスタ（読むだけ）。</summary>
    internal static void PrdfObserve(World w, List<(Region Region, int Bloc, int Ideo, double Size, double MilSum)> observed, double totalPop)
    {
        const int com = (int)Ideology.Communism, soc = (int)Ideology.Socialism;
        double thr = FactionSnapshot.MinSizeFraction * totalPop;
        int nBody = 0, nBodyCom = 0, nBodySoc = 0, nCellCom = 0;
        double maxCellCom = 0, maxCellSoc = 0;
        var bodyCom = new HashSet<(int Region, int Bloc)>();
        foreach (var (reg, bloc, ideo, size, _) in observed)
        {
            bool body = size >= thr;
            if (body) nBody++;
            if (ideo == com)
            {
                nCellCom++; if (size > maxCellCom) maxCellCom = size;
                if (body) { nBodyCom++; bodyCom.Add((reg.Id, bloc)); }
            }
            else if (ideo == soc)
            {
                if (size > maxCellSoc) maxCellSoc = size;
                if (body) nBodySoc++;
            }
        }
        double all = 0, popCom = 0, popComSub = 0, popSoc = 0;
        foreach (var (reg, p) in w.AllPopsWithRegion())
        {
            if (p.Size <= 1e-9) continue;
            all += p.Size;
            var (bloc, ideo) = FactionSnapshot.BlocIdeo(p);
            if (ideo == com) { popCom += p.Size; if (!bodyCom.Contains((reg.Id, bloc))) popComSub += p.Size; }
            else if (ideo == soc) popSoc += p.Size;
        }
        double maxCoh = 0;
        foreach (var f in w.Factions)
            if (f.Ideo == com && bodyCom.Contains((f.Region.Id, f.Bloc)) && f.Cohesion > maxCoh) maxCoh = f.Cohesion;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        double Fr(double v) => all > 0 ? v / all : 0.0;
        double Tp(double v) => totalPop > 0 ? v / totalPop : 0.0;
        int q = Calendar.QuarterIndexOfTick(w.Tick);
        var row = new PrdfRow
        {
            Q = q,
            Head = string.Join('\t', q.ToString(inv), w.Year.ToString(inv), NameOf(w), ((int)w.Polity).ToString(inv),
                Discovery.Known(w.Discovered, Discovery.CommunismRow) ? "1" : "0", w.ManifestoIdeology == com ? "1" : "0",
                nBody.ToString(inv), nBodyCom.ToString(inv), nBodySoc.ToString(inv), F(Fr(popCom)), F(Fr(popComSub)), F(Fr(popSoc)),
                nCellCom.ToString(inv), F(Tp(maxCellCom)), F(Tp(maxCellSoc)), F(maxCoh)),
            Tail = string.Join('\t', F(w.RegimeTrackRecord[0]), F(w.RegimeTrackRecord[1]), F(w.RegimeTrackRecord[2]), F(w.RegimeTrackRecord[3])),
        };
        PrdfPending.GetOrCreateValue(w).Add(row);
    }

    /// <summary><see cref="Politics"/> の TriggerFactionRevolt の目標政体が決まった直後（計器だけ）：当四半期の行に目標を記す（Factions.Step より後に呼ばれる）。</summary>
    internal static void PrdfRevolt(World w, int target)
    {
        if (!PrdfPending.TryGetValue(w, out var rows) || rows.Count == 0) return;
        var row = rows[^1];
        if (row.Q != Calendar.QuarterIndexOfTick(w.Tick)) return;
        row.RevoltTarget = target; row.RevoltN++;
    }

    /// <summary>四半期の末（<see cref="Quarter"/>）：溜めた行を書き出す。</summary>
    static void PrdfTable(MultiNationSim sim, ulong seed)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var n in sim.Nations)
        {
            if (!PrdfPending.TryGetValue(n.World, out var rows) || rows.Count == 0) continue;
            var sk = PrdfSinks.GetValue(sim, _ => new PrdfSink());
            sk.W ??= Open("prdf", seed);
            if (!sk.Header) { sk.W.WriteLine(PrdfHeaderText); sk.Header = true; }
            foreach (var r in rows) sk.W.WriteLine(r.Head + "\t" + r.RevoltTarget.ToString(inv) + "\t" + r.RevoltN.ToString(inv) + "\t" + r.Tail);
            rows.Clear();
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void PrdfClose(MultiNationSim sim)
    {
        if (PrdfSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
