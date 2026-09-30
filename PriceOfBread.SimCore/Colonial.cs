namespace PriceOfBread.SimCore;

/// <summary>
/// 植民地の整備（§6.1 拡張）。固有資源（史実・1780）＋人口プール＋開発度。初期は産出ほぼ0で、投資（輸送船＋鉄道）が
/// 遅れて実る＝開発度の飽和曲線で産出が伸び、限界収益は逓減する（過剰投資はしない）。私的投資家（ブルジョワ）は
/// 「増える資源の価値 ＞ 投資コスト」の間だけ自腹で投資する（レバーなし＝市場任せ）。資源側は EconomyStep が固有資源だけを産出率倍で輸入する。
/// 保存：投資は投資家→生産者(pop)（内部移転）、捕囚市場は外部マネー流入（CumulativeTradeFlow）。
/// </summary>
public static class Colonial
{
    internal const double OilViableProductionFrac = 0.25;       // 石油を燃料二択に載せる継続生産の下限（必要量比）。海軍運用燃料・民間汽船燃料（運用コスト監査2026-08-16）と共有
    private const double ColonyLiteracyCap = 0.9;               // 植民地識字の上限
    private const double ColonyMilitancyLiteracyBase = 0.3;     // 不満成長の識字ベース項（教育された植民地ほど独立志向）
    private const double FreedColonyMilitancy = 0.2;            // 独立達成後にリセットする不満値
    private const double SettlerMonthlyOutflowCap = 0.01;       // 入植の安全上限＝1植民地あたり月に本国大衆の1%まで（係数の誤設定で本国が空になる事故の物理停止）

    /// <summary>開発度→産出率（0で0、飽和で1へ。初期低・逓減）。</summary>
    public static double OutputCurve(double dev, double half) => dev <= 0 ? 0 : dev / (dev + half);

