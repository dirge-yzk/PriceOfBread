namespace PriceOfBread.SimCore;

/// <summary>
/// 兵士POP の力学（§6.1 軍制・Part 2）。軍隊規模＝兵士POP数。軍事費が兵士を徴募し給与を払う
/// （給与＝国庫→兵士で基礎貨幣保存）。兵士は生産せず消費する＝補給の充足（食料＋日用品＝<see cref="PowerShares.SoldierWelfare"/>）が戦力ゲージの乗数＝
/// 給与や補給が切れた軍は大幅に弱くなる。平時は軍事費が兵数を決め、戦時は徴兵で大衆を兵士化
/// （上限あり・行き過ぎは労働力を奪い経済に打撃）。戦死は兵士POPのみ（<see cref="Diplomacy"/>）。
/// 治安維持力（BaseCoercion＝警察・内政の鎮圧）とは別レバー。兵士POP の無い地域（単一国テスト等）は対象外＝従来挙動。
/// </summary>
public static class Military
{
    internal const double MobilizationGateFloor = 0.05;   // 国民意識ゲートの下限乗数（低国民意識でも最低これだけは動員が届く）。internal＝App のヘッダー/政治タブが同じ床を読む（値の写しを作らない・2026-09-11 #8）

    /// <summary>陸海の熟練度（2026-08-04・作者仕様(5)）。平時は維持費が閾値を超えていれば育ち、下回れば錆びる。
    /// 戦争（内戦を含む）はそれとは別に育てる＝実戦は訓練に勝る。約2世代で完全に減衰＝訓練された世代の退役。
    /// ⚠決定論（RNGなし）。⚠SkillGainRate/SkillWarGain/SkillDecayPerQuarter が全て0なら値が動かない＝厳密恒等。</summary>
    public static void StepSkill(World w, Coefficients c, bool atWar)
    {
        bool fighting = atWar || w.InCivilWar;   // ★B3：述語1本   // 内戦も実戦（作者仕様「内戦含め戦争で上昇」）
        w.ArmySkill = NextSkill(w.ArmySkill, w.ArmyUpkeep, fighting, c);
        w.NavySkill = NextSkill(w.NavySkill, w.NavyUpkeep, fighting, c);
    }
    private static double NextSkill(double skill, double upkeep, bool fighting, Coefficients c)
    {
        double v = skill;
        double over = upkeep - c.SkillUpkeepThreshold;
        if (over > 0) v += c.SkillGainRate * over * (1.0 - v);        // 閾値超過分で育つ（1.0へ漸近＝上限を別に置かない）
        else v -= c.SkillDecayPerQuarter * v * Math.Min(1.0, -over / Math.Max(1e-9, c.SkillUpkeepThreshold));   // 割れた深さに比例して錆びる
        if (fighting) v += c.SkillWarGain * (1.0 - v);                 // 実戦（内戦含む）
        return PoliticalQuantities.Clamp01(v);
    }

    /// <summary>熟練度による戦力倍率（1.0〜1+SkillCombatMax）。作者指示「最大でも2倍程度の強さに留める」。</summary>
    public static double SkillCombatMultiplier(double skill, Coefficients c) => 1.0 + c.SkillCombatMax * PoliticalQuantities.Clamp01(skill);

    /// <summary>扶養限界目標＝SustainLimit × clamp(近隣ライバル最大limit ÷ 自国limit, ArmsRaceLow, ArmsRaceHigh)。
    /// Step（平時の兵の目標＝これそのもの・(145)）と armyneed プローブと画面の**単一ソース**。
    /// ★(145)：外交の無い世界（単一国較正）は「緊張を持つ相手がいない」＝ArmsRaceLow（覇権国の平時の小軍と同じ1本の規則・旧＝0＝不発で人口と軍事費の段の目標へ落ちた）。</summary>
    public static double SustainTargetOf(World w, Coefficients c, out double natManpower)
    {
        double ownLimit = SustainLimitOf(w, c, out natManpower);
        if (!(ownLimit > 1e-9)) return 0;   // 否定形＝NaN も0（兵の目標へ NaN を通さない）
        double rivalMax = 0;
        if (w.Diplomacy is { } dip && w.SelfPower is { } sp)
            foreach (var rp in dip.Powers)
            {
                if (rp == sp || rp.Nation is not { } rw) continue;
                if (dip.Tension(sp, rp) < c.ArmsRaceTensionFloor) continue;   // 緊張を持つ相手だけがライバル（0なら全比較）
                double rl = SustainLimitOf(rw, c, out _);
                if (rl > rivalMax) rivalMax = rl;
            }
        double mod = rivalMax > 1e-9 ? Math.Clamp(rivalMax / ownLimit, c.ArmsRaceLow, c.ArmsRaceHigh) : c.ArmsRaceLow;
        return ownLimit * mod;
    }

