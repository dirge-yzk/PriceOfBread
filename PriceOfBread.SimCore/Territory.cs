namespace PriceOfBread.SimCore;

/// <summary>
/// 領土（§5.2／§A-8）。国家は最終版図を最初から保有し、地域は active か locked。
/// locked 地域はインフラ開発（資金）で active 化し、地域間移民が流入する（地図は増えずロックが開く）。
/// 移民は「引力＝土地機会（豊かさ×収容力の空き）− 摩擦＝輸送」で active 地域間を流れる（§2.2.1）。
/// ＝ロシア型の辺境開発（シベリア鉄道＋移民・§A-9）。pop の規模・貯蓄は保存移動。
/// </summary>
public static class Territory
{
    private const double MaxMigrationFraction = 0.2;      // 1ステップに1州が流出できる人口割合の上限
    internal const double OpportunityFactorFloor = 0.1;    // 機会の各乗算因子（賃金/不穏/住居）の下限クランプ

    public static void Step(World w, Coefficients c)
    {
        // 1. locked 地域の解錠（§A-8）：懐柔（開発）か強硬（武力）の択。
        foreach (var r in w.Regions)
            if (r.State == RegionState.Locked)
            {
                if (w.ForceUnlock)
                {
                    // 武力（強硬）：速いが軍需の出血・闘争度↑（§A-8／§6.1 兵力×経済）。
                    // ★支払いの一元化 段3-2（2026-09-30・作者裁定 Q8・C7）：制圧の費用は**軍需の購買**＝首都の市場から陸の軍需（基本段の財のうち艦隊の財〔FleetGoodIds〕を除く・
                    //   発見の解禁度を通した比＝国軍の基本段の調達と同じ比）を月の予算 ForceUnlockTreasuryCostPerMonth ぶん買って消費する（国家の調達の1本 Colonial.BuyUpkeepGood
                    //   ＝価格の門・国家の支払い率 r・在庫・代金は④の持分・区分＝陸軍〔作者裁定 Q14〕）。進み方と制圧の不満は買えた割合（代金÷予算）に比例＝軍需が無ければ制圧は進まない。
                    //   旧＝国庫から全POP へ頭数で配った（財を買わない国家支出）・進み方は r だけ。
                    var cap = w.Region;
                    double[] unlockRates = EconomyStep.DiscoveredRates(cap, c, c.MilitaryGoods, c.MilitaryRates);
                    double unlockRateSum = 0;
                    for (int b = 0; b < c.MilitaryGoods.Length && b < unlockRates.Length; b++)
                        if (c.MilitaryGoods[b] < cap.Markets.Length && System.Array.IndexOf(c.FleetGoodIds, c.MilitaryGoods[b]) < 0) unlockRateSum += unlockRates[b];
                    double unlockSpent = 0;
                    if (unlockRateSum > 1e-12)
                        for (int b = 0; b < c.MilitaryGoods.Length && b < unlockRates.Length; b++)
                        {
                            int g = c.MilitaryGoods[b];
                            if (g >= cap.Markets.Length || System.Array.IndexOf(c.FleetGoodIds, g) >= 0 || !(unlockRates[b] > 0)) continue;
                            double price = Math.Max(cap.Markets[g].Price, 1e-9);   // BuyUpkeepGood が払う単価と同じ
                            unlockSpent += Colonial.BuyUpkeepGood(w, cap, g, (c.ForceUnlockTreasuryCostPerMonth * (unlockRates[b] / unlockRateSum) / price).Value, TreasuryCat.Army) * price;
                        }
                    double unlockFill = c.ForceUnlockTreasuryCostPerMonth.Value > 0 ? PoliticalQuantities.Clamp01(unlockSpent / c.ForceUnlockTreasuryCostPerMonth.Value) : 0;
                    r.Infra += c.ForceUnlockRate * unlockFill;
                    foreach (var p in w.AllPops())
                        p.Militancy = PoliticalQuantities.Clamp01(p.Militancy + c.ForceUnlockMilitancy * unlockFill);   // 制圧の不満（戦争疲弊）＝制圧の進み方（買えた軍需）に比例
                }
                else
                {
                    // 資金注入（懐柔/開発）：遅いが軋轢小（§2.5/§2.2.1）。
                    // （旧＝分離独立で失った州 Seceded は対象外だった＝その経路〔単国の世界の単一州の分離〕は 2026-09-28 段2 束3 で削除＝Locked は辺境だけ）
                    r.Infra += c.FrontierDevelopmentRate;
                }

                if (r.Infra >= c.FrontierUnlockThreshold)
                {
                    r.State = RegionState.Active;
                    foreach (var m in r.Markets) m.PrevStock = m.Stock;   // 財収支の恒等式を満たす初期化（次月から市場が回る）
                }
            }

        // 2. active 地域間の移民（§2.2.1）。
        // ★2026-10-01 (141)：行き先は**機会の差の比で全ての州へ**配り、**距離で減らす**＝越境移民（MultiNationSim.MigrateBetweenNations）と同じ規則。
        //   出る州 s から行き先 d への差 g_sd＝(d の機会−s の残る値打ち−摩擦 TransportFriction)×距離の減衰（MigrationDecay＝越境と同じ1本）。
        //   s の流量の割合＝RegionMobility×最良の行き先の差（上限 MaxMigrationFraction＝流出の上限は1回だけ＝行き先の数に依らない）を g_sd の比で全ての行き先へ
        //   （越境の段3〜4 と同じ形）。受け入れる州ごとに上限で縮め（(125)・縮んだ分は出る州に残る）、決まった人数を移す（越境の段5 と同じ）。
        //   旧＝最大の機会の1州（と第2の州に MigrationSecondDestShare＝0.3）だけへ、距離に依らず流した＝上位2州の受け入れの上限が国の移民の総量を決め
        //   （(125) の後の国内の移民＝州の間の出÷州の人口 四半期 0.2‰）、征服された遠い州からも大陸を越えて首都圏へ流れた（(137) の 4e-2b の移民の1/10 の1件＝バルナウル→プーナ）。
        var active = new List<Region>();
        foreach (var r in w.Regions) if (r.State == RegionState.Active) active.Add(r);
        int k = active.Count;
        if (k < 2) return;

        // 月初の盤面で全て決める（出る州・行き先の順に依らない）：機会・残る値打ち・座標・行き先に居る階級（兵士を除く＝MoveRegion が移せる相手＝PopOf と同じ述語）。
        // ★非有限の遮断（2026-07-31・潜在NaNの封じ込め）：機会が評価できない州（NaN/∞）は行き先にも出る州にもしない＝fail-closed
        //   （旧＝先頭州の NaN が最大値として居座り、全ての出る州の差が NaN になって国内全州の Size/Savings が一括で NaN 化した）。有限値だけの世界では何も変えない。
        var opp = new double[k]; var stay = new double[k]; var lat = new double[k]; var lon = new double[k]; var classes = new int[k];
        for (int i = 0; i < k; i++)
        {
            var r = active[i];
            opp[i] = OppChecked(r, c);
            stay[i] = opp[i] * Holding(r, c);   // ★(125)：出る側は「残る値打ち」＝機会×残る人の土地の持ち分（人が抜けるほど増え、流出が止まる＝州の空洞化の根の2つ目）
            (lat[i], lon[i]) = Places.Coord(r.Name);
            foreach (var p in r.Pops) if (!p.IsSoldier) classes[i] |= 1 << p.ClassId;
        }
        var plan = new List<(int S, int D, double F)>();
        var inflow = new double[k];
        var gap = new double[k];
        var size0 = new double[k][];   // 出る州の POP の月初の規模（移す人数＝月初の規模×流量の割合）
        long seenPairs = 0;   // 計算量の台帳（診断）：出る州×行き先の組（(146)＝O(州²)・旧は数えず単価に入っていた）
        for (int s = 0; s < k; s++)
        {
            if (!double.IsFinite(stay[s])) continue;
            seenPairs += k;
            double gMax = 0, wSum = 0;
            for (int d = 0; d < k; d++)
            {
                gap[d] = 0;
                if (d == s || !double.IsFinite(opp[d])) continue;
                double g = opp[d] - stay[s] - c.TransportFriction;
                if (!(g > 0)) continue;   // ★否定形＝NaN も弾く。差が0以下の州へは1人も行かない
                g *= MigrationDecay(Geo.DistanceKm(lat[s], lon[s], lat[d], lon[d]), w.WorldTechLevel, c);
                if (!(g > 0)) continue;
                gap[d] = g; wSum += g;
                if (g > gMax) gMax = g;
            }
            if (!(wSum > 0)) continue;
            double fraction = Math.Clamp(c.RegionMobility * gMax, 0, MaxMigrationFraction);
            size0[s] = new double[active[s].Pops.Count];
            for (int i = 0; i < size0[s].Length; i++) size0[s][i] = active[s].Pops[i].Size;
            for (int d = 0; d < k; d++)
            {
                if (!(gap[d] > 0)) continue;
                double f = fraction * (gap[d] / wSum);
                plan.Add((s, d, f));
                inflow[d] += f * Movable(active[s], classes[d]);
            }
        }
        w.Cost[CostLedger.MigrationPairs] += seenPairs;
        // ★(125)：受け入れる州ごとに上限で縮める（縮んだ分は出る州に残る＝越境移民の段4 と同じ式）
        var scale = new double[k];
        for (int d = 0; d < k; d++) if (inflow[d] > 0) scale[d] = ReceiveScale(active[d], inflow[d], c);
        // 移す＝決まった人数（月初の規模×割合）。旧＝今の規模に比を掛けた（受け取った後の州から計画より多く出た・第2の行き先は残りの比で割り戻した〔監査G10#2〕）
        //   ＝月初の規模で切れば、出る州と行き先の順に依らず、受け入れの上限を計画の人数で守る。
        foreach (var (s, d, f) in plan)
        {
            double g = f * scale[d];
            if (g > 0) MoveRegion(active[s], active[d], g, size0[s]!, c);
        }
    }

