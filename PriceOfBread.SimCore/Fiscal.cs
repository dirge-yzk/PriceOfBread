namespace PriceOfBread.SimCore;

/// <summary>
/// 国庫・財政（§9-F／§6.2）。所得税で国庫を満たし、連合（軍・官僚）の維持費を払う。
/// 税は可処分所得を削る＝中心ジレンマ（§3.1）。国庫が維持費を払えないと funding ratio が下がり、
/// 治安維持力が崩壊して革命の扉が開く（§6.2）。税（pop→国庫）と俸給（国庫→pop）は移転で基礎貨幣保存。
/// 国家レベル＝全地域の pop を集計する。
/// </summary>
public static class Fiscal
{
    // マジックナンバー命名（挙動不変・値据え置き）
    private const double GoldSuspendMinFundingRatio = 0.7;   // 金本位維持の最低funding比（下回ると強制離脱）※加入閾値 c.GoldStandardMinFunding とは別（ヒステリシス）
    private const double ForeignBondYieldRatioCap = 3.0;     // 対外債容量の利回り倍率クランプ上限（GovBondRate/BondBaseRate の上限）
    private const double FranchiseAdultMale = 0.7;           // 参政権レベル→市民運動基盤の重み（男子普通選挙）
    private const double FranchiseCensus = 0.25;             // 参政権レベル→市民運動基盤の重み（制限選挙）
    private const double DebtRatioUnbounded = 1e9;            // NominalGdp≈0 かつ債務ありのときの代替（極大）債務/GDP比

