namespace PriceOfBread.SimCore;

/// <summary>
/// メリトクラシー＝社会移動（昇格・転落）。近代的利益団体（専門職・ブルジョワ＝IsModernElite）の
/// 勢力割合＋政体の近代性で **メリトクラシー値 M（0..1）** を決め、M が高いほど教育済みPOP
/// （識字の高い都市労働者）が上の階級（専門職・技師）へ昇格する。
/// 社会移動の全体像（四半期）：
/// ① 下層→中流：識字×高等教育（中流が買えた余剰教育）で専門職へ（工業化が職の器）。
/// ② 下層→聖職者：宗教学校の社会で聖職者が儲かると、識字の高い者が教会へ＝聖職者の政治力↑→世俗化を阻む罠。
/// ③ 中流→資本家：十分な資金（経済規模比の閾値）を貯めた中流は資本家階級へ。
/// ④ 上流→中流：貴族・資本家・聖職者も資金が尽きると転落（恐慌＝資本家の大量転落）。
/// ⑤ 中流→下層：収入が絶え貧した専門職（失業教師など）は労働者へ転落。
/// 転落は不満（闘争度）を運ぶ＝没落者は革命の火種。規模・貯蓄は保存移動。
/// ①'' 失業プール（2026-08-15作者指示・JobSearchRate=0.025点火）：失業者は階級（上流/中流/下流）のプールに入り、
///     識字×高等教育の資格で判断して同階級のもっともマシな職へ転職・不可能なら1段下の最良職へ降格。
///     >0 で没落④⑤⑥・手工業の没落①' の行き先も同経路（無条件「→労働者」の廃止）。
/// </summary>
public static class Meritocracy
{
    private const double Eps = 1e-9;
    private const double MaxUrbanShareCap = 0.85;   // 都市化率の絶対上限（PopDecision.MaxUrbanShareCap と同値・重複はcross_file_needs参照）

