namespace PriceOfBread.SimCore;

/// <summary>
/// 植民地の維持費の側（§6.1 拡張）。植民地格の州の距離×質量から補給線の海運占有・燃料・キニーネを求め、反帝世論を本国の大衆の闘争度へ還す。
/// 輸出作物の大農園の不在地主（R1）の動詞もここ。（Colony オブジェクトの開発度・住民・独立・入植の経路は 2026-10-18 に削除＝試合で一度も回らなかった。）
/// </summary>
public static class Colonial
{
    internal const double OilViableProductionFrac = 0.25;       // 石油を燃料二択に載せる継続生産の下限（必要量比）。海軍運用燃料・民間汽船燃料（運用コスト監査2026-08-16）と共有

    /// <summary>開発度→産出率（0で0、飽和で1へ。初期低・逓減）。</summary>
    public static double OutputCurve(double dev, double half) => dev <= 0 ? 0 : dev / (dev + half);

    public static void Step(World w, Coefficients c)
    {
        int colonies = w.SelfPower?.Colonies ?? 0;
        var r = w.Region;   // 本国（首都地域）
        // 全喪失後に旧値が残らない（接1診断）。★回1-1（col-08）：海運の占有も0へ戻す（旧＝最後の月の占有が永久に残り、交易の海運を細らせた）。
        if (colonies <= 0 || r.ColonialResources is null) { w.LastColonialUpkeepPerMonth = new MoneyPerMonth(0); w.ColonialShipBurden = 0; return; }

        // 0. 維持費（帝国の過伸展）：不満は俸給（金銭）、距離は**物理**（補給線が占有する船腹＝交易の海運を細らせる＋占有された船腹の汽船の燃料）から創発。
        //    遠方植民地が多い国（イギリス）ほど交易船が補給に取られ、汽船の時代は燃料も要る＝一強の是正を物理コストで律する（固定倍率2.0を排除）。
        //    ★(177) 注記の一巡（反証役 vc1#8）：駐留の缶詰糧食と距離×質量の石炭は (173) の R4 で撤去（下の R4 の注記）＝この段の旧の記述（汽船の石炭＋駐留の缶詰糧食・石炭と糧食が逼迫）を直した。
        // ★2026-09-28（植民地の費用の側・c1・c2・R2）：
        //   c1＝補給線の石炭は民間の汽船と**同じ状態**（汽船の割合 ShipSteamShare＝帆走なら0）と**同じ物理**（機関の時代係数 EraColonyCoal＝容量あたりの燃料・Transport と共有）で焚く。
        //       旧＝汽船の発明の前から焚いた（英1783-1802 の植民地維持費の実払いの62%）。缶詰糧食は缶詰の財の解禁度（Discovery.GoodGate＝装備の解禁の唯一の読み口）で食った
        //       ＝アペール法の前の駐留は缶詰を食わない。旧の EraColonyFood（缶詰・冷蔵で距離あたりの糧食が下がる）はこの解禁度が置き換えた＝係数ごと削除。
        //       ⚠(173) R4：駐留の缶詰糧食そのものを撤去（兵士 POP は給与で州の食料を買う＝二度食わせていた）・補給線の燃料は占有された船腹の汽船の分（下の R4 の注記）。
        //   c2＝平場（旧 ColonyUpkeepBase＝1行150/月）を撤去＝同じ俸給は行政の購買（植民地格の州の官僚の給与×人数×首都からの距離の割増＝Fiscal.AdminDistanceMul・2026-09-30 (135) に
        //       旧の行政費の人頭割り AdminCostPerPop から移した＝Fiscal.AdminDistanceMul）が払っている。
        //   ★(140)（2026-09-30・col-16）：不満の上乗せ（旧 ColonyUpkeepMilitancyScale＝30×人口ウェイト×不満の月の俸給）も撤去＝兵士の兵力の生産者へ払っていた＝
        //       植民地格の州に駐留する兵は兵力の購買（兵数×民間の賃金）で既に払われている＝同じ兵への2度目の支払い（兵は1人も増えない・思想⑨）。
        //       駐留と鎮圧の費用は兵力の購買の1本（兵数×賃金）。不穏が兵数を増やす経路（理想の軍隊の形の NeedInternal）は兵数の目標に入っていない＝申し送り。
        //   R2＝距離の費用と海運占有は行（州）の数でなく**質量**（ColonyMassUnits の加数＝人口ウェイト）で数える＝行を分けただけで増えない（2026-08-28「行の数でなく中身」）。
        //       個数単位への換算は既存の錨（初期の 個数/質量＝SelfPlay の植民地防衛と同じ・旧の NationalScore.Wealth は (456) で消えた）＝抽象標的を含む世界の全植民地の初期の総量は旧の行の数と同じ目盛り
        //       （実在国の植民地は平均より重い＝実在国の合計は旧の約1.19倍）・資源の無い前哨は小さく・大植民地は大きく。
        // ★R4（作者裁定 2026-09-29 の4「史実はもちろん、ゲーム的に植民地国家が汽船の発見を急ぐ必要がある程度には経済的なラインに」・2026-10-02 (173)・形は AI の推奨＝作者の承認は未確認）：
        //   ①缶詰糧食（旧 ColonyUpkeepFoodPerDist＝180×距離×質量）を撤去＝植民地格の州に駐留する兵は兵士 POP で、給与（兵力の請求）で州の市場の食料を買って食べている
        //     ＝同じ兵を2度食わせていた（思想⑨）。(173) の計器＝r172 の5種で英の植民地の補給財（ほぼ缶詰）が 1803-12 に国の税の 4.6倍・西 6.3倍・蘭 7.4倍
        //     （段2A で缶詰の解禁が2値になり、解禁の年から満額＝旧の解禁度の連続の立ち上がりが消えた）。
        //   ②海運占有は汽船の割合の分だけ軽い＝占有×((1−汽船の割合)＋汽船の割合÷汽船の容量倍)。容量倍＝民間の船の選び方と同じ1本（Transport.BuyToward＝ShipTier2CapMult×
        //     首都の汽船の解禁度÷機関の時代係数＝汽船1単位は帆船の 2.5〜5 単位を運ぶ＝速さと大きさ）＝「船の占有・所要日数・隻数を減らす」。容量倍が1以下（未解禁）は帆船と同じ。
        //   ③補給線の石炭＝占有された船腹の汽船の分の燃料（民間の汽船と同じ ShipFuelRate×時代係数×容量×汽船の割合・占有された容量＝海運の容量×占有÷(1＋占有)）
        //     ＝「汽船は補給の石炭を足す」。旧の ColonyUpkeepCoalPerDist（400×距離×質量）は民間の船隊の燃料の数百倍の別の物差しだった＝係数ごと削除。
        //     民間の燃料（Transport.BuyShipFuel）は交易に使える容量（÷(1＋占有)）の分だけ＝同じ船腹を2度焚かない（思想⑨）。
        // 海路距離：欧州→印/太平洋の補給線はアフリカ（喜望峰）を回る＝大圏直線の約2倍。スエズ(1869)/パナマ(1914)開通で短絡（史実の通商革命が維持費に効く）。
        bool suezOpen = w.Diplomacy?.ChokeOpenFor(Diplomacy.SuezId, w.SelfPower) ?? false;   // 2026-07-28：生文字列リテラルを廃止（表示名を変えると静かに false 固定＝維持費が永久にケープ迂回になる罠だった）
        bool panamaOpen = w.Diplomacy?.ChokeOpenFor(Diplomacy.PanamaId, w.SelfPower) ?? false;
        double shipBurden = 0, quinineWant = 0;
        double colUnitPop = w.Diplomacy is { } dipM && dipM.ColonyUnitPop > 1e-9 ? dipM.ColonyUnitPop : 1.0;   // 質量の単位（千人）＝初期の植民地の平均の住民数（旧＝質量→個数の錨 InitialColonyMass ÷ 係数 ColonialPopScale）
        // ★植民地の州昇格（2026-08-26・旧の旗 ColoniesAreRegions は 2026-10-18 に焼き込み）：昇格した植民地は ColonyList に居ないので、
        //   **同じ式**を植民地格の州にも当てる＝帝国の過伸展のコストが昇格で消えない。
        //   ⚠単位を揃える：人口ウェイトは col.Population と同じ物差しへ戻す（PopSize = Population × ColonyUnitPop の逆）。
        //   ⚠二重計上は起きない＝昇格の瞬間に ColonyList から Remove されるので、どちらか一方にしか居ない。
        //   座標未設定(0,0)の抽象植民地は距離項を中立に＝幻の遠距離コストを防ぐ（A-2#5。例：仏首都→(0,0)＝ギニア湾沖の幻の5400km）。
        void AccumUpkeep(double clat, double clon, double popWeight, bool malarial)
        {
            double km = (Math.Abs(clat) < 1e-9 && Math.Abs(clon) < 1e-9) ? 0 : Geo.SeaDistanceKm(w.Latitude, w.Longitude, clat, clon, suezOpen, panamaOpen);
            double massDist = km / Diplomacy.DistanceNormKm * Math.Max(0, popWeight);   // R2：距離×質量（個数単位＝質量の単位が平均の植民地1つ）
            shipBurden += c.ColonyShipOccupyPerDist * massDist;   // 距離×質量に比例の海運占有（帆船の船腹の目盛り＝下で汽船の分を軽くする）
            if (malarial) quinineWant += c.MalariaQuininePerColony;   // マラリア地は風土病鎮静にキニーネを要する（供給が実コスト）
        }
        foreach (var cr in w.Regions)
        {
            if (cr.Tier != RegionTier.Colonial) continue;
            var ccr = Places.Coord(cr.Name);
            double pw = cr.SumPopulation() / colUnitPop;   // col.Population と同じ物差しへ戻す
            AccumUpkeep(ccr.Lat, ccr.Lon, pw, cr.Malarial);
        }
        double era = c.EraFactor(w.WorldTechLevel, c.EraColonyCoal);   // 機関の時代係数（民間の汽船と同じ1本＝Transport.Step）
        double steam = w.ShipSteamShare.Value;
        double steamMult = c.ShipTier2CapMult * Discovery.GoodGate(r, c.ShipTier2GoodId) / era;   // R4②：汽船1単位の容量倍（Transport.BuyToward と同じ式・era≥0.05＝EraFactor の床で有限）
        if (steamMult > 1.0) shipBurden *= (1.0 - steam) + steam / steamMult;                      // 汽船の分だけ占有が軽い（未解禁・容量倍≦1 は帆船と同じ）
        w.ColonialShipBurden = shipBurden;   // 交易の実効海運を絞る（InternationalMarket/Trade が ShipCapacity/(1+これ) で参照）
        w.LastColonialUpkeepPerMonth = new MoneyPerMonth(0);   // 請求（r を掛ける前）＝下の補給財の請求（BuyUpkeepGood が足す）だけ＝翌月の月初の請求の見込み（Simulation）と予算表が読む。★(140) 不満の俸給は撤去（上の注記）
        // 補給線の燃料（R4③）：占有された船腹（海運の容量×占有÷(1＋占有)）の汽船の分が焚く＝民間の汽船の燃料と同じ式（Transport.BuyShipFuel）。国庫が本国市場から買う
        //   （在庫律速・需要計上で価格律速・産業と競合）。支払いは生産者へ（保存）。帆走（割合0）は焚かない（c1 と同じ）。
        double coalWant = c.ShipFuelRate * era * steam * OccupiedShipCapacity(w);
        var (fuelG, fuelWant) = ChooseFuelGood(r, c, coalWant);   // 石炭/原油の二択（海軍・民間汽船と同じ1本）
        BuyUpkeepGood(w, r, fuelG, fuelWant);
        const int quinineG = Catalog.QuinineId;   // キニーネ（マラリア鎮静）
        if (quinineWant > 1e-9) BuyUpkeepGood(w, r, quinineG, quinineWant);   // 本国市場から買えた割合＝鎮静度（キニーネ供給がコスト＝工業化＋キナ植民地が要る＝中後期）

        // C1（改修2026-06-28）：植民地の不満が本国政治へ還流＝**反帝世論**。植民地の流血・抑圧（インド大反乱/ボーア戦争）が本国大衆の闘争度を上げ、自由党/反帝運動の素地になる。
        // 植民地人口加重の平均不満 × 帝国規模（植民地数）で、本国の非特権大衆（農民・労働者・職人）の闘争度に薄く加算。多国・植民地保有国のみ＝較正中立。
        if (c.ColonialUnrestHomeWeight > 1e-9)
        {
            double colMilSum = 0, colPopSum = 0;
            // ★AUD-09（2026-09-03）：不満の加重平均は昇格した植民地格の州の闘争度で取る（帝国規模の項 `colonies` が数えるのと同じ集合＝分子と分母を揃える）。単位は州の人口 Σ Pops.Size。
            foreach (var cr in w.Regions)
                {
                    if (cr.Tier != RegionTier.Colonial) continue;
                    var (mn, md) = Politics.RegionMilitancy(cr);   // 州の闘争度の和の1本（平民の民間人・段4e H）
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

        // 【墓標2026-10-18・恒等の削除】植民地の旧い型（Colony オブジェクト＝国の ColonyList）の経路一式を消した：開発度への投資（ColonialInfra・ColonialPop）・住民の成長と不満・独立（IndependentColonies）・
        //   植民地格の州への入植と昇格（ColonialRegionSettlement ほか）。61 カ国の ColonyList は開幕の PromoteAllColonies で空になり、受け取りの1本（Diplomacy.AcquireColony）も国を持つ受け手には州で渡す＝試合では一度も回らなかった。
        //   ColonyPromotionMinPop は Politics の分離の圧力が読む＝残す。
    }

    /// <summary>★R4（2026-10-02 (173)）：植民地の補給線が占有している船腹＝海運の容量×占有÷(1＋占有)（交易に使える容量＝容量÷(1＋占有)の残り）。
    /// 補給線の燃料（<see cref="Step"/>）と民間の汽船の燃料（<see cref="Transport"/> の BuyShipFuel＝容量−これ）が同じ1本を読む＝同じ船腹を2度焚かない。</summary>
    internal static double OccupiedShipCapacity(World w)
    {
        double b = w.ColonialShipBurden;
        return b > 0 ? w.ShipCapacity * b / (1.0 + b) : 0.0;   // 否定形でなく正の確かめ＝NaN と負は占有0
    }

    /// <summary>★R1（作者裁定 2026-09-28「R1 不在地主＝本国首都の投資家POP」・2026-09-29「輸出作物の生産は不在地主（本国の投資家）・主食の生産は現地の地主のまま」
    /// 「判別は既存のデータ（州の生産が輸出作物か主食か）」・メモリの要約＝原文は手元に無い）＝植民地格の州の**輸出作物の大農園**か。
    /// 判別（AI の読み・作者の承認は未確認）＝州の賦存（<see cref="Region.Resources"/>＝その州の輸出向け一次産品＝本国の大農園の地代の上書き〔Simulation の D6〕と同じ定義）に載る RGO で、
    /// 主食でない（主食＝穀物法の客体と同じ集合＝主食の段か麦の連鎖）。⚠鉱山・畜産・果実・漁獲・木・毛皮も賦存なら「輸出作物」の側（D6 の「大農園/鉱山の経営」と同じ）。
    /// 状態を持たない＝州の格が植民地格を出れば（入植者の州昇格）その場で在地の持ち主へ戻る。</summary>
    internal static bool AbsenteeRgo(Region r, Producer s)
        => r.Tier == RegionTier.Colonial && s.IsRgo && ExportCrop(s.GoodId) && Array.IndexOf(r.Resources, s.GoodId) >= 0;

    /// <summary>★R1：主食でない財か（主食＝穀物法の客体と同じ集合＝主食の段か麦の連鎖＝BilateralMarket の穀物法の罰）。植民地格の州の賦存と合わせて「輸出作物」の判別に使う。</summary>
    internal static bool ExportCrop(int goodId)
        => goodId >= 0 && goodId < Catalog.Goods.Length && !(Catalog.Goods[goodId].Tier == GoodTier.Staple || Catalog.Goods[goodId].GrainChain);

    /// <summary>★R1：不在地主＝本国の首都の投資家 POP（国の首都の州の <see cref="Region.InvestorPopIndex"/>）。首都そのものの州・投資家の居ない国は null（在地の持ち主のまま）。</summary>
    internal static Pop? AbsenteeOwner(World w, Region r)
    {
        var cap = w.Region;
        if (ReferenceEquals(cap, r) || cap.InvestorPopIndex < 0 || cap.InvestorPopIndex >= cap.Pops.Count) return null;
        return cap.Pops[cap.InvestorPopIndex];
    }

    /// <summary>★R1：植民地格の州の月末の精算の後に、輸出作物の大農園の地代（<paramref name="net"/>＝在地の持ち主の口座ごとの符号つきの和・EconomyStep の④が積む）を
    /// 不在地主へ送る（移し方の1本＝在地の口座→首都の投資家・所得にも書く）。国内の売上の地代は④で在地の持ち主の月内の仮勘定に入り、州の月末の精算で貯蓄になる
    /// ＝首都の州はもう精算を終えている（州は順に回る）ので、④で直接首都の投資家の仮勘定へ書くと貯蓄に入らず消える＝精算の後に移す。
    /// 所得と地代の記録（<see cref="Pop.EarnedPerMonth"/>・<see cref="Pop.LandRentPerMonth"/>＝課税・票・思想の基数）も在地の口座から不在地主へ移す（金は移し方の1本が動かした額だけ）。
    /// 赤字（負の地代）は不在地主が負う＝逆向きに移す。輸出代金の地代は清算（<see cref="InternationalMarket.PaySales"/>）で直接不在地主へ払う（精算の後の受け取り＝同じ規則）。
    /// 使った <paramref name="net"/> の要素は0へ戻す。</summary>
    internal static void RemitAbsenteeRent(World w, Region r, double[] net)
    {
        var owner = AbsenteeOwner(w, r);
        for (int k = 0; k < r.Pops.Count && k < net.Length; k++)
        {
            double a = net[k]; net[k] = 0;
            if (owner is null || !(Math.Abs(a) > 0)) continue;
            var local = r.Pops[k];
            // 黒字＝在地の口座→不在地主・赤字＝不在地主→在地の口座（在地の赤字を不在地主が埋める）。受け手は移し方の1本が所得にも書き、払い手の所得の記録は同じ額だけ下げる
            //   ＝所得の記録がお金と一緒に動く（黒字なら在地の所得から地代が抜け不在地主へ・赤字なら在地の損の記録が消え不在地主の損になる）。
            var (payer, payee) = a > 0 ? (local, owner) : (owner, local);
            double moved = World.Transfer((w, [payer]), (w, [payee], [1.0]), Math.Abs(a), income: true);   // 受け手の重み1＝頭数に依らない（在地の資本家の口座は人数0のことがある）
            payer.EarnedPerMonth -= new MoneyPerMonth(moved);
            if (a > 0)
            {
                double land = Math.Min(moved, Math.Max(0, local.LandRentPerMonth.Value));   // 地代（土地税の課税ベース）の記録も移す
                local.LandRentPerMonth -= new MoneyPerMonth(land); owner.LandRentPerMonth += new MoneyPerMonth(land);
            }
        }
    }

    // 補給線燃料の石炭→重油二択（B6・2026-07-19）：石炭 vs 原油の実効単価が安い方（原油はエネルギー密度で同熱量の必要量が1/比）。
    // 原油は掘削の機械が要る（原油のレシピの機械の投入 OilMachineryInput）＝転換時期は価格から創発（明示の技術ゲート不要）。ColonyFuelOilEnergyRatio=0で石炭固定。
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

    // 維持費の物理消費：国庫が本国市場から good を買い消費する（補給線の燃料〔占有された船腹の汽船の分・石炭か原油〕・マラリア地のキニーネ・(177) 注記の一巡＝駐留の缶詰糧食は (173) R4 で撤去）。需要計上で価格律速＝産業と奪い合う。代金は本国の生産者/経済へ＝保存。
    // 運用コスト監査2026-08-16：海軍の運用燃料（Military.NavalFuelStep）と共有＝internal 化＋台帳区分（cat）を引数化。既定は従来どおり ColonialUpkeep＝既存呼び出しはビット恒等。
    internal static double BuyUpkeepGood(World w, Region r, int goodId, double want, TreasuryCat cat = TreasuryCat.ColonialUpkeep)
    {
        if (!(want > 1e-9) || goodId >= r.Markets.Length) return 0;   // ⚠否定形＝NaN も弾く（LastDemand を汚さない）
        // ★国家調達の価格感応（2026-08-03・全ての政府支出で同じ式）：参照価格より高騰していれば買い控える＝価格の門の1本（EconomyStep.StatePriceAfford・段4e K）。
        //   ⚠燃料・糧食を買い控えた分は維持費が浮く（国庫に残る）＝補給が細れば植民地の不満が育つ経路が既にある。
        want *= EconomyStep.StatePriceAfford(r, w.Coef, goodId);
        if (!(want > 1e-9)) return 0;
        // 植民地の補給財の請求（r を掛ける前）を植民地維持費の請求へ積む＝翌月の月初の国家の支払いの請求に入る（例外なし・作者裁定 Q2）。
        //   ⚠旧は国庫の残りで買える分だけ買った（月の最後の残りを取る＝暗黙の最下位の優先順位）＝英・西・蘭・葡・オマーンでは補給財の請求が歳入を超える（2026-09-28 canary）。
        if (cat == TreasuryCat.ColonialUpkeep) w.LastColonialUpkeepPerMonth += new MoneyPerMonth(want * Math.Max(r.Markets[goodId].Price, 1e-9));
        want *= w.FiscalFundingRatio.Value;                                      // ★国家の支払い率 r の分だけ買う＝量を減らす（2026-09-28 段2-1＝旧は国庫で買える上限の門）
        if (!(want > 1e-9)) return 0;
        // 買い付けは市場の外の買い付けの1本（InternationalMarket.Procure・段4e K）＝需要の計上・在庫の律速・域外へ（LastExport）・
        //   代金はその財の生産者の④の持分へ（段3-2・C6＝旧は本国の全POP へ頭数で配った）。戻り値＝買えた量（マラリア鎮静の充足率の算出に使う）。
        return InternationalMarket.Procure(w, r, goodId, want, null, cat);
    }
}