    public static void Step(World w, Coefficients c)
    {
        if (!w.FiscalEnabled) return;

        var pops = new List<Pop>();
        foreach (var r in w.Regions) pops.AddRange(r.Pops);

        // 行政力＝行政効率の移動平均。検地台帳（土地税）のアンロック＝一度できれば残る。
        // ★裁定2026-09-12（FIX2）：所得税の解禁ラッチを削除。閾値は所得税の天井（Government・同日廃止）の飽和点と同一の比・同一の値で、
        //   「ラッチが立つ瞬間＝天井が拘束しなくなる瞬間」＝同じ規則の端点の二重表現だった。しかも徴収本体（下の所得税）はラッチを
        //   一度も読まず、実体は「UIのスライダーだけを灰色にする」規則＝AI（SelfPlay）は同じ世界で自由に所得税を置いていた
        //   ＝AI/プレイヤー非対称の出どころ。行政の弱さは下の汚職漏れが額で表す。
        w.AdminPowerSmoothed += c.AdminPowerSmoothing * (w.AdminEfficiency - w.AdminPowerSmoothed);
        if (!w.LandTaxUnlocked && w.AdminPowerSmoothed >= c.LandTaxAdminGate) w.LandTaxUnlocked = true;

        // ★撤去2026-08-16（掃除キューC2-C5・棚卸C群）：汚職根絶＝清廉度ストック CleanGovernance の月次更新（debtRoom/corruptionRoom/統治スパン2本）を物理削除。
        //   汚職V2（(1−行政)×(1−監視noisy-or)＝World.EffectiveCorruption）が恒久ON裁定済み＝V2では清廉度ストックごと不読＝この更新は死骸だった。
        //   ⚠V2を切って旧式へ戻す道はこの撤去で消滅（裁定済み・20260814_汚職をサブシステムにする§10）。監視は法（報道/資格任用/議会予算権）で買う＝金で直接買う清廉度は無い。

        // 税収（§3.4）：所得税＋人頭税＋土地税。いずれも今月の所得/地代に課税し国庫へ。可処分所得を削る（§3.1）。
        // 所得税は率をそのまま課す＝**行政の弱さは下の汚職leakが額で表す**（査定機構が未熟な国は取り立てた金が国庫に届かない）。
        // 徴収は手持ち貯蓄まで（所得を消費し尽くした薄貯蓄の階級が負にならない）。専売は EconomyStep（購入時に上乗せ）。
        double revenue = 0;   // 倍率排除：土地税の landEff（行政力→徴収効率の別倍率）を撤去。行政不足の徴収漏れは下の汚職leakに一本化（所得税と同方式・二重計上の解消）
        // 参政権の範囲2法（作者裁定2026-08-16・SuffrageScopeGate>0のみ）：課税への同意（下の consent）が読む声を「政治的国民への算入率」で加重
        // ＝植民地格の州・本国の非中核popの徴税力に参政権と同じ梯子を適用（代表なくして課税なしの植民地版＝印紙法1765）。
        // 走査順は pops の構築（w.Regions→r.Pops）と同一＝添字が1:1対応。gate=0 では配列を作らず1命令も走らない＝厳密恒等（⚠既定は1.0＝点火2026-09-06）。
        double[]? scopeInc = null;
        if (c.SuffrageScopeGate > 0 && c.TaxConsentWeight > 0)
        {
            scopeInc = new double[pops.Count];
            int si = 0;
            foreach (var r in w.Regions) foreach (var p in r.Pops) scopeInc[si++] = InterestGroups.SuffrageInclusion(w, r, p);
        }
        // ★AUD-07（2026-09-03）：身分制免税の駆動量＝王権の取り分（PrivilegeFromCrownShare=0 の既定では**呼ばない**＝1命令も増えない恒等）。
        double crownShareForPrivilege = c.PrivilegeFromCrownShare > 0 && c.PrivilegeTaxDiscount > 0 ? PowerAffinity.Shares(w)[0] : 0;
        // ★支配度の一般化 段3（作者採用 §5・2026-09-07）：課税ベース＝国家区分＋在地区分×届け率（RuleDelivery）。在地の仲介者の取り分
        //   （在地区分×免税率×あるべき税）は納税者→その州の貴族へ移転（保存）し領主権の台帳へ。無統治・外国区分からは取れない。
        //   走査順は pops の構築（w.Regions→r.Pops）と同一＝添字が1:1対応。RuleSharesTaxBase=0 では配列を作らず1命令も走らない＝厳密恒等。
        double[]? ruleMul = null, ruleCut = null; Pop?[]? ruleLord = null;
        if (c.RuleSharesTaxBase > 0)
        {
            double g = c.RuleSharesTaxBase, deliver = 1.0 - Math.Clamp(c.PrivilegeTaxDiscount, 0, 1);
            ruleMul = new double[pops.Count]; ruleCut = new double[pops.Count]; ruleLord = new Pop?[pops.Count];
            int ri = 0;
            foreach (var r in w.Regions)
            {
                Pop? lord = null; foreach (var lp in r.Pops) if (lp.ClassId == PopClass.Noble && !lp.IsSoldier && lp.Size > 1e-9) { lord = lp; break; }
                double mul = Politics.RuleDelivery(r, c, g);
                double cut = r.Rule.Length == 4 ? g * Math.Clamp(r.Rule[1], 0, 1) * (1.0 - deliver) : 0;
                foreach (var p in r.Pops) { if (ri >= pops.Count) break; ruleMul[ri] = mul; ruleCut[ri] = lord is not null ? cut : 0; ruleLord[ri] = lord; ri++; }
            }
            if (ri != pops.Count) { ruleMul = null; ruleCut = null; ruleLord = null; }   // ⚠添字が合わない（集合の定義が変わった）ときは旧挙動へ倒す＝壊さない
        }
        for (int pi = 0; pi < pops.Count; pi++)
        {
            var p = pops[pi];
            // C-高4（史実検証2026-07-22）：身分制免税＝旧体制（絶対王政/専制/幕府）では閉鎖階級（貴族・聖職）の課税ベースが過少査定される
            //（taille réelle・ユンカーの Grundsteuer 免除。完全免除でなく割引＝vingtième は貴族にも課された）。立憲/共和/軍政では特権消滅。
            // PrivilegeTaxDiscount=0 で privBase=1.0＝乗算恒等（IEEE754で×1.0は厳密）。所得税と土地税の両方に掛ける（土地税だけ塞がないと同じ金が逃げ道を替えるだけ）。
            // ★AUD-07（2026-09-03）：政体ラベルの集合判定 → 王権の取り分に比例する連続量へ（PrivilegeFromCrownShare=0 で旧＝厳密恒等）。
            double privBase = Privileges.TaxAssessment(w, c, p, crownShareForPrivilege);   // ★特権台帳へ移設（2026-09-06・式はビット同一）
            // ★課税への同意（2026-08-04・作者指摘「政治がゲームに与える影響が小さすぎる」）。
            //   ── なぜ要るか ────────────────────────────────────────────────────
            //   実測（lawaudit2）＝参政権を「なし」から「男性普通」へ広げると **正統性−52%・歳入−13.7%・国力+1.4%**。
            //   政体を絶対王政→共和制にしても **国力−4.3%**（満足+14%と歳入−45%/技術−25%が相殺）。
            //   ＝**民主化が純粋なコスト**で、政治的変革が国力に何も返さない。これが「政治がゲームに効かない」の実体だった。
            //   史実は逆で、代議制の国家は絶対王政より**多く**取れた（英は仏の約2倍を1人あたりで徴収した。
            //   議会が同意した税だから払われた＝「代表なくして課税なし」の裏面）。
            //   ── 何を足したか ──────────────────────────────────────────────────
            //   **発言権のない者からは取りきれない**（脱税・隠匿・抵抗・徴収費）。係数は既存の <see cref="InterestGroups.FranchiseVoice"/>
            //   ＝議会の票と**同じ量**を使う＝新しい判断を持ち込まない（参政権の段と階級だけから導く）。
            //   ⚠**人頭税と土地税には掛けない**：どちらも**源泉で力ずくで取る税**で同意を要さない。
            //     人頭税は頭数に課す（だから逆進的で、専制の税だった）。土地税＝地租も、生産の現場で在地の仲介者
            //     （徴税請負・ザミンダール・ティマール）が取り立てるもので、納税者の申告にも同意にも依存しない。
            //     同意が要るのは**申告に基づく近代税**＝所得税と累進課税（英1799年の所得税は毎年議会の更新を要した）。
            //     この非対称がそのまま政治の選択になる＝参政権を絞る国は逆進的な人頭税と地租に頼るしかなく、
            //     広げた国は所得税という弾力的な税源を開く。**倍率でなく税源の切り替えとして効く。**
            //   ⚠実測（2026-08-04）：当初は土地税にも掛けたが、税源が地租と人頭税しかない農業専制国
            //     （ビルマ/エチオピア/アワド/アラビア/ネパール/シャム）の歳入が消えて忠実度が各−30〜−50pt へ崩れた。
            //     史実の彼らは弱い財政国家ではあったが徴税不能ではない＝**区分が粗すぎた**ための崩れで、係数の大小の話ではない。
            //   ⚠身分制免税（privBase）とは別物＝あちらは「査定の特権」（旧体制の貴族）・こちらは「徴収の同意」（払う側全員）。
            //   ⚠TaxConsentWeight=0 で consent=1.0＝乗算恒等（IEEE754で×1.0は厳密）＝休眠。
            //   参政権の範囲2法（2026-08-16）：scopeInc（政治的国民への算入率）が非nullなら声に乗算＝植民地州/非中核popは
            //   算入されるまで「発言権のない者」側＝同意が立たず所得税/累進が取りきれない。null（gate=0）＝現行の式そのまま。
            double consent = 1.0;
            if (c.TaxConsentWeight > 0)
            {
                double voice = InterestGroups.FranchiseVoice(p, w.Suffrage, c);
                if (scopeInc is not null) voice *= scopeInc[pi];
                consent = 1.0 - c.TaxConsentWeight * (1.0 - voice);
            }
            // 所得税：全階級の所得への定率課税。率そのまま課し、行政不足の徴収漏れは下の汚職leakが担う（土地税と同方式・2026-09-12 の天井廃止で経路は1本だけ）。
            double tax = Math.Min(w.TaxRate * Math.Max(0, p.Earned) * privBase * consent, Math.Max(0, p.Savings));
            if (c.PrivilegeLedgerEnabled > 0 && privBase < 1.0)   // ★特権台帳（段1・観測）：免税で抜けた分＝あるべき税×(1−privBase)（貯蓄上限は無視＝取り分の定義は査定の特権）
                Privileges.Record(w, Privileges.TaxExemption, (w.TaxRate * Math.Max(0, p.Earned) * consent + w.LandTax * Math.Max(0, p.LandRent)) * (1.0 - privBase));
            // 累進課税（A-1#3配線2026-06-24）：富裕層（近代エリート＝資本家/ブルジョワ・閉鎖階級＝貴族/聖職）の所得に**追加**課税＝再分配。所得税と同じ査定機構（漏れも同じ汚職leak）。富裕が強く抵抗（PolicyPreferences）。
            if (w.ProgressiveTax > 1e-9 && (p.IsModernElite || p.Closed) && !p.IsSoldier)
                tax += Math.Min(w.ProgressiveTax * Math.Max(0, p.Earned) * consent, Math.Max(0, p.Savings - tax));
            // 人頭税：頭数に一律＝行政不要で確実だが逆進的。閉鎖階級（貴族・聖職）は免除＝特権（大衆の不満の種）。兵士は国が雇い主＝対象外。
            if (w.PollTax > 1e-9 && !p.Closed && !p.IsSoldier)
                tax += Math.Min(c.PollTaxRate * w.PollTax * p.Size, Math.Max(0, p.Savings - tax));
            // 土地税：RGOの地代（封建貢租の受け手＝地主）に課税。率そのまま課し、行政不足の徴収漏れは汚職leakが担う（所得税と同方式）。
            if (w.LandTax > 1e-9 && p.LandRent > 0)
                tax += Math.Min(w.LandTax * p.LandRent * privBase, Math.Max(0, p.Savings - tax));   // C-高4：土地税も特権の過少査定（史実の免税の本丸＝taille réelle は土地税）。⚠同意(consent)は掛けない＝源泉で取る税
            if (ruleMul is not null)
            {
                double full = tax;
                tax = full * ruleMul[pi];
                double cut = full * ruleCut![pi];
                if (cut > 1e-12 && ruleLord![pi] is { } lord && !ReferenceEquals(lord, p))
                {
                    cut = Math.Min(cut, Math.Max(0, p.Savings - tax));
                    p.Savings -= cut; lord.Savings += cut; lord.Earned += cut;   // 仲介者の取り分（納税者→貴族＝保存）
                    if (c.PrivilegeLedgerEnabled > 0) Privileges.Record(w, Privileges.Seigneurial, cut);
                }
            }
            p.Savings -= tax;
            revenue += tax;
        }
        // 汚職：行政（官僚の役務）が足りないと税収の一部が国庫に届かず役人の懐へ消える（pop→官僚の移転＝保存）。
        // 官僚を増やし行政を買う（AdminBudget）ほど漏れが減る＝「軍事費の行政版」。行政財の無い世界は AdminEfficiency=1.0＝漏れなし。
        // ★汚職再設計C1（20260814_汚職をサブシステムにする【一部】.md・点火2026-08-15 ign_corr）：
        //   V2＝(1−行政)×(1−監視noisy-or)＝World.EffectiveCorruption の正準定義を直読み（定義の一本化）。
        // ★撤去2026-08-16（C2-C5）：旧式OFF枝（(1−行政)×(1−清廉度)＋civicGraft別項）を物理削除＝V2恒久ON裁定済みで到達不能の死骸だった。
        //   civicGraft（低国民意識×代議制の構造汚職の床）は V2 では「議会の予算権」（OversightV2 の第3因子）が担う＝別項を持たない。
        //   ⚠NationalConsciousnessCorruptionWeight 係数自体は存続＝読者が別に2箇所生きている（World.civicCarriesEthnic の二重計上回避ゲート・CrownParliament の国民意識蓄積チャネル）。
        double leak = revenue * c.CorruptionMax * w.EffectiveCorruption;
        w.LastTaxLeak = leak;   // 診断：汚職漏れの実額
        if (leak > 1e-12)
        {
            var bureaucrats = new List<Pop>();
            foreach (var p in pops) if (p.ClassId == PopClass.Bureaucrat && !p.IsSoldier) bureaucrats.Add(p);   // ClassId 8＝官僚
            if (bureaucrats.Count > 0)
            {
                // 官僚が実際に受け取れた分だけ歳入から漏れる（官僚popが存在してもSize≈0だとDistributeが未配分で返す＝
                // 旧コードは戻りを捨てて revenue-=leak だけ実行し、その額が誰の手にも渡らず消えていた＝seed25/27の貨幣保存違反の根因）。
                double delivered = leak - Distribute(bureaucrats, leak);
                revenue -= delivered;
                // ★特権台帳（段1・観測）：既得の官職の取り分＝官僚へ渡った汚職の分×身分の度合い（猟官1／資格任用0.5／専門0＝PowerShares.PrivilegeIntact と同じ物差し）
                if (c.PrivilegeLedgerEnabled > 0)
                    Privileges.Record(w, Privileges.VestedOffice, delivered * Privileges.EstateDegree(w));
                // ★8b：汚職所得は【官職という財産からの地代】＝LandRent へ記帳し rentier=rent/(rent+earned) を押し上げる
                //   →官僚が改革（自分の収入源の廃止）に反対する回路の入力＝制度疲労が国名なしで出る（doc §5）。
                //   ⚠金は上の Distribute（Savings）で移動済み＝ここは所得の**分類**の記帳のみ（金は二重に湧かない）。
                //   （撤去2026-08-16 C2-C5：V2恒久ONにつき CorruptionCoreV2>0 ガードを外し無条件化＝挙動恒等）
                if (delivered > 1e-12)
                {
                    double bsum = 0; foreach (var b in bureaucrats) bsum += b.Size;
                    if (bsum > 1e-9) foreach (var b in bureaucrats) b.LandRent += delivered * (b.Size / bsum);
                }
            }
        }
        w.Pay(TreasuryCat.Tax, revenue);

        // 十分の一税（教会税）：大衆（非特権・非兵士＝農民・労働者・職人）の所得の1/10を聖職者が受け取る。
        // 公教育に教育の担い手を奪われても食える恒常収入（要望）。pop→聖職者の移転＝国庫を通さず基礎貨幣保存。
        // 1934拡張E6 国教法：寛容化ほど十分の一税の徴収力が下がる（内心の自由=0.6・信仰の自由=0.3＝教会の税権が国家に制限される）。StateReligionGate=0で×1.0＝恒等。
        // 例外①（2026-07-28）：国家無神論＝教会税の全廃（ソ連1918政教分離令＝教会財産の没収・収入源の遮断）。
        double titheCollected = Privileges.CollectTithe(w, c, pops);   // ★特権台帳へ移設（2026-09-06・順序も式もビット同一）
        if (c.PrivilegeLedgerEnabled > 0) Privileges.Record(w, Privileges.Tithe, titheCollected);   // ★特権台帳（段1・観測）
        if (c.PrivilegeLedgerEnabled > 0) Privileges.StepMonth(w, c);   // ★特権台帳（段1）：今月の取り分を畳み保有年数を積む（月次・十分の一税の後）

        // 連合維持費（§6.2）＋広大な領土/人口の行政コスト（大国ほど重い）。払えた割合が funding ratio（<1 で治安維持力↓→政情不安）。
        double popSizeAll = 0; foreach (var p in pops) popSizeAll += p.Size;
        double coercionScale = popSizeAll / Math.Max(1.0, c.PoliticalReferencePop);
        // ★E-05段4（作者裁定2026-09-03「維持費は本国州にも必要なはずだ。植民地は遠く、それが高額なだけ、という形が正しい」）。
        //   ★実測＝**本国州は既に維持費を払っている**（この行の AdminCostPerPop×全州POP。⚠植民地格の州のPOPもここに入っている）。
        //     つまり植民地は「行政費」と「植民地維持費（ColonyUpkeepBase）」を**二重に**払っていた＝植民地専用の平場は要らない。
        //   ⇒ 足りないのは**距離**だけ。同じ行政費に首都からの距離の割増を掛ければ、格を見ずに作者裁定の形になる。
        //   ⚠形と係数は `Diplomacy.Projection`／E-05段1の分離判定と共有（DistanceWarDecay / DistanceNormKm）＝新しい数字を作らない。
        //   ⚠座標未登録(0,0)の州は割増なし＝中立（幻の遠距離を作らない）。UpkeepDistanceGeneral=0 で州別ループに入らない＝厳密恒等。
        double adminCost = c.AdminCostPerPop * popSizeAll;
        if (c.UpkeepDistanceGeneral > 0)
        {
            adminCost = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double rp = 0; foreach (var p in r.Pops) rp += p.Size;
                if (!(rp > 0)) continue;
                double mul = 1.0;
                var rc = Places.Coord(r.Name);
                if (!(rc.Lat == 0 && rc.Lon == 0))
                {
                    double km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
                    if (km > 0 && double.IsFinite(km)) mul = 1.0 + c.UpkeepDistanceGeneral * c.DistanceWarDecay * km / Diplomacy.DistanceNormKm;
                }
                adminCost += c.AdminCostPerPop * rp * mul;
            }
        }
        double upkeep = c.CoercionUpkeepRate * w.BaseCoercion * coercionScale + adminCost;