    /// <summary>★(141)：移民の距離の減衰＝越境移民（MultiNationSim.MigrateBetweenNations）と地域間移民（<see cref="Step"/>）の同じ1本。
    /// 1/(1＋距離km÷(MigrationDistanceScale×EraFactor(世界の技術, EraMigrationScale)))＝時代とともに遠くへ行きやすくなる（大西洋横断が6週間→6日）。
    /// 差（行き先の機会−出る側の残る値打ち−摩擦）に掛ける＝遠い行き先ほど選ばれにくく、流れが細い（止まる位置＝差の符号は変えない）。MigrationDistanceScale≦0 は1（距離に無感応）。
    /// 旧＝越境だけが国の座標の間で読み（MultiNationSim の中の式）、国内の移民は距離を読まなかった（棚卸し pop-24「同じ移動で規則が2つ」）。</summary>
    internal static double MigrationDecay(double km, double worldTech, Coefficients c)
        => c.MigrationDistanceScale > 0 ? 1.0 / (1.0 + km / (c.MigrationDistanceScale * c.EraFactor(worldTech, c.EraMigrationScale))) : 1.0;

    /// <summary>★(125)：src から行き先へ動ける人数＝閉鎖階級と兵士を除き、行き先に同じ階級（兵士を除く POP＝<paramref name="dstClasses"/> のビット）が居る POP の規模の和
    /// ＝<see cref="MoveRegion"/> が実際に動かす母集団（★(141)：行き先の階級は PopOf と同じ述語＝旧は兵士の POP も「居る」に数えた）。</summary>
    private static double Movable(Region src, int dstClasses)
    {
        double m = 0;
        foreach (var sp in src.Pops)
            if (sp.IsCommoner && (dstClasses >> sp.ClassId & 1) != 0) m += sp.Size;
        return m;
    }

