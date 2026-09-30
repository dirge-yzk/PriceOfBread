namespace PriceOfBread.SimCore;

/// <summary>
/// 輸送容量（§5.1拡張）：鉄道（内陸→港）と海運（港→世界市場）の容量を、鉄道財/船財の投資で積む。
/// 投資家が国内市場から鉄道(線路+機関車)を、商人が船（帆船か汽船の、容量あたりの総費用が安い方）を買い→容量に変換（在庫消費＝非貨幣）。
/// 容量は毎期減耗し、維持に再投資が要る＝鉄道/船の持続需要を生む（史実：輸送財の需要増）。
/// 金は 買い手pop→生産者の④の持分（出力の割合で按分＝輸出代金と同じ売上の経路・InternationalMarket.PaySales）で移転＝保存（BaseMoney不変）。積んだ容量が交易量を律速する（<see cref="InternationalMarket"/>＝清算の本体は BilateralMarket.cs）。
/// </summary>
public static class Transport
{
    public static void Step(World w, Coefficients c)
    {
        if (!c.TransportEnabled) return;   // TradeEnabledに依存しない（多国シムは InternationalMarket 経由・TradeEnabled=false のため）
        w.RailCapacity = System.Math.Max(0, w.RailCapacity * (1 - c.TransportDecay));   // 減耗（鉄道は0スタート）
        w.ShipCapacity = System.Math.Max(0, w.ShipCapacity * (1 - c.TransportDecay));
        double target = w.TotalPopulation * c.TransportTargetPerPop;   // 経済規模に応じた目標容量
        // ★T4（輸送doc §3-1・2026-08-13）：目標の錨を**人口**から「実際に運びたかった量」（LastTradePotential＝律速率の分母）へ寄せる。
        //   ⚠人口だけの目標は、交易しない人口大国に容量を積ませ、交易集約の小国に積ませない（軍とまったく同じ病＝費用も需要も見ない目標追従）。
        //   ★形は軍で確立したもの（扶養限界のブレンド比 ArmySustainWeight・(145) で k＝1 に達して削除）を踏襲する——⚠`Math.Max(式A, 式B)` で競わせない。
        //     2式が拮抗する国で支配側が切り替わるたび値が不連続に飛ぶ（軍の3回目の失敗の原因）。**片方に寄せるのでなくブレンドで統一する。**
        //   ★人口項を残すのは較正の土台であると同時に、容量0からの立ち上がりを支えるため（潜在量は容量が無いと観測されない＝鶏卵）。
        //   既定0＝人口のみ＝**厳密恒等**。
        if (c.TransportPotentialWeight > 0)
        {
            double pot = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) pot += r.LastTradePotential;
            if (!(pot >= 0)) pot = 0;   // NaNは人口項のみへ倒す（否定形で書く）
            target = target * (1.0 - c.TransportPotentialWeight) + pot * c.TransportPotentialHeadroom * c.TransportPotentialWeight;
        }
        // ★安保doc手順3（2026-08-30点火＝既定1.0）：国防の目的関数＝受けている脅威が大きいほど目標容量を積む（戦略鉄道の一般化＝路線を1本も定義しない）。
        //   脅威比＝前期の SecurityThreat/自国質量（無次元）。target×(1+k×脅威比)＝民間項（上）に**乗せるだけ**＝どの路線かは書かない。
        //   ⚠正のフィードバック（鉄道→脅威→鉄道）は国庫の有限性だけが歯止め。0＝この分岐を通らない＝厳密恒等。
        if (c.ThreatRailTargetWeight > 0 && w.SecurityThreatOwnMass > 1e-9)
        {
            double threatRatio = w.SecurityThreat / w.SecurityThreatOwnMass;
            if (threatRatio > 0 && double.IsFinite(threatRatio)) target *= 1.0 + c.ThreatRailTargetWeight * threatRatio;
        }
        // 接3（監査2026-07-11・2026-07-25休眠配線・2026-07-27点火＝既定1.0）：混雑シグナル（輸送律速率）を目標へ接続＝交易集約の小国は容量を積み増し、
        // 交易の乏しい人口大国は使わない容量へ資金を誤配分しない。混雑＝活動州の(1−律速率)平均（港湾の混雑ゲートと同じ状態量）。0＝不読＝厳密恒等。
        if (c.TransportCongestionWeight > 0)
        {
            double congSum = 0; int congN = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) { congSum += 1.0 - r.LastTradeThrottle; congN++; }
            if (congN > 0) target *= 1.0 + c.TransportCongestionWeight * (congSum / congN);
        }
        // 鉄道は技術で解禁（史実1820s〜）：技術がonset未満は内陸輸送が道路/河川（基準値）のみ＝序盤の内陸交易は強く絞られる。船（帆船＝海運）は古来あり据え置き。
        double railGate = c.RailGate(w.TechLevel);   // 鉄道の門の1本（清算の陸路の費用と同じ・段4e K）
        BuyToward(w, c, [c.RailGoodId, c.LocoGoodId], target * railGate, rail: true, 1.0);
        // ★2026-09-27（回4-3 の再挑戦・作者裁定 2026-09-26 (3)「汽船の性能を上げるべき」）：汽船の性能を、植民地補給線と同じ機関の時代係数から導く。
        //   era＝EraFactor(前線技術, EraColonyCoal)＝運賃革命のランプ（前線 1.7〜2.9）に沿って 1.0→0.5（複式機関1860年代→三段膨張1880年代・史実の石炭/馬力時は約4ポンド→1.5ポンド）。
        //   ①汽船1単位の容量＝TransportCapPerUnit×ShipTier2CapMult×その州の汽船の解禁度（BuyToward の注記）÷era（解禁度1で 2.5→5.0）＝同時代の換算「汽船1トンは帆船3〜5トン分を運ぶ」
        //     （速さ 8〜10ノット対 帆走の実効 4〜5ノット・天候に左右されない定期性・港の回転・スエズ1869を通れる・1880年代の鋼の船体の大型化。
        //      英〜印の往復＝帆船のケープ回り年1回に対し、スエズ経由の汽船は年約3回）。②容量あたりの燃料＝ShipFuelRate×era（同じ石炭で2倍運ぶ）。
        //   ⇒ 解禁度1の汽船1隻が月に焚く石炭（容量×燃料率）は era に依らない＝性能の上がった分だけ容量単価が下がる。新しい係数・状態は無い（era は補給線と同じ1本・解禁度は軍の装備と同じ1本を読むだけ）。
        double era = c.EraFactor(w.WorldTechLevel, c.EraColonyCoal);
        double shipBefore = w.ShipCapacity;   // 減耗の後・購入の前の容量
        var (shipAdd, steamAdd) = BuyToward(w, c, c.ShipGoodIds, target, rail: false, era);
        // 運用コスト監査2026-08-16 #3（汽船の運行燃料）：ShipCapacity は帆船と汽船の合算＝購入後に区別が消える。燃料需要＝ShipFuelRate×era×ShipCapacity×汽船の割合。
        // ★2026-09-26（回4-3・項目8）：汽船の割合を平滑値（旧 ShipSteamShareSmoothing）でなく、容量の在庫に占める汽船の容量そのものにする
        //   ＝（汽船の割合×購入前の容量＋今月の汽船の容量）÷購入後の容量。減耗（上）と戦災（Diplomacy の輸送容量の戦災）は帆船と汽船に同じ率で掛かる＝割合を変えない。
        //   燃料の係数の門の外で常に更新する。Min(1) は丸めの守り（定義上1を超えない）。
        if (shipAdd > 0)
        {
            double s = (w.ShipSteamShareEma * shipBefore + steamAdd) / w.ShipCapacity;
            w.ShipSteamShareEma = s >= 0 ? System.Math.Min(1.0, s) : 0.0;   // NaN は帆走の側へ
        }
        if (c.ShipFuelRate > 0) BuyShipFuel(w, c, era);
    }

    /// <summary>★州の取り分の規則の1本（2026-09-30 (118)）＝国の輸送の容量のうち州が使う割合の基底（国ごと）。
    /// 清算（BilateralMarket の港と鉄道の容量）と州の移し替え（<see cref="Territory.TransferRegion"/>＝容量が州と一緒に移る）が同じ規則を読む。
    /// 均等割り＝活動州の数の逆数・潜在量比例の度合い＝TransportShareByPotential（0..1）・潜在量の和＝活動州の LastTradePotential（負は0・四半期の最初の清算が書いた値＝2026-09-30 (130)）。
    /// 潜在量の和が痩せたら（開幕・実績なし）均等へ倒す＝<see cref="RegionShare"/> が読む。</summary>
    internal static (double Even, double KPot, double TotalPot) ShareBasis(World w, Coefficients c)
    {
        int activeCount = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) activeCount++;
        double evenShare = activeCount > 0 ? 1.0 / activeCount : 1.0;
        double kPot = PoliticalQuantities.Clamp01(c.TransportShareByPotential);
        double totalPot = 0;
        if (kPot > 0) foreach (var r in w.Regions) if (r.State == RegionState.Active) totalPot += System.Math.Max(0, r.LastTradePotential);
        return (evenShare, kPot, totalPot);
    }

    /// <summary>州の取り分＝(1−k)×均等＋k×州の潜在量÷潜在量の和（潜在量の和≦1e-9 か k＝0 なら均等）。基底は <see cref="ShareBasis"/>。</summary>
    internal static double RegionShare((double Even, double KPot, double TotalPot) b, Region r)
        => b.KPot > 0 && b.TotalPot > 1e-9 ? (1.0 - b.KPot) * b.Even + b.KPot * System.Math.Max(0, r.LastTradePotential) / b.TotalPot : b.Even;

    /// <summary>★移ってきた容量を受け取る（2026-09-30 (118)）＝割譲・再併合・建国で州と一緒に来た鉄道と船。汽船の割合は容量で加重して混ぜる
    /// （汽船の割合は容量の在庫に占める汽船＝Step の注記と同じ定義）。</summary>
    internal static void Receive(World to, double rail, double ship, double steamShare)
    {
        if (rail > 0) to.RailCapacity += rail;
        if (!(ship > 0)) return;   // 否定形＝NaN も受け取らない
        double tot = to.ShipCapacity + ship;
        double s = (to.ShipSteamShareEma * to.ShipCapacity + steamShare * ship) / tot;
        to.ShipSteamShareEma = s >= 0 ? System.Math.Min(1.0, s) : 0.0;   // NaN は帆走の側へ（Step と同じ守り）
        to.ShipCapacity = tot;
    }

    // #3 汽船の運行燃料（運用コスト監査2026-08-16）：商人pop（船の買い手＝BuyToward と同じ選択）が地元市場から石炭（または重油＝
    // Colonial B6 の二択・供給実在ガード込み）を買って焚く。決済は BuyToward の船購入と同一パターン（需要計上→在庫/財布律速→
    // 生産者の持ち主へ出力の割合で支払い＝pop→pop 移転で保存・LastSold 計上で財収支を保つ）。呼び手が ShipFuelRate>0 でゲート済み。
    // era＝機関の時代係数（Step の注記）＝容量あたりの燃料は era 倍（三段膨張の時代は半分）。
    private static void BuyShipFuel(World w, Coefficients c, double era)
    {
        double totalWant = c.ShipFuelRate * era * w.ShipCapacity * w.ShipSteamShareEma;   // 帆走のみ（割合0）なら需要ゼロ
        if (!(totalWant > 1e-9)) return;   // 否定形＝NaNも不走行側へ
        int regCount = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active && (r.MerchantPopIndex >= 0 || r.InvestorPopIndex >= 0)) regCount++;
        if (regCount == 0) return;
        double per = totalWant / regCount;   // BuyToward と同じ州按分（1州あたりの希望量）
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            int buyerIdx = r.MerchantPopIndex >= 0 ? r.MerchantPopIndex : r.InvestorPopIndex;   // 船＝商人が負担（BuyToward と同一の選択）
            if (buyerIdx < 0 || Catalog.CoalId >= r.Markets.Length) continue;
            var buyer = r.Pops[buyerIdx];
            var (fuelG, want) = Colonial.ChooseFuelGood(r, c, per);   // 石炭→重油の二択（海軍・植民地補給線と同式）
            // 汽船が焚く＝市場の外の買い付けの1本（InternationalMarket.Procure・段4e K）＝需要の計上（在庫が無くても＝鶏卵回避）・買い手の貯蓄まで・販売（LastSold）・
            //   代金は④の持分（★2026-09-26 回4-3・econ-12＝旧は最初の生産者の持ち主へ全額／★段3-1 C4＝地代・国営持分・賃金＝Earned を通す）。
            //   生産者が居ない（在庫は輸入の在庫）なら在庫の持ち主＝捌き手の商人へ（段2-2 の A2 と同じ規則）。買い手が商人なら自分の在庫を使うだけ（払って受け取る＝純0）。
            InternationalMarket.Procure(w, r, fuelG, want, buyer);
        }
    }

    // 目標容量へ向けて鉄道/船を国内市場から購入→容量を積む。**鉄道＝投資家（資本家）・船＝商人**が負担：
    // 商人は貿易と流通で稼ぎ、商船を買って海運を支える（交易↑→商人が富む→商船↑→さらに交易↑の自己強化）。
    // 在庫が無くても希望量を需要に計上＝生産を立ち上げる（鶏卵回避）。買えた分だけ容量に変換（在庫消費＝非貨幣）。金は買い手pop→生産者の持ち主で保存。
    // ★2026-09-26（回4-3・項目8・econ-12）：船は州ごとに「容量1あたりの総費用」が安い1財だけを欲しがる（旧＝帆船と汽船を同じ単位数ずつ・値段を見ない）。
    //   帆船＝値段÷TransportCapPerUnit。汽船＝値段÷(TransportCapPerUnit×汽船の容量倍)＋寿命（1÷TransportDecay か月）の間に焚く燃料の代金
    //   （ShipFuelRate×era×燃料の値段÷TransportDecay・燃料は石炭か重油の安い方＝Colonial.ChooseFuelGood と同じ二択）。
    //   汽船の容量倍＝ShipTier2CapMult×その州の汽船の解禁度（EconomyStep.EquipUnlock＝製法の技術ゲート・発見の唯一の読み口）÷era（Step の注記）。
    //   ★2026-09-27（回4-3 の再挑戦）：解禁度は「その州で汽船を外洋で使いこなせる度合い」＝技術 0.5→1.5 で 0→1（英は1810年代の沿岸の外輪船→1838年のシリウス・グレート・ウェスタン号の大西洋横断→1840年のキュナード）。
    //   ⇒ 燃料が高い州・時代は帆船に戻る（転換は費用で起きる）。
    //   汽船は、その州で汽船が発見済み（解禁度＞0）で、かつ供給が実在するとき（在庫があるか、今月の生産か輸入が正）だけ候補に入る（ChooseFuelGood の供給の確かめと同じ作法）
    //   ＝作れない時代に汽船の幻の需要は立たない（⚠在庫だけでは足りない＝全州が開幕に汽船200単位を持つ・Simulation.CatalogEconomy の一律の初期在庫）。
    //   同値は帆船（先に並ぶ方）。鉄道（線路と機関車）は補完財なので両方を同じ単位数ずつ（今のまま・era は読まない）。
    //   量＝州の不足の容量を、選んだ財の1単位の容量で割った単位数。
    // 戻り値＝今月積んだ容量の（合計, うち汽船tier2分）。Step が汽船の割合（World.ShipSteamShareEma）を更新する。
    private static (double Total, double Steam) BuyToward(World w, Coefficients c, int[] goods, double target, bool rail, double era)
    {
        double addTotal = 0, addSteam = 0;
        double cap = rail ? w.RailCapacity : w.ShipCapacity;
        double gapUnits = (target - cap) * c.TransportBuildRate / c.TransportCapPerUnit;   // 今月詰めたい総単位数（1単位＝TransportCapPerUnit の容量で数える）
        if (gapUnits <= 1e-9) return (0, 0);
        int regCount = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active && r.InvestorPopIndex >= 0) regCount++;
        if (regCount == 0) return (0, 0);
        double perGood = rail ? gapUnits / regCount / goods.Length : gapUnits / regCount;   // 1州1財あたりの希望単位（船は選んだ1財だけ）
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || r.InvestorPopIndex < 0) continue;
            int buyerIdx = rail ? r.InvestorPopIndex : (r.MerchantPopIndex >= 0 ? r.MerchantPopIndex : r.InvestorPopIndex);
            var investor = r.Pops[buyerIdx];
            double steamGate = rail ? 0.0 : EconomyStep.EquipUnlock(r, c, c.ShipTier2GoodId);   // 汽船の解禁度（0..1・NaN は0）
            double steamMult = c.ShipTier2CapMult * steamGate / era;   // 汽船1単位の容量倍（era≥0.05＝EraFactor の床で有限）
            int pick = -1;   // 船：この州で容量あたりの総費用が最も安い財
            if (!rail)
            {
                double best = 0;
                foreach (int gi in goods)
                {
                    if (gi < 0 || gi >= r.Markets.Length) continue;
                    var mg = r.Markets[gi];
                    bool steam = gi == c.ShipTier2GoodId;
                    if (steam && !(steamGate > 0 && (mg.Stock > 1e-9 || mg.LastProduced + mg.LastImport > 0))) continue;   // 未発見か、汽船の供給が実在しない州では候補にしない
                    double cost = mg.Price / (c.TransportCapPerUnit * (steam ? steamMult : 1.0));
                    if (steam && c.ShipFuelRate > 0)
                    {
                        var (fuelG, fuelQty) = Colonial.ChooseFuelGood(r, c, 1.0);   // 石炭1の熱量あたりの燃料（重油なら1/比）
                        cost += c.ShipFuelRate * era * r.Markets[fuelG].Price * fuelQty / c.TransportDecay;
                    }
                    if (pick < 0 || cost < best) { pick = gi; best = cost; }
                }
            }
            foreach (int gi in goods)
            {
                if (gi < 0 || gi >= r.Markets.Length) continue;
                if (!rail && gi != pick) continue;
                var m = r.Markets[gi];
                double mult = (!rail && gi == c.ShipTier2GoodId) ? steamMult : 1.0;   // 汽船=tier2＝1単位あたり容量が大（蒸気船の革命・時代とともに伸びる）
                // T1（輸送doc §3-3・2026-07-31）：**買えない国の需要が世界市場に載り、買える国の調達費を押し上げる**歪みの是正。
                // 旧＝希望量をそのまま計上（在庫0でも生産を駆動する鶏卵回避が目的で、意図は正しい）。⚠ ただし財布を一切見ていなかった。
                // >0 で「買い手の手持ちで買える単位数 × これ」を希望量の上限にする＝**貯蓄があれば在庫0でも需要は立つ**（鶏卵回避は保つ）が、無い袖の需要は消える。
                // 0（既定）＝上限なし＝分岐に入らず**厳密恒等**。
                double want = perGood / mult;   // 同じ容量に要る単位数（汽船は1/steamMult）
                if (c.TransportDemandAffordCap > 0 && m.Price > 1e-9)
                    want = System.Math.Min(want, c.TransportDemandAffordCap * System.Math.Max(0, investor.Savings) / m.Price);
                // 買い付けは燃料と同じ1本（InternationalMarket.Procure・段4e K）＝需要の計上（在庫が無くても＝生産を駆動＝鉄道/船の需要が上がる＝史実・ブートストラップ）・
                //   買い手の貯蓄まで・販売（LastSold＝在庫減を販売として記録し§11.6財収支を保つ）・代金は④の持分（econ-12・段3-1 の C4）。
                double buy = InternationalMarket.Procure(w, r, gi, want, investor);
                if (buy <= 1e-9) continue;
                double add = buy * c.TransportCapPerUnit * mult;
                if (rail) w.RailCapacity += add; else w.ShipCapacity += add;
                addTotal += add;
                if (!rail && gi == c.ShipTier2GoodId) addSteam += add;   // 汽船の割合（容量加重）の分子
            }
        }
        return (addTotal, addSteam);
    }
}