        // === 国債：維持費＋利払いを国庫で賄えない分を借金で埋める（保存：国内＝純移転／海外＝BaseMoney+CumulativeTradeFlow同時）===
        var capitalists = new List<Pop>();
        foreach (var p in pops) if (CapitalMarket.IsInvestor(p, c)) capitalists.Add(p);   // 国内の買い手＝資本家/近代エリート（NobleInvests>0で貴族も。既定0＝旧述語と厳密恒等）
        // R9（満期構造）：既往債は発行時クーポン(GovAvgCoupon)で利払い。毎月 BondRolloverFrac だけ現行市場金利GovBondRateへ乗り換わる。
        // BondRolloverFrac=1.0（既定）で GovAvgCoupon=GovBondRate にビット一致＝浮動永久債＝旧式 GovDebt*GovBondRate/12 と厳密一致。
        double rollover = System.Math.Clamp(c.BondRolloverFrac, 0, 1);
        w.GovAvgCoupon = (1.0 - rollover) * w.GovAvgCoupon + rollover * w.GovBondRate;
        double interest = w.GovDebt * w.GovAvgCoupon / Calendar.MonthsPerYear;   // 月次（クーポンは年率。Fiscal.Step は月次実行なので /12）
        // ★金建て債務の実質負担（銀本位doc G8・2026-08-13）＝**史実で銀本位国を金へ追い込んだ最大の力**。
        //   対外債務はロンドン建て＝金建てである（★インドの Home Charges・清の庚子賠款）。銀が下がれば、同じ利払いに
        //   より多くの自国通貨が要る＝⚠実質負担が膨らむ。膨らんだ分は【債権者が受け取る】——移転であって、湧いてはならない
        //   （下の foreignDebtFrac が同じ倍率で割り当てるので、国内分＋海外分＝interest が恒等的に閉じる）。
        //   GoldDebtBurdenGate=0 なら burden=1（★2026-08-29点火＝既定1＝金建て）。
        double goldDebtBurden = 1.0;
        if (c.GoldDebtBurdenGate > 0 && w.MonetaryLaw == MonetaryStandardLaw.Silver && w.WorldSilverPrice > 1e-9)
        {
            double refSilver = Catalog.RefPrices[Catalog.SilverId];
            double b = refSilver > 1e-9 ? refSilver / w.WorldSilverPrice : 1.0;
            b = Math.Clamp(b, 1.0, c.GoldDebtBurdenCap);
            if (!(b >= 1.0 && b <= c.GoldDebtBurdenCap)) b = 1.0;   // NaNは負担なしへ倒す（否定形＝Clampは NaN を消さない）
            goldDebtBurden = b;
        }
        double foreignShare = w.GovDebt > 1e-9 ? Math.Clamp(w.ForeignDebt / w.GovDebt, 0, 1) : 0;
        if (goldDebtBurden > 1.0) interest *= 1.0 + foreignShare * (goldDebtBurden - 1.0);   // 対外分だけが金建て＝国内債は自国通貨のまま
        // 裁量余剰（EMA）＝公共サービスの財源。**純税収から強制義務（維持費＋利払い）を引いた余り**から支弁する（funding修正の核心）：
        // 余剰の無い国（高治安維持力で維持費が歳入を食う France 等）は自然にサービス≈0＝赤字を作らない／余剰のある国（英）は歳入からサービスが機能。
        // 債務が増えると利払い↑→余剰↓→サービス自動縮小＝赤字スパイラルを利払い経由で自己制限（旧「×国庫(0で0)」の自己制限の正しい復活）。
        // 行政＝徴税の費用＝歳入への第一請求権（維持費と同列の強制義務）。余剰の前に引く＝軍需/福祉/インフラはその残りから引く
        // （同じ歳入を行政と軍需に二重に充てない）。行政自体は粗歳入から支弁（Simulation の AdminBudget）＝ブートストラップ・デッドロック解消（2026-06-22）。
        // 間接税の予算基数算入（監査2026-07-11 接2・2026-07-12ユーザー裁定で有効化＝「史実どおりの物理で良い」）：
        // 関税（四半期実収→月次換算）と専売（当月実収）は国庫に現金として届く一方、旧・予算基数（行政・軍装備・裁量余剰の請求権）は
        // 所得系の税のみで、19世紀の主柱歳入を関税に頼る国（米・露型）の行政・軍拡が構造的に過小だった。実収ベース＝現金の裏付けがある額のみ算入
        //（保存非接触＝計上の変更のみ）。輸出補助金（負の関税収入）は基数に入れない。
        // 有効化で高関税国（保守綱領の専制等）の行政予算が実勢どおり立つ＝回帰番人「教育で技術が分かれる」は前改革期（300Q）判定へ変更（露の遅い近代化は史実）。
        double indirectRevenue = Math.Max(0, w.LastTariffRevenue) / w.Coef.MonthsPerClearing;   // 国際市場は清算単位（四半期 or 月次清算）→月次換算（単位ヘルパー・2026-08-22）
        foreach (var r in w.Regions) if (r.State == RegionState.Active) indirectRevenue += r.LastStateMonopolyRevenue;
        double budgetBase = revenue + c.IndirectTaxInBudgetBase * indirectRevenue;
        double adminClaim = c.AdminBudgetFrac * w.AdminSpending * budgetBase;
        double militaryClaim = w.MilitarySpending * c.MilitaryRevenueShare * budgetBase;   // 軍需装備も粗歳入の第一請求権（Simulation の MilitaryBudget と対）＝軍を装備する費用。余剰（福祉/インフラ）はこの後の残り＝guns-vs-butter
        // ★S3a（2026-07-31）：Simulation 側で軍需予算を min(上限, 必要額) にするとき、**請求額もそろえる**。
        // 揃えないと「使いもしない軍事費が福祉・インフラを圧迫する」（請求は満額・実支出は必要額どまり）。
        // MilitaryBudgetNeedCapped=0 で従来＝厳密恒等。
        // ⚠必要額は Simulation 側と同一の全4段（EconomyStep.MilitaryProcurementNeed）。基本段だけで打ち切ると残余＝上位装備の財源が消える。
        if (c.MilitaryBudgetNeedCapped > 0)
        {
            double milNeed = 0;
            foreach (var r in w.Regions)
                if (r.State == RegionState.Active && r.Id == 0) milNeed += EconomyStep.MilitaryProcurementNeed(r, c);
            militaryClaim = Math.Min(militaryClaim, milNeed);
        }
        // ★撤去2026-08-16（C2-C5）：汚職根絶費の請求 antiCorruptionClaim を物理削除（V2点火2026-08-15で請求は既に遮断済み＝恒等0の死骸だった）。
        //   V2では根絶費の動詞ごと死ぬ（清廉度はもう読まれない＝金を燃やすだけの支出を許さない）。旧式へ戻す道は消滅（裁定済み）。
        double surplus = Math.Max(0, budgetBase - upkeep - interest - adminClaim - militaryClaim);
        w.LastRevenue = budgetBase; w.LastUpkeep = upkeep; w.LastInterest = interest;   // 診断（LastRevenue は Simulation の AdminBudget/MilitaryBudget の基数＝間接税込みへ）
        w.LastMilitaryClaim = militaryClaim; w.LastAdminClaim = adminClaim;          // 診断（予算表の支出内訳）
        w.FiscalCapacity = w.FiscalCapacity < 1e-9 ? surplus : w.FiscalCapacity + c.FiscalCapacitySmoothing * (surplus - w.FiscalCapacity);
        // === 借金ボタン（2026-06-22・自動国債発行を置換）：能動的借入のみ。1回押す＝GDP×割合を借入＋1スタック ===
        // 調達＝資本家の余剰貯蓄（クラウディングアウト）＋外資（開放度）＝旧・自動国債と同じ保存経路（国内は純移転・海外は CumulativeTradeFlow）。
        // プールを超えては借りられない（保存）。20スタックで頭打ち（もう借りても公共サービスが0%＝国が回らない）。返済でスタック減・返せねば のべくり。
        if (w.BorrowQueue > 0 && w.BorrowStacks < c.MaxBorrowStacks && w.BorrowCooldown <= 0)   // 予約キューを毎月1件処理（連続起債）。デフォルト後のクールダウン中は借りられない（信用喪失）
        {
            double borrowWant = Math.Max(0, w.Gdp) * c.BorrowFractionOfGdp;
            double capSavings = 0; foreach (var p in capitalists) capSavings += Math.Max(0, p.Savings);
            // 財6（資本開放度の脱・関税連動）：openness の関税連動を CapitalOpennessTariffWeight で連続化。既定1.0＝openness=Clamp(1−1.0×Tariff,0,1)＝旧 Clamp(1−Tariff,0,1) と代数的に厳密一致（数値中立）。
            // 0で関税と独立（19世紀の資本移動は関税と別＝高関税の米/露も外債を発行）→兌換性/信認ベースの開放度へ。中間で部分連動。
            // ★トリレンマを閉じる①（2026-08-13）：資本開放度を**法**から導出し、関税との連動を切る。
            //   旧＝関税の従属変数なので「関税は高いが資本は自由」（19世紀後半の独/米）が原理的に表現できなかった。
            //   規制でも0にしない（★密輸と同じ理由＝規制しても抜け道はあった）。既定 CapitalMobilityLawGate=0 で下の旧式のみ＝厳密恒等。
            double openness = c.CapitalMobilityLawGate > 0
                ? (w.CapitalControlLaw ? Math.Clamp(c.CapitalControlOpenness, 0, 1) : 1.0)
                : Math.Clamp(1.0 - c.CapitalOpennessTariffWeight * w.Tariff, 0, 1);
            double domesticPool = capSavings * c.BondAppetite;
            double foreignPool = openness * c.ForeignBondCapacity * Math.Max(0, w.Gdp) * Math.Clamp(w.GovBondRate / Math.Max(1e-9, c.BondBaseRate), 0, ForeignBondYieldRatioCap);
            // 財5（対外国債をモデル外の無限プールから閉鎖系へ）：ForeignBondFromInvestors=1 で foreignPool を他モデル国の資本家貯蓄から実引き当て。
            // 02断線走査2026-07-28：注入（MultiNationSim→World.ForeignBondInvestorPool）と保有国配分（CapitalMarket の外債レグ＝ForeignBondClaims 繰上げ＋利払い配当）は**実装済み**＝
            // 旧「ここに閉じて実装不能・統合担当の設計が要る」注記は陳腐化のため是正。既定1.0＝他国投資家プールから実引き当て。0なら無限プール（旧）。
            if (c.ForeignBondFromInvestors > 0.5 && w.ForeignBondInvestorPool >= 0.0)
                foreignPool = Math.Min(foreignPool, Math.Max(0.0, w.ForeignBondInvestorPool));   // 実引き当て：他国投資家プールを超えて対外債を発行できない（保存）
            double pool = domesticPool + foreignPool;
            double borrowed = Math.Min(borrowWant, pool);
            if (borrowed > 1e-9)
            {
                double foreignPortion = pool > 1e-9 ? borrowed * (foreignPool / pool) : 0;
                double domesticPortion = borrowed - foreignPortion;
                // ★保有台帳（2026-09-21裁定）：各資本家は**払った額だけ**国債を持つ（台帳は金を動かさない＝払う額の式は旧と同一）。
                //   ⚠門を `capSavings > 1e-9` から `> 0` へ：旧は 0<capSavings≤1e-9 のとき国内分（≤capSavings×BondAppetite）が誰からも引かれずに
                //   国庫へ湧き、持ち手のいない元本が立っていた。capSavings=0 なら domesticPool=0＝foreignPool/pool=1 で domesticPortion は厳密に0。
                if (domesticPortion > 0 && capSavings > 0)
                    foreach (var p in capitalists)
                    {
                        double paid = domesticPortion * (Math.Max(0, p.Savings) / capSavings);
                        p.Savings -= paid; p.BondHoldings += paid;   // 国内＝資本家貯蓄→国庫（純移転・クラウディングアウト）＋同額の保有
                    }
                w.CumulativeTradeFlow += foreignPortion;   // 海外＝対外資本流入（BaseMoney+CumulativeTradeFlow同時＝保存）
                if (c.ForeignLendingDebits > 0) w.PendingForeignBorrow += foreignPortion;   // ★§1-4：貸し手側の引き落とし予約（四半期末に MultiNationSim が他国資本家から対称に控除＝両建て）。0＝旧・片側記帳
                w.Pay(TreasuryCat.Borrow, borrowed); w.GovDebt += borrowed; w.ForeignDebt += foreignPortion;
                // R9：新発債を現行市場金利で既往クーポンへ加重平均で混ぜる（BondRolloverFrac<1の時のみ意味を持つ。既定1.0では翌月頭のrolloverでGovBondRateに上書きされ未読＝ビット一致）。残高0からの借入はdebtBefore=0で現行金利へ自然収束
                { double debtBefore = w.GovDebt - borrowed; w.GovAvgCoupon = (debtBefore * w.GovAvgCoupon + borrowed * w.GovBondRate) / System.Math.Max(1e-9, w.GovDebt); }
                w.BorrowStacks += 1.0;   // 1件＝1スタック（出費上限ペナルティ）
                w.BorrowQueue--;         // 実際に借りられた時だけ予約を消費（残りは翌月以降に順次処理＝連続起債。プール枯渇/上限/冷却中は保留）
            }
        }