    /// <summary>★(125)：受け入れの上限で流量を縮める比＝越境移民と同じ式（上限＝ImmigrationReceiveCapPerQuarter〔四半期〕÷3×受け入れる州の人口×<see cref="AbsorbCap"/>・
    /// 計画が上限以下なら1）。上限が NaN なら0の側へ倒す（fail-closed＝MultiNationSim の段4と同じ）。</summary>
    private static double ReceiveScale(Region dst, double planned, Coefficients c)
    {
        double cap = (Calendar.PerQuarterToPerMonth(c.ImmigrationReceiveCapPerQuarter) * dst.SumPopulation() * AbsorbCap(dst, c)).Value;
        return planned <= cap ? 1.0 : cap > 0 ? cap / planned : 0.0;
    }

    // 土地機会 ＝ 生産性 × 土地係数(landFactor) ×(1−失業率)。容量(room)の壁ではなく、土地という固定生産要素の収穫逓減で混雑を表す：
    //  ・農村/世界の技術水準＝農村人口が耕地賦存量を超えると landFactor↓＝1人あたり産出↓＝機会↓＝移民が自然に止まる（自給農は失業に出ないので、土地の逓減が唯一の飽和力）。
    //  ・都市＝農村人口が少なく landFactor≈1＝土地に縛られず、職(1−失業)だけが律速＝工業が伸びる限り無上限に育つ。
    // 空の世界の技術水準は農村人口≈0で landFactor=1＋高い生産性で初期入植を呼ぶ（鶏卵回避）。生産関数(EconomyStep)と同一の係数を使う＝機会と実産出が一致。
    /// <summary>Opportunity の非有限検査つきラッパ（診断のみ・値は素通し）。既定OFF＝実行ゼロ。
    /// ⚠非有限を「無かったこと」にしないための窓＝どの州のどのスカラーが壊れたかを stderr へ出す（leakhunt と同じ流儀）。</summary>
    private static double OppChecked(Region r, Coefficients c)
    {
        double o = Opportunity(r, c);
        if (Simulation.ConservationProbe && !double.IsFinite(o))
            Console.Error.WriteLine($"[非有限診断] Territory.Opportunity {r.Name}={o}　pop={r.SumPopulation()} 賃金P={r.WagePremium} 工業={r.TotalIndustrialization} 住居={r.HousingFill} 犯罪={r.CrimeRate} 耕地={r.ArableLand} 生産性={r.ProductivityBonus}");
        return o;
    }

