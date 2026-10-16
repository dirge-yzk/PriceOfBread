namespace PriceOfBread.SimCore;

/// <summary>
/// 国庫・財政（§9-F／§6.2）。所得税で国庫を満たし、利子・福祉・年金を払う（連合〔軍・官僚〕の給与は兵力と行政の購買＝2026-09-30 (135) に維持費の人頭割りを撤去）。
/// 税は可処分所得を削る＝中心ジレンマ（§3.1）。国庫が国家の支払いの請求を払えないと国家の支払い率（FiscalFundingRatio）が下がり、
/// 治安維持力が崩壊して革命の扉が開く（§6.2）。税（pop→国庫）と俸給（国庫→pop）は移転で基礎貨幣保存。
/// ★支払いの規則は1本（作者裁定 F3・2026-09-28 段2-1）：率 r は月初に Simulation が決め（全ての国家の支払いの請求÷払える額）、
///   ここの利子・福祉・年金にも同じ r を掛ける（旧＝利払い→維持費→福祉/年金→減債の順に国庫の残りで切る滝）。
/// 国家レベル＝全地域の pop を集計する。
/// </summary>
public static class Fiscal
{
    // マジックナンバー命名（挙動不変・値据え置き）
    private const double GoldSuspendMinFundingRatio = 0.7;   // 金本位維持の最低funding比（下回ると強制離脱）※加入閾値 c.GoldStandardMinFunding とは別（ヒステリシス）
    private const double ForeignBondYieldRatioCap = 3.0;     // 対外債容量の信認の倍率クランプ上限（BondBaseRatePerYear/GovBondRatePerYear の上限＝2026-09-29 に向きを是正）
    private const double FranchiseAdultMale = 0.7;           // 参政権レベル→市民運動基盤の重み（男子普通選挙）
    private const double FranchiseCensus = 0.25;             // 参政権レベル→市民運動基盤の重み（制限選挙）
    private static readonly RatioToGdpYear DebtToGdpYearUnbounded = new(DebtRatioUnbounded);   // 債務÷年GDP の番兵（型つきの版）
    private const double DebtRatioUnbounded = 1e9;            // NominalGdpPerMonth≈0 かつ債務（か切り下げ）ありのときの代替（極大）の比（債務÷年GDP・切り下げの速さ÷年GDP）