        // === 不随意の赤字（維持費＋利払いを国庫で賄えない）→ 通貨切り下げ（インフレ税）/薄給で劣化 ===
        // 自動国債発行は廃止＝借入はボタンのみ。借りない国は赤字を切り下げ（全popの貯蓄を比例希薄化→国庫＝pop→国庫の純移転・通貨は刷らない）で埋める。
        // 金本位中は切り下げ封印＝underfunding（連合の薄給→funding↓→治安維持力↓→政情不安）で優雅に劣化＝規律の代償。緊縮（弱者救済削減＋逆進課税）が一般機構で帰結を出す。
        double deficit = Math.Max(0, upkeep + interest - Math.Max(0, w.Treasury));
        // 兌換可能性で切り下げ可能量を連続スケール（倍率排除②）：完全兌換(Conv=1)＝切り下げ封印（金の規律）／不換(Conv=0)＝赤字全額を切り下げ可。部分兌換は中間。
        double devalueRoom = deficit * (1.0 - w.Convertibility);
        // ★銀の段の軽い規律（SilverLightDiscipline・2026-08-21）：銀正貨国の切り下げ＝**改鋳（debasement）**は史実で日常だった
        //   （清の咸豊大銭・ムガル以来の含有量操作）。封印されるのは**銀行券の発行**の側＝正貨比率（BaseMoney/BroadMoney）ぶんは
        //   兌換中でも改鋳の窓が開く：room = deficit×(1 − Conv×(1−正貨比率))。純正貨経済（比率≈1）はほぼ全開・銀行券経済ほど封印が効く。
        //   ⚠新しい定数ゼロ（19-10差し戻しの根③＝切下げ封印の全世界閉鎖、の是正）。**live=1（2026-08-21点火）**・0に戻せば旧式と厳密恒等。金の段は従来どおり封印。
        if (c.SilverLightDiscipline > 0 && w.MonetaryLaw == MonetaryStandardLaw.Silver)
        {
            double specie = w.BroadMoney > 1e-9 ? w.BaseMoney / w.BroadMoney : 1.0;
            if (!(specie >= 0)) specie = 0; else if (specie > 1) specie = 1;   // NaNは0へ倒す（否定形＝ClampはNaNを消さない）
            devalueRoom = deficit * (1.0 - w.Convertibility * (1.0 - specie));
        }
        bool devalued = false;   // 通貨改革の発動ゲート（切下げゼロの連続月数）が読む
        if (devalueRoom > 1e-9)
        {
            double totalSav = 0; foreach (var p in pops) totalSav += Math.Max(0, p.Savings);
            // 財3（cadence整合・切り下げ上限）：DevaluationCapFrac は注釈上「四半期あたり」だが Fiscal.Step は月次実行＝そのまま毎月適用すると実効約14%/四半期に膨れる（1−(1−0.05)^3）。
            // FiscalQuarterlyGating=1 で月次上限を3か月ぶんに割り（÷3）、四半期あたりの累積が DevaluationCapFrac に整合。既定0＝割らず（÷1）＝現行と厳密一致（数値中立）。
            double devalCap = c.DevaluationCapFrac / (1.0 + c.FiscalQuarterlyGating * (Calendar.MonthsPerQuarter - 1));   // 既定0で ÷1（現行）／1で ÷3（月次を四半期整合）
            double devalue = Math.Min(devalueRoom, totalSav * devalCap);   // 四半期あたり有界（インフレ税の上限）。超過は underfunding で劣化
            if (devalue > 1e-9)
            {
                // 不換シニョリッジ（fiat-expansion-as-haircut-not-issue）：切り下げのうち FiatIssueFrac は pop から取らず新規発行＝マネーストック膨張(M↑)。
                // 既定 FiatIssueFrac=0 で issue=0＝全額ヘアカット(pop→国庫の純移転)＝現行恒等。発行ぶんは CumulativeFiatIssued に積み Invariants の expected に算入(鋳造と同型)。M↑→P↑は下の FiatInflationPressure が表現＝二重取りなし。
                double issue = devalue * c.FiatIssueFrac;
                double haircut = devalue - issue;
                if (haircut > 1e-12) foreach (var p in pops) p.Savings -= haircut * (Math.Max(0, p.Savings) / totalSav);
                w.Pay(TreasuryCat.Devalue, devalue); w.CumulativeDevaluation += devalue; devalued = true;
                w.CumulativeFiatIssued += issue;   // BaseMoney の正当な増加源（Invariants 算入）
                double broad = Math.Max(1.0, w.BroadMoney);
                w.FiatInflationPressure += devalue / broad;   // 紙幣増発＝インフレ圧（名目GDP↑→実質債務減）
            }
        }