    internal static double Opportunity(Region r, Coefficients c)
    {
        double pop = r.SumPopulation();
        double land = LandFactor(r, r.SumRuralPopulation(), c);
        // ★2026-09-30 (125)：工業の引力（旧＝×(1＋0.5×工業化度)・2026-06-28）を外した＝州の空洞化の根。工業化度は州に立っている資本の**総量**
        //   （人が集まる州ほど建設が積もる）で、移民1人から見た機会（1人あたりの量）ではない＝人が来るほど資本が積もり、さらに人を引く正帰還
        //   （計器・種3＝マルセイユ 1783→1793 で人口 5,203→15,088 千人・工業化度 0.06→3.7、パリ 7,920→1,703→1820年に101 千人＝出る州の方が1人あたり所得が高い〔パリの賃金のプレミアム 1.07→2.69〕）。
        //   工業が1人に与える機会は、賃金（WagePremium＝1人あたり所得の対国平均比）と雇用（1−失業）が既に運ぶ＝二重計上（思想⑨）。工業化度の廃止（残タスクの7）と同じ向き。
        // 労働価格プレミアム（キュー3・金発見移民）：高賃金州（金鉱・ブーム州）ほど機会が大きく移民を引く＝ゴールドラッシュを賃金経由で創発。
        // WagePremium は UpdateRegionalPriceLevels が四半期更新（[0.25,4]クランプ済み）。OpportunityWageWeight 既定0.2（点火）・0で wagePull=1＝旧と厳密一致。
        double wagePull = Math.Max(OpportunityFactorFloor, 1.0 + c.OpportunityWageWeight * (r.WagePremium - 1.0));
        if (!(pop >= 1.0)) return r.ProductivityBonus * wagePull * land;   // ★否定形＝pop が NaN のとき除算枝（unemp/pop）へ落ちない。有限値では `pop < 1.0` と厳密に同値
        // ★(161)（(157) の反証役の指摘 va#8）：兵士は失業者に数えない＝兵士の〈失業〉は給与の未払いの割合（Simulation が書く・脱走と志願の空きだけが読む）で、民間の職の無さではない。
        //   分母は州の人口のまま（兵士は職に就いている人として数える＝貴族・聖職と同じ）。
        double unemp = 0; foreach (var p in r.Pops) if (!p.IsSoldier) unemp += p.Size * p.UnemploymentRate.Value;
        // プッシュ要因（B-4）：地域の不満（迫害・治安悪化）が機会を下げ、その地域からの流出を促す（ポグロム・抑圧からの逃避）。
        //   ★2026-09-30 段4e H：不穏＝州の平均闘争度の1本（平民の民間人＝駐留の兵と閉鎖階級を除く）＝旧は全 POP の和÷全人口（兵と貴族の闘争度が州の不穏に混ざった）。
        double unrest = Politics.AvgRegionMilitancy(r);
        // 接16（監査2026-07-11・2026-07-25休眠配線）：州別犯罪率も押し出し要因へ（「治安悪化」の実配線＝従来の闘争度経由の間接経路は
        // 機会ペナルティ約0.3%とノイズ級だった）。CrimePushWeight 既定6.0（点火2026-07-27）・0で不読＝旧と厳密一致。
        double pushPenalty = 1.0 - c.PushUnrestWeight * unrest - c.CrimePushWeight * r.CrimeRate;
        // 都市住宅（housing-urban-rent-absent）：住居充足が低い(過密)州は移民の引力が下がる＝過密で流入が自然飽和（無上限吸引の解消）。
        // HousingMigrationFriction 既定0.5（点火2026-07-19）・0（または HousingFill=1.0＝住居財なし/需要0）で摩擦=1.0＝旧と厳密一致。
        double housingFriction = 1.0 - c.HousingMigrationFriction * (1.0 - r.HousingFill);
        return r.ProductivityBonus * wagePull * land * (1.0 - unemp / pop) * Math.Max(OpportunityFactorFloor, pushPenalty) * Math.Max(OpportunityFactorFloor, housingFriction);
    }

    // 土地（耕地）の収穫逓減係数。農村人口が耕地賦存量(ArableLand)以下なら1（影響なし）、超えると逓減。EconomyStep の土地作物RGOと Opportunity が共有。
    // ＝上限1（ブースト無し）なので、初期（農村人口≤耕地×余裕）では全シナリオで landFactor=1＝既存の収支・較正を厳密保持。逓減は混雑時のみ。
    internal static double LandFactor(Region r, double ruralLabor, Coefficients c)
    {
        if (r.ArableLand >= 1e8) return 1.0;   // 未設定＝制約なし（合成較正・最小テスト世界）
        double ratio = r.ArableLand / Math.Max(1.0, ruralLabor);
        return Math.Min(1.0, Math.Pow(ratio, c.LandDiminishExponent));
    }

    /// <summary>★2026-09-30 (125)：残る人の土地の持ち分（州の空洞化の根の2つ目）＝出ていく人が置いていく土地の値打ち（その土地に固有の資本）。
    /// 生産の土地係数（<see cref="LandFactor"/>＝min(1,(耕地÷農村人口)^指数)）が上限1で切った分＝(耕地÷農村人口)^指数 の1を超えた分
    /// ＝max(1, 耕地÷農村人口)^LandDiminishExponent（1人あたりの耕地が1以下＝土地が混む州は1＝土地係数の逓減がそのまま効く）。
    /// ⚠ 生産の土地係数は1人あたりの耕地が1を超えると1で平ら（労働が律速＝開幕は余裕1.8で平らな帯の中）＝人が抜けても残る人の産出も所得も上がらず、
    ///   機会の差が続く限り州は空になるまで流れ出た（計器＝(125) の工業の引力を外した腕の5種で移民による空洞化の214州のうち212州は窓の始めと終わりで土地係数1〔耕地÷農村の中央値 1.9→20.8〕）。
    ///   上限は生産の話（1人が耕せる広さ）＝残る人の持ち分は上限で切らない（思想⑧）＝人が抜けるほど残る人の持ち分が増え、出ていけば失う土地が増える
    ///   ＝土地係数の逓減（混む側）と切れ目なく続く収穫逓減の出る側＝流出が止まる。
    ///   ⚠ 探索の腕で「入植の時の持ち分（1.8）を超えた分だけ」を試した＝耕地÷農村 1〜1.8 に土地係数も持ち分も動かない平らな帯が残り、
    ///   育った後に痩せる州（種9 のロンドン 1825→1926）が帯の中で止まらずに流れ出た＝採らない。
    /// ⚠ 出る側（残る値打ち）だけに掛ける＝入る側の機会（<see cref="Opportunity"/>）には掛けない：入る人は土地を持って来ない（開墾し、買う）＝
    ///   耕地が桁違いの辺境（米西部 740・シベリア 238）の引力は今までどおり（掛けると辺境が全国を一瞬で吸う）。辺境の住民は出にくい（持ち分が大きい）。
    /// 状態も係数も増やさない（耕地・農村人口・土地の逓減の指数の既存の値だけ）。耕地の未設定（合成較正・最小テスト世界）は1。</summary>
    internal static double Holding(Region r, Coefficients c)
    {
        if (r.ArableLand >= 1e8) return 1.0;   // 未設定＝制約なし（LandFactor と同じ）
        double ratio = r.ArableLand / Math.Max(1.0, r.SumRuralPopulation());   // LandFactor と同じ入力
        return Math.Pow(Math.Max(1.0, ratio), c.LandDiminishExponent);
    }

