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
    /// Step（兵数の錨）と SelfPlay 配線C（請求書駆動の軍事費）と armyneed プローブの**単一ソース**。
    /// Diplomacy/SelfPower が無い世界（単一国較正）は0＝不発。</summary>
    public static double SustainTargetOf(World w, Coefficients c, out double natManpower)
    {
        natManpower = 0;
        if (w.Diplomacy is not { } dip || w.SelfPower is not { } sp) return 0;
        double ownLimit = SustainLimitOf(w, c, out natManpower);
        if (ownLimit <= 1e-9) return 0;
        double rivalMax = 0;
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
    /// 経済側＝持続可能な軍事支出（歳入×SustainMilShare）÷ 兵1人あたり月次コスト（給与＝徴募プールの前月賃金×SoldierPayMult
    /// ＋装備＝MilitaryUpkeepScale×Σ(rate×RefPrice)＝**参照価格建て**＝市場価格の循環を避ける・S4欠陥の教訓）。
    /// 人口側＝徴募可能人口（兵士POPを持つ州の農民+労働者+兵士）×MaxSoldierFraction。
    /// min が国ごとのボトルネックを選ぶ＝露は経済・英は人口（作者の設計意図どおり式から出る）。
    /// ⚠錨は歳入・人口・参照価格＝ゆっくりしか動かない量のみ（相手の兵数・充足率を読まない＝軍拡ループを閉じない）。</summary>
    public static double SustainLimitOf(World w, Coefficients c, out double natManpower)
    {
        natManpower = 0;
        double poolEarn = 0, poolSize = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            Pop? sld = null; double pa = 0, pb = 0, pe = 0, ps = 0;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier) sld = p;
                else if (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB)
                { pa += p.ClassId == c.SoldierRecruitClassA ? p.Size : 0; pb += p.ClassId == c.SoldierRecruitClassB ? p.Size : 0; pe += Math.Max(0, p.LastEarned); ps += p.Size; }
            }
            // ★N4 徴兵の州別化（国民意識doc・2026-08-13）：**国民意識の無い州からは兵が出ない。**
            //   ⚠旧＝兵士POPを持つ活動州の頭数をそのまま足していた＝多民族帝国の周辺州も中核州と同率で徴募できた。
            //   ★史実＝墺洪の非独語圏・露のポーランド/中央アジアは、頭数はあっても「皇帝のために死ぬ理由」が無く、
            //     徴兵忌避と脱走で実効の動員力にならなかった。国民国家（仏・普）が同じ人口からはるかに多くの兵を出せたのはこれである。
            //   ⚠国名も民族名も書かない——読むのは州のPOPの国民意識だけ。既定0＝×1.0＝**厳密恒等**。
            if (sld is not null)
            {
                double share = 1.0;
                if (c.ConscriptConsciousnessWeight > 0)
                {
                    double nc = 0, ncSize = 0;
                    foreach (var p in r.Pops) if (!p.IsSoldier && p.NationalConsciousness >= 0) { nc += p.NationalConsciousness * p.Size; ncSize += p.Size; }
                    double avgNc = ncSize > 1e-9 ? PoliticalQuantities.Clamp01(nc / ncSize) : 1.0;   // 1つも捕捉されていない＝機構が休眠＝割引なし
                    share = Math.Max(0, 1.0 - c.ConscriptConsciousnessWeight * (1.0 - avgNc));
                }
                natManpower += (pa + pb + sld.Size) * share; poolEarn += pe; poolSize += ps;
            }
        }
        double manLimit = c.MaxSoldierFraction * natManpower;
        double wage = poolSize > 1e-9 ? poolEarn / poolSize : 0;   // 徴募プールの前月賃金＝兵士の機会費用（富国の兵は高い）
        double equip = 0;
        for (int b = 0; b < c.MilitaryGoods.Length; b++)
            if (c.MilitaryGoods[b] < Catalog.RefPrices.Length) equip += c.MilitaryUpkeepScale * c.MilitaryRates[b] * Catalog.RefPrices[c.MilitaryGoods[b]];
        double perSoldier = wage * c.SoldierPayMult + equip;
        double econLimit = perSoldier > 1e-9 ? c.SustainMilShare * Math.Max(0, w.SpendableRevenue()) / perSoldier : 0;   // 支出反射：潤沢国庫は扶養限界にも翻訳（ユリウス塔の戦争準備金。既定0=LastRevenueと厳密同値）
        return Math.Min(econLimit, manLimit);
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
        // ⚠ArmySustainWeight=0 ならブロックごとスキップ＝厳密恒等（★ 2026-08-08 採用＝既定0.25）。Diplomacy==null（単一国較正）も不発。
        double sustainTargetNat = 0, sustainNatManpower = 0;
        if (c.ArmySustainWeight > 0 && w.Diplomacy is not null && w.SelfPower is not null)
            sustainTargetNat = SustainTargetOf(w, c, out sustainNatManpower);
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;

            Pop? soldier = null, poolA = null, poolB = null;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier) soldier = p;
                else if (p.ClassId == c.SoldierRecruitClassA) poolA = p;   // 農民
                else if (p.ClassId == c.SoldierRecruitClassB) poolB = p;   // 都市労働者
            }
            if (soldier == null) continue;

            double poolSize = (poolA?.Size ?? 0) + (poolB?.Size ?? 0);
            double manpower = (poolSize + soldier.Size) * Politics.RuleDelivery(r, c, c.RuleSharesTaxBase);   // 兵役の母数＝農民＋都市労働者＋現兵士（エリートは除く）。★支配度 段3：徴兵ベース＝国家区分＋在地区分×村請け率（RuleSharesTaxBase=0 で ×1＝恒等）

            // ※給与は「兵力」（役務財）の調達に移行：兵士が兵力を算出し国家が買う（EconomyStep・国庫→兵士で保存）。
            //   兵力財を持たない簡易世界（SoldierPopIndex<0）では給与なし＝従来の充足ベース挙動のみ。

            // === 徴募/離職：軍事費が決める目標兵数へ漸進（プールから規模＋貯蓄を比例移動＝総人口・総マネー保存）===
            double desired = w.MilitarySpending * c.MaxSoldierFraction * manpower;
            // ⚠墓標2026-08-08：ここには「富で兵を養う」(SoldierWealthExponent)と「軍の恒常性」(SoldierSustainDriven)が
            //   あったが、いずれも不合格で**再点火禁止**。恒常性は「積分器＋欠陥信号＋min凍結」の三重機序で発散し、
            //   富の乗数は NeedMen 錨定（ArmyShape）が上位互換。★実装は 2026-08-12 に撤去（30行）。
            //   撤去時の基準線＝SOURCE-HASH 778729fcd7aa72b7（build緑・テスト283/283緑・savetest 相対差0.0E+000）。
            // 扶養限界方式：desired = (1−k)×人口×スライダー駆動 + k×扶養限界目標の州按分（州manpower／全国manpower）。
            // 戦時徴兵の max 経路（下の wartimeConscript）は不変＝平時=扶養限界・戦時=人口。
            if (sustainTargetNat > 0 && sustainNatManpower > 1e-9)
            {
                double susK = PoliticalQuantities.Clamp01(c.ArmySustainWeight);
                desired = (1.0 - susK) * desired + susK * sustainTargetNat * (manpower / sustainNatManpower);
            }
            double rate = c.SoldierMigrationRate;
            // 1934拡張E5 徴兵法：戦時の強制徴募は**徴兵制の国だけ**（志願兵制は戦時も志願規模＝MilitarySpending駆動のまま＝小さい軍・不満なし）。
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
            if (delta > 0) DrawFromPool(soldier, poolA, poolB, delta, c, wartimeConscript || peacetimeColonial, peacetimeColonial ? c.PeacetimeConscriptGrievance : 1.0);    // 徴募（プール→兵士）＝規模比例。遺恨は wartimeConscript＝徴兵法≥選抜の戦時だけ（★M1 2026-09-19：2026-08-31の「総動員なら志願制でも発火」を覆す＝志願兵制は総動員しても強制徴募されない＝遺恨も立たない。番人テスト MilitaryTests で固定）
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
    /// <see cref="Colonial.BuyUpkeepGood"/> 経路＝需要計上・在庫律速・国庫上限・World.Distribute で保存・台帳は TreasuryCat.Navy）。
    /// 需要式＝NavalFuelPerGauge×FleetGauge×NavyUpkeep×蒸気ゲート×EraFactor（帆走時代は燃料ゼロ・NavyUpkeep=係船か常時哨戒か）。
    /// 充足はEMA（World.NavalFuelFillEma）→ Diplomacy の同期が Power.NavalFuelMul（下限フロア付き線形）へ写し
    /// NavalPower の min に緩く噛む（燃料が切れた艦隊は港から出られない）。⚠年号・年代は読まない（蒸気ゲート＝SteamTechOnset のみ）。
    /// ⚠既定 NavalFuelPerGauge=0 では1命令も走らない（EMA の読み書きもしない）＝厳密恒等。</summary>
    public static void NavalFuelStep(World w, Coefficients c)
    {
        if (!(c.NavalFuelPerGauge > 0)) return;   // 否定形＝NaN係数も不走行側へ
        var r = w.Region;
        double gauge = w.SelfPower?.FleetGauge ?? 0;   // 単一国較正（SelfPowerなし）・艦隊駆動OFFでは0＝燃料需要なし（fill=1.0側）
        double steamGate = PoliticalQuantities.Clamp01((w.TechLevel - c.SteamTechOnset) / c.SteamTechWidth);   // EconomyStep の蒸気ゲートと同式（帆走艦隊は焚かない）（NaN は Clamp01 が0＝帆走側へ）
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
    private static void DrawFromPool(Pop soldier, Pop? a, Pop? b, double amount, Coefficients c, bool conscription, double grievanceScale = 1.0)
    {
        double sa = a?.Size ?? 0, sb = b?.Size ?? 0, pool = sa + sb;
        if (pool < 1e-9) return;
        amount = Math.Min(amount, pool * 0.5);
        // Min クランプは amount≤pool/2 ゆえ商は常に sa/2 未満＝不発（比例で厳密に引き抜く）。
        double da = Math.Min(amount * (sa / pool), sa), db = Math.Min(amount * (sb / pool), sb);
        MoveCohort(a, soldier, da, c);
        MoveCohort(b, soldier, db, c);
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

    /// <summary>階級間の人の移動（規模＋貯蓄を比例移動＝per-capita貯蓄を保つ。国内移籍で総人口・総マネー保存）。setup と徴募で共用。</summary>
    /// <summary>★運搬の加重平均を1本へ（思想⑫・docs/202608 §14-9 段1・2026-08-29）：闘争度の「書き手」8箇所が
    /// 同じ式（到着側＝サイズ加重平均）を別々に書いていた＝ラチェット（WriterRatchetTests）の分子を減らす純リファクタ。
    /// ⚠clamp の有無は site ごとに違った（Meritocracy/PopDecision/Territory＝あり・Military/移民＝なし）＝**引数で保存**
    ///   ＝加重平均は数学的に[0,1]を出ないが浮動小数の1ulp超えで clamp が効く縁があるため、揃えるとビット恒等が崩れる。</summary>
    public static double BlendCarry(double toVal, double toSize, double fromVal, double amount, double newSize, bool clamp)
    {
        double v = (toVal * toSize + fromVal * amount) / newSize;
        return clamp ? PoliticalQuantities.Clamp01(v) : v;
    }

    // 【削除2026-09-26（棚卸し sweep-23）】3引数版 MoveCohort（呼ぶたびに既定の係数 new Coefficients() を作って4引数版を呼んでいた）＝呼び手は手元の係数を渡す。

    /// <summary>階級間の人の移動（係数付き）。人1（人的資本の運搬整合）：MigrationCarriesHumanCapital=1 で識字・闘争度も size 加重で運搬（0=現行の無運搬で厳密恒等）。</summary>
    public static void MoveCohort(Pop? from, Pop? to, double amount, Coefficients c)
    {
        if (from == null || to == null || amount < 1e-12 || from.Size < 1e-12) return;
        amount = Math.Min(amount, from.Size);
        double size0 = from.Size;   // 動かす前の人数＝お金の按分の分母（お金は末尾で1回だけ運ぶ）
        if (to.IsSoldier && !from.IsSoldier)
        {
            // 徴兵：**成人男性**だけが抜ける（動けるのは成人男性の数まで）。残る集団の女性比率・子供比率が上がる＝男手の消えた村。
            double adults = from.Size * (1.0 - from.ChildShare);
            // 他13（監査2026-07-11・2026-07-25休眠配線）：旧上限は老年男性を含む＝上限まで徴兵すると「出征した老年男性」が下の olds 保存で
            // 残留側にも数え直され OldShare が水増し（出産可能プールの過小・年金基盤の過大＝深い総動員時のみ発火）。1で上限を働き盛り男性
            //（成人−老年）へ締める。既定0＝旧上限＝現行と厳密一致。
            double draftable = c.ConscriptExcludeOldMales > 0 ? from.Size * Math.Max(0, 1.0 - from.ChildShare - from.OldShare) : adults;
            amount = Math.Min(amount, draftable * (1.0 - from.FemaleShare));
            if (amount < 1e-12) return;
            double children = from.Size * from.ChildShare;
            double olds = from.Size * from.OldShare;   // 老年は徴兵されない（子供と同型の人数保存）＝男手が抜けると残る集団の老年比率が上がる
            double females = adults * from.FemaleShare;
            from.Size -= amount;
            if (from.Size > 1e-12)
            {
                from.ChildShare = PoliticalQuantities.Clamp01(children / from.Size);
                from.OldShare = PoliticalQuantities.Clamp01(olds / from.Size);   // 老年人数を保存（OldShare=0の世界では0のまま＝較正中立）
                double adultsLeft = adults - amount;
                from.FemaleShare = adultsLeft > 1e-12 ? PoliticalQuantities.Clamp01(females / adultsLeft) : 0.5;
            }
            if (to.Size + amount > 1e-12) to.CoreEthnicShare = (to.CoreEthnicShare * to.Size + from.CoreEthnicShare * amount) / (to.Size + amount);   // 倍率排除⑧：徴兵は中核民族割合を運搬（軍の結束は兵士の民族構成から創発）
            // 人1：識字・闘争度も運搬（新兵の人的資本を無償生成しない）。MigrationCarriesHumanCapital=0 なら from の属性を無視＝旧の to 据え置きと厳密恒等（★ #17 で有効化＝既定1.0）。
            if (c.MigrationCarriesHumanCapital > 0 && to.Size + amount > 1e-12)
            {
                to.Literacy = (to.Literacy * to.Size + from.Literacy * amount) / (to.Size + amount);
                to.Militancy = BlendCarry(to.Militancy, to.Size, from.Militancy, amount, to.Size + amount, clamp: false);   // ★運搬1本化（段1・恒等）
            }
            to.Size += amount;   // 兵士は成人男性のみ（FemaleShare=0・ChildShare=0のまま）
        }
        else if (from.IsSoldier && !to.IsSoldier)
        {
            // 復員：成人男性が市民階級へ戻る＝受け入れ側の女性比率・子供比率が下がる。
            double childrenTo = to.Size * to.ChildShare;
            double oldsTo = to.Size * to.OldShare;   // 復員兵（成人男性）は老年でない＝受入側の老年「人数」を保存（流入で老年比率は下がる）
            double adultsTo = to.Size * (1.0 - to.ChildShare);
            double femalesTo = adultsTo * to.FemaleShare;
            if (to.Size + amount > 1e-12) to.CoreEthnicShare = (to.CoreEthnicShare * to.Size + from.CoreEthnicShare * amount) / (to.Size + amount);   // 倍率排除⑧：復員も中核民族割合を運搬
            // 人1：復員兵の識字・闘争度も市民階級へ運搬（従軍で得た闘争度＝退役軍人の政治化）。MigrationCarriesHumanCapital=0 なら恒等（★ #17 で有効化＝既定1.0）。
            if (c.MigrationCarriesHumanCapital > 0 && to.Size + amount > 1e-12)
            {
                to.Literacy = (to.Literacy * to.Size + from.Literacy * amount) / (to.Size + amount);
                to.Militancy = BlendCarry(to.Militancy, to.Size, from.Militancy, amount, to.Size + amount, clamp: false);   // ★運搬1本化（段1・恒等）
            }
            from.Size -= amount;
            to.Size += amount;
            if (to.Size > 1e-12)
            {
                to.ChildShare = PoliticalQuantities.Clamp01(childrenTo / to.Size);
                to.OldShare = PoliticalQuantities.Clamp01(oldsTo / to.Size);   // 老年人数を保存（OldShare=0の世界では0のまま＝較正中立）
                to.FemaleShare = PoliticalQuantities.Clamp01(femalesTo / (adultsTo + amount));
            }
        }
        else
        {
            // 通常の階級間移動：男女比・子供比は移動者で加重混合
            double newSize = to.Size + amount;
            if (newSize > 1e-12)
            {
                // ★分子と分母の非対称（wave2b G11#2）：FemaleShare は成人内比率（徴兵経路の females=adults*FemaleShare と同じ定義）だが、
                //   ここは子供込みの Size で加重していた＝from/to の ChildShare が異なると女性人数の和がずれる。成人数で加重して揃える（同じChildShareなら旧式と厳密同値）。
                double adultsToF = to.Size * (1.0 - to.ChildShare), adultsFromF = amount * (1.0 - from.ChildShare);
                double adultsSumF = adultsToF + adultsFromF;
                to.FemaleShare = adultsSumF > 1e-12 ? (to.FemaleShare * adultsToF + from.FemaleShare * adultsFromF) / adultsSumF : to.FemaleShare;
                to.ChildShare = (to.ChildShare * to.Size + from.ChildShare * amount) / newSize;
                to.OldShare = (to.OldShare * to.Size + from.OldShare * amount) / newSize;   // 倍率排除⑧：老年比率も運搬（OldShare=0の世界では恒等＝較正中立）
                to.CoreEthnicShare = (to.CoreEthnicShare * to.Size + from.CoreEthnicShare * amount) / newSize;   // 倍率排除⑧：中核民族割合も運搬
                // 人1：階級間移動でも識字・闘争度を運搬（着地と同時の無償識字獲得を解消）。MigrationCarriesHumanCapital=0 なら from の属性を無視＝旧の to 据え置きと厳密恒等（★ #17 で有効化＝既定1.0）。
                if (c.MigrationCarriesHumanCapital > 0)
                {
                    to.Literacy = (to.Literacy * to.Size + from.Literacy * amount) / newSize;
                    to.Militancy = BlendCarry(to.Militancy, to.Size, from.Militancy, amount, newSize, clamp: false);   // ★運搬1本化（段1・恒等）
                }
            }
            from.Size -= amount;
            to.Size += amount;
        }
        // ★お金の運搬（3経路で1本＝支払いの一元化 段1-5・2026-09-28）：貯蓄・個人債務（2026-08-02）・国債の保有台帳（2026-09-21・徴兵は国内＝発行国は同じ）を
        //   1人あたりで按分して運ぶ。分母は動かす前の人数 size0＝旧の3枝（徴兵・復員・通常）と同じ式・同じ値（上の枝は Savings・Debt・BondHoldings を読まない）。
        double sav = from.Savings * (amount / size0);
        double debt = from.Debt * (amount / size0);
        double bondm = from.BondHoldings * (amount / size0);
        Pop.Carry(from, to, sav, debt, bondm);   // お金の3行（段4f＝Pop の中の運搬の1本）
    }
}