    /// <summary>扶養限界（作者設計2026-08-08）＝この国が経済に悪影響なく養える兵数。
    /// 経済側＝持続可能な軍事支出（歳入×SustainMilShare）÷ 兵1人あたり月次コスト（給与＝徴募プールの前月の働き盛り1人あたりの賃金×SoldierPayMult＝<see cref="RecruitWage"/> と同じ規則
    /// ＋装備＝MilitaryUpkeepScale×Σ(rate×RefPrice)×自己費用の物価水準＝**参照価格建てを名目へ**＝市場価格の循環を避ける・S4欠陥の教訓）。
    /// ★(145)：装備に <see cref="World.CostPriceLevel"/>（P の遅い趨勢＝参照価格の単位を名目へ直す物価・鉱物の自己費用と同じ1本）を掛けた
    ///   ＝分子（歳入）と賃金は名目・装備だけ参照価格の単位だった（単位の非対称＝物価の高い国ほど装備が安く見えた）。
    /// 人口側＝徴募可能人口（兵士POPを持つ州の徴募元と兵士の働き盛りの男＝<see cref="RegionManpower"/>）×MaxSoldierFraction。
    /// ★(145)：国民意識の割引（N4・ConscriptConsciousnessWeight）を外した＝国民意識は戦時の動員の深さ（Step の国民意識ゲート）だけが読む
    ///   （旧＝平時の扶養限界の人口の側にも掛けた＝同じ国民意識の2度目・思想⑨。しかも州への按分の分母だけが割り引かれ、分子の州の母数は割り引かれなかった
    ///   ＝国の目標の和が 1÷平均の国民意識 倍に膨らんだ＝清の平時の目標が約12倍＝分子と分母の非対称）。
    /// min が国ごとのボトルネックを選ぶ＝露は経済・英は人口（作者の設計意図どおり式から出る）。
    /// ⚠錨は歳入・人口・参照価格＝ゆっくりしか動かない量のみ（相手の兵数・充足率を読まない＝軍拡ループを閉じない）。</summary>
    public static double SustainLimitOf(World w, Coefficients c, out double natManpower)
    {
        natManpower = 0;
        double poolEarn = 0, poolSize = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double m = RegionManpower(r, c);
            if (!(m > 0)) continue;   // 兵士POPの無い州は母数に入らない（Step も同じ州を飛ばす）
            natManpower += m;
            foreach (var p in r.Pops)
                if (!p.IsSoldier && (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB))
                { poolEarn += Math.Max(0, p.LastEarned); poolSize += WorkingAdults(p); }   // 賃金の分母＝働き盛り（(140)）
        }
        double manLimit = c.MaxSoldierFraction * natManpower;
        double wage = poolSize > 1e-9 ? poolEarn / poolSize : 0;   // 徴募プールの前月の働き盛り1人あたりの賃金＝兵士の機会費用（富国の兵は高い）＝RecruitWage と同じ規則（ここは国の和）
        double equip = 0;
        for (int b = 0; b < c.MilitaryGoods.Length; b++)
            if (c.MilitaryGoods[b] < Catalog.RefPrices.Length) equip += c.MilitaryUpkeepScale * c.MilitaryRates[b] * Catalog.RefPrices[c.MilitaryGoods[b]];
        double perSoldier = wage * c.SoldierPayMult + equip * w.CostPriceLevel;
        double econLimit = perSoldier > 1e-9 ? c.SustainMilShare * Math.Max(0, w.SpendableRevenue()) / perSoldier : 0;   // 支出反射：潤沢国庫は扶養限界にも翻訳（ユリウス塔の戦争準備金。既定0=LastRevenueと厳密同値）
        return Math.Min(econLimit, manLimit);
    }

    /// <summary>★州の兵役の母数の1本（(145)）＝徴募元（農民・都市労働者）と兵士の働き盛りの男（<see cref="RecruitMen"/>）×支配度の届き方。兵士POPの無い州は0。
    /// 兵の目標（<see cref="Step"/>＝州への按分と戦時の徴兵の目標）と扶養限界（<see cref="SustainLimitOf"/>＝国の和）が同じ規則で数える
    /// ＝国の目標を州へ配る比の分子と分母が同じ量（旧＝分子は Step の州の頭数×届き方、分母は扶養限界の国民意識で割り引いた和＝和が目標を超えた）。</summary>
    internal static double RegionManpower(Region r, Coefficients c)
    {
        double m = 0; bool sld = false;
        foreach (var p in r.Pops)
        {
            if (p.IsSoldier) { sld = true; m += RecruitMen(p); }   // 兵士も徴募元と同じ単位＝徴兵と復員で母数が動かない（(144)）
            else if (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB) m += RecruitMen(p);
        }
        return sld ? m * Politics.RuleDelivery(r, c, c.RuleSharesTaxBase) : 0;   // ★支配度 段3：徴兵ベース＝国家区分＋在地区分×村請け率（RuleSharesTaxBase=0 で ×1＝恒等）
    }

    /// <summary>兵1人の機会費用の分母＝働き盛り（16〜59歳・男女）の人数。兵は働き盛りの男の1人＝世帯の頭数（子供と老人を含む）で割った所得で払うと
    /// 分子（大人1人）と分母（世帯の頭数）が非対称＝兵の給与が民間の大人の賃金の約半分になる（2026-09-30 (140)）。
    /// ★(145)：兵役の母数（<see cref="RecruitMen"/>）も同じ窓（16〜59歳の男）になった＝(142) で分かれた窓がそろった。
    ///   労働供給の人数（女性の参加率・60歳以上の0.75・児童労働）にはそろえていない（男1人の機会費用を1割前後低く出す【推】＝兵1人の費用の史実の約3倍との差より小さい＝記録）。</summary>
    internal static double WorkingAdults(Pop p) => p.Size * p.AgeShare(16, 60);

    /// <summary>★州の兵1人の月給の請求の単価＝徴募の母数（農民・都市労働者）の前月の所得÷働き盛りの人数×SoldierPayMult の手前（2026-09-30 (140)）。
    /// 請求（Simulation の SoldierPayBudget＝兵数×これ×SoldierPayMult）と扶養限界（<see cref="SustainLimitOf"/>＝国の和で同じ規則）が同じ母数と同じ窓を読む。
    /// <paramref name="last"/>＝前月の所得の欄（月の決算の後＝LastEarned・月の決算の前＝Earned＝どちらも前月の所得）。</summary>
    internal static double RecruitWage(Region r, Coefficients c, bool last)
    {
        double pe = 0, pa = 0;
        foreach (var p in r.Pops)
            if (!p.IsSoldier && (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB))
            { pe += Math.Max(0, last ? p.LastEarned : p.Earned); pa += WorkingAdults(p); }
        return pa > 1e-9 ? pe / pa : 0;
    }

    public static void Step(World w, Coefficients c, bool atWar)
    {
        // 外12：戦時徴兵率を戦争の規模でスケール（一律12%総動員→限定戦争は数%）。⚠**ConscriptionScalesWithWar は 1.0 で点火済み**（旧コメントの「0（既定）」は誤り・2026-08-01訂正）＝戦時徴兵は一律ではない。0 なら mobilizeFrac=ConscriptionFraction＝現行と厳密恒等。
        // 規模の代理＝厭戦(WarExhaustion＝長期戦・経済悪化・戦死の総合)・戦線数（多正面ほど深い動員）・敵陸軍比（劣勢ほど総動員）。軍需側 WarMobilizationDepth と同型の線形段階化。
        double conscriptFrac = c.ConscriptionFraction;
        if (atWar && c.ConscriptionScalesWithWar > 0 && w.Diplomacy is { } dip && w.SelfPower is { } self)
        {
            int fronts = 0; double enemyLand = 0;
            foreach (var war in dip.Wars)
            {
                Power? foe = war.A == self ? war.B : war.B == self ? war.A : null;
                if (foe is null) continue;
                fronts++; enemyLand += foe.LandPower;
            }
            double exh = self.WarExhaustion;                                             // 0..1（総力戦の進行度の代理）
            double frontDepth = Math.Min(1.0, Math.Max(0, fronts - 1) * c.ConscriptionFrontStep);   // 単一戦線=0（恒等）、多正面で深まる
            double ratioDepth = self.LandPower > 1e-9 ? PoliticalQuantities.Clamp01(enemyLand / self.LandPower - 1.0) : 0;   // 敵優勢(比>1)で深まる。互角以下=0
            double depth = PoliticalQuantities.Clamp01(c.ConscriptionExhaustionWeight * exh + frontDepth + c.ConscriptionEnemyRatioWeight * ratioDepth);   // 段階化の合成（各重み0で depth=0）
            // 下限＝限定戦争の徴兵率、上限＝現行の総動員（ConscriptionFraction）。depth=0（既定or小戦争）で上限に張り付かせず ConscriptionFloorFraction へ、depth=1 で総動員。
            conscriptFrac = c.ConscriptionFloorFraction + (c.ConscriptionFraction - c.ConscriptionFloorFraction) * depth;
        }
        // 総動員（#106・プレイヤーの戦時レバー）＝徴兵の深さを levée en masse 水準（MobilizationConscriptFrac）へ引き上げ＝非兵士POPを兵士へ大量変換（下の国民意識ゲートで低NC国は届かない＝1793仏 vs 墺洪）。★M1（作者裁定2026-09-19）：この深さを読むのは下の強制徴募（wartimeConscript＝徴兵法≥選抜）だけ＝総動員は**既にある徴兵を深める**のであって志願兵制の国を徴兵しない（英1916兵役法・米1863/1917徴兵法・南部連合1862＝代議制国家の大量徴兵は必ず立法を通った）。
        // 既定 w.Mobilized=false で不発（AI の書き手は SelfPlay.DecideQuarter の総動員分岐のみ＝2026-09-20 の段の点火で常時 live）。国民意識ゲート(下)の前に上げることで、総動員も低国民意識国では脱走で目減りする。
        if (atWar && w.Mobilized) conscriptFrac = Math.Max(conscriptFrac, c.MobilizationConscriptFrac);
        // 国民意識ゲート（docs/2026-07-13_国民意識.md）：大規模動員＝nation-in-armsには高い国民意識が要る。低国民意識/多民族帝国は総動員が届かず
        // （脱走・不服従）浅い軍しか立てられない（1793 levée en masse＝革命的国民意識 vs 墺洪1914の限定動員）。外12（戦争規模）で決めた深さを国民意識が上限で抑える。
        // EthnicUnityの軍質(UnityMilitaryQuality＝多民族軍は脆い＝質)とは別＝こちらは動員の深さ(量)。既定 Weight=0 で恒等。
        if (c.NationBuildMobilizationWeight > 0)
        {
            // ★熟練度が総動員のペナルティを軽くする（2026-08-04・作者仕様(5)）。
            //   訓練された幹部（下士官・予備役将校）がいる軍は、急に人を集めても部隊の形になる
            //   ＝プロイセンの予備役制度が「動員しても崩れない軍」を作った。
            //   ⚠国民意識のゲート（低NC国は総動員が届かない）を**緩める**方向にだけ効く＝ゲート自体は残す。
            //   ⚠SkillMobilizationRelief=0 で ×1.0＝厳密恒等。
            double ncGate = c.NationBuildMobilizationWeight * (1.0 - Math.Max(0, w.NationalConsciousness));
            ncGate *= Math.Max(0, 1.0 - c.SkillMobilizationRelief * PoliticalQuantities.Clamp01(w.ArmySkill));
            conscriptFrac *= Math.Max(MobilizationGateFloor, 1.0 - ncGate);
        }
        // 扶養限界方式（作者設計2026-08-08＝軍拡・建艦競争の駆動）：目標兵数の錨を「人口と経済から決まる扶養限界」に置く。
        // SustainLimit = min(経済側, 人口側)。倍率 = clamp(近隣ライバルの最大限界÷自国限界, ArmsRaceLow, ArmsRaceHigh)＝
        // 覇権国は−20%（平時の小軍）・追う側は+20%（軍拡の無理＝afford<1と労働流出が自然に請求書を出す）。
        // ★錨は人口・歳入＝ゆっくりしか動かない量＝「兵数→脅威→目標」の外側ループが構造的に閉じている
        //   （NeedMen錨定 R2a の失敗＝相手の現在兵数を読んで ρ0.79→0.43、の是正）。
        // ★(145) 平時の兵の目標＝扶養限界の目標**そのもの**（作者設計 2026-08-08 の扶養限界の上で AI の点火の梯子 0.25→0.5→1.0 の終点）。旧＝AI が 2026-08-08 の梯子の測定から採った混合
        //   （0.75×軍事費の段×上限割合×母数＋0.25×扶養限界）＝平時の目標に財政の上限が無く、軍拡の段の国は払えるかに依らず母数の約3%を持った
        //   （清1800 ＝約1,060万［史実 80〜100万］・英1860 ＝人口の約4%［約1%］）。混合比 ArmySustainWeight は削除。
        //   ⚠軍事費の段は平時の兵数を動かさない＝装備の予算（MilitaryBudget）と即応度・治安維持力を動かす（兵数は歳入と母数で決まる）。
        double sustainTargetNat = SustainTargetOf(w, c, out double sustainNatManpower);
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;

            Pop? soldier = null, poolA = null, poolB = null;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier) soldier ??= p;                                // 最初の一致（移る先の同じ階級の POP の探し方の1本＝Region.PopOf と同じ規則・段4e I）
                else if (p.ClassId == c.SoldierRecruitClassA) poolA ??= p;   // 農民
                else if (p.ClassId == c.SoldierRecruitClassB) poolB ??= p;   // 都市労働者
            }
            if (soldier == null) continue;

            // ★兵役の母数（(145) 兵の単位）＝徴募元と兵士の**働き盛りの男（16〜59歳）の実数**（RegionManpower＝扶養限界の国の和と同じ1本）。
            //   上限割合（平時 MaxSoldierFraction・戦時 ConscriptionFraction・限定戦争の床・総動員・皆兵の倍率）は「働き盛りの男の何割」と読む。
            //   旧（(142) 段4e 後半〜(144)）＝男20〜39歳を標準のピラミッドの人数に換算（÷0.145）＝割合が「全ての人の何%」のまま＝平時の上限 4%＝男の15%・
            //   戦時 12%＝男の43%・総動員 22%＝男の80%・皆兵×2.5＝男の全てを超えた。働き盛りの男で読むと平時の上限＝人口の約1.1%・戦時 3.3%・総動員 6%・皆兵 8%（史実の桁）。
            //   徴兵が引くのは徴兵の年齢の男（MoveCohort の 20〜39／皆兵 49）＝母数の部分集合＝徴兵でも復員でも母数は動かない（(144) の性質のまま）。
            //   兵を単位にした較正（戦力ゲージ・治安維持力・装備・政治の正規化）は同じ比（WorkingMenStdShare）で読み替えた（Coefficients の各行）。
            double manpower = RegionManpower(r, c);   // 兵役の母数＝農民＋都市労働者＋現兵士（エリートは除く）×支配度の届き方

            // ※給与は「兵力」（役務財）の調達に移行：兵士が兵力を算出し国家が買う（EconomyStep・国庫→兵士で保存）。
            //   兵力財を持たない簡易世界（SoldierPopIndex<0）では給与なし＝従来の充足ベース挙動のみ。

            // === 徴募/離職：扶養限界の目標へ漸進（プールから規模＋貯蓄を比例移動＝総人口・総マネー保存）===
            // ⚠墓標2026-08-08：ここには「富で兵を養う」(SoldierWealthExponent)と「軍の恒常性」(SoldierSustainDriven)が
            //   あったが、いずれも不合格で**再点火禁止**。恒常性は「積分器＋欠陥信号＋min凍結」の三重機序で発散し、
            //   富の乗数は NeedMen 錨定（ArmyShape）が上位互換。★実装は 2026-08-12 に撤去（30行）。
            //   撤去時の基準線＝SOURCE-HASH 778729fcd7aa72b7（build緑・テスト283/283緑・savetest 相対差0.0E+000）。
            // 平時＝扶養限界の目標の州按分（州の母数÷国の母数＝分子と分母が同じ RegionManpower＝州の和が国の目標に一致）。
            // 戦時徴兵の max 経路（下の wartimeConscript）は不変＝平時=扶養限界・戦時=人口。
            double desired = sustainNatManpower > 1e-9 ? sustainTargetNat * (manpower / sustainNatManpower) : 0;
            double rate = c.SoldierMigrationRate;
            // 1934拡張E5 徴兵法：戦時の強制徴募は**徴兵制の国だけ**（志願兵制は戦時も平時の目標＝扶養限界のまま＝小さい軍・不満なし）。
            // 既定 Conscription＝現行挙動（戦時徴兵）。conscriptFrac は外12で戦争規模スケール（既定＝ConscriptionFraction＝恒等）。
            bool wartimeConscript = atWar && w.ConscriptionLaw >= ConscriptionLaw.Selective;   // ★3値化2026-08-01：選抜以上で発動（旧2値と同値）   // ★M1（作者裁定2026-09-19）：総動員は条件に入らない＝志願兵制を迂回しない（旧 `|| w.Mobilized`＝2026-08-31の是正を覆す）。総動員は上の conscriptFrac で深さだけを上げる
            // ★3値化2026-08-01：**引ける人数が法で変わる**。選抜＝籤で一部（旧＝1.0＝厳密同値）／皆兵＝適齢男子ぜんぶ＋予備役。
            // ⚠これは「強い軍が持てる」だけの話ではない——引いた人数ぶん労働力が畑と工場から消える＝供給側の穴が開く。
            double lawFrac = w.ConscriptionLaw == ConscriptionLaw.Universal ? c.UniversalConscriptionFactor : 1.0;
            if (wartimeConscript) { desired = Math.Max(desired, conscriptFrac * lawFrac * manpower); rate = c.ConscriptionRate; }  // 戦時＝徴兵（速く・上限まで）
            double delta = rate * (desired - soldier.Size);
            // ★支配度doc §15（2026-09-06 追記裁定・器2026-09-07）：平時の常備軍の徴募でも、植民地格または在地区分が国家区分を上回る州から引いた分は遺恨を積む
            //   （英印軍・セネガル狙撃兵＝平時の徴募が本国軍と同じ規則で恨まれないのが誤り）。PeacetimeConscriptGrievance＝遺恨率の倍率（0＝旧＝厳密恒等）。
            bool peacetimeColonial = c.PeacetimeConscriptGrievance > 0 && !wartimeConscript && (r.Tier == RegionTier.Colonial || (r.Rule.Length == 4 && r.Rule[1] > r.Rule[0]));
            if (delta > 0) DrawFromPool(soldier, poolA, poolB, delta, c, wartimeConscript || peacetimeColonial, peacetimeColonial ? c.PeacetimeConscriptGrievance : 1.0, DraftAgeHi(w));    // 徴募（プール→兵士）＝規模比例。遺恨は wartimeConscript＝徴兵法≥選抜の戦時だけ（★M1 2026-09-19：2026-08-31の「総動員なら志願制でも発火」を覆す＝志願兵制は総動員しても強制徴募されない＝遺恨も立たない。番人テスト MilitaryTests で固定）
            else DischargeToPool(soldier, poolA, poolB, -delta, c);          // 離職/復員（兵士→プール）
        }

        // 即応度（戦力ゲージの乗数）＝兵卒の補給の充足 × 装備充足(MilitaryEquipFill)。
        // 給与・補給が切れる（兵が食えない・買えない）か、武器を買えない（材不足）と下がる＝弱い軍。材で詰まった戦争は負ける（要望）。
        // ★階層3分割（2026-09-22 既定化）：旧＝兵士POPの満足度の平均。分割後の満足度には食料が載らない＝「食えない軍」が即応度から消える
        //   （1812年の退却・1917年の前線＝飢えた軍は戦えない）。⇒ 兵卒の漏れと**同じ1本**（PowerShares.SoldierWelfare＝食料＋日用品の台帳）を読む
        //   ＝旧は同じ量（兵士POPの人口加重平均満足度）を2箇所で別々に集計していた重複も畳む。
        double sSize = w.SoldierSize();
        double moraleFill = PowerShares.SoldierWelfare(w);
        // 課題6（駐屯州調達）：即応の装備充足＝兵士加重の全国平均へ（g=0では旧＝首都州のみ＝厳密同値）。
        double equipFillEff = w.Region.MilitaryEquipFill;
        if (c.GarrisonProcurement > 0 && sSize > 1e-9)
        {
            double fw = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double ls = 0; foreach (var p in r.Pops) if (p.IsSoldier) ls += p.Size;
                fw += ls * r.MilitaryEquipFill;
            }
            double garFill = fw / sSize;
            equipFillEff = (1.0 - c.GarrisonProcurement) * equipFillEff + c.GarrisonProcurement * garFill;
        }
        w.MilitaryReadiness = PoliticalQuantities.Clamp01(moraleFill * equipFillEff);

        NavalFuelStep(w, c);   // 運用コスト監査2026-08-16（#1 海軍の運用燃料）：NavalFuelPerGauge=0（既定）では先頭ガードで即return＝厳密恒等
    }

    /// <summary>海軍の運用燃料（運用コスト監査2026-08-16 #1・休眠）。海軍維持費（NavyUpkeep）は兵数と熟練度にしか行かず、
    /// 艦隊を動かす石炭・重油の物理消費がゼロだった欠落の是正＝国庫が本国市場から燃料を買い焚く（植民地補給線と同じ
    /// <see cref="Colonial.BuyUpkeepGood"/> 経路＝需要計上・在庫律速・国庫上限・移し方の1本〔World.Transfer〕で保存・台帳は TreasuryCat.Navy）。
    /// 需要式＝NavalFuelPerGauge×FleetGauge×NavyUpkeep×蒸気ゲート×EraFactor（帆走時代は燃料ゼロ・NavyUpkeep=係船か常時哨戒か）。
    /// 充足はEMA（World.NavalFuelFillEma）→ Diplomacy の同期が Power.NavalFuelMul（下限フロア付き線形）へ写し
    /// NavalPower の min に緩く噛む（燃料が切れた艦隊は港から出られない）。⚠年号・年代は読まない（蒸気ゲート＝SteamTechOnset のみ）。
    /// ⚠既定 NavalFuelPerGauge=0 では1命令も走らない（EMA の読み書きもしない）＝厳密恒等。</summary>
    public static void NavalFuelStep(World w, Coefficients c)
    {
        if (!(c.NavalFuelPerGauge > 0)) return;   // 否定形＝NaN係数も不走行側へ
        var r = w.Region;
        double gauge = w.SelfPower?.FleetGauge ?? 0;   // 単一国較正（SelfPowerなし）・艦隊駆動OFFでは0＝燃料需要なし（fill=1.0側）
        double steamGate = c.SteamGate(w.TechLevel);   // 蒸気の門の1本（EconomyStep と同じ・段4e K＝帆走艦隊は焚かない）（NaN は Clamp01 が0＝帆走側へ）
        double coalWant = c.NavalFuelPerGauge * gauge * PoliticalQuantities.Clamp01(w.NavyUpkeep) * steamGate * c.EraFactor(w.WorldTechLevel, c.EraNavalFuel);
        double fill = 1.0;   // 需要ゼロ（帆走・係船・艦隊なし）＝燃料は制約でない
        if (coalWant > 1e-9 && r.State == RegionState.Active && Catalog.CoalId < r.Markets.Length)
        {
            var (fuelG, want) = Colonial.ChooseFuelGood(r, c, coalWant);   // 石炭→重油の二択（供給実在ガード込み・植民地補給線と同式）
            double got = Colonial.BuyUpkeepGood(w, r, fuelG, want, TreasuryCat.Navy);   // 国庫→本国市場（保存）。買い控え（価格ゲート）・在庫切れ・国庫涸れは充足低下として現れる
            fill = want > 1e-9 ? got / want : 1.0;
            if (!(fill >= 0 && fill <= 1)) fill = 1.0;   // 否定形＝NaNは中立へ
        }
        double a = PoliticalQuantities.Clamp01(c.NavalFuelFillSmoothing);   // 炭庫の在庫月数＝月次の品切れで即座に艦隊を縛らない
        w.NavalFuelFillEma = (1.0 - a) * w.NavalFuelFillEma + a * fill;
        if (!(w.NavalFuelFillEma >= 0 && w.NavalFuelFillEma <= 1)) w.NavalFuelFillEma = 1.0;   // 否定形＝NaNは中立へ
    }

    // 徴募：プール（農民＋労働者）→兵士。規模に比例して引き抜く（1ヶ月にプールの半分まで）。
    // 戦時の強制徴兵（徴兵法≥選抜の国。総動員はその深さを上げる）は取られた家の不満を積む（徴兵率×ConscriptGrievanceRate。志願募兵は総動員中でも積まない＝M1）＝E5徴兵法の一般効果。
    // 供給源の選別（POP動詞③=均等/農民/貴族士官…）は2026-07-21ユーザー指示で撤去済み・この遺恨のみ残置（供給源に依らず戦時徴兵で発火）。
    private static void DrawFromPool(Pop soldier, Pop? a, Pop? b, double amount, Coefficients c, bool conscription, double grievanceScale, double draftHi)
    {
        double sa = a?.Size ?? 0, sb = b?.Size ?? 0, pool = sa + sb;
        if (pool < 1e-9) return;
        amount = Math.Min(amount, pool * 0.5);
        // Min クランプは amount≤pool/2 ゆえ商は常に sa/2 未満＝不発（比例で厳密に引き抜く）。
        double da = Math.Min(amount * (sa / pool), sa), db = Math.Min(amount * (sb / pool), sb);
        MoveCohort(a, soldier, da, c, draftHi);
        MoveCohort(b, soldier, db, c, draftHi);
        if (conscription && c.ConscriptGrievanceRate > 0)
        {
            if (a is not null && sa > 1e-9 && da > 1e-12) a.Militancy = Math.Min(1, a.Militancy + c.ConscriptGrievanceRate * grievanceScale * (da / sa));
            if (b is not null && sb > 1e-9 && db > 1e-12) b.Militancy = Math.Min(1, b.Militancy + c.ConscriptGrievanceRate * grievanceScale * (db / sb));
        }
    }

    // 離職/復員：兵士→プール（農民＋労働者）へ規模比例で戻す。
    private static void DischargeToPool(Pop soldier, Pop? a, Pop? b, double amount, Coefficients c)
    {
        amount = Math.Min(amount, soldier.Size);
        double pool = (a?.Size ?? 0) + (b?.Size ?? 0);
        if (pool < 1e-9) { MoveCohort(soldier, a ?? b, amount, c); return; }
        MoveCohort(soldier, a, amount * ((a?.Size ?? 0) / pool), c);
        MoveCohort(soldier, b, amount * ((b?.Size ?? 0) / pool), c);
    }

    // 【削除2026-09-26（棚卸し sweep-23）】3引数版 MoveCohort（呼ぶたびに既定の係数 new Coefficients() を作って4引数版を呼んでいた）＝呼び手は手元の係数を渡す。
    // 【削除2026-09-30 段4e I】BlendCarry（闘争度の運搬の式の1本・段1 2026-08-29）＝人の移し方の1本 Pop.Blend が全ての属性を混ぜる（闘争度の書き手の運搬8か所が1行へ）。

    /// <summary>階級間の人の移動（徴兵・復員・通常＝規模・属性・貯蓄と債務と国債の保有を運ぶ＝国内移籍で総人口・総マネー保存）。setup と徴募で共用。
    /// ★2026-09-30 処理の一元化 段4e I：本体は人の移し方の1本 <see cref="Pop.Blend"/>（旧の3枝＝徴兵・復員・通常の混ぜ方の写しを畳んだ）。
    /// 徴兵＝**徴兵の年齢の男**（20〜<paramref name="draftHi"/> 歳・<see cref="DraftAgeHi"/>）だけが抜ける（動けるのはその年齢の男の数まで・<see cref="Pop.Blend"/> の draft）
    /// ＝残る集団の女性・子供・老年の割合が上がる＝男手の消えた村。
    /// 復員＝兵士 POP の構成（年を取った男）のまま戻る（兵士も年を取る＝長い軍務の後は年上の男が戻る）。</summary>
    public static void MoveCohort(Pop? from, Pop? to, double amount, Coefficients c, double draftHi = 40)
    {
        if (from == null || to == null || amount < 1e-12 || from.Size < 1e-12) return;
        amount = Math.Min(amount, from.Size);
        bool draft = to.IsSoldier && !from.IsSoldier;
        if (draft)
        {
            // ★年齢の層（2026-09-30 段4e 後半・推奨で採択＝作者の恒久指示 2026-09-29・設計書 §9 の Q9〔(144) 訂正〕）：上限は徴兵の年齢の男＝男20〜39歳（選抜・志願）／男20〜49歳（皆兵）。
            //   旧＝成人（16歳以上）−老年（60歳以上）の男（旗 ConscriptExcludeOldMales）。
            amount = Math.Min(amount, from.Size * from.AgeShare(20, draftHi, 0));
            if (amount < 1e-12) return;
        }
        Pop.Blend(from, to, amount, draft: draft, draftLo: 20, draftHi: draftHi);
    }

    /// <summary>★徴兵の年齢の上の端（2026-09-30 段4e 後半・推奨で採択＝作者の恒久指示 2026-09-29・設計書 §9 の Q9〔(144) 訂正〕）＝選抜・志願は男20〜39歳・皆兵は男20〜49歳（予備役まで＝池を広げる）。
    /// ⚠皆兵の倍率 UniversalConscriptionFactor の削除（物理の池が倍率を置き換える）は別の腕（1つの腕に1つの変更＝申し送り）。</summary>
    internal static double DraftAgeHi(World w) => w.ConscriptionLaw == ConscriptionLaw.Universal ? 50 : 40;

    /// <summary>★兵役の母数の1本（(145) 兵の単位）＝POP の**働き盛りの男（16〜59歳）の実数**（規模×男16〜59の割合）。
    /// MaxSoldierFraction・徴兵の率の4本（戦時・限定戦争の床・総動員・皆兵の倍率）は「働き盛りの男の何割」（史実の桁＝平時の上限 人口の約1.1%・戦時 3.3%・総動員 6%・皆兵 8%）。
    /// 旧（2026-09-30 段4e 後半・設計書 §4-1〜(144)）＝男20〜39歳を標準のピラミッドの人数に換算（÷0.145）＝割合が「全ての人の何%」のままだった。
    /// 窓＝賃金の分母（<see cref="WorkingAdults"/>）と同じ 16〜59歳。徴兵が引く男20〜39（皆兵49）はこの部分集合。
    /// 読み手＝州の母数（<see cref="RegionManpower"/>＝兵の目標と扶養限界）・画面と診断の写し。現兵士も同じ単位（徴兵と復員で母数が動かない・(144)・番人 MilitaryTests）。</summary>
    internal static double RecruitMen(Pop? p) => p is null ? 0 : p.Size * p.AgeShare(16, 60, 0);

    /// <summary>★兵の単位の換算（(145)）＝標準のピラミッドの働き盛りの男（16〜59歳）の割合（0.2765）＝新しい数0（標準のピラミッドの表から）。
    /// 兵役の母数を「全ての人（標準の構成）」から「働き盛りの男」へ読み替えた比＝同じ割合の値で兵の数がこの比になる。
    /// 兵を単位にした較正（戦力ゲージ SoldiersPerMilitary・治安維持力 CoercionPerSoldier・装備 MilitaryUpkeepScale・
    /// 軍政の力 MilitaryCoupWeight・軍の政治力 ArmyPoliticalWeight・徴兵の国民形成 NationBuildConscript・戦争熱 WarFeverSoldier）と
    /// 政治の正規化（平時の上限の人口比 <see cref="PeaceCapShareOfPop"/>）は、この比で読み替える＝旧の兵1単位の働きを新しい兵の単位で保つ。
    /// ★(149) 反証の指摘：民兵の戦力 MilitiaCombatValue（既定0）は「本当の兵1人に対する比」＝兵と同じ分子に入る＝読み替えない（値は旧のまま）。
    /// ⚠ MilitaryUpkeepScale は休眠の反乱の武器の購入（EconomyStep＝MilitaryUpkeepScale×参加人数〔本当の頭数〕）も読む＝点火すると反乱の装備の必要量が約3.6倍（1÷この比）＝点火の回で決める。</summary>
    internal static readonly double WorkingMenStdShare = Pop.StdShare(16, 60, 0);

    /// <summary>★平時の上限の人口比（(145)）＝MaxSoldierFraction（働き盛りの男の割合）×標準の構成の働き盛りの男の割合＝「兵÷総人口」で読む正規化の分母
    /// （軍の社会的重み・職業軍の度合い π・軍部の組織性）。旧＝MaxSoldierFraction をそのまま人口比として読んだ（母数が全ての人の頃の単位）。</summary>
    internal static double PeaceCapShareOfPop(Coefficients c) => c.MaxSoldierFraction * WorkingMenStdShare;
}