    /// <summary>★受入能力（2026-08-12・作者裁定③「移民が殺到→移民求心力が落ちる→適切な水準に落ち着く」）。
    ///
    /// 旧＝受入上限 cap = 0.012 × 自国人口 が binding で、しかも**率**なので人口が増えれば上限も同率で増える＝正帰還。
    /// 引力(pull)側には混雑項（土地逓減・失業・住居・不穏）が4本あるのに、cap に張り付いた国では inflow が pull に
    /// 依存しないので**混雑がいくら進んでも流入が1人も減らない**＝信号線が上限で切断されていた。
    /// 結果、ネーデルラントは10年で人口2.14倍（年8.3%）・アメリカ2.39倍（年9.6%）と指数で発散し、有限の定常点が存在しない。
    ///
    /// 新＝**binding な制約そのもの**に既存の混雑量を通す。cap ∝ pop × (食料充足×住居充足×土地逓減)^e となり、
    /// 耕地は移民で増えない既存ストックなので、指数 e が十分大きいと1人あたり流入が人口の減少関数になり**水準で止まる**。
    /// 止まる位置を決めるのは新しい数ではなく、既に較正されている耕地・住宅の供給力・食料生産である。
    /// ★回1-2（2026-09-27）：絞られた分は送出州に残る（旧＝世界プールの余剰再配分で他国へ回した。プールは削除）。
    ///
    /// 既定 <c>ImmigrationAbsorbExponent = 2.0</c>＝受入能力が有効。0 なら 1.0 を返す＝**厳密恒等**（掛け算が1回増えるだけで値は不変）。
    /// ★2026-09-30 (125)：国家間移民（MultiNationSim.MigrateBetweenNations）と地域間移民（<see cref="Step"/>）の受け入れの上限が同じこの1本を読む（旧＝MultiNationSim の private）。</summary>
    internal static double AbsorbCap(Region? r, Coefficients c)
    {
        if (r is null || !(c.ImmigrationAbsorbExponent > 0)) return 1.0;
        double slack = PoliticalQuantities.Clamp01(r.FoodFill) * PoliticalQuantities.Clamp01(r.HousingFill) * LandFactor(r, r.SumRuralPopulation(), c);
        if (!(slack > 0)) return 0;   // 否定形＝NaN もここで落ちる（有限値では厳密同値）
        return Math.Pow(slack, c.ImmigrationAbsorbExponent);
    }

    // src の各クラスから月初の規模×fraction を dst の同クラスへ移す（規模・貯蓄を保存移動）。★(141)：月初の規模 size0（src.Pops と同じ並び）に掛ける＝決まった人数
    private static void MoveRegion(Region src, Region dst, double fraction, double[] size0, Coefficients c)
    {
        for (int i = 0; i < src.Pops.Count; i++)
        {
            var sp = src.Pops[i];
            // 人4：地域間移民は経済移民＝閉鎖階級（貴族・聖職）・兵士を動かさない（PopDecision:20 の明示除外・越境の !IsSoldier と対称化）。
            // 貴族が毎月世界の技術水準へ経済移民する非対称の解消（★2026-09-26：旗 RegionMigrationExcludesClosed（#17点火）を焼き込み・x-01）。
            // FrontierDevelopment 較正世界は農民pop単体（Closed/IsSoldier なし）＝TerritoryTests 中立。
            if (sp.Closed || sp.IsSoldier || sp.IsCriminal) continue;   // ★(366) 犯罪者は経済移民の比較（賃金）が当てはまらない＝州の間を動かない（犯罪者の POP が無い世界では偽＝恒等）
            double moveSize = size0[i] * fraction;
            if (!(moveSize > 1e-9)) continue;   // ★否定形＝NaN も弾く（旧 `moveSize <= 1e-9` は NaN で false ＝ Size/Savings を直接 NaN 化していた）。有限値では厳密同値
            if (dst.PopOf(sp.ClassId) is not { } dp) continue;   // 目的地に同クラスが無ければ移さない（最小核）
            MoveBetweenRegions(sp, dp, moveSize, c);
            if (Instruments.On) Instruments.Migrated(src, dst, moveSize);   // 計器（読むだけ）
        }
    }
    /// <summary>★B1（2026-09-07）：1つの pop から同クラスの別州 pop へ moveSize を運ぶ（規模・貯蓄・債務・国債の保有・年齢と男女の構成・識字・闘争度・政治意識・国民意識・思想・民族）。
    /// MoveRegion（地域間移民）と Colonial.Step の入植（本国→植民地格の州）と植民地の同名州の吸収が**同じ1本**を呼ぶ。
    /// ★2026-09-30 処理の一元化 段4e I：本体は人の移し方の1本 <see cref="Pop.Blend"/>（旧＝ここの写しは女性比を子供込みの規模で重み付け、識字を旗を見ずに運んだ）。
    ///   州の間の移動は国の中＝発行国は同じ＝国債の保有も1人あたりで運ぶ。</summary>
    internal static void MoveBetweenRegions(Pop sp, Pop dp, double moveSize, Coefficients c) => Pop.Blend(sp, dp, moveSize);