    /// <param name="spare">月初の払える額（借入と切り下げの後）−国家の支払いの請求（Simulation が r と同時に求める）。＞0＝ r＝1 の月の余剰＝減債の原資。
    /// 借入と切り下げは月初に Simulation が払える額の側へ入れて取る（借入＝<see cref="Borrow"/>・2026-09-28）＝ここでは借りも取りもしない。単体で呼ぶ試験の既定0＝減債もしない。</param>
    /// <param name="devalued">この月の月初に Simulation が取った切り下げの額（国庫の台帳の区分 Devalue へ入った額）＝国債の金利の信認の喪失が読む「直近の切り下げの速さ」（2026-09-29）。単体で呼ぶ試験の既定0＝この月は切り下げていない。</param>
    public static void Step(World w, Coefficients c, double spare = 0, double devalued = 0)
    {
        if (!w.FiscalEnabled) return;
        double payRate = w.FiscalFundingRatio.Value;   // 国家の支払い率 r（月初に Simulation が決めた1本）
        // 今月の借入の門（月初の Borrow と同じ1本＝利払い÷歳入が限界の下）。下で LastInterestPerMonth・LastRevenue を今月の量へ書き換える前に読む＝不履行の計数が読む（下）
        bool underLimit = w.LastInterestPerMonth < c.MaxInterestShareOfRevenue * w.LastRevenuePerMonth;
        // ★A2-2（2026-09-26）：GDP（州×生産者の全件走査）はこの1回だけ（旧＝実質3か所・名目4か所で呼ぶたびに走査）。
        //   この関数が書くのは国庫・貯蓄・国債と台帳だけ＝産出・州・物価水準は動かない＝下の読み手は getter とビット単位で同じ値を読む。
        double gdp = w.GdpPerMonth.Value, nominalGdp = w.NominalOf(gdp);

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
        //   ★(351) 州×POP のなぞりは**この 1 本**（下の pops と、POP ごとの並行の列〔算入率・領主の取り分〕を同じ 1 周で作る＝旧は pops を平らにする周と並行の配列を埋める周が別で、
        //     添字が合わなければ黙って旧挙動へ倒していた〔集合の定義がずれると黙って外れる〕）。列は pops と同じ長さ・同じ並びで同時に伸びる＝添字合わせが構造で消える。集合＝国家会計の集合（絞らない）。
        var scan = TaxScan.Of(w, c);
        var pops = scan.Pops;
        List<double>? scopeInc = scan.ScopeInc, ruleMul = scan.RuleMul, ruleCut = scan.RuleCut; List<Pop?>? ruleLord = scan.RuleLord;
        // ★AUD-07（2026-09-03）：身分制免税の駆動量＝王権の取り分（PrivilegeFromCrownShare=0 の既定では**呼ばない**＝1命令も増えない恒等）。
        double crownShareForPrivilege = c.PrivilegeFromCrownShare.Value > 0 && c.PrivilegeTaxDiscount.Value > 0 ? PowerAffinity.Shares(w)[0] : 0;
        // ★支配度の一般化 段3（作者採用 §5・2026-09-07）：課税ベース＝国家区分＋在地区分×届け率（RuleDelivery）。在地の仲介者の取り分
        //   （在地区分×免税率×あるべき税）は納税者→その州の貴族へ移転（保存）し領主権の台帳へ。無統治・外国区分からは取れない。
        //   走査順は pops の構築（w.Regions→r.Pops）と同一＝添字が1:1対応。RuleSharesTaxBase=0 では配列を作らず1命令も走らない＝厳密恒等。
        //   （列 ruleMul・ruleCut・ruleLord は上の 1 周で作る＝RuleSharesTaxBase=0 では作らない）
        // ★支払いの一元化 段3-4（2026-09-30・B6・作者裁定 Q4＝処理の一元化 段4d）：**POP の義務は1本の率**。税（所得・累進・人頭・土地）と十分の一税と領主の取り分を
        //   POP ごとに全部集め、r_p＝min(1, 払える額÷請求の和)（払える額＝max(0, 貯蓄)）を全ての請求に同じに掛ける＝優先順位を作らない（国家の支払い率と同じ規則の POP 版）。
        //   旧＝所得→累進→人頭→土地の順に残りの貯蓄で切り、十分の一税は税の後の残りから（別の関数 Privileges.CollectTithe・国家が教会に優先）。
        //   払えない分は請求が立たない（Q4＝不足は記録しない）。十分の一税の受け手（聖職）がいなければ十分の一税の請求は立たない（受け手不在＝払い手に残る）。
        //   ★所得の記帳は義務の間で揃える（C15 の確かめ）＝どの義務も払い手の総所得（Earned）を減らさない（所得税と同じ＝総所得から払う）・受け取りは受け手の所得
        //   （聖職の十分の一税・領主の取り分＝教会と領主の役務の対価＝国庫の税が官僚の俸給として所得になるのと同じ）。
        double titheRate = c.TitheRate.Value * Privileges.TitheDegree(w, c);   // 国教法の変調は TitheDegree の1本（旧 CollectTithe の1行目と同じ式）
        List<Pop>? clergy = null;
        if (titheRate > 1e-9)
        {
            clergy = [];
            double clergySize = 0;
            foreach (var p in pops) if (p.ClassId == PopClass.Clergy && p.Size > 1e-9) { clergy.Add(p); clergySize += p.Size; }
            if (!(clergySize > 1e-9)) clergy = null;
        }
        double titheCollected = 0;
        // ★2026-09-30 段4g：払い手と自分の請求（1本の率を掛けた後）を集め、走査の後に移し方の1本で取る（払い手の重み＝自分の請求・払い手ごとの1本の率＝Q4）。
        //   旧＝走査の中で貯蓄を手で引き（税＋取り分＋十分の一税）、国庫は合計を1回で足した（国庫の片側の記帳）・十分の一税は按分の本体を直に呼んだ。
        var taxPayers = new List<Pop>(); var taxClaim = new List<double>();
        var tithePayers = new List<Pop>(); var titheClaim = new List<double>();
        for (int pi = 0; pi < pops.Count; pi++)
        {
            var p = pops[pi]; if (p.IsCriminal) continue;   // ★(366) 犯罪者は申告せず頭数も把握されない＝所得税・累進・人頭税・土地税・十分の一税の請求を立てない（盗んだ金に所得税が掛かるのを防ぐ）。犯罪者の POP が無い世界では偽＝恒等
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
            // ★段3-1（2026-09-29）：課税の基数＝**確定した前の月の所得**（LastEarnedPerMonth＝EconomyStep が月初に写す前の月の Earned の全部）。旧＝今月の Earned＝
            //   月次の財政より後に入る受け取り（輸出代金と運賃〔国際市場の清算は全ての国の月次決算の後〕・領主の取り分）が翌月の頭で0へ戻り、課税されなかった。
            //   史実の所得税も前の期の所得を査定した（英1799 ピットの所得税・1842 ピールの再導入）。
            double tax = w.TaxRate.Value * Math.Max(0, p.LastEarnedPerMonth.Value) * privBase * consent;   // 請求（査定の全額）＝払うのは下の1本の率 r_p を掛けた額
            if (c.PrivilegeLedgerEnabled > 0 && privBase < 1.0)   // ★特権台帳（段1・観測）：免税で抜けた分＝あるべき税×(1−privBase)（貯蓄上限は無視＝取り分の定義は査定の特権）
                Privileges.Record(w, Privileges.TaxExemption, (w.TaxRate.Value * Math.Max(0, p.LastEarnedPerMonth.Value) * consent + w.LandTax * Math.Max(0, p.LandRentPerMonth.Value)) * (1.0 - privBase));
            // 累進課税（A-1#3配線2026-06-24）：富裕層（近代エリート＝資本家/ブルジョワ・閉鎖階級＝貴族/聖職）の所得に**追加**課税＝再分配。所得税と同じ査定機構（漏れも同じ汚職leak）。富裕が強く抵抗（PolicyPreferences）。
            if (w.ProgressiveTax > 1e-9 && (p.IsModernElite || p.Closed) && !p.IsSoldier)
                tax += w.ProgressiveTax * Math.Max(0, p.LastEarnedPerMonth.Value) * consent;
            // 人頭税：頭数に一律＝行政不要で確実だが逆進的。閉鎖階級（貴族・聖職）は免除＝特権（大衆の不満の種）。兵士は国が雇い主＝対象外。
            if (w.PollTax > 1e-9 && p.IsCommoner)
                tax += c.PollTaxRate * w.PollTax * p.Size;
            // 土地税：RGOの地代（封建貢租の受け手＝地主）に課税。率そのまま課し、行政不足の徴収漏れは汚職leakが担う（所得税と同方式）。
            if (w.LandTax > 1e-9 && p.LandRentPerMonth.Value > 0)
                tax += (w.LandTax * p.LandRentPerMonth * privBase).Value;   // C-高4：土地税も特権の過少査定（史実の免税の本丸＝taille réelle は土地税）。⚠同意(consent)は掛けない＝源泉で取る税
            // 仲介者の取り分（支配度 段3・休眠 RuleSharesTaxBase=0）＝国庫へ届く分と在地の貴族の分に割る（査定の全額に掛ける）。
            double cut = 0; Pop? lord = null;
            if (ruleMul is not null)
            {
                double full = tax;
                tax = full * ruleMul[pi];
                if (ruleLord![pi] is { } lp && !ReferenceEquals(lp, p) && full * ruleCut![pi] > 1e-12) { cut = full * ruleCut[pi]; lord = lp; }
            }
            double tithe = clergy is not null && Politics.IsMassPop(p) && p.LastEarnedPerMonth.Value > 0 ? (titheRate * p.LastEarnedPerMonth).Value : 0;   // 十分の一税の請求（大衆の前の月の所得＝所得税と同じ基数）
            double claims = tax + cut + tithe;
            if (!(claims > 0)) continue;   // 否定形＝NaN の請求は立てない
            double held = Math.Max(0, p.Savings.Value);
            double rp = held >= claims ? 1.0 : held / claims;   // 1本の率（払える額が足りれば 1＝全額）
            tax *= rp; cut *= rp; tithe *= rp;
            if (lord is not null && cut > 0)
            {
                // 仲介者の取り分（納税者→貴族＝移し方の1本・所得にも書く）。受け手は重み1（在地の貴族1人＝頭数に依らない＝旧と同じ）。
                double moved = World.Transfer((w, [p]), (w, [lord], [1.0]), cut, income: true);
                if (c.PrivilegeLedgerEnabled > 0) Privileges.Record(w, Privileges.Seigneurial, moved);
            }
            if (tax > 0) { taxPayers.Add(p); taxClaim.Add(tax); }
            if (tithe > 0) { tithePayers.Add(p); titheClaim.Add(tithe); }
            titheCollected += tithe;
            revenue += tax;
        }
        // 汚職：行政（官僚の役務）が足りないと税収の一部が国庫に届かず役人の懐へ消える（pop→官僚の移転＝保存）。
        // 官僚を増やし行政を買う（AdminBudget）ほど漏れが減る＝「軍事費の行政版」。行政財の無い世界は AdminEfficiency=1.0＝漏れなし。
        // ★汚職再設計C1（20260814_汚職をサブシステムにする【一部】.md・点火2026-08-15 ign_corr）：
        //   V2＝(1−行政)×(1−監視noisy-or)＝World.EffectiveCorruption の正準定義を直読み（定義の一本化）。
        // ★撤去2026-08-16（C2-C5）：旧式OFF枝（(1−行政)×(1−清廉度)＋civicGraft別項）を物理削除＝V2恒久ON裁定済みで到達不能の死骸だった。
        //   civicGraft（低国民意識×代議制の構造汚職の床）は V2 では「議会の予算権」（OversightV2 の第3因子）が担う＝別項を持たない。
        //   ⚠NationalConsciousnessCorruptionWeight 係数自体は存続＝読者が別に2箇所生きている（World.civicCarriesEthnic の二重計上回避ゲート・CrownParliament の国民意識蓄積チャネル）。
        double leak = revenue * c.CorruptionMax.Value * w.EffectiveCorruption;
        w.LastTaxLeak = leak;   // 診断：汚職漏れの実額
        double delivered = 0;
        if (leak > 1e-12)
        {
            var bureaucrats = new List<Pop>();
            foreach (var p in pops) if (p.ClassId == PopClass.Bureaucrat && !p.IsSoldier) bureaucrats.Add(p);   // ClassId 8＝官僚
            if (bureaucrats.Count > 0)
            {
                // 官僚が実際に受け取れた分だけ歳入から漏れる（官僚popが存在してもSize≈0なら受け手不在＝移し方の1本が動かさない＝漏れない）。
                // ★2026-09-30 段4g：納税者→官僚の移し方の1本（払い手の重み＝自分の税の請求＝受け手が2つ〔官僚と国庫〕の1つ目）。旧＝国庫の取り分から按分の本体を直に呼んだ。
                delivered = World.Transfer((w, taxPayers), (w, bureaucrats, null), leak, fromWeights: taxClaim);
                // ★特権台帳（段1・観測）：既得の官職の取り分＝官僚へ渡った汚職の分×身分の度合い（猟官1／資格任用0.5／専門0＝PowerShares.PrivilegeIntact と同じ物差し）
                if (c.PrivilegeLedgerEnabled > 0)
                    Privileges.Record(w, Privileges.VestedOffice, delivered * Privileges.EstateDegree(w));
                // ★8b：汚職所得は【官職という財産からの地代】＝LandRent へ記帳し rentier=rent/(rent+earned) を押し上げる
                //   →官僚が改革（自分の収入源の廃止）に反対する回路の入力＝改革への抵抗が国名なしで出る（doc §5・★(373) 制度疲労のスカラーは物理削除）。
                //   ⚠金は上の Distribute（Savings）で移動済み＝ここは所得の**分類**の記帳のみ（金は二重に湧かない）。
                //   （撤去2026-08-16 C2-C5：V2恒久ONにつき CorruptionCoreV2>0 ガードを外し無条件化＝挙動恒等）
                if (delivered > 1e-12)
                {
                    double bsum = 0; foreach (var b in bureaucrats) bsum += b.Size;
                    if (bsum > 1e-9) foreach (var b in bureaucrats) b.LandRentPerMonth += new MoneyPerMonth(delivered * (b.Size / bsum));
                }
            }
        }
        // 国庫の取り分＝税の請求の和−官僚へ漏れた分（納税者→国庫の移し方の1本・払い手の重み＝自分の税の請求＝受け手の2つ目）。動いた額が純税収。
        revenue = World.Transfer((w, taxPayers), (w, null, null), revenue - delivered, TreasuryCat.Tax, fromWeights: taxClaim);

        // 十分の一税（教会税）：大衆（非特権・非兵士＝農民・労働者・職人）の所得の TitheRate を聖職者が受け取る。
        // 公教育に教育の担い手を奪われても食える恒常収入（要望）。pop→聖職者の移転＝国庫を通さず基礎貨幣保存。
        // 1934拡張E6 国教法：寛容化ほど十分の一税の徴収力が下がる（内心の自由=0.6・信仰の自由=0.3＝教会の税権が国家に制限される）。StateReligionGate=0で×1.0＝恒等。
        // 例外①（2026-07-28）：国家無神論＝教会税の全廃（ソ連1918政教分離令＝教会財産の没収・収入源の遮断）。
        // ★段3-4（2026-09-30）：徴収は上の税と同じ1本の率（旧 Privileges.CollectTithe の徴収の走査を畳んだ）。貯蓄は按分の本体（頭数・端数は最後の聖職）・所得も同じ1行で。
        //   ★2026-09-30 段4g：大衆→聖職の移し方の1本（払い手の重み＝自分の十分の一税の請求・受け手は頭数・所得にも書く）。
        if (clergy is not null && titheCollected > 0) titheCollected = World.Transfer((w, tithePayers), (w, clergy, null), titheCollected, income: true, fromWeights: titheClaim);
        if (c.PrivilegeLedgerEnabled > 0) Privileges.Record(w, Privileges.Tithe, titheCollected);   // ★特権台帳（段1・観測）
        if (c.PrivilegeLedgerEnabled > 0) Privileges.StepMonth(w, c);   // ★特権台帳（段1）：今月の取り分を畳み保有年数を積む（月次・十分の一税の後）

        // ★費用の側＝維持費の人頭割りを撤去（2026-09-30 (135)・(109) §9-2 と (127) §10-1 の申し送り）。
        //   旧＝連合維持費（§6.2）＝治安維持費（CoercionUpkeepRate×治安維持力の基礎×人口÷基準人口）＋行政費の人頭割り（AdminCostPerPop×州の人口×首都からの距離の割増）を、
        //   役務の購買とは別の強制義務として請求し、兵士と官僚の④の持分へ給与の上乗せとして払った（段3-2）＝同じ役務の二重・三重の支払い：
        //   行政は官僚の役務を市場で買う行（行政の予算→EconomyStep の行政の購買＝官僚の給与×人数）が既にあり、治安維持力は 2026-06-23 から兵数×即応度×係数
        //   ＝軍そのもの（給与 SoldierPayBudget・装備 MilitaryBudget に続く3度目の課金＝2026-08-01 導出の精査で確認済み・人口の2乗に比例）。
        //   しかも名目の定数×頭数（物価と賃金に非連動）＝1人あたりの産出の低い大国ほど重い（分子と分母の非対称：費用は頭数・GDP は産出）
        //   ＝1784 の年GDP 比 清 7.3%（人頭割り 3.7・治安維持 3.6）・西 7.1・蘭 5.1・英 3.0（史実の文官の費用は年GDP の約1%・清の国家の支出は全部で2〜3%）。
        //   ⇒ 費用は役務の購買の1本（官僚と兵士の給与×人数）。作者裁定 2026-09-03（E-05段4）「維持費は本国州にも必要なはずだ。植民地は遠く、それが高額なだけ」は
        //     行政の購買の州ごとの距離の割増（AdminDistanceMul＝旧と同じ形と係数）＝遠い州の官僚は同じ働きに割増の俸給が要る
        //     （予算と単価の両方に掛ける＝充足は変わらず金額だけ増える＝下の行政の請求も同じ割増の平均）。

        // === 国債：利払い（借入は月初の Borrow＝払える額の側・2026-09-28）===
        // R9（満期構造）：既往債は発行時クーポン(GovAvgCouponPerYear)で利払い。毎月 BondRolloverFracPerMonth だけ現行市場金利GovBondRatePerYearへ乗り換わる。
        // BondRolloverFracPerMonth=1.0（既定）で GovAvgCouponPerYear=GovBondRatePerYear にビット一致＝浮動永久債＝旧式 GovDebt*GovBondRatePerYear/12 と厳密一致。
        double rollover = PoliticalQuantities.Clamp01(c.BondRolloverFracPerMonth.Value);
        w.GovAvgCouponPerYear = (1.0 - rollover) * w.GovAvgCouponPerYear + rollover * w.GovBondRatePerYear;
        double interest = Calendar.PerYearToPerMonth(w.GovDebt.Value * w.GovAvgCouponPerYear.Value);   // 月次（クーポンは年率。Fiscal.Step は月次実行なので /12）
        // ★金建て債務の実質負担（銀本位doc G8・2026-08-13）＝**史実で銀本位国を金へ追い込んだ最大の力**。
        //   対外債務はロンドン建て＝金建てである（★インドの Home Charges・清の庚子賠款）。銀が下がれば、同じ利払いに
        //   より多くの自国通貨が要る＝⚠実質負担が膨らむ。膨らんだ分は【債権者が受け取る】——移転であって、湧いてはならない
        //   （下の foreignDebtFrac が同じ倍率で割り当てるので、国内分＋海外分＝interest が恒等的に閉じる）。
        //   ★2026-09-26：旗 GoldDebtBurdenGate（点火2026-08-29＝既定1＝金建て）を焼き込み（x-01）。
        double goldDebtBurden = 1.0;
        if (w.MonetaryLaw == MonetaryStandardLaw.Silver && w.WorldSilverPrice > 1e-9)
        {
            double refSilver = Catalog.RefPrices[Catalog.SilverId];
            double b = refSilver > 1e-9 ? refSilver / w.WorldSilverPrice : 1.0;
            b = Math.Clamp(b, 1.0, c.GoldDebtBurdenCap);
            if (!(b >= 1.0 && b <= c.GoldDebtBurdenCap)) b = 1.0;   // NaNは負担なしへ倒す（否定形＝Clampは NaN を消さない）
            goldDebtBurden = b;
        }
        double foreignShare = w.GovDebt.Value > 1e-9 ? PoliticalQuantities.Clamp01(w.ForeignDebt / w.GovDebt) : 0;
        if (goldDebtBurden > 1.0) interest *= 1.0 + foreignShare * (goldDebtBurden - 1.0);   // 対外分だけが金建て＝国内債は自国通貨のまま
        // 裁量余剰（EMA）＝公共サービスの財源。**純税収から強制義務（利払い・行政・軍需の請求）を引いた余り**から支弁する（funding修正の核心）：
        // 余剰の無い国は自然にサービス≈0＝赤字を作らない／余剰のある国（英）は歳入からサービスが機能。
        // 債務が増えると利払い↑→余剰↓→サービス自動縮小＝赤字スパイラルを利払い経由で自己制限（旧「×国庫(0で0)」の自己制限の正しい復活）。
        // 行政＝徴税の費用＝歳入への第一請求権（利払いと同列の強制義務）。余剰の前に引く＝軍需/福祉/インフラはその残りから引く
        // （同じ歳入を行政と軍需に二重に充てない）。行政自体は粗歳入から支弁（Simulation の AdminBudget）＝ブートストラップ・デッドロック解消（2026-06-22）。
        // 間接税の予算基数算入（監査2026-07-11 接2・2026-07-12ユーザー裁定で有効化＝「史実どおりの物理で良い」）：
        // 関税（四半期実収→月次換算）と専売（当月実収）は国庫に現金として届く一方、旧・予算基数（行政・軍装備・裁量余剰の請求権）は
        // 所得系の税のみで、19世紀の主柱歳入を関税に頼る国（米・露型）の行政・軍拡が構造的に過小だった。実収ベース＝現金の裏付けがある額のみ算入
        //（保存非接触＝計上の変更のみ）。輸出補助金（負の関税収入）は基数に入れない。
        // 有効化で高関税国（保守綱領の専制等）の行政予算が実勢どおり立つ＝回帰番人「教育で技術が分かれる」は前改革期（300Q）判定へ変更（露の遅い近代化は史実）。
        double indirectRevenue = Math.Max(0, w.LastTariffRevenuePerClearing) / w.Coef.MonthsPerClearing;   // 国際市場は清算単位（四半期 or 月次清算）→月次換算（単位ヘルパー・2026-08-22）
        foreach (var r in w.Regions) if (r.State == RegionState.Active) indirectRevenue -= r.StateSpent[(int)TreasuryCat.StateMonopoly];   // 当月の専売益（負の支出＝収入。2026-09-28 に旧の別持ち LastStateMonopolyRevenue を畳んだ＝塩などの独占レントの国庫の取り分も同じ区分）
        double budgetBase = revenue + c.IndirectTaxInBudgetBase * indirectRevenue;
        double adminClaim = c.AdminBudgetFrac.Value * w.AdminSpending * budgetBase * AdminDistanceMean(w);   // 州ごとの行政の予算（人口の比×距離の割増＝Simulation）の和と同じ量（2026-09-30 (135)）
        // ★(155) 軍の装備の請求＝その月の月初の予算（Simulation の軍事費の吸い込み＝器から給与の後の装備〔全4段の必要額まで〕）が書いた World.LastMilitaryClaimPerMonth を読む
        //   ＝軍を装備する費用も粗歳入の第一請求権（余剰〔福祉/インフラ〕はこの後の残り＝guns-vs-butter）。使い切れない器は請求しない＝余剰を圧迫しない（旧 S3a の「請求額もそろえる」と同じ向き）。
        //   旧＝ここで 軍事費×MilitaryRevenueShare×基数 を全4段の必要額で切って書いた（Simulation と同じ式の写し＝2本）。給与と質（幹部の給与）は旧の給与と同じく外（LastSoldierPayPerMonth＝財政バッファは算入しない・R3安定形）。
        double militaryClaim = Math.Max(0, w.LastMilitaryClaimPerMonth.Value);
        // ★撤去2026-08-16（C2-C5）：汚職根絶費の請求 antiCorruptionClaim を物理削除（V2点火2026-08-15で請求は既に遮断済み＝恒等0の死骸だった）。
        //   V2では根絶費の動詞ごと死ぬ（清廉度はもう読まれない＝金を燃やすだけの支出を許さない）。旧式へ戻す道は消滅（裁定済み）。
        double surplus = Math.Max(0, budgetBase - interest - adminClaim - militaryClaim);
        if (Instruments.On) Instruments.Revenue(w, revenue, indirectRevenue);   // 計器（読むだけ）：歳入の内訳
        w.LastRevenuePerMonth = new MoneyPerMonth(budgetBase); w.LastInterestPerMonth = new MoneyPerMonth(interest);   // 診断（LastRevenue は Simulation の AdminBudget/MilitaryBudget の基数＝間接税込みへ）
        w.LastAdminClaimPerMonth = new MoneyPerMonth(adminClaim);          // 診断（予算表の支出内訳）（★(155) LastMilitaryClaimPerMonth の書き手は月初の予算＝Simulation）
        w.FiscalCapacityPerMonth = new MoneyPerMonth(w.FiscalCapacityPerMonth.Value < 1e-9 ? surplus : w.FiscalCapacityPerMonth.Value + c.FiscalCapacitySmoothing * (surplus - w.FiscalCapacityPerMonth.Value));
        // （借入は 2026-09-28 に月初の Simulation の払える額の側へ移した＝Borrow（下）。払える額の中身の順＝税・歳入→借入→それでも足りない分だけ切り下げ（作者裁定 2026-09-28 (b)）。
        //   旧＝ここ（月末）で借り、借りた金は翌月の払える額になった＝その月は切り下げが先に払えない額を埋め（切り下げの97〜99% は借りられた四半期）、
        //   借入は切り下げの残りだけ＝紙幣の増発が信用より先に立っていた。）

        // （切り下げ＝インフレ税は 2026-09-28 に月初の Simulation の国家の支払い率へ移した＝払える額の側。旧＝2-1 でここの原資が「その月に払えなかった請求の全額」になり、
        //   請求を r で切って払った上に同じ額を全POPから国庫へ移していた＝不足を量で減らした上で他人からも取る二重（作者裁定 F4・思想⑨）。）

        // ==== 通貨改革（2026-08-13・作者採用）====
        // 旧通貨を清算し、実力に合った新平価で建て直す。過去の切下げの記憶（CumulativeDevaluation）を消す代わりに、
        // **現に残っている預金を焼却**する。平価を高く取るほど焼却は増える＝英1925の旧平価復帰が自傷になる。
        // ⚠CumulativeDevaluation は保存則の恒等式（Invariants の4項）に入っていないので、減らしても番人は微動もしない。
        // ⚠焼却は pop→消滅（国庫へは渡さない）＝切下げ（pop→国庫の純移転）との本質的な違い。帳尻は
        //   CumulativeFiatIssued の**負方向**（＝不換の回収）で閉じ、意図額と実測の一致を先に検査する。
        // ⚠新平価は Convertibility>0 でしか効かない（World の rateTarget）＝「金本位に戻った日に履行される約束」。
        // （発動の門が読む切下げゼロの連続月数 NoDevaluationMonths は、切り下げと同じ月初の1か所＝Simulation が数える＝この月の値）
        if (w.CurrencyReformCooldown > 0) w.CurrencyReformCooldown--;
        // ★切下げの記憶が壊れている（不信＝累積切下げ／名目GDP が閾値超）。読み手は2つ＝改革の発動と、下の金本位への再加入（2026-09-27）。
        //   改革した月は改革の前の値のまま＝再加入はその翌月から（改革→兌換の回復の順）。
        bool broken = nominalGdp > 1e-9 && w.CumulativeDevaluation / nominalGdp > c.CapitalFlightDistrustThreshold;
        if (c.CurrencyReformEnabled > 0.5 && w.PendingCurrencyReform > 0)
        {
            double parity = w.PendingCurrencyReform;
            w.PendingCurrencyReform = 0;
            if (w.MonetaryEnabled && !w.OnGoldStandard && broken
                && w.NoDevaluationMonths >= c.CurrencyReformCalmMonths
                && w.FiscalFundingRatio >= c.GoldStandardMinFunding
                && w.CurrencyReformCooldown <= 0)
            {
                double sav = 0; foreach (var p in pops) sav += Math.Max(0, p.Savings.Value);
                // ①記憶の代価＝累積切下げの一定割合 ②平価の代価＝その平価を維持するのに要る貨幣の収縮
                double burnMem = c.CurrencyReformBurnShare.Value * w.CumulativeDevaluation;
                double kp = parity * Math.Max(1e-9, w.PriceLevel);
                double burnParity = (w.TotalMoney * Math.Max(0, 1.0 - 1.0 / Math.Max(1e-9, kp))).Value;
                double burn = Math.Min(Math.Max(burnMem, burnParity), sav * c.CurrencyReformBurnCap.Value);
                // 自己制限：焼却は正貨カバー率を悪化させる（Mが減りLは減らせない）。改革が自分で金本位復帰の
                // ゲート（GoldCoverMin×GoldRejoinCoverMargin）を閉じては本末転倒なので、そこまでで止める。既存係数の再利用。
                double lo = c.GoldCoverMin.Value * c.GoldRejoinCoverMargin;
                double loans = w.OutstandingLoans.Value, tre = Math.Max(0, w.Treasury.Value);
                if (loans > tre && lo < 1.0) burn = Math.Min(burn, Math.Max(0, w.TotalMoney.Value - (loans - tre) / (1.0 - lo)));
                if (burn > 1e-12 && sav > 1e-9)
                {
                    // ★2026-09-30 段4g：焼却＝全POP（貨幣の保有＝貯蓄の比・d5）→世界の外（不換の発行者＝Outside.Fiat）の移し方の1本＝不換の**回収**
                    //   （基礎貨幣の正当な減少源＝台帳 CumulativeFiatIssued が動いた額だけ下がる＝Invariants の expected が同額下がる）。
                    //   旧＝貯蓄を手で引き、基礎貨幣の前後の差を実測して台帳へ書き、意図との不一致を例外で検査した（移し方の1本は動いた額をそのまま台帳へ書く＝構造で閉じる）。
                    burn = World.Transfer((w, pops), (null, null, null), burn, outside: World.Outside.Fiat, fromHeld: sav);   // ★(351) 払い手の保有の合計は上の sav と同じ順の同じ和（間に何も書かない）
                    double bite = burn / sav;
                    foreach (var p in pops) p.Militancy = Math.Min(1, p.Militancy + bite);   // 消えた分だけ怒る（新係数を作らない）
                }
                double forgiveRatio = nominalGdp > 1e-9 ? Math.Min(1.0, w.CumulativeDevaluation / nominalGdp) : 0;
                w.CumulativeDevaluation *= 1.0 - PoliticalQuantities.Clamp01(c.CurrencyReformForgiveShare.Value);
                double legBefore = w.Legitimacy;
                w.Legitimacy = Math.Max(0, w.Legitimacy - c.CurrencyReformLegitimacyHit * forgiveRatio);
                w.Explain?.Add("Legitimacy", "通貨改革(預金を焼いて記憶を消す)", w.Legitimacy - legBefore);
                w.GoldParity = parity;
                w.CurrencyReformCooldown = c.CurrencyReformCooldownMonths;
                w.LogAdd($"{w.Year} {w.SelfPower?.Name}が通貨改革（新平価{parity:F2}・焼却{burn:F0}）");
            }
        }

        // 利払い（国庫→債権者。国内＝資本家へ分配／海外＝対外流出）＝請求×r（旧＝国庫の残りで切る滝の先頭）。
        double interestPaid = interest * payRate;
        // ★G8（2026-08-13）：金建て負担で膨らんだ利払いのうち海外へ行く割合。国内分は膨らまないので、分子だけに倍率が乗る
        //   ＝domInt + foreignInt が恒等的に interestPaid に閉じる（保存）。burden=1（既定）では旧式と厳密一致。
        double ForeignFrac(double share) => goldDebtBurden > 1.0
            ? share * goldDebtBurden / Math.Max(1e-12, 1.0 + share * (goldDebtBurden - 1.0))
            : share;
        double foreignDebtFrac = ForeignFrac(foreignShare);
        double foreignInt = interestPaid * foreignDebtFrac;
        // ★保有台帳（2026-09-21裁定）：国内分の利払いは**保有に比例**。旧＝接5（2026-07-27点火）の「今の貯蓄比」＋頭数比の残り
        //   ＝「保有台帳を持たない現行設計での最良近似」だった（BondInterestSavingsWeighted は台帳で不要になり削除）。
        //   受け手＝国の全POP（階級が落ちて投資家でなくなっても、持っている国債の利子は受け取る）。持ち手不在＝国庫残置（保存）。
        // ★2026-09-30 段4g：利払いは2つの脚の移し方の1本＝国内の脚（国庫→持ち手・保有の重み）と対外の脚（国庫→世界の外・国境の出入りの台帳）。
        //   旧＝国庫から全額を引き、持ち手へ配れなかった分を戻し（Fiscal.PayBondHolders＝削除）、対外の流出は台帳を手で書いた。
        double domInt = interestPaid - foreignInt;
        if (domInt > 1e-12) { var (holders, held) = BondHolders(pops); World.Transfer((w, null), (w, holders, held), domInt, TreasuryCat.Interest); }
        World.Transfer((w, null), (null, null, null), foreignInt, TreasuryCat.Interest);   // 対外流出（国境の出入りの台帳へ＝保存）
        w.ForeignIntPaidThisQuarter += foreignInt;   // 02断線走査（2026-07-28）：CapitalMarketが債権国投資家へ配当（旧＝世界から消えるだけの片側記帳）
        // ★利子の未払いは元本へ繰り入れる（作者裁定 Q3・2026-09-28）＝お金は動かない・請求権だけが増える。国内分は持ち手の保有へ（保有に比例＝台帳の不変条件
        //   Σ保有＝GovDebt−ForeignDebt を保つ）・対外分は対外債へ（CapitalMarket が新しい対外債として債権国へ付ける）。持ち手のいない国内分は債権者がいない＝繰り入れない。
        //   不履行は既存の計数（借入の上限に張り付いたまま＝InsolvencyQuarters）のまま。
        //   ★開始前の助走（World.WarmupFrozen）は繰り入れない（2026-09-29 (109)）＝国債の残高は時間が経って積み上がる量（借入・減債・不履行の計数と同じ門）。
        //     旧は助走の歳入が請求の数倍で r＜1 の月が来ず、この穴が表に出なかった（1783年の税の段で米の国債が助走で +0.02 動いた）。
        double unpaidInt = interest - interestPaid;
        if (unpaidInt > 1e-12 && !w.WarmupFrozen)
        {
            double unpaidForeign = unpaidInt * foreignDebtFrac;
            double unpaidDom = unpaidInt - unpaidForeign;
            var unpaidForeignStock = new MoneyStock(unpaidForeign);   // (252) 未払いの利子の対外分＝国債と対外債に同じ額を足す
            w.GovDebt += unpaidForeignStock; w.ForeignDebt += unpaidForeignStock;
            MoneyStock held = default; foreach (var p in pops) if (p.BondHoldings.Value > 0) held += p.BondHoldings;
            if (held.Value > 0 && unpaidDom > 0)
            {
                double grow = 1.0 + unpaidDom / held.Value;
                foreach (var p in pops) if (p.BondHoldings.Value > 0) p.BondHoldings *= grow;
                w.GovDebt += new MoneyStock(unpaidDom);
            }
        }

        //（連合維持費の支払い＝治安維持と行政の人頭割りの給与の上乗せは 2026-09-30 (135) に撤去＝兵士と官僚の給与は兵力と行政の購買の1本＝上の注記）

        // ★撤去2026-08-16（C2-C5）：汚職根絶費の実支払い（acPay→官僚popへ俸給分配・TreasuryCat.AntiCorruption 記帳・LastAntiCorruptionCost 診断）を物理削除。
        //   V2点火2026-08-15で請求が恒等0＝この支払いは一度も走らない死骸だった。TreasuryCat.AntiCorruption の enum 値自体は台帳の添字互換のため残置（払い手ゼロ＝表示は0行フィルタで出ない）。

        // 減債：r＝1 の月の余剰（月初の払える額−全ての国家の支払いの請求＝spare）から返済（作者裁定 F3・Q2・2026-09-28）。金利＝基礎＋×(債務/GDP)。
        //   借入は月初の払えない額（spare＜0）の分だけ（Borrow）＝借りた後の spare は0以下＝同じ月に借りながら返すことが構造的に起きない（旧＝借入の線 国庫＜LastUpkeep＋LastInterestPerMonth に揃えた2026-09-27 の形の一般形）。
        //   ★開始前の助走（World.WarmupFrozen）は返さない（2026-09-29）＝国債の残高は「年月が経って積み上がる/減る量」（人口・資本と同じ・借入 Borrow と不履行の計数も同じ門）。
        //   旧＝助走の5年（60か月）の余剰で初期の国債を返した＝仏の米独立戦争債務（年GDP の約0.76）が開幕に0.04 へ痩せていた。
        if (w.GovDebt.Value > 1e-9 && payRate >= 1 && spare > 0 && !w.WarmupFrozen)
        {
            double repay = Math.Min(w.GovDebt.Value * c.BondRepayFracPerMonth.Value, spare);
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
            double shareNow = PoliticalQuantities.Clamp01(w.ForeignDebt / w.GovDebt);
            double frForeign = repay * shareNow;   // ★2026-09-26：旗 DebtRepayUsesPrincipalShare（点火2026-09-05）を焼き込み（x-01）＝元本の比
            MoneyStock domBefore = w.GovDebt - w.ForeignDebt;
            w.GovDebt -= new MoneyStock(repay); w.ForeignDebt = new MoneyStock(Math.Max(0, w.ForeignDebt.Value - frForeign));
            // ★保有台帳（2026-09-21裁定）：国内分の償還は**保有に比例**して払い、同じ比で保有を減らす
            //   （旧＝貯蓄比で買い・頭数比で償還＝往復ごとに富裕な資本家から貧しい資本家へ富が移る分子と分母の非対称）。
            //   減らす額＝国内元本の実減（GovDebt−ForeignDebt の差分）＝台帳と元本が同じ量で動く。持ち手不在＝国庫残置（債権者の消えた国内債は実質帳消し・保存）。
            // ★2026-09-30 段4g：減債も利払いと同じ2つの脚の移し方の1本（国内＝国庫→持ち手・保有の重み／対外＝国庫→世界の外・国境の出入りの台帳）。
            //   保有を同じ比で縮める1行は旧 Fiscal.PayBondHolders（削除）からここへ（計画 §10-12-2-4）。
            var (holders, held) = BondHolders(pops);
            double heldTotal = 0; foreach (var h in held) heldTotal += h;
            World.Transfer((w, null), (w, holders, held), Math.Max(0, repay - frForeign), TreasuryCat.DebtRepay);
            double principal = (domBefore - (w.GovDebt - w.ForeignDebt)).Value;
            if (principal > 0 && heldTotal > 0)
            {
                double keep = principal < heldTotal ? 1.0 - principal / heldTotal : 0.0;   // 同じ比で減らす＝和は total−principal（負にしない）
                foreach (var p in pops) p.BondHoldings *= keep;
            }
            // ★対外の元本も利子と同じ管で債権国へ（2026-09-22）：旧＝ここで流出を記帳するだけで、CapitalMarket は債権（ForeignBondClaims）を
            //   減らすだけ＝**どの貸し手にも金が届かず世界から消えていた**（国ごとの保存は閉じるので計器に掛からない片側記帳。
            //   seed1789・604四半期で 467,565）。利払い（上の foreignInt）と貸付（MultiNationSim の貸し手の引き落とし）は両建てだった。
            //   CapitalMarket が四半期末に、減らした債権と同じ比（＝債権の比）で債権国の投資家へ GiveGold する。
            World.Transfer((w, null), (null, null, null), frForeign, TreasuryCat.DebtRepay);   // 対外の元本の流出（国境の出入りの台帳へ）
            w.ForeignIntPaidThisQuarter += frForeign;
            // （返せた分だけ国債と翌月の利払いが減る＝借入の上限〔利払い÷歳入〕から離れる。旧のスタックの減算は 2026-09-28 にスタックごと削除）
        }
        //（借入スタックの出費上限 SpendingCapFactor は 2026-09-28 に撤去＝作者裁定 Q9・債務の重さは利子の請求が大きくなることで r に出る＝二重計上を避ける）

        // === ソブリン・デフォルト：借入上限に張り付き返済できない＝信用が尽き支払不能→**債務不履行（致命的）** ===
        // 借入の逃げ道（革命による債務帳消し）が無くなった分、過剰債務は「破滅的なデフォルト」で決着する＝借りすぎた覇権/軍国が崩れて上位が入れ替わる。
        // 財3（cadence）：BorrowCooldownMonths（減算）・InsolvencyQuarters（加算）は**月**で進むカウンタ（Fiscal.Step は月次実行）。
        //   ⚠名前は「Quarters」だが単位は月＝InsolvencyQuarters は払えなかった請求の割合の月ごとの累計（月の請求の何か月分・(205)）・DefaultCooldownMonths=24 は2年の借入禁止。
        //   ★2026-09-26（dorm-29）：四半期の締め月だけ進める休眠の器 FiscalQuarterlyGating（既定0＝毎月）を削除し、単位の書き方を月へ直した（挙動は1ビットも変わらない）。
        if (w.BorrowCooldownMonths > 0) w.BorrowCooldownMonths--;
        // ★張り付き＝今月の借入の門が閉じていた（直近の利払い≧限界×直近の歳入＝月初の Borrow と同じ1本＝上の underLimit の外）のに、払えない（r＜1）月（2026-09-28）。
        //   旧＝スタックが上限20に張り付いた月（r は見ない）。旧のスタックは r＜1 の月の借入でだけ積もり r＝1 の月の減債で離れた＝張り付きは「払えずに借り尽くした」だった。
        //   利払い÷歳入は借りなくても歳入の落ち込みや金利の上昇で限界を越える＝r＝1 で全ての請求を払えている国（国庫の蓄え）は数えない＝払えている国を不履行にしない。
        //   助走（World.WarmupFrozen）は数えない＝時間が経たない月に不履行へ届かない（国債の残高を動かさない＝減債・借入と同じ門）。
        // ★(205) 2026-10-04＝**払えなかった大きさで数える**（README §1-8「閾値で0か1に反転する判断より入力の大きさに応じた判断」・推奨で進めた＝作者の承認は未確認）。
        //   1か月の重み＝払えなかった請求の割合（1−r）＝累計は「月の請求の何か月分を払えなかったか」＝Coefficients.DefaultArrearsMonths（1.0）に届いたら不履行。
        //   旧＝r＜1 の月を大きさに関係なく1つと数え8か月で不履行（AI の実装＝2026-09-28 (93) でスタックの張り付きを置き換えた形・作者の裁定は Q3「利子の未払いは元本へ繰り入れ＋既存の不履行の計数」）。
        //   兌換停止の後の兌換は四半期 ConvertibilityDropSpeed で0 へ漸近するだけで0 にならない＝切り下げの余地＝払えない額×(1−兌換) の残り（払えない額×兌換）・
        //   銀の改鋳の窓の残り（×(1−正貨比率)）・浮動小数の端（r＝1−1e-10）が毎月1か月に数えられ、ほぼ全てを払えていた国が8か月で国債を全て否認した
        //   （(203) の10種で148件中67件・英5件中4件・計器 scratchpad/m204/inj_d204）。重みは入力に比例＝天井も床も足していない（思想⑧）。戻りは旧と同じ（払えた月・門が開いた月に0）。
        if (payRate < 1 && w.GovDebt.Value > 1e-9 && !underLimit && !w.WarmupFrozen) w.InsolvencyQuarters += 1.0 - payRate; else w.InsolvencyQuarters = 0;
        if (w.InsolvencyQuarters >= c.DefaultArrearsMonths)
        {
            // 債務消滅（請求権＝保存無影響）。致命的な代償＝兌換崩壊・**信用凍結（貸付消滅→工場が資本を得られず投資/工業化が崩落＝恐慌）**・正統性崩壊（革命リスク）・大衆の暴動・当面 借入不能。
            RepudiateDebt(w, c);   // 債務・保有・兌換・借入の停止＝急進革命の否認と同じ中身（1か所）。以下はデフォルトだけの代償
            w.InsolvencyQuarters = 0; w.HadSovereignDefault = true; w.SovereignDefaultCount++;   // C-中9（2026-07-22）：回数も数える（恒久boolでは2度目以降の破産が数えられない）。★(373) 制度疲労の削除の後、回数は計器（defaultCount・sovereignDefaultCumulativeCount）とセーブだけが読む
            if (w.Bank is { } dbank) { dbank.Confidence = 0; dbank.Heat = 0; }   // 信用凍結＝Confidence0で次期以降 与信が自然収縮（恐慌）。**Loansは直接0にしない**（基礎貨幣=広義−与信 の保存を破るため）
            double legBeforeDefault = w.Legitimacy;
            w.Legitimacy = Math.Max(0, w.Legitimacy - c.DefaultLegitimacyHit);
            w.Explain?.Add("Legitimacy", "国家破産（デフォルト）の信認崩壊", w.Legitimacy - legBeforeDefault);   // 説明API（#63）
            foreach (var dp in pops) if (Politics.IsMassPop(dp)) dp.Militancy = PoliticalQuantities.Clamp01(dp.Militancy + c.DefaultMilitancyHit);
        }
        // 財7（債務/GDPの単位整合）：NominalGdpPerMonth は月次フロー、GovBondRatePerYear は年率（利払いを/12）。分母は年の名目GDP（月次×12）。
        //   ★2026-09-29：旗 DebtGdpAnnualized（2026-07-26 点火＝既定1.0）を焼き込み＝国債の金利の2つの上乗せ（債務比・信認の喪失）を史実の年の単位で出し直した（下）＝旧の月次の枝（0）は較正の意味を失った。
        MoneyPerYear gdpPerYear = Calendar.PerMonthToPerYear(new MoneyPerMonth(nominalGdp));   // 年の名目GDP（★(257) 反証役 C2 低#1：型つきの換算を通す＝double 版と同じ1回の乗算）
        double gdpDenom = gdpPerYear.Value;
        // [単位・B13] 債務/GDP＝**年次名目**（GovDebt÷(月次NominalGdpPerMonth×12)）。国債金利の債務比プレミアム・CapitalMarket の対外信用の債務の項・
        //   Government の緊縮の強度・Credit の銀行の評価損・PolicyPreferences の国家の歳入要求（どれも 2026-09-29 から）はこの単位（旧＝Credit は月次名目・Government/PolicyPreferences は月次実質）。
        //   台帳（2026-09-11 #10）：PowerShares.cs の debtGrip＝月次名目の Clamp（物理・用途別に別＝触らない）／Runner milfund・hangtest の dGdp＝月次名目（計器）／App のヘッダー「対年GDP」＝この行と同じ年次名目（旧＝年次実質＝表示だけ別物差しだったのを統一）
        RatioToGdpYear dGdp = gdpDenom > 1e-9 ? Calendar.ToGdpYear(w.GovDebt, gdpPerYear) : (w.GovDebt.Value > 0 ? DebtToGdpYearUnbounded : default);   // #1 名目GDP基準＝デフレ国は実質債務が膨らみ国債金利が上がる（負債デフレ）
        // ★AUD-09（監査2026-09-02 low・規律「比のガードは絶対値で書かない」）：分母の絶対値ガード（1e-9）だけでは、
        //   分母が**微小だが非ゼロ**のとき比が番兵を何桁も飛び越える（GDPが消えかけた残余国で万%＝WorstLossRate と同型）。
        //   ⚠**否定形**で書く＝NaN も同じ枝へ落ちる（`Math.Min` では NaN が素通りして GovBondRatePerYear を毒する）。
        //   ★有限値では厳密同値：読み手は国債金利（BondRateMaxPerYear=0.30 でクランプ済み）だけで、番兵 1e9 のはるか手前で飽和している
        //     ＝**挙動は変わらず、報告される数字と NaN 経路だけが正される**（兌換の門は 2026-09-28 から債務比を読まない＝下）。
        if (!(dGdp <= DebtToGdpYearUnbounded)) dGdp = DebtToGdpYearUnbounded;
        // 金本位の信用ゲート（資格＝コストでない）：準備が足り、払える財政なら加入できる。加入中＝国債金利を信用で優遇（低金利）。
        // 加入中でも取り付けで準備が割れる／維持費を払えないと正貨を守れず強制離脱（兌換停止＝信用暴落）。紙幣増発で赤字を埋める国は信用が無く加入不可。
        // ★債務の水準では門を引かない（作者裁定 2026-09-28 F6 の1歩目）。旧＝債務/年GDP の固定の閾値（停止 7.5%・再加入 4.2%＝係数
        //   GoldStandardSuspendDebt・GoldStandardMaxDebt。注記が自ら「英1815=250% に対し極端に厳しい」と書いていた）＝金の法の英が兌換へ戻れず財政が細った。
        //   史実の兌換停止は債務の水準でなく、取り付け（金の流出が準備を超える＝英1797の侵攻の噂）と戦費（米1862・仏1870）で起きた。
        //   英は1815年に国債が GDP の約2.5倍でも1821年に兌換を再開して1914年まで保ち、仏は1871〜73年の賠償（GDP の約4分の1）の後も1876〜78年に金へ移った。
        //   ⇒ 門は既存の2つの量で引く＝①正貨準備カバー率（取り付け）②維持費を払えた比 FiscalFundingRatio（利払いは維持費より先に国庫から出る
        //   ＝債務の重さは利払いが歳入を食う分だけここに出る・戦費で国庫が尽きた月も同じ）。債務の水準そのものは国債金利の債務比プレミアムと、
        //   借入の上限→不履行（上＝強制離脱）が持つ＝二重に数えない。係数2本を削除。
        if (w.MonetaryEnabled)
        {
            // 正貨準備カバー率（convertibility-not-reserve-backed＋fx-peg防衛）：信用込み広義マネーに対する正貨(基礎貨幣)の比＝BaseMoney/BroadMoney。
            // 低いと金流出で兌換を守れず停止（1797英/1893米型の投機アタック）。GoldCoverMin=0 なら coverage<0 never・coverage>=0 always＝発火しない（⚠既定は0.25＝live・x-02 で注記を是正）。
            double coverage = w.BroadMoney.Value > 1e-9 ? w.BaseMoney / w.BroadMoney : 1.0;
            if (w.OnGoldStandard)
            {
                // ★3値化2026-08-13：兌換の規律は**銀本位でも同じ足枷**＝MetallicStandardLaw（銀でも金でも真）を読む。休眠時は旧 GoldStandardLaw と厳密に一致。
                if (!w.MetallicStandardLaw || w.FiscalFundingRatio.Value < GoldSuspendMinFundingRatio || coverage < c.GoldCoverMin.Value)
                {
                    w.OnGoldStandard = false; w.GoldStandardSuspendedInCrisis = w.MetallicStandardLaw;   // 自発離脱 or 危機での強制停止（準備枯渇＝兌換停止クライシス含む）
                    // GoldCoverMin有効化2026-07-10：強制停止を観測可能に（スカラーカウンタ＝batch頻度集計・Diplomacyログ＝年代記）。自発離脱（AIが志向を下げた）は数えない。
                    if (w.GoldStandardSuspendedInCrisis)
                    {
                        bool coverTrip = coverage < c.GoldCoverMin.Value;
                        w.GoldSuspensions++; if (coverTrip) w.GoldCoverSuspensions++;
                        w.LogAdd($"{w.Year} {w.SelfPower?.Name}が兌換停止（金本位から強制離脱・{(coverTrip ? $"正貨準備カバー率{coverage:P0}<{c.GoldCoverMin:P0}" : "財政不足")}）");
                    }
                }
            }
            // #125 振動抑制（2026-07-25）：再加入は離脱閾値より高いカバー率（×GoldRejoinCoverMargin＝シュミットトリガー）。Margin=1.0 で旧条件と厳密一致。
            // ★兌換の回復は通貨改革を通る（2026-09-27）：切下げの記憶が壊れたまま（broken＝改革の発動と同じ1本）では戻れない。
            //   旧＝カバー率・財政だけを見た＝改革より先に再加入すると、改革は兌換停止中にしかできないので記憶が1世紀凍結し、
            //   CapitalMarket の資本逃避が止まらなかった（露1804〜11年の先着争い＝世界規模の補給崩壊の根）。史実＝露カンクリン改革1839〜43（アシグナツィアを実勢で銀に換算してから兌換）・ウィッテ改革1897・レンテンマルク1923→金本位1924。
            else if (w.MetallicStandardLaw && w.FiscalFundingRatio >= c.GoldStandardMinFunding
                     && coverage >= c.GoldCoverMin.Value * c.GoldRejoinCoverMargin && !broken)
            { w.OnGoldStandard = true; w.GoldStandardSuspendedInCrisis = false; }
        }
        // #2 Fisher：名目金利＝実質（基礎＋債務上乗せ）＋期待インフレ。高インフレ→高名目金利／デフレ→低名目金利だが実質債務は重い。
        // 財4：旧 Max(0,·) はデフレ側を遮断し「デフレ→低名目金利」のコメントと逆だった（1873-96大デフレのコンソル利回り低下3%→2.5%が再現不能＝負債デフレだけ効く片肺）。
        // 下限を FisherDeflationFloor でゲート。既定 -0.02＝デフレ側を軽く通す。0.0なら旧 Max(0,·) と厳密同値。
        double fisher = Math.Max(c.FisherDeflationFloor, w.ExpectedInflationPerYear.Value) * c.FisherInflationWeight;
        // 金利の兌換割引を連続化（倍率排除②）：完全兌換(Conv=1)で GoldStandardRateDiscount(0.6)・不換(Conv=0)で1.0。部分兌換は中間＝信用プレミアムが連続。
        double convDiscount = 1.0 - w.Convertibility * (1.0 - c.GoldStandardRateDiscount);
        // 政策金利（バンクレート）が国債金利の床を押し上げる（引締め＝政府の借入も高くつく）。較正は InterestRate=0＝中立。
        // ★信認の喪失＝**直近の切り下げの速さ**（この月の切り下げ×12÷年の名目GDP＝年の切り下げが年GDP の何割か・名目÷名目）（2026-09-29・作者裁定＝(94) の申し送り1 の (i)）。
        //   旧（2026-06-28 金融改革①）＝累積の切り下げ÷**月次**名目GDP×0.06＝累積は通貨改革まで減らず、分母は月次（年に直すと12倍）＝(94)(95) で上限の国×四半期の中央 0.45〜1.28
        //   ＝単独で BondRateMaxPerYear 0.30 を越え、国債の平均クーポンが 0.29〜0.30 に張り付いた（上限＝利払い÷歳入≧0.55 が国債÷年GDP 約0.2〜0.4 で閉じた）。
        //   史実の信認の喪失は累積でなく最近の増発の速さ（アッシニャ1790〜96・墺1811 の前後・米のグリーンバック1862〜64・アルゼンチン1885〜90＝不換の発行が年に GDP の数%の国で
        //   国債の利回りが数%上がり、増発を止めると戻った）＝GDP 比1%につき約0.5%（BondRateDistrustScale）。
        //   ★記憶は払う利子の側が持つ＝増発を止めた月から市場の金利は下がるが、高い金利で乗り換えた既往債の平均クーポン GovAvgCouponPerYear（R9 の満期構造＝月に
        //   BondRolloverFracPerMonth だけ乗り換わる）が時間とともに薄まる＝新しい状態を作らない。⚠累積の切り下げ CumulativeDevaluation は資本逃避（CapitalMarket）・
        //   通貨改革と兌換の再加入の門（broken）・AI の改革の判断が読むまま（金利生活者のポンプの歯止め＝2026-09-27）＝国債の金利だけが読まなくなった。
        //   ⚠否定形＝NaN の切り下げは0・年GDP が0で切り下げありは番兵（BondRateMaxPerYear で頭打ち）。
        double devalSpeed = devalued > 0 ? (gdpDenom > 1e-9 ? Calendar.PerMonthToPerYear(devalued) / gdpDenom : DebtRatioUnbounded) : 0;
        if (!(devalSpeed <= DebtRatioUnbounded)) devalSpeed = DebtRatioUnbounded;
        double distrust = c.BondRateDistrustScale * devalSpeed;
        // ★債務比の上乗せ（BondRateDebtScale×債務÷年名目GDP）は史実から（2026-09-29）＝英のコンソル（国債÷年GDP 約2.5 の1815〜20年に利回り約4.5%）を信認のある国
        //   （完全兌換・議会が予算を握る＝割引 0.6×0.9）で出す：(0.04＋2.5×0.018)×0.54≒4.6%。同じ式で不換・議会の弱い周縁国（西・葡・希・オスマン・ラテンアメリカ）は
        //   国債÷年GDP 1〜2 で 5.8〜7.6%＋期待インフレと信認の喪失（史実6〜10%）。旧＝1.44（旧の月次比 0.12×12 を年へ換算したまま＝年GDP 比1につき+144%）。
        //   監査2026-07-05（F2）の債務の二次項 BondRateDebtQuad（既定0＝休眠）は削除（英の債務比2.5 で金利が跳ねない史実と合わない・棚卸し dorm-51／fis-17）。
        double bondInside = c.BondBaseRatePerYear.Value + c.BondRateDebtScale * dGdp.Value + fisher + distrust + c.PolicyRateBondWeight * w.InterestRate;
        // 政体フレーバー レバー3（2026-07-24）→第3波連続化（2026-07-28）：議会の信認割引＝議会の予算統制（ParliamentControl）が
        // 強いほど債権者の信認で金利が安い（英コンソル＝財政軍事国家論）。旧・政体ラベル二値→統制力の連続量へ（倍率排除＝ラベル不読・
        // DebtGdp年換算フリップ2026-07-26で再挑戦解禁の記録どおり）。BondRateRepresentationDiscount=0で×1.0＝厳密恒等。
        double reprDiscount = 1.0 - c.BondRateRepresentationDiscount.Value * PoliticalQuantities.Clamp01(w.ParliamentControl);
        double bondPre = bondInside * convDiscount * reprDiscount;
        w.GovBondRatePerYear = new RatePerYear(Math.Min(c.BondRateMaxPerYear.Value, bondPre));
        // 説明API（#63）：国債金利の内訳＝各プレミアム×兌換割引−上限クランプ。Explain=nullでゼロコスト・戻り値不変。
        if (w.Explain is { } bl)
        {
            bl.Reset("BondRate");
            bl.Add("BondRate", "基礎金利", (c.BondBaseRatePerYear * convDiscount).Value);
            bl.Add("BondRate", "債務比プレミアム", c.BondRateDebtScale * dGdp.Value * convDiscount);
            bl.Add("BondRate", "インフレ期待", fisher * convDiscount);
            bl.Add("BondRate", "信認喪失（直近の切り下げ）", distrust * convDiscount);
            bl.Add("BondRate", "政策金利", c.PolicyRateBondWeight * w.InterestRate * convDiscount);
            bl.Add("BondRate", "金本位の信認割引", bondInside * (convDiscount - 1.0));
            bl.Add("BondRate", "議会の信認割引", bondInside * convDiscount * (reprDiscount - 1.0));
            bl.Add("BondRate", "上限クランプ", w.GovBondRatePerYear.Value - bondPre);
        }
        // 計器（読むだけ・(265)）：Explain と同じ項を、Explain を立てずに直に写す（Explain を立てると全域の Add の経路が動く）
        if (Instruments.On) Instruments.BondRate(w, (c.BondBaseRatePerYear * convDiscount).Value, c.BondRateDebtScale * dGdp.Value * convDiscount, fisher * convDiscount, distrust * convDiscount, c.PolicyRateBondWeight * w.InterestRate * convDiscount,
            bondInside * (convDiscount - 1.0), bondInside * convDiscount * (reprDiscount - 1.0), w.GovBondRatePerYear.Value - bondPre, dGdp.Value, convDiscount, reprDiscount);

        // ※旧・教育費（国庫→pop一律）は廃止：教育のコストは教育財の市場が直接表現する
        //   （授業料＝popが教師/聖職者に払う／義務教育＝国庫が買い与える＝EconomyStep）。教育費スライダーは義務教育の財源率。

        // 社会保障の給付：国庫→大衆（労働者・農民）＝市場へ流れる（保存）。大衆人口に比例＝高コスト＝レッセフェールとの選択。
        if (w.SocialSecurity > 1e-9)
        {
            var masses = new List<Pop>();
            foreach (var p in pops) if (!p.Closed && !p.IsModernElite) masses.Add(p);
            double massSize = 0; foreach (var p in masses) massSize += p.Size;
            double welfareReq = c.WelfareCostRatePerMonth * w.SocialSecurity * massSize;
            // funding修正：給付の請求の上限を**裁量余剰(FiscalCapacityPerMonth)**から（余剰の無い国は福祉を約束しない）。
            double welfareClaim = Math.Min(welfareReq, (c.WelfareRevenueShare * w.FiscalCapacityPerMonth).Value);
            // ★払うのは請求×r（作者裁定 F3・2026-09-28 段2-1）。撤去＝借入スタックの出費上限 SpendingCapFactor（Q9）と国庫残高のクランプ WelfarePensionClampToTreasury
            //   （旧＝滝の後段で国庫の残りまで）。充足率 WelfareFunding は r と同じ量になるので畳んだ（保険・医療は FiscalFundingRatio を読む）。
            double welfareCost = welfareClaim * payRate;
            w.LastWelfareCostPerMonth = new MoneyPerMonth(welfareClaim);   // 請求（r を掛ける前）＝翌月の月初の請求の見込み（Simulation）と予算表が読む
            World.Transfer((w, null), (w, masses, null), welfareCost, TreasuryCat.Welfare);   // 国庫→大衆（頭数）＝移し方の1本（段4f）。受け手不在＝動かさない（国庫に残る）
        }
        else w.LastWelfareCostPerMonth = new MoneyPerMonth(0);

        // 年金の給付（①年齢コホート Phase2）：国庫→老年人口（保存）。老年人口に比例＝高齢化が進むほど財政負担（ビスマルク年金1889〜）。年金なし（既定）＝level0で無効＝較正中立。
        double pensionLevel = w.Pension switch { ReformLevel.Full => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
        if (pensionLevel > 1e-9)
        {
            double oldSize = 0; foreach (var p in pops) if (!p.IsSoldier) oldSize += p.Size * p.AgeShare(60, double.PositiveInfinity);   // 老年＝60歳以上（年齢の層・段4e 後半）
            double pensionReq = c.PensionCostRatePerMonth * pensionLevel * oldSize;
            double pensionClaim = Math.Min(pensionReq, (c.WelfareRevenueShare * w.FiscalCapacityPerMonth).Value);   // 福祉と同じ裁量余剰から（請求）
            double pensionCost = pensionClaim * payRate;   // 払うのは請求×r（福祉と同じ・2026-09-28 段2-1＝SpendingCapFactor と国庫残高のクランプを撤去）
            w.LastPensionCostPerMonth = new MoneyPerMonth(pensionClaim);   // 請求（r を掛ける前）＝翌月の月初の請求の見込みと予算表が読む
            if (pensionCost > 0)   // 老年人口に比例して配分（年金は老人の所得＝消費に回る）。wave2b：分母を受け手と同じフィルタで作り直し（旧oldSizeはOldShare≤1e-9の pop も算入したまま
            {                       // 給付側だけ除外していた＝分子分母の非対称）、Distribute作法どおり端数は最後の受け手へ・受け手不在は国庫残置（保存）。
                var elderly = new List<Pop>(); var elderW = new List<double>();
                double elderSize = 0, reachW = 0;
                // ★支配度 段4（2026-09-07）：給付の到達＝州の国家区分（RuleReach）。g=0 で重み1＝旧と同じ受け手・同じ順序（AllPops＝州順→pop順）。
                //   届かない分は国庫残置（受け手不在と同じ作法＝保存）。
                bool reachOn = c.RuleSharesReach > 0;
                foreach (var rg in w.Regions)
                {
                    double reach = reachOn ? Politics.RuleReach(rg, c) : 1.0;
                    foreach (var p in rg.Pops)
                    {
                        double os = p.AgeShare(60, double.PositiveInfinity);
                        if (!p.IsSoldier && os > 1e-9) { elderly.Add(p); elderW.Add(p.Size * os * reach); elderSize += p.Size * os; reachW += p.Size * os * reach; }
                    }
                }
                // ★支払いの一元化 段2-3（E1・2026-09-29）：按分の本体 World.Distribute へ畳んだ（旧＝式 pensionCost×w/Σ の結合順が本体と違い、到達の枝は端数を寄せなかった＝同じ支払いに端数規則が2つ）。
                //   届く額＝給付×(届く重み÷老年人口)を届く重みで配る＝各人の額は旧と同じ量。届かない分・受け手不在・配れなかった額は国庫残置。
                // ★段4f（2026-09-30）：国庫→老年は移し方の1本（旧＝給付の全額を引いてから届かない分を Pay で戻す2行）＝国庫から出るのは届く額だけ（到達の重みが1＝既定では給付の全額）。
                double reached = elderSize > 1e-9 ? pensionCost * (reachW / elderSize) : 0;
                World.Transfer((w, null), (w, elderly, elderW), reached, TreasuryCat.Pension);
            }
        }
        else w.LastPensionCostPerMonth = new MoneyPerMonth(0);

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
            w.NationBuildingSpending = newsSz > 1e-9 ? PoliticalQuantities.Clamp01(newsW / newsSz * zeal) : 0;
        }
        // POP動詞①（階級への優遇/弾圧）は廃止（2026-07-20ユーザー指示＝税で表現済みで冗長）。弾圧の概念は国内危機（Politics.CheckFactionRevolt→CrisisThroughLawPath＝通知のみ・ボタンなし）へ移行＝⚠2026-09-08に無血鎮圧の枝は撤去・2026-09-11に弾圧/譲歩の択も撤去＝対処は法改正か内戦だけ。
    }