    public static void Step(World w, Coefficients c)
    {
        int colonies = w.SelfPower?.Colonies ?? 0;
        var r = w.Region;   // 本国（首都地域）
        // 開発度の減衰（維持しないと衰える）。★助走中は敷設（下の w.ColonialInfra += dev）を止めているので、減衰だけ効かせると
        //   植民地開発度が一方的に痩せる＝凍結は両側で（2026-08-10 追加裁定・本国の工業化の減価と同じ扱い）。
        //   ★回1-1（add-04・2026-09-26）：下の早期 return より**前**に置く＝植民地を全て失った国でも開発度は減衰する
        //   （旧＝return の後ろ＝最後の値のまま凍結し、再獲得で古い開発度が効き始めた）。
        if (!w.WarmupFrozen) w.ColonialInfra = Math.Max(0, w.ColonialInfra * (1 - c.ColonialInfraDepreciation));
        // 全喪失後に旧値が残らない（接1診断）。★回1-1（col-08）：海運の占有も0へ戻す（旧＝最後の月の占有が永久に残り、交易の海運を細らせた）。
        if (colonies <= 0 || r.State != RegionState.Active || r.ColonialResources is null) { w.ColonialPop = 0; w.LastColonialUpkeep = 0; w.ColonialShipBurden = 0; return; }

        // 0. 維持費（帝国の過伸展）：不満は俸給（金銭）、距離は**物理消費**（汽船の石炭＋駐留の缶詰糧食）＋海運の混雑から創発。
        //    遠方植民地が多い国（イギリス）ほど石炭・糧食が逼迫し交易船が補給に取られる＝一強の是正を物理コストで律する（固定倍率2.0を排除）。
        // ★2026-09-28（植民地の費用の側・c1・c2・R2）：
        //   c1＝補給線の石炭は民間の汽船と**同じ状態**（汽船の割合 ShipSteamShareEma＝帆走なら0）と**同じ物理**（機関の時代係数 EraColonyCoal＝容量あたりの燃料・Transport と共有）で焚く。
        //       旧＝汽船の発明の前から焚いた（英1783-1802 の植民地維持費の実払いの62%）。缶詰糧食は缶詰の財の解禁度（EquipUnlock＝装備の解禁の唯一の読み口）で食う
        //       ＝アペール法の前の駐留は缶詰を食わない。旧の EraColonyFood（缶詰・冷蔵で距離あたりの糧食が下がる）はこの解禁度が置き換えた＝係数ごと削除。
        //   c2＝平場（旧 ColonyUpkeepBase＝1行150/月）を撤去＝同じ俸給は行政の購買（植民地格の州の官僚の給与×人数×首都からの距離の割増＝UpkeepDistanceGeneral・2026-09-30 (135) に
        //       旧の行政費の人頭割り AdminCostPerPop から移した＝Fiscal.AdminDistanceMul）が払っている。不満の上乗せ（駐留・鎮圧）は行政の購買にも兵力の購買にも無い＝残す。
        //   R2＝距離の費用と海運占有は行（州）の数でなく**質量**（ColonyMassUnits の加数＝人口ウェイト）で数える＝行を分けただけで増えない（2026-08-28「行の数でなく中身」）。
        //       個数単位への換算は既存の錨（初期の 個数/質量＝NationalScore.Wealth・SelfPlay の植民地防衛と同じ）＝抽象標的を含む世界の全植民地の初期の総量は旧の行の数と同じ目盛り
        //       （実在国の植民地は平均より重い＝実在国の合計は旧の約1.19倍）・資源の無い前哨は小さく・大植民地は大きく。
        const int foodG = Catalog.CannedFoodId;   // 缶詰糧食（駐留の携行食）。燃料（石炭/原油）は ChooseFuelGood が選ぶ
        // 海路距離：欧州→印/太平洋の補給線はアフリカ（喜望峰）を回る＝大圏直線の約2倍。スエズ(1869)/パナマ(1914)開通で短絡（史実の通商革命が維持費に効く）。
        bool suezOpen = w.Diplomacy?.ChokeOpenFor(Diplomacy.SuezId, w.SelfPower) ?? false;   // 2026-07-28：生文字列リテラルを廃止（表示名を変えると静かに false 固定＝維持費が永久にケープ迂回になる罠だった）
        bool panamaOpen = w.Diplomacy?.ChokeOpenFor(Diplomacy.PanamaId, w.SelfPower) ?? false;
        double totalUpkeep = 0, coalWant = 0, foodWant = 0, shipBurden = 0, quinineWant = 0;
        double coalPerMassDist = c.ColonyUpkeepCoalPerDist * w.ShipSteamShareEma * c.EraFactor(w.WorldTechLevel, c.EraColonyCoal);   // c1：帆走（割合0）の補給線は石炭を焚かない
        double foodPerMassDist = c.ColonyUpkeepFoodPerDist * EconomyStep.EquipUnlock(r, c, foodG);                                    // c1：缶詰の未発見の間は缶詰を食わない
        double countPerMass = w.Diplomacy is { } dipM && dipM.InitialColonyMass > 1e-9 ? dipM.InitialColonyCount / dipM.InitialColonyMass : 1.0;   // R2：質量→個数単位の錨
        // ★植民地の州昇格（2026-08-26・ColoniesAreRegions）：昇格した植民地は ColonyList に居ないので、
        //   **同じ式**を植民地格の州にも当てる＝帝国の過伸展のコストが昇格で消えない。
        //   ⚠単位を揃える：人口ウェイトは col.Population と同じ物差しへ戻す（PopSize = Population × ColonialPopScale の逆）。
        //   ⚠二重計上は起きない＝昇格の瞬間に ColonyList から Remove されるので、どちらか一方にしか居ない。
        //   座標未設定(0,0)の抽象植民地は距離項を中立に＝幻の遠距離コストを防ぐ（A-2#5。例：仏首都→(0,0)＝ギニア湾沖の幻の5400km）。
        void AccumUpkeep(double clat, double clon, double popWeight, double militancy, bool malarial)
        {
            double km = (Math.Abs(clat) < 1e-9 && Math.Abs(clon) < 1e-9) ? 0 : Geo.SeaDistanceKm(w.Latitude, w.Longitude, clat, clon, suezOpen, panamaOpen);
            double massDist = km / Diplomacy.DistanceNormKm * Math.Max(0, popWeight) * countPerMass;   // R2：距離×質量（個数単位）
            // 俸給（金銭）＝不満の上乗せだけ（c2）。人口ウェイト×不満（B-1：高不満の大植民地ほど駐留・鎮圧が高くつく＝インド大反乱型）。
            totalUpkeep += c.ColonyUpkeepMilitancyScale * popWeight * militancy;
            coalWant += coalPerMassDist * massDist;         // 距離×質量に比例の石炭（汽船補給線）
            foodWant += foodPerMassDist * massDist;         // 距離×質量に比例の缶詰糧食（駐留）
            shipBurden += c.ColonyShipOccupyPerDist * massDist;   // 距離×質量に比例の海運占有
            if (malarial) quinineWant += c.MalariaQuininePerColony;   // マラリア地は風土病鎮静にキニーネを要する（供給が実コスト）
        }
        if (c.ColoniesAreRegions > 0)
            foreach (var cr in w.Regions)
            {
                if (cr.State != RegionState.Active || cr.Tier != RegionTier.Colonial) continue;
                var ccr = Places.Coord(cr.Name);
                double pw = c.ColonialPopScale > 1e-9 ? cr.SumPopulation() / c.ColonialPopScale : 0;   // col.Population と同じ物差しへ戻す
                double mn = 0, md = 0;
                foreach (var cp2 in cr.Pops) { mn += cp2.Militancy * cp2.Size; md += cp2.Size; }
                AccumUpkeep(ccr.Lat, ccr.Lon, pw, md > 1e-9 ? mn / md : 0, cr.Malarial);
            }
        foreach (var col in w.SelfPower!.ColonyList) AccumUpkeep(col.Latitude, col.Longitude, col.Population, col.Militancy, col.Malarial);
        w.ColonialShipBurden = shipBurden;   // 交易の実効海運を絞る（InternationalMarket/Trade が ShipCapacity/(1+これ) で参照）
        // ★段4d（2026-09-30・分子と分母を同じ規則で）：俸給の請求は受け手（兵士の兵力の生産者）のいる分だけ＝連合維持費（Fiscal）と同じ1本の述語。
        //   旧＝兵士がいなくても請求に入った＝払えない（国庫に残る）請求で翌月の国家の支払い率 r が下がった。
        if (!InternationalMarket.HasSellers(w, Catalog.MilitaryPowerId)) totalUpkeep = 0;
        w.LastColonialUpkeep = totalUpkeep;   // 請求（r を掛ける前・2026-09-28 から＝旧は実支払い）＝翌月の月初の請求の見込み（Simulation）と予算表が読む。下の補給財の請求も BuyUpkeepGood が足す
        if (totalUpkeep > 1e-9)
        {
            double paid = totalUpkeep * w.FiscalFundingRatio;   // 国家の支払い率 r の分だけ（2026-09-28 段2-1＝旧は国庫の残りで切る門）
            // ★支払いの一元化 段3-2（2026-09-30・作者裁定 Q8 (a)・C7）：駐留と鎮圧の俸給＝国として兵士の兵力を買う代金を、兵力の生産者の④の持分（兵士の賃金・Earned を通す）で。
            //   旧＝本国の全POP へ頭数で配った（名目は駐留の俸給なのに受け手は全国民）。兵士がいなければ請求も立たない（上の段4d）。
            InternationalMarket.PaySales((w, null), null, Catalog.MilitaryPowerId, paid, w, TreasuryCat.ColonialUpkeep);
        }
        // 距離維持費の物理消費：国庫が本国市場から燃料・缶詰糧食を買う（在庫律速・需要計上で価格律速・産業と競合）。支払いは生産者へ（保存）。
        var (fuelG, fuelWant) = ChooseFuelGood(r, c, coalWant);   // 石炭/原油の二択（海軍・民間汽船と同じ1本）
        BuyUpkeepGood(w, r, fuelG, fuelWant);
        BuyUpkeepGood(w, r, foodG, foodWant);
        const int quinineG = Catalog.QuinineId;   // キニーネ（マラリア鎮静）
        double malariaFill = quinineWant > 1e-9 ? BuyUpkeepGood(w, r, quinineG, quinineWant) / quinineWant : 1.0;   // 本国市場から買えた割合＝鎮静度（キニーネ供給がコスト＝工業化＋キナ植民地が要る＝中後期）

        // 1. 投資：輸送船(汽船85)＋鉄道(線路56)を植民地へ送り開発度を積む。
        //    私的投資家＝「限界資源価値 ＞ 投資コスト」の間だけ自腹で（飽和＝逓減で過剰投資しない）。
        const int ship = Catalog.SteamshipId, rail = Catalog.RailId;
        if (ship < r.Markets.Length && rail < r.Markets.Length && r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count && r.ColonialResources.Length > 0)
        {
            double shipP = Math.Max(r.Markets[ship].Price, 1e-9), railP = Math.Max(r.Markets[rail].Price, 1e-9);
            double devCost = c.ColonialShipPerInfra * shipP + c.ColonialRailPerInfra * railP;   // 開発度1あたりの財コスト
            double half = c.ColonialOutputHalf;
            double dOdD = half / ((w.ColonialInfra + half) * (w.ColonialInfra + half));         // 限界産出（飽和曲線の傾き＝逓減）
            double resourceValue = 0;                                                           // 固有資源の価値＝需要×価格（希少なほど高い）
            foreach (int g in r.ColonialResources)
                if (g >= 0 && g < r.Markets.Length) resourceValue += r.Markets[g].LastDemand * Math.Max(r.Markets[g].Price, 1e-9);
            double marginalValue = dOdD * resourceValue * c.ColonialReturnScale;                // 開発度を1上げて増える資源の価値

            bool economic = marginalValue > devCost;
            if (economic)   // 採算が合う間だけ＝市場任せ
            {
                var inv = r.Pops[r.InvestorPopIndex];
                double budget = c.ColonialInvestFrac * Math.Max(0, inv.Savings);
                double affordableDev = devCost > 1e-9 ? budget / devCost : 0;
                double byStock = Math.Min(r.Markets[ship].Stock / Math.Max(1e-9, c.ColonialShipPerInfra),
                                          r.Markets[rail].Stock / Math.Max(1e-9, c.ColonialRailPerInfra));
                double dev = Math.Max(0, Math.Min(c.ColonialDevStep, Math.Min(affordableDev, byStock)));
                if (dev > 0)
                {
                    double shipUse = Math.Min(dev * c.ColonialShipPerInfra, r.Markets[ship].Stock);   // 丸めで在庫を超えない（非負保証）
                    double railUse = Math.Min(dev * c.ColonialRailPerInfra, r.Markets[rail].Stock);
                    r.Markets[ship].Stock -= shipUse; r.Markets[ship].LastExport += shipUse;   // 植民地へ輸出（在庫減＝財収支は Export に計上）
                    r.Markets[rail].Stock -= railUse; r.Markets[rail].LastExport += railUse;
                    // ★支払いの一元化 段3-2（2026-09-30・C6）：私的投資家が自腹で、汽船と線路の代金をその財の生産者の④の持分へ（輸送財の買い付けと同じ1本＝InternationalMarket.PaySales）。
                    //   旧＝本国の全POP へ頭数で配った（市場の在庫を取るのに、その財を作った者でなく全国民が代金を受け取った）。
                    InternationalMarket.PaySales((w, [inv]), r, ship, shipUse * shipP, w);
                    InternationalMarket.PaySales((w, [inv]), r, rail, railUse * railP, w);
                    if (!w.WarmupFrozen) w.ColonialInfra += dev;   // ★助走：汽船・線路は買って払うが**開発度は上げない**（能力の凍結・本国の建設と同じ扱い）
                }
            }
        }

        // 2. 植民地POP：住民が実体を持つ（本国の利益団体には属さない＝台帳の外）。
        //    人口はゆっくり成長し、識字は開発（インフラ）との接触で上がり、不満は**搾取×識字**で育つ
        //    ＝開発するほど従順になるのではなく、教育された植民地ほど独立を求める（史実の弁証法）。
        double outputRate = OutputCurve(w.ColonialInfra, c.ColonialOutputHalf);
        bool perColony = c.ColonialInfraPerColony > 0.5;   // R13：植民地ごとの開発度を使うか（既定1.0＝点火2026-07-15・0で国家一律）
        double popSum = 0, perColonyOutput = 0;
        foreach (var col in w.SelfPower!.ColonyList)
        {
            double exploitation = 1.0 - c.ColonialPayShare;   // 採取の上がりのうち現地に落ちない割合＝搾取
            double malariaDeath = col.Malarial ? c.ColonialExploitMortality * (1.0 - malariaFill) : 0;   // マラリア地は既存の植民地月次死亡率で、キニーネ未供給の分だけ蝕まれる（マラリア専用倍率を置かない）
            // 開始前の助走（w.WarmupFrozen・2026-08-10）は現地人口・識字・不満を進めない＝どれも「年月が経って積み上がる」量。
            // 産出（PopSize比例）と資源供給は下でそのまま算出される＝助走で落ち着かせたい経済側は止まらない。
            if (!w.WarmupFrozen)
            {
                // 搾取と不満が現地人口を蝕む（B-4・ベンガル飢饉型の負ループ）：搾取×(0.5+不満)の死亡が成長を上回ると人口が減り、産出（PopSize比例）も落ちる。
                col.PopSize *= Math.Max(0.5, 1 + c.ColonialPopGrowth - c.ColonialExploitMortality * exploitation * (0.5 + col.Militancy) - malariaDeath);
                col.Literacy = Math.Min(ColonyLiteracyCap, col.Literacy + c.ColonialLiteracyGrowth * outputRate);
                // 恐慌の脱植民地化（旧ColonialCrisisMilitancy）はC裁定2026-07-09で撤去＝脆弱な挑戦者の植民地をより奪い覇権の相対優位を拡大する逆効果を実証済み
                col.Militancy = PoliticalQuantities.Clamp01(col.Militancy + c.ColonialMilitancyGrowth * exploitation * (ColonyMilitancyLiteracyBase + col.Literacy) - c.ColonialMilitancyDecay);
            }
            popSum += col.PopSize + col.SettlerSize;   // 入植者も「数えられる」人口（州昇格2026-08-09。既定 SettlerSize=0＝厳密恒等）
            if (perColony)
            {
                // R13：植民地ごとの開発度。**非対称収束**＝成長期(col.Infra<所有国水準)は所有国水準を即採用（遅延バイアスなし＝長期保有の植民地は現行と同産出）／所有国より発展した植民地(弱国が奪取・独立時＝col.Infra>所有国水準)のみ緩やかに減衰＝開発を長く保持。col.Infraは征服/独立でオブジェクトごと移動済み＝奪った開発済み植民地は高産出を引き継ぐ。
                if (col.Infra < w.ColonialInfra) col.Infra = w.ColonialInfra;
                else col.Infra += (w.ColonialInfra - col.Infra) * c.ColonialInfraConverge;
                perColonyOutput += (col.PopSize + col.SettlerSize) * OutputCurve(col.Infra, c.ColonialOutputHalf);
            }
        }
        w.ColonialPop = perColony ? perColonyOutput : popSum * outputRate;   // 国力・スコア互換の集計（開発が進むほど「数えられる」人口）。R13点火時は植民地ごとの開発度で合算

        // 2b. 入植（州昇格の前段・2026-08-09・作者裁定2026-08-08）：本国の大衆（農民・労働者）が植民地へ移住する。
        //     物理＝渡航費（海路距離の減衰）とマラリア（キニーネ鎮静率＝「白人の墓場」はキニーネが届くまで入植が立たない＝アフリカ分割の物質的前提）。
        //     保存則＝送出popから Size/Savings を物理移転し PopulationDelta/CumulativeTradeFlow で台帳を閉じる（越境移民 MigrateBetween と同型・湧かない）。
        //     ColonySettlerRate=0 でブロックごと不走行（既定0.00015＝点火）。決定論＝乱数不使用・列挙順は Regions/ColonyList のリスト順。
        //     ★助走中（w.WarmupFrozen・2026-08-10）は走らせない＝本国から植民地への移住も「年月をかけて起きる」こと
        //       （暦を止めた10年の助走で回すと、開始盤面の本国人口が入植で目減りする＝1783年の人口でなくなる）。
        if (c.ColonySettlerRate > 0 && !w.WarmupFrozen && w.SelfPower!.ColonyList.Count > 0)
        {
            // 農奴は移動の自由がない（移動証明・人格的隷属）＝農奴解放の段階が農民の送出を握る（None=0／Limited=0.5／Full=1）。
            // 都市労働者は賃労働＝常に自由。露/普の入植が農奴解放（1861年型＝ゲーム内の法）後に始まる史実の順序が**法から**出る（国名条件なし）。
            double serfFree = 1.0 - Privileges.SeigneurialDegree(w);   // 移動の自由＝1−領主権の残高（なし0／限定0.5／全面1・物差しは Privileges の1本）
            var srcPops = new List<(Pop P, double Wgt)>(); double srcMass = 0;
            foreach (var rg in w.Regions)
            {
                if (rg.State != RegionState.Active) continue;
                foreach (var p in rg.Pops)
                {
                    if (p.IsSoldier || p.Size <= 1e-9) continue;
                    double wgt = p.ClassId == PopClass.Peasant ? p.Size * serfFree : p.ClassId == PopClass.Worker ? p.Size : 0;
                    if (wgt > 1e-9) { srcPops.Add((p, wgt)); srcMass += wgt; }
                }
            }
            foreach (var col in w.SelfPower.ColonyList)
            {
                // 距離減衰＝渡航費の物理（維持費・独立holdと同じ海路距離を使う＝新係数を作らない）。座標未設定(0,0)の抽象植民地は距離項を中立に（AccumUpkeep と同じ幻距離回避）。
                double kmS = (Math.Abs(col.Latitude) < 1e-9 && Math.Abs(col.Longitude) < 1e-9) ? 0 : Geo.SeaDistanceKm(w.Latitude, w.Longitude, col.Latitude, col.Longitude, suezOpen, panamaOpen);
                double attract = 1.0 / (1.0 + kmS / Diplomacy.DistanceNormKm);
                if (col.Malarial) attract *= malariaFill;   // マラリア地はキニーネの鎮静率のぶんだけしか入植が立たない
                double flow = Math.Min(c.ColonySettlerRate * srcMass * attract, srcMass * SettlerMonthlyOutflowCap);
                if (srcMass > 1e-9 && flow > 1e-9)
                {
                    double moved = 0, carried = 0, litSum = 0;
                    foreach (var (p, wgt) in srcPops)
                    {
                        if (p.Size <= 1e-9) continue;
                        double f = Math.Min(flow * (wgt / srcMass), p.Size);   // 送出比率は自由度加重（同月内の後続植民地は近似＝Sizeで上限）
                        if (f <= 0) continue;
                        double sv = p.Savings * (f / p.Size);   // 持参金＝1人あたり貯蓄ぶん（越境移民と同型）
                        // ★国債の保有台帳（2026-09-21）：入植者の保有は**送り出しPOPに残す**（債務・結晶資産と同じ扱い）。植民地（Colony）はPOPでなく台帳を持てず、
                        //   入植先は同じ国の中＝発行国は変わらない＝国の保有の和は不変（送出元は農民・労働者＝買い手でなく、保有は転落で運ばれた分だけ）。
                        p.Size -= f; p.Savings -= sv;
                        moved += f; carried += sv; litSum += p.Literacy * f;
                    }
                    if (moved > 1e-12)
                    {
                        col.SettlerLiteracy = (col.SettlerLiteracy * col.SettlerSize + litSum) / (col.SettlerSize + moved);   // 識字を運搬（加重平均）
                        col.SettlerSize += moved; col.SettlerSavings += carried;
                        w.PopulationDelta -= moved;        // 人口台帳（植民地は台帳の外＝国外流出の作法。昇格時に同額が台帳へ戻る）
                        w.CumulativeTradeFlow -= carried;  // 持参金＝対外移転（基礎貨幣の台帳が閉じる）
                        srcMass -= moved;                  // 次の植民地は残量から引く（同月の合計が母数を超えない）
                    }
                }
                // 入植者の人口動態＝現地住民と同じ月次成長・マラリア死（搾取死は受けない＝搾取の主体側）。SettlerSize=0では書き込みごとスキップ＝恒等
                if (col.SettlerSize > 0)
                    col.SettlerSize *= Math.Max(0.5, 1 + c.ColonialPopGrowth - (col.Malarial ? c.ColonialExploitMortality * (1.0 - malariaFill) : 0));
            }
        }

        // C1（改修2026-06-28）：植民地の不満が本国政治へ還流＝**反帝世論**。植民地の流血・抑圧（インド大反乱/ボーア戦争）が本国大衆の闘争度を上げ、自由党/反帝運動の素地になる。
        // 植民地人口加重の平均不満 × 帝国規模（植民地数）で、本国の非特権大衆（農民・労働者・職人）の闘争度に薄く加算。多国・植民地保有国のみ＝較正中立。
        if (c.ColonialUnrestHomeWeight > 1e-9)
        {
            double colMilSum = 0, colPopSum = 0;
            foreach (var col in w.SelfPower!.ColonyList) { colMilSum += col.Militancy * Math.Max(0, col.PopSize); colPopSum += Math.Max(0, col.PopSize); }
            // ★AUD-09（監査2026-09-02 medium・2026-09-03投入）：⚠**分子と分母が別の台帳だった**。
            //   帝国規模の項（下の `colonies`）は `ColonyList.Count + ColonialRegions`＝昇格した植民地格の州も数えるのに、
            //   不満の加重平均はここで ColonyList しか見ない。⇒ 開幕一括昇格で ColonyList が空になった帝国は
            //   colPopSum≈0 でこのブロックごと素通り＝**反帝世論が厳密に0**（インド大反乱もボーア戦争も本国に一切響かない）。
            //   ⚠維持費（上の AccumUpkeep）は 2026-08-26 に両方を見るよう直されており、**ここだけ取り残されていた**
            //   ＝「型の統合が起こす静かな破損」の再発（旧型の読み手が残る）。
            //   ★単位：col.PopSize = Population×ColonialPopScale なので、州側は SumPopulation 相当（Σ Pops.Size）がそのまま同じ物差し。
            //   ⚠0で厳密恒等（休眠）。
            if (c.AntiImperialCoversRegions > 0)
                foreach (var cr in w.Regions)
                {
                    if (cr.State != RegionState.Active || cr.Tier != RegionTier.Colonial) continue;
                    double mn = 0, md = 0;
                    foreach (var cp in cr.Pops) { mn += cp.Militancy * cp.Size; md += cp.Size; }
                    if (md > 1e-9) { colMilSum += mn; colPopSum += md; }
                }
            MechanismProbe.Eval(MechanismProbe.Site.AntiImperialOpinion);   // ★D-34：試した回数（⚠ここが0なら配線漏れ・Hitが0なら死んだ機構）
            if (colPopSum > 1e-9)
            {
                double homePressure = c.ColonialUnrestHomeWeight * (colMilSum / colPopSum) * Math.Min(1.0, colonies / Math.Max(1.0, c.ColoniesForFullColonial));
                if (homePressure > 1e-9) MechanismProbe.Hit(MechanismProbe.Site.AntiImperialOpinion);
                if (homePressure > 1e-9)
                    foreach (var pop in w.AllPops()) if (Politics.IsMassPop(pop)) pop.Militancy = Math.Min(1, pop.Militancy + homePressure);
            }
        }

        // 3. 捕囚市場（関税なし）：植民地POPが**自分の蓄え（搾取の分け前）**で本国の完成財を買う＝金が本国へ還流。
        //    財布の実体ができたので、買える量は植民地の貯えに律速される（貧しい植民地は買えない＝搾取しすぎは市場を殺す）。
        double spendBudget = 0;
        foreach (var col in w.SelfPower.ColonyList) spendBudget += col.Savings * c.ColonialSpendRate;
        double revenue = 0;
        if (spendBudget > 1e-9)
        {
            double perGood = spendBudget / Math.Max(1, c.ColonialMarketGoods.Length);
            foreach (int gid in c.ColonialMarketGoods)
            {
                if (gid >= r.Markets.Length) continue;
                var m = r.Markets[gid];
                double price = Math.Max(m.Price, 1e-9);
                double want = perGood / price;       // 植民地が買いたい量（価格が高いほど減る＝価格律速）
                m.LastDemand += want;                // 捕囚市場の需要を計上＝価格上昇＆本国の増産を駆動（在庫25%クランプの代替＝品薄は価格で律速）
                double got = Math.Min(want, m.Stock);   // 在庫超は買えない（物理制約のみ）
                if (got <= 1e-9) continue;
                m.Stock -= got; m.LastExport += got;
                // ★支払いの一元化 段3-2（2026-09-30・C6）：代金はその財の生産者の④の持分へ（払い手＝植民地の蓄え＝世界の外・流入は移し方の1本が台帳に書く）。
                //   旧＝本国の投資家1人へ全額（投資家不在なら全POP へ頭数）＋台帳を手で書いた。
                revenue += InternationalMarket.PaySales((null, null), r, gid, got * price, w);
            }
        }
        if (revenue > 1e-9)
        {
            // 植民地の蓄えから支払い（按分）＝受け取った額と同じ（外部マネーの流入の台帳は上の移し方の1本が書いた）。
            double bs = 0; foreach (var col in w.SelfPower.ColonyList) bs += col.Savings;
            if (bs > 1e-9) foreach (var col in w.SelfPower.ColonyList) col.Savings -= revenue * (col.Savings / bs);
        }

        // 4. 独立：圧力（人口×不満）が本国の抑え（軍）を超えた大植民地は離反する＝帝国は軍で保たれる。
        //    本国が戦争で疲弊（即応度低下）した隙に大植民地が独立する＝アメリカ独立戦争の構図。離反した植民地は戻らない。
        //    ★即応度は Power.Military に既に入っている（Diplomacy の戦力合成）＝ここでもう一度掛けない（2026-09-29 処理の一元化 段3 G・
        //    旧＝軍×max(0.2, 即応度)＝即応度の二乗・下限の定数 ColonyHoldReadinessFloor ごと削除）。
        if (w.SelfPower.ColonyList.Count > 0)
        {
            // 恐慌のgrip割引（旧DepressionColonyGrip）はC裁定2026-07-09で撤去（ColonialCrisisMilitancyとペアで逆効果を実証済み）
            double baseHold = w.SelfPower.Military * c.ColonialHoldScale;
            // 外5：同時多発の按分（ColonialHoldSharedArmy 既定0.5・0で各植民地が全軍と対峙）。>0で不穏植民地数（独立閾値超）に軍を割り、10植民地同時不穏なら各々に届く抑えが薄まる（戦争が戦線数で戦力を割るのと同型）。
            // 分母＝1+ColonialHoldSharedArmy×(不穏数−1)。0で分母1・1で不穏数ぶんに全軍を按分。
            int unrestCount = 0;
            foreach (var col in w.SelfPower.ColonyList)
                if (col.Militancy > c.ColonialIndependenceMilitancy) unrestCount++;
            double sharedDiv = 1.0 + c.ColonialHoldSharedArmy * Math.Max(0, unrestCount - 1);
            List<Colony>? freed = null;
            foreach (var col in w.SelfPower.ColonyList)
            {
                // 外5：距離減衰（ColonialHoldDistanceDecay 既定0.3・0で距離項なし）。>0で本国首都→植民地の海路距離で hold を割り引く（属国離反 Diplomacy.Projection と同型＝地球の裏側は隣接島より保ちにくい）。
                // 座標未設定(0,0)の抽象植民地は距離項を中立に（AccumUpkeep と同じ幻の遠距離コスト回避）。
                double km = (Math.Abs(col.Latitude) < 1e-9 && Math.Abs(col.Longitude) < 1e-9) ? 0 : Geo.SeaDistanceKm(w.Latitude, w.Longitude, col.Latitude, col.Longitude, suezOpen, panamaOpen);
                double distDecay = 1.0 / (1.0 + c.ColonialHoldDistanceDecay * km / Diplomacy.DistanceNormKm);
                double hold = baseHold * distDecay / sharedDiv;
                MechanismProbe.Eval(MechanismProbe.Site.ColonyIndependence);   // ★D-34：⚠Eval が小さいこと自体が所見＝昇格で ColonyList が痩せると独立判定に載る植民地が居なくなる
                if (col.Militancy > c.ColonialIndependenceMilitancy && col.PopSize * col.Militancy > hold)
                { (freed ??= []).Add(col); MechanismProbe.Hit(MechanismProbe.Site.ColonyIndependence); }
            }
            if (freed is not null)
                foreach (var col in freed)
                {
                    w.SelfPower.ColonyList.Remove(col);
                    w.LogAdd($"{w.Year} {col.Name}が{w.SelfPower.Name}から独立（人口{col.PopSize:F0}・不満{col.Militancy:F2}）");
                    col.Militancy = FreedColonyMilitancy;   // 独立達成＝不満は晴れる（以後は自由な供給者として静かに暮らす）
                    col.FreedYear = w.Year;   // 独立ショック（B-4）：独立直後は産出が崩壊し、数年かけて自由な供給者へ回復（ハイチ1804の砂糖供給崩壊型）
                    w.Diplomacy?.IndependentColonies.Add(col);   // 資源は世界から消えない＝自由貿易の供給者へ（旧宗主国は専有を失う）
                }
        }

        // 5. 州昇格（2026-08-09・作者裁定2026-08-08「植民地州昇格＝実装」・設計原典＝docs/202607/20260731_植民地のラベル化_部分完了.md §3-3d）：
        //    入植者が過半数（ColonyPromotionCoreShare・作者裁定2026-07-31）かつ総人口が北西部条例線（ColonyPromotionMinPop＝自由人口6万人）を
        //    超えた植民地は、本国の州（RegionTier.Integrated＝議席あり・植民地維持費なし）へ**自動**昇格する（米の州昇格・アルジェリア県化・北海道開拓の物理）。
        //    単調＝降格なし（作者裁定）・自動＝プレイヤー/AI対等（管理レバーなし）。決め手は「誰が住んでいるか」だけ＝経済価値では昇格しない
        //    （インドは最も価値ある植民地で、人口の門番により原理的に昇格しない）。ColonySettlerRate=0 では SettlerSize=0＝不発火。
        if (c.ColonySettlerRate > 0 && w.SelfPower.ColonyList.Count > 0)
        {
            List<Colony>? promote = null;
            foreach (var col in w.SelfPower.ColonyList)
            {
                double tot = Math.Max(0, col.PopSize) + col.SettlerSize;
                if (col.SettlerSize > 1e-9 && tot >= c.ColonyPromotionMinPop && col.SettlerSize / tot > c.ColonyPromotionCoreShare
                    && !w.Regions.Exists(r => r.Name == col.Name))   // 同名州が既に居る国（旧例：蝦夷地＝日本の州と抽象標的の二重在籍・2026-09-28 に抽象標的を削除＝既定のデータには無い）では昇格しない＝州名の一意性（座標表・地図・扼者解決のキー）を守る
                    (promote ??= []).Add(col);
            }
            if (promote is not null)
                foreach (var col in promote)
                {
                    w.SelfPower.ColonyList.Remove(col);
                    if (w.Diplomacy is { } pdip) pdip.PromotedColonies++;   // 保存検査の台帳（Validation「植民地の保存」）
                    Simulation.PromoteColonyToRegion(w, c, col);   // 州の実体化（pop/生産者/市場）＝保存則は中で PopulationDelta/CumulativeTradeFlow に記帳
                    w.LogAdd($"{w.Year} {col.Name}が{w.SelfPower.Name}の州へ昇格（入植者{col.SettlerSize:F0}・現地住民{Math.Max(0, col.PopSize):F0}）");
                }
        }

        // ★B1（実バグ21件・2026-09-07・単独計測）：ColoniesAreRegions=1 では ColonyList が Q1 から空＝上の入植（2b）も昇格（5）も一度も走らない
        //   （ColonySettlerRate/ColonyPromotionCoreShare/ColonyPromotionMinPop が読み手ゼロ・加/豪/南アの自治領化が131年間0件）。
        //   主体を「植民地格の州」へ移す（新しい状態を作らない）＝入植者比率は既存の AvgRegionCoreShare（宗主の中核民族の比率）、人口は州の pop。
        //   入植の流量＝本国の農民（農奴解放加重）/労働者 → 植民地格の州の同クラス pop（Territory.MoveBetweenRegions＝地域間移民と同じ運搬・国内移動なので台帳は不変）
        //   × 距離減衰 × マラリア鎮静率（上の 2b と同じ式・同じ係数）。昇格＝中核民族シェア ≥ ColonyPromotionCoreShare かつ 人口 ≥ ColonyPromotionMinPop → Integrated
        //   （議席あり・維持費なし・Core にはならない）。PromotedColonies++ で保存検査（植民地の保存）の台帳が閉じる。ColonialRegionSettlement=0＝旧＝厳密恒等。
        if (c.ColonialRegionSettlement > 0 && c.ColonySettlerRate > 0 && !w.WarmupFrozen)
        {
            double serfFreeR = 1.0 - Privileges.SeigneurialDegree(w);
            var srcR = new List<(Pop P, double Wgt)>(); double srcMassR = 0;
            foreach (var rg in w.Regions)
            {
                if (rg.State != RegionState.Active || rg.Tier == RegionTier.Colonial) continue;
                foreach (var p in rg.Pops)
                {
                    if (p.IsSoldier || p.Size <= 1e-9) continue;
                    double wgt = p.ClassId == PopClass.Peasant ? p.Size * serfFreeR : p.ClassId == PopClass.Worker ? p.Size : 0;
                    if (wgt > 1e-9) { srcR.Add((p, wgt)); srcMassR += wgt; }
                }
            }
            List<Region>? promoteR = null;
            foreach (var cr in w.Regions)
            {
                if (cr.State != RegionState.Active || cr.Tier != RegionTier.Colonial || cr.Id == 0) continue;
                var ccr = Places.Coord(cr.Name);
                double kmS = (Math.Abs(ccr.Lat) < 1e-9 && Math.Abs(ccr.Lon) < 1e-9) ? 0 : Geo.SeaDistanceKm(w.Latitude, w.Longitude, ccr.Lat, ccr.Lon, suezOpen, panamaOpen);
                double attract = 1.0 / (1.0 + kmS / Diplomacy.DistanceNormKm);
                if (cr.Malarial) attract *= malariaFill;
                double flow = Math.Min(c.ColonySettlerRate * srcMassR * attract, srcMassR * SettlerMonthlyOutflowCap);
                if (srcMassR > 1e-9 && flow > 1e-9)
                {
                    double moved = 0;
                    foreach (var (p, wgt) in srcR)
                    {
                        if (p.Size <= 1e-9) continue;
                        double f = Math.Min(flow * (wgt / srcMassR), p.Size);
                        if (!(f > 1e-12)) continue;
                        Pop? dp = null;
                        foreach (var d in cr.Pops) if (d.ClassId == p.ClassId) { dp = d; break; }
                        if (dp is null) continue;
                        Territory.MoveBetweenRegions(p, dp, f, c);
                        moved += f;
                    }
                    srcMassR -= moved;
                }
                if (Politics.AvgRegionCoreShare(cr) >= c.ColonyPromotionCoreShare && cr.SumPopulation() >= c.ColonyPromotionMinPop)
                    (promoteR ??= []).Add(cr);
            }
            if (promoteR is not null)
                foreach (var cr in promoteR)
                {
                    cr.Tier = RegionTier.Integrated;
                    if (w.Diplomacy is { } pdip) pdip.PromotedColonies++;   // 保存検査の台帳（植民地格の州が1つ減る）
                    w.LogAdd($"{w.Year} {cr.Name}が{w.SelfPower.Name}の州へ昇格（中核民族{Politics.AvgRegionCoreShare(cr):P0}・人口{cr.SumPopulation():F0}）");
                }
        }
    }

