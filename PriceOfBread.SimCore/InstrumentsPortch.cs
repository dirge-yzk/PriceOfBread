namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (754) 内陸州の港の選び方の表 portch_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsMigform.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（世界を書かない）。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝事前登録 measurements/m754_20261102_prereg_pl.txt の P1（配線が関数の答えを実際に読んだか）・P2（鉄道の減衰の新÷旧）・P3（港が変わる州の数）を同じ盤面で読む。
// 取り方＝Instruments.Quarter の年末（q%4==3＝その四半期の港の張り直し <c>MultiNationSim.RewirePorts</c> の後）に全ての国の内陸州（港でない・座標のある州）を 1 巡し、
//   港を選ぶ純関数 <see cref="Simulation.PortChoice"/> を byLand=false（旧）と true（新）の両方で呼ぶ。portUsed・railUsedKm は州の欄の実際の値（エンジンが張った港と km）。
// 行＝内陸州×年。列（接尾辞：Km・Index）：year・nation・region・portGc（大圏の最小の港）・portLand（陸路の最小の港）・portUsed（実際に張った NearestPortName）・
//   gcKm（portGc までの大圏）・landOldKm（portGc までの陸路＝旧の PortLandKm）・landNewKm（portLand までの陸路）・railUsedKm（実際の NearestPortDistanceKm）・
//   railDecayOldIndex＝1/(1+k·gcKm/N)・railDecayNewIndex＝1/(1+k·landNewKm/N)・railDecayUsedIndex＝1/(1+k·railUsedKm/N)（k＝InlandPortDistanceDecay・N＝Diplomacy.DistanceNormKm・BilateralMarket と同じ式）・basinOld・basinNew（各港の海盆）。
// ⚠限界（計器が自己申告する）：年末の断面（四半期の途中の張り直しは見ない）／港の一覧の作り方（IsPort かつ座標あり）は Simulation.WirePortDistances の写し＝そこが替われば追いつかない／
//   railDecayOldIndex は旧の規則を今の盤面に当てた反実仮想（旗 1 の軌道では旧の規則の世界は走らない）。
public static partial class Instruments
{
    internal const string PortchHeaderText = "year\tnation\tregion\tportGc\tportLand\tportUsed\tgcKm\tlandOldKm\tlandNewKm\trailUsedKm\trailDecayOldIndex\trailDecayNewIndex\trailDecayUsedIndex\tbasinOld\tbasinNew";

    sealed class PortchSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PortchSink> PortchSinks = new();

    /// <summary>年末の四半期の末（<see cref="Quarter"/>）：全ての国の内陸州を 1 行ずつ書く。</summary>
    static void PortchTable(MultiNationSim sim, ulong seed, int q)
    {
        if (q % Calendar.QuartersPerYear != Calendar.QuartersPerYear - 1) return;
        var sk = PortchSinks.GetValue(sim, _ => new PortchSink());
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            var ports = new List<(double Lat, double Lon, string Name)>();
            foreach (var r in w.Regions)
                if (r.IsPort) { var pc = Climate.CityCoord(r.Name); if (pc.Lat != 0 || pc.Lon != 0) ports.Add((pc.Lat, pc.Lon, r.Name)); }
            if (ports.Count == 0) continue;
            double k = w.Coef.InlandPortDistanceDecay, norm = Diplomacy.DistanceNormKm;
            string nat = NameOf(w);
            foreach (var r in w.Regions)
            {
                if (r.IsPort) continue;
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) continue;
                var old = Simulation.PortChoice(cc.Lat, cc.Lon, ports, false);
                var nw = Simulation.PortChoice(cc.Lat, cc.Lon, ports, true);
                var po = Climate.CityCoord(old.Name); var pn = Climate.CityCoord(nw.Name);
                sk.W ??= Open("portch", seed);
                if (!sk.Header) { sk.W.WriteLine(PortchHeaderText); sk.Header = true; }
                sk.W.WriteLine(string.Join('\t', w.Year.ToString(inv), nat, r.Name, old.Name, nw.Name, r.NearestPortName,
                    F(old.GcKm), F(old.LandKm), F(nw.LandKm), F(r.NearestPortDistanceKm),
                    F(1.0 / (1.0 + k * old.GcKm / norm)), F(1.0 / (1.0 + k * nw.LandKm / norm)), F(1.0 / (1.0 + k * r.NearestPortDistanceKm / norm)),
                    SeaGraph.BasinAt(po.Lat, po.Lon).ToString(), SeaGraph.BasinAt(pn.Lat, pn.Lon).ToString()));
            }
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void PortchClose(MultiNationSim sim)
    {
        if (PortchSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