        // ==== 通貨改革（2026-08-13・作者採用）====
        // 旧通貨を清算し、実力に合った新平価で建て直す。過去の切下げの記憶（CumulativeDevaluation）を消す代わりに、
        // **現に残っている預金を焼却**する。平価を高く取るほど焼却は増える＝英1925の旧平価復帰が自傷になる。
        // ⚠CumulativeDevaluation は保存則の恒等式（Invariants の4項）に入っていないので、減らしても番人は微動もしない。
        // ⚠焼却は pop→消滅（国庫へは渡さない）＝切下げ（pop→国庫の純移転）との本質的な違い。帳尻は
        //   CumulativeFiatIssued の**負方向**（＝不換の回収）で閉じ、意図額と実測の一致を先に検査する。
        // ⚠新平価は Convertibility>0 でしか効かない（World の rateTarget）＝「金本位に戻った日に履行される約束」。
        w.NoDevaluationMonths = devalued ? 0 : w.NoDevaluationMonths + 1;
        if (w.CurrencyReformCooldown > 0) w.CurrencyReformCooldown--;
        if (c.CurrencyReformEnabled > 0.5 && w.PendingCurrencyReform > 0)
        {
            double parity = w.PendingCurrencyReform;
            w.PendingCurrencyReform = 0;
            bool broken = w.NominalGdp > 1e-9 && w.CumulativeDevaluation / w.NominalGdp > c.CapitalFlightDistrustThreshold;
            if (w.MonetaryEnabled && !w.OnGoldStandard && broken
                && w.NoDevaluationMonths >= c.CurrencyReformCalmMonths
                && w.FiscalFundingRatio >= c.GoldStandardMinFunding
                && w.CurrencyReformCooldown <= 0)
            {
                double sav = 0; foreach (var p in pops) sav += Math.Max(0, p.Savings);
                // ①記憶の代価＝累積切下げの一定割合 ②平価の代価＝その平価を維持するのに要る貨幣の収縮
                double burnMem = c.CurrencyReformBurnShare * w.CumulativeDevaluation;
                double kp = parity * Math.Max(1e-9, w.PriceLevel);
                double burnParity = w.TotalMoney * Math.Max(0, 1.0 - 1.0 / Math.Max(1e-9, kp));
                double burn = Math.Min(Math.Max(burnMem, burnParity), sav * c.CurrencyReformBurnCap);
                // 自己制限：焼却は正貨カバー率を悪化させる（Mが減りLは減らせない）。改革が自分で金本位復帰の
                // ゲート（GoldCoverMin×GoldRejoinCoverMargin）を閉じては本末転倒なので、そこまでで止める。既存係数の再利用。
                double lo = c.GoldCoverMin * c.GoldRejoinCoverMargin;
                double loans = w.OutstandingLoans, tre = Math.Max(0, w.Treasury);
                if (loans > tre && lo < 1.0) burn = Math.Min(burn, Math.Max(0, w.TotalMoney - (loans - tre) / (1.0 - lo)));
                if (burn > 1e-12 && sav > 1e-9)
                {
                    double baseBefore = w.BaseMoney;
                    foreach (var p in pops) p.Savings -= burn * (Math.Max(0, p.Savings) / sav);
                    double drop = baseBefore - w.BaseMoney;   // ★実測で閉じる（意図額をそのまま台帳へ書かない）
                    if (Math.Abs(drop - burn) > 1e-8 * Math.Max(1.0, Math.Abs(baseBefore)))
                        throw new InvariantViolationException($"通貨改革：焼却の意図{burn}と基礎貨幣の減少{drop}が不一致");
                    w.CumulativeFiatIssued -= drop;   // 不換の**回収**＝基礎貨幣の正当な減少源（Invariants の expected が同額下がる）
                    double bite = burn / sav;
                    foreach (var p in pops) p.Militancy = Math.Min(1, p.Militancy + bite);   // 消えた分だけ怒る（新係数を作らない）
                }
                double forgiveRatio = w.NominalGdp > 1e-9 ? Math.Min(1.0, w.CumulativeDevaluation / w.NominalGdp) : 0;
                w.CumulativeDevaluation *= 1.0 - Math.Clamp(c.CurrencyReformForgiveShare, 0, 1);
                double legBefore = w.Legitimacy;
                w.Legitimacy = Math.Max(0, w.Legitimacy - c.CurrencyReformLegitimacyHit * forgiveRatio);
                w.Explain?.Add("Legitimacy", "通貨改革(預金を焼いて記憶を消す)", w.Legitimacy - legBefore);
                w.GoldParity = parity;
                w.CurrencyReformCooldown = c.CurrencyReformCooldownMonths;
                w.LogAdd($"{w.Year} {w.SelfPower?.Name}が通貨改革(新平価{parity:F2}・焼却{burn:F0})");
            }
        }

        // 利払い（国庫→債権者。国内＝資本家へ分配／海外＝対外流出。切り下げで国庫は満たされている）
        double interestPaid = Math.Min(Math.Max(0, w.Treasury), interest);
        // ★G8（2026-08-13）：金建て負担で膨らんだ利払いのうち海外へ行く割合。国内分は膨らまないので、分子だけに倍率が乗る
        //   ＝domInt + foreignInt が恒等的に interestPaid に閉じる（保存）。burden=1（既定）では旧式と厳密一致。
        double ForeignFrac(double share) => goldDebtBurden > 1.0
            ? share * goldDebtBurden / Math.Max(1e-12, 1.0 + share * (goldDebtBurden - 1.0))
            : share;
        double foreignDebtFrac = ForeignFrac(foreignShare);
        double foreignInt = interestPaid * foreignDebtFrac;
        w.Pay(TreasuryCat.Interest, -interestPaid);
        // ★保有台帳（2026-09-21裁定）：国内分の利払いは**保有に比例**。旧＝接5（2026-07-27点火）の「今の貯蓄比」＋頭数比の残り
        //   ＝「保有台帳を持たない現行設計での最良近似」だった（BondInterestSavingsWeighted は台帳で不要になり削除）。
        //   受け手＝国の全POP（階級が落ちて投資家でなくなっても、持っている国債の利子は受け取る）。持ち手不在＝国庫残置（保存）。
        double domInt = interestPaid - foreignInt;
        if (domInt > 1e-12) { double back = PayBondHolders(pops, domInt, 0); if (back > 0) w.Pay(TreasuryCat.Interest, back); }
        w.CumulativeTradeFlow -= foreignInt;   // 対外流出（保存）
        w.ForeignIntPaidThisQuarter += foreignInt;   // 02断線走査（2026-07-28）：CapitalMarketが債権国投資家へ配当（旧＝世界から消えるだけの片側記帳）

        // 維持費の支払い（国債で補充された国庫から）。
        double pay = Math.Min(Math.Max(0, w.Treasury), upkeep);
        w.Pay(TreasuryCat.Admin, -pay);
        // 財14（抑圧装置の維持費の帰属）：連合維持費（軍・警察・官僚の俸給）を CoercionUpkeepToSoldiers で分配先を傾斜。
        // 既定0＝全popへ頭数比例（現行）＝抑圧装置の維持費が全国民へヘリコプター移転され「重い常備軍が民を貧しくする」経路が名目税負担だけに希薄化。
        // 1で兵士pop・官僚(ClassId8)popへ傾斜（俸給を受け取る当事者）。受け手が不在なら国庫に残置（分配せず＝民へは還流しない）。中間で線形ブレンド。
        if (c.CoercionUpkeepToSoldiers > 1e-9 && pay > 1e-12)
        {
            var coercers = new List<Pop>();
            foreach (var p in pops) if (p.IsSoldier || (p.ClassId == PopClass.Bureaucrat && !p.Closed)) coercers.Add(p);   // 兵士＋官僚＝維持費の俸給を受け取る抑圧装置
            double coercerSize = 0; foreach (var p in coercers) coercerSize += p.Size;
            double targeted = pay * c.CoercionUpkeepToSoldiers;   // 傾斜配分に回す分
            double flat = pay - targeted;                          // 残りは従来どおり全popへ頭数比例
            if (flat > 1e-12) w.Pay(TreasuryCat.Admin, Distribute(pops, flat));
            w.Pay(TreasuryCat.Admin, Distribute(coercers, targeted));   // 受け手不在＝国庫残置（民へは還流しない＝維持費が経済から抜ける）
        }
        else
            w.Pay(TreasuryCat.Admin, Distribute(pops, pay));   // 既定＝全popへ頭数比例（現行と厳密一致）。受け手不在＝国庫残置（保存）
        w.FiscalFundingRatio = upkeep > 1e-9 ? pay / upkeep : 1.0;

        // ★撤去2026-08-16（C2-C5）：汚職根絶費の実支払い（acPay→官僚popへ俸給分配・TreasuryCat.AntiCorruption 記帳・LastAntiCorruptionCost 診断）を物理削除。
        //   V2点火2026-08-15で請求が恒等0＝この支払いは一度も走らない死骸だった。TreasuryCat.AntiCorruption の enum 値自体は台帳の添字互換のため残置（払い手ゼロ＝表示は0行フィルタで出ない）。