    /// <summary>行政の購買の距離の割増（作者裁定 2026-09-03 E-05段4「維持費は本国州にも必要なはずだ。植民地は遠く、それが高額なだけ」）
    /// ＝1＋UpkeepDistanceGeneral×DistanceWarDecay×首都からの距離÷DistanceNormKm（戦力投射と同じ形と物差し）。遠い州の官僚は同じ働きに割増の俸給が要る。
    /// 座標未登録(0,0)の州・距離が有限でない州は1（幻の遠距離を作らない）。2026-09-30 (135) に維持費の人頭割りから行政の購買へ移した。
    /// 読み手＝州の行政の予算（Simulation）・行政の購買の単価（EconomyStep）・行政の請求の平均（<see cref="AdminDistanceMean"/>）＝同じ1本。</summary>
    internal static double AdminDistanceMul(World w, Region r)
    {
        var c = w.Coef;
        if (!(c.UpkeepDistanceGeneral > 0)) return 1.0;
        var rc = Places.Coord(r.Name);
        if (rc.Lat == 0 && rc.Lon == 0) return 1.0;
        double km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
        return km > 0 && double.IsFinite(km) ? 1.0 + c.UpkeepDistanceGeneral * c.DistanceWarDecay * km / Diplomacy.DistanceNormKm : 1.0;
    }

