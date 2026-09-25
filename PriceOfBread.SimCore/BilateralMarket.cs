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
    private static double[] CreateFilled(int n, double v) { var a = new double[n]; System.Array.Fill(a, v); return a; }
    // ★清算内部の計器（2026-09-01・POB_TIME=1 のみ・純カウンタ＝挙動非接触。timeprobe が末尾で読む）
    internal static long TimeListsTicks, TimeClearTicks;   // 財ごとの (a)平均+売買リスト構築 (b)買い手清算ループ の累計（Stopwatch の刻み。⚠2026-09-26 是正＝旧は財ごとに ElapsedMilliseconds を足して端数を捨てていた＝1財1ms未満の清算がほぼ0に丸められ、高速化の後は「清算ループ」が実時間の数分の1しか数えなかった）
    internal static long CntGoods, CntBuyers, CntRollIters, CntScanSteps, CntSkips, CntFills, CntDeals;
    // 非常口（POB_PAR と同作法・恒等検証のA/B用）：PairRisk のメモ化／連結リスト圧縮を旧walkへ倒す。プロセス内で不変＝1回だけ読む
    private static readonly bool RiskMemoOff = System.Environment.GetEnvironmentVariable("POB_RISK_MEMO") == "0";
    private static readonly bool SkipListOff = System.Environment.GetEnvironmentVariable("POB_SKIPLIST") == "0";
    private const int RolloverGuardMax = 400;   // ロールオーバー反復の停止保証（X は成約ごとに厳密に減る＝通常は数回で終わる。届いたら諦めるだけ＝破綻しない）
    public double[][]? PairFlowQ;
    public double[][]? LastPairFlow;   // ★段7：直近に消費した四半期のペア行列（純テレメトリ＝pairprobe/診断が読む。StepDiffusion がスワップ退避）
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

    private readonly struct Seller(int node, double price, double stock, double flow)
    {
        public readonly int Node = node;        // tr 内の添字
        public readonly double Price = price;   // 金建ての局所価格（＝この売り手のFOB）
        public readonly double Stock = stock;   // 在庫（これ以上は売れない）
        public readonly double Flow = flow;     // この期に動かせる量（アクセス×速度×規模）
    }

    /// <summary>材ごとの「最大額の相手」を更新する（同額は先着＝走査順が決定論なので再現する）。</summary>
    private static void RecordPartner(string[]? slot, int g, string name, double value, double[] top)
    {
        if (slot is null || !(value > top[g])) return;
        top[g] = value; slot[g] = name;
    }

    private void StepBilateral(IReadOnlyList<Simulation> nations, Coefficients c, bool firstClearingOfQuarter = true)
    {
        int gc = _price.Length, nn = nations.Count;
        if (nn == 0) return;   // 参加国なし＝清算なし（下の nations[0] を守る）
        // ==== S4 裁定清算（TradeArbitrageClearing・2026-08-30・docs/202608 §1-2〜1-4/§2）====
        //   ON＝数量を水準式で解く：買い X=min(価格静止 X_price, 物理 X_need)・売り Y=min(価格静止 Y_price, 在庫−地元1ヶ月分)。
        //   TradeSpeed（定数速度×自州規模）は ON 経路で読まない。0＝旧経路を一字不変に通る＝厳密恒等。
        bool clearOn = c.TradeArbitrageClearing > 0;
        bool capHard = clearOn && c.TradeCapacityHard > 0;   // S4-1：同期容量制約（両端減算・θ乗算撤去）
        // ★月次清算（2026-08-22）：LastPortThroughput は四半期末の読み手（港湾投資・保険・通行料・ArmyShape・UI）が四半期量として読む＝
        //   月次清算では四半期の第1清算だけリセットして以後は累計する。当清算ぶんは start との差分（トランジット通行料は差分に掛ける＝3重課金回避）。
        var portStart = new Dictionary<Region, double>();
        var goodById = new Good?[gc];
        foreach (var g in nations[0].World.Region.Goods) goodById[g.Id] = g;

        var exRate = new double[nn]; var tar = new double[nn]; var corn = new double[nn];
        var oban = new bool[nn]; var oexban = new bool[nn]; var biasCost = new double[nn]; var milExportAcc = new double[nn];
        for (int i = 0; i < nn; i++)
        {
            var w0 = nations[i].World;
            double er = w0.ExchangeRate; exRate[i] = er > 1e-9 ? er : 1.0;
            tar[i] = Math.Clamp(w0.Tariff * TreatyPortTariffScale(w0, c), c.TariffFloor, 1);
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
        var tr = new List<(int nation, Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk)>();
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
        var natAccessN = new double[nn];   // 国別アクセス（末尾の律速率計算が再計算せずに読む）
        var suezOkN = new bool[nn]; var panOkN = new bool[nn];
        for (int i = 0; i < nn; i++)
        {
            var W = nations[i].World;
            suezOkN[i] = suez is { IsOpen: true } && !Blocked(W, suez);
            panOkN[i] = pan is { IsOpen: true } && !Blocked(W, pan);
            natHostility[i] = dip is { } dH && W.SelfPower is { } spH ? dH.BasinHostility(spH) : new double[Basins.Count];
            bool any = false; foreach (var hv in natHostility[i]) if (hv > 1e-9) { any = true; break; }
            natAnyHostile[i] = any;
            double natAccess = natAccessN[i] = W.ExternalTradeAccess();
            int activeCount = 0;
            foreach (var r in W.Regions) if (r.State == RegionState.Active) activeCount++;
            double evenShare = activeCount > 0 ? 1.0 / activeCount : 1.0;
            // ★(4) 潜在量比例の配分（TransportShareByPotential・既定1.0＝潜在量比例。0で均等割り）。前期の LastTradePotential を読む。
            double kPot = Math.Clamp(c.TransportShareByPotential, 0, 1);
            double totalPot = 0;
            if (kPot > 0) foreach (var r in W.Regions) if (r.State == RegionState.Active) totalPot += Math.Max(0, r.LastTradePotential);
            bool potOk = kPot > 0 && totalPot > 1e-9;   // 分母が痩せたら（開幕・実績なし）均等へ倒す＝立ち上がりの跳躍なし
            // ★B-06（2026-09-03）：鉄道以前の基礎容量は**陸**（道路・河川）。0＝旧（海運へ加算）＝厳密恒等。
            double railCapNat = W.RailCapacity + (c.TransportBaselineIsLand > 0 ? c.TransportBaseline : 0);
            double shipCapNat = W.ShipCapacity / (1.0 + W.ColonialShipBurden) + (c.TransportBaselineIsLand > 0 ? 0 : c.TransportBaseline);
            foreach (var r in W.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double share = potOk ? (1.0 - kPot) * evenShare + kPot * Math.Max(0, r.LastTradePotential) / totalPot : evenShare;
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
                    inlandRail = railToPort / (1.0 + c.InlandPortDistanceDecay * r.NearestPortDistanceKm / DistanceScaleKm);
                double cap = c.TransportEnabled ? (r.IsPort ? shipEff : Math.Min(inlandRail, shipEff)) + portBonus : double.MaxValue;
                r.LastTransportCap = cap;   // ★純テレメトリ（式の二重定義をしない）
                // ★輸送容量を実際に効かせる（2026-08-11の取りこぼし修正）。旧経路は事前パスで潜在輸送量を積んで
                //   `acc *= LastTradeThrottle` していた＝**容量が交易量を絞っていた**。二国間では相手ごとに量が決まるため
                //   事前パスで潜在量を出せないので、**前月の積載から決まった律速率**を今月のアクセスに掛ける（月次のフィードバック）。
                //   容量を増やせば翌月から緩む＝鉄道・港湾への投資が効く筋は同じ。初月は1.0（積載実績なし）。
                // ★実バグ修正（2026-08-21・同上）：旧＝`<= 0 ? 1.0` の番兵が**容量ちょうど0の州をフルアクセスへ化かしていた**（fail-open）。
                //   「実績なし」は初期値1.0（Region.LastTradeThrottle）が既に担っており、書き手（本メソッド末尾の律速率計算）は potential>0 のとき正しく 0 を書ける＝0 は本物の律速。
                //   NaN・負は否定形で 1.0 へ（未計算の番兵）。0 はそのまま通す＝容量ゼロは運べない。
                double thr0 = r.LastTradeThrottle;
                double throttle = !c.TransportEnabled ? 1.0 : !(thr0 >= 0) ? 1.0 : thr0 > 1.0 ? 1.0 : thr0;
                // ★(1.5)：陸直行のために**州自身の座標**も持つ（上の lat/lon は最寄り港へ置き換えられている）。
                //   ★港に繋がらない内陸州も陸路なら交易できる＝旧は linked=false で**交易から完全に除外**されていた
                //     （中央アジア・アフリカ内陸・独墺の内陸州）。seaOk＝海に出られるか（港経由の可否）を別に持つ。
                bool seaOk = linked;
                if (c.LandRouteEnabled > 0 && (cc.Lat != 0 || cc.Lon != 0)) linked = true;
                // ★S4-1（capHard）：容量は同期制約（capLeft）で効くので、前月律速率の乗算（遅延乗数）を外す＝同じ容量が2回効く二重拘束の回避
                tr.Add((i, r, lat, lon, landKm, natAccess * (capHard ? 1.0 : throttle), cap, linked, cc.Lat, cc.Lon, seaOk));
            }
        }
        int tc = tr.Count;
        // ★(3) 地形の陸路コスト係数（設計doc 20260826_地形と輸送コスト.DONE.md）＝州ごとに1回だけ算出。
        //   実効係数＝馬と鉄道の**railGate ブレンド**（既存の技術連続ゲート＝新しい時計を作らない）。
        //   水路タグは >0 のとき陸脚コストをその値へ**置換**（川が道の代わり＝内陸なのに港に近い扱い）。
        //   TerrainCostEnabled=0 または表が全1.0・水運0なら terrCost は恒等的に1.0（既定は点火＝表が効く）。
        var terrCost = new double[tc];
        var landRatio = new double[tc];   // ★州ごとの実効 陸/海コスト比（鉄道時代の低下・RailCostFraction=1.0で12のまま＝恒等）
        for (int t0 = 0; t0 < tc; t0++)
        {
            var W0 = nations[tr[t0].nation].World;
            double rg0 = Math.Clamp((W0.TechLevel - c.RailTechOnset) / Math.Max(1e-9, c.RailTechWidth), 0, 1);
            landRatio[t0] = c.LandSeaCostRatio * (1.0 - rg0 * (1.0 - Math.Clamp(c.RailCostFraction, 0, 1)));
            if (!(c.TerrainCostEnabled > 0)) { terrCost[t0] = 1.0; continue; }   // ★栓（0＝表を読まない＝厳密恒等）
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
        var kmTab = new double[tc * tc];
        System.Array.Fill(kmTab, -1.0);
        var kmLand = new bool[tc * tc];   // ★陸路可視化（2026-08-31・純テレメトリ）：この対の最短が陸直行だったか（PairKm と同じ比較＝算術に触れない）
        double KmOf(int a, int b)
        {
            double v = kmTab[a * tc + b];
            if (v >= 0) return v;
            v = PairKm(a, b);
            kmTab[a * tc + b] = v;   // ⚠この向きのみ（鏡像は書かない＝ビット恒等の維持）
            kmLand[a * tc + b] = LastPairWasLand;
            return v;
        }
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
        double[]? riskTab = null;
        double PairRisk(int seller, int buyer)
        {
            int bi = tr[buyer].nation;
            if (!natAnyHostile[bi]) return 0;
            if (RiskMemoOff)
            {
                var kmB0 = SeaGraph.RouteBasinKm(SeaGraph.NearestSeaNode(tr[seller].lat, tr[seller].lon), SeaGraph.NearestSeaNode(tr[buyer].lat, tr[buyer].lon), suezOkN[bi], panOkN[bi]);
                double integ0 = 0;
                for (int b = 0; b < Basins.Count; b++) integ0 += natHostility[bi][b] * kmB0[b];
                return 1.0 - Math.Exp(-c.RiskLambda * integ0 / DistanceScaleKm);
            }
            riskTab ??= CreateFilled(tc * tc, -1.0);
            double cached = riskTab[seller * tc + buyer];
            if (cached >= 0) return cached;
            var kmB = SeaGraph.RouteBasinKm(SeaGraph.NearestSeaNode(tr[seller].lat, tr[seller].lon), SeaGraph.NearestSeaNode(tr[buyer].lat, tr[buyer].lon), suezOkN[bi], panOkN[bi]);
            double integral = 0;
            for (int b = 0; b < Basins.Count; b++) integral += natHostility[bi][b] * kmB[b];
            double risk = 1.0 - Math.Exp(-c.RiskLambda * integral / DistanceScaleKm);
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

        var netFlow = new double[nn]; var direct = new double[nn];
        var importVal = new double[nn]; var grossVal = new double[nn];
        double freightPool = 0;
        var shipShare = new double[nn];
        if (c.FreightRatePer10Mm > 1e-12)
        {
            double shipSum = 0;
            for (int i2 = 0; i2 < nn; i2++) { shipShare[i2] = Math.Max(0, nations[i2].World.ShipCapacity / (1.0 + Math.Max(0, nations[i2].World.ColonialShipBurden))); shipSum += shipShare[i2]; }
            if (shipSum > 1e-9) for (int i2 = 0; i2 < nn; i2++) shipShare[i2] /= shipSum;
            else for (int i2 = 0; i2 < nn; i2++) shipShare[i2] = 1.0 / nn;   // ★船腹が全国0（開幕Q1・合成世界）＝運賃プールが誰にも渡らず消える片側記帳を回避＝均等配分へ倒す
        }

        // 従量成分（重量/価値比×運賃革命）＝旧 Step のローカル関数と同式（A-高2）。前線技術は世界財＝旗に依らない。
        double frontierTech = 0;
        if (c.TransportRevolutionMax > 0)
            for (int i = 0; i < nn; i++) if (nations[i].World.TechLevel > frontierTech) frontierTech = nations[i].World.TechLevel;
        double freightRevolution = c.TransportRevolutionMax > 0
            ? 1.0 - c.TransportRevolutionMax * Math.Clamp((frontierTech - c.TransportRevolutionOnset) / c.TransportRevolutionSpan, 0.0, 1.0)
            : 1.0;
        // ★貿易復活②（2026-08-15・tradewhy分解＝99_実装ログ§4-15）：運賃革命を**ad valoremマージンにも**適用。
        //   旧＝従量成分（WeightMarginAdd）だけが半減し、TransportMargin×(1+距離)の従価部分は1783年から不変
        //   ＝蒸気船・スエズ・鉄道が「重い財」しか安くしない歪み（史実1870-1913の運賃崩落は全面的）。
        //   FreightRevolutionAdValorem=0＝×1.0（IEEEビット恒等）。点火はA/B（貿易/GDP曲線＋帯）。
        double adValoremRev = c.FreightRevolutionAdValorem > 0 ? freightRevolution : 1.0;
        double WeightMarginAdd(int g)
        {
            if (c.TransportWeightMarginScale <= 0) return 0.0;
            double rp = goodById[g] is { } gd0 ? gd0.RefPrice : 0.0;
            return rp > 1e-9 ? c.TransportWeightMarginScale * Catalog.TransportWeight(g) / rp * freightRevolution : 0.0;
        }

        // ★貿易復活④（2026-08-17・裁定流量の価格差感応化＝docs/202608/20260817_裁定流量の価格差感応化_完了.md）
        //   旧＝売り手が動かせる量は **アクセス×TradeSpeed×規模** だけで**価格差を1文字も見ない**（＝定数蓋）。
        //   帰結＝国内が値崩れしても世界へ出る速さが変わらない＝**ダンピングで世界価格を下げられない**
        //   （鋼市場診断β／8/15の「残る律速は速度」＝別の入口から同じ犯人）。
        //   新＝裁定margin x＝avg/lp−1（＝「世界平均で売れば何割乗るか」・**既存の量だけ**で作る＝新しい観測を増やさない）に
        //   応じて流量を x/(1+x/arbMax) だけ上乗せする＝**自己有界**（x→∞ で 1+arbMax 倍へ漸近・壁でなく逓減）。
        //   有界にする理由＝上限のない乗数だと lp→0 の産地から在庫が尽きるまで無限に流れ、値崩れが「無限の輸出能力」に化ける。
        //   x/(1+x/K) を選んだ理由＝(a) x が小さい領域では f≈x＝**線形**（較正済みの平時の挙動を歪めない）
        //   (b) 上限が K そのもの＝係数が「最大何倍まで速くなるか」として読める（掃引の物差しになる）
        //   (c) clamp（壁）と違い微分が連続＝閾値の前後で挙動が飛ばない。
        //   ⚠買い手側の q は **既に**価格差に比例している（(lp−bestDeliver)/avg）＝感応が無いのは売り手だけ。
        //   実測（instrumented tradeprobe・Q200）＝売り手候補の**97%→85%が流量律速**（在庫でなく蓋で止まっている）・
        //   成約の**25%**／希望額の**37%**が left[best] で切られる＝**裁定チャネル（最安の産地）の律速は売り手側**。
        //   一方で提示量の**1.5%**しか動いていない＝総量の水準を決めているのは買い手側の q。よって蓋を上げるのは売り手側だけでよい
        //   （買い手にも同じ x を掛けると価格差が**二乗**で効く＝二重計上）。
        //   0＝この分岐を1命令も通らない＝ビット恒等（(1+0×…)のような乗算を残さない）。NaN は false 側＝休眠へ倒れる。
        double arbMax = c.TradeArbitrageMax;
        bool arbOn = arbMax > 0;

        // ---- S4 のプール（clearOn時のみ確保）----
        double twoEps = 2.0 * c.PriceElasticity;   // X_price の指数（ε=0.5 なら 1＝D·rp/d−P−I）
        double[]? budgetLeft = null; double[]? capLeft = null; double[]? potNew = null;
        int[]? nextS = null;   // ★連結リスト圧縮（2026-09-01）：在庫が尽きた売り手（と 2026-09-26 から容量が尽きた売り手）を走査から恒久除去する「次の生存索引」（ON経路のみ）
        // ★逐次の候補表（2026-09-26・高速化・厳密恒等）：買い手1行のあいだ、訪問済みで有資格の売り手（着値＜MaxValue）を持つ＝伸長のみ・買い手ごとに0から。
        //   旧＝反復のたびに先頭から走査し直していた（成約の直後の同一の再走査・一時スキップの踏み直し）。下の買い手ループの注記を参照。
        int[]? candS = null; double[]? candD = null; double[]? candK = null;
        if (clearOn)
        {
            budgetLeft = new double[nn];   // 支払能力＝Apply と同一集合 Σmax(0,Savings)（裁定11・国庫は含めない）
            for (int i2 = 0; i2 < nn; i2++)
            {
                double bsum = 0;
                foreach (var r2 in nations[i2].World.Regions) foreach (var p2 in r2.Pops) bsum += Math.Max(0, p2.Savings);
                budgetLeft[i2] = bsum;
            }
            potNew = new double[tc];   // 新定義の潜在量（容量で切る前の pot·W を両端に計上）＝θ/T4 の分母
            if (capHard) { capLeft = new double[tc]; for (int t2 = 0; t2 < tc; t2++) capLeft[t2] = tr[t2].cap; }
        }

        var sellers = new List<Seller>(tc);
        var buyers = new List<(int Node, double Price)>(tc);
        var distTop = new double[tc];   // 距離の診断＝どの取引を代表に採ったか（最大額）
        var volLoad = new double[tc];   // 各州がこの期に動かした重量（容量律速の診断＝LastTradeThrottle）
        // ★財15の生産者の列の記憶（2026-09-26・高速化・厳密恒等）：州(tr添字)ごと・財ごとに PayExportToProducers が初回に作る（財が変わるたびに空へ戻す）。
        var payPr = new Producer[]?[tc]; var payTot = new double[tc];

        var gOrder = new int[gc];
        for (int gi0 = 0; gi0 < gc; gi0++) gOrder[gi0] = gi0;
        if (clearOn)   // ON＝価値密度（RefPrice/重量）降順＝容量・予算の先取りが価値順（新定数なし・同値はid昇順＝決定論）。OFF＝id順（恒等）
        {
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
            Array.Clear(payPr);   // 生産者の列は財ごと（前の財の列を読まない）
            // ★TradeColonialGoods（点火2026-08-26＝既定1.0）：植民地財も二国間清算に載る。0なら旧判定（植民地財除外・阿片は別ゲート）と厳密同値。
            if (gd is null || (gd.IsColonial && !(c.TradeColonialGoods > 0) && !(g == c.OpiumGoodId && c.OpiumAddictionGrowth > 0)) || gd.NonTradeable) continue;

            // ---- 平均価格（世界価格の後継）＝参加州の金建て局所価格の単純平均。市場ではなく**統計** ----
            //   用途は①表示②数量の正規化（旧が世界価格で割っていた分母）だけ。清算には使わない。
            double sum = 0; int cnt = 0;
            for (int t = 0; t < tc; t++)
            {
                if (!tr[t].linked) continue;
                double lp0 = tr[t].r.Markets[g].Price * exRate[tr[t].nation];
                if (lp0 > 1e-9)
                {
                    sum += lp0; cnt++;
                    if (TradeReachDiag.On)   // 純テレメトリ：取引可能な市場の総額（売り手側も買い手側も込み）＝GDPと突き合わせる分母
                    {
                        var m0 = tr[t].r.Markets[g];
                        double st0 = Math.Max(Math.Max(m0.LastDemand, m0.LastProduced), m0.TargetStock);
                        TradeReachDiag.RowsWorld++; TradeReachDiag.MarketValWorld += st0 * lp0;
                        TradeReachDiag.MarketRefWorld += st0 * gd.RefPrice;   // 同じ数量を参照価格で評価＝GDP（Σ産出×参照価格）と同じ物差し
                        // 器の内訳＝max のどれが効いているか（「器が縮んだ」主張の反証用・純加算）
                        TradeReachDiag.RefDemandW += m0.LastDemand * gd.RefPrice; TradeReachDiag.RefProducedW += m0.LastProduced * gd.RefPrice;
                        TradeReachDiag.RefTargetW += m0.TargetStock * gd.RefPrice; TradeReachDiag.RefScaleW += st0 * gd.RefPrice;
                        if (st0 <= m0.LastDemand) TradeReachDiag.WinD++; else if (st0 <= m0.LastProduced) TradeReachDiag.WinP++; else TradeReachDiag.WinT++;
                    }
                }
            }
            if (cnt == 0) continue;
            double avg = sum / cnt;
            if (avg <= 1e-9) continue;
            _price[g] = c.WorldPriceStatUnclamped > 0 ? avg : Math.Clamp(avg, gd.RefPrice * PriceBandFloorMul, gd.RefPrice * PriceBandCeilMul);   // 表示用の「世界価格」＝平均価格（★器 WorldPriceStatUnclamped=1 でクランプなし＝偏在財で計器が振り切れない）

            double weightAdd = WeightMarginAdd(g);
            double Wg = Catalog.TransportWeight(g);   // 財の輸送重量（積載・容量の単位）
            bool milGood = Array.IndexOf(c.MilitaryExportGoods, g) >= 0;

            // ---- 売り手候補＝平均より安い州。価格の昇順（枝刈りのため） ----
            // ★貿易復活③（2026-08-15・tradewhy2＝量の分解）：平均分割の帯を広げる。旧＝売り手lp<平均・買い手lp>平均は
            //   「世界の裾と裾」しか取引できない＝穀物大国（露≈平均価格）がロンドン（平均の1.5倍）に売れない構造遮断。
            //   売り手帯＝平均×(1+w)・買い手帯＝平均/(1+w)＝**ペア単位で黒字な取引を到達可能に**（成立判定は従来どおり着値vs自州価格）。
            //   TradeBandWiden=0＝×1.0/÷1.0＝ビット恒等。点火はA/B（貿易/GDP＋帯）。
            double sellCap = avg * (1.0 + c.TradeBandWiden);
            double buyFloor = avg / (1.0 + c.TradeBandWiden);
            sellers.Clear();
            for (int t = 0; t < tc; t++)
            {
                if (!tr[t].linked) continue;
                var m = tr[t].r.Markets[g];
                double lp = m.Price * exRate[tr[t].nation];
                if (lp <= 1e-9 || lp >= sellCap || m.Stock <= 1e-9) continue;
                int ni = tr[t].nation;
                double sellAcc = tr[t].access * (milGood ? milExportAcc[ni] : 1.0);
                if (c.IsolationExportWeight > 0) sellAcc *= System.Math.Max(0, 1.0 - c.IsolationExportWeight * System.Math.Max(0, tar[ni]) * System.Math.Max(0, tar[ni]));   // ★鎖国の対称化（高関税レジーム＝輸出側も閉じる・0＝恒等）
                if (g == c.OpiumGoodId && oexban[ni]) sellAcc *= Contraband.LeakFraction(tr[t].r.AdminFill, c);   // 輸出禁止＝取り締まれた分だけ止まる
                if (sellAcc <= 1e-12) continue;
                if (clearOn)
                {
                    // ON：出せる量＝在庫−地元1ヶ月分（裁定3＝剥ぎ取り不可）。速度蓋（TradeSpeed）は読まない＝量は買い手の X と売り手の Y_price が決める
                    double availS = m.Stock - m.TargetStock;
                    if (!(availS > 1e-12)) continue;
                    sellers.Add(new Seller(t, lp, availS, availS));   // sellAcc は成約時に再計算（sellers は後で価格順ソート＝並走リスト不可）
                    continue;
                }
                double scaleS = Math.Max(Math.Max(m.LastDemand, m.LastProduced), m.TargetStock);
                if (c.TradeScaleUsesDesired > 0) scaleS = Math.Max(Math.Max(m.LastDesiredDemand, m.LastProduced), m.TargetStock);   // 反実仮想（測定専用・既定0で通らない）
                double flowS = sellAcc * c.TradeSpeed * scaleS;
                if (c.TradeSellerFlowMult != 1.0) flowS *= c.TradeSellerFlowMult;   // 診断レバー（既定1.0＝この分岐を通らない＝ビット恒等）
                if (arbOn)
                {
                    double x = avg / lp - 1.0;   // 裁定margin。lp>1e-9・avg>1e-9 は上で確定済み。売り手帯の上端（lp≧avg）では x≦0＝上乗せなし
                    double boost = x > 0 ? x / (1.0 + x / arbMax) : 0.0;   // 自己有界＝x→∞ で arbMax へ漸近
                    if (boost > 0) flowS += flowS * boost;   // 否定形と同値の肯定形＝NaN/非有限は上乗せ側へ入らない（休眠へ倒れる）
                }
                sellers.Add(new Seller(t, lp, m.Stock, flowS));   // 在庫の蓋（left=min(Stock,Flow)）は不変＝無から財は湧かない
            }
            if (sellers.Count == 0) continue;
            if (TradeReachDiag.On)   // 純テレメトリ：売り手が「この期に出せる」総額＝在庫と流量の小さい方×自分の相場
                for (int s = 0; s < sellers.Count; s++)
                {
                    double avail = Math.Min(sellers[s].Stock, sellers[s].Flow);
                    TradeReachDiag.SellRows++; TradeReachDiag.SellOfferVal += avail * sellers[s].Price;
                    if (sellers[s].Flow < sellers[s].Stock) TradeReachDiag.SellFlowBound++;
                }
            sellers.Sort((a, b) => a.Price != b.Price ? a.Price.CompareTo(b.Price) : string.CompareOrdinal(tr[a.Node].r.Name, tr[b.Node].r.Name));   // ★買い手ソート(:515)と統一＝カルチャ非依存の決定論的タイブレーク（wave2b是正）

            var left = new double[sellers.Count];   // 売り手ごとの残り（在庫と流量の小さい方）
            for (int s = 0; s < sellers.Count; s++) left[s] = Math.Min(sellers[s].Stock, sellers[s].Flow);
            int headS = 0;   // 先頭の生存売り手（価格昇順の死んだ接頭辞を跨ぐ）
            if (clearOn)     // ★連結リスト圧縮：財ごとに全員生存で初期化（読み手は ON 経路だけ）
            {
                if (nextS is null || nextS.Length < sellers.Count) nextS = new int[Math.Max(64, sellers.Count * 2)];
                for (int s = 0; s < sellers.Count; s++) nextS[s] = s + 1;
            }

            // ---- 買い手＝平均より高い州。**高く買う相手から順に**応じる（作者指示2026-08-11）。
            //   売り手の在庫は有限なので、処理順がそのまま「誰に売るか」になる＝局所価格の高い買い手を先に通す。
            //   同値は州名で全順序＝決定論（走査順に依存しない）。
            buyers.Clear();
            for (int t = 0; t < tc; t++)
            {
                if (!tr[t].linked) continue;
                double lpB = tr[t].r.Markets[g].Price * exRate[tr[t].nation];
                if (lpB > buyFloor) buyers.Add((t, lpB));   // ★③帯の拡張（TradeBandWiden=0＝旧: >平均と厳密同値）
            }
            if (buyers.Count == 0) continue;
            buyers.Sort((x, y) => x.Price != y.Price ? y.Price.CompareTo(x.Price) : string.CompareOrdinal(tr[x.Node].r.Name, tr[y.Node].r.Name));
            if (swSec is not null) { TimeListsTicks += swSec.ElapsedTicks; swSec.Restart(); CntGoods++; CntBuyers += buyers.Count; }   // ★計器
            foreach (var (t, lp) in buyers)
            {
                var m = tr[t].r.Markets[g];
                int ni = tr[t].nation;
                if (TradeReachDiag.On)   // 純テレメトリ：買い手候補行（＝この財を買いうる市場）の規模と額
                {
                    double stA = Math.Max(Math.Max(m.LastDemand, m.LastProduced), m.TargetStock);
                    TradeReachDiag.RowsAll++; TradeReachDiag.ScaleTAll += stA; TradeReachDiag.MarketValAll += stA * lp;
                }
                int bias = nations[ni].World.TradeBiasOf(g);
                double buyDear = bias > 0 ? c.TradeBiasStrength : 0;
                double protect = Math.Max(0, tar[ni]);
                double cornPen = gd.Tier == GoodTier.Staple || (c.CornLawGrainChainGate > 0 && gd.GrainChain) ? corn[ni] * c.CornLawImportPenalty : 0;
                double wedge = protect * c.TariffProtect + cornPen - buyDear;   // 買い手側の敷居（関税・穀物法・売買方針）
                // ★貿易復活①（2026-08-15）：関税・穀物法は**国境の税**＝同国の売り手には掛けない（内国関税は1789仏/1834独関税同盟で消えた制度。
                //   旧＝自国州からの購入にも関税wedgeが掛かり、国内州間交易まで殺していた＝tradewhy分解の壁の片翼）。
                //   TariffBorderOnly=0＝旧（全ペアにwedge＝枝刈り含めビット恒等）。買い手方針buyDearは境を問わず据え置き。
                bool borderOnly = c.TariffBorderOnly > 0;
                double wedgeHome = borderOnly ? -buyDear : wedge;   // 同国＝関税・穀物法なし（方針だけ残る）

                if (clearOn)
                {
                    // ==== S4 ON：水準式の裁定清算（同じ買い手が X が尽きるか売り手/容量/予算が尽きるまで次善へ回る）====
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
                    //     各売り手を1行につき1回しか訪問しないので不要（距離・危険料の表 kmTab/riskTab は清算全体の純メモのまま＝埋まる対の集合も旧と同一）。
                    //   ⚠候補表に入れるのは着値 < MaxValue の売り手だけ＝旧の更新規則（初期値 MaxValue との厳密比較）で best/second に一度も入らない売り手（∞・NaN）は旧でも読まれない。
                    if (candS is null || candS.Length < sellers.Count) { int cn0 = Math.Max(64, sellers.Count * 2); candS = new int[cn0]; candD = new double[cn0]; candK = new double[cn0]; }
                    double lbMul = 1 + c.TransportMargin * adValoremRev + (borderOnly ? Math.Min(wedge, wedgeHome) : wedge);   // 昇順の打ち切りの下界の倍率（真の下界＝運賃革命の従価割引込み・旧の式そのもの）
                    int candN = 0, resS = headS, resPrev = -1;   // 候補の数／次に調べる売り手（打ち切り位置）とその手前の生存売り手
                    int bestA = -1, bestPos = -1; double bestDelA = double.MaxValue, secondDelA = double.MaxValue, bestKmA = 0;
                    bool keepBest = false;   // (a)：前回の best/second をそのまま使う
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
                                    //   純no-opだけ＝訪問集合・順序・打ち切りは不変（ビット恒等）。POB_SKIPLIST=0 で旧walk（unlockなし）。
                                    if (MultiNationSim.TimeOn) CntSkips++;   // ★計器
                                    int nxS = nextS![s2];
                                    if (!SkipListOff) { if (prevS < 0) headS = nxS; else nextS[prevS] = nxS; }
                                    else prevS = s2;
                                    s2 = nxS; continue;
                                }
                                if (capHard && capLeft![sellers[s2].Node] <= 1e-12)
                                {   // ★容量切れの売り手も同じく外す（2026-09-26）：capLeft は清算の中で減る一方（成約ごとに qA×Wg＞0 を引く）＝一度尽きたら
                                    //   以後この財の全ての買い手で必ず continue する純no-op（旧＝財をまたいで全ての買い手・全ての反復で踏み直していた）。
                                    if (MultiNationSim.TimeOn) CntSkips++;   // ★計器
                                    int nxS = nextS![s2];
                                    if (!SkipListOff) { if (prevS < 0) headS = nxS; else nextS[prevS] = nxS; }
                                    else prevS = s2;
                                    s2 = nxS; continue;
                                }
                                if (sellers[s2].Price * lbMul >= bestDelA) break;   // 昇順の打ち切り
                                if (MultiNationSim.TimeOn) { CntScanSteps++; CntFills++; }   // ★計器（各売り手は1行につき1回だけ訪問＝訪問＝着値の計算）
                                double km2 = KmOf(sellers[s2].Node, t);
                                double risk2 = PairRisk(sellers[s2].Node, t);
                                double bb2 = gd.Tier == GoodTier.Staple ? c.BlockadeStapleBias * risk2 : 0.0;
                                double mar2 = c.TransportMargin * (1.0 + c.DistanceMarginScale * km2 / DistanceScaleKm) * adValoremRev
                                            + weightAdd * (km2 / DistanceScaleKm) + risk2;
                                double wS2 = borderOnly && tr[sellers[s2].Node].nation == ni ? wedgeHome : wedge;
                                double del2 = sellers[s2].Price * (1 + mar2 + wS2 + bb2);
                                if (del2 < double.MaxValue)
                                {
                                    candS[candN] = s2; candD![candN] = del2; candK![candN] = km2;
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
                        // ★価格振動の根②（2026-09-07・器 TradeBuyerSolvesAtUnitPrice）：買い手の静止点を「実際に払う単価 unitA」で解く（売り手側 yPrice と同じ物差し）。
                        //   0＝旧式＝最良の着値 bestDelA（単一供給では unitA=lp より安い価格で解く＝過剰発注→閾で0の bang-bang）。
                        double solveDelA = c.TradeBuyerSolvesAtUnitPrice > 0 ? unitA : bestDelA;
                        double xPrice = rpT > 1e-9 && solveDelA > 1e-9
                            ? m.LastDemand * Math.Pow(rpT / solveDelA, twoEps) - (m.LastProduced + m.LastImport) : 0.0;
                        double xNeed = m.TargetStock + m.LastDemand - m.LastProduced - m.Stock;
                        double X = tr[t].access * Math.Max(0, Math.Min(xPrice, xNeed));
                        if (g == c.OpiumGoodId && oban[ni]) X *= Contraband.LeakFraction(tr[t].r.AdminFill, c);
                        // --- 売り手 Y（§1-3）：u が高いほど地元を絞って出す（固定点＝現在の輸出率）---
                        double sellAccB = tr[snA].access * (milGood ? milExportAcc[siA] : 1.0);
                        if (c.IsolationExportWeight > 0) sellAccB *= System.Math.Max(0, 1.0 - c.IsolationExportWeight * System.Math.Max(0, tar[siA]) * System.Math.Max(0, tar[siA]));   // ★鎖国の対称化（ON経路＝旧経路と同じ1行の鏡映）
                        if (g == c.OpiumGoodId && oexban[siA]) sellAccB *= Contraband.LeakFraction(tr[snA].r.AdminFill, c);
                        double rpS = gd.RefPrice * tr[snA].r.PriceLevel * exRate[siA];
                        double yPrice = rpS > 1e-9 ? (msA.LastProduced + msA.LastImport) * unitA / rpS - msA.LastDemand - msA.LastExport : 0.0;
                        double Y = sellAccB * Math.Max(0, Math.Min(yPrice, left[bestA]));   // 物理上限＝avail（=Stock−TargetStock・left が消化を追う）
                        double frA = c.FreightRatePer10Mm > 1e-12 ? c.FreightRatePer10Mm * bestKmA / DistanceScaleKm : 0.0;
                        double pot = Math.Min(Math.Min(X, Y), left[bestA]);
                        pot = Math.Min(pot, budgetLeft![ni] / Math.Max(1e-12, unitA * (1 + frA)));   // 支払能力（Apply集合）
                        if (pot > 0) { potNew![t] += pot * Wg; potNew![snA] += pot * Wg; }   // 容量で切る**前**＝θ/T4 の分母
                        double qA = pot;
                        if (capHard) qA = Math.Min(qA, Math.Min(capLeft![t], capLeft![snA]) / Math.Max(1e-9, Wg));
                        if (!(qA > 1e-12))
                        {
                            if (!(X > 1e-12)) break;          // 買い手が満ちた＝この財は終了
                            // 売り手側の事情（Y=0/容量0/在庫0）＝次善へ回る（旧＝一時スキップの印 skipS[bestA]）＝候補表から外す
                            candN--; candS[bestPos] = candS[candN]; candD![bestPos] = candD[candN]; candK![bestPos] = candK[candN];
                            continue;
                        }
                        // ---- 記帳（OFF側の記帳ブロックの写し＝会計は同一。⚠OFF側を直したらここも直す）----
                        if (MultiNationSim.TimeOn) CntDeals++;   // ★計器
                        double valueA = qA * unitA;
                        if (TradeReachDiag.On) { TradeReachDiag.Deal(siA, ni, valueA, bestKmA); TradeReachDiag.DealRef(siA, ni, qA * gd.RefPrice); }
                        left[bestA] -= qA;
                        msA.Stock -= qA; msA.LastExport += qA;
                        m.Stock += qA; m.LastImport += qA;
                        volLoad[t] += qA * Wg; volLoad[snA] += qA * Wg;
                        bool crossA = !(c.TariffBorderOnly > 0) || siA != ni;
                        if (c.DiffusionWeight > 0 && siA != ni) AccumulatePairFlow(siA, ni, valueA, netFlow.Length);   // ★段7：伝播のペア記帳（国際取引のみ・0＝不読＝恒等）
                        netFlow[ni] -= valueA; if (crossA) importVal[ni] += valueA; grossVal[ni] += valueA;
                        tr[t].r.LastPortThroughput += valueA; if (crossA) impByGood[ni][g] += valueA;
                        grossVal[siA] += valueA; tr[snA].r.LastPortThroughput += valueA; if (crossA) expByGood[siA][g] += valueA;
                        double toProdA = c.ExportRevenueToProducers > 1e-12 ? PayExportToProducers(tr[snA].r, g, valueA * Math.Clamp(c.ExportRevenueToProducers, 0, 1), ref payPr[snA], ref payTot[snA]) : 0.0;
                        netFlow[siA] += valueA - toProdA; direct[siA] += toProdA;
                        if (c.FreightRatePer10Mm > 1e-12)
                        {
                            double freightA = c.FreightRatePer10Mm * valueA * (bestKmA / DistanceScaleKm);
                            netFlow[ni] -= freightA; freightPool += freightA;
                        }
                        if (buyDear > 0) biasCost[ni] += qA * avg * buyDear;
                        _volume[g] += qA;
                        RecordPartner(nations[ni].World.ImportFromByGood, g, tr[snA].r.Name, valueA, impTop[ni]);
                        RecordPartner(nations[siA].World.ExportToByGood, g, tr[t].r.Name, valueA, expTop[siA]);
                        if (valueA > distTop[t]) { distTop[t] = valueA; tr[t].r.LastTradeDistanceKm = bestKmA; }
                        if (valueA > distTop[snA]) { distTop[snA] = valueA; tr[snA].r.LastTradeDistanceKm = bestKmA; }
                        // ★陸路可視化（2026-08-31）：この対の最短が陸直行なら、両端の州へ「陸路の最大相手」を記帳（純テレメトリ・地図が実経路を描く）
                        if (kmLand[snA * tc + t] || kmLand[t * tc + snA])
                        {
                            if (valueA > tr[t].r.LastLandPartnerFlow) { tr[t].r.LastLandPartnerFlow = valueA; tr[t].r.LastLandPartnerName = tr[snA].r.Name; }
                            if (valueA > tr[snA].r.LastLandPartnerFlow) { tr[snA].r.LastLandPartnerFlow = valueA; tr[snA].r.LastLandPartnerName = tr[t].r.Name; }
                        }
                        // ---- ONの追加簿記：予算と容量 ----
                        budgetLeft[ni] -= qA * unitA * (1 + frA); budgetLeft[siA] += qA * unitA;
                        if (capHard)
                        {
                            capLeft![t] -= qA * Wg; capLeft![snA] -= qA * Wg;
                            if (capLeft![t] <= 1e-12) break;   // この買い手は今清算これ以上運べない
                        }
                        // 最良が有資格のまま（旧の走査の資格判定そのもの＝否定形の NaN も同じ側）なら (a)、外れたら (b)
                        if (!(left[bestA] <= 1e-12) && !(capHard && capLeft![snA] <= 1e-12)) keepBest = true;
                        else { candN--; candS[bestPos] = candS[candN]; candD![bestPos] = candD[candN]; candK![bestPos] = candK[candN]; }
                    }
                    continue;   // 次の買い手へ（旧経路は通らない）
                }
                // 最良と次善を同時に取る＝価格は**次善の相手**が決める（1人しか売り手が居なければ買い手の言い値が上限）。
                int best = -1; double bestDeliver = double.MaxValue, secondDeliver = double.MaxValue, bestKm = 0;
                double minFreightMargin = c.TransportMargin * adValoremRev;   // 距離0でも掛かる最低マージン＝枝刈りの下界（運賃革命の従価割引込み）
                double pruneWedge = borderOnly ? Math.Min(wedge, wedgeHome) : wedge;   // 枝刈りは小さい方のwedgeで（打ち切り条件の健全性）
                for (int s = 0; s < sellers.Count; s++)
                {
                    if (left[s] <= 1e-12) continue;
                    if (sellers[s].Price * (1 + minFreightMargin + pruneWedge) >= bestDeliver) break;   // 昇順なので以降は必ず高い＝打ち切り
                    if (TradeReachDiag.On) TradeReachDiag.ScanPruned++;
                    double km = KmOf(sellers[s].Node, t);   // ★手1：メモ化（値は PairKm と同一）
                    double risk = PairRisk(sellers[s].Node, t);
                    double blockadeBias = gd.Tier == GoodTier.Staple ? c.BlockadeStapleBias * risk : 0.0;
                    double margin = c.TransportMargin * (1.0 + c.DistanceMarginScale * km / DistanceScaleKm) * adValoremRev
                                  + weightAdd * (km / DistanceScaleKm) + risk;
                    double wedgeS = borderOnly && tr[sellers[s].Node].nation == ni ? wedgeHome : wedge;   // ★①：同国＝国境の税なし
                    double deliver = sellers[s].Price * (1 + margin + wedgeS + blockadeBias);
                    if (deliver < bestDeliver) { secondDeliver = bestDeliver; bestDeliver = deliver; best = s; bestKm = km; }
                    else if (deliver < secondDeliver) secondDeliver = deliver;
                }
                if (TradeReachDiag.On && !TradeReachDiag.Cheap)   // 純テレメトリ：枝刈り（相手の絞り込み）が最良の売り手を取り逃していないか＝全探索と突き合わせる
                {
                    int bestF = -1; double bestDelF = double.MaxValue;
                    for (int s = 0; s < sellers.Count; s++)
                    {
                        if (left[s] <= 1e-12) continue;
                        TradeReachDiag.ScanFull++;
                        double kmF = KmOf(sellers[s].Node, t);   // ★手1：メモ化（診断側も同じ表）
                        double bbF = gd.Tier == GoodTier.Staple ? c.BlockadeStapleBias * PairRisk(sellers[s].Node, t) : 0.0;
                        double marF = c.TransportMargin * (1.0 + c.DistanceMarginScale * kmF / DistanceScaleKm) * adValoremRev
                                    + weightAdd * (kmF / DistanceScaleKm) + PairRisk(sellers[s].Node, t);
                        double wF = borderOnly && tr[sellers[s].Node].nation == ni ? wedgeHome : wedge;
                        double dF = sellers[s].Price * (1 + marF + wF + bbF);
                        if (dF < bestDelF) { bestDelF = dF; bestF = s; }
                    }
                    TradeReachDiag.BuyerRows++;
                    if (bestF < 0) TradeReachDiag.NoSeller++;
                    else if (!(lp > bestDelF)) TradeReachDiag.NoMargin++;
                    else TradeReachDiag.Dealt++;
                    if (bestF >= 0 && bestDelF < bestDeliver - 1e-12)
                    {
                        TradeReachDiag.PruneWorseBest++;
                        TradeReachDiag.PruneDeliverExcess += bestDeliver / Math.Max(1e-12, bestDelF) - 1.0;
                        double qF = Math.Max(0, (lp - bestDelF) / avg), qP = best >= 0 ? Math.Max(0, (lp - bestDeliver) / avg) : 0;
                        TradeReachDiag.PruneQLost += (qF - qP) * tr[t].access * c.TradeSpeed * Math.Max(Math.Max(m.LastDemand, m.LastProduced), m.TargetStock);
                        if (lp > bestDelF && (best < 0 || !(lp > bestDeliver))) TradeReachDiag.PruneKilledDeal++;
                    }
                }
                if (TradeReachDiag.On && TradeReachDiag.Cheap)   // 軽量モードの棄却理由（枝刈り後の best で分類＝全探索版と同じ意味の分類）
                {
                    TradeReachDiag.BuyerRows++;
                    if (best < 0) TradeReachDiag.NoSeller++;
                    else if (!(lp > bestDeliver)) TradeReachDiag.NoMargin++;
                    else TradeReachDiag.Dealt++;
                }
                if (best < 0 || !(lp > bestDeliver)) continue;   // 否定形＝NaNは取引しない

                // 単価＝次善の相手の着値（競争）。売り手が1人なら買い手の言い値まで（独占レント＝markup係数を使わずに出る）。
                double unit = Math.Min(lp, double.IsPositiveInfinity(secondDeliver) ? lp : secondDeliver);
                unit = Math.Max(unit, sellers[best].Price);   // 売り手は自分の相場を割って売らない

                double scaleT = Math.Max(Math.Max(m.LastDemand, m.LastProduced), m.TargetStock);
                if (c.TradeScaleUsesDesired > 0) scaleT = Math.Max(Math.Max(m.LastDesiredDemand, m.LastProduced), m.TargetStock);   // 反実仮想（測定専用・既定0で通らない）
                double q = tr[t].access * c.TradeSpeed * ((lp - bestDeliver) / avg) * scaleT;
                if (c.TradeBuyerScaleMult != 1.0) q *= c.TradeBuyerScaleMult;   // 診断レバー（既定1.0＝この分岐を通らない＝ビット恒等）
                // ★読み取り専用の反実仮想（2026-08-21）：買い手の scale だけを「予算で切られる前の需要」に置き換えたら
                //   希望輸入量が何倍になるか。q は scaleT に**線形**なので比はそのまま scale の比＝新しい観測を増やさない。
                if (BudgetBindDiag.On && q > 0)
                {
                    double scaleTd = Math.Max(Math.Max(m.LastDesiredDemand, m.LastProduced), m.TargetStock);
                    BudgetBindDiag.TradeScaleRow(nations[ni].World, q, scaleT > 1e-12 ? scaleTd / scaleT : 1.0);
                }
                if (g == c.OpiumGoodId && oban[ni]) q *= Contraband.LeakFraction(tr[t].r.AdminFill, c);   // 輸入禁止＝取り締まれた分だけ止まる
                if (TradeReachDiag.On)
                {
                    TradeReachDiag.QWant += Math.Max(0, q); if (q > left[best]) TradeReachDiag.QClipped++;
                    // 恒等分解（RefPrice建て・数量だけの物差し）：Σq×Ref = TradeSpeed × Σ(access×gap×scaleT×Ref)（アヘン禁制の漏れ率を除く）
                    double gapR = (lp - bestDeliver) / avg, accR = tr[t].access, rp = gd.RefPrice;
                    TradeReachDiag.QRefWant += Math.Max(0, q) * rp;
                    TradeReachDiag.ScaleRefDealt += scaleT * rp;
                    TradeReachDiag.AccGapScaleRefDealt += accR * gapR * scaleT * rp;
                    TradeReachDiag.AccScaleRefDealt += accR * scaleT * rp;
                    TradeReachDiag.GapScaleRefDealt += gapR * scaleT * rp;
                }
                q = Math.Min(q, left[best]);
                if (!(q > 1e-12)) continue;
                if (TradeReachDiag.On) { TradeReachDiag.QGot += q; TradeReachDiag.QRefGot += q * gd.RefPrice; }

                int sn = sellers[best].Node, si = tr[sn].nation;
                var ms = tr[sn].r.Markets[g];
                double value = q * unit;
                if (TradeReachDiag.On)
                {
                    TradeReachDiag.Deal(si, ni, value, bestKm);
                    TradeReachDiag.DealRef(si, ni, q * gd.RefPrice);   // 参照価格建ての同じ成約（GDPと同じ物差し・純テレメトリ）
                    // 量式の各因子を成約行だけで積む（value = access × TradeSpeed × gap × scaleT × unit）
                    TradeReachDiag.RowsDealt++; TradeReachDiag.ScaleTDealt += scaleT; TradeReachDiag.MarketValDealt += scaleT * lp;
                    TradeReachDiag.AccessDealt += tr[t].access; TradeReachDiag.GapDealt += (lp - bestDeliver) / avg;
                    TradeReachDiag.UnitOverLp += unit / lp; TradeReachDiag.ValueDealt += value; TradeReachDiag.LpOverAvgDealt += lp / avg;
                }
                left[best] -= q;
                ms.Stock -= q; ms.LastExport += q;
                m.Stock += q; m.LastImport += q;
                volLoad[t] += q * Wg; volLoad[sn] += q * Wg;

                // 決済（旧と同型）：買い手が払い、売り手が受け取る。運賃だけが荷主から海運プールへ。
                // ★貿易復活①：TariffBorderOnly>0 では**同国取引を輸入に数えない**（importVal＝関税徴収の基礎・impByGood/expByGood＝国際貿易統計）。
                //   金の移動（netFlow）と港の取扱高・総取引量（grossVal）は国内輸送の実体なので従来どおり。フラグ0＝旧記帳とビット恒等。
                bool crossBorder = !(c.TariffBorderOnly > 0) || si != ni;
                if (c.DiffusionWeight > 0 && si != ni) AccumulatePairFlow(si, ni, value, netFlow.Length);   // ★段7：ON経路と同じ1行（⚠OFF側を直したらここも直す、の鏡映）
                netFlow[ni] -= value; if (crossBorder) importVal[ni] += value; grossVal[ni] += value;
                tr[t].r.LastPortThroughput += value; if (crossBorder) impByGood[ni][g] += value;
                grossVal[si] += value; tr[sn].r.LastPortThroughput += value; if (crossBorder) expByGood[si][g] += value;
                double toProducers = c.ExportRevenueToProducers > 1e-12 ? PayExportToProducers(tr[sn].r, g, value * Math.Clamp(c.ExportRevenueToProducers, 0, 1), ref payPr[sn], ref payTot[sn]) : 0.0;
                netFlow[si] += value - toProducers; direct[si] += toProducers;
                if (c.FreightRatePer10Mm > 1e-12)
                {
                    double freight = c.FreightRatePer10Mm * value * (bestKm / DistanceScaleKm);
                    netFlow[ni] -= freight; freightPool += freight;
                }
                if (buyDear > 0) biasCost[ni] += q * avg * buyDear;
                _volume[g] += q;
                // ★実際の相手を記録（作者指示2026-08-11・UIの「輸入元/輸出先」）＝最大額の相手だけを残す。
                //   二国間になったので、これは近似ではなく**その財で最も多く取引した実在の相手**。
                RecordPartner(nations[ni].World.ImportFromByGood, g, tr[sn].r.Name, value, impTop[ni]);
                RecordPartner(nations[si].World.ExportToByGood, g, tr[t].r.Name, value, expTop[si]);
                // 診断：この州が実際に運んだ距離＝**最大額の取引の距離**（旧の「重心まで」の後継。貿易タブとテストが読む）。
                if (value > distTop[t]) { distTop[t] = value; tr[t].r.LastTradeDistanceKm = bestKm; }
                if (value > distTop[sn]) { distTop[sn] = value; tr[sn].r.LastTradeDistanceKm = bestKm; }
                if (kmLand[sn * tc + t] || kmLand[t * tc + sn])   // 陸路可視化（ON経路の記帳ブロックの鏡映）
                {
                    if (value > tr[t].r.LastLandPartnerFlow) { tr[t].r.LastLandPartnerFlow = value; tr[t].r.LastLandPartnerName = tr[sn].r.Name; }
                    if (value > tr[sn].r.LastLandPartnerFlow) { tr[sn].r.LastLandPartnerFlow = value; tr[sn].r.LastLandPartnerName = tr[t].r.Name; }
                }
            }
        }
        if (swSec is not null) TimeClearTicks += swSec.ElapsedTicks;   // ★計器：最後の財の清算ループ（旧は足していなかった）

        // 容量律速の診断（旧 LastTradeThrottle と同じ意味＝1未満なら鉄道/船/港が交易を絞っていた）
        for (int t = 0; t < tc; t++)
        {
            double cap = tr[t].cap;
            // 今月の実積載から次月の律速率を出す。★律速中は積載自体が絞られているので、絞る前の潜在量へ戻して測る
            //   （絞った後の量で割ると必ず1.0付近に戻ってしまい、容量が効かなくなる＝自己解除のバグになる）。
            double thisThrottle = Math.Clamp(tr[t].access > 1e-12 ? tr[t].access / Math.Max(1e-12, natAccessN[tr[t].nation]) : 1.0, 1e-6, 1.0);
            double potential = clearOn ? potNew![t] : volLoad[t] / thisThrottle;   // ON＝容量で切る前の pot·W（新定義・§2-3）
            tr[t].r.LastTradeThrottle = potential > 1e-9 && cap < double.MaxValue ? Math.Min(1.0, cap / potential) : 1.0;
            tr[t].r.LastTradePotential = potential;   // ★T0（輸送doc・2026-08-13）：比だけでなく**絶対量**を残す（純テレメトリ・力学は読まない）
            // 取引が1件も立たなかった州＝陸脚だけは確定しているので、そこまでを距離として残す（0km＝未接続と紛れさせない）。
            if (distTop[t] <= 0 && tr[t].linked) tr[t].r.LastTradeDistanceKm = tr[t].landKm * terrCost[t] * landRatio[t];   // ★実着値の物差し（:262/:273）に統一＝地形/鉄道低下を無視した旧定数(12)フォールバックを廃止
        }

        for (int i = 0; i < nn; i++)
        {
            if (freightPool > 1e-12) netFlow[i] += freightPool * shipShare[i];
            if (Math.Abs(netFlow[i]) > 1e-12 || Math.Abs(direct[i]) > 1e-12)
                Apply(nations[i].World, netFlow[i], grossVal[i], c, direct[i]);
        }
        for (int i = 0; i < nn; i++)
        {
            var w0 = nations[i].World;
            w0.LastTariffRevenue = 0;
            w0.LastImportValue = importVal[i];
            w0.LastExportValue = Math.Max(0, grossVal[i] - importVal[i]);
            if (tar[i] >= 0) CollectTariff(w0, importVal[i] * tar[i] * c.TariffRevenueRate);
            else PaySubsidy(w0, w0.LastExportValue * Math.Abs(tar[i]) * c.TariffRevenueRate, tradeBias: false);
            if (biasCost[i] > 1e-12) PaySubsidy(w0, biasCost[i], tradeBias: true);
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
            var payScale = new double[nn];
            for (int u = 0; u < nn; u++)
                payScale[u] = totalByUser[u] > 1e-9 ? Math.Min(totalByUser[u], Math.Max(0.0, nations[u].World.Treasury)) / totalByUser[u] : 0.0;
            foreach (var k in order)
            {
                double toll = owed[k] * payScale[k.U];
                if (toll <= 1e-12) continue;
                var uw = nations[k.U].World; var ow = nations[k.O].World;
                uw.Pay(TreasuryCat.OtherOut, -toll); uw.CumulativeTradeFlow -= toll;
                ow.Pay(TreasuryCat.OtherIn, toll); ow.CumulativeTradeFlow += toll;
            }
        }
    }
}