        // 減債：維持費の上に余剰があれば返済（健全国は減債＝スパイラル回避）。金利＝基礎＋×(債務/GDP)。
        if (w.GovDebt > 1e-9 && w.Treasury > upkeep)
        {
            double repay = Math.Min(w.GovDebt * c.BondRepayFrac, w.Treasury - upkeep);
            // ★AUD-09（監査2026-09-02 low・2026-09-03投入）：⚠ここは**元本**の按分なのに、上で作った**利払い**の按分
            //   （foreignDebtFrac＝金建て負担 goldDebtBurden で分子だけ膨らませた比）を流用していた＝分子と分母が別規則。
            //   元本の台帳 `ForeignDebt` は自国通貨建てなので、正しい按分は膨らませていない `foreignShare`。
            //   ⚠実害は銀本位/不換で金建て対外債を負う国＝元本を利払い比で過大に償却し `CumulativeTradeFlow` も同比で余計に流出する。
            //   ⚠goldDebtBurden≤1 では両者は恒等（foreignDebtFrac=foreignShare）＝差が出るのは金建て負担のある国だけ。0で旧式（利払いの比）。
            //   （2026-09-21：どちらの腕も比は下の shareNow＝償還時点の元本から取る＝0の腕も起債のある月は旧式とビット一致しない）
            // ★按分の比は**償還の時点の**元本から取る（2026-09-21・保有台帳の不変条件で露見）。上の foreignShare は月初＝起債の前の比で、
            //   同じ月に起債すると新発債の構成が按分に入らない：全額対外で借りた月に「国内分」を償還して ForeignDebt>GovDebt（国内元本が負）
            //   になり、持ち手がいなければ対外債務が国庫残置でタダで消え、いれば払っていない国内の持ち手へ対外の元本が渡っていた。
            //   利払い（上）は月初の残高に掛かる＝月初の比のままが正しい。起債の無い月は shareNow＝foreignShare（ビット同値）。
            double shareNow = Math.Clamp(w.ForeignDebt / w.GovDebt, 0, 1);
            double frForeign = repay * (c.DebtRepayUsesPrincipalShare > 0 ? shareNow : ForeignFrac(shareNow));
            double domBefore = w.GovDebt - w.ForeignDebt;
            w.Pay(TreasuryCat.DebtRepay, -repay); w.GovDebt -= repay; w.ForeignDebt = Math.Max(0, w.ForeignDebt - frForeign);
            // ★保有台帳（2026-09-21裁定）：国内分の償還は**保有に比例**して払い、同じ比で保有を減らす
            //   （旧＝貯蓄比で買い・頭数比で償還＝往復ごとに富裕な資本家から貧しい資本家へ富が移る分子と分母の非対称）。
            //   減らす額＝国内元本の実減（GovDebt−ForeignDebt の差分）＝台帳と元本が同じ量で動く。持ち手不在＝国庫残置（債権者の消えた国内債は実質帳消し・保存）。
            double back = PayBondHolders(pops, Math.Max(0, repay - frForeign), domBefore - (w.GovDebt - w.ForeignDebt));
            if (back > 0) w.Pay(TreasuryCat.DebtRepay, back);
            w.CumulativeTradeFlow -= frForeign;
            // 借金ボタンのスタックを返済分だけ減らす（1スタック＝GDP×割合相当）。返せた分だけ出費上限が回復＝のべくりの解除。
            double loanUnit = Math.Max(1e-9, Math.Max(0, w.Gdp) * c.BorrowFractionOfGdp);
            w.BorrowStacks = Math.Max(0, w.BorrowStacks - repay / loanUnit);
        }
        if (w.GovDebt <= 1e-9) w.BorrowStacks = 0;   // 完済＝スタック消滅
        w.SpendingCapFactor = Math.Max(0, 1.0 - w.BorrowStacks / Math.Max(1e-9, c.MaxBorrowStacks));   // 借入スタックの出費上限ペナルティ（福祉/インフラ予算に乗る・行政=徴税費/軍需は対象外）

        // === ソブリン・デフォルト：借入上限に張り付き返済できない＝信用が尽き支払不能→**債務不履行（致命的）** ===
        // 借金ボタンの逃げ道（革命による債務帳消し）が無くなった分、過剰債務は「破滅的なデフォルト」で決着する＝借りすぎた覇権/軍国が崩れて上位が入れ替わる。
        // 財3（cadence整合・四半期カウンタ）：BorrowCooldown（減算）・InsolvencyQuarters（加算）は名前どおり「四半期」単位のはずだが Fiscal.Step が月次実行のため月次で進み、
        // クールダウン「6年(=24Q)」が実効2年・「≒2年(=8Q)の支払不能」が8か月に3倍速化する。FiscalQuarterlyGating=1 で四半期の締め月(w.IsQuarterEndMonth)にのみ進める。
        // 既定0＝毎月進む（現行と厳密一致）。四半期の締め月フラグ＝World.IsQuarterEndMonth は Simulation の月次決算で設定済み（02断線走査2026-07-28：「要用意」の陳腐注記を是正）。
        bool quarterTick = c.FiscalQuarterlyGating <= 0.5 || w.IsQuarterEndMonth;   // 既定＝常に true（毎月＝現行）／gating時＝締め月のみ
        if (quarterTick)
        {
            if (w.BorrowCooldown > 0) w.BorrowCooldown--;
            if (w.BorrowStacks >= c.MaxBorrowStacks - 1e-9) w.InsolvencyQuarters++; else w.InsolvencyQuarters = 0;
        }
        else if (w.BorrowStacks < c.MaxBorrowStacks - 1e-9) w.InsolvencyQuarters = 0;   // 上限を離れたら即リセット（非締め月でも支払不能の連続性を保つ＝gating時のみ到達）
        if (w.InsolvencyQuarters >= c.DefaultTriggerQuarters)
        {
            // 債務消滅（請求権＝保存無影響）。致命的な代償＝兌換崩壊・**信用凍結（貸付消滅→工場が資本を得られず投資/工業化が崩落＝恐慌）**・正統性崩壊（革命リスク）・大衆の暴動・当面 借入不能。
            w.GovDebt = 0; w.ForeignDebt = 0; w.BorrowStacks = 0; w.SpendingCapFactor = 1.0;
            foreach (var hp in pops) hp.BondHoldings = 0;   // ★保有台帳（2026-09-21）：国が払うのを止めた割合（全額）だけ保有を消す＝損は持ち手（国庫へは何も戻らない・金は動かない）
            w.InsolvencyQuarters = 0; w.BorrowCooldown = c.DefaultCooldownQuarters; w.HadSovereignDefault = true; w.SovereignDefaultCount++;   // C-中9（2026-07-22）：回数も数える（恒久boolでは2度目以降の破産が制度疲労の窓を開けない）
            w.OnGoldStandard = false; w.GoldStandardSuspendedInCrisis = true;
            if (w.Bank is { } dbank) { dbank.Confidence = 0; dbank.Heat = 0; }   // 信用凍結＝Confidence0で次期以降 与信が自然収縮（恐慌）。**Loansは直接0にしない**（基礎貨幣=広義−与信 の保存を破るため）
            double legBeforeDefault = w.Legitimacy;
            w.Legitimacy = Math.Max(0, w.Legitimacy - c.DefaultLegitimacyHit);
            w.Explain?.Add("Legitimacy", "国家破産（デフォルト）の信認崩壊", w.Legitimacy - legBeforeDefault);   // 説明API（#63）
            foreach (var dp in pops) if (!dp.Closed && !dp.IsModernElite && !dp.IsSoldier) dp.Militancy = Math.Clamp(dp.Militancy + c.DefaultMilitancyHit, 0, 1);
        }
        // 財7（債務/GDPの単位整合）：NominalGdp は月次フロー、GovBondRate は年率（利払いを/12）。分母を DebtGdpAnnualized で年換算する（×12）。
        // 既定1.0＝分母を年換算（×12・2026-07-26点火）。0なら旧・月次GDP基準のまま。
        double gdpDenom = w.NominalGdp * (1.0 + c.DebtGdpAnnualized * (Calendar.MonthsPerYear - 1));   // 既定1.0で ×12（年換算）／0で ×1（月次のまま・旧）
        // [単位・B13] 債務/GDP＝**年次名目**（GovDebt÷(月次NominalGdp×12)・DebtGdpAnnualized=1）。金本位加入・国債金利の閾値はこの単位。⚠CapitalMarket/Credit は月次名目・Government/PolicyPreferences は月次実質＝同名で3種（#17で1本化）
        //   台帳（2026-09-11 #10）：PowerShares.cs の debtGrip＝月次名目の Clamp（物理・用途別に別＝触らない）／Runner milfund・hangtest の dGdp＝月次名目（計器）／App のヘッダー「対年GDP」＝この行と同じ年次名目（旧＝年次実質＝表示だけ別物差しだったのを統一）
        double dGdp = gdpDenom > 1e-9 ? w.GovDebt / gdpDenom : (w.GovDebt > 0 ? DebtRatioUnbounded : 0);   // #1 名目GDP基準＝デフレ国は実質債務が膨らみ兌換維持不能（負債デフレ→兌換停止）
        // ★AUD-09（監査2026-09-02 low・規律「比のガードは絶対値で書かない」）：分母の絶対値ガード（1e-9）だけでは、
        //   分母が**微小だが非ゼロ**のとき比が番兵を何桁も飛び越える（GDPが消えかけた残余国で万%＝WorstLossRate と同型）。
        //   ⚠**否定形**で書く＝NaN も同じ枝へ落ちる（`Math.Min` では NaN が素通りして GovBondRate を毒する）。
        //   ★有限値では厳密同値：読み手は兌換停止（閾値0.075）と国債金利（BondRateMax=0.30 でクランプ済み）の2つだけで、
        //     どちらも番兵 1e9 のはるか手前で飽和している＝**挙動は変わらず、報告される数字と NaN 経路だけが正される**。
        if (!(dGdp <= DebtRatioUnbounded)) dGdp = DebtRatioUnbounded;
        // 金本位の信用ゲート（資格＝コストでない）：低債務＋払える財政なら加入できる。加入中＝国債金利を信用で優遇（低金利）。
        // 加入中でも債務が膨らむ／維持費を払えないと正貨を守れず強制離脱（兌換停止＝信用暴落）。紙幣増発で赤字を埋める国は信用が無く加入不可。
        if (w.MonetaryEnabled)
        {
            // 正貨準備カバー率（convertibility-not-reserve-backed＋fx-peg防衛）：信用込み広義マネーに対する正貨(基礎貨幣)の比＝BaseMoney/BroadMoney。
            // 低いと金流出で兌換を守れず停止（1797英/1893米型の投機アタック）。既定GoldCoverMin=0で coverage<0 never・coverage>=0 always＝発火せず＝現行恒等。
            double coverage = w.BroadMoney > 1e-9 ? w.BaseMoney / w.BroadMoney : 1.0;
            if (w.OnGoldStandard)
            {
                // ★3値化2026-08-13：兌換の規律は**銀本位でも同じ足枷**＝MetallicStandardLaw（銀でも金でも真）を読む。休眠時は旧 GoldStandardLaw と厳密に一致。
                if (!w.MetallicStandardLaw || dGdp > c.GoldStandardSuspendDebt || w.FiscalFundingRatio < GoldSuspendMinFundingRatio || coverage < c.GoldCoverMin)
                {
                    w.OnGoldStandard = false; w.GoldStandardSuspendedInCrisis = w.MetallicStandardLaw;   // 自発離脱 or 危機での強制停止（準備枯渇＝兌換停止クライシス含む）
                    // GoldCoverMin有効化2026-07-10：強制停止を観測可能に（スカラーカウンタ＝batch頻度集計・Diplomacyログ＝年代記）。自発離脱（AIが志向を下げた）は数えない。
                    if (w.GoldStandardSuspendedInCrisis)
                    {
                        bool coverTrip = coverage < c.GoldCoverMin;
                        w.GoldSuspensions++; if (coverTrip) w.GoldCoverSuspensions++;
                        w.LogAdd($"{w.Year} {w.SelfPower?.Name}が兌換停止（金本位から強制離脱・{(coverTrip ? $"正貨準備カバー率{coverage:P0}<{c.GoldCoverMin:P0}" : dGdp > c.GoldStandardSuspendDebt ? "債務超過" : "財政不足")}）");
                    }
                }
            }
            // #125 振動抑制（2026-07-25）：再加入は離脱閾値より高いカバー率（×GoldRejoinCoverMargin＝シュミットトリガー）＋クールダウン明け。
            // Margin=1.0・Years=0（既定）で旧条件と厳密一致＝恒等。
            else if (w.MetallicStandardLaw && dGdp <= c.GoldStandardMaxDebt && w.FiscalFundingRatio >= c.GoldStandardMinFunding
                     && coverage >= c.GoldCoverMin * c.GoldRejoinCoverMargin)
            { w.OnGoldStandard = true; w.GoldStandardSuspendedInCrisis = false; }
        }
        // #2 Fisher：名目金利＝実質（基礎＋債務上乗せ）＋期待インフレ。高インフレ→高名目金利／デフレ→低名目金利だが実質債務は重い。
        // 財4：旧 Max(0,·) はデフレ側を遮断し「デフレ→低名目金利」のコメントと逆だった（1873-96大デフレのコンソル利回り低下3%→2.5%が再現不能＝負債デフレだけ効く片肺）。
        // 下限を FisherDeflationFloor でゲート。既定 -0.02＝デフレ側を軽く通す。0.0なら旧 Max(0,·) と厳密同値。
        double fisher = Math.Max(c.FisherDeflationFloor, w.ExpectedInflation) * c.FisherInflationWeight;
        // 金利の兌換割引を連続化（倍率排除②）：完全兌換(Conv=1)で GoldStandardRateDiscount(0.6)・不換(Conv=0)で1.0。部分兌換は中間＝信用プレミアムが連続。
        double convDiscount = 1.0 - w.Convertibility * (1.0 - c.GoldStandardRateDiscount);
        // 政策金利（バンクレート）が国債金利の床を押し上げる（引締め＝政府の借入も高くつく）。較正は InterestRate=0＝中立。
        // 信認の喪失（改修2026-06-28・金融改革①）：累積切り下げ/名目GDP比＝紙幣増発で赤字を埋めた履歴が国債金利に構造的に上乗せ（切り下げ常習国は債券価格が低迷＝信認の喪失を内生化）。
        double distrust = c.BondRateDistrustScale * (w.CumulativeDevaluation / Math.Max(1.0, w.NominalGdp));
        // 監査2026-07-05（F2）：債務二次項＝高債務での非線形スパイク（買い手希少性）。BondRateDebtQuad=0（既定）で現行の線形と厳密一致。
        double bondInside = c.BondBaseRate + c.BondRateDebtScale * dGdp + c.BondRateDebtQuad * dGdp * dGdp + fisher + distrust + c.PolicyRateBondWeight * w.InterestRate;
        // 政体フレーバー レバー3（2026-07-24）→第3波連続化（2026-07-28）：議会の信認割引＝議会の予算統制（ParliamentControl）が
        // 強いほど債権者の信認で金利が安い（英コンソル＝財政軍事国家論）。旧・政体ラベル二値→統制力の連続量へ（倍率排除＝ラベル不読・
        // DebtGdp年換算フリップ2026-07-26で再挑戦解禁の記録どおり）。BondRateRepresentationDiscount=0で×1.0＝厳密恒等。
        double reprDiscount = 1.0 - c.BondRateRepresentationDiscount * Math.Clamp(w.ParliamentControl, 0, 1);
        double bondPre = bondInside * convDiscount * reprDiscount;
        w.GovBondRate = Math.Min(c.BondRateMax, bondPre);
        // 説明API（#63）：国債金利の内訳＝各プレミアム×兌換割引−上限クランプ。Explain=nullでゼロコスト・戻り値不変。
        if (w.Explain is { } bl)
        {
            bl.Reset("BondRate");
            bl.Add("BondRate", "基礎金利", c.BondBaseRate * convDiscount);
            bl.Add("BondRate", "債務比プレミアム", c.BondRateDebtScale * dGdp * convDiscount);
            bl.Add("BondRate", "高債務の加速", c.BondRateDebtQuad * dGdp * dGdp * convDiscount);
            bl.Add("BondRate", "インフレ期待", fisher * convDiscount);
            bl.Add("BondRate", "信認喪失（切り下げ歴）", distrust * convDiscount);
            bl.Add("BondRate", "政策金利", c.PolicyRateBondWeight * w.InterestRate * convDiscount);
            bl.Add("BondRate", "金本位の信認割引", bondInside * (convDiscount - 1.0));
            bl.Add("BondRate", "議会の信認割引", bondInside * convDiscount * (reprDiscount - 1.0));
            bl.Add("BondRate", "上限クランプ", w.GovBondRate - bondPre);
        }