    /// <summary>行政の距離の割増の人口加重の平均（活きた州）＝州ごとの行政の予算（人口の比×割増＝Simulation）の和を、国の行政の請求（Fiscal）と予算表（UI）が同じ量で読む。</summary>
    public static double AdminDistanceMean(World w)
    {
        double s = 0, n = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double rp = r.SumPopulation();
            if (!(rp > 0)) continue;
            s += rp * AdminDistanceMul(w, r); n += rp;
        }
        return n > 0 ? s / n : 1.0;
    }

    /// <summary>★借入の門（2026-10-03 (175) に <see cref="Borrow"/> の3行を1本へ畳んだ＝挙動は同じ）＝冷却中でない（デフォルト後の信用喪失）・開始前の助走でない・
    /// 直近の利払いが歳入の限界（<see cref="Coefficients.MaxInterestShareOfRevenue"/>）の下。読み手＝<see cref="Borrow"/> と戦時段の器（<see cref="Military.EnvelopeAt"/>＝戦時国債）の2つ
    /// ＝戦時段が借りる余地と実際に借りる門が同じ1本（思想⑨＝借入の経路を2本にしない）。
    /// ★助走の門（2026-09-29）＝国債の残高は時間が経って積み上がる量（減債・不履行の計数と同じ門・Step）。否定形＝NaN は借りない側（歳入0の国は利払い0でも借りられない＝返す財源の無い国に貸し手はいない）。</summary>
    internal static bool BorrowGateOpen(World w, Coefficients c)
        => !(w.BorrowCooldownMonths > 0) && !w.WarmupFrozen && w.LastInterestPerMonth < c.MaxInterestShareOfRevenue * w.LastRevenuePerMonth;

    /// <summary>★1件の額＝月次の名目GDP×<see cref="Coefficients.BorrowFractionOfGdp"/>（負の GDP は0）＝月に借りられる額の上限。
    /// 読み手＝<see cref="Borrow"/>（払えない額と比べる）と戦時段の器（<see cref="Military.EnvelopeAt"/>）の2つ＝同じ1本（2026-10-03 (175)）。</summary>
    internal static double BorrowTicket(double nominalGdp, Coefficients c) => Math.Max(0, nominalGdp) * c.BorrowFractionOfGdp.Value;

    /// <summary>★借入（エンジンが自動で借りる・作者裁定 2026-09-28「借入は自動化・借金ボタンは不要」）：借入額＝min(1件の額, 月初の払えない額, 貸し手のプール)。
    /// **月初に Simulation が国家の支払い率 r を決める前に呼ぶ**＝払える額の中身の順＝税・歳入→借入（この関数）→それでも足りない分だけ切り下げ（作者裁定 2026-09-28 (b)）。
    /// 借りた金はその月の支払いに使う（呼び手が払える額へ足す）。返り値＝借りた額（国庫へ入った額）。呼び手は Simulation の月次決算の1か所だけ（AI とプレイヤーの経路は無い＝等価）。</summary>
    /// <param name="spare">月初の払える額−国家の支払いの請求（借越を含む）。＜0 の分＝払えない額＝借りる額の上限。≧0（と NaN）は借りない。</param>
    internal static double Borrow(World w, Coefficients c, double spare)
    {
        // 調達＝資本家の余剰貯蓄（クラウディングアウト）＋外資（開放度）＝旧・自動国債と同じ保存経路（国内は純移転・海外は CumulativeTradeFlow）。
        // プールを超えては借りられない（保存）。利払いが歳入の限界に届いたら頭打ち→払えない（r＜1）まま続けば不履行（Step）。
        // ★条件は1行だけ＝払えない額がある月・借入の上限の下・冷却中でない。旧＝AI（SelfPlay）と画面（GameViewModel）が同じ条件で
        //   四半期ごとに予約の列（BorrowQueue）へ積み、Fiscal.Step が月に1件ずつ実行した＝予約の列と2行を削除した（AI とプレイヤーの経路は無い＝等価）。
        //   借りる主体のいない世界（試験の NullAgent）も同じ1行で借りる＝利子の未払いが元本へ複利で積もり続け、上限→不履行の出口に届かない形が閉じる。
        // ★切り下げより先に借りる（作者裁定 2026-09-28 (b)「切り下げは借りられないとき（上限・冷却中）だけ」）：旧（2026-09-28 (89)〜(93)）＝月末の Fiscal.Step で
        //   r＜1 の月に借り、借りた金は翌月の払える額になった＝その月の払えない額は月初の切り下げが先に埋め（切り下げの97〜99% は借りられた四半期・借入の11〜32倍・
        //   切り下げの収入は税収の30〜45%）、借入は切り下げの残りだけだった。史実の紙幣の増発は信用が尽きた後（アッシニャ1790〜・墺1811・米のグリーンバック1862）
        //   ＝借りられる月は借り、上限か冷却中の月（と1件の額・貸し手のプールを越えた分）だけ切り下げで埋める。優先順位の滝ではない＝同じ1本の率 r の払える額の中身の順。
        //   門は旧の「r＜1」から「払えない額がある（spare＜0）」へ＝r は借入の後に決まる量（借入の前の r＜1 に借越だけの月を足したもの＝切り下げの余地と同じ量）。
        // ★借りる額は払えない額だけ（作者裁定 2026-09-28）：旧＝r＜1 なら不足の大きさに関係なく1件＝1件の額（月次GDP×割合）を満額借りた
        //   ＝切り下げでほぼ埋まった r＝1−ε の月（r＜1 の四半期の77% が r≧0.99）も満額→上限→不履行→借入禁止を繰り返した。
        //   払えない額＝月初の −spare（請求−払える額＋借越）＝切り下げの余地と同じ量・減債の余剰（spare＞0）の裏側。
        // ★借入の上限＝利払いが歳入の限界（MaxInterestShareOfRevenue）に届く所（作者裁定 2026-09-28・棚卸し fis-01）。旧＝借入スタック20（1スタック＝月次**実質**GDP の10%
        //   ＝年GDP の約17%で全ての国が同じ天井・1815年の英の250%は原理的に出ない）。史実で借りられなくなったのは水準そのものでなく、利払いが歳入を食う割合と市場の信認
        //   （英1818-22 利払い約3,100万ポンド÷歳入約5,600万≒0.55 で借りずに持ちこたえ〔借りないことを選んだ線＝限界ではない〕・仏1788 3.18億÷4.72億リーヴル≒0.67 で支払停止＝(294) から限界はこの 0.67）＝同じ限界でも、
        //   歳入の厚い国（英）と金利の安い国（金本位・議会の信認割引）ほど深く借りられ、信認を失った国（直近の切り下げ・高債務の金利＝その金利で乗り換えた平均クーポン）は浅い所で止まる。
        //   ⚠門は直近の月次決算の利払いの請求（LastInterestPerMonth＝国債×平均クーポン）と歳入（LastRevenue）＝どちらも名目の量＝物価水準が変わっても比は変わらない
        //   （r の請求と払える額が読むのと同じ2つの量・Step の不履行の計数と巨大建造物の「まだ借りられる」も同じ1本）。旧のスタック（過去の1件の額で数えた借入の累計）は
        //   分子と分母が別の時点・別の物差しだった＝2026-09-28 に削除（思想⑩・棚卸し fis-01「スタック数は残高から導ける」）。
        //   否定形＝NaN は借りない。歳入0の国は利払い0でも借りられない（返す財源の無い国に貸し手はいない）。
        double shortfall = spare < 0 ? -spare : 0;   // 否定形＝NaN は借りない
        if (Instruments.On && shortfall > 0 && !BorrowGateOpen(w, c)) Instruments.Gate(w, shortfall);   // 計器（読むだけ）
        if (!(shortfall > 0) || !BorrowGateOpen(w, c)) return 0;   // 月に1件。門（冷却・助走・利払いの限界）は戦時段の器（Military.EnvelopeAt）と同じ1本
        // 1件の額＝月次**名目**GDP×割合（2026-09-28・単位の対称）。旧＝実質GDP（参照価格の産出）＝払えない額・国債・プールは名目なので、
        //   物価水準が1から離れる国ほど1件の物差しがずれた（分子と分母の非対称）。下の外資のプールも同じ理由で名目。
        //   ⚠月初＝その月の産出の前＝名目GDP は直近の月の産出（Step が読むのと同じ産出）。GDP の走査は借りる月だけ。
        double nominalGdp = w.NominalGdpPerMonth.Value;
        double borrowWant = Math.Min(BorrowTicket(nominalGdp, c), shortfall);
        var capitalists = new List<Pop>();
        foreach (var (_, p) in w.AllPopsWithRegion()) if (CapitalMarket.IsInvestor(p)) capitalists.Add(p);   // 国内の買い手＝資本家/近代エリート
        double capSavings = 0; foreach (var p in capitalists) capSavings += Math.Max(0, p.Savings.Value);
        // 財6（資本開放度の脱・関税連動）：openness の関税連動を CapitalOpennessTariffWeight で連続化。既定1.0＝openness=Clamp(1−1.0×Tariff,0,1)＝旧 Clamp(1−Tariff,0,1) と代数的に厳密一致（数値中立）。
        // 0で関税と独立（19世紀の資本移動は関税と別＝高関税の米/露も外債を発行）→兌換性/信認ベースの開放度へ。中間で部分連動。
        // ★トリレンマを閉じる①（2026-08-13）：資本開放度を**法**から導出し、関税との連動を切る。
        //   旧＝関税の従属変数なので「関税は高いが資本は自由」（19世紀後半の独/米）が原理的に表現できなかった。
        //   規制でも0にしない（★密輸と同じ理由＝規制しても抜け道はあった）。CapitalMobilityLawGate=0 で下の旧式のみ（⚠既定は点火済みの1.0＝2026-08-13・x-02 で注記を是正）。
        double openness = c.CapitalMobilityLawGate > 0
            ? (w.CapitalControlLaw ? PoliticalQuantities.Clamp01(c.CapitalControlOpenness.Value) : 1.0)
            : PoliticalQuantities.Clamp01(1.0 - c.CapitalOpennessTariffWeight * w.Tariff);
        double domesticPool = capSavings * c.BondAppetite.Value;
        // ★外資の貸し手は**低金利の国ほど深い**（2026-09-29・向きの是正）＝信認（基礎金利÷その国の国債の金利）に比例。旧＝金利÷基礎金利（金利が下がると外資が縮む）。
        //   国債の金利は利回りを求める誘引ではなく危険の値段（債務比・期待インフレ・直近の切り下げの上乗せを、兌換と議会の信認で割り引いた量）＝同じ危険なら期待収益は同じ。
        //   史実＝低金利の国ほど深い市場（英のコンソル 1850〜1913 約3%・蘭）で、高金利の国は割り当てで締め出された（オスマン1875・エジプト1876・アルゼンチン1890 の前後に新規の起債が止まった）。
        //   否定形＝金利が NaN ならプールは NaN＝下の `!(borrowed > 1e-9)` で借りない（旧と同じ）。
        double foreignPool = openness * c.ForeignBondCapacity.Value * Math.Max(0, nominalGdp) * Math.Clamp(c.BondBaseRatePerYear.Value / Math.Max(1e-9, w.GovBondRatePerYear.Value), 0, ForeignBondYieldRatioCap);
        // 財5（対外国債をモデル外の無限プールから閉鎖系へ）：foreignPool を他モデル国の資本家貯蓄から実引き当て。
        // 注入（MultiNationSim→World.ForeignBondInvestorPool）と保有国配分（CapitalMarket の外債レグ＝ForeignBondClaims 繰上げ＋利払い配当）は実装済み。
        // ★2026-09-26：旗 ForeignBondFromInvestors（既定1.0）を焼き込み（x-01）。
        // ★支払いの一元化 段2-2（A5・2026-09-29）：貸し手のいない対外借入は立たない＝プールが未注入（既定 −1＝単一国の世界・共有層の最初の四半期の前）なら外資は0。
        //   旧＝未注入ならモデル外の無限プール＝世界の外から湧いた（四半期末の貸し手の引き落としが無い）。
        double foreignPoolRaw = foreignPool;   // (271) 計器（読むだけ）：他国の投資家の貯蓄で切る前の外資のプール
        foreignPool = Math.Min(foreignPool, Math.Max(0.0, w.ForeignBondInvestorPool));   // 実引き当て：他国投資家プールを超えて対外債を発行できない（保存）
        double pool = domesticPool + foreignPool;
        double borrowed = Math.Min(borrowWant, pool);
        if (Instruments.On) Instruments.Borrow(w, shortfall, BorrowTicket(nominalGdp, c), domesticPool, foreignPool, borrowed, foreignPoolRaw);   // 計器（読むだけ）
        if (!(borrowed > 1e-9)) return 0;
        double foreignPortion = pool > 1e-9 ? borrowed * (foreignPool / pool) : 0;
        double domesticPortion = borrowed - foreignPortion;
        // ★保有台帳（2026-09-21裁定）：各資本家は**払った額だけ**国債を持つ（台帳は金を動かさない＝払う額の式は旧と同一）。
        //   ⚠門を `capSavings > 1e-9` から `> 0` へ：旧は 0<capSavings≤1e-9 のとき国内分（≤capSavings×BondAppetite）が誰からも引かれずに
        //   国庫へ湧き、持ち手のいない元本が立っていた。capSavings=0 なら domesticPool=0＝foreignPool/pool=1 で domesticPortion は厳密に0。
        // ★2026-09-30 段4g：借入は2つの脚の移し方の1本＝国内の脚（資本家→国庫＝貯蓄の比＝旧の式そのまま・保有は払い手ごとの動いた額）と
        //   対外の脚（世界の外→国庫＝国境の出入りの台帳・貸し手の引き落としの予約）。旧＝資本家の貯蓄を手で引き、国庫へ合計を1回で足し（片側の記帳）、台帳を手で書いた。
        double domesticMoved = 0;
        if (domesticPortion > 0 && capSavings > 0)
        {
            var paid = new List<double>(capitalists.Count);
            domesticMoved = World.Transfer((w, capitalists), (w, null, null), domesticPortion, TreasuryCat.Borrow, taken: paid);   // 国内＝資本家貯蓄→国庫（純移転・クラウディングアウト）
            for (int i = 0; i < paid.Count; i++) capitalists[i].BondHoldings += new MoneyStock(paid[i]);   // ＋払った額だけの保有
        }
        World.Transfer((null, null), (w, null, null), foreignPortion, TreasuryCat.Borrow);   // 海外＝対外資本流入（BaseMoney+CumulativeTradeFlow同時＝保存）
        w.PendingForeignBorrow += foreignPortion;   // ★§1-4：貸し手側の引き落とし予約（四半期末に MultiNationSim が他国資本家から対称に控除＝両建て）。旗 ForeignLendingDebits は2026-09-26に焼き込み（x-01）
        double added = domesticMoved + foreignPortion;   // 国庫へ入った額（国内は動いた額＝払い手ごとの額の和）＝国債の元本も同じ額
        w.GovDebt += new MoneyStock(added); var foreignPortionStock = new MoneyStock(foreignPortion); w.ForeignDebt += foreignPortionStock;
        // ★段3-3（C13・2026-09-30）：対外分の債権は四半期末の貸し手の引き落とし（MultiNationSim）が**払った額そのもの**で貸し手の国へ付ける＝
        //   CapitalMarket の繰上げ（ForeignDebt−PrevForeignDebt）で数え直さない（払えなかった分は引き落としが両方から戻す）。
        w.PrevForeignDebt += foreignPortionStock;
        // R9：新発債を現行市場金利で既往クーポンへ加重平均で混ぜる（BondRolloverFracPerMonth<1の時のみ意味を持つ。既定1.0では月末の Step の rollover で GovBondRatePerYear に上書き＝ビット一致）。残高0からの借入はdebtBefore=0で現行金利へ自然収束
        { double debtBefore = w.GovDebt.Value - added; w.GovAvgCouponPerYear = (debtBefore * w.GovAvgCouponPerYear + added * w.GovBondRatePerYear) / System.Math.Max(1e-9, w.GovDebt.Value); }
        // 返り値＝借りた額（貸し手に割り当てた額）＝呼び手（月初の Simulation）が払える額の見込みへ足す。国内の脚の動いた額は払い手ごとの額の和＝割り当てた額と端数の丸め
        //   （1ulp）だけ違う＝払える額の見込み（歳入の見込みと同じ見込みの量）は割り当てた額で数える＝「払えない額をちょうど借りた月の r は 1」を保つ（2026-09-30 段4g）。
        return domesticMoved > 0 ? borrowed : foreignPortion;
    }

    /// <summary>★国債の持ち手と保有（2026-09-21 保有台帳・2026-09-30 段4g に旧 PayBondHolders を移し方の1本へ畳んだ残り）＝保有が正の POP と、その保有（受け手の重み）。
    /// 利払いと減債の国内の脚が読む（持ち手＝保有が正の POP・重み＝保有・端数は最後の持ち手）。持ち手不在（保有の和≈0）なら移し方の1本は動かさない＝国庫残置（保存）。</summary>
    /// <summary>★(351) 課税の走査＝POP の一覧と、POP ごとの並行の列（課税への同意の算入率・領主への届き方・領主の取り分・その州の領主）を**同じ 1 周**で作る（州の並び→州の中の POP の並び＝国家会計の集合）。
    /// 列は <see cref="Pops"/> と同じ長さ・同じ並びで同時に伸びる＝添字合わせが構造で消える（旧＝pops を平らにする周と並行の配列を別のなぞりで埋める周が別で、数が合わなければ黙って旧挙動へ倒していた）。
    /// 列の有無は旧と同じ門：<see cref="ScopeInc"/>＝SuffrageScopeGate&gt;0 かつ TaxConsentWeight&gt;0／<see cref="RuleMul"/>・<see cref="RuleCut"/>・<see cref="RuleLord"/>＝RuleSharesTaxBase&gt;0（門が閉じていれば作らない・その分の命令は走らない）。</summary>
    internal sealed class TaxScan
    {
        internal readonly List<Pop> Pops = [];
        internal List<double>? ScopeInc, RuleMul, RuleCut;
        internal List<Pop?>? RuleLord;

        internal static TaxScan Of(World w, Coefficients c)
        {
            var s = new TaxScan();
            bool scopeOn = c.SuffrageScopeGate > 0 && c.TaxConsentWeight > 0, ruleOn = c.RuleSharesTaxBase > 0;
            double ruleG = 0, ruleDeliver = 1.0;
            if (ruleOn) { ruleG = c.RuleSharesTaxBase; ruleDeliver = 1.0 - PoliticalQuantities.Clamp01(c.PrivilegeTaxDiscount.Value); }
            if (scopeOn) s.ScopeInc = [];
            if (ruleOn) { s.RuleMul = []; s.RuleCut = []; s.RuleLord = []; }
            foreach (var r in w.Regions)
            {
                Pop? lord = null; double mul = 1.0, cut = 0;
                if (ruleOn)
                {
                    foreach (var lp in r.Pops) if (lp.ClassId == PopClass.Noble && !lp.IsSoldier && lp.Size > 1e-9) { lord = lp; break; }   // その州の領主＝最初の貴族
                    mul = Politics.RuleDelivery(r, c, ruleG);
                    cut = r.Rule.Length == 4 ? ruleG * PoliticalQuantities.Clamp01(r.Rule[1]) * (1.0 - ruleDeliver) : 0;
                }
                foreach (var p in r.Pops)
                {
                    s.Pops.Add(p);
                    s.ScopeInc?.Add(InterestGroups.SuffrageInclusion(w, r, p));
                    if (ruleOn) { s.RuleMul!.Add(mul); s.RuleCut!.Add(lord is not null ? cut : 0); s.RuleLord!.Add(lord); }
                }
            }
            return s;
        }
    }

    private static (List<Pop> Holders, List<double> Held) BondHolders(List<Pop> pops)
    {
        var holders = new List<Pop>(); var held = new List<double>();
        foreach (var p in pops) if (p.BondHoldings.Value > 0) { holders.Add(p); held.Add(p.BondHoldings.Value); }
        return (holders, held);
    }

    /// <summary>★対外債の起票の1本（2026-09-21 保有台帳の国外振替・2026-09-30 段3-3 で国家間の移転の未払いへ広げた）：請求権 h（発行国の通貨）を
    /// 発行国の**対外債**にし、持ち手の国の対外債権（ForeignBondClaims・持ち手の国の通貨＝為替で換算）にする。
    /// <list type="bullet">
    /// <item><paramref name="p"/>≠null＝持ち手が**発行国の外へ出る**（割譲・分離建国・越境移民・招聘）ときの保有の振替＝国内元本 GovDebt−ForeignDebt が h 減り、台帳も h 減る（GovDebt は不変・金は動かない）。
    ///   ★史実：国債は発行国との契約で、持ち手の主権が変わっても債務者は変わらない（フランクフルト条約1871＝ドイツはフランス国債を
    ///   一切継承せず、アルザス＝ロレーヌの住民が持つフランス国債はフランスへの請求権のまま国外保有になった）＝POPに「どの国の国債か」の次元を持たせずに済む。</item>
    /// <item><paramref name="p"/>=null＝**国家間の移転の未払い**（貢納・賠償＝作者裁定 Q6・計画 §10-12-3-2）＝払えなかった額が払い手の国債（対外）になり、受け手の国の債権になる
    ///   （以後の利子は受け手の国の投資家へ＝国の債権を持つ量が他に無い＝Q6 の推奨どおり）。GovDebt も h 増える（金は動かない）。
    ///   ★史実：エジプトの貢納と外債（1870年代）・オスマンの戦費の外債化・仏の1871年の賠償は国債（ティエールの公債）で払われた。</item>
    /// </list>
    /// ⚠PrevForeignDebt も同額上げる＝CapitalMarket の外債レグがこの増分を「新しい対外借入」として全債権国へ按分し直さない（持ち手の国へ直接付け替え済み）。</summary>
    internal static void BookForeignDebt(Pop? p, MoneyStock h, World issuer, World holderNation)
    {
        if (!(h.Value > 0)) return;
        if (p is null) issuer.GovDebt += h; else p.BondHoldings -= h;
        issuer.ForeignDebt += h; issuer.PrevForeignDebt += h;
        holderNation.ForeignBondClaims += h * issuer.FxRate / holderNation.FxRate;
    }

    /// <summary>国債の消滅（急進革命の否認と、ソブリン・デフォルトの共通の中身＝1か所・支払いの一元化 段1-5・2026-09-28＝旧 Politics.RepudiateDebt と Fiscal のデフォルトの同じ文）。
    /// 政権交代に伴う国債の扱いは**既定は継承**（史実：復古フランスも明治政府も旧体制の債務を引き継いだ）＝**急進革命（プロレタリア独裁・無政府）のみ否認**して
    /// ここを呼ぶ（ソ連1917のツァーリ債務否認・呼び手は Politics の2か所）＝「借金を貯めてから政体を変えれば財政リセット」という非現実的な攻略を封じる。
    /// 国債と対外債を消し、保有台帳も同じ割合（全額）だけ消す＝損は持ち手（国庫へは何も戻らない・金は動かない＝請求権だけ）。
    /// 対外信用の崩壊＝兌換崩壊→外資逃避（CapitalMarket）・当面 借りられない（DefaultCooldownMonths）。
    /// C-中1（史実検証2026-07-22）：革命的否認に信用市場の代償が無かった（BorrowCooldownMonths も触らないため即座に再起債できる＝一過性Anarchyが債務ロンダリング装置になる）。
    /// 史実の代償は「永久追放」でなく長期市場からの排除＋高コスト短期化（ソヴィエト1918否認→ジェノヴァ1922決裂、以後も短期の供給者信用は継続）。
    /// ※（★(373) 制度疲労ショックは物理削除）信用凍結・正統性はデフォルトの側だけが足す（革命そのものが既に体制転換＝二重計上を避ける）。（旗 RepudiationCreditPenalty は 2026-09-26 に焼き込み）</summary>
    internal static void RepudiateDebt(World w, Coefficients c)
    {
        w.GovDebt = new MoneyStock(0); w.ForeignDebt = default;
        foreach (var (_, p) in w.AllPopsWithRegion()) p.BondHoldings = default;   // ★保有台帳（2026-09-21）
        w.OnGoldStandard = false; w.GoldStandardSuspendedInCrisis = true;
        w.BorrowCooldownMonths = c.DefaultCooldownMonths;
    }

    /// <summary>★保有台帳の起票（2026-09-21）：国内債があるのに台帳が空の国（開始盤面の初期国債＝仏の米独立戦争債務／台帳導入前のセーブ）に、
    /// **起債の購入と同じ規則**（投資家の貯蓄比）で保有を起こす。金は動かさない（既に在る請求権の持ち主を記すだけ）。
    /// 台帳が立っている国・国内債0（検査の許容内）の国では何もしない＝通常の走では構築時に1回だけ効く。投資家の貯蓄が0なら全POPの頭数比（<see cref="World.Distribute"/> と同じ最後の受け皿）。</summary>
    internal static void SeedBondHoldings(World w)
    {
        MoneyStock dom = w.GovDebt - w.ForeignDebt, held = default; double invSav = 0, size = 0;
        foreach (var (_, p) in w.AllPopsWithRegion()) { held += p.BondHoldings; size += p.Size; if (CapitalMarket.IsInvestor(p)) invSav += Math.Max(0, p.Savings.Value); }
        if (held.Value > 0 || !(dom.Value > 0) || BondLedger.Balanced(w, held)) return;   // 検査が割れない（端数の残差だけ）なら起票しない＝台帳入りのセーブはビット不変
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
                p.BondHoldings += dom * (invSav > 0 ? (CapitalMarket.IsInvestor(p) ? Math.Max(0, p.Savings.Value) / invSav : 0) : (size > 0 ? p.Size / size : 0));
    }
}