    // 補給線燃料の石炭→重油二択（B6・2026-07-19）：石炭 vs 原油の実効単価が安い方（原油はエネルギー密度で同熱量の必要量が1/比）。
    // 原油は DefaultRecipeOnset=0.6（1859）まで供給ゼロ＝転換時期は価格から創発（明示の技術ゲート不要）。ColonyFuelOilEnergyRatio=0で石炭固定。
    // 読み手＝Colonial.Step（植民地補給線）・海軍の運用燃料・民間汽船（運用コスト監査2026-08-16）＝式は1本。
    internal static (int GoodId, double Want) ChooseFuelGood(Region r, Coefficients c, double coalWant)
    {
        const int oilG = Catalog.CrudeOilId, coalG = Catalog.CoalId;
        double oilNeed = c.ColonyFuelOilEnergyRatio > 1e-9 ? coalWant / c.ColonyFuelOilEnergyRatio : 0;
        // 供給実在ガード：掘削解禁(1859)前は供給ゼロでも「需要なしの安い紙価格」が付くため、無ガードだと架空の安値で石油を選び
        // 何も買えず石炭購入まで止まる（石炭州への現金注入が消える・地域物価テスト2026-07-19）。必要量の1/4以上の継続生産がある時のみ二択に載せる。
        bool oilViable = c.ColonyFuelOilEnergyRatio > 1e-9 && oilG < r.Markets.Length
                      && r.Markets[oilG].LastProduced > OilViableProductionFrac * oilNeed;
        if (oilViable && coalG < r.Markets.Length
            && Math.Max(r.Markets[oilG].Price, 1e-9) / c.ColonyFuelOilEnergyRatio < Math.Max(r.Markets[coalG].Price, 1e-9))
            return (oilG, oilNeed);   // 石油炊き＝同熱量の必要量は1/比
        return (coalG, coalWant);
    }