        // ※旧・教育費（国庫→pop一律）は廃止：教育のコストは教育財の市場が直接表現する
        //   （授業料＝popが教師/聖職者に払う／義務教育＝国庫が買い与える＝EconomyStep）。教育費スライダーは義務教育の財源率。

        // 社会保障の給付：国庫→大衆（労働者・農民）＝市場へ流れる（保存）。大衆人口に比例＝高コスト＝レッセフェールとの選択。
        if (w.SocialSecurity > 1e-9)
        {
            var masses = new List<Pop>();
            foreach (var p in pops) if (!p.Closed && !p.IsModernElite) masses.Add(p);
            double massSize = 0; foreach (var p in masses) massSize += p.Size;
            double welfareReq = c.WelfareCostRate * w.SocialSecurity * massSize;
            // funding修正：給付の上限を**裁量余剰(FiscalCapacity)**から（国庫は基礎義務に食われ常時0で福祉が餓死していた）。余剰の無い国は福祉も出せず赤字を作らない。
            double welfareCost = Math.Min(welfareReq, c.WelfareRevenueShare * w.FiscalCapacity * w.SpendingCapFactor);   // 借入スタックの出費上限ペナルティ（公共サービス）
            // 財1：福祉支出を国庫残高でクランプ（無記録の当座借越の防止）。利払い/維持費は Max(0,Treasury) で守られているが福祉/年金だけ守られておらず、残高が負に落ちても債務計上も利払いもなく翌月税収で黙って埋まっていた（記録されない政府短期紙幣の発行）。既定 WelfarePensionClampToTreasury=0＝現行の無制限（数値中立）／1＝残高までクランプ（財政規律）。
            // B4裁定（2026-07-31）：白黒スイッチの連続化＝0で無制限／1で残高まで完全クランプ／中間は当座借越を部分的に許す
            // （StateSpendTreasuryClamp と同型のブレンド。0.5超で発火という崖を廃止＝「途中の度合いも取れる」）。
            if (c.WelfarePensionClampToTreasury > 0)
                welfareCost = welfareCost * (1.0 - c.WelfarePensionClampToTreasury)
                            + Math.Min(welfareCost, Math.Max(0, w.Treasury)) * c.WelfarePensionClampToTreasury;
            w.LastWelfareCost = welfareCost;   // 診断
            w.WelfareFunding = welfareReq > 1e-9 ? welfareCost / welfareReq : 1.0;   // 充足率。約束した福祉が歳入を超えると減衰（財源不足）
            w.Pay(TreasuryCat.Welfare, -welfareCost);   // 国庫から支払い（不足分は赤字→国債→緊縮の既存フィードバックが SocialSecurity を抑える）
            w.Pay(TreasuryCat.Welfare, Distribute(masses, welfareCost));   // 受け手不在＝国庫残置（保存）
        }
        else w.WelfareFunding = 1.0;