    /// <summary>州が持ち主を替えるときの種類（<see cref="Rehome"/> の引数）。</summary>
    internal enum Rehoming
    {
        Ceded,      // 割譲（講和の割譲・植民地格の州の複数割譲）
        Seceded,    // 建国（開戦時の建国・分裂・分離独立）と、内戦の白紙講和で反乱国が親から取った州
        Returned,   // 再併合（反乱国の州が親へ戻る）
        Sovereign,  // 独立の確定（州は動かない＝新しい国の本土になる）
    }

    /// <summary>★移転の付け直し（回1-1・col-03／col-04・2026-09-26）＝割譲・建国・再併合・独立の確定が**必ず通る1本**。
    /// 州の格・中核民族の割合（独立の確定では支配度も）を「新しい持ち主から見た関係」で決め直す。
    /// ⚠旧＝割譲（Diplomacy.TransferRegion）・建国（MultiNationSim.SpawnNation）・再併合（ApplyReintegrations）の3か所が
    ///   市場のリセットだけを写し、格・支配度・民族は前の持ち主のまま運んでいた＝独立した旧植民地は首都を含む全州が植民地格のまま、
    ///   ポーランドの州は露へ移っても露の中核民族のままだった。
    /// ・割譲＝植民地格は植民地のまま・本国の州は編入（Integrated＝ゲーム中にコア化はしない）。**支配度は動かさない**（前の持ち主の下の値のまま）。
    ///   ⚠回1-1 の計測で「支配度0から積み上げる」を試した＝差し戻し：支配度0の州は翌四半期の積分で引き留め≦離脱圧力なら即座に独立の戦争へ入る
    ///   （割譲の翌四半期に分離独立＝10シードの建国 35→296・世界規模の補給崩壊 1→2/10）。割譲の支配度の初期値は回6-x（保有と由来から導く）で決める。
    ///   民族＝両国の主流文化（<see cref="Power.Culture"/>）が同じなら割合を保ち、違えば0（征服した土地の住民は征服者の中核民族ではない
    ///   ＝越境移民が受入国で core=0 になるのと同じ規約）。
    /// ・建国＝民族の割合を反転（旧少数派が新しい国の中核）。格と支配度は**独立が確定するまで動かさない**
    ///   （親が勝てば再併合で元の格のまま戻る＝失敗した植民地の蜂起で植民地が本土に化けない）。
    /// ・再併合＝建国の反転を戻す。★格は分離の時点の親の下の格（<see cref="Region.TierInParent"/>）へ戻す（2026-10-03 (183)・州の格の不変条件）＝
    ///   分断の固定（内戦の白紙）で独立が確定した残骸が親へ平和的に戻るとき、独立の確定が本土へ上書きした植民地の格を戻す（旧＝本土の格のまま戻った）。支配度は動かさない。
    /// ・独立の確定＝新しい国の本土（Core・支配度1＝保有開始年0の州と同じ初期値）。
    /// 市場の月次台帳のリセット（財収支の恒等式を保つ）と植民地財の空化は移転の3種で共通。⚠植民地財を null にしない（col-02）：
    ///   null は単一国テストの旧経路（EconomyStep の else 枝）＝需要まで植民地財11種を代金なしに湧かせ、首都になれば
    ///   Simulation の植民地資源の上書き（非 null の首都だけ）も効かず、植民地の代価を永久に払わない国になった。
    /// 状態も係数も増やさない（読むのは州の格・民族の割合と、呼び手が渡す主流文化の一致だけ）。</summary>
    internal static void Rehome(Region r, Rehoming how, bool sameCore = false)
    {
        if (how != Rehoming.Sovereign)
        {
            foreach (var m in r.Markets) { m.PrevStock = m.Stock; m.LastProduced = m.LastSold = m.LastImport = m.LastExport = m.LastShortage = 0; m.PopBought = null; }
            r.ColonialResources = [];   // World複製フィールドは新しい持ち主の次回 MonthSettle が上書き（首都になった州だけが植民地財の産出を担う）
        }
        switch (how)
        {
            case Rehoming.Ceded:
                if (r.Tier != RegionTier.Colonial) r.Tier = RegionTier.Integrated;
                if (!sameCore) foreach (var p in r.Pops) p.CoreEthnicShare = new Share(0.0);
                r.TierInParent = null;   // ★(183) 第三の国へ渡った＝分離した親へ再併合で戻る州ではない
                break;
            case Rehoming.Seceded:
                foreach (var p in r.Pops) p.CoreEthnicShare = new Share(1.0 - p.CoreEthnicShare.Value);
                r.TierInParent = r.Tier;   // ★(183) 分離の時点の親の下の格（再併合で戻す）
                r.AnnexBaggage = 0;   // 案Y：建国の州は新しい国の「生まれの人口」＝併合の荷物ではない（親の下で付いた旧記帳もここで清算）
                break;
            case Rehoming.Returned:
                foreach (var p in r.Pops) p.CoreEthnicShare = new Share(1.0 - p.CoreEthnicShare.Value);   // 建国時の反転を戻す（親の中核定義へ復帰）
                if (r.TierInParent is { } tp) r.Tier = tp;   // ★(183) 分離の時点の親の下の格へ（独立の確定で本土になった残骸の州も）
                r.TierInParent = null;
                break;
            case Rehoming.Sovereign:
                r.Tier = RegionTier.Core;
                r.Dominance = 1.0;
                break;
        }
    }