    // 維持費の物理消費：国庫が本国市場から good を買い消費する（汽船の石炭・駐留の缶詰糧食）。需要計上で価格律速＝産業と奪い合う。代金は本国の生産者/経済へ＝保存。
    // 運用コスト監査2026-08-16：海軍の運用燃料（Military.NavalFuelStep）と共有＝internal 化＋台帳区分（cat）を引数化。既定は従来どおり ColonialUpkeep＝既存呼び出しはビット恒等。
    internal static double BuyUpkeepGood(World w, Region r, int goodId, double want, TreasuryCat cat = TreasuryCat.ColonialUpkeep)
    {
        if (!(want > 1e-9) || goodId >= r.Markets.Length) return 0;   // ⚠否定形＝NaN も弾く（LastDemand を汚さない）
        // ★国家調達の価格感応（2026-08-03・全ての政府支出で同じ式）：参照価格より高騰していれば買い控える。
        //   ⚠燃料・糧食を買い控えた分は維持費が浮く（国庫に残る）＝補給が細れば植民地の不満が育つ経路が既にある。
        if (w.Coef.StateProcurePriceGate > 0 && goodId < Catalog.RefPrices.Length && Catalog.RefPrices[goodId] > 1e-9)
        {
            double a = w.Coef.FactoryBuildAfford(Math.Max(r.Markets[goodId].Price, 1e-9), Catalog.RefPrices[goodId] * Math.Max(r.PriceLevel, 1e-9));   // ★実質で比べる（2026-08-04）
            if (a > 0 && a <= 1) want *= 1.0 - w.Coef.StateProcurePriceGate * (1.0 - a);
            if (!(want > 1e-9)) return 0;
        }
        // 植民地の補給財の請求（r を掛ける前）を植民地維持費の請求へ積む＝翌月の月初の国家の支払いの請求に入る（例外なし・作者裁定 Q2）。
        //   ⚠旧は国庫の残りで買える分だけ買った（月の最後の残りを取る＝暗黙の最下位の優先順位）＝英・西・蘭・葡・オマーンでは補給財の請求が歳入を超える（2026-09-28 canary）。
        if (cat == TreasuryCat.ColonialUpkeep) w.LastColonialUpkeep += want * Math.Max(r.Markets[goodId].Price, 1e-9);
        want *= w.FiscalFundingRatio;                                      // ★国家の支払い率 r の分だけ買う＝量を減らす（2026-09-28 段2-1＝旧は国庫で買える上限の門）
        if (!(want > 1e-9)) return 0;
        var m = r.Markets[goodId];
        m.LastDemand += want;                                              // 需要を計上＝価格上昇＆増産を駆動（在庫が無くても＝補給の引きが産業を圧迫）
        double price = Math.Max(m.Price, 1e-9);
        double got = Math.Min(want, m.Stock);                              // 在庫超は買えない（物理の律速・財政は上の r）
        if (got <= 1e-9) return 0;
        double cost = got * price;
        m.Stock -= got; m.LastExport += got;                              // 物理消費（汽船が焚く・駐留が食う＝本国市場から域外圏へ出る。財収支台帳に計上）
        // ★支払いの一元化 段3-2（2026-09-30・C6）：代金はその財の生産者の④の持分へ（国庫→地代・国営持分・賃金＝輸送財の買い付けと同じ1本 InternationalMarket.PaySales）。
        //   旧＝本国の全POP へ頭数で配った（補給の石炭・缶詰・キニーネ・侵攻の物資・海軍の燃料の代金を、作った者でなく全国民が受け取った）。
        InternationalMarket.PaySales((w, null), r, goodId, cost, w, cat);
        return got;                                                        // 買えた量（マラリア鎮静の充足率の算出に使う）
    }
}