        // 年金の給付（①年齢コホート Phase2）：国庫→老年人口（保存）。老年人口に比例＝高齢化が進むほど財政負担（ビスマルク年金1889〜）。年金なし（既定）＝level0で無効＝較正中立。
        double pensionLevel = w.Pension switch { ReformLevel.Full => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
        if (pensionLevel > 1e-9)
        {
            double oldSize = 0; foreach (var p in pops) if (!p.IsSoldier) oldSize += p.Size * p.OldShare;
            double pensionReq = c.PensionCostRate * pensionLevel * oldSize;
            double pensionCost = Math.Min(pensionReq, c.WelfareRevenueShare * w.FiscalCapacity * w.SpendingCapFactor);   // 福祉と同じ裁量余剰から（借入スタックの出費上限ペナルティ込み）
            // 財1：年金支出も国庫残高でクランプ（福祉と同型・無記録の当座借越の防止）。既定 WelfarePensionClampToTreasury=0＝現行の無制限（数値中立）／1＝残高までクランプ。
            if (c.WelfarePensionClampToTreasury > 0)   // B4裁定（2026-07-31）：福祉と同型の連続ブレンド
                pensionCost = pensionCost * (1.0 - c.WelfarePensionClampToTreasury)
                            + Math.Min(pensionCost, Math.Max(0, w.Treasury)) * c.WelfarePensionClampToTreasury;
            w.LastPensionCost = pensionCost;   // 診断
            w.Pay(TreasuryCat.Pension, -pensionCost);
            if (pensionCost > 0)   // 老年人口に比例して配分（年金は老人の所得＝消費に回る）。wave2b：分母を受け手と同じフィルタで作り直し（旧oldSizeはOldShare≤1e-9の pop も算入したまま
            {                       // 給付側だけ除外していた＝分子分母の非対称）、Distribute作法どおり端数は最後の受け手へ・受け手不在は国庫残置（保存）。
                var elderly = new List<Pop>(); var elderW = new List<double>();
                double elderSize = 0;
                // ★支配度 段4（2026-09-07）：給付の到達＝州の国家区分（RuleReach）。g=0 で重み1＝旧と同じ受け手・同じ順序（AllPops＝州順→pop順）＝ビット恒等。
                //   届かない分は国庫残置（受け手不在と同じ作法＝保存）。
                bool reachOn = c.RuleSharesReach > 0;
                foreach (var rg in w.Regions)
                {
                    if (rg.Seceded) continue;   // AllPops() と同じ集合
                    double reach = reachOn ? Politics.RuleReach(rg, c) : 1.0;
                    foreach (var p in rg.Pops) if (!p.IsSoldier && p.OldShare > 1e-9) { elderly.Add(p); elderW.Add(p.Size * p.OldShare * reach); elderSize += p.Size * p.OldShare; }
                }
                if (elderSize > 1e-9)
                {
                    double done = 0;
                    if (!reachOn)
                    {
                        for (int i = 0; i < elderly.Count - 1; i++)
                        {
                            double share = pensionCost * (elderly[i].Size * elderly[i].OldShare) / elderSize;
                            elderly[i].Savings += share; done += share;
                        }
                        elderly[^1].Savings += pensionCost - done;
                    }
                    else
                    {
                        for (int i = 0; i < elderly.Count; i++) { double share = pensionCost * elderW[i] / elderSize; elderly[i].Savings += share; done += share; }
                        if (pensionCost - done > 1e-12) w.Pay(TreasuryCat.Pension, pensionCost - done);   // 届かなかった給付＝国庫残置
                    }
                }
                else w.Pay(TreasuryCat.Pension, pensionCost);   // 受け手不在＝国庫残置
            }
        }
        else w.LastPensionCost = 0;

        // P9改（2026-07-19ユーザー裁定）：国民形成費レバーを廃止＝「選挙を望む層」が自費で国民運動を賄う（新聞・学校協会・政治結社・教練団）。
        // 需要＝識字した非特権・非兵士の運動意欲（参政権の獲得が進むと熱意半減＝獲得運動→維持出資）。支払い＝当該popの貯蓄→官僚pop
        // （教師・視学官・記者＝旧・国庫支払いと同じ受け手＝国内移転で保存）。実効レバー相当値＝実支払い/満額 を
        // World.NationBuildingSpending へ書き戻す＝CrownParliament の購入式（支出×識字×教育法×(1−汚職)…）は不変。プレイヤー/AI対称・レバーなし。
        // 1934拡張E3（2026-07-19）：国民運動の**財化**＝旧・貯蓄→官僚popの抽象拠出（新聞・学校協会・政治結社）を、
        // 実在の新聞財(134)の市場購入（pop予算→新聞生産者＝EconomyStep）に置換。実効レバー相当値は「新聞をどれだけ読めているか」＝
        // 非特権・非兵士popの size加重 NewsFill × 熱意（参政権獲得後は獲得運動→維持出資で半減＝旧式の zeal 継承）。
        // 新聞財の無い小シナリオは NewsFill=0＝NationBuildingSpending 0（旧フラグOFFと同じ帰結）。
        // 第3段3-6（2026-07-27）：NationBuildPurchaseMode=1（点火済み）を焼き込み＝else枝（v1：常時0）の到達不能を削除。
        {
            double franchise = w.Suffrage switch { Suffrage.Universal => 1.0, Suffrage.AdultMale => FranchiseAdultMale, Suffrage.Census => FranchiseCensus, _ => 0.0 };
            double zeal = 1.0 - 0.5 * franchise;
            double newsW = 0, newsSz = 0;
            foreach (var p in pops)
            {
                if (p.IsSoldier || p.Closed) continue;   // 特権身分・兵士は市民運動の担い手でない
                newsW += p.NewsFill * p.Literacy * p.Size; newsSz += p.Size;   // 識字×読めた率＝「実際に新聞を読む国民」の割合（NewsFillは識字比需要の充足なので識字を再掛けして規模化）
            }
            w.NationBuildingSpending = newsSz > 1e-9 ? Math.Clamp(newsW / newsSz * zeal, 0, 1) : 0;
        }
        // POP動詞①（階級への優遇/弾圧）は廃止（2026-07-20ユーザー指示＝税で表現済みで冗長）。弾圧の概念は国内危機（Politics.CheckFactionRevolt→CrisisThroughLawPath＝通知のみ・ボタンなし）へ移行＝⚠2026-09-08に無血鎮圧の枝は撤去・2026-09-11に弾圧/譲歩の択も撤去＝対処は法改正か内戦だけ。
    }

    /// <summary>★国債の保有台帳（2026-09-21裁定）：cash を**保有に比例**して配り、principal ぶん保有を同じ比で減らす。
    /// 台帳は金を動かさない＝配る金は呼び手が国庫から出した cash だけ。端数は最後の持ち手へ（<see cref="Distribute"/> と同じ作法＝配った額の和が cash に厳密一致）。
    /// 持ち手不在（保有の和が0）なら cash をそのまま返す＝呼び手が国庫残置して保存則を守る。</summary>
    private static double PayBondHolders(List<Pop> pops, double cash, double principal)
    {
        double total = 0; Pop? last = null;
        foreach (var p in pops) if (p.BondHoldings > 0) { total += p.BondHoldings; last = p; }
        if (last is null) return cash;
        double done = 0;
        foreach (var p in pops)
        {
            if (!(p.BondHoldings > 0) || ReferenceEquals(p, last)) continue;
            double share = cash * (p.BondHoldings / total);
            p.Savings += share; done += share;
        }
        last.Savings += cash - done;
        if (principal > 0)
        {
            double keep = principal < total ? 1.0 - principal / total : 0.0;   // 同じ比で減らす＝和は total−principal（負にしない）
            foreach (var p in pops) p.BondHoldings *= keep;
        }
        return 0;
    }

    /// <summary>★保有台帳の国外振替（2026-09-21）：持ち手が**発行国の外へ出る**（割譲・分離建国・越境移民・招聘）とき、その保有 h を
    /// 発行国の**対外債**へ振り替える＝請求権は発行国に対するまま残り、持ち手の国の対外債権（ForeignBondClaims）になる。
    /// ★史実：国債は発行国との契約で、持ち手の主権が変わっても債務者は変わらない（フランクフルト条約1871＝ドイツはフランス国債を
    ///   一切継承せず、アルザス＝ロレーヌの住民が持つフランス国債はフランスへの請求権のまま国外保有になった）。
    /// ⚠金は動かない・GovDebt も不変＝国内元本 GovDebt−ForeignDebt が h 減り、台帳も h 減る＝不変条件が厳密に保たれる
    ///   ＝POPに「どの国の国債か」の次元を持たせずに済む（1本のスカラーは持ち手が発行国の中にいる間だけ有効）。
    /// ⚠PrevForeignDebt も同額上げる＝CapitalMarket の外債レグがこの増分を「新規の対外借入」として全債権国へ按分し直さない（持ち手の国へ直接付け替え済み）。</summary>
    internal static void ExportBondHoldings(Pop p, double h, World issuer, World holderNation)
    {
        if (!(h > 0)) return;
        p.BondHoldings -= h;
        issuer.ForeignDebt += h; issuer.PrevForeignDebt += h;
        holderNation.ForeignBondClaims += h;
    }

    /// <summary>★保有台帳の起票（2026-09-21）：国内債があるのに台帳が空の国（開始盤面の初期国債＝仏の米独立戦争債務／台帳導入前のセーブ）に、
    /// **起債の購入と同じ規則**（投資家の貯蓄比）で保有を起こす。金は動かさない（既に在る請求権の持ち主を記すだけ）。
    /// 台帳が立っている国・国内債0（検査の許容内）の国では何もしない＝通常の走では構築時に1回だけ効く。投資家の貯蓄が0なら全POPの頭数比（<see cref="Distribute"/> と同じ最後の受け皿）。</summary>
    internal static void SeedBondHoldings(World w)
    {
        double dom = w.GovDebt - w.ForeignDebt, held = 0, invSav = 0, size = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops) { held += p.BondHoldings; size += p.Size; if (CapitalMarket.IsInvestor(p, w.Coef)) invSav += Math.Max(0, p.Savings); }
        if (held > 0 || !(dom > 0) || BondLedger.Balanced(w, held)) return;   // 検査が割れない（端数の残差だけ）なら起票しない＝台帳入りのセーブはビット不変
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
                p.BondHoldings += dom * (invSav > 0 ? (CapitalMarket.IsInvestor(p, w.Coef) ? Math.Max(0, p.Savings) / invSav : 0) : (size > 0 ? p.Size / size : 0));
    }

    /// <summary>頭数比例の分配。受け手不在（空リスト・総人口≈0）なら配れなかった額を返す＝呼び手が国庫残置して保存則を守る
    /// （貨幣保存リーク修正 2026-07-07：国内債権者が州割譲で転出し capitalists が空になると、利払い/減債の国内分が国庫から引かれたまま無配分で消えていた）。</summary>
    private static double Distribute(List<Pop> pops, double amount)
    {
        if (amount <= 1e-12) return 0;
        if (pops.Count == 0) return amount;
        double total = 0;
        foreach (var p in pops) total += p.Size;
        if (total <= 1e-9) return amount;

        double done = 0;
        for (int i = 0; i < pops.Count - 1; i++)
        {
            double share = amount * (pops[i].Size / total);
            pops[i].Savings += share;
            done += share;
        }
        pops[^1].Savings += amount - done;
        return 0;
    }
}