    public static void Step(World w, Coefficients c)
    {
        Span<int> targets = stackalloc int[5];     // 昇格先（技師・教師・官僚・医者・★研究者）の作業領域（ループ外確保）。⚠5がちょうど上限＝これ以上足すなら幅も広げる
        double meritW = 0, meritP = 0;             // メリトクラシー値の全活性州 人口加重平均（グローバル診断値。旧：毎州上書きで最後の州だけ残るバグ）
        bool fallReform = c.FallAnchorReform > 0;   // rank7（点火0.3）：>0で③④⑤⑥の物差しを結晶資産込み＋所得年数アンカーへ（0＝下の分岐が一切走らない厳密恒等）。州に依存しない＝ループ外で1回だけ評価
        bool gateEma = c.EducationGateUsesEma > 0;  // 高等教育ゲートに瞬間Fillでなく多年EMAを使うか。州に依存しない＝ループ外で1回だけ評価
        // ★(155) 兵士の席（⑦）の国の量＝ループ外で1回（定員は国の和＝州へは州の母数÷国の母数で配る＝Military.Step の強制の徴募と同じ1本）
        double seatsNat = Military.EstablishmentOf(w, c, out double seatNatMan);
        bool atWar = w.AtWarNow;   // 戦時は復員しない（Military.Step の交戦の判定と同じ＝自国の戦争が1本でもある）
        double dischargeQ = 1.0 - Math.Pow(1.0 - c.SoldierMigrationRate, Calendar.MonthsPerQuarter);   // 平時の定員超過を戻す速さ＝旧の月の徴募/離職の速さの四半期の値（新しい数なし）
        double volunteers = 0;   // この四半期に席へ入った志願兵（国の和）＝熟練を薄める（Military.DiluteSkill）
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;

            double total = 0, modern = 0, totalSav = 0, totalAsset = 0, closed = 0;
            foreach (var p in r.Pops)
            {
                total += p.Size; totalSav += Math.Max(0, p.Savings); if (p.IsModernElite) modern += p.Size;
                if (p.Closed) closed += p.Size;   // ★AUD-07：身分（貴族・聖職）の人口シェア＝政体ラベルの代わりに読む実体
                if (fallReform) totalAsset += Math.Max(0, p.Savings);   // rank7(a)：分母も判定資産と同じ定義＝対称（結晶資産は2026-09-26に削除＝常に0だった）
            }
            if (total <= Eps) continue;
            double avgPerCap = totalSav / total;   // 地域の1人あたり平均貯蓄＝閾値の経済規模スケール
            double fallAvg = fallReform ? totalAsset / total : avgPerCap;   // rank7：③④⑤⑥のアンカー分母（f=0でavgPerCapと同一参照値）

            // M ＝ 政体の近代性 ＋ 近代的利益団体の勢力割合
            // ★AUD-07（2026-09-03）：政体ラベルの固定表 → 身分の閉鎖度からの導出へ（MeritModernityFromClosed=0 で旧＝厳密恒等）。
            double modernity = c.MeritModernityFromClosed > 0
                ? PoliticalQuantities.Clamp01(1.0 - closed / total) * c.MeritModernityFromClosed
                : PolityModernity(w.Polity);
            double m = PoliticalQuantities.Clamp01(modernity + (modern / total) * c.MeritGroupWeight);
            meritW += m * total; meritP += total;   // グローバル値は全州の人口加重平均でループ後に確定（per-regionの昇格は下で local m を使う）

            if (r.MeritFromIndex < 0 || r.MeritToIndex < 0) continue;
            var from = r.Pops[r.MeritFromIndex];   // 都市労働者

            // 昇格先の候補＝中流の専門職：技師＋（居れば）教師・官僚・医者。
            int tn = 0;
            targets[tn++] = r.MeritToIndex;
            if (r.TeacherPopIndex >= 0) targets[tn++] = r.TeacherPopIndex;
            if (r.BureaucratPopIndex >= 0) targets[tn++] = r.BureaucratPopIndex;
            if (r.DoctorPopIndex >= 0) targets[tn++] = r.DoctorPopIndex;
            // ★段②（2026-08-17）：研究者もインテリ（医者/技師/研究者）の1人＝昇格先。⚠-1（研究者POPを持たない世界）では数えない＝tn が変わらず厳密恒等。
            if (r.ResearcherPopIndex >= 0) targets[tn++] = r.ResearcherPopIndex;
            // ※商人は昇格先に入れない：教育資格の要る専門職でなく商売の元手の問題＝増減は階級遷移（PopDecision）に委ねる。
            //   （旧・枠時代は商人の算入が枠を埋めて官僚・教師の育成を凍結する事故も起こした＝行政力0.03を観測）

            // ===== ① 下層 → 中流（専門職）：**給与差**で職を選ぶ（枠＝計画経済の残滓は廃止）=====
            // 言語は地域間移動（PopDecision §9-D）と同じ「1人あたり所得（Earned/Size）」。
            // その職の給与が労働者の給与×Premium を超える時だけ志望者が流れ、参入で給与が薄まれば自然に止まる。
            // 出口は⑤（貯蓄が尽きた中流の転落）＝参入は所得・退出は資産枯渇＝時間差が不感帯になり発振を抑える。
            // 旧・雇用枠（M×工業化度×上限×職別比率）は需要の代理変数だったが、需要は給与（役務の売上→賃金）が
            // より正確に表現する（貧しい世界＝医療が売れない＝医者の給与が低い＝誰も目指さない）。
            // 枠の事故2件（技師の枠占有による教師崩壊／枠が初期シェア未満で昇格凍結）もこれで根絶。
            double fromIncome = from.Size > Eps ? Math.Max(0, from.Earned) / from.Size : 0;
            // 世俗水路：神学校の高等教育は技師・教師・医者の資格にならない＝宗教教育の社会はこれらを**再生産できない**
            // （既存の専門職は残るが増えない＝エリートの創発（TechEducation）が階級構成で停滞する経路）。
            // 官僚は例外＝宗教社会も官僚制は築ける（科挙・寺社官僚）。代わりに官僚はエリートに数えない（同所参照）。
            double secular = w.EducationLaw switch
            {
                EducationLaw.Public or EducationLaw.Compulsory => 1.0,
                EducationLaw.Religious => c.ReligiousProfessionShare,
                _ => 0.0,
            };
            // 高等教育ゲート：中流が買えた教育（＝下層が買った後の**余剰**教育）が専門職への転職資格。
            // 教育の供給が下層の基礎教育で尽きる社会では専門職が育たない。教育財の無い小シナリオは Fill=1.0＝従来挙動。
            // 人13：既定は当月 EducationFill（瞬間フロー）＝不況で1ヶ月教育が買えないと社会移動が即凍結。EducationGateUsesEma=1 で
            // 多年EMA（修学年数の蓄積ストック＝EducationEma・識字と同型の緩和）をゲートに使う＝一時的な教育の途絶では資格が消えない（時間スケールの整合）。
            double he = 0, hw = 0;
            foreach (var p in r.Pops) if (p.IsModernElite) { he += (gateEma ? p.EducationEma : p.EducationFill) * p.Size; hw += p.Size; }
            double higherEdu = hw > Eps ? he / hw : 1.0;
            // ★2026-08-12（作者裁定②「移民の国の識字が低いのは正しい。そこから教師が時間とともに増えるならよい」）：
            //   資格の物差しが「**エリートに届いた余剰**教育（higherEdu）」だけなので、教育が不足している社会では
            //   資格が厳密に0になり、しかも下の flow は乗算なので**価格も賃金もいくら叫んでも流量が0**になる。
            //   ＝教育不足→余剰0→資格0→教師を育てられない→教育不足、の完全な自己閉鎖ループ。
            //   実測：移民で人口が2倍以上に膨らむアメリカ・ネーデルラントで教師密度が希釈され、識字が
            //   60%→28%・56%→23%へ**下がる**（人口成長に教師供給が原理的に追随できない）。
            //   欠けている物理＝**学校を出た平民から教師が出る**という再生産経路。教師は貴族の余暇からは出ない。
            //   志望者本人の就学（下層が優先的に買えた EducationFill／そのEMA）を混ぜて閂を外す。0で厳密恒等。
            //   ★形は**混合でなく max**（2026-08-13の是正）。初版は higherEdu と ownEdu の線形混合にしたが、
            //     ownEdu < higherEdu の国では**既存の資格経路を黙って下げて**しまい、イギリスが所得税を解禁できなくなった
            //     （TaxTests「所得税は歳入を生み保存則を保つ」が赤）。壊したかったのは自己閉鎖であって
            //     既存の経路ではない。「エリート層に余剰教育がある」か「本人が学校を出た」かの**どちらかで資格になる**＝
            //     職への入り口が2本あるという実際の話であり、下げる方向には決して働かない（単調）。
            //   ⚠恒等性は max で保証される（higherEdu ≥ 0 なので k=0 では max(higherEdu, 0) == higherEdu＝ビット恒等）。
            //   ★対象は**官僚を除く**専門職（2026-08-13の是正・一貫性）。官僚は既にすぐ下で「宗教社会も官僚制は築ける
            //     （科挙・寺社官僚）」という理由から世俗の水路(secular)を免除されている＝**近代教育の系に属さない**と
            //     既に判断済みの職である。そこへ近代的な就学の経路だけ足すのはその判断と矛盾する。
            //     実害も出た：官僚が増えると行政財の必要量が増えて同じ予算では AdminFill が薄まり、行政力が育たない
            //     （当時は所得税の解禁ゲートに届かず TaxTests が赤・教育と全く同じ希釈の形。ゲート自体は2026-09-12に廃止）。
            double gateEduPro = higherEdu;
            if (c.PromotionGateOwnSchooling > 0)
                gateEduPro = Math.Max(higherEdu, c.PromotionGateOwnSchooling * (gateEma ? from.EducationEma : from.EducationFill));

            // ===== ⑦ 兵士の席（2026-10-02 (155)・作者承認 2026-10-01「それでよい。ただし兵士は他の職とは性質が違うので、多少の外科的な処置は許可する」＝推奨で進めた）=====
            // 兵士は下流（0）の職の1つ＝席の数は制度（定員＝Military.EstablishmentOf の州按分＝州の母数÷国の母数）・給与は国家の規定（Simulation の SoldierPayBudget）。
            // ・志願＝失業プール（①''）と強制の転落（④⑤⑥・①'）の職の選択に兵士の席が入る（席の空きまで・FillJobs）。
            // ・平時に定員を超えた分＝国家が兵を民間の最良の職へ戻す（旧 Military の DischargeToPool＝農民と都市労働者へ比例を置き換え）。速さは旧の平時の徴募/離職の速さ（月 SoldierMigrationRate）の四半期の値。
            //   ★(161) 行き先の比べ方は JobIncome の1本（民間の職を働き手1人あたりへ直す）＝兵の給与×転職のプレミアムを超える職へ按分（低温の softmax）・超える職が無ければ強制の残りが最良の1つへ（総取り）。
            // ・兵士だけの差分（許可済み）：志願兵は服役の年限ですぐ辞めない＝職の比較で自分から出ていかない（①'' の対象は未払いの分だけ＝兵士の「失業」＝給与の未払いの割合）・
            //   戦時は復員しない（下の枝は平時だけ）・徴兵は強制で席を埋める（Military.Step）。
            Pop? soldierPop = null;
            foreach (var p in r.Pops) if (p.IsSoldier) { soldierPop = p; break; }   // 最初の一致＝Military.Step と同じ探し方
            double soldierVac = 0;
            if (soldierPop is not null && seatNatMan > Eps)
            {
                double seatsR = seatsNat * (Military.RegionManpower(r, c) / seatNatMan);
                if (!atWar && soldierPop.Size > seatsR + Eps && c.JobSearchRate > 0)
                {
                    double noSeat = 0;   // 復員の行き先に兵士の席は入れない（自分自身＝FillJobs が ReferenceEquals で外す）
                    RouteViaPool(r, w, m, avgPerCap, soldierPop, 0, dischargeQ * (soldierPop.Size - seatsR), secular, higherEdu, gateEma, c, ref noSeat, forced: true);
                }
                soldierVac = Math.Max(0, seatsR - soldierPop.Size);
            }
            double soldierVac0 = soldierVac;   // 志願で埋まった席＝空きの減り（MoveToJob が動いた人数だけ空きから引く）

            for (int i = 0; i < tn; i++)
            {
                var t = r.Pops[targets[i]];
                if (t.Size <= Eps) continue;   // 完全に空の階級は給与が観測できない（転落は比例移動＝厳密に0にはならないので実質起きない）
                double gate = targets[i] == r.BureaucratPopIndex ? 1.0 : secular;
                if (gate <= Eps) continue;
                // 魅力＝その職の給与 ÷（自分の給与×転職プレミアム）。失業者（所得0）はどんな職にも飛びつく（流量clampが暴走を抑える）。
                double attract = Math.Max(0, t.Earned) / Math.Max(Eps, t.Size) / Math.Max(Eps, fromIncome * c.PromotionPremium);
                if (attract <= 1.0) continue;
                // #14 少数民族の昇格摩擦（ガラスの天井）：少数派ほど(core<1)昇格しにくい＝下層に滞留。均質pop(core=1)で係数1＝中立。
                // rank17（点火0.5 MinorityPenaltyModernBlend）：開かれた社会（M高）ほど法的障壁が畳まれる。blend=0で×1.0＝旧と厳密恒等。
                double effPen = c.MinorityPromotionPenalty * (1.0 - c.MinorityPenaltyModernBlend * m);
                double minorityF = w.EthnicEnabled ? Math.Max(0, 1.0 - effPen * (1.0 - from.CoreEthnicShare)) : 1.0;
                double gateEdu = targets[i] == r.BureaucratPopIndex ? higherEdu : gateEduPro;   // 官僚は旧来の資格（上のコメント）
                // ★段②（2026-08-17・作者裁定）：**技師・研究者だけ**は志望者本人が高等教育（財136）を買えていることを要する。
                //   ⚠教師・医者・官僚は対象外＝初等教員も町医者も大学を出ていない時代の話（大学の希少さがそのまま昇格の律速になる）。
                //   ⚠既存の gateEdu（エリートの余剰教育／本人の就学）に**乗算**＝資格を下げる方向にしか働かない単調な絞り。
                //   PromotionGateHigherEdu=0 でこの行が走らない＝**厳密恒等**。
                if (c.PromotionGateHigherEdu > 0 && (targets[i] == r.MeritToIndex || targets[i] == r.ResearcherPopIndex))
                    gateEdu *= 1.0 - c.PromotionGateHigherEdu * (1.0 - from.HigherEduFill);
                double flow = c.PromotionSpeed * m * from.Literacy * gateEdu * gate * from.Size * Math.Min(attract - 1.0, 1.0) * minorityF;
                if (c.CohortGateK > 0) flow *= CohortGate(from, c);   // rank8（休眠）：昇格は入職時＝新成人流量で律速（0＝この行が走らない厳密恒等）
                Move(from, t, Math.Min(flow, from.Size), c, selectLiteracy: true, genderAccess: GenderAccess(w, r, targets[i], c));   // 人1：昇格は識字上位の選抜（下層から識字の高い者が抜ける＝srcの識字が下がる。既定は選抜あり=点火）。rank12：性別の選抜運搬（既定1.0点火・専門職のみ）
            }

            // ===== ①* 労働者 → 小営業（職人・商人）：徒弟→親方・行商→店舗（リアリティ監査rank6・PettyEntryRate=0.02点火2026-08-16）=====
            // 実史の下層上昇路の中段。所得差駆動（①と同じ言語・識字ゲート不要＝腕と元手の職）×元手ゲート（1人あたり貯蓄≥地域平均）。
            // ⚠出口（①' ArtisanDeclineRate）と同時点火が条件＝出口単独では職人が単調消滅（1848急進主義の母体が消える）。
            if (c.PettyEntryRate > 0 && avgPerCap > Eps && from.Size > Eps
                && from.Savings / from.Size >= avgPerCap)
            {
                for (int pi = 0; pi < 2; pi++)   // 候補2つ＝職人・商人（stackallocは州ループ内でCA2014＝素朴な2択で書く）
                {
                    int idx = pi == 0 ? r.ArtisanPopIndex : r.MerchantPopIndex;
                    if (idx < 0) continue;
                    var t = r.Pops[idx];
                    if (t.Size <= Eps) continue;
                    double attract = Math.Max(0, t.Earned) / Math.Max(Eps, t.Size) / Math.Max(Eps, fromIncome * c.PromotionPremium);
                    if (attract > 1.0)
                    {
                        double petty = Math.Min(c.PettyEntryRate * from.Size * Math.Min(attract - 1.0, 1.0), from.Size);
                        if (c.CohortGateK > 0) petty *= CohortGate(from, c);   // rank8（休眠）：徒弟→親方も入職時の移動＝世代律速
                        Move(from, t, petty, c);
                    }
                }
            }

            // ===== ①' 職人 → 都市労働者（手工業の没落・2026-08-15作者指摘「農民や職人が工場に生産性で負けて流れるロジック」）=====
            // 工場に負けた職人は賃金労働へ流れる（ラッダイトの前段）。駆動は①と同じ言語＝**失業でなく1人あたり所得差**
            //（工場が職人の市場を奪う→職人のEarnedが痩せる→労働者給与との比が閾値を超える→流出）。識字ゲート不要（下方移動）。
            // ArtisanDeclineRate=0.01点火（2026-08-16・S級梯子r6+9＝入口PettyEntryRateと同時）。0なら1命令も走らない厳密恒等。
            if (c.ArtisanDeclineRate > 0 && r.ArtisanPopIndex >= 0)
            {
                var artisan = r.Pops[r.ArtisanPopIndex];
                if (artisan.Size > Eps && from.Size > Eps)
                {
                    double artIncome = Math.Max(0, artisan.Earned) / artisan.Size;
                    double wrkIncome = Math.Max(0, from.Earned) / from.Size;
                    double attract = wrkIncome / Math.Max(Eps, artIncome * c.PromotionPremium);
                    if (attract > 1.0)
                    {
                        double f = Math.Min(c.ArtisanDeclineRate * artisan.Size * Math.Min(attract - 1.0, 1.0), artisan.Size);
                        if (c.CohortGateK > 0) f *= CohortGate(artisan, c);   // rank8（休眠）：所得差駆動の緩い没落＝非ショック（破産⑥は素通しのまま）
                        // ★特権 段4（同業組合・2026-09-07）：組合が職人を職に縛る＝工場への移動が法の度合×GuildMobilityDrag だけ遅れる（歪み＝表裏の裏）。0＝恒等
                        if (c.GuildLawGate > 0 && c.GuildMobilityDrag > 0) f *= 1.0 - PoliticalQuantities.Clamp01(c.GuildMobilityDrag * Privileges.GuildDegree(w));
                        // 失業プール点火時は行き先も同経路（資格があれば専門職に踏み留まる職人＝没落一直線でない）
                        if (c.JobSearchRate > 0) RouteViaPool(r, w, m, avgPerCap, artisan, 1, f, secular, higherEdu, gateEma, c, ref soldierVac, forced: true);
                        else Move(artisan, from, f, c);
                    }
                }
            }

            // ===== ①'' 失業プール（2026-08-15作者指示・0.025点火＝live）：失業者は階級のプールに入り、同階級のもっともマシな職へ =====
            // 対象＝労働を市場で売る全pop（労働力/農業/高度＝UnemploymentRate が立つ層。自給農の自作は失業に数えない：EconomyStep）。
            // 「プール」は四半期内の判定＝滞留の動力学は UnemploymentRate 自体が持つ（売れ残りが続く限り毎期プールに入り続ける）。
            // JobSearchRate=0 なら1命令も走らない厳密恒等（点火済み＝既定0.025。OFF対照はPOB_COEF="JobSearchRate=0"）。
            // ★(155)：兵士も対象＝兵士の「失業」は給与の未払いの割合（Simulation が国家の支払い率から書く・多年EMA＝未払いが長引くほど強い）＝未払いが長引けば脱走して民間の最良の職へ（強制＝席に留まれない）。
            if (c.JobSearchRate > 0)
                for (int i = 0; i < r.Pops.Count; i++)
                {
                    var p = r.Pops[i];
                    if (p.Closed || p.Size <= Eps) continue;
                    // rank10b（点火0.02 JobSearchUsesEma）：駆動は慢性失業（U多年EMA）＝循環失業は滞留して闘争度へ。0＝瞬間U（旧）。
                    double u = c.JobSearchUsesEma > 0 && p.UnemploymentEma >= 0 ? p.UnemploymentEma : p.UnemploymentRate;
                    if (u <= Eps) continue;
                    int cls = ClassOf(r, i);
                    if (cls < 0) continue;
                    // rank10a（点火0.5 JobSearchLiteracyWeight）：探索効率の識字連動（新聞求人・文書応募の代理）。0で×1.0＝旧と厳密恒等。
                    double eff = c.JobSearchRate * (1.0 - c.JobSearchLiteracyWeight * (1.0 - p.Literacy));
                    double poolFlow = eff * u * p.Size;
                    if (c.CohortGateK > 0) poolFlow *= CohortGate(p, c);   // rank8（休眠）：失業転職も大流量の非ショック移動＝世代律速（④⑤⑥の強制転落は素通し）
                    RouteViaPool(r, w, m, avgPerCap, p, cls, poolFlow, secular, higherEdu, gateEma, c, ref soldierVac, forced: p.IsSoldier);   // 脱走は強制（席に留まれない）
                }

            // ===== ② 下層 → 聖職者（宗教学校の社会のみ）：教育で儲かる聖職者は魅力的な職 =====
            // 工業化に依らない前近代の上昇路。教育済みPOPが教会に吸われ、聖職者の富＝政治力が増え、
            // 公教育への移行（世俗化）への抵抗が強まる＝教育投資が自らの近代化を阻む自己強化の罠。
            if (w.EducationLaw == EducationLaw.Religious && r.ClergyPopIndex >= 0 && avgPerCap > Eps)
            {
                var clergy = r.Pops[r.ClergyPopIndex];
                double clergyPerCap = clergy.Size > Eps ? clergy.Savings / clergy.Size : 0;
                double attract = clergyPerCap / avgPerCap;   // 平均比の豊かさ＝魅力
                if (attract > c.ClergyAttractThreshold)
                    Move(from, clergy, c.ClergyJoinRate * from.Literacy * from.Size * Math.Min(1.0, attract / c.ClergyAttractThreshold - 1.0), c, selectLiteracy: true);   // 人1：聖職者昇格も識字比例＝選抜
            }

            // ===== ②'' 聖職者 → 教師/都市労働者（世俗化の退出・AI審査3-5・2026-08-31★live同日点火 0.004）＝②の対称 =====
            // 入口（②）だけの非対称の是正：公教育/義務教育の社会では教会が教育を売れず（収入減は既存）、
            // 聖職の実入りが平均を割ると人も出て行く＝1934年でも身分比が1783年のままの是正。
            // 行き先＝教師（識字を持つ職の連続性・居れば）／いなければ都市労働者。ClergyExitRate=0＝厳密恒等。
            if (c.ClergyExitRate > 0 && w.EducationLaw != EducationLaw.Religious && r.ClergyPopIndex >= 0 && avgPerCap > Eps)
            {
                var clergyOut = r.Pops[r.ClergyPopIndex];
                if (clergyOut.Size > Eps)
                {
                    double poverty = 1.0 - (clergyOut.Savings / clergyOut.Size) / avgPerCap;   // 平均より貧しいほど強く退出（豊かなら0＝壁でなく逓減）
                    if (poverty > 0)
                    {
                        Pop? dest = r.TeacherPopIndex >= 0 ? r.Pops[r.TeacherPopIndex] : null;
                        if (dest is null)
                            foreach (var pp in r.Pops) if (pp.IsCommoner && pp.ClassId == PopClass.Worker) { dest = pp; break; }
                        if (dest is not null) Move(clergyOut, dest, c.ClergyExitRate * poverty * clergyOut.Size, c);
                    }
                }
            }

            // ===== ②' 農民 → 教師/聖職者（農村識字の直行便・リアリティ監査rank16・休眠 RuralPromotionRate）=====
            // 識字が高く工業化が遅い社会（北欧・スコットランド・明治日本型）では、都市化天井が「村の秀才→師範」を
            // ①の二段経路（都市化→翌期昇格）ごと塞ぐ。天井と独立の小水路1本＝§2.4（都市化は工業化に従う）は不壊。
            // ゲートは①と同一の物差し（m×識字×高等教育×世俗・本人就学PromotionGateOwnSchooling込み）。
            if (c.RuralPromotionRate > 0)
            {
                Pop? peasant = null;
                foreach (var p in r.Pops) if (p.IsCommoner && !p.IsUrban && p.ClassId == PopClass.Peasant) { peasant = p; break; }
                if (peasant is { Size: > Eps } pz)
                {
                    double pzInc = Math.Max(0, pz.Earned) / pz.Size;
                    double pzEdu = higherEdu;
                    if (c.PromotionGateOwnSchooling > 0)
                        pzEdu = Math.Max(higherEdu, c.PromotionGateOwnSchooling * (gateEma ? pz.EducationEma : pz.EducationFill));
                    if (r.TeacherPopIndex >= 0 && secular > Eps)
                    {
                        var t = r.Pops[r.TeacherPopIndex];
                        if (t.Size > Eps)
                        {
                            double attract = Math.Max(0, t.Earned) / Math.Max(Eps, t.Size) / Math.Max(Eps, pzInc * c.PromotionPremium);
                            if (attract > 1.0)
                                Move(pz, t, Math.Min(c.RuralPromotionRate * m * pz.Literacy * pzEdu * secular * pz.Size * Math.Min(attract - 1.0, 1.0), pz.Size), c, selectLiteracy: true);
                        }
                    }
                    // 宗教社会では聖職禄が農村の受け皿（②と同型・率だけ本係数）
                    if (w.EducationLaw == EducationLaw.Religious && r.ClergyPopIndex >= 0 && avgPerCap > Eps)
                    {
                        var clergy = r.Pops[r.ClergyPopIndex];
                        double clergyPerCap = clergy.Size > Eps ? clergy.Savings / clergy.Size : 0;
                        double attract = clergyPerCap / avgPerCap;
                        if (attract > c.ClergyAttractThreshold)
                            Move(pz, clergy, Math.Min(c.RuralPromotionRate * pz.Literacy * pz.Size * Math.Min(1.0, attract / c.ClergyAttractThreshold - 1.0), pz.Size), c, selectLiteracy: true);
                    }
                }
            }

            // ===== ③ 中流 → 資本家：十分な資金を貯めた中流は資本家階級へ（閾値＝経済規模比・履歴差で発振防止）=====
            if (r.InvestorPopIndex >= 0 && avgPerCap > Eps)
            {
                var capitalist = r.Pops[r.InvestorPopIndex];
                for (int i = 0; i < tn; i++)
                {
                    var t = r.Pops[targets[i]];
                    if (t.Size <= Eps) continue;
                    // rank7（点火 FallAnchorReform=0.3）：判定資産＝結晶込み・アンカー＝自分の所得年数ブレンド（f=0で旧式にビット恒等）。
                    // ③⑤⑥④の全てに同じ物差し（1箇所だけ直すと按分6箇所ルールと同じ罠＝監査の明記）。
                    if (AssetPerCap(t) > c.CapitalRiseThreshold * FallAnchor(t, fallAvg, c))
                        Move(t, capitalist, c.CapitalRiseRate * t.Size, c);
                }
                // rank1（点火1.0 MerchantArtisanCapitalRise）：商業・手工業資本→産業資本（Crouzet＝第一世代工場主の過半が商人・職人出自。
                // targets列挙が専門職のみ＝商人・職人がどれほど貯めても資本家になれない凍結の解除）。閾値・率は既存を共用（本係数は率の乗数）。
                if (c.MerchantArtisanCapitalRise > 0)
                {
                    for (int pi = 0; pi < 2; pi++)   // 候補2つ＝商人・職人（stackallocは州ループ内でCA2014＝素朴な2択で書く）
                    {
                        int idx = pi == 0 ? r.MerchantPopIndex : r.ArtisanPopIndex;
                        if (idx < 0) continue;
                        var t = r.Pops[idx];
                        if (t.Size <= Eps) continue;
                        if (AssetPerCap(t) > c.CapitalRiseThreshold * FallAnchor(t, fallAvg, c))   // rank7：③と同じ物差し
                            Move(t, capitalist, c.CapitalRiseRate * c.MerchantArtisanCapitalRise * t.Size, c);
                    }
                }
            }

            // ===== ④ 上流 → 中流：貴族・資本家・聖職者も資金が尽きると転落（恐慌＝資本家の大量転落）=====
            // 資本家はプチブル（職人＝小資本/自営）へ、貴族・聖職者は中流の専門職へ。転落は不満を運ぶ（没落者＝革命の火種）。
            // 破産（貯蓄が枯渇し信用で延命中）の資本家は高い率で速やかに抜く＝ゾンビ工場の信用延命（LossFinanced）を断つ。
            if (avgPerCap > Eps && tn > 0)
            {
                int best = targets[0];
                double bestPc = -1;
                for (int i = 0; i < tn; i++)
                {
                    var t = r.Pops[targets[i]];
                    double pc = t.Size > Eps ? t.Savings / t.Size : 0;
                    if (pc > bestPc) { bestPc = pc; best = targets[i]; }
                }
                foreach (var p in r.Pops)
                {
                    bool isCapitalist = r.InvestorPopIndex >= 0 && p == r.Pops[r.InvestorPopIndex];
                    bool upper = p.Closed || isCapitalist;
                    if (!upper || p.IsSoldier || p.Size <= Eps) continue;
                    // 聖職者は人口比の下限で没落が止まる＝世俗化で明確に減るが**教会は消えない**（設計仕様）。
                    if (r.ClergyPopIndex >= 0 && p == r.Pops[r.ClergyPopIndex] && p.Size <= c.ClergyFloorShare * total) continue;
                    double pc = AssetPerCap(p);   // rank7(a)：Savings/Size（結晶資産は2026-09-26に削除＝常に0だった）
                    double anchor = FallAnchor(p, fallAvg, c);   // rank7(b)（点火0.3）：f>0で自分の平滑所得×年数へブレンド。f=0で avgPerCap
                    // ★返済不能（POPの個人債務・2026-08-02）：債務が年所得の DebtRuinRatio 倍を超えたら**没落の判定に算入**する。
                    // ⚠★破産判定そのもの（pc）は `Savings` 据え置き＝**純資産（Savings−Debt）にしない**。純資産にすると
                    //   (a) 分母 avgPerCap（Σmax(0,Savings)）と基準が食い違い、(b) レバレッジの高い黒字資本家を
                    //   BankruptcyFallRate=0.15 で大量に落として較正を壊す。債務の帰結は**別の判定**として足すのが正しい。
                    bool debtRuined = c.PopDebtDriven > 0 && c.DebtRuinRatio > 0
                                   && p.Debt > c.DebtRuinRatio * Math.Max(Eps, p.Earned * Calendar.MonthsPerYear);
                    if (!debtRuined && pc >= c.UpperFallThreshold * anchor) continue;
                    // rank7(c)（点火0.3）：転落率の深さ連続化＝浅い割れはゆっくり・深い割れ（恐慌）は速い。debtRuined は素通し（債務破滅は速いまま）。f=0で1.0＝ビット恒等
                    double depth = FallDepth(pc, c.UpperFallThreshold * anchor, debtRuined, c);
                    if (c.JobSearchRate > 0)
                    {
                        // ①''点火時：転落の行き先もプール経路（同階級＝上流は入れる職なし→中流の最良職へ資格の按分・残りは職人等へ）
                        double rate = isCapitalist && (debtRuined || pc < c.InsolventThreshold * anchor) ? c.BankruptcyFallRate : c.UpperFallRate;
                        RouteViaPool(r, w, m, avgPerCap, p, 2, rate * depth * p.Size, secular, higherEdu, gateEma, c, ref soldierVac, forced: true);
                    }
                    else if (isCapitalist && r.ArtisanPopIndex >= 0)
                    {
                        // 破産資本家＝信用で延命中なら速やか、単に平均より貧しいだけなら緩やかにプチブル（職人）へ。
                        double rate = debtRuined || pc < c.InsolventThreshold * anchor ? c.BankruptcyFallRate : c.UpperFallRate;
                        Move(p, r.Pops[r.ArtisanPopIndex], rate * depth * p.Size, c);
                    }
                    else
                        Move(p, r.Pops[best], c.UpperFallRate * depth * p.Size, c, genderAccess: GenderAccess(w, r, best, c));
                }
            }

            // ===== ⑤ 中流 → 下層：収入が絶え貧した専門職（失業教師など）は労働者へ転落 =====
            if (avgPerCap > Eps)
                for (int i = 0; i < tn; i++)
                {
                    var t = r.Pops[targets[i]];
                    if (t.Size <= Eps) continue;
                    double pc5 = AssetPerCap(t);   // rank7：③④⑥と同じ物差し（f=0でビット恒等）
                    double thr5 = c.MiddleFallThreshold * FallAnchor(t, fallAvg, c);
                    if (pc5 < thr5)
                    {
                        double depth5 = FallDepth(pc5, thr5, false, c);   // rank7(c)：深さ比例（f=0で1.0）
                        // ①''点火時：まず同階級で今より稼げる職（貧した教師→給与の立つ技師等）・駄目なら下層の最良職へ
                        if (c.JobSearchRate > 0) RouteViaPool(r, w, m, avgPerCap, t, 1, c.MiddleFallRate * depth5 * t.Size, secular, higherEdu, gateEma, c, ref soldierVac, forced: true);
                        else Move(t, from, c.MiddleFallRate * depth5 * t.Size, c);
                    }
                }

            // rank1対称（点火1.0 MerchantArtisanCapitalRise）：商人の転落も解凍（⑤と同型。職人は⑥が既に担う）＝凍結を両方向解く。
            if (c.MerchantArtisanCapitalRise > 0 && r.MerchantPopIndex >= 0 && avgPerCap > Eps)
            {
                var mt = r.Pops[r.MerchantPopIndex];
                double pcM = mt.Size > Eps ? AssetPerCap(mt) : 0;   // rank7：⑤と同じ物差し
                double thrM = c.MiddleFallThreshold * FallAnchor(mt, fallAvg, c);
                if (mt.Size > Eps && pcM < thrM)
                {
                    double depthM = FallDepth(pcM, thrM, false, c);
                    if (c.JobSearchRate > 0) RouteViaPool(r, w, m, avgPerCap, mt, 1, c.MiddleFallRate * depthM * mt.Size, secular, higherEdu, gateEma, c, ref soldierVac, forced: true);
                    else Move(mt, from, c.MiddleFallRate * depthM * mt.Size, c);
                }
            }

            // ===== ⑥ 職人（プチブル）→ 労働者：工場との競争に敗れた手工業者・没落資本家は労働者へ（ラッダイト＝闘争度を携えて工場へ）=====
            // 破産（信用延命中）の職人は速やかに抜く＝資本家→職人→労働者のラダーで破産者を確実に賃労働者（健全＝黒字側）へ落とし切る。
            if (r.ArtisanPopIndex >= 0 && avgPerCap > Eps)
            {
                var artisan = r.Pops[r.ArtisanPopIndex];
                double pc = artisan.Size > Eps ? AssetPerCap(artisan) : 0;   // rank7：③④⑤と同じ物差し（f=0でビット恒等）
                double anc6 = FallAnchor(artisan, fallAvg, c);
                if (artisan.Size > Eps && pc < c.MiddleFallThreshold * anc6)
                {
                    double rate = pc < c.InsolventThreshold * anc6 ? c.BankruptcyFallRate : c.MiddleFallRate;
                    double depth6 = FallDepth(pc, c.MiddleFallThreshold * anc6, false, c);   // 深割れ（破産圏）は自然に速いまま＝depth≈1
                    // ①''点火時：破産職人もプール経路（資格のある者は専門職へ・残りは下層の最良職へ）
                    if (c.JobSearchRate > 0) RouteViaPool(r, w, m, avgPerCap, artisan, 1, rate * depth6 * artisan.Size, secular, higherEdu, gateEma, c, ref soldierVac, forced: true);
                    else Move(artisan, from, rate * depth6 * artisan.Size, c);
                }
            }

            volunteers += Math.Max(0, soldierVac0 - soldierVac);   // ★(155) この州で志願が埋めた席
        }
        if (meritP > Eps) w.Meritocracy = meritW / meritP;   // 全活性州の人口加重平均（旧：foreach内で毎州上書き＝最後の州の値だけ残るバグ）
        if (volunteers > Eps) { double sEnd = w.SoldierSize(); Military.DiluteSkill(w, sEnd - volunteers, sEnd); }   // ★(155) 志願の新兵は訓練を受けていない＝熟練を薄める
    }

    // ===== 失業プール（①''）の道具 =====

    /// <summary>階級の判定（①''）：上流(2)=閉鎖階級（貴族・聖職）＋資本家／中流(1)=専門職（技師・教師・官僚・医者）＋商人＋職人／
    /// 下流(0)=残り（農民・労働者・使用人等）＋★兵士（2026-10-02 (155)・兵士は転職の仕組みの職の1つ＝旧＝軍事力学の管轄で対象外(-1)）。</summary>
    internal static int ClassOf(Region r, int i)   // internal＝Runnerのclassprobe（階級シェア計器）が同じ物差しを読む
    {
        var p = r.Pops[i];
        if (p.Closed || i == r.InvestorPopIndex) return 2;
        if (i == r.MeritToIndex || i == r.TeacherPopIndex || i == r.BureaucratPopIndex
            || i == r.DoctorPopIndex || i == r.MerchantPopIndex || i == r.ArtisanPopIndex) return 1;
        return 0;
    }

    /// <summary>失業プールの経路（①''・JobSearchRate>0 のみ）：同階級で今より稼げる職へ資格の按分で転職し、
    /// 残りは1段下の最良職へ降格する（下流はこれ以上落ちない＝残留）。「もっともマシな職」＝1人あたり所得の高い順に
    /// 試すウォーターフォール（JobPoolTemperature>0 では所得×資格×空きの低温softmax按分＝総取りの振動を消す）。
    /// 資格＝識字×高等教育（①と同じ物差し＝世俗の水路・官僚の例外も同一）。下流職は資格不要。
    /// 同階級転職には PromotionPremium（①と同じ転職プレミアム＝不感帯）。降格は所得比較なし（職の無さ・没落よりは増し）。
    /// 農村→都市の移動は §2.4 都市化天井のヘッドルーム内のみ（住宅・インフラが無ければ都市職に入れず元の職に留まる）。
    /// 1段ずつ＝落ち切らなければ翌四半期に再び判定される。</summary>
    private static void RouteViaPool(Region r, World w, double m, double avgPerCap, Pop src, int srcClass, double flow,
                                     double secular, double higherEdu, bool gateEma, Coefficients c, ref double soldierVac, bool forced = false)
    {
        if (flow <= Eps || srcClass < 0 || src.Size <= Eps) return;
        // ★C-03（2026-09-03）：農奴は「職探し」でも都市へは出られない（移動証明・人格的隷属）。
        //   ⚠PopDecision の都市化 softmax と**同じ式・同じ梯子**を、同じ向き（農村発）にだけ掛ける。
        //   SerfGateCoversPool=0（既定）では乗算に入らない＝厳密恒等。
        if (c.SerfGateCoversPool > 0 && c.SerfMobilityGate > 0 && !src.IsUrban)
        {
            double serfFreePool = 1.0 - Privileges.SeigneurialDegree(w);
            flow *= (1.0 - c.SerfMobilityGate) + c.SerfMobilityGate * serfFreePool;
            if (flow <= Eps) return;
        }
        double srcInc = Math.Max(0, src.Earned) / src.Size;
        double remaining = Math.Min(flow, src.Size);
        double headroom = UrbanHeadroom(r, c);   // §2.4：このプール経路が消費できる都市流入の残枠（既存天井の管轄拡張＝素通り穴の閉鎖）
        remaining = FillJobs(r, w, m, avgPerCap, src, srcClass, remaining, srcInc * c.PromotionPremium, secular, higherEdu, gateEma, ref headroom, ref soldierVac, c);
        if (remaining > Eps && srcClass > 0)
            remaining = FillJobs(r, w, m, avgPerCap, src, srcClass - 1, remaining, -1.0, secular, higherEdu, gateEma, ref headroom, ref soldierVac, c);
        // 強制転落（④⑤⑥・①'）は残余を残さない＝破産者は元の階級に留まれない（滞留は失業①''の専有）。
        // 資格・空き・天井で吸収しきれない残りは1段下の最良職へ全量（旧経路と同じ「必ず落ち切る」）。
        // ★これが細ると排水が出生に負ける：聖職者爆発（182→8362）の教訓＝softmax残余が④の排水を細らせ、
        //   出生配分の凍結分母（InitialFertileShare）ループに負けた（docs/20260815参照・凍結分母は別件の裁定候補）。
        if (forced && remaining > Eps)
            for (int cls = Math.Max(0, srcClass - 1); cls >= 0 && remaining > Eps; cls--)
            {
                int best = -1; double bestInc = -1;
                for (int i = 0; i < r.Pops.Count; i++)
                {
                    var t = r.Pops[i];
                    if (ReferenceEquals(t, src) || t.Closed || t.IsSoldier || t.Size <= Eps) continue;
                    if (i == r.InvestorPopIndex || i == r.MerchantPopIndex) continue;
                    if (ClassOf(r, i) != cls) continue;
                    double inc = JobIncome(t, src);   // ★(161) 比べ方の1本（送り手が兵士なら民間の職を働き手1人あたりで・民間どうしは頭数あたり＝旧と同じ値）
                    if (inc > bestInc) { bestInc = inc; best = i; }
                }
                if (best >= 0) { Move(src, r.Pops[best], remaining, c, genderAccess: GenderAccess(w, r, best, c)); remaining = 0; }
            }
    }

    /// <summary>§2.4 都市化天井のヘッドルーム（人数）：PopDecision と同じペア物差し（最初の非閉鎖の農村pop/都市pop）・
    /// 同じ天井式（BaseUrbanCap＋工業化×UrbanPerInd・平滑状態 UrbanIndSmoothed は読み取りのみ・0.85は既存ハードストップの再利用）。
    /// CapitalEnabled=false（天井の無い世界）とペア不成立は無制限。</summary>
    private static double UrbanHeadroom(Region r, Coefficients c)
    {
        if (!r.CapitalEnabled) return double.PositiveInfinity;
        Pop? rural = null, urban = null;
        foreach (var p in r.Pops)
        {
            if (p.Closed) continue;
            if (p.IsUrban) urban ??= p; else rural ??= p;
        }
        if (rural is null || urban is null) return double.PositiveInfinity;
        double total = rural.Size + urban.Size;
        if (total <= Eps) return 0;
        double indForCap = c.UrbanCapFollowSpeed < 1.0 && r.UrbanIndSmoothed >= 0 ? r.UrbanIndSmoothed : r.TotalIndustrialization;
        double maxShare = Math.Clamp(c.BaseUrbanCap + indForCap * c.UrbanPerInd, 0, MaxUrbanShareCap);
        return Math.Max(0, maxShare * total - urban.Size);
    }

    /// <summary>クラス jobClass の職を1人あたり所得の高い順に試し、資格ゲートぶんを移す。残量を返す。
    /// JobPoolTemperature>0 では総取りでなく所得×資格×空き(1−受け皿失業率)の低温softmax按分（T→0で総取りへ収束・Σ重み正規化＝自己有界）。
    /// 農村→都市の移動は urbanHeadroom（§2.4天井の残枠）を消費し、枠が尽きたら都市職はスキップ（住宅が無ければ入れない）。
    /// 行き先の候補から閉鎖階級（貴族・聖職＝身分は買えない）・資本家（参入は資本＝③の管轄）・
    /// 商人（参入は元手＝①の判断を踏襲）・兵士（徴募の管轄）・自分自身を除く。minIncome=-1 で所得条件なし（降格）。</summary>
    private static double FillJobs(Region r, World w, double m, double avgPerCap, Pop src, int jobClass, double amount, double minIncome,
                                   double secular, double higherEdu, bool gateEma, ref double urbanHeadroom, ref double soldierVac, Coefficients c)
    {
        bool srcRural = !src.IsUrban;
        if (c.JobPoolTemperature > 0)
        {
            // 低温softmax按分（監査rank2）：史実の「失業者は空きのある職が吸収する」形。吸収されない残余は返す＝srcに滞留し翌期再試行。
            int n = r.Pops.Count;
            Span<double> share = n <= 64 ? stackalloc double[n] : new double[n];
            share.Clear();
            double incMax = double.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                var t = r.Pops[i];
                if (ReferenceEquals(t, src) || t.Closed || t.Size <= Eps) continue;
                if (t.IsSoldier && !(soldierVac > Eps)) continue;   // ★(155) 兵士の席は空きがあるときだけ候補（席の数＝定員・⑦）
                if (i == r.InvestorPopIndex || i == r.MerchantPopIndex) continue;
                if (ClassOf(r, i) != jobClass) continue;
                double inc = JobIncome(t, src);
                if (inc <= minIncome) continue;
                share[i] = 1.0; if (inc > incMax) incMax = inc;   // 1.0＝候補マーク（重みは第2パス）
            }
            if (incMax == double.NegativeInfinity) return amount;
            double sumW = 0;
            for (int i = 0; i < n; i++)
                if (share[i] > 0)
                {
                    var t = r.Pops[i];
                    double inc = JobIncome(t, src);
                    share[i] = Math.Exp(Math.Max((inc - incMax) / c.JobPoolTemperature, -40));
                    sumW += share[i];
                }
            double placed = 0;
            for (int i = 0; i < n; i++)
                if (share[i] > 0)
                {
                    var t = r.Pops[i];
                    double gate = JobGate(r, w, m, avgPerCap, src, i, jobClass, secular, higherEdu, gateEma, c);
                    double vacancy = Math.Max(0, 1.0 - t.UnemploymentRate);   // 空き＝受け皿の売れ残りが少ないほど吸収する（兵士の「失業」＝給与の未払いの割合＝払われない軍には志願しない）
                    double f = amount * (share[i] / sumW) * gate * vacancy;
                    if (srcRural && t.IsUrban) { f = Math.Min(f, urbanHeadroom); }
                    if (t.IsSoldier) f = Math.Min(f, soldierVac);   // 席の空きまで
                    if (f <= Eps) continue;
                    f = MoveToJob(w, r, i, src, t, f, c, ref soldierVac);   // rank12：着地先が専門職なら性別の選抜運搬（既定off=恒等）・兵士は徴兵の年齢の男だけ（動いた人数を返す）
                    placed += f;
                    if (srcRural && t.IsUrban) urbanHeadroom -= f;
                }
            return amount - placed;
        }
        Span<bool> tried = r.Pops.Count <= 64 ? stackalloc bool[r.Pops.Count] : new bool[r.Pops.Count];
        tried.Clear();   // stackalloc の内容は言語仕様上は未定義＝明示ゼロ化
        while (amount > Eps)
        {
            int bestIdx = -1; double bestInc = minIncome;
            for (int i = 0; i < r.Pops.Count; i++)
            {
                if (tried[i]) continue;
                var t = r.Pops[i];
                if (ReferenceEquals(t, src) || t.Closed || t.Size <= Eps) continue;   // 空の職は給与が観測できない（①と同じ）
                if (t.IsSoldier && !(soldierVac > Eps)) continue;   // ★(155) 兵士の席は空きがあるときだけ候補（⑦）
                if (i == r.InvestorPopIndex || i == r.MerchantPopIndex) continue;
                if (ClassOf(r, i) != jobClass) continue;
                double inc = JobIncome(t, src);
                if (inc > bestInc) { bestInc = inc; bestIdx = i; }
            }
            if (bestIdx < 0) break;
            tried[bestIdx] = true;
            var dst = r.Pops[bestIdx];
            double gate = JobGate(r, w, m, avgPerCap, src, bestIdx, jobClass, secular, higherEdu, gateEma, c);
            if (gate > Eps)
            {
                double f = amount * gate;
                if (dst.IsSoldier) f = Math.Min(f, soldierVac);   // ★(155) 席の空きまで
                if (srcRural && dst.IsUrban)
                {
                    f = Math.Min(f, urbanHeadroom);   // §2.4：都市化天井の残枠まで（枠切れ＝この都市職には入れない）
                    if (f <= Eps) continue;
                    urbanHeadroom -= f;
                }
                if (f <= Eps) continue;
                f = MoveToJob(w, r, bestIdx, src, dst, f, c, ref soldierVac);   // rank12：着地先が専門職なら性別の選抜運搬（既定off=恒等）・兵士は徴兵の年齢の男だけ
                amount -= f;
            }
        }
        return amount;
    }

    /// <summary>★(155) 職の比較の所得（①''・失業プールの着地）＝1人あたりの所得。兵士の席は兵1人の給与（兵士 POP は働き盛りの男だけ＝頭数＝働き手）を
    /// 送り手の世帯の1人あたりへ直す（×送り手の働き盛りの人数÷頭数＝<see cref="Military.WorkingAdults"/>）＝分子と分母を同じ規則で（民間の職は世帯の頭数あたり・
    /// 兵の給与は働き手1人あたり＝そのまま比べると兵士が約2倍に見える）。
    /// ★(161)（(157) の反証役の指摘 va#5＝片向き）：**送り手が兵士のとき**（平時の復員・未払いの脱走）も同じ規則＝民間の職の所得を働き手1人あたり（世帯の所得÷働き盛り
    /// ＝その職で働き手1人が得る額）にしてから送り手の世帯の1人あたりへ直す（兵士 POP は働き盛り÷頭数≈1）。旧＝民間の職は頭数あたりのまま＝兵1人の給与（働き手1人あたり）が
    /// 約2倍に見え、第1段（今より稼げる職×転職のプレミアム）がほぼ通らず強制の残り（最良の1つへ総取り）へ落ちた。民間どうしの比較は頭数あたりのまま（変えていない）。</summary>
    private static double JobIncome(Pop t, Pop src)
    {
        double inc = Math.Max(0, t.Earned) / t.Size;
        if (t.IsSoldier == src.IsSoldier) return inc;   // 民間どうし（兵士どうしは自分自身＝候補にならない）＝世帯の頭数あたりのまま
        double wa = t.IsSoldier ? t.Size : Military.WorkingAdults(t);   // 働き手の人数（兵士 POP は働き盛りの男だけ＝頭数）
        double perWorker = wa > Eps ? Math.Max(0, t.Earned) / wa : 0;
        double perHead = src.Size > Eps ? Military.WorkingAdults(src) / src.Size : 0;
        return perWorker * perHead;
    }

    /// <summary>★(155) 職への移動の1本（①''の着地）＝民間の職は <see cref="Move"/>（世帯ごと）・兵士の席は徴兵と同じ人の移し方（<see cref="Military.MoveCohort"/>＝徴兵の年齢の男だけが抜ける・
    /// 動けるのはその年齢の男の数まで）。返り値＝実際に動いた人数（席の空きから引く）。</summary>
    private static double MoveToJob(World w, Region r, int idx, Pop src, Pop dst, double f, Coefficients c, ref double soldierVac)
    {
        if (!dst.IsSoldier) { Move(src, dst, f, c, genderAccess: GenderAccess(w, r, idx, c)); return f; }
        double before = dst.Size;
        Military.MoveCohort(src, dst, f, c, Military.DraftAgeHi(w));
        double moved = Math.Max(0, dst.Size - before);
        soldierVac = Math.Max(0, soldierVac - moved);
        return moved;
    }

    /// <summary>職の資格（①''）：中流の専門職＝識字×高等教育×世俗の水路（①と同じ。官僚は世俗免除・資格は余剰教育のみ）。
    /// 職人＝小営業（教育資格なし。rank6点火時は元手ゲート＝1人あたり貯蓄≥地域平均＝「無一文の失業者が工房主」の閉鎖）。
    /// 下流・上流の職＝資格なし（上流はそもそも候補が無い）。
    /// rank17：専門職にはガラスの天井 minorityF（①と同じ物差し・時代ブレンド込み）＝プール点火時の天井迂回の閉鎖。</summary>
    private static double JobGate(Region r, World w, double m, double avgPerCap, Pop src, int target, int jobClass,
                                  double secular, double higherEdu, bool gateEma, Coefficients c)
    {
        if (jobClass != 1) return 1.0;
        if (target == r.ArtisanPopIndex)
            return c.PettyEntryRate > 0 && avgPerCap > Eps && src.Size > Eps && src.Savings / src.Size < avgPerCap ? 0.0 : 1.0;
        double minorityF = w.EthnicEnabled
            ? Math.Max(0, 1.0 - c.MinorityPromotionPenalty * (1.0 - c.MinorityPenaltyModernBlend * m) * (1.0 - src.CoreEthnicShare))
            : 1.0;
        if (target == r.BureaucratPopIndex) return PoliticalQuantities.Clamp01(src.Literacy * higherEdu * minorityF);
        double gateEdu = higherEdu;
        if (c.PromotionGateOwnSchooling > 0)
            gateEdu = Math.Max(higherEdu, c.PromotionGateOwnSchooling * (gateEma ? src.EducationEma : src.EducationFill));
        return PoliticalQuantities.Clamp01(src.Literacy * gateEdu * secular * minorityF);
    }

    // ===== リアリティ監査 rank7(点火0.3)/rank8(休眠)/rank12(点火1.0)（作者裁定2026-08-16）の道具 =====

    /// <summary>rank7(a)：③④⑤⑥の判定資産（1人あたり）＝Savings/Size。
    /// ⚠旧の f&gt;0 の枝は (Savings+IlliquidWealth)/Size だったが、結晶資産は休眠の枝だけが書き常に0だった＝2026-09-26 に欄ごと削除（dorm-24）。</summary>
    private static double AssetPerCap(Pop p)
        => p.Savings / Math.Max(Eps, p.Size);

    /// <summary>rank7(b)（点火 FallAnchorReform=0.3）：③④⑤⑥の判定アンカー。f=0＝地域平均資産（avgAsset＝従来avgPerCap）＝
    /// 「他人の平均」相対で、富の裾が膨れるほど健全な中流を機械的に転落させる因果の逆転を持つ。f=1＝**自分の平滑所得×1年**
    /// （debtRuined判定の p.Earned×MonthsPerYear と同じ言語・平滑=max(当月,前月)＝結晶化アンカー（EconomyStep）と同じ取り方）。
    /// ⚠f&gt;0では閾値係数（UpperFallThreshold=0.8等）の単位が「平均の何割」→「年所得の何倍」へ切替わる（係数コメントに明記済み）。</summary>
    private static double FallAnchor(Pop p, double avgAsset, Coefficients c)
    {
        double f = c.FallAnchorReform;
        if (f <= 0) return avgAsset;
        double annualPerCap = Math.Max(0, Math.Max(p.Earned, p.LastEarned)) * Calendar.MonthsPerYear / Math.Max(Eps, p.Size);
        return (1.0 - f) * avgAsset + f * annualPerCap;
    }

    /// <summary>rank7(c)（点火 FallAnchorReform=0.3）：転落率の深さ係数＝rate×((1−f)+f×clamp((閾値−資産)/閾値,0,1))。
    /// f=0（またはショック＝debtRuined）で1.0＝旧二値率とビット恒等。f&gt;0で浅い割れはゆっくり・深い割れ（恐慌）は速い
    /// ＝閾値張り付きの一方向融解（半減期8.6年＝Clarkのエリート持続の7-10倍速）が消え、恐慌の大量転落は保たれる。</summary>
    private static double FallDepth(double pc, double threshold, bool shock, Coefficients c)
    {
        double f = c.FallAnchorReform;
        if (f <= 0 || shock) return 1.0;
        double depth = threshold > Eps ? PoliticalQuantities.Clamp01((threshold - pc) / threshold) : 1.0;
        return (1.0 - f) + f * depth;
    }

    /// <summary>rank8（休眠 CohortGateK）：世代ゲート＝大流量の非ショック移動（①昇格・①*小営業参入・①'手工業没落・①''失業プール）の律速。
    /// 物差しは人口動態の加齢（年齢の層・段4e 後半＝10〜19 の層から 20〜29 へ出ていく流れ＝<see cref="Pop.EnteringAdultsMonthly"/>）と同一＝四半期の新成人率
    /// ＝月の流れ×3＝**新しい量を作らない**（旧＝子供比×3÷(AgingYears×12)）。gate=min(1, K×新成人率)＝移動の大半は入職時（Long&amp;Ferrie）で
    /// 均衡は不変・時定数だけ世代スケールへ。④⑤⑥の転落・戦争・破産（BankruptcyFallRate経路）は素通し＝「破産は速い」が史実。
    /// 呼び出し側が K&gt;0 のときだけ乗算する（K=0＝1命令も走らない厳密恒等）。新成人率≈0.5%/四半期なのでK≈数十〜百が実効目盛り。</summary>
    private static double CohortGate(Pop src, Coefficients c)
        => Math.Min(1.0, c.CohortGateK * src.EnteringAdultsMonthly() * Calendar.MonthsPerQuarter);

    /// <summary>rank12（点火 ProfessionGenderGate=1.0）：行き先職の女性アクセス（1.0＝制約なし）。**新しい法は作らない**（作者裁定）＝
    /// 既存の「女性の権利」梯子（なし0／財産権1/3／教育権2/3／選挙権1＝FactionSnapshotと同じ物差し）×WomenWageRetention
    /// （財産権の社会的確立の連続量＝法直後の段差を平滑化）から導出。制約は専門職（技師/官僚/医者）のみ＝教師/使用人/その他は1.0。
    /// 女性教育権の通過で教職の女性化が創発し、技師・官僚は世紀を通じ男性のまま＝両方向が同じ1本の法から出る。
    /// gate=0で常に1.0（Moveの選抜ブロックが走らない）＝従来の按分混合とビット恒等。</summary>
    private static double GenderAccess(World w, Region r, int targetIdx, Coefficients c)
    {
        double g = c.ProfessionGenderGate;
        if (g <= 0) return 1.0;
        if (targetIdx != r.MeritToIndex && targetIdx != r.BureaucratPopIndex && targetIdx != r.DoctorPopIndex) return 1.0;
        double access = (int)w.WomenRights / (double)(int)WomenRightsLaw.Suffrage * w.WomenWageRetention;   // 選挙権=1.0／教育権=中（2/3）／財産権以下=低（≤1/3）
        return 1.0 - g * (1.0 - access);
    }

    /// <summary>階級間の移動（規模＋貯蓄を比例移動＝保存）＝人の移し方の1本（<see cref="Pop.Blend"/>・2026-09-30 処理の一元化 段4e I）。
    /// 移動者は自分の闘争度・識字・政治意識・国民意識・思想・年齢と男女の構成を持ち込む（行き先に加重混合）。
    /// 没落の不満は固定上乗せでなく、転落先の低所得→満足度↓→want↑→闘争度（Politics）が自然に拾う（倍率排除＝二重計上解消）。
    /// 人1：selectLiteracy=true の昇格は識字上位を選抜し src の識字を引き下げる（PromotionLiteracySelection ゲート・昇格ポンプの自己補充＝二重計上を解消）。
    ///   選抜コホートの識字＝src平均＋選抜強度×(1−src平均)（上位ほど識字が高い方向）。残留は質量保存で下がる（Blend の選抜の枝）。
    /// rank12：genderAccess&lt;1（点火 ProfessionGenderGate=1.0 の専門職行き）では移動コホートの FemaleShare=src×access に選抜し、
    /// 残留側で質量保存（識字選抜と同型・男が出て女は残る＝srcの女性比が上がる）。
    /// ⚠旧＝ここの写しは女性比を子供込みの規模で重み付けた（成人のうちの女性比＝Blend は成人数で重み付ける）。旧の旗 MigrationCarriesHumanCapital（識字の運搬と選抜の門）は焼き込み。</summary>
    private static void Move(Pop fromPop, Pop toPop, double flow, Coefficients c, bool selectLiteracy = false, double genderAccess = 1.0)
    {
        if (flow <= Eps || fromPop.Size <= Eps || ReferenceEquals(fromPop, toPop)) return;
        flow = Math.Min(flow, fromPop.Size);
        bool select = fromPop.Size - flow > Eps;   // 選抜は残る人がいるときだけ（質量保存の分母）
        double moveLit = selectLiteracy && c.PromotionLiteracySelection > 0 && select
            ? PoliticalQuantities.Clamp01(fromPop.Literacy + c.PromotionLiteracySelection * (1.0 - fromPop.Literacy)) : double.NaN;
        double femAccess = genderAccess < 1.0 && select ? genderAccess : double.NaN;   // 性別の門（移る組の女性比＝送り手の女性比×これ・Blend の中）
        Pop.Blend(fromPop, toPop, flow, femaleAccess: femAccess, literacy: moveLit);
    }

    // 政体の近代性：君主の身分制＝低、自由（立憲君主・共和）・独裁（能力主義的動員）＝開かれた社会＝高
    private static double PolityModernity(Polity p) => p switch
    {
        Polity.ConstitutionalMonarchy => 0.5,
        Polity.Republic => 0.6,
        Polity.ProletarianDictatorship => 0.7,
        Polity.BourgeoisDictatorship => 0.5,
        Polity.Despotic => 0.3,
        Polity.MilitaryDictatorship => 0.35,     // 軍事独裁＝上からの近代化（専制よりやや組織的）
        _ => 0.1,                                // 絶対君主制・幕府・無政府
    };
}
