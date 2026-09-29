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
    private const double OpportunityFactorFloor = 0.1;    // 機会の各乗算因子（賃金/不穏/住居）の下限クランプ

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
                    //   発見の解禁度を通した比＝国軍の基本段の調達と同じ比）を月の予算 ForceUnlockTreasuryCost ぶん買って消費する（国家の調達の1本 Colonial.BuyUpkeepGood
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
                            unlockSpent += Colonial.BuyUpkeepGood(w, cap, g, c.ForceUnlockTreasuryCost * (unlockRates[b] / unlockRateSum) / price, TreasuryCat.Army) * price;
                        }
                    double unlockFill = c.ForceUnlockTreasuryCost > 0 ? PoliticalQuantities.Clamp01(unlockSpent / c.ForceUnlockTreasuryCost) : 0;
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

        // 2. active 地域間の移民（§2.2.1）。最大機会の地域を目的地に、他から引き寄せる
        var active = new List<Region>();
        foreach (var r in w.Regions) if (r.State == RegionState.Active) active.Add(r);
        if (active.Count < 2) return;

        // ★非有限の遮断（2026-07-31・潜在NaNの封じ込め）：旧実装は bestOpp を active[0] の機会で初期化していたため、
        // 先頭州の機会が NaN だと以後の比較 `o > NaN` が恒偽になり **NaN が最大値として居座り**、
        // 下の gap が全 src 州で NaN → クランプも「gap<=0」も NaN を素通し → 国内全州の Size/Savings が一括で NaN 化した。
        // ここでは「評価できない州は目的地に選ばない」＝fail-closed にする。有限値だけの世界では最初の最大値を選ぶ挙動が旧と厳密一致
        //（旧も strict `>` で先頭の最大を保持する）。⚠握り潰しではない：プローブONなら現場を stderr に出して上流を追える。
        Region? dest = null;
        double bestOpp = double.NegativeInfinity;
        foreach (var r in active) { double o = OppChecked(r, c); if (double.IsFinite(o) && o > bestOpp) { bestOpp = o; dest = r; } }
        if (dest is null) return;   // 全州が評価不能＝移民しない（人口は動かさない＝保存則に触れない）
        // 監査2026-07-05（D2）：移民の分散＝第2目的地への分岐。MigrationSecondDestShare 既定0.3（点火）・0で全量が最大機会の単一州へ＝旧と厳密一致。
        // >0 で流量の一部を第2機会の州へ分ける（現実の移民は複数の魅力的地域へ分散する）。決定論＝RNGなし。
        Region? dest2 = null; double secondOpp = double.MinValue;
        if (c.MigrationSecondDestShare > 0 && active.Count >= 3)
            foreach (var r in active) { if (ReferenceEquals(r, dest)) continue; double o = OppChecked(r, c); if (double.IsFinite(o) && o > secondOpp) { secondOpp = o; dest2 = r; } }

        foreach (var src in active)
        {
            if (ReferenceEquals(src, dest)) continue;
            double gap = bestOpp - OppChecked(src, c) - c.TransportFriction;
            if (!(gap > 0)) continue;   // ★否定形で書く＝NaN も弾く（`gap <= 0` は NaN で false になり素通しだった）。有限値では `gap <= 0` と厳密に同値
            double fraction = Math.Clamp(c.RegionMobility * gap, 0, MaxMigrationFraction);
            if (dest2 is not null && !ReferenceEquals(src, dest2) && secondOpp - Opportunity(src, c) - c.TransportFriction > 0)
            {
                double firstFrac = fraction * (1.0 - c.MigrationSecondDestShare);
                MoveRegion(src, dest, firstFrac, c);
                // ★是正（監査G10#2）：MoveRegionは逐次適用（sp.Sizeを直接減らす）＝2回目のfractionは1回目で
                //   既に減ったSizeに掛かる＝第2流量が意図（元Size×fraction×share）より系統的に小さかった。
                //   残ったSize基準の比へ補正し、同じ元Sizeから両方を切る（分母はfraction≤0.2でクランプ済みなので1に近づかない）。
                MoveRegion(src, dest2, fraction * c.MigrationSecondDestShare / Math.Max(1e-9, 1.0 - firstFrac), c);
            }
            else
                MoveRegion(src, dest, fraction, c);
        }
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
        double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        double land = LandFactor(r, rural, c);
        // 改修2026-06-28：工業化度を機会に連結。工業州ほど雇用機会が大きく移民を引く＝空世界の技術水準（高ProductivityBonus・無工業）一極吸引を解く。
        // 空世界の技術水準は Ind=0 で従来どおり初期入植を呼び、工業化した州は後年に競り勝つ（史実：移民は東部都市にも大量に滞留）。
        double indPull = 1.0 + c.IndustrialOpportunityWeight * r.TotalIndustrialization;   // 雇用機会は立っている工業の規模＝新旧合計（失業は(1−unemp)側が既に見る。ヴィンテージ2026-07-10）
        // 労働価格プレミアム（キュー3・金発見移民）：高賃金州（金鉱・ブーム州）ほど機会が大きく移民を引く＝ゴールドラッシュを賃金経由で創発。
        // WagePremium は UpdateRegionalPriceLevels が四半期更新（[0.25,4]クランプ済み）。OpportunityWageWeight 既定0.2（点火）・0で wagePull=1＝旧と厳密一致。
        double wagePull = Math.Max(OpportunityFactorFloor, 1.0 + c.OpportunityWageWeight * (r.WagePremium - 1.0));
        if (!(pop >= 1.0)) return r.ProductivityBonus * indPull * wagePull * land;   // ★否定形＝pop が NaN のとき除算枝（unemp/pop）へ落ちない。有限値では `pop < 1.0` と厳密に同値
        double unemp = 0; foreach (var p in r.Pops) unemp += p.Size * p.UnemploymentRate;
        // プッシュ要因（B-4）：地域の不満（迫害・治安悪化）が機会を下げ、その地域からの流出を促す（ポグロム・抑圧からの逃避）。
        double unrest = 0; foreach (var p in r.Pops) unrest += p.Size * p.Militancy;
        // 接16（監査2026-07-11・2026-07-25休眠配線）：州別犯罪率も押し出し要因へ（「治安悪化」の実配線＝従来の闘争度経由の間接経路は
        // 機会ペナルティ約0.3%とノイズ級だった）。CrimePushWeight 既定6.0（点火2026-07-27）・0で不読＝旧と厳密一致。
        double pushPenalty = 1.0 - c.PushUnrestWeight * (unrest / pop) - c.CrimePushWeight * r.CrimeRate;
        // 都市住宅（housing-urban-rent-absent）：住居充足が低い(過密)州は移民の引力が下がる＝過密で流入が自然飽和（無上限吸引の解消）。
        // HousingMigrationFriction 既定0.5（点火2026-07-19）・0（または HousingFill=1.0＝住居財なし/需要0）で摩擦=1.0＝旧と厳密一致。
        double housingFriction = 1.0 - c.HousingMigrationFriction * (1.0 - r.HousingFill);
        return r.ProductivityBonus * indPull * wagePull * land * (1.0 - unemp / pop) * Math.Max(OpportunityFactorFloor, pushPenalty) * Math.Max(OpportunityFactorFloor, housingFriction);
    }

    // 土地（耕地）の収穫逓減係数。農村人口が耕地賦存量(ArableLand)以下なら1（影響なし）、超えると逓減。EconomyStep の土地作物RGOと Opportunity が共有。
    // ＝上限1（ブースト無し）なので、初期（農村人口≤耕地×余裕）では全シナリオで landFactor=1＝既存の収支・較正を厳密保持。逓減は混雑時のみ。
    internal static double LandFactor(Region r, double ruralLabor, Coefficients c)
    {
        if (r.ArableLand >= 1e8) return 1.0;   // 未設定＝制約なし（合成較正・最小テスト世界）
        double ratio = r.ArableLand / Math.Max(1.0, ruralLabor);
        return Math.Min(1.0, Math.Pow(ratio, c.LandDiminishExponent));
    }

    // src の各クラスから fraction を dest の同クラスへ移す（規模・貯蓄を保存移動）
    private static void MoveRegion(Region src, Region dst, double fraction, Coefficients c)
    {
        foreach (var sp in src.Pops)
        {
            // 人4：地域間移民は経済移民＝閉鎖階級（貴族・聖職）・兵士を動かさない（PopDecision:20 の明示除外・越境の !IsSoldier と対称化）。
            // 貴族が毎月世界の技術水準へ経済移民する非対称の解消（★2026-09-26：旗 RegionMigrationExcludesClosed（#17点火）を焼き込み・x-01）。
            // FrontierDevelopment 較正世界は農民pop単体（Closed/IsSoldier なし）＝TerritoryTests 中立。
            if (sp.Closed || sp.IsSoldier) continue;
            double moveSize = sp.Size * fraction;
            if (!(moveSize > 1e-9)) continue;   // ★否定形＝NaN も弾く（旧 `moveSize <= 1e-9` は NaN で false ＝ Size/Savings を直接 NaN 化していた）。有限値では厳密同値
            Pop? dp = null;
            foreach (var d in dst.Pops) if (d.ClassId == sp.ClassId) { dp = d; break; }
            if (dp is null) continue;   // 目的地に同クラスが無ければ移さない（最小核）
            MoveBetweenRegions(sp, dp, moveSize, c);
        }
    }
    /// <summary>★B1（2026-09-07）：1つの pop から同クラスの別州 pop へ moveSize を運ぶ（規模・貯蓄・債務・結晶資産・民族/男女/子供/老年/識字/闘争度）。
    /// MoveRegion（地域間移民）と Colonial.Step の入植（本国→植民地格の州）が**同じ1本**を呼ぶ＝運搬漏れの再発を防ぐ（抽出＝ビット恒等）。</summary>
    internal static void MoveBetweenRegions(Pop sp, Pop dp, double moveSize, Coefficients c)
    {
        // ★支払いの一元化 段2-3（E3・2026-09-29）：お金の運搬の式を1つに＝ストック×(運ぶ人数÷規模)（社会移動・徴兵・越境移民・招聘と同じ結合順。旧＝(ストック÷規模)×人数）。
        double frac = sp.Size > 1e-9 ? moveSize / sp.Size : 0;
        double moveSavings = sp.Savings * frac;
        double moveDebt = sp.Debt * frac;   // 個人債務も按分して運ぶ（2026-08-02・既定0で恒等）
        double moveBond = sp.BondHoldings * frac;   // ★国債の保有台帳（2026-09-21）も按分（州間移動・入植は同じ国の中＝発行国は同じ）

        sp.Size -= moveSize; sp.Savings -= moveSavings; sp.Debt -= moveDebt; sp.BondHoldings -= moveBond;
        // 入植者は人口構成（民族・男女・子供・識字）を携えて移動先へ混ざる＝他の全移動サイト（PopDecision/Meritocracy/Military/越境移民）と同型。
        // これが欠けると入植先のSizeだけ増えてCoreEthnicShare等が据え置き＝EthnicUnity(加重平均)・男女比の保存が静かに破れる（倍率排除⑧の運搬漏れ修正）。
        double newSize = dp.Size + moveSize;
        if (newSize > 1e-9)
        {
            dp.CoreEthnicShare = (dp.CoreEthnicShare * dp.Size + sp.CoreEthnicShare * moveSize) / newSize;
            dp.FemaleShare = (dp.FemaleShare * dp.Size + sp.FemaleShare * moveSize) / newSize;
            dp.ChildShare = (dp.ChildShare * dp.Size + sp.ChildShare * moveSize) / newSize;
            dp.OldShare = (dp.OldShare * dp.Size + sp.OldShare * moveSize) / newSize;   // 倍率排除⑧：老年比率も運搬（欠けると移動先のOldShareが希釈されず年金課税ベースが乖離）。OldShare=0の世界では恒等＝較正中立
            dp.Literacy = (dp.Literacy * dp.Size + sp.Literacy * moveSize) / newSize;
            // 人1：闘争度の運搬漏れを埋める（他移動サイトとの統一）。地域間移民は識字は運ぶのに闘争度を運ばず＝不満の per-region 分布が静かに破れていた。
            // MigrationCarriesHumanCapital 既定1.0（#17点火）・0で従来どおり運ばない＝旧挙動。>0 で加重混合＝国家集計は中立（既存の識字運搬と同型）。
            if (c.MigrationCarriesHumanCapital > 0)
                dp.Militancy = Military.BlendCarry(dp.Militancy, dp.Size, sp.Militancy, moveSize, newSize, clamp: true);   // ★運搬1本化（段1・恒等）
        }
        dp.Size += moveSize; dp.Savings += moveSavings; dp.Debt += moveDebt; dp.BondHoldings += moveBond;

    }

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
    /// ・再併合＝建国の反転を戻す。格・支配度は動かさない。
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
                if (!sameCore) foreach (var p in r.Pops) p.CoreEthnicShare = 0.0;
                break;
            case Rehoming.Seceded:
                foreach (var p in r.Pops) p.CoreEthnicShare = 1.0 - p.CoreEthnicShare;
                r.AnnexBaggage = 0;   // 案Y：建国の州は新しい国の「生まれの人口」＝併合の荷物ではない（親の下で付いた旧記帳もここで清算）
                break;
            case Rehoming.Returned:
                foreach (var p in r.Pops) p.CoreEthnicShare = 1.0 - p.CoreEthnicShare;   // 建国時の反転を戻す（親の中核定義へ復帰）
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
        double S = r.SumSavings(), P = r.SumPopulation(), D = 0;
        foreach (var p in r.Pops) D += p.Debt;
        double B = S - D;
        // ★輸送の容量は州と一緒に移る（2026-09-30 (118)・推奨で進めた＝保存）：鉄道と船は国の値（World.RailCapacity／ShipCapacity）で、
        //   清算はそれを州の取り分（Transport.RegionShare）で配る＝州が使っていた分は、その州の港と線路にある物。
        //   旧＝移し替えで容量は送り手に全部残った＝建国の新しい国は船腹0・鉄道0で生まれ（fff9f01 の10種＝生まれ≥1000 の新しい国の最初の8四半期は4分の3以上で輸出0）、
        //   送り手の残る州は同じ容量を少ない州で分けた（取り分の窓落ち）。新＝出ていく前の取り分（清算と同じ規則）で送り手から引き、受け手へ足す（建国は呼び手が新しい国へ）。
        //   残る州の容量は変わらない（潜在量比例＝既定の TransportShareByPotential=1 では、送り手の容量×(1−s) を残る州の潜在量で分け直す＝前と同じ）。活動していない州は取り分0。
        double sh = r.State == RegionState.Active ? Transport.RegionShare(Transport.ShareBasis(from, from.Coef), r) : 0.0;
        if (!(sh > 0)) sh = 0;   // 否定形＝NaN は移さない側へ（取り分は定義上 0..1）
        double rail = from.RailCapacity * sh, ship = from.ShipCapacity * sh;
        from.RailCapacity -= rail; from.ShipCapacity -= ship;
        from.Regions.Remove(r);
        from.CumulativeTradeFlow -= B; from.PopulationDelta -= P;
        from.Bank!.Carry(to?.Bank, D);
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
            to.CumulativeTradeFlow += B; to.PopulationDelta += P;
            Transport.Receive(to, rail, ship, from.ShipSteamShareEma);
        }
        if (wasCapital && NextCapital(from) is { } cap) from.SetCapital(cap);   // 首都を失った＝「首都を持つ」不変条件を維持
        Rehome(r, how, sameCore);
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
