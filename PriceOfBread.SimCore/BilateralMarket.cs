namespace PriceOfBread.SimCore;

/// <summary>二国間交易（作者裁定2026-08-11／設計＝docs/202608/二国間交易への置換_設計_完了.md【完了】.md）。
///
/// 旧＝各州が「世界の交易重心」までの距離でマージンを負い、1つの池（世界価格）に対して売買していた
/// ＝取引の相手が存在せず、輸入元を答えられなかった。
/// 新＝**州と州**が実経路（陸→最寄り港→海盆→港→陸）で結ばれ、買い手は「自分のところまで最も安く着く売り手」を選ぶ。
///
/// 保存則は旧と同型＝買い手が払い、売り手が受け取り、運賃だけが海運プールへ回る（leakhunt が検証）。
/// 変えたのは**相手と価格の決まり方**だけで、金の流れ方の骨格は変えていない。
/// </summary>
public sealed partial class InternationalMarket
{
    // ★発見・段7（2026-08-31）：四半期のペア貿易額 [売り手国][買い手国]（純記帳＝力学の読者は MultiNationSim.StepDiffusion のみ・四半期消費でゼロ化）。
    //   ⚠SaveGame は2次元配列を往復できない（Arr は1次元前提）＝**ジャグ配列**で持つ。DiffusionWeight=0 では一度も確保されない＝恒等。
    // ★清算内部の計器（2026-09-01・POB_TIME=1 のみ・純カウンタ＝挙動非接触。timeprobe が末尾で読む）
    internal static long TimeListsTicks, TimeClearTicks;   // 財ごとの (a)平均+売買リスト構築 (b)買い手清算ループ の累計（Stopwatch の刻み。⚠2026-09-26 是正＝旧は財ごとに ElapsedMilliseconds を足して端数を捨てていた＝1財1ms未満の清算がほぼ0に丸められ、高速化の後は「清算ループ」が実時間の数分の1しか数えなかった）
    internal static long CntGoods, CntBuyers, CntRollIters, CntScanSteps, CntSkips, CntFills, CntDeals;
    // ★無効な売り手を訪ねない（2026-09-26・高速化・厳密恒等）の計器：1a＝行ごと飛ばした買い手行（条件別）／1b＝自州価格 lp で打ち切った回数／1c＝国境の楔の下界で飛ばした売り手
    internal static long CntRowSkipNeed, CntRowSkipRef, CntRowSkipDead, CntRowSkipBudget, CntCutLp, CntWedgeSkips;
    // ★計算量の台帳（2026-09-30・<see cref="CostLedger"/>）＝清算が見た件数（買い手行・着値の充填）。⚠作業領域＝セーブに載らない（[NonSerialized]）・シミュの値はこれを1つも読まない。
    //   インスタンスのフィールド＝バッチで並列に回る別の試合と混ざらない（上の static の計器は POB_TIME の単独実行専用）。
    [System.NonSerialized] private long[]? _cost;
    internal long[] Cost => _cost ??= new long[CostLedger.Count];
    internal long[]? CostOrNull => _cost;
    private const int RolloverGuardMax = 400;   // ロールオーバー反復の停止保証（X は成約ごとに厳密に減る＝通常は数回で終わる。届いたら諦めるだけ＝破綻しない）
    public double[][]? PairFlowQ;
    public double[][]? LastPairFlow;   // ★段7：直近に消費した四半期のペア行列（純テレメトリ＝pairprobe/診断が読む。StepDiffusion がスワップ退避）
    /// <summary>★発見の伝わり（段2A・2026-10-01・設計 §1A-7＝距離でなく取引量・Q15 (a)＝財の次元の記帳）：国境を越えた成約1件ごとに
    /// ①世界のその財の取引額（金建て・四半期）と ②その財を運び手に持つ行のうち、売り手の国が解禁済みで買い手の国がまだの行へ買い手の記帳（<see cref="World.DiscoveryImportQ"/>）。
    /// 四半期末の共有層（MultiNationSim の発見の段）が接触＝②÷その行の運び手の財の①の和 に直して0へ戻す。</summary>
    public double[]? DiscoveryGoodFlowQ;
    [System.NonSerialized] private int[][]? _discCarriers, _discRowsByCarrier;   // 作業領域（財とレシピの定義から導く・セーブに載らない＝ロード後に同じ表を作り直す）
    internal int[][] DiscoveryCarriers(IReadOnlyList<Simulation> nations)
    {
        if (_discCarriers is null)
        {
            var ws = new List<World>(nations.Count);
            foreach (var n in nations) ws.Add(n.World);
            _discCarriers = Discovery.CarriersByRow(ws);
            _discRowsByCarrier = Discovery.RowsByCarrier(_discCarriers);
        }
        return _discCarriers;
    }
    private void AccumulateDiscovery(IReadOnlyList<Simulation> nations, int seller, int buyer, int g, double value)
    {
        var gf = DiscoveryGoodFlowQ ??= new double[Catalog.Goods.Length];
        if (g < 0 || g >= gf.Length || !(value > 0)) return;
        gf[g] += value;
        DiscoveryCarriers(nations);
        var rows = _discRowsByCarrier![g];
        if (rows.Length == 0) return;
        var ks = nations[seller].World.Discovered; var wb = nations[buyer].World; var kb = wb.Discovered;
        foreach (int r in rows)
            if (Discovery.Known(ks, r) && !Discovery.Known(kb, r))
                (wb.DiscoveryImportQ ??= new double[Catalog.Discoveries.Length])[r] += value;
    }
    private void AccumulatePairFlow(int seller, int buyer, double value, int n)
    {
        if (PairFlowQ is null || PairFlowQ.Length != n)
        {
            PairFlowQ = new double[n][];
            for (int i2 = 0; i2 < n; i2++) PairFlowQ[i2] = new double[n];   // 建国で国数が変わったら作り直し（四半期テレメトリ＝失って良い）
        }
        PairFlowQ[seller][buyer] += value;
    }

    /// <summary>売り手候補1件（財ごとに作り直す小さな配列＝毎月の割り当てを避けるため上限で確保して使い回す）。</summary>
    private bool LastPairWasLand;   // ★陸路可視化：直近 PairKm の勝者が陸直行か（Step 内でのみ読み書き・純テレメトリ）

    private readonly struct Seller(int node, double price, double stock)
    {
        public readonly int Node = node;        // tr 内の添字
        public readonly double Price = price;   // 金建ての局所価格（＝この売り手のFOB）
        public readonly double Stock = stock;   // 出せる量＝在庫−地元1ヶ月分（これ以上は売れない）
    }

    /// <summary>材ごとの「最大額の相手」を更新する（同額は先着＝走査順が決定論なので再現する）。</summary>
    private static void RecordPartner(string[]? slot, int g, string name, double value, double[] top)
    {
        if (slot is null || !(value > top[g])) return;
        top[g] = value; slot[g] = name;
    }

    /// <summary>距離の表の持ち越し（2026-09-29 高速化の段 S2）の作業領域＝スレッドごと（selfplay-batch は試合ごとにスレッド）。
    /// 表の値は署名（Sig）の純関数＝どの試合が残した表でも署名が同じなら同じ値。静的＝セーブに載らない（状態ではない）。</summary>
    private sealed class KmScratch
    {
        public int Tc = -1, G;
        public double LandRoute = double.NaN, RiskLambda = double.NaN;
        public double[] Km = [], Risk = [];
        public bool[] Land = [];
        public int[] Gen = [];
        public long[] Sig = [], SigNext = [], RSig = [], RSigNext = [];
    }
    [System.ThreadStatic] private static KmScratch? _kmScratch;