    /// <summary>★州の移し替えの1本（2026-09-28 処理の一元化 段2 B）＝割譲（講和の割譲・植民地格の州の複数割譲・反乱国の膨張）・
    /// 建国（新しい国の器は呼び手が州から組む＝<paramref name="to"/> が null）・再併合が必ず通る。種類の差は <see cref="Rehome"/> の引数だけ。
    /// ⚠旧＝割譲（Diplomacy.TransferRegion）・建国（MultiNationSim.SpawnNation）・再併合（ReabsorbNations）が州を外し・Id を振り・台帳を付ける手順を
    ///   それぞれ書いていた＝再併合だけ返還州の Id を「親の州の数」で振り、建国で親の州が抜けて Id が歯抜けのとき既存の州と同じ Id が付いた
    ///   （0..9 から 4 が抜けて数が9 → 返還州に 9）。Id で州を引く所と割譲先の同点決着が崩れる。
    /// 手順＝①保存量（同一値を両辺に使う）②外す＋送り手の台帳 ③受け手へ（<see cref="Adopt"/>＝Id は最大＋1・併合の荷物・国債の保有は送り手の対外債へ振替・受け手の台帳）
    ///   ④首都だったら残る州のうち最大GDPの州へ遷都（<see cref="World.SetCapital"/>）⑤付け直し（<see cref="Rehome"/>）。
    /// 講和の帰結（被征服の不満・失地の累計・年代記）は割譲の呼び手（Diplomacy.CedeRegion）だけが足す。状態も係数も増やさない。</summary>
    internal static (double Rail, double Ship) TransferRegion(World from, World? to, Region r, Rehoming how, bool sameCore = false)
    {
        bool wasCapital = r.Id == 0;
        // 保存量＝移転州の貯蓄総額・人口（同一値を両辺に使い浮動小数ズレ回避＝TotalMoney/TotalPopulation の実減と CumulativeTradeFlow/PopulationDelta の調整をビット一致させる）。
        // ★債務は債務者について行く（2026-09-29 S3b）＝州の POP の債務 D（輸入の為替手形）の分の家計の与信を受け手の銀行へ（建国＝新しい国の銀行は構築で POP の債務から始まる）。
        //   運ぶ基礎貨幣＝貯蓄−債務（同一値を両辺）＝両国の保存が閉じる。債務の無い州は D＝0＝旧と同じ。
        MoneyStock S = r.SumSavings(), D = default; double P = r.SumPopulation();
        foreach (var p in r.Pops) D += p.Debt;
        MoneyStock B = S - D;
        // ★輸送の容量は州と一緒に移る（2026-09-30 (118)・推奨で進めた＝保存）：鉄道と船は国の値（World.RailCapacity／ShipCapacity）で、
        //   清算はそれを州の取り分（Transport.RegionShare）で配る＝州が使っていた分は、その州の港と線路にある物。
        //   旧＝移し替えで容量は送り手に全部残った＝建国の新しい国は船腹0・鉄道0で生まれ（fff9f01 の10種＝生まれ≥1000 の新しい国の最初の8四半期は4分の3以上で輸出0）、
        //   送り手の残る州は同じ容量を少ない州で分けた（取り分の窓落ち）。新＝出ていく前の取り分（清算と同じ規則）で送り手から引き、受け手へ足す（建国は呼び手が新しい国へ）。
        //   残る州の容量は変わらない（潜在量比例＝旧の旗 TransportShareByPotential=1 の既定の形では、送り手の容量×(1−s) を残る州の潜在量で分け直す＝前と同じ）。活動していない州は取り分0。
        double sh = r.State == RegionState.Active ? Transport.RegionShare(Transport.ShareBasis(from, from.Coef), r) : 0.0;
        if (!(sh > 0)) sh = 0;   // 否定形＝NaN は移さない側へ（取り分は定義上 0..1）
        double rail = from.RailCapacity * sh, ship = from.ShipCapacity * sh;
        from.RailCapacity -= rail; from.ShipCapacity -= ship;
        from.Regions.Remove(r);
        from.CumulativeTradeFlow -= B; from.PopulationDelta -= P;
        // ★人と動くお金の換算（2026-09-30 処理の一元化 段4e＝(120) の残り）：州ごと別の国へ移る POP の貯蓄と債務を受け手の通貨へ（換算の1本 World.CrossFx・差は換算の台帳へ）。
        //   旧＝名目のまま＝割譲と再併合で、為替の違う国の州の貯蓄が同じ数字のまま別の通貨の貨幣になった（為替 0.5 の国から 1 の国へ移ると貯蓄の値打ちが2倍）。
        //   建国（to＝null）は新しい国の為替が親の値から始まる（MultiNationSim.SpawnNation）＝換算しない。受け手の台帳は換算後の和（同じ値を両辺に）。
        MoneyStock Din = D, Bin = B;
        if (to is not null && to.FxRate != from.FxRate)
        {
            MoneyStock Sin = default; Din = default;
            foreach (var p in r.Pops) { p.Rebase(from, to); Sin += p.Savings; Din += p.Debt; }
            Bin = Sin - Din;
        }
        if (Instruments.On) Instruments.RegionMoved(to, r);   // (262) 計器（読むだけ）：州ごと別の国へ移る POP の貯蓄（換算の後）を貯蓄の台帳へ
        from.Bank!.Carry(to?.Bank, D, Din);
        if (to is not null)
        {
            Adopt(to, r);
            // 案Y（人口物差しの脱汚染・2026-08-21）：併合の持ち込み人口を記帳＝受け手の PopGrowth（1783年比・領土不変な定義）の分子から除く。
            // 故地への復帰（1783年の所有国が取り返した＝OriginPowerId 一致）は0＝再征服・回復は帯へ戻る。**観測専用**＝力学は読まない。建国の付け直しは0へ戻す（下の Rehome）。
            r.AnnexBaggage = to.SelfPower is { } tp && tp.Id == r.OriginPowerId ? 0.0 : P;
            // ★国債の保有台帳（2026-09-21）：移る州のPOPが持つ送り手の国債は**送り手の対外債へ振替**＝受け手の対外債権になる（金は動かない）。
            //   ★史実：フランクフルト条約1871＝ドイツはフランス国債を一切継承せず、アルザス＝ロレーヌの住民が持つフランス国債はフランスへの請求権のまま残った。
            //   ⚠勝者の按分継承（ツューリヒ1859・ヴェルサイユ254条）は国家間の債務分担の取り決めで、持ち手の請求権の債務者は変わらない。
            //   帳消しにする形は、失地で送り手の債務が軽くなり持ち手だけが損をする（負けるほど財政が楽になる）。再併合は反乱国の国債を先に消している＝0 は素通り。
            foreach (var p in r.Pops) Fiscal.BookForeignDebt(p, p.BondHoldings, from, to);
            to.CumulativeTradeFlow += Bin; to.PopulationDelta += P;
            Transport.Receive(to, rail, ship, from.ShipSteamShare.Value);
        }
        if (wasCapital && NextCapital(from) is { } cap) from.SetCapital(cap);   // 首都を失った＝「首都を持つ」不変条件を維持
        // ★(183) 植民地の保存の台帳：植民地格へ戻る州は、植民地格を出たときの出口（独立の確定＝本土・植民地の昇格＝編入）から1つ戻す
        //   （Validation「植民地の保存」＝植民地格の州＋独立＋昇格＋吸収＝初期＝植民地格が1つ増えた分だけ出口を減らす）。
        bool backToColony = how == Rehoming.Returned && r.TierInParent == RegionTier.Colonial && r.Tier != RegionTier.Colonial && r.State == RegionState.Active;
        RegionTier tierLeft = r.Tier;
        Rehome(r, how, sameCore);
        if (backToColony && to?.Diplomacy is { } dg)
        {
            if (tierLeft == RegionTier.Core) dg.SovereignColonialRegions--; else dg.PromotedColonies--;
        }
        return (rail, ship);   // 建国（to＝null）は呼び手が新しい国の器を組んでから受け取る
    }

    /// <summary>★遷都先の選び方の1本＝首都を失った国が移る州＝残る active 州のうち最大GDP（同値は Id の小さい方）。
    /// 呼び手＝州の移し替え（<see cref="TransferRegion"/>＝首都州はもう外れている）と、首都の分離独立の建国（MultiNationSim.SpawnNation の段5＝
    /// 首都州が出ていく前に <paramref name="leaving"/> を除いて同じ州を先に決め、忠誠の兵と武器庫をそこへ送る・2026-09-29）。
    /// ⚠建国で出ていく州は両経路とも1つ（首都の蜂起＝本拠州・分離独立＝州ごとに建国）＝2つの呼び手は同じ州を選ぶ。</summary>
    internal static Region? NextCapital(World from, IReadOnlyList<Region>? leaving = null)
    {
        Region? cap = null; double capBest = double.MinValue;
        foreach (var q in from.Regions)
        {
            if (q.State != RegionState.Active || (leaving is not null && leaving.Contains(q))) continue;
            double g = 0; foreach (var s in q.Producers) { var good = q.GoodOf(s.GoodId); if (good is null || good.Tier == GoodTier.Service) continue; g += s.Output * good.RefPrice; }
            if (g > capBest || (g == capBest && (cap is null || q.Id < cap.Id))) { capBest = g; cap = q; }
        }
        return cap;
    }

    /// <summary>州を国へ迎える＝Id は国の中で最大＋1（Id0＝首都は不変）。移し替え（<see cref="TransferRegion"/>）と植民地の州化（Simulation.PromoteColonyToRegion）の1本。</summary>
    internal static void Adopt(World to, Region r)
    {
        int id = 1; foreach (var q in to.Regions) if (q.Id >= id) id = q.Id + 1;
        r.Id = id;
        to.Regions.Add(r);
    }
}