    private void StepBilateral(IReadOnlyList<Simulation> nations, Coefficients c, bool firstClearingOfQuarter = true)
    {
        int gc = _price.Length, nn = nations.Count;
        if (nn == 0) return;   // 参加国なし＝清算なし（下の nations[0] を守る）
        // ==== S4 裁定清算（2026-08-30 点火・docs/202608 §1-2〜1-4/§2）====
        //   数量を水準式で解く：買い X=min(価格静止 X_price, 物理 X_need)・売り Y=min(価格静止 Y_price, 在庫−地元1ヶ月分)。
        //   容量は同期制約（S4-1＝両端から減算・θ乗算撤去）。
        //   ★2026-09-26（棚卸し econ-15・回0-3）：旧経路（定数速度×自州規模・1買い手1成約＝TradeArbitrageClearing=0）と、容量の遅延乗数の腕
        //   （TradeCapacityHard=0）を旗ごと削除した＝既定では1命令も走っていなかった（差し戻しは git の1手）。
        // ★月次清算（2026-08-22）：LastPortThroughput は四半期末の読み手（港湾投資・保険・通行料・ArmyShape・UI）が四半期量として読む＝
        //   月次清算では四半期の第1清算だけリセットして以後は累計する。当清算ぶんは start との差分（トランジット通行料は差分に掛ける＝3重課金回避）。
        var portStart = new Dictionary<Region, double>();
        var goodById = new Good?[gc];
        foreach (var g in nations[0].World.Region.Goods) goodById[g.Id] = g;

        var exRate = new double[nn]; var tar = new double[nn]; var corn = new double[nn];
        var oban = new bool[nn]; var oexban = new bool[nn]; var milExportAcc = new double[nn];
        for (int i = 0; i < nn; i++)
        {
            var w0 = nations[i].World;
            exRate[i] = w0.FxRate;
            tar[i] = TariffRate(w0, c);
            corn[i] = CornFactor(w0.CornLaws);
            oban[i] = c.OpiumAddictionGrowth > 0 && w0.OpiumPossession != ReformLevel.None;
            oexban[i] = c.OpiumAddictionGrowth > 0 && w0.OpiumExportBan;
            milExportAcc[i] = w0.MilitaryExportAccess();
        }

        var dip = nations[0].World.Diplomacy;
        Chokepoint? Cp(string id) { if (dip is not null) foreach (var c2 in dip.Chokepoints) if (c2.Id == id) return c2; return null; }
        var suez = Cp(Diplomacy.SuezId); var pan = Cp(Diplomacy.PanamaId);
        bool Blocked(World w, Chokepoint? cp) => cp is { Owner.Nation: not null } && cp.Owner != w.SelfPower && dip is { } d && w.SelfPower is { } sp && d.IsAtWar(sp, cp.Owner!);

        // ---- 交易主体＝全active州。州ごとに「海に出る点（自港 or 最寄り港）・そこまでの陸路・アクセス・容量」を1回だけ作る ----
        // ★重心は作らない（廃止）。距離は下の対ごとに引く。
        var trList = new List<(int nation, Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk)>();
        // Stage C（裁定A3）：内陸国トランジット＝自国に港が無い州は外国の港を借りる。旧経路と同じ候補（構築時の上位2港）を読み、
        //   現所有国と交戦中なら次候補へ。借りた州は下で通行料（取扱高×TransitTollShare）を港所有国へ払う。
        bool transitOn = c.TransitEnabled > 0.5;
        Dictionary<string, int>? transitPortOwner = null;
        List<(int user, int owner, Region r)>? transitUses = null;
        if (transitOn)
        {
            transitPortOwner = new Dictionary<string, int>();
            for (int i2 = 0; i2 < nn; i2++)
                foreach (var r2 in nations[i2].World.Regions)
                    if (r2.State == RegionState.Active && r2.IsPort) transitPortOwner.TryAdd(r2.Name, i2);
            transitUses = [];
        }
        var natHostility = new double[nn][];
        var natAnyHostile = new bool[nn];
        var suezOkN = new bool[nn]; var panOkN = new bool[nn];
        for (int i = 0; i < nn; i++)
        {
            var W = nations[i].World;
            suezOkN[i] = suez is { IsOpen: true } && !Blocked(W, suez);
            panOkN[i] = pan is { IsOpen: true } && !Blocked(W, pan);
            natHostility[i] = dip is { } dH && W.SelfPower is { } spH ? dH.BasinHostility(spH) : new double[Basins.Count];
            bool any = false; foreach (var hv in natHostility[i]) if (hv > 1e-9) { any = true; break; }
            natAnyHostile[i] = any;
            double natAccess = W.ExternalTradeAccess();
            // ★(4) 潜在量比例の配分（TransportShareByPotential・既定1.0＝潜在量比例。0で均等割り）。前期の LastTradePotential を読む。
            //   規則は1本（Transport.ShareBasis／RegionShare＝州の移し替えで容量が州と一緒に移るときも同じ割合・2026-09-30 (118)）。分母が痩せたら（開幕・実績なし）均等へ倒す＝立ち上がりの跳躍なし
            var shareBasis = Transport.ShareBasis(W, c);
            // ★B-06（2026-09-03 点火・旗 TransportBaselineIsLand は 2026-09-26 に焼き込み）：鉄道以前の基礎容量は**陸**（道路・河川）。
            //   ★(166) 鉄道以前の陸の容量＝人口×基礎の目標（Transport.PreRailCapacity・旧＝国ごとの定数 TransportBaseline 50＝大国の内陸が通した割合 0.26〜10% に絞られた〔(165) の木の5種・1784〕→ 0.36〜1.0〔(166) の10種〕）。
            double railCapNat = W.RailCapacity + Transport.PreRailCapacity(W, c);
            double shipCapNat = W.ShipCapacity / (1.0 + W.ColonialShipBurden);
            foreach (var r in W.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double share = Transport.RegionShare(shareBasis, r);
                double railToPort = railCapNat * share;
                double shipEff = shipCapNat * share;
                if (firstClearingOfQuarter) { r.LastPortThroughput = 0; r.LastLandPartnerName = ""; r.LastLandPartnerFlow = 0; }   // 四半期の第1清算でリセット（従来＝毎回＝既定 true で厳密同値）
                portStart[r] = r.LastPortThroughput;                     // 当清算の差分の起点
                // 海に出る点＝港州なら自州、内陸州なら最寄り港（そこまでの陸路が landKm）。
                // 港へ繋がらない内陸州は海上交易に参加しない（旧実装の「鉄道0＝非接続」と同じ扱い）。
                var cc = Climate.CityCoord(r.Name);
                double lat = cc.Lat, lon = cc.Lon, landKm = 0; bool linked = r.IsPort;
                if (!r.IsPort && r.NearestPortName.Length > 0)
                {
                    var pc = Climate.CityCoord(r.NearestPortName);
                    if (pc.Lat != 0 || pc.Lon != 0) { lat = pc.Lat; lon = pc.Lon; landKm = r.PortLandKm; linked = true; }
                }
                else if (transitOn && !r.IsPort && r.TransitPortName.Length > 0)
                {
                    r.LastTransitPortName = "";
                    for (int cix = 0; cix < 2; cix++)
                    {
                        string tName = cix == 0 ? r.TransitPortName : r.TransitPort2Name;
                        double tKm = cix == 0 ? r.TransitLandKm : r.TransitLand2Km;
                        if (tName.Length == 0 || !transitPortOwner!.TryGetValue(tName, out int oi)) continue;
                        if (oi != i && dip is { } dT && W.SelfPower is { } spT && nations[oi].World.SelfPower is { } opT && dT.IsAtWar(spT, opT)) continue;   // 交戦＝この港は通れない
                        var pcT = Climate.CityCoord(tName);
                        if (pcT.Lat == 0 && pcT.Lon == 0) continue;
                        lat = pcT.Lat; lon = pcT.Lon; landKm = tKm; linked = true;
                        r.LastTransitPortName = tName;
                        if (oi != i) transitUses!.Add((i, oi, r));   // 自国港化していれば通行料なし
                        break;
                    }
                }
                if (lat == 0 && lon == 0) linked = false;   // 座標のない地点は経路を引けない
                // ★冬季閉鎖（作者裁定2026-08-11）：凍る港はその四半期だけ海に出る口にならない。
                //   ロシアのバルト海が冬に閉じる一方、黒海とペルシャ湾は通年＝**南下する動機が物理から出る**（グレートゲーム）。
                //   凍結は港の場所の性質なので、自国港でも借りた港でも同じように効く。
                // ★実バグ修正（2026-08-21・貿易の第2の栓ワークフローが検出）：W.Quarter の値域は {1,2,3,4}（Simulation.QuarterEnd で 4→1 へ巻き戻し）だが、
                //   Climate.PortFrozen は 0基準（Q1=0 が真冬・Q4=3）を期待する。旧＝ずれのせいで**凍結2四半期の港は真夏の Q3 にだけ凍り、
                //   凍結1四半期の港（リガ等）は一度も凍らなかった**。−1 で 0基準へ合わせる。
                if (linked && c.WinterPortFreeze > 0.5 && Climate.PortFrozen(lat, lon, W.Quarter - 1)) { linked = false; r.LastPortFrozen = true; }
                else r.LastPortFrozen = false;
                double portBonus = c.TransportEnabled ? c.PortCapToThroughput * r.PortCapacity : 0;
                double inlandRail = railToPort;
                if (!r.IsPort && c.InlandPortDistanceDecay > 1e-12 && r.NearestPortDistanceKm > 1e-9)
                    inlandRail = railToPort / (1.0 + c.InlandPortDistanceDecay * r.NearestPortDistanceKm / Diplomacy.DistanceNormKm);
                double cap = c.TransportEnabled ? (r.IsPort ? shipEff : Math.Min(inlandRail, shipEff)) + portBonus : double.MaxValue;
                r.LastTransportCap = cap;   // ★純テレメトリ（式の二重定義をしない）
                // ★(1.5)：陸直行のために**州自身の座標**も持つ（上の lat/lon は最寄り港へ置き換えられている）。
                //   ★港に繋がらない内陸州も陸路なら交易できる＝旧は linked=false で**交易から完全に除外**されていた
                //     （中央アジア・アフリカ内陸・独墺の内陸州）。seaOk＝海に出られるか（港経由の可否）を別に持つ。
                bool seaOk = linked;
                if (c.LandRouteEnabled > 0 && (cc.Lat != 0 || cc.Lon != 0)) linked = true;
                // ★S4-1：容量は同期制約（capLeft）で効く＝前月の律速率 LastTradeThrottle はアクセスに掛けない（同じ容量が2回効く二重拘束の回避）
                trList.Add((i, r, lat, lon, landKm, natAccess, cap, linked, cc.Lat, cc.Lon, seaOk));
            }
        }
        int tc = trList.Count;
        // ★2026-09-29（高速化の段 S2・厳密恒等）：州の表は配列で読む（List の添字は11項目の組を丸ごと写してから項目を読む＝清算の内側のループで1回約80バイト）。
        //   組の項目への書き込みは無い（List では書けない）＝読む値は同じ。
        var tr = trList.ToArray();
        // ★(3) 地形の陸路コスト係数（設計doc 20260826_地形と輸送コスト.DONE.md）＝州ごとに1回だけ算出。
        //   実効係数＝馬と鉄道の**railGate ブレンド**。★2026-10-01 (150)：railGate＝鉄道輸送の門＝発見の表の「機関車」の行（2値・Transport の容量と同じ1本）
        //   ＝旧＝技術水準の時計の傾斜 RailGate(TechLevel)（発見と関係なく開いた）。
        //   水路タグは >0 のとき陸脚コストをその値へ**置換**（川が道の代わり＝内陸なのに港に近い扱い）。
        //   表が全1.0・水運0なら terrCost は恒等的に1.0（旗 TerrainCostEnabled は 2026-09-26 に焼き込み＝表は常に効く）。
        var terrCost = new double[tc];
        var landRatio = new double[tc];   // ★州ごとの実効 陸/海コスト比（鉄道時代の低下・RailCostFraction=1.0で12のまま＝恒等）
        for (int t0 = 0; t0 < tc; t0++)
        {
            var W0 = nations[tr[t0].nation].World;
            double rg0 = Discovery.RailTransport(W0.Discovered) ? 1.0 : 0.0;   // 鉄道の門の1本＝機関車の発見
            landRatio[t0] = c.LandSeaCostRatio * (1.0 - rg0 * (1.0 - PoliticalQuantities.Clamp01(c.RailCostFraction)));
            double rg = rg0;
            int ti = (int)tr[t0].r.Terrain;
            double horse = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
            double rail = ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0;
            double m0 = horse + rg * (rail - horse);
            if (tr[t0].r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) m0 = c.TerrainWaterMajorCost;
            else if (tr[t0].r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) m0 = c.TerrainWaterMinorCost;
            terrCost[t0] = m0;
        }
        // ★(1.5)：各州の陸グラフ最寄りノードと接ぎ木距離を**1回だけ**引く（州×州のループで NearestNode を呼ばない）。
        var landNode = new int[tc]; var landGraft = new double[tc];
        if (c.LandRouteEnabled > 0)
            for (int t0 = 0; t0 < tc; t0++)
            {
                int nd = PathGraph.NearestNode(tr[t0].ownLat, tr[t0].ownLon);
                landNode[t0] = nd;
                landGraft[t0] = nd >= 0 ? Geo.DistanceKm(tr[t0].ownLat, tr[t0].ownLon, PathGraph.Nodes[nd].Lat, PathGraph.Nodes[nd].Lon) : 0;
            }
        else for (int t0 = 0; t0 < tc; t0++) landNode[t0] = -1;
        if (TradeReachDiag.On)   // 純テレメトリ（州の到達性）。Off では1命令も走らない＝ビット恒等
        {
            TradeReachDiag.Begin(nn);
            for (int t = 0; t < tc; t++)
            {
                var e = tr[t]; TradeReachDiag.RegTotal++; TradeReachDiag.ActiveByNation[e.nation]++;
                if (e.linked) { TradeReachDiag.RegLinked++; TradeReachDiag.LinkedByNation[e.nation]++; }
                else if (e.r.LastPortFrozen) TradeReachDiag.RegFrozen++;
                else if (!e.r.IsPort && e.r.NearestPortName.Length == 0 && e.r.TransitPortName.Length == 0) TradeReachDiag.RegNoPortLink++;
                else TradeReachDiag.RegNoCoord++;
            }
        }

        // ★手1（交易高速化doc・2026-08-30・厳密恒等）：州対距離の遅延メモ化。PairKm は Step 内で純関数（全入力が財ループ前に凍結・doc §2-1）
        //   ⚠対称保存はしない（2026-08-30実測＝armstrace DIFF）：PairKm は数学的に対称でも**ビット単位では非対称**
        //   （海路の経路加算順・viaSea の加算順が向きで異なる）＝計算した向きだけ保存＝旧と完全に同じ値・同じ演算。
        //   旧＝財×買い手×ロールオーバー×走査深さ ≈ 数百万回/清算 → メモ化後は高々 tc(tc+1)/2 ≈ 4.6万回。
        // ★表の持ち越し（2026-09-29 高速化の段 S2・厳密恒等）：PairKm(a,b) は両端の州の経路の入力（下の署名＝海に出る点の座標・海に出られるか・
        //   スエズとパナマの可否・陸脚の長さ・地形係数・陸/海の比・陸グラフの最寄りノードと接ぎ木）と c.LandRouteEnabled だけで決まる純関数
        //   （ほかに読むのはプロセスで不変の静的な表＝海グラフ・陸グラフ・接ぎ木の予算）。旧＝清算ごとに表を作り直し、同じ対を四半期に3回引き直していた
        //   （1試合で約1億回・清算の約2割）。新＝前の清算の表をスレッドの作業領域（KmScratch＝静的＝セーブに載らない・状態ではない）に残し、
        //   署名が1ビットでも違う州の行と列だけを捨てる（州の数か陸路の旗が違えば全て捨てる）＝残る対は同じ入力から同じ式で出した同じ値。
        //   陸路の記帳が読む印（kmLand）は「この清算で引いた対だけ」＝旧（清算ごとに false から）と同じに保つ（世代 kmGen）。
        //   危険料の表（下の PairRisk）も同じ作法で持ち越す＝署名は両端の海に出る点の座標と、その州の国のスエズとパナマの可否・海盆ごとの敵対度（c.RiskLambda は全体）。
        var ks = _kmScratch ??= new KmScratch();
        {
            const int KS = 10, KR = 4 + Basins.Count;
            if (ks.SigNext.Length < tc * KS) ks.SigNext = new long[tc * KS];
            if (ks.RSigNext.Length < tc * KR) ks.RSigNext = new long[tc * KR];
            var sg = ks.SigNext; var rg = ks.RSigNext;
            for (int t0 = 0; t0 < tc; t0++)
            {
                var e = tr[t0]; int o = t0 * KS;
                sg[o] = e.seaOk ? 1 : 0; sg[o + 1] = BitConverter.DoubleToInt64Bits(e.lat); sg[o + 2] = BitConverter.DoubleToInt64Bits(e.lon);
                sg[o + 3] = suezOkN[e.nation] ? 1 : 0; sg[o + 4] = panOkN[e.nation] ? 1 : 0; sg[o + 5] = BitConverter.DoubleToInt64Bits(e.landKm);
                sg[o + 6] = BitConverter.DoubleToInt64Bits(terrCost[t0]); sg[o + 7] = BitConverter.DoubleToInt64Bits(landRatio[t0]);
                sg[o + 8] = landNode[t0]; sg[o + 9] = BitConverter.DoubleToInt64Bits(landGraft[t0]);
                int r0 = t0 * KR; var hv0 = natHostility[e.nation];
                rg[r0] = BitConverter.DoubleToInt64Bits(e.lat); rg[r0 + 1] = BitConverter.DoubleToInt64Bits(e.lon);
                rg[r0 + 2] = suezOkN[e.nation] ? 1 : 0; rg[r0 + 3] = panOkN[e.nation] ? 1 : 0;
                for (int b = 0; b < Basins.Count; b++) rg[r0 + 4 + b] = BitConverter.DoubleToInt64Bits(hv0[b]);
            }
            // 署名の違う州の行（売り手）と列（買い手）を捨てる
            static void Drop(double[] tab, long[] sOld, long[] sNew, int k, int n)
            {
                for (int a = 0; a < n; a++)
                {
                    int o = a * k; bool same = true;
                    for (int q = 0; q < k; q++) if (sOld[o + q] != sNew[o + q]) { same = false; break; }
                    if (same) continue;
                    System.Array.Fill(tab, -1.0, a * n, n);
                    for (int j = 0; j < n; j++) tab[j * n + a] = -1.0;
                }
            }
            if (ks.Tc != tc)
            {
                ks.Tc = tc; ks.LandRoute = c.LandRouteEnabled; ks.RiskLambda = c.RiskLambda;
                ks.Km = new double[tc * tc]; System.Array.Fill(ks.Km, -1.0); ks.Land = new bool[tc * tc]; ks.Gen = new int[tc * tc];
                ks.Risk = new double[tc * tc]; System.Array.Fill(ks.Risk, -1.0);
            }
            else
            {
                if (BitConverter.DoubleToInt64Bits(ks.LandRoute) != BitConverter.DoubleToInt64Bits(c.LandRouteEnabled)) { ks.LandRoute = c.LandRouteEnabled; System.Array.Fill(ks.Km, -1.0); }
                else Drop(ks.Km, ks.Sig, sg, KS, tc);
                if (BitConverter.DoubleToInt64Bits(ks.RiskLambda) != BitConverter.DoubleToInt64Bits(c.RiskLambda)) { ks.RiskLambda = c.RiskLambda; System.Array.Fill(ks.Risk, -1.0); }
                else Drop(ks.Risk, ks.RSig, rg, KR, tc);
            }
            (ks.Sig, ks.SigNext) = (sg, ks.Sig);
            (ks.RSig, ks.RSigNext) = (rg, ks.RSig);
        }
        var kmTab = ks.Km;
        var kmLand = ks.Land;   // ★陸路可視化（2026-08-31・純テレメトリ）：この対の最短が陸直行だったか（PairKm と同じ比較＝算術に触れない）
        var kmGen = ks.Gen; int kmG = ++ks.G;
        double KmOf(int a, int b)
        {
            int ix = a * tc + b;
            double v = kmTab[ix];
            if (v >= 0) { kmGen[ix] = kmG; return v; }
            v = PairKm(a, b);
            kmTab[ix] = v;   // ⚠この向きのみ（鏡像は書かない＝ビット恒等の維持）
            kmLand[ix] = LastPairWasLand;
            kmGen[ix] = kmG;
            return v;
        }
        bool LandNow(int ix) => kmGen[ix] == kmG && kmLand[ix];   // この清算で引いた対の印だけ（旧＝清算ごとに false の表から）
        // 対ごとの実距離（km）。SeaGraph が内部でキャッシュしているので引き直しても実質ゼロコスト
        // （実測 routecost＝港81件・3240対で2回目 0ms）。陸路は LandSeaCostRatio 倍して足す＝港湾都市の優位が距離から出る。
        double PairKm(int a, int b)
        {
            double viaSea = double.PositiveInfinity;
            if (tr[a].seaOk && tr[b].seaOk)
            {
                double sea = Geo.SeaDistanceKm(tr[a].lat, tr[a].lon, tr[b].lat, tr[b].lon, suezOkN[tr[a].nation] && suezOkN[tr[b].nation], panOkN[tr[a].nation] && panOkN[tr[b].nation]);
                viaSea = sea + tr[a].landKm * terrCost[a] * landRatio[a] + tr[b].landKm * terrCost[b] * landRatio[b];   // ★(3)＋鉄道低下：陸脚＝地形×実効比
            }
            LastPairWasLand = false;   // ★既定＝海（下の陸直行が勝ったときだけ true）
            if (!(c.LandRouteEnabled > 0)) return viaSea;   // 否定形＝NaNも従来側。⚠休眠時は seaOk==linked なので両者とも真＝従来と厳密同値
            // ★(1.5) 陸直行：州自身の座標どうしを陸グラフの最短路で結ぶ。別陸塊なら +∞ ＝ 自動的に海が選ばれる。
            //   ⚠全点対の行列を引くだけ（PathGraph.LandMatrix）＝州の二乗のループでも Dijkstra を回さない。
            int na = landNode[a], nb = landNode[b];
            if (na < 0 || nb < 0) return viaSea;
            double raw = PathGraph.LandMatrix()[na * PathGraph.Nodes.Length + nb];
            if (double.IsPositiveInfinity(raw)) return viaSea;
            // ★(3)＋鉄道低下：陸直行にも両端の地形係数と実効比（経路の中間はタグを見ない＝粗いコア・思想⑧）
            double viaLand = (raw + landGraft[a] + landGraft[b]) * 0.5 * (terrCost[a] * landRatio[a] + terrCost[b] * landRatio[b]);
            LastPairWasLand = viaLand < viaSea;   // ★純テレメトリ（戻り値・算術は不変）
            return Math.Min(viaSea, viaLand);
        }
        // 経路のリスク料＝敵支配度の線積分（買い手側の敵対度で見る＝荷を受け取る側が保険を負う）。交戦なしで0。
        // ★通商破壊リスクのメモ化（2026-09-01・高速化第2弾）：PairRisk は Step 内の純関数（依存＝tr座標・natHostility・choke開閉＝全てStep冒頭で確定）
        //   なのに、戦時の買い手×売り手×財×ロールオーバーごとに海盆経路探索（RouteBasinKm）を再実行していた＝戦争が増える終盤×貿易復活で
        //   清算が2倍超遅化した主犯（kmTab と同じ番兵式・⚠**向き付き**＝買い手国の敵対ベクトルに依存＝鏡像は書かない）。純メモ＝ビット恒等。
        //   ★2026-09-29（高速化の段 S2）：表は清算をまたいで持ち越す（上の KmScratch・署名の違う州の行と列だけ捨てる）。
        var riskTab = ks.Risk;
        double PairRisk(int seller, int buyer)
        {
            int bi = tr[buyer].nation;
            if (!natAnyHostile[bi]) return 0;
            double cached = riskTab[seller * tc + buyer];
            if (cached >= 0) return cached;
            var kmB = SeaGraph.RouteBasinKm(SeaGraph.NearestSeaNode(tr[seller].lat, tr[seller].lon), SeaGraph.NearestSeaNode(tr[buyer].lat, tr[buyer].lon), suezOkN[bi], panOkN[bi]);
            double integral = 0;
            for (int b = 0; b < Basins.Count; b++) integral += natHostility[bi][b] * kmB[b];
            double risk = 1.0 - Math.Exp(-c.RiskLambda * integral / Diplomacy.DistanceNormKm);
            riskTab[seller * tc + buyer] = risk;
            return risk;
        }

        // 相手の台帳＝材ごとに「最大額の相手」だけを残す（全対を持つと 54×136×54 になる）。値は同期の最大額。
        var impTop = new double[nn][]; var expTop = new double[nn][];
        for (int i = 0; i < nn; i++)
        {
            var w0 = nations[i].World;
            if (w0.ImportFromByGood is null || w0.ImportFromByGood.Length != gc) { w0.ImportFromByGood = new string[gc]; w0.ExportToByGood = new string[gc]; }
            else { Array.Fill(w0.ImportFromByGood, null!); Array.Fill(w0.ExportToByGood!, null!); }
            impTop[i] = new double[gc]; expTop[i] = new double[gc];
        }

        var expByGood = new double[nn][]; var impByGood = new double[nn][];
        for (int i = 0; i < nn; i++)
        {
            var w0 = nations[i].World;
            if (w0.ExportsByGood is not { } eg0 || eg0.Length != gc) { w0.ExportsByGood = new double[gc]; w0.ImportsByGood = new double[gc]; }
            else { Array.Clear(eg0); Array.Clear(w0.ImportsByGood!); }
            expByGood[i] = w0.ExportsByGood!; impByGood[i] = w0.ImportsByGood!;
        }

        // ★支払いの一元化 段3-1（貿易・2026-09-29・計画 §10-12-9 の 3-1・C1〜C5・D4・A7・econ-07/B21）：旧＝国ごとの純流入 netFlow を Apply で
        //   国の全POP の貯蓄比に配っていた（輸入代金を払うのは国の全POP・運賃も関税の残りも同じ）＝払い手と受け手が名目と違った。
        //   新＝**成約ごとの当事者が払い、受け取る**：輸入代金・運賃・関税は輸入の捌き手（買い手の州の商人POP＝国内でその在庫を売って代金を受け取る口座と同じ・Q7 の b）が払う／
        //   売り手の受取（積出し価格＝次善の着値を最良の売り手の楔で割り戻した値）は④の持分（地代・国営持分・賃金＝Earned を通す）／運賃は船の持ち主（商人）へ／
        //   関税と穀物法の分は通関の時点で輸入国の国庫へ（econ-07）／金建ての額は各国の通貨へ為替で換算（A7）。
        //   清算所＝世界の外（払い手は清算所へ払い、受け手は清算所から受け取る）＝同じ国の州どうしの取引は同じ国の台帳に −x と +x が入って打ち消す（国内の決済）。
        var importVal = new double[nn]; var exportVal = new double[nn];   // 越境の輸入額（CIF＝積出し価格＋運賃）・輸出額（FOB＝積出し価格）・金建て（同じ国の州どうしは数えない＝分子と分母を同じ規則で）
        // ★(210) 取引の相手の本位（World.TradePartnerStandardBalance）＝越境の成約の積出し価格を買い手と売り手の両方に、相手の本位の符号（金＋1・銀−1・不換0）で積む。
        //   本位は清算の間に変わらない＝前もって国ごとに1回引く（成約の行で World を引かない）。
        var partnerStd = new double[nn]; var partnerBal = new double[nn]; var partnerTot = new double[nn];
        for (int i = 0; i < nn; i++) partnerStd[i] = nations[i].World.MonetaryLaw switch { MonetaryStandardLaw.Gold => 1.0, MonetaryStandardLaw.Silver => -1.0, _ => 0.0 };
        double freightPool = 0;   // 運賃（金建て）＝船腹の比で各国の船の持ち主へ
        var shipShare = new double[nn];
        // ★段3-1（C3）：運賃の受け手＝船の持ち主＝その国の商人POP（船を買うのは州の商人＝Transport.BuyToward）。旧＝運賃は各国の純流入に混ぜて国の全POP の貯蓄比（Apply）。
        //   商人の居ない国（合成の試験世界）は活動州の全POP。受け手の居ない国は船腹の比から外す（運賃が誰にも渡らない片側記帳を作らない）。
        var shipOwners = new List<Pop>[nn];
        double freightRate = 0;
        if (c.FreightRatePer10Mm > 1e-12)
        {
            double shipSum = 0; int eligible = 0;
            var ok = new bool[nn];
            for (int i2 = 0; i2 < nn; i2++)
            {
                var so = new List<Pop>();
                foreach (var r2 in nations[i2].World.Regions)
                    if (r2.State == RegionState.Active && r2.MerchantPopIndex >= 0 && r2.MerchantPopIndex < r2.Pops.Count) so.Add(r2.Pops[r2.MerchantPopIndex]);
                if (so.Count == 0) foreach (var r2 in nations[i2].World.Regions) if (r2.State == RegionState.Active) so.AddRange(r2.Pops);
                double sz = 0; foreach (var p2 in so) sz += p2.Size;
                shipOwners[i2] = so;
                if (!(sz > 1e-9)) continue;   // 受け手不在＝船腹の比から外す
                ok[i2] = true; eligible++;
                shipShare[i2] = Math.Max(0, nations[i2].World.ShipCapacity / (1.0 + Math.Max(0, nations[i2].World.ColonialShipBurden))); shipSum += shipShare[i2];
            }
            if (shipSum > 1e-9) for (int i2 = 0; i2 < nn; i2++) shipShare[i2] /= shipSum;
            else for (int i2 = 0; i2 < nn; i2++) shipShare[i2] = ok[i2] ? 1.0 / eligible : 0.0;   // ★船腹が全国0（開幕Q1・合成世界）＝受け手のいる国で均等に
            if (eligible > 0) freightRate = c.FreightRatePer10Mm;   // 受け手の居る国が1つも無ければ運賃を取らない
        }

        // 従量成分（重量/価値比×運賃革命）＝旧 Step のローカル関数と同式（A-高2）。前線技術は世界財＝旗に依らない。
        double frontierTech = 0;
        if (c.TransportRevolutionMax > 0)
            for (int i = 0; i < nn; i++) if (nations[i].World.TechLevel > frontierTech) frontierTech = nations[i].World.TechLevel;
        double freightRevolution = FreightRevolution(frontierTech, c);   // ★(169) 式は InternationalMarket.FreightRevolution（発見の着値と同じ1本）
        // ★貿易復活②（2026-08-15・tradewhy分解＝99_実装ログ§4-15）：運賃革命を**ad valoremマージンにも**適用。
        //   旧＝従量成分（WeightMarginAdd）だけが半減し、TransportMargin×(1+距離)の従価部分は1783年から不変
        //   ＝蒸気船・スエズ・鉄道が「重い財」しか安くしない歪み（史実1870-1913の運賃崩落は全面的）。
        //   ★点火2026-08-15（旗 FreightRevolutionAdValorem は 2026-09-26 に焼き込み）。
        double adValoremRev = freightRevolution;
        double WeightMarginAdd(int g)
        {
            if (c.TransportWeightMarginScale <= 0) return 0.0;
            double rp = goodById[g] is { } gd0 ? gd0.RefPrice : 0.0;
            return rp > 1e-9 ? c.TransportWeightMarginScale * Catalog.TransportWeight(g) / rp * freightRevolution : 0.0;
        }

        // ---- S4 のプール ----
        double twoEps = 2.0 * c.PriceElasticity;   // X_price の指数（ε=0.5 なら 1＝D·rp/d−P−I）
        int[]? nextS = null;   // ★連結リスト圧縮（2026-09-01）：在庫が尽きた売り手（と 2026-09-26 から容量が尽きた売り手）を走査から恒久除去する「次の生存索引」
        // ★逐次の候補表（2026-09-26・高速化・厳密恒等）：買い手1行のあいだ、訪問済みで有資格の売り手（着値＜MaxValue）を持つ＝伸長のみ・買い手ごとに0から。
        //   旧＝反復のたびに先頭から走査し直していた（成約の直後の同一の再走査・一時スキップの踏み直し）。下の買い手ループの注記を参照。
        int[]? candS = null; double[]? candD = null; double[]? candK = null; double[]? candF = null;   // candF＝着値の倍率（1＋運賃＋楔＋封鎖）＝売り手の受取を割り戻す分母
        // ★段3-1：支払能力＝輸入の捌き手（州の商人POP・商人の居ない州は州の全POP）の貯蓄（自国通貨・清算の始めの値）。旧＝国の全POP の貯蓄の和（Apply と同一集合）。
        //   この清算の受け取り（商人の仲介料・生産者のいない在庫の代金）は清算の終わりに入る＝この清算の支払能力に足さない（先に使えない）。
        // ★為替手形（2026-09-29 高速化の段 S3b・補給の鎖の緩衝＝緊急の輸入の財布）：捌き手の商人は積荷を担保に自国の銀行で手形を割り引いて払い、
        //   その在庫を国内で売った代金（EconomyStep の商人の取り分）で次の清算の始めに返す（Bank.Lend の1本＝債務の記録・預金・家計の与信を同じ額で）。
        //   旧＝支払能力は商人の貯蓄だけ（(108) Q7 の b）＝商人の貯蓄が尽きた州（人口の抜けた州・建国直後の国）は、日用品が参照の約49倍に張り付き
        //   配給0のまま何十年も輸入できなかった（S3a＝補給崩壊の国×四半期の95%）。史実：輸入は為替手形（ロンドン宛て手形）の引受と割引で賄われた。
        //   ①清算の始めに、前の清算の手形を貯蓄から返す（州の全POP＝社会移動で債務を運んだ POP も同じ規則）
        //   ②返し切れない手形の残る商人は新しい手形を振り出せない（不渡りの商人は引き受けてもらえない）＝支払能力は貯蓄だけ
        //   ③手形を振り出せる商人の支払能力は積荷が担保＝上限は量（X＝物理の不足と価格の量）だけ＝∞。払う時に貯蓄で足りない分だけ借りる。
        //   ⚠天井でもタイマーでもない（返す理由＝売った代金が入った・貸さない理由＝前の手形が返っていない）。商人の居ない州（試験世界）は借りない。
        var payerOf = new List<Pop>?[tc];   // 輸入の払い手（null＝払い手なし＝輸入しない）
        var budget = new double[tc];
        var bill = new bool[tc];            // 手形を振り出せる（商人の払い手・返し切れない手形が無い）
        for (int t2 = 0; t2 < tc; t2++)
        {
            var r2 = tr[t2].r;
            var w2 = nations[tr[t2].nation].World;   // 全ての世界に銀行（段2-2）
            foreach (var p2 in r2.Pops)
                if (p2.Debt.Value > 0) { double back = Math.Min(p2.Debt.Value, Math.Max(0, p2.Savings.Value)); if (back > 0) Bank.Lend(w2, p2, -back); }   // ①手形の決済（否定形の門＝NaN は返さない）
            if (r2.MerchantPopIndex >= 0 && r2.MerchantPopIndex < r2.Pops.Count) { payerOf[t2] = [r2.Pops[r2.MerchantPopIndex]]; bill[t2] = !(r2.Pops[r2.MerchantPopIndex].Debt.Value > 0); }
            else if (r2.Pops.Count > 0) payerOf[t2] = r2.Pops;
            if (payerOf[t2] is { } pl) foreach (var p2 in pl) budget[t2] += Math.Max(0, p2.Savings.Value);
            if (bill[t2]) budget[t2] = double.PositiveInfinity;   // ③積荷が担保（量は X が決める）
        }
        var payHouse = new double[tc];   // 払い手が清算所へ払う額（輸入代金＋運賃＋港湾料＋保険料・自国通貨）
        // ★支払いの一元化 段3-4（2026-09-30・C10＝処理の一元化 段4d）：**港湾料と海上保険の保険料は取引の時点の上乗せ**＝海路の成約ごとに輸入の捌き手（荷主）が払う
        //   （単価に入る＝払い手の支払能力が量を減らす・手形の振り出しも同じ額）。受け手＝港湾料は両端の港の州の投資家（港の持ち主・1端ごとに取扱高×PortFeeRate）・
        //   保険料は輸入国の引受手（その国の商人の頭数）。旧＝四半期末に国の全POP から貯蓄の比で（StepPorts／StepMarineInsurance）＝荷主でない内陸の農民も払った。
        //   港の州は IsPort（旧と同じ＝内陸州の取扱高には港湾料が掛からない）・持ち主のいない港は取らない・引受手のいない国は保険を掛けない（旧と同じ）。
        var portOwner = new Pop?[tc];     // 港の持ち主（港の州の投資家・不在は null＝港湾料を取らない）
        var portRecv = new double[tc];    // 港の持ち主の受け取り（その州の国の通貨）
        for (int t2 = 0; t2 < tc; t2++)
        {
            var r2 = tr[t2].r;
            if (c.PortFeeRate > 0 && r2.IsPort && r2.InvestorPopIndex >= 0 && r2.InvestorPopIndex < r2.Pops.Count && r2.Pops[r2.InvestorPopIndex].Size > 1e-9) portOwner[t2] = r2.Pops[r2.InvestorPopIndex];   // 受け手が居る＝移し方の1本が配れる（頭数＞0）
        }
        var underwriters = new List<Pop>?[nn];   // 引受手＝その国の活きた州の商人（居ない国・外交層の無い国は保険市場が立たない＝null）
        var premRecv = new double[nn];           // 引受手の受け取り（その国の通貨）
        if (c.MarinePremiumRate > 0)
            for (int i2 = 0; i2 < nn; i2++)
            {
                var w2 = nations[i2].World;
                if (w2.SelfPower is null) continue;
                var uw = new List<Pop>(); double uwSize = 0;
                foreach (var r2 in w2.Regions) if (r2.State == RegionState.Active && r2.MerchantPopIndex >= 0 && r2.MerchantPopIndex < r2.Pops.Count) { uw.Add(r2.Pops[r2.MerchantPopIndex]); uwSize += r2.Pops[r2.MerchantPopIndex].Size; }
                if (uwSize > 1e-9) underwriters[i2] = uw;   // 受け手が居る＝移し方の1本が頭数で配れる（払わせて配れない額を作らない）
            }
        var payTariff = new double[tc];  // 払い手が自国の国庫へ払う関税と穀物法の分（自国通貨）
        var paySub = new double[tc];     // 売買方針（高く買う）の補助＝国庫→払い手（自国通貨・国家の支払い率 r は払う時に掛ける）
        var rev = new double[tc]; var revX = new double[tc];   // 財ごとの売り手の受取（自国通貨）・うち越境（仲介料の基数）＝財の清算の終わりに④で分けて0へ戻す
        var expReg = new double[tc];     // 越境の輸出の受取（自国通貨・全財）＝ダンピング補助の重み（輸出を捌いた州の商人）
        double fxIntended = 0;           // 清算所の名目の出入りの差（受け取る側の通貨−払う側の通貨）＝為替の換算で世界の名目の基礎貨幣が動く分（A7）
        var potNew = new double[tc];   // 新定義の潜在量（容量で切る前の pot·W を両端に計上）＝θ/T4 の分母
        var capLeft = new double[tc]; for (int t2 = 0; t2 < tc; t2++) capLeft[t2] = tr[t2].cap;
        // ★国際市場の間引き（作者裁定 2026-09-29 の3・(6)）：前の清算で成約が無かった買い手の行（州×財）は眠る＝月次の清算では回さない。
        //   起こす理由＝①四半期の最初の清算で値段の差が運賃を超えているか軽く確かめる（下の 1b の打ち切り＝最良の売り手の着値の下界 ≧ 自州価格なら O(1) で終わる）
        //   ②この月にその財が足りなかった（Market.LastShortage＞0＝飢饉の輸入を遅らせない）。前の清算で成約したか＝Market.PrevImport（EconomyStep が月初に写す前の月の輸入）
        //   ＝新しい状態を持たない。⚠四半期に1回の清算（TradeMonthlyClearing=0）は毎回が四半期の最初＝間引かない。
        // ★不足で起きたが成約しない行を眠らせる（2026-09-30 計算量の予算 第2回・近似・作者裁定「起きている物だけ回す」）：②で起きた行（前の月に輸入が無く不足だけ）が
        //   前の走査で成約しなかった（Market.ClearFailLp＝その時の自州価格）なら、自州価格がその値を超えて上がるまで眠る＝起こす理由は「自州価格と着値の差が買い手の側から開いた」。
        //   売り手の側（在庫の出現・積出し価格・関税・運賃）で差が開いた分は①の四半期の最初の清算が拾う（印はそこで全て消す）＝遅れは (108) の間引きと同じく最長2か月。
        //   census（2026-09-30・種1全長）＝この形の行は清算の着値の充填の約1割（不足だけで起きた行の成約の額の 6.7%＝貿易額の全体の 0.66% が四半期の最初まで遅れる）。
        //   ⚠印は作業領域（[NonSerialized]・セーブに載らない）。四半期の境のセーブは次の清算が①＝印を作り直す（往復は一致）。
        // ★月の清算は潜在量を書かない＋買い手の容量が尽きた行は回さない（2026-09-30 (130) 計算量の予算・清算の眠りの切り分け）：
        //   ①潜在量（容量で切る前の量＝州の輸送の容量の取り分〔Transport.ShareBasis〕・混雑の信号〔律速率＝Transport.Step の目標・港の拡張〕の分母）は
        //     全ての行が起きている清算（四半期の最初）で測る。旧＝月の清算も上書きしていた＝眠った行（上の間引きと不足で起きた行の眠り）の分を数え漏らした
        //     潜在量が、次の月の取り分と四半期末の港の拡張の判定に入っていた（計器 (130)＝局所の盤面12で州の潜在量の差 2〜13%・律速率の差 3〜21%）。
        //   ②月の清算で、行の番が来た時に買い手の州の輸送の容量が尽きている（capLeft÷重量≦1e-12＝下の成約の量 qA の上限）行は1件も成約できない（ソースから決まる）。
        //     旧＝有効な売り手を回って潜在量だけを書いていた（自州価格が上がれば起きる＝起こす理由が効かない行）。①で月の清算の潜在量は読まれない＝回さない。
        //     印は走査して成約しなかった行と同じ（自州価格）。計器（(130)・5種の全長・読むだけ）＝月の清算の充填の 35% がこの行（清算の充填の全体の 18.9%）。
        bool thin = c.TradeMonthlyClearing > 0 && !firstClearingOfQuarter;
        if (!thin)
            for (int i = 0; i < nn; i++)
                foreach (var rC in nations[i].World.Regions)
                    foreach (var mC in rC.Markets) mC.ClearFailLp = 0;   // ①＝全ての行を確かめ直す（前の四半期の印を残さない）

        var sellers = new List<Seller>(tc);
        var buyers = new List<(int Node, double Price)>(tc);
        var distTop = new double[tc];   // 距離の診断＝どの取引を代表に採ったか（最大額）

        var gOrder = new int[gc];
        for (int gi0 = 0; gi0 < gc; gi0++) gOrder[gi0] = gi0;
        {   // 価値密度（RefPrice/重量）降順＝容量・予算の先取りが価値順（新定数なし・同値はid昇順＝決定論）
            var dens = new double[gc];
            for (int gi0 = 0; gi0 < gc; gi0++) dens[gi0] = goodById[gi0] is { } gdd ? gdd.RefPrice / Math.Max(1e-9, Catalog.TransportWeight(gi0)) : 0.0;
            Array.Sort(gOrder, (a, b) => dens[b] != dens[a] ? dens[b].CompareTo(dens[a]) : a.CompareTo(b));
        }
        var swSec = MultiNationSim.TimeOn ? System.Diagnostics.Stopwatch.StartNew() : null;   // ★計器（POB_TIME）
        for (int gIdx = 0; gIdx < gc; gIdx++)
        {
            int g = gOrder[gIdx];
            var gd = goodById[g];
            if (swSec is not null) { TimeClearTicks += swSec.ElapsedTicks; swSec.Restart(); }   // ★前財の清算ループ時間（初回は≈0）
            _volume[g] = 0;
            // ★植民地財も二国間清算に載る（点火2026-08-26・2026-09-28 に旗 TradeColonialGoods ごと焼き込み）。
            if (gd is null || gd.NonTradeable) continue;

            // ---- 平均価格（世界価格の後継）＝参加州の金建て局所価格の単純平均。市場ではなく**統計** ----
            //   用途は①表示②数量の正規化（旧が世界価格で割っていた分母）だけ。清算には使わない。
            double sum = 0; int cnt = 0;
            for (int t = 0; t < tc; t++)
            {
                if (!tr[t].linked) continue;
                double lp0 = tr[t].r.Markets[g].Price * exRate[tr[t].nation];
                if (lp0 > 1e-9) { sum += lp0; cnt++; }
            }
            if (cnt == 0) continue;
            double avg = sum / cnt;
            if (avg <= 1e-9) continue;
            _price[g] = c.WorldPriceStatUnclamped > 0 ? avg : Math.Clamp(avg, gd.RefPrice * PriceBandFloorMul, gd.RefPrice * PriceBandCeilMul);   // 表示用の「世界価格」＝平均価格（★器 WorldPriceStatUnclamped=1 でクランプなし＝偏在財で計器が振り切れない）

            double weightAdd = WeightMarginAdd(g);
            double Wg = Catalog.TransportWeight(g);   // 財の輸送重量（積載・容量の単位）
            bool milGood = Array.IndexOf(c.MilitaryExportGoods, g) >= 0;

            // ---- 売り手候補＝出せる在庫のある州の全て。価格の昇順（枝刈りのため） ----
            // ★段3-1（R6・英の交易の診断 2026-09-28 の裁定）：旧＝売り手は平均×(1+TradeBandWiden) より安い州・買い手は平均÷(1+w) より高い州だけ
            //   ＝表示用の単純平均（統計）が成立の門になっていた（偏在財で平均が振れると組が閉じる・銀の平均は為替に自己参照する）。
            //   新＝帯を撤去し、成立は組ごとの判定（最良の売り手の着値＜自州価格・下の 1b／1c）だけ。係数 TradeBandWiden を削除。
            sellers.Clear();
            for (int t = 0; t < tc; t++)
            {
                if (!tr[t].linked) continue;
                var m = tr[t].r.Markets[g];
                double lp = m.Price * exRate[tr[t].nation];
                if (lp <= 1e-9 || m.Stock <= 1e-9) continue;
                int ni = tr[t].nation;
                double sellAcc = tr[t].access * (milGood ? milExportAcc[ni] : 1.0);
                if (c.IsolationExportWeight > 0) sellAcc *= System.Math.Max(0, 1.0 - c.IsolationExportWeight * System.Math.Max(0, tar[ni]) * System.Math.Max(0, tar[ni]));   // ★鎖国の対称化（高関税レジーム＝輸出側も閉じる・0＝恒等）
                if (g == c.OpiumGoodId && oexban[ni]) sellAcc *= Contraband.LeakFraction(tr[t].r.AdminFill, c);   // 輸出禁止＝取り締まれた分だけ止まる
                if (sellAcc <= 1e-12) continue;
                // 出せる量＝在庫−地元1ヶ月分（裁定3＝剥ぎ取り不可）。速度の蓋は無い＝量は買い手の X と売り手の Y_price が決める
                double availS = m.Stock - m.TargetStock;
                if (!(availS > 1e-12)) continue;
                sellers.Add(new Seller(t, lp, availS));   // sellAcc は成約時に再計算（sellers は後で価格順ソート＝並走リスト不可）
            }
            if (sellers.Count == 0) continue;
            sellers.Sort((a, b) => a.Price != b.Price ? a.Price.CompareTo(b.Price) : string.CompareOrdinal(tr[a.Node].r.Name, tr[b.Node].r.Name));   // ★買い手ソート(:515)と統一＝カルチャ非依存の決定論的タイブレーク（wave2b是正）

            var left = new double[sellers.Count];   // 売り手ごとの残り（出せる量から成約ごとに減る）
            for (int s = 0; s < sellers.Count; s++) left[s] = sellers[s].Stock;
            int headS = 0;   // 先頭の生存売り手（価格昇順の死んだ接頭辞を跨ぐ）
            // ★連結リスト圧縮：財ごとに全員生存で初期化
            if (nextS is null || nextS.Length < sellers.Count) nextS = new int[Math.Max(64, sellers.Count * 2)];
            for (int s = 0; s < sellers.Count; s++) nextS[s] = s + 1;

            // ---- 買い手＝全ての州（段3-1 の R6＝帯を撤去）。**高く買う相手から順に**応じる（作者指示2026-08-11）。
            //   売り手の在庫は有限なので、処理順がそのまま「誰に売るか」になる＝局所価格の高い買い手を先に通す。
            //   同値は州名で全順序＝決定論（走査順に依存しない）。
            buyers.Clear();
            for (int t = 0; t < tc; t++)
            {
                if (!tr[t].linked) continue;
                double lpB = tr[t].r.Markets[g].Price * exRate[tr[t].nation];
                if (lpB > 1e-9) buyers.Add((t, lpB));
            }
            if (buyers.Count == 0) continue;
            buyers.Sort((x, y) => x.Price != y.Price ? y.Price.CompareTo(x.Price) : string.CompareOrdinal(tr[x.Node].r.Name, tr[y.Node].r.Name));
            if (swSec is not null) { TimeListsTicks += swSec.ElapsedTicks; swSec.Restart(); CntGoods++; CntBuyers += buyers.Count; }   // ★計器
            var costC = Cost; costC[CostLedger.ClearingRows] += buyers.Count;   // 計算量の台帳（診断）：この財で見る買い手行（眠った行・飛ばした行も1件）
            long nFill = 0;   // 着値の充填の局所の数（財の終わりに台帳へまとめて足す）
            foreach (var (t, lp) in buyers)
            {
                var m = tr[t].r.Markets[g];
                if (thin && !(m.PrevImport > 0) && !(m.LastShortage > 0)) continue;   // ★間引き：眠っている行（上の thin の注記）
                if (thin && !(m.PrevImport > 0) && m.ClearFailLp > 0 && !(lp > m.ClearFailLp)) continue;   // ★不足で起きたが前に成約しなかった行＝自州価格が上がるまで眠る（上の注記・近似）
                m.ClearFailLp = 0;   // 印は走査して成約しなかったときだけ（下の行の終わり）＝門で飛ばした行・成約した行は印なし
                int ni = tr[t].nation;
                int bias = nations[ni].World.TradeBiasOf(g);
                double buyDear = bias > 0 ? c.TradeBiasStrength : 0;
                double protect = Math.Max(0, tar[ni]);
                double cornPen = gd.Tier == GoodTier.Staple || gd.GrainChain ? corn[ni] * c.CornLawImportPenalty : 0;   // 穀物法の客体＝主食＋麦の連鎖（旗 CornLawGrainChainGate は 2026-09-26 に焼き込み）
                double duty = protect * c.TariffProtect + cornPen;   // ★段3-1（econ-07）：関税と穀物法の分＝通関で輸入国の国庫へ入る楔（積出し価格に掛かる）
                double wedge = duty - buyDear;   // 買い手側の敷居（関税・穀物法・売買方針）
                // ★貿易復活①（2026-08-15）：関税・穀物法は**国境の税**＝同国の売り手には掛けない（内国関税は1789仏/1834独関税同盟で消えた制度。
                //   旧＝自国州からの購入にも関税wedgeが掛かり、国内州間交易まで殺していた＝tradewhy分解の壁の片翼）。
                //   ★点火2026-08-15（旗 TariffBorderOnly は 2026-09-26 に焼き込み）。買い手方針buyDearは境を問わず据え置き。
                double wedgeHome = -buyDear;   // 同国＝関税・穀物法なし（方針だけ残る）

                // ==== S4：水準式の裁定清算（同じ買い手が X が尽きるか売り手/容量/予算が尽きるまで次善へ回る）====
                // ★逐次の候補表（2026-09-26・高速化・厳密恒等）。旧＝反復のたびに先頭(headS)から走査し直し、訪問した有資格の売り手の中から
                //   最良(best)と次善(second)を引いていた。反復と反復のあいだに変わるのは「最良の売り手1人の資格」だけなので、走査をやり直さない：
                //   (a) 成約の後も最良が有資格のまま（在庫 left と容量 capLeft が残る）なら、次の反復は旧と1点も違わない走査になる
                //       ＝前回の best/second をそのまま使う（旧は成約3.16億回のうち99.8%でこの同一の再走査をしていた）。
                //       ⚠他の売り手の資格は成約で変わらない＝売り手の行は財ごとに州1つにつき1件・変わる容量は買い手 t と売り手 snA の2州だけで、
                //         買い手 t 自身の容量が尽きたら下の break で行ごと終わる。着値は買い手の行のあいだ不変（価格・距離・楔・危険料）。
                //   (b) 最良が外れた（一時スキップ・在庫切れ・容量切れ）ときは、候補表（訪問済みで有資格の売り手）から1点除いて最良と次善を引き直し、
                //       前回の打ち切り位置(resS)から同じ規則で走査を続ける。1点除くと各位置の「その時点までの最良」は下がらない＝旧の再走査は
                //       resS より前で打ち切られない＝旧と同じ集合を訪問する。最良＝(着値, 添字) の辞書式最小（旧の厳密比較＝同値は先に訪問した方＝添字の小さい方）、
                //       次善＝着値の多重集合の2番目の値（旧の更新規則と同値）＝走査順に依らない。
                //   ⇒ 一時スキップの印（旧 skipS）は二度と読まれない（外した売り手は resS より前にある）＝不要。着値の遅延表（旧 delivA/kmA）も、
                //     各売り手を1行につき1回しか訪問しないので不要（距離・危険料の表 kmTab/riskTab は清算全体の純メモのまま＝値は同じ。
                //     ⚠下の 1a/1b/1c から埋まる対は減る＝逆向きの kmLand を読む陸路の記帳（LastLandPartner*・読み手は App の地図だけ）が1ulp の同着で変わりうる）。
                //   ⚠候補表に入れるのは着値 < MaxValue の売り手だけ＝旧の更新規則（初期値 MaxValue との厳密比較）で best/second に一度も入らない売り手（∞・NaN）は旧でも読まれない。
                // ★無効な売り手（着値 ≥ 自州価格 lp・∞・NaN）を訪ねない（2026-09-26・高速化・厳密恒等）。無効な売り手は行の結果に効かない：
                //   最良になれば下の `!(lp > bestDelA)` で書き込みの前に行が終わる／次善になっても単価は min(lp, 次善)＝lp（次善が MaxValue と同じ値）／
                //   候補表から外す経路と記帳は最良が有効なときしか通らない／(着値, 添字) の最良は有効な売り手が1人でもいれば無効な売り手の有無で変わらない。
                //   ⇒ 有効な売り手の集合・訪ねる順・各反復で起きること（成約／外す／行を終える）が旧と同じ＝同じ組が同じ順・量・単価で成約する。
                // 1a：行を丸ごと飛ばす＝最初の反復が書き込みの前に必ず終わる行（X≦0・NaN か pot≦0・NaN）。どれも買い手だけで決まり、行の始めの値が最初の反復で読む値そのもの。
                //   ⚠買い手の容量が尽きた行（capLeft[t]≦0）は飛ばさない＝有効な売り手ごとに potNew を書く（翌月の輸送容量の配分の入力）。
                if (!(m.TargetStock + m.LastDemand - m.LastProduced - m.Stock > 0)) { if (MultiNationSim.TimeOn) CntRowSkipNeed++; continue; }   // 物理の不足 xNeed≦0（下の xNeed と同じ式）
                if (!(gd.RefPrice * tr[t].r.PriceLevel * exRate[ni] > 1e-9)) { if (MultiNationSim.TimeOn) CntRowSkipRef++; continue; }   // rpT≦1e-9＝xPrice が0の枝（下の rpT と同じ式）
                if (!(m.LastDemand > 0) && !(m.LastProduced + m.LastImport < 0)) { if (MultiNationSim.TimeOn) CntRowSkipDead++; continue; }   // 需要0＝D·Pow≦0 から P+I≧0 を引く＝xPrice≦0
                if (!(budget[t] > 0)) { if (MultiNationSim.TimeOn) CntRowSkipBudget++; continue; }   // 支払能力0＝pot≦0（この清算の中で増えない）
                if (thin && !(capLeft[t] / Math.Max(1e-9, Wg) > 1e-12)) { m.ClearFailLp = lp; continue; }   // ★(130)②買い手の容量が尽きた行＝成約できない（qA の上限）・月の清算の潜在量は読まれない＝回さない。印は走査して成約しなかった行と同じ（上の (130) の注記・否定形＝NaN も回さない）
                if (candS is null || candS.Length < sellers.Count) { int cn0 = Math.Max(64, sellers.Count * 2); candS = new int[cn0]; candD = new double[cn0]; candK = new double[cn0]; candF = new double[cn0]; }
                double unmetQ = 0;   // ★潜在量は組ごとに1回（作者裁定 2026-09-29 の2）＝この行で「容量で切られた不足」として既に数えた量（同じ不足を次善の売り手で数え直さない）
                double baseMul = 1 + c.TransportMargin * adValoremRev;   // 距離0・危険0の運賃の倍率（lbMul と同じ評価順＝(1＋TM·adV)＋楔）
                double lbMul = baseMul + Math.Min(wedge, wedgeHome);   // 昇順の打ち切りの下界の倍率（真の下界＝運賃革命の従価割引込み・旧の式そのもの）
                int candN = 0, resS = headS, resPrev = -1;   // 候補の数／次に調べる売り手（打ち切り位置）とその手前の生存売り手
                int bestA = -1, bestPos = -1; double bestDelA = double.MaxValue, secondDelA = double.MaxValue, bestKmA = 0;
                bool keepBest = false;   // (a)：前回の best/second をそのまま使う
                bool dealtRow = false;   // この行で1件でも成約したか（成約しなかった行に印＝ClearFailLp）
                int guard = 0;
                while (true)
                {
                    if (MultiNationSim.TimeOn) CntRollIters++;   // ★計器
                    if (++guard > RolloverGuardMax) break;   // 停止保証
                    if (!keepBest)
                    {
                        // (b) 候補表から最良と次善を引き直す（(着値, 添字) の辞書式＝候補表の並び順に依らない）
                        bestA = -1; bestPos = -1; bestDelA = double.MaxValue; secondDelA = double.MaxValue;
                        for (int k = 0; k < candN; k++)
                        {
                            double dk = candD![k]; int sk = candS[k];
                            if (dk < bestDelA || (dk == bestDelA && sk < bestA)) { secondDelA = bestDelA; bestDelA = dk; bestA = sk; bestPos = k; }
                            else if (dk < secondDelA) secondDelA = dk;
                        }
                        // 打ち切り位置から同じ規則で走査を続ける（初回は headS から＝旧の走査そのもの）
                        int s2 = resS, prevS = resPrev;
                        while (s2 < sellers.Count)
                        {
                            if (left[s2] <= 1e-12)
                            {   // ★連結リスト圧縮：left は財の清算内で単調減少＝尽きたら戻らない。外すのは旧走査でも必ず continue した
                                //   純no-opだけ＝訪問集合・順序・打ち切りは不変（ビット恒等）。
                                if (MultiNationSim.TimeOn) CntSkips++;   // ★計器
                                int nxS = nextS![s2];
                                if (prevS < 0) headS = nxS; else nextS[prevS] = nxS;
                                s2 = nxS; continue;
                            }
                            if (capLeft[sellers[s2].Node] <= 1e-12)
                            {   // ★容量切れの売り手も同じく外す（2026-09-26）：capLeft は清算の中で減る一方（成約ごとに qA×Wg＞0 を引く）＝一度尽きたら
                                //   以後この財の全ての買い手で必ず continue する純no-op（旧＝財をまたいで全ての買い手・全ての反復で踏み直していた）。
                                if (MultiNationSim.TimeOn) CntSkips++;   // ★計器
                                int nxS = nextS![s2];
                                if (prevS < 0) headS = nxS; else nextS[prevS] = nxS;
                                s2 = nxS; continue;
                            }
                            // 昇順の打ち切り。★1b：下界 Price·lbMul≦着値（浮動小数でも成り立つ＝旧の打ち切りが頼る不等式と同じ）と価格の昇順から、
                            //   Price·lbMul≧lp の位置から先は全員無効。旧が lp より先まで走るのは有効な売り手が候補表に1人もいない（bestDelA≧lp）ときだけで、
                            //   そのとき余分に訪ねるのは全員無効＝有効な売り手がいる間は bestDelA<lp なので打ち切りの位置は旧と同じ。
                            double lbv = sellers[s2].Price * lbMul;
                            if (lbv >= bestDelA) break;
                            if (lbv >= lp) { if (MultiNationSim.TimeOn) CntCutLp++; break; }
                            if (MultiNationSim.TimeOn) CntScanSteps++;   // ★計器（各売り手は1行につき1回だけ訪問）
                            double wS2 = tr[sellers[s2].Node].nation == ni ? wedgeHome : wedge;
                            // ★1c：売り手ごとの下界 Price·((1＋TM·adV)＋楔)≦着値 で無効と分かる売り手（関税・穀物法で閉じた外国の売り手）は、距離と危険料を引かずに飛ばす。
                            //   ⚠生きている売り手なので prevS を進める（進めないと上の連結リストの付け替えが壊れる）。打ち切りの位置は min(bestDelA, lp) で旧と同じ。
                            if (sellers[s2].Price * (baseMul + wS2) >= lp) { if (MultiNationSim.TimeOn) CntWedgeSkips++; prevS = s2; s2 = nextS![s2]; continue; }
                            if (MultiNationSim.TimeOn) CntFills++;   // ★計器（着値の計算）
                            nFill++;
                            double km2 = KmOf(sellers[s2].Node, t);
                            double risk2 = PairRisk(sellers[s2].Node, t);
                            double bb2 = gd.Tier == GoodTier.Staple ? c.BlockadeStapleBias * risk2 : 0.0;
                            double mar2 = c.TransportMargin * (1.0 + c.DistanceMarginScale * km2 / Diplomacy.DistanceNormKm) * adValoremRev
                                        + weightAdd * (km2 / Diplomacy.DistanceNormKm) + risk2;
                            double fac2 = 1 + mar2 + wS2 + bb2;   // 着値の倍率（段3-1＝売り手の受取を割り戻す分母）
                            double del2 = sellers[s2].Price * fac2;
                            if (del2 < double.MaxValue)
                            {
                                candS[candN] = s2; candD![candN] = del2; candK![candN] = km2; candF![candN] = fac2;
                                if (del2 < bestDelA) { secondDelA = bestDelA; bestDelA = del2; bestA = s2; bestPos = candN; }   // 新しく訪問した売り手は候補表の誰よりも添字が大きい＝旧の厳密比較と同じ
                                else if (del2 < secondDelA) secondDelA = del2;
                                candN++;
                            }
                            prevS = s2; s2 = nextS![s2];
                        }
                        resS = s2; resPrev = prevS;
                        bestKmA = bestPos >= 0 ? candK![bestPos] : 0;
                    }
                    keepBest = false;
                    if (bestA < 0 || !(lp > bestDelA)) break;   // 否定形＝NaNは取引しない
                    double unitA = Math.Min(lp, double.IsPositiveInfinity(secondDelA) ? lp : secondDelA);
                    unitA = Math.Max(unitA, sellers[bestA].Price);
                    int snA = sellers[bestA].Node; int siA = tr[snA].nation;
                    var msA = tr[snA].r.Markets[g];
                    // --- 買い手 X（§1-2）：価格静止と物理の2腕 min（低すぎる着値で静止点が無い穴を X_need が塞ぐ）---
                    double rpT = gd.RefPrice * tr[t].r.PriceLevel * exRate[ni];
                    // ★価格振動の根②（2026-09-07・2026-09-08 点火＝旗 TradeBuyerSolvesAtUnitPrice は 2026-09-26 に焼き込み）：買い手の静止点を「実際に払う単価 unitA」で解く
                    //   （売り手側 yPrice と同じ物差し）。旧式＝最良の着値 bestDelA は、単一供給では unitA=lp より安い価格で解く＝過剰発注→閾で0の bang-bang だった。
                    double xPrice = rpT > 1e-9 && unitA > 1e-9
                        ? m.LastDemand * Math.Pow(rpT / unitA, twoEps) - (m.LastProduced + m.LastImport) : 0.0;
                    double xNeed = m.TargetStock + m.LastDemand - m.LastProduced - m.Stock;
                    double X = tr[t].access * Math.Max(0, Math.Min(xPrice, xNeed));
                    if (g == c.OpiumGoodId && oban[ni]) X *= Contraband.LeakFraction(tr[t].r.AdminFill, c);
                    // --- 売り手 Y（§1-3）：u が高いほど地元を絞って出す（固定点＝現在の輸出率）---
                    double sellAccB = tr[snA].access * (milGood ? milExportAcc[siA] : 1.0);
                    if (c.IsolationExportWeight > 0) sellAccB *= System.Math.Max(0, 1.0 - c.IsolationExportWeight * System.Math.Max(0, tar[siA]) * System.Math.Max(0, tar[siA]));   // ★鎖国の対称化（高関税レジーム＝輸出側も閉じる・0＝恒等）
                    if (g == c.OpiumGoodId && oexban[siA]) sellAccB *= Contraband.LeakFraction(tr[snA].r.AdminFill, c);
                    double rpS = gd.RefPrice * tr[snA].r.PriceLevel * exRate[siA];
                    double yPrice = rpS > 1e-9 ? (msA.LastProduced + msA.LastImport) * unitA / rpS - msA.LastDemand - msA.LastExport : 0.0;
                    double Y = sellAccB * Math.Max(0, Math.Min(yPrice, left[bestA]));   // 物理上限＝avail（=Stock−TargetStock・left が消化を追う）
                    double frA = freightRate > 1e-12 ? freightRate * bestKmA / Diplomacy.DistanceNormKm : 0.0;
                    // ★段3-1（econ-07/B21）：売り手の受取＝次善の着値（単価 unitA）を最良の売り手の着値の倍率（1＋運賃＋楔＋封鎖）で割り戻した積出し価格。
                    //   旧＝単価を丸ごと売り手に払った＝輸入国の関税・穀物法と影の運送（従価の運送の楔・危険料）が外国の売り手の収入になっていた
                    //   （関税を上げるほど輸出国への支払いが増える＝帰着が逆）。関税と穀物法の分は通関で輸入国の国庫へ・影の運送は誰にも払わない
                    //   （捌き手の商人が国内で売る値との差として残る）・運賃は積出し価格に掛かり船の持ち主へ。
                    bool crossA = siA != ni;   // 同国取引は輸入の統計と関税の基礎に数えない（国境の税）
                    double sA = unitA / candF![bestPos];
                    double dutyA = crossA ? duty : 0.0;
                    // 港湾料と保険料の率（海路の成約だけ＝港の取扱高と同じ印・上の段3-4 の注記）
                    bool seaA = !LandNow(snA * tc + t);
                    double portT = seaA && portOwner[t] is not null ? c.PortFeeRate : 0.0, portS = seaA && portOwner[snA] is not null ? c.PortFeeRate : 0.0;
                    double premA = seaA && underwriters[ni] is not null ? c.MarinePremiumRate : 0.0;
                    double costA = sA * (1 + dutyA + frA + portT + portS + premA) / exRate[ni];   // 払い手が1単位に払う額（積出し価格＋関税＋運賃＋港湾料＋保険料・自国通貨＝A7）
                    double pot = Math.Min(Math.Min(X, Y), left[bestA]);
                    pot = Math.Min(pot, budget[t] / Math.Max(1e-12, costA));   // 支払能力（捌き手の貯蓄）
                    double qA = pot;
                    qA = Math.Min(qA, Math.Min(capLeft[t], capLeft[snA]) / Math.Max(1e-9, Wg));
                    // 潜在量（容量で切る**前**＝θ/T4 の分母）＝成約した量＋容量で切られた不足。★不足は組ごとに1回（作者裁定 2026-09-29 の2）：
                    //   旧＝切られた不足を次善の売り手ごとに数え直した（同じ不足が売り手の数だけ重なる＝思想⑨）。
                    //   新＝この売り手で切られた分のうち、この行の不足（X）でまだ数えていない分だけを足す（行の潜在量は X を超えない）。
                    if (pot > 0)
                    {
                        double doneQ = qA > 1e-12 ? qA : 0.0;
                        double newUnmet = Math.Min(pot - doneQ, Math.Max(0, X - doneQ - unmetQ));
                        potNew[t] += (doneQ + newUnmet) * Wg; potNew[snA] += (doneQ + newUnmet) * Wg;
                        unmetQ += newUnmet;
                    }
                    if (!(qA > 1e-12))
                    {
                        if (!(X > 1e-12)) break;          // 買い手が満ちた＝この財は終了
                        if (capLeft[t] <= 1e-12 && !(X - unmetQ > 1e-12)) break;   // 買い手の容量が尽き、不足は全て数えた＝この行は終わり（以後の売り手は数え直しになるだけ）
                        if (!(budget[t] > 1e-12)) break;  // 払い手の支払能力が尽きた（この清算の中で増えない）
                        // 売り手側の事情（Y=0/容量0/在庫0）＝次善へ回る（旧＝一時スキップの印 skipS[bestA]）＝候補表から外す
                        candN--; candS[bestPos] = candS[candN]; candD![bestPos] = candD[candN]; candK![bestPos] = candK[candN]; candF![bestPos] = candF[candN];
                        continue;
                    }
                    // ---- 記帳（払い手が清算所へ払い、売り手は財の清算の終わりに④で受け取る。運賃は船の持ち主へ・関税は国庫へ）----
                    if (MultiNationSim.TimeOn) CntDeals++;   // ★計器
                    dealtRow = true;
                    double vA = qA * sA;              // 積出し価格の額（金建て）
                    double frG = vA * frA;            // 運賃（金建て）
                    if (TradeReachDiag.On) { TradeReachDiag.Deal(siA, ni, vA, bestKmA); TradeReachDiag.DealRef(siA, ni, qA * gd.RefPrice); }
                    left[bestA] -= qA;
                    msA.Stock -= qA; msA.LastExport += qA;
                    m.Stock += qA; m.LastImport += qA;
                    if (c.DiffusionWeight > 0 && crossA) AccumulatePairFlow(siA, ni, vA, nn);   // ★段7：伝播のペア記帳（国際取引のみ・0＝不読＝恒等）
                    if (crossA) AccumulateDiscovery(nations, siA, ni, g, vA);                    // ★段2A：発見の伝わりの記帳（その財を運び手に持つ行＝解禁済みの国からまだの国へ）
                    double payA = (vA + frG) / exRate[ni];     // 清算所へ（輸入代金＋運賃・買い手の通貨）
                    double dutyPay = vA * dutyA / exRate[ni];  // 国庫へ（関税と穀物法・買い手の通貨）
                    double feePay = vA * (portT + portS + premA) / exRate[ni];   // 清算所へ（港湾料と保険料・買い手の通貨）＝受け手へは清算の終わりに
                    payHouse[t] += payA + feePay; payTariff[t] += dutyPay; budget[t] -= payA + dutyPay + feePay;
                    double recvA = vA / exRate[siA];            // 売り手の受取（売り手の通貨）
                    rev[snA] += recvA;
                    if (crossA) { revX[snA] += recvA; expReg[snA] += recvA; }
                    fxIntended += recvA - payA;                 // 為替の換算の差（運賃の受取は清算の終わりに足す）
                    if (portT > 0) portRecv[t] += vA * portT / exRate[ni];
                    if (portS > 0) { double rS = vA * portS / exRate[siA]; portRecv[snA] += rS; fxIntended += rS - vA * portS / exRate[ni]; }   // 積出し港の持ち主は自国の通貨で（換算の差は台帳へ）
                    if (premA > 0) premRecv[ni] += vA * premA / exRate[ni];
                    freightPool += frG;
                    if (buyDear > 0) paySub[t] += vA * buyDear / exRate[ni];   // 売買方針（高く買う）＝積出し価格×楔（旧＝数量×表示用の平均×楔＝R6）
                    // ★2026-09-27（回4-1・項目12①・新1）：輸出の統計も輸入と同じ「国境を越えるか」の1本で数える。旧＝総取引額（買い手側と売り手側の両方に積む）
                    //   から輸入額を引いて輸出額にしていた＝同じ国の州どうしの取引が2回入り、輸出額＝本当の輸出＋国内の州間取引の2倍だった。
                    //   ★段3-1：輸出＝積出し価格（FOB）・輸入＝積出し価格＋運賃（CIF）＝史実の貿易統計の慣行（旧＝どちらも単価＝次善の着値）。
                    if (crossA) { importVal[ni] += vA + frG; impByGood[ni][g] += vA + frG; exportVal[siA] += vA; expByGood[siA][g] += vA; }
                    if (crossA) { partnerBal[ni] += partnerStd[siA] * vA; partnerTot[ni] += vA; partnerBal[siA] += partnerStd[ni] * vA; partnerTot[siA] += vA; }   // ★(210) 相手の本位（買い手と売り手の両方）
                    // ★2026-09-27（回4-1・項目12②）：港の取扱高は海路の成約だけ（この組の着値を決めた経路＝KmOf(snA, t) が書いた向きの印）。
                    //   陸路の直行は港を通らない。同じ国の沿岸の海路（英のニューカッスル→ロンドンの石炭）は積む＝国境でなく経路で分ける。
                    if (seaA) { tr[t].r.LastPortThroughput += vA; tr[snA].r.LastPortThroughput += vA; }
                    _volume[g] += qA;
                    RecordPartner(nations[ni].World.ImportFromByGood, g, tr[snA].r.Name, vA, impTop[ni]);
                    RecordPartner(nations[siA].World.ExportToByGood, g, tr[t].r.Name, vA, expTop[siA]);
                    if (vA > distTop[t]) { distTop[t] = vA; tr[t].r.LastTradeDistanceKm = bestKmA; }
                    if (vA > distTop[snA]) { distTop[snA] = vA; tr[snA].r.LastTradeDistanceKm = bestKmA; }
                    // ★陸路可視化（2026-08-31）：この対の最短が陸直行なら、両端の州へ「陸路の最大相手」を記帳（純テレメトリ・地図が実経路を描く）
                    if (LandNow(snA * tc + t) || LandNow(t * tc + snA))
                    {
                        if (vA > tr[t].r.LastLandPartnerFlow) { tr[t].r.LastLandPartnerFlow = vA; tr[t].r.LastLandPartnerName = tr[snA].r.Name; }
                        if (vA > tr[snA].r.LastLandPartnerFlow) { tr[snA].r.LastLandPartnerFlow = vA; tr[snA].r.LastLandPartnerName = tr[t].r.Name; }
                    }
                    // ---- 容量の簿記 ----
                    capLeft[t] -= qA * Wg; capLeft[snA] -= qA * Wg;
                    if (capLeft[t] <= 1e-12) break;   // この買い手は今清算これ以上運べない
                    // 最良が有資格のまま（旧の走査の資格判定そのもの＝否定形の NaN も同じ側）なら (a)、外れたら (b)
                    if (!(left[bestA] <= 1e-12) && !(capLeft[snA] <= 1e-12)) keepBest = true;
                    else { candN--; candS[bestPos] = candS[candN]; candD![bestPos] = candD[candN]; candK![bestPos] = candK[candN]; candF![bestPos] = candF[candN]; }
                }
                if (!dealtRow) m.ClearFailLp = lp;   // ★走査して成約しなかった＝この自州価格を印に（月の清算で、これを超えて上がるまで眠る＝上の thin の注記）
            }
            costC[CostLedger.ClearingFills] += nFill;   // 計算量の台帳（診断）：この財の着値の充填
            // ---- 財の清算の終わり：売り手の受取を④の持分で分ける（段3-1・C4）----
            //   旧＝成約ごとに受取の半分（係数 ExportRevenueToProducers）を生産者の持ち主の貯蓄へ直接・残りは国の全POP の貯蓄比（Apply）＝賃金分が無く Earned を通らなかった。
            //   新＝州×財ごとに1回（成約あたりは O(1) の足し算だけ）：越境の分の仲介料（MerchantTradeMargin）を州の商人へ・残りを④の持分
            //   （地代→持ち主・国営持分→国庫・賃金→労働）＝国内の売上と同じ分け方で Earned を通す（所得・課税の基数）。生産者のいない在庫は捌き手の商人へ。
            for (int t = 0; t < tc; t++)
            {
                if (!(rev[t] > 0)) continue;
                var rS = tr[t].r; var wS = nations[tr[t].nation].World;
                double comm = 0;
                if (revX[t] > 0 && rS.MerchantPopIndex >= 0 && rS.MerchantPopIndex < rS.Pops.Count)
                    comm = World.Transfer((null, null), (wS, [rS.Pops[rS.MerchantPopIndex]], null), revX[t] * c.MerchantTradeMargin, TreasuryCat.OtherIn, income: true);
                PaySales((null, null), rS, g, rev[t] - comm, wS);
                rev[t] = 0; revX[t] = 0;
            }
        }
        if (swSec is not null) TimeClearTicks += swSec.ElapsedTicks;   // ★計器：最後の財の清算ループ（旧は足していなかった）

        // 容量律速（LastTradeThrottle＝1未満なら鉄道/船/港が交易を絞っていた）と潜在量（LastTradePotential）＝読み手は州の輸送の容量の取り分（Transport.ShareBasis）・
        //   輸送の目標の混雑（Transport.Step）・港の拡張（MultiNationSim.StepPorts）・貿易タブ。★(130)①：四半期の最初の清算（眠る行が無い）だけが書く。
        for (int t = 0; t < tc; t++)
        {
            double cap = tr[t].cap;
            // ★律速中は積載自体が絞られているので、容量で切る**前**の潜在量（pot·W・§2-3）で測る
            //   （絞った後の量で割ると必ず1.0付近に戻ってしまう）。
            double potential = potNew[t];
            if (!thin)
            {
                tr[t].r.LastTradeThrottle = potential > 1e-9 && cap < double.MaxValue ? Math.Min(1.0, cap / potential) : 1.0;
                tr[t].r.LastTradePotential = potential;   // ★T0（輸送doc・2026-08-13）：比だけでなく**絶対量**を残す
            }
            // 取引が1件も立たなかった州＝陸脚だけは確定しているので、そこまでを距離として残す（0km＝未接続と紛れさせない）。
            if (distTop[t] <= 0 && tr[t].linked) tr[t].r.LastTradeDistanceKm = tr[t].landKm * terrCost[t] * landRatio[t];   // ★実着値の物差し（:262/:273）に統一＝地形/鉄道低下を無視した旧定数(12)フォールバックを廃止
        }

        // ---- 清算の終わり（段3-1）：運賃→船の持ち主／補助→払い手／関税→国庫（通関）／輸入代金と運賃→清算所 ----
        //   受け取りを先に・払いを後に（払い手の支払能力は清算の始めの貯蓄で数えた＝この清算の受け取りは使っていない）。
        //   運賃＝船腹の比で各国の船の持ち主（船を買う商人＝Transport.BuyToward）へ頭数で。
        for (int i = 0; i < nn; i++)
        {
            if (!(freightPool > 0) || !(shipShare[i] > 0)) continue;
            double amt = freightPool * shipShare[i] / exRate[i];
            fxIntended += amt;
            World.Transfer((null, null), (nations[i].World, shipOwners[i], null), amt, TreasuryCat.OtherIn, income: true);
        }
        // 港湾料→港の持ち主・保険料→輸入国の引受手（段3-4・C10＝上の成約の時点で捌き手が清算所へ払った分・各自の通貨＝所得を通す）
        for (int t = 0; t < tc; t++)
            if (portRecv[t] > 0 && portOwner[t] is { } po) World.Transfer((null, null), (nations[tr[t].nation].World, [po], null), portRecv[t], TreasuryCat.OtherIn, income: true);
        for (int i = 0; i < nn; i++)
            if (premRecv[i] > 0 && underwriters[i] is { } uw) World.Transfer((null, null), (nations[i].World, uw, null), premRecv[i], TreasuryCat.OtherIn, income: true);
        for (int i = 0; i < nn; i++) nations[i].World.LastTariffRevenuePerClearing = 0;
        for (int t = 0; t < tc; t++)
        {
            var w0 = nations[tr[t].nation].World;
            if (paySub[t] > 0 && payerOf[t] is { } ps) World.Transfer((w0, null), (w0, ps, null), paySub[t] * w0.FiscalFundingRatio.Value, TreasuryCat.Subsidy);   // 売買方針の補助＝国家の支払い率 r の分だけ（区分 Subsidy・作者裁定 Q14）
            if (bill[t] && payerOf[t] is [var mer])
            {   // ③手形の振り出し＝貯蓄（補助の後）で足りない分だけ自国の銀行で割り引く（払いの前＝関税と代金を満額払える）
                double due = payTariff[t] + payHouse[t], have = Math.Max(0, mer.Savings.Value);
                if (due > have) Bank.Lend(w0, mer, due - have);
            }
            if (payTariff[t] > 0 && payerOf[t] is { } pt) w0.LastTariffRevenuePerClearing += World.Transfer((w0, pt), (w0, null, null), payTariff[t], TreasuryCat.Tariff);   // 関税と穀物法＝輸入者から通関の時点で（econ-07）
            if (payHouse[t] > 0 && payerOf[t] is { } pp) World.Transfer((w0, pp), (null, null, null), payHouse[t]);   // 輸入代金と運賃＝清算所へ（国境をまたげば世界の台帳へ・同じ国の州どうしは台帳で打ち消す）
        }
        if (dip is not null) dip.CumulativeFxResidual += fxIntended;   // 為替の換算の台帳（共有層＝移し方の1本が書く国境をまたぐ払いと同じ1本・段3-3）
        for (int i = 0; i < nn; i++)
        {
            var w0 = nations[i].World;
            w0.LastImportValue = importVal[i];
            w0.LastExportValue = exportVal[i];   // ＝ExportsByGood の和（同じ成約を同じ条件で積む）
            w0.TradePartnerStandardBalance = partnerTot[i] > 1e-12 ? partnerBal[i] / partnerTot[i] : 0;   // ★(210) 取引の相手の本位の差（越境の成約が無い清算は0＝相手の過半が無い）
            // ダンピングの輸出補助（負の関税）＝国庫→輸出を捌いた州の商人（越境の輸出の受取の比）・国家の支払い率 r の分だけ・関税の純額から減算。
            //   旧＝全州の商人の頭数で等分・輸出額は単価の和・TariffRevenueRate（削除）を掛けた。
            if (tar[i] < 0 && exportVal[i] > 0)
            {
                var ms = new List<Pop>(); var mw = new List<double>();
                for (int t = 0; t < tc; t++)
                    if (tr[t].nation == i && expReg[t] > 0 && tr[t].r.MerchantPopIndex >= 0 && tr[t].r.MerchantPopIndex < tr[t].r.Pops.Count)
                    { ms.Add(tr[t].r.Pops[tr[t].r.MerchantPopIndex]); mw.Add(expReg[t]); }
                w0.LastTariffRevenuePerClearing -= World.Transfer((w0, null), (w0, ms, mw), exportVal[i] / exRate[i] * Math.Abs(tar[i]) * w0.FiscalFundingRatio.Value, TreasuryCat.Tariff);
            }
        }
        // Stage C：トランジット通行料＝利用国の国庫から港所有国の国庫へ（運河通行料と同じ台帳作法・両建てで保存）。
        if (transitUses is { Count: > 0 } && c.TransitTollShare > 1e-12)
        {
            var owed = new Dictionary<(int U, int O), double>();
            var order = new List<(int U, int O)>();
            var totalByUser = new double[nn];
            foreach (var (u, o, r) in transitUses)
            {
                double amt = c.TransitTollShare * (r.LastPortThroughput - (portStart.TryGetValue(r, out double ps0) ? ps0 : 0.0));   // 当清算ぶんの差分（四半期累計に掛けない）
                if (amt <= 1e-12) continue;
                var k = (u, o);
                if (owed.TryGetValue(k, out double cur)) owed[k] = cur + amt; else { owed[k] = amt; order.Add(k); }
                totalByUser[u] += amt;
            }
            // 利用国の国家の支払い率 r の分だけ払う（2026-09-28 段2-1＝旧は利用国ごとに国庫の残り÷請求の合計＝この場所だけの率）。
            //   払えない分だけ通過を止める（作者裁定 Q6）は未実施（量を減らす側＝清算の経路の変更）。
            // ★段3-3（2026-09-30）：取扱高は金建て（積出し価格）＝利用国の通貨へ換算して払い、港の国は自国の通貨で受け取る（移し方の1本＝為替の換算の台帳へ）。
            //   旧＝金建ての額を両国の通貨で同じ額（名目 1:1）。国庫の出入りと台帳は移し方の1本が書く（区分は払いが その他の支出・受け取りが その他の収入）。
            foreach (var k in order)
            {
                double toll = owed[k] / exRate[k.U] * nations[k.U].World.FiscalFundingRatio.Value;
                if (toll <= 1e-12) continue;
                World.Transfer((nations[k.U].World, null), (nations[k.O].World, null, null), toll, TreasuryCat.OtherOut, toCat: TreasuryCat.OtherIn);
            }
        }
    }
}
