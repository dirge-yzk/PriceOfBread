namespace PriceOfBread.SimCore;

/// <summary>
/// 政府＝政策の適用（四半期）。**利益集団の選好（<see cref="PolicyPreferences"/>）が政治を動かす**：
/// スライダーは変更方向に反対するブロックの力で律速され、法整備は力の均衡が許す向きに一歩ずつ進む。
/// ＝「力ある団体が嫌う法は通らない」。参政権はブルジョワが制限選挙で梯子を外す（近代エリートの選好が反転）。
/// **政治資本の在庫**（2026-09-21 裁定S1・㉖で既定化）：純支持が負の一歩は、残り a＝max(0, b−S) が代価に届くときだけ押し切れる
/// （<see cref="PoliticalCapitalAffords"/>）。押し切った分だけ目減り S（<see cref="World.MandateSpent"/>）が積もり正統性も燃える（Politics が減算）＝自己制限的。
/// 憲法級（参政権等）だけは硬い「純支持＞0」（在庫では押せない）。旧＝余白 θ=MandateWeight×(正統性−0.5)（2026-07-09〜09-21）。
/// 法整備はもう即時可決しない＝AI/プレイヤーの意図は政治のゲートを通る（NullAgent は目標＝現状で不動＝後方互換）。
/// </summary>
public static class Government
{
    internal const double SliderStep = 0.05;          // スライダーの5%刻み（UI/AI共通の20段量子化）。⚠internal（2026-09-20）＝専売の広さ（World.StateMonopolyBreadth）も同じ格子に乗る（番人が丸め幅を名指しで書ける）
    /// <summary>税・支出の法の最上段（作者裁定2026-09-12 R1「なし/低/中/高の4段階のボタン」）＝序数0..3。値は 段×<see cref="TaxLawTop"/>/3。
    /// internal＝要求への応答（Parliament.TargetDemand）・勢力の譲歩（Politics.ApplyDemand）・AIの目標整形（SelfPlay.GateAllLawTargets）が同じ段数で置く。</summary>
    internal const int TaxLawStages = 3;
    // ==== 段の錨（2026-09-18）＝最上段が意味する率 ====
    // なぜ要るか：梯子を [0,1] の3等分に打つと最下段の非ゼロが所得の33%になり、**エンジン自身の教義値（0.15〜0.35）が全て段0（＝無税）へ丸まる**。
    // 作者制約「税の挙動は今までと同じ」（R1）が破れ、議会掌握（PowerShares.ParliamentGrip の分子＝所得税＋累進）が構造的に0になる。
    // 軍事費の段が正しく見えていたのは「高＝教義最大」だからではなく **エンジンの最頻値0.25がちょうど段の1つに乗っていた**から＝
    // 一般化すべき性質は「エンジンの値が集まっている所に段を打つ」こと。税の軸では教義最大を最上段に取るとそれが成り立つ。
    // ★新しい数は1つも作らない＝下の6本は**既存のリテラルを移したもの**で、元の書き手（SelfPlay の教義・NationConfig の開始値）が
    //   今度はここを参照する＝1軸1個へ減る（「梯子の最上段＝その軸の教義最大」が構文で保証される）。
    // ⚠史実（1783-1934）：所得の33%を経常で取った国は無い（英1799年ピット10%／1842年ピール約3%／1918年標準税率30%・プロイセン分類所得税5%未満・米1913年1〜7%）。
    //   新しい4段 0／11.7／23.3／35% は全て史実の帯の内側で、最上段35%が1918年の英標準税率30%を上回る＝「高率」の名に合う。
    internal const double IncomeTaxTop = 0.35;          // 緊縮教義の所得税（SelfPlay.Apply の case Stance.Fiscal）＝この軸でエンジンが置く最大値
    internal const double ProgressiveTaxTop = 0.8;      // ソ連風税制の累進（SelfPlay.Apply の SovietTaxProfile ブロック）
    internal const double PollTaxTop = 0.5;             // 人頭税の最上段（旧＝全61カ国の開始値・2026-09-29 (109) から開始値は国ごとの段＝NationConfig.PollTaxStage）
    // ★専売の教義最大（旧 StateMonopolyTop=0.8）は物理削除（作者裁定2026-09-20）＝専売に率の軸は無い。ソ連風税制は**対象を全ての消費財へ広げる**（World.StateMonopolyStepOf の最上段）で表す。
    internal const double LandTaxTop = 0.4;             // 近代化/福祉/緊縮/軍国の4教義が共有する土地税（SelfPlay.Apply）
    internal const double EducationSpendingTop = 1.0;   // 近代化教義の教育費（SelfPlay.Apply の case Stance.Modernize・Agents.ModernizerAgent）＝旧の錨と同値＝教育費の段は1ビットも動かない
    /// <summary>その軸の最上段が意味する率。段を持たない軸は 1.0＝旧の錨（呼ばれない）。</summary>
    internal static double TaxLawTop(PolicyAxis a) => a switch
    {
        PolicyAxis.IncomeTax => IncomeTaxTop,
        PolicyAxis.ProgressiveTax => ProgressiveTaxTop,
        PolicyAxis.PollTax => PollTaxTop,
        PolicyAxis.LandTax => LandTaxTop,
        _ => EducationSpendingTop,   // 教育費＝1.0（段を持たない軸も同値＝旧の [0,1] 3等分）
    };
    /// <summary>軍事費の法の最上段（作者裁定2026-09-12 R2「低/中/高/戦時の4段」）＝序数1..4。値は 段/4（0.25／0.5／0.75／1.0）。
    /// ⚠「なし」が無いので分母は4（税の3ではない）＝最下段でも軍事費は0にならない。最上段＝戦時経済＝総動員とセット。</summary>
    internal const int MilitaryLawStages = 4;
    internal const int SocialSecurityMaxLevel = 3;    // 社会保障法の最上段（Pension）＝4段（PoorLaw..Pension）の最大序数。internal（2026-09-11）＝Parliament.TargetDemand が法の段で目標を置く。2026-07-21「なし」廃止で救貧法が最下段（序数0＝福祉水準0）

    /// <summary>R1 蜂起R群2026-08-16（診断A1・本丸）：軍事力→治安維持力目標。CoercionSoftCap=0＝旧 min(x, CoercionMax)＝**ビット恒等**／
    /// >0＝漸近逓減 x/(1+x/CoercionMax)（SelfPlay.Score.Military と同じ既存の逓減形・x→∞でCoercionMaxへ漸近）。
    /// 壁の何が悪いか：旧 CoercionMax=4000 は挑戦力の理論最大（BaseCoercion換算 ≈ PoliticalReferencePop×1.25 ≈ 1250）の3.2倍＝
    /// 軍事費スライダー≳0.28でcapに張り付き「不満100%・意識100%でも革命が代数的に不能」（クーデタも同じ動員ゲート従属）。
    /// ⚠これは**形の切替であり値の縮尺でない**：min未満の帯でも x/(1+x/cap)&lt;x＝平時のBaseCoercion（Politics・治安維持力維持費・
    /// FactionSnapshot表示の読み手）も下がる。旧と厳密恒等にするのはフラグ0の旧式のみ。
    /// ★2026-08-16の点火梯子で**CoercionSoftCap=1.0 に点火済み（live）**＝水準 CoercionMax も同時に 4000→2000（等号点1250の1.6倍）へ是正
    /// （漸近値が等号点1250へ近づくほど「大軍でも正統性×財政が痩せれば倒れる」帯が開く）。見る帯＝総政変・政体exact。
    /// internal＝ON経路テスト用（InternalsVisibleTo）。</summary>
    internal static double CoercionTargetFromForce(double force, Coefficients c)
        => c.CoercionSoftCap > 0
            ? force / (1.0 + force / Math.Max(1e-9, c.CoercionMax))
            : Math.Min(force, c.CoercionMax);

    /// <summary>★段の法（enum の法と bool の法）の軸の1行（2026-09-28 処理の一元化 段1）。値は段の序数（bool は 0/1）。
    /// 施行（<see cref="Step"/>）・戦線の数え（<see cref="CountOpenFronts"/>）・段（<see cref="StagedAxis"/>／<see cref="CurrentStage"/>）・
    /// 位置（<see cref="FactionSnapshot.AxisPosition"/>）が**同じ1行**を読む＝軸を足すときに書く所が1行になる（旧＝軸ごとに4か所を手で対応させていた）。
    /// <paramref name="Target"/>＝施行と戦線の目標／<paramref name="Top"/>＝最上段（位置＝min(段,Top)/Top）／
    /// <paramref name="Stage"/>＝2：段飛ばし禁止（StagedAxis）で現在段を持つ・1：現在段だけ（NextStage が読む）・0：段なし／
    /// <paramref name="Open"/>＝施行と戦線の両方の門（null＝常に）／<paramref name="Enact"/>＝施行の特例（null＝<see cref="StepLevel"/> で目標へ1段・第3引数は Target の値）／
    /// <paramref name="FrontOpen"/>＝戦線の数えだけの門（null＝Open）。
    /// ★2026-09-28（処理の一元化 段2 束2）：施行と戦線がずれていた2本（通貨＝戦線だけの門 FrontsCountMonetary の既定0・『全て』国有化＝施行は政体で丸めた目標）を揃えた
    /// ＝どの行も施行と戦線が同じ目標と同じ門を読む（戦線だけの門は国教のプロレタリア独裁＝政体が法を無効化する1本だけ）。</summary>
    internal sealed record LevelLaw(PolicyAxis Axis, Func<World, int> Get, Action<World, int> Set, Func<World, NationPolicy, int> Target, int Top, int Stage = 0,
        Func<World, bool>? Open = null, Action<World, NationPolicy, int>? Enact = null, Func<World, bool>? FrontOpen = null);

    /// <summary>段の法の表＝**施行の順**（純支持が負の一歩は政治資本の在庫を先着で使う＝並びは挙動）。</summary>
    internal static readonly LevelLaw[] LevelLaws =
    [
        new(PolicyAxis.EducationLaw, w => (int)w.EducationLaw, (w, v) => w.EducationLaw = (EducationLaw)v, (w, p) => (int)p.EducationLaw, 3, Stage: 2),
        // 女性参政権（男女普通選挙 Universal）の前提＝女性の権利の梯子が最上段（選挙権）まで登っていること。
        // 第3段3-6（2026-07-27）で財産権前提の到達不能枝を削除・2026-08-10 で梯子へ統合＝分離（財産権1870/82と参政権1918）は段の並びが恒久的に保証する。
        // ★裁定2026-09-12（FIX3-1）：ここで目標を AdultMale へ書き換えていた行を削除＝**天井型の前提は登る側にしか掛けない**（同日の反証と同文・3例目）。
        //   登る側は StepLevel が LawPrereq（case Suffrage＝「女性の権利（選挙権）が前提」）を照会して既に塞いでいる＝完全な重複だった。
        //   固有の産物は降りる側だけ＝{現行=Universal かつ 目標=Universal かつ !WomenSuffrage}（女性の権利が断裂の窓/内戦譲歩で剥がれた国）で
        //   誰も選んでいない男女普通選挙→男性普通選挙の降格目標が黙って立ち、以後 w≠p が恒久的な幽霊戦線になっていた。
        new(PolicyAxis.Suffrage, w => (int)w.Suffrage, (w, v) => w.Suffrage = (Suffrage)v, (w, p) => (int)p.Suffrage, 3, Stage: 2),
        // ★女性の権利（統合法・2026-08-10）：旧・3本の StepBool（財産権/参政権/教育権）を**1本の段の法**へ（家父長制の難改革＝旧支配層−2）。
        //   なし→財産権→教育権→選挙権を1期1段ずつ登る＝史実の順序（英1870/82→女子教育→1918）を前提条件を書かずに段の並びで保証する。
        //   旧・財産権/参政権/教育権の3戦線は1戦線に畳まれる（政治予算 (1/N)^Gate の N が減る＝他軸の押しが強まる）。位置＝なし0／財産権1/3／教育権2/3／選挙権1.0。
        new(PolicyAxis.WomenRights, w => (int)w.WomenRights, (w, v) => w.WomenRights = (WomenRightsLaw)v, (w, p) => (int)p.WomenRights, 3, Stage: 2),
        // ★親軍法の施行行は2026-08-18に撤去（作者裁定「厳密には法ではなく政治機構だ」）。軍国主義は単一の法でも単一のスカラーでもなく
        //   **4つの別々の実体**へ分解された（PoliticalQuantities の①練度／②政治的優位／③好戦世論／④任免権＝いずれも既存量からの導出）。
        //   よってここで押す3本目の戦線は存在しない（CountOpenFronts の N も1本減る＝他軸の押しが厚くなる）。
        // ⚠**法で表現してはいけない**ことは史実が独立に示す＝日本の軍部大臣現役武官制は1913-06-13に**緩和されたが、その後23年間で一度も行使されなかった**
        //   （予備役・退役の陸軍大臣は一人も任命されていない）。なのに海軍は1914年4月に建艦予算のためだけに内閣の成立を潰した
        //   ＝**拒否権は制度的であって法定ではなく、法を緩めても何も変わらなかった**。
        // ★通貨制度＝債務者と債権者の政治。3値化（銀本位doc G4・2026-08-13）：不換→銀→金を1期1段。位置＝不換0／銀0.5／金1.0。
        // ⚠SilverStandardGate=0（旧既定・点火1.0=2026-08-21銀本位ラウンド）では中段へ入らず、旧 StepBool と同じ 0/1 の一歩＝旧挙動と厳密恒等（数値中立）。
        // ★B16（実バグ21件・2026-09-07）：通貨の梯子は政治ゲートを通るのに戦線に数えられていなかった＝到達不能な通貨目標を持つAIの
        //   開きっぱなしの戦線が診断にも現れない（当時の徴候＝毎四半期 BlockedGrievance が発火し続けること。その発火は裁定C2で止まり 2026-09-20 に削除したが、
        //   幽霊戦線そのもの＝PoliticalPushBudget を毎期薄める害は残るので、この数え上げは無関係に要る）。
        //   ★2026-09-28（処理の一元化 段2 束2）：戦線だけの門（FrontsCountMonetary・既定0）を削除＝施行と同じ目標の写像（SilverStandardGate の有無）で1戦線に数える。
        new(PolicyAxis.MonetaryStandard, w => (int)w.MonetaryLaw, (w, v) => w.MonetaryLaw = (MonetaryStandardLaw)v,
            (w, p) => w.Coef.SilverStandardGate > 0 ? Math.Clamp(p.MonetaryTarget, 0, 2) : (p.MonetaryTarget >= 2 ? (int)MonetaryStandardLaw.Gold : (int)MonetaryStandardLaw.Fiat), 2,
            Enact: (w, p, t) => w.MonetaryLaw = w.Coef.SilverStandardGate > 0
                ? (MonetaryStandardLaw)StepLevel((int)w.MonetaryLaw, t, w, PolicyAxis.MonetaryStandard)
                : StepLevel(w.MonetaryLaw == MonetaryStandardLaw.Gold ? 1 : 0, p.MonetaryTarget >= 2 ? 1 : 0, w, PolicyAxis.MonetaryStandard) == 1 ? MonetaryStandardLaw.Gold : MonetaryStandardLaw.Fiat),
        // ★資本移動＝海外へ投資する資本と国内に雇用を求める側の政治（トリレンマdoc・2026-08-13）。位置＝自由0／規制1.0。
        new(PolicyAxis.CapitalMobility, w => w.CapitalControlLaw ? 1 : 0, (w, v) => w.CapitalControlLaw = v == 1, (w, p) => p.CapitalControlTarget ? 1 : 0, 1),
        new(PolicyAxis.Censorship, w => (int)w.Censorship, (w, v) => w.Censorship = (CensorshipLaw)v, (w, p) => (int)p.Censorship, 2, Stage: 2),   // 報道の自由化（旧支配層が抵抗）
        new(PolicyAxis.LaborUnion, w => (int)w.LaborUnion, (w, v) => w.LaborUnion = (LaborUnionLaw)v, (w, p) => (int)p.LaborUnion, 2, Stage: 2),   // 労働組合の容認（資本・旧支配層が抵抗）
        // 労働時間（A-1#2修正2026-06-24）：短縮＝enum減（Unlimited→Twelve→Ten→Eight）。資本（近代エリート）が抵抗するので政治ロック越しに1段ずつ＝社会党勝利でも即8時間にはならない（旧：Policyを生で施行＝抵抗ゼロのバグ）。
        //   位置も軸の「進める向き」＝enum増＝長時間化（PolicyPreferences と同規約）。
        new(PolicyAxis.ShorterHours, w => (int)w.WorkHours, (w, v) => w.WorkHours = (WorkHours)v, (w, p) => (int)p.WorkHours, 3, Stage: 2),
        // 児童労働法（02断線走査2026-07-28）：禁止＝資本の抵抗を通す（英1833工場法）。軸の増＝合法化。
        new(PolicyAxis.ChildLabor, w => w.ChildLaborAllowed ? 1 : 0, (w, v) => w.ChildLaborAllowed = v == 1, (w, p) => p.ChildLaborAllowed ? 1 : 0, 1),
        // 議会の法案（段5・第4増分）：力の均衡（＋議席）が許せば一歩。穀物法は地主が保護を望む（撤廃は自由/大衆）。土地改革・農奴解放は地主が強く抵抗。
        new(PolicyAxis.CornLaws, w => (int)w.CornLaws, (w, v) => w.CornLaws = (ReformLevel)v, (w, p) => (int)p.CornLaws, 2, Stage: 2),   // なし→限定的→全面 を1段ずつ（1期での総反転を防ぐ）
        // 土地改革は検地台帳（LandTaxUnlocked＝土地税と同じ前提）が要る＝測量・登記なしに土地は再分配できない（史実：地租改正1873）。
        // 土地税とは課税ベースでも自動連動（改革で地代が縮む→LandRent↓→土地税の税収↓＝両者は税源を取り合う）。
        new(PolicyAxis.LandReform, w => (int)w.LandReform, (w, v) => w.LandReform = (ReformLevel)v, (w, p) => (int)p.LandReform, 2, Stage: 2, Open: w => w.LandTaxUnlocked),
        new(PolicyAxis.SerfEmancipation, w => (int)w.SerfEmancipation, (w, v) => w.SerfEmancipation = (ReformLevel)v, (w, p) => (int)p.SerfEmancipation, 2, Stage: 2),   // なし→限定的→全面。農奴解放は布告＝検地台帳不要（露1861）
        // 社会保障法（1934拡張E5）：4段の法が福祉水準と年金を駆動（旧スライダー廃止・年金を第4段に吸収）。
        // 第3段3-6（2026-07-27）：SocialSecurityLawGate=1.0（E9点火）を焼き込み＝旧・別年金法else枝（到達不能）を削除。
        // AIの福祉意図(SocialSecurityTarget 0..1)を法の段階へ写像＝別AI規則を書かずスライダー廃止と等価（AI/議会は従来どおり目標を出す）。
        // 救貧0/労災0.33/失業0.67/年金1.0＝段階が福祉水準を駆動（救貧法=福祉水準0で旧「なし」相当・較正中立）。
        // ⚠緊縮は福祉水準を削らない（裁定B・2026-09-12・旧の Min クランプは 2026-09-20 の段の点火で削除）：法の段がそのまま福祉水準＝
        //   緊縮は LawPrereq の「緊縮の上限」で**登れなくする**だけで既に制定された法は剥がさない。位置は福祉水準 w.SocialSecurity（AxisPosition）。
        new(PolicyAxis.SocialSecurity, w => (int)w.SocialSecurityLaw, (w, v) => w.SocialSecurityLaw = (SocialSecurityLaw)v,
            (w, p) => Math.Clamp((int)Math.Round(Q5(p.SocialSecurityTarget) * SocialSecurityMaxLevel), 0, SocialSecurityMaxLevel), SocialSecurityMaxLevel, Stage: 2,
            Enact: (w, p, t) =>
            {
                p.SocialSecurityLaw = (SocialSecurityLaw)t;
                w.SocialSecurityLaw = (SocialSecurityLaw)StepLevel((int)w.SocialSecurityLaw, t, w, PolicyAxis.SocialSecurity);
                w.SocialSecurity = (int)w.SocialSecurityLaw / (double)SocialSecurityMaxLevel;
                w.Pension = w.SocialSecurityLaw == SocialSecurityLaw.Pension ? ReformLevel.Full : ReformLevel.None;   // 年金を第4段に吸収（別年金法は廃止）
            }),
        new(PolicyAxis.ConscriptionLaw, w => (int)w.ConscriptionLaw, (w, v) => w.ConscriptionLaw = (ConscriptionLaw)v, (w, p) => (int)p.ConscriptionLaw, 2, Stage: 2),   // 徴兵法（E5）：志願兵→徴兵制（旧支配層/軍が推進・大衆が抵抗）
        new(PolicyAxis.SecretBallot, w => (int)w.SecretBallot, (w, v) => w.SecretBallot = (SecretBallotLaw)v, (w, p) => (int)p.SecretBallot, 1),   // 秘密投票法（E5）：公開→秘密（旧支配層が抵抗・近代/大衆が推進）
        new(PolicyAxis.Bureaucracy, w => (int)w.Bureaucracy, (w, v) => w.Bureaucracy = (BureaucracyLaw)v, (w, p) => (int)p.Bureaucracy, 2, Stage: 2),   // 官僚制度（E5）：猟官→資格任用→専門（旧支配層が抵抗・近代/大衆が推進）
        new(PolicyAxis.ImmigrationRestriction, w => (int)w.ImmigrationRestriction, (w, v) => w.ImmigrationRestriction = (ImmigrationLaw)v, (w, p) => (int)p.ImmigrationRestriction, 2, Stage: 2),   // ★移民制限法（2026-08-01）：開放→選別→閉鎖
        new(PolicyAxis.SpecialistExitBan, w => (int)w.SpecialistExitLaw, (w, v) => w.SpecialistExitLaw = (SpecialistExitLaw)v, (w, p) => (int)p.SpecialistExitLaw, 2, Stage: 2),   // ★技術者流出禁止法（段⑤・2026-08-17）：自由→制限→禁止（いきなり全面禁止にはできない）
        // 国教法（E6）：国教のみ→内心の自由→信仰の自由（教会が抵抗）。位置の上限は信仰の自由（国家無神論は政体の属性＝1.0扱い・2026-09 wave2b）。
        // 例外①（2026-07-28）：プロレタリア独裁＝国家無神論を毎期強制（§13-7＝法が戻るのではなく政体が無効化する。ソ連1918政教分離令）。
        // 政体を離れると現在値StateAtheismから通常の梯子（Government）で目標へ緩やかに復帰＝教会の再建は漸進（史実：ロシア正教の復権）。
        new(PolicyAxis.StateReligion, w => (int)w.StateReligion, (w, v) => w.StateReligion = (StateReligionLaw)v, (w, p) => (int)p.StateReligion, 2, Stage: 2,
            Enact: (w, p, t) => w.StateReligion = w.Polity == Polity.ProletarianDictatorship
                ? StateReligionLaw.StateAtheism
                : (StateReligionLaw)StepLevel((int)w.StateReligion, t, w, PolicyAxis.StateReligion),
            FrontOpen: w => w.Polity != Polity.ProletarianDictatorship),
        new(PolicyAxis.Antitrust, w => (int)w.Antitrust, (w, v) => w.Antitrust = (AntitrustLaw)v, (w, p) => (int)p.Antitrust, 2, Stage: 2),   // 独占禁止法（E6）：規制なし→談合禁止→独占の解体（独占レント保有層が抵抗）
        new(PolicyAxis.Guild, w => (int)w.Guild, (w, v) => w.Guild = (GuildLaw)v, (w, p) => (int)p.Guild, 2, Stage: 2, Open: w => w.Coef.GuildLawGate > 0),   // ★同業組合法（特権 段4・2026-09-07）：保護→制限→廃止（職人が抵抗）。ゲート0＝法が動かない＝恒等
        new(PolicyAxis.WartimeRationing, w => w.WartimeRationing ? 1 : 0, (w, v) => w.WartimeRationing = v == 1, (w, p) => p.WartimeRationing ? 1 : 0, 1),   // 戦時配給法（E6・あり/なし）：戦時のみ発火（資本が反対）
        new(PolicyAxis.MilitaryExport, w => w.MilitaryExportBan ? 1 : 0, (w, v) => w.MilitaryExportBan = v == 1, (w, p) => p.MilitaryExportBan ? 1 : 0, 1),   // 軍需輸出の法（禁制品doc C5・2026-07-30）：武器で稼ぐ資本が禁止に反対＝軍需産業の大きい国ほど通らない・軸の増＝禁止
        // ★民間の武器購入の法（設計メモ§7 (a)・2026-09-11）：旧支配層が禁止を推し近代/大衆が抵抗。ゲート0＝法が動かない＝恒等。軸の増＝禁止。
        //   計器（MechanismProbe）＝試行は「目標≠現在で政治ゲートに掛けた」・成立は「法が動いた」（ゲート緑≠機構が働いた）。
        new(PolicyAxis.CivilianArms, w => w.CivilianArmsBan ? 1 : 0, (w, v) => w.CivilianArmsBan = v == 1, (w, p) => p.CivilianArmsBan ? 1 : 0, 1, Open: w => w.Coef.RebelArmsMarketEnabled > 0,
            Enact: (w, p, t) =>
            {
                int before = w.CivilianArmsBan ? 1 : 0;
                if (before != t) MechanismProbe.Eval(MechanismProbe.Site.CivilianArmsLaw);
                int after = StepLevel(before, t, w, PolicyAxis.CivilianArms);
                w.CivilianArmsBan = after == 1;
                if (after != before) MechanismProbe.Hit(MechanismProbe.Site.CivilianArmsLaw);
            }),
        new(PolicyAxis.OpiumPossession, w => (int)w.OpiumPossession, (w, v) => w.OpiumPossession = (ReformLevel)v, (w, p) => (int)p.OpiumPossession, 2, Stage: 2),   // 阿片の保有法（禁制品doc C3・2026-07-30）：許可→輸入禁止→禁止の単調な梯子（1段ずつ＝史実の順序をルールを書かずに保証）
        new(PolicyAxis.OpiumExport, w => w.OpiumExportBan ? 1 : 0, (w, v) => w.OpiumExportBan = v == 1, (w, p) => p.OpiumExportBan ? 1 : 0, 1),   // 阿片の輸出法（同）：植民地の阿片で稼ぐ資本が禁止に反対
        // 国有化法（E6・5部門＋『全て』）：各部門を独立にトグル。全て同じ政治価数（資本が反対・大衆が賛成）。位置は6行の点灯率（FactionSnapshot.AxisPosition）。
        new(PolicyAxis.Nationalization, w => w.NationalizeRailways ? 1 : 0, (w, v) => w.NationalizeRailways = v == 1, (w, p) => p.NationalizeRailways ? 1 : 0, 1),
        new(PolicyAxis.Nationalization, w => w.NationalizeMines ? 1 : 0, (w, v) => w.NationalizeMines = v == 1, (w, p) => p.NationalizeMines ? 1 : 0, 1),
        new(PolicyAxis.Nationalization, w => w.NationalizeMilitary ? 1 : 0, (w, v) => w.NationalizeMilitary = v == 1, (w, p) => p.NationalizeMilitary ? 1 : 0, 1),
        new(PolicyAxis.Nationalization, w => w.NationalizeOil ? 1 : 0, (w, v) => w.NationalizeOil = v == 1, (w, p) => p.NationalizeOil ? 1 : 0, 1),
        new(PolicyAxis.Nationalization, w => w.NationalizeElectricity ? 1 : 0, (w, v) => w.NationalizeElectricity = v == 1, (w, p) => p.NationalizeElectricity ? 1 : 0, 1),
        // ★『全て』（作者裁定2026-08-14 §11-6d）＝旧 Telecom/Manufacturing/Agriculture/Retail の統合。専用軸 NationalizationAll ＝
        //   賛否は Nationalization と同一で、前提DAG（プロレタリア独裁のみ＋官僚制度）だけが加わる（StepLevel 内の LawPrereq が読む）。
        //   政体を離れて残った制定目標は現状へ丸める。撤回（false向き）は常に目標どおり＝ポスト・ソ連の民営化。
        //   ⚠2026-09-18 点検：旧注記の「参政権 sufTarget と同じ作法」の sufTarget は 2026-09-12 FIX3-1 で削除済み。この丸めも施行上は LawPrereq（プロレタリア独裁のみ）と重複する。
        //   ★2026-09-28（処理の一元化 段2 束2）：丸めを**目標（Target）そのもの**へ移した＝施行と戦線の数え上げが同じ丸めた目標を読む
        //     （旧＝施行だけが丸め、戦線は丸めない p を読んだ＝政体を離れた国に届かない『全て』の目標が恒久の幽霊戦線になっていた）。施行は既定の StepLevel（同じ一歩）。
        //   位置は表に出さない（FactionSnapshot.AxisPosition＝0.5・要求の軸から除く ComputeDemands と同じ）。旧4部門の4戦線が1戦線に畳まれる（§11-6d）。
        new(PolicyAxis.NationalizationAll, w => w.NationalizeAll ? 1 : 0, (w, v) => w.NationalizeAll = v == 1,
            (w, p) => p.NationalizeAll && (w.Polity == Polity.ProletarianDictatorship || w.NationalizeAll) ? 1 : 0, 1),
        // 参政権の範囲2法（作者裁定2026-08-16・SuffrageScopeGate=1.0＝点火2026-09-06）：植民地参政権（3段）・移民原住民参政権（2段）。
        // gate=0 へ戻すと目標＝現在（既定 None・AI/議会/勢力が目標を置かない）＝StepLevel が即 return＝厳密恒等。
        new(PolicyAxis.ColonialSuffrage, w => (int)w.ColonialSuffrage, (w, v) => w.ColonialSuffrage = (ColonialSuffrageLaw)v, (w, p) => (int)p.ColonialSuffrage, 2, Stage: 2),   // なし→自国民のみ→全面を1段ずつ
        new(PolicyAxis.MigrantNativeSuffrage, w => (int)w.MigrantNativeSuffrage, (w, v) => w.MigrantNativeSuffrage = (MigrantNativeSuffrageLaw)v, (w, p) => (int)p.MigrantNativeSuffrage, 1, Stage: 1),   // なし0／あり1（2段＝段の飛び越しが存在しない）
    ];

    /// <summary>軸→表の行（同じ軸の行が複数あるのは国有化の5部門だけ＝先頭の行・段も位置も表を読まない軸）。</summary>
    private static readonly Dictionary<PolicyAxis, LevelLaw> LevelLawByAxis = LevelLaws.GroupBy(l => l.Axis).ToDictionary(g => g.Key, g => g.First());

    /// <summary>★税・支出の段の軸（作者裁定2026-09-12 R1・2026-09-20 点火）の1行（2026-09-28 処理の一元化 段1）＝施行（<see cref="Move"/> の目標は <see cref="Q4"/>）・
    /// 戦線の数え・段（<see cref="TaxStage"/>）・位置・施行中の述語（<see cref="EnactingStage"/>）が同じ1行を読む。値は実効の率・目標は政策の目標（0..1）。</summary>
    internal sealed record RateLaw(PolicyAxis Axis, Func<World, double> Get, Action<World, double> Set, Func<NationPolicy, double> Target);

    /// <summary>税・支出の段の軸の表＝**施行の順**（Move は政治資本を燃やす＝並びは挙動）。</summary>
    internal static readonly RateLaw[] RateLaws =
    [
        new(PolicyAxis.IncomeTax, w => w.TaxRate.Value, (w, v) => w.TaxRate = new Share(v), p => p.IncomeTaxTarget),
        new(PolicyAxis.ProgressiveTax, w => w.ProgressiveTax, (w, v) => w.ProgressiveTax = v, p => p.ProgressiveTaxTarget),   // 累進課税（A-1#3配線）＝富裕の政治抵抗で律速（査定の弱さは徴収側の漏れ）
        new(PolicyAxis.PollTax, w => w.PollTax, (w, v) => w.PollTax = v, p => p.PollTaxTarget),
        new(PolicyAxis.LandTax, w => w.LandTax, (w, v) => w.LandTax = v, p => p.LandTaxTarget),
        new(PolicyAxis.EducationSpending, w => w.EducationSpending, (w, v) => w.EducationSpending = v, p => p.EducationSpendingTarget),   // ★R1：社会保障（法）と同じ4段
    ];

    private static readonly Dictionary<PolicyAxis, RateLaw> RateLawByAxis = RateLaws.ToDictionary(l => l.Axis);

    /// <summary>法の位置∈[0,1]（<see cref="FactionSnapshot.AxisPosition"/> の表の分）。段の法＝min(段,最上段)/最上段・税と支出の段＝段の序数/段数・表に無い軸は 0.5（中庸）。
    /// ★税・支出の段（作者裁定2026-09-12 R1）：段になった軸は**段の序数/3**で測る（段の法の `段/最上段` と同形）。
    ///   ⚠Move は目標へ指数接近するだけで到達しないので、実効値のままだと最上段を与えきっても v&lt;1 が残り
    ///     「法を与えきっても要求が消えない／不満（LawGap）が満額のまま」になる＝2026-08-01 の no-op 要求と同型の力学バグ。
    /// ★錨2026-09-18：**Q4（率）ではなく段序数/段数**を読む。錨が1.0だった間だけ両者は数値的に同一で、軸ごとの錨（所得税0.35等）を
    ///   入れた瞬間に Q4 は率を返す＝最上段を与えきっても v=0.35 で Headroom が0.65 残り、要求も不満（LawGap）も永久に消えない。ここは「正規化位置」を測る場所なので段で測る。</summary>
    internal static double LevelPosition(World w, PolicyAxis a)
        => RateLawByAxis.TryGetValue(a, out var rate) ? TaxStage(a, rate.Get(w)) / (double)TaxLawStages
         : LevelLawByAxis.TryGetValue(a, out var law) ? Math.Min(law.Get(w), law.Top) / (double)law.Top : 0.5;

    public static void Step(World w, Coefficients c)
    {
        var p = w.Policy;

        // 第6段6-1（2026-07-28）政治予算制約：この四半期に開いている戦線（目標≠現在の政治軸）の数Nを先に数え、
        // 押す力を (1/N)^PoliticalBudgetGate で正規化（Moveの速度に乗る。旧はマンデート余白 θ にも乗っていた＝θ は 2026-09-21 に退役）＝「全部を同時には押せない」。
        // 0で全軸に全力＝厳密恒等・1で完全正規化。★これが入ると一根一ポートの手整理の多くが不要になる（予算が自然に絞る）。
        w.PoliticalPushBudget = 1.0;
        if (c.PoliticalBudgetGate > 0)
        {
            int fronts = CountOpenFronts(w, p);
            if (Instruments.On) Instruments.PushFronts(w, fronts);   // 計器（読むだけ）：開いた戦線の数
            if (fronts > 1) w.PoliticalPushBudget = Math.Pow(1.0 / fronts, c.PoliticalBudgetGate);
        }

        // ★政治資本の最大量 b（作者裁定N2・2026-09-21）＝**四半期に1回だけ**導出して World へ置く（すぐ上の政治予算と同じ場所・同じ作法）。
        //   軸ごとに導出すると材料の Ideologies.National（全POP走査）が 1四半期に法35軸＋スライダー10本ぶん、
        //   法律タブに至っては毎tick 45回走る＝重い。★2026-09-21 ㉖で旗 PoliticalCapitalStock を焼き込み＝毎四半期かならず導出する。
        w.PoliticalCapitalMax = PoliticalQuantities.PoliticalCapitalMax(w);

        // 債務緊縮の強度（下の施行の後に置く＝下の注記）。[単位・B13] 債務/GDP＝**年次名目**（GovDebt÷(月次NominalGdpPerMonth×12)）＝Fiscal の国債の金利の dGdp・CapitalMarket の対外信用と同じ物差し（2026-09-29・棚卸し fis-02 の残り）。
        //   旧＝**月次実質**（GovDebt÷月次Gdp）＝年GDP の約1.7% で緊縮が始まり約8% で満額＝史実の国債の国は開幕から強度1（値の史実は Coefficients.AusterityStartDebt）。
        //   ⚠導出をここ（施行の前）に置いても値は同じ＝材料の国債と名目GDP（州×生産者の産出×参照価格×物価水準）を下の施行は書かない。
        double austGdp = w.FiscalEnabled && w.GovDebt.Value > 0 ? w.NominalGdpPerMonth.Value : 0;   // 州×生産者の走査は債務のある国だけ・1回（旧は条件と割り算で2回）
        double austerity = austGdp > 1e-9
            ? PoliticalQuantities.Clamp01((Calendar.ToGdpYear(w.GovDebt, Calendar.PerMonthToPerYear(new MoneyPerMonth(austGdp))) - c.AusterityStartDebt) / c.AusterityRange) : 0;

        // ★助走は法を動かさない（2026-09-30 (119)・(109) の申し送り3）＝施行（下の段の法・税と支出の段・治安維持力・軍事費と陸海の維持費・関税・金利・行政費）は
        //   開始前の助走（World.WarmupFrozen）では走らない＝開幕の法は world.json と史実の初期値のまま始まる。法の施行は「年月をかけて起きること」＝国庫・国債・人口・資本と同じ門。
        //   旧＝助走は政治（PoliticsEnabled=false）だけを止め、AI の目標と施行は回っていた＝5年の助走で所得税 0→0.02〜0.05・土地税の最上段 0.4→0.32・金利 0.25→0.02〜0.24・
        //   関税・官僚制・秘密投票・徴兵法・教育法・穀物法・国教・通貨（不換→銀）まで動いた（seed 1789 で61カ国の全て）。
        //   導出量（上の政治予算・政治資本の最大量・緊縮の強度）は助走でも導出する＝開幕の最初の四半期の議会と法の門は助走の盤面の値を読む（旧と同じ）。
        //   AI の目標（DecideQuarter）は止めない＝目標は意図であって法ではない（開幕の最初の四半期に同じ関数が置き直す＝AI とプレイヤーは同じ入口）。
        //   ⚠法でないもの＝専売の材・補助・売買方針・国営工場（生産の決断＝作者裁定 2026-09-20）・制度の解禁（特許・株式会社・鉄道・検地台帳＝社会の盤面から導く）・兌換の運転（信用の門）は助走でも動く＝経済の定常の一部。
        if (w.WarmupFrozen) { w.AusterityIntensity = austerity; return; }

        // ★(178) 戦時の非常の法（徴兵法・作者 2026-10-03「全て推奨でよい」で採った推奨「戦時に上げた徴兵法は戦後に戻る」・形は (168) のパッチ）＝戦時に上げた徴兵法は、戦争が終わった四半期に戦前の段へ戻る（R2「戦時経済は総動員とセットで、戦後は下の値に戻る」と同じ形＝戦時の段は戦争が前提）。
        //   史実＝英の兵役法（1916）は戦争の期間に限る法で 1920 に廃止・米の徴兵法（1863・1917）は戦後に終わった・仏の国民総動員（1793）の後は平時の法へ戻った。
        //   平時から有った徴兵法（普・仏・露）は戻らない（戦前の段が徴兵なら徴兵のまま）＝戻すのは「今の段が戦前の段より上」のときだけ。平時の再制定は AI もプレイヤーも門を通す（下のループ）。
        //   戦前の段は交戦に入った四半期に記録する（World.ConscriptionPreWar＝思想⑩の例外の理由はその注記）。クールダウンではない＝戦争の終わりという状態で決まる。
        //   保存目標も同じ段へ戻す（R2 の戦時段の失効と同じ＝次の戦争で選び直さずに黙って上がらない）。全国が四半期頭に通る1箇所＝AI とプレイヤーの共通経路（IsPlayer を読まない）。
        if (w.AtWarNow) { if (w.ConscriptionPreWar < 0) w.ConscriptionPreWar = (int)w.ConscriptionLaw; }
        else if (w.ConscriptionPreWar >= 0)
        {
            if ((int)w.ConscriptionLaw > w.ConscriptionPreWar) { w.ConscriptionLaw = (ConscriptionLaw)w.ConscriptionPreWar; p.ConscriptionLaw = w.ConscriptionLaw; }
            w.ConscriptionPreWar = -1;
        }

        // 法整備（離散）：力の均衡が許す向きに一歩ずつ。教育法・参政権・徴兵法は World が現在値を持ち効果に効く。
        // ★段の法は表 <see cref="LevelLaws"/> を施行の順に1本のループで回す（2026-09-28 処理の一元化 段1＝旧は軸ごとに StepEnum/StepBool の1行・各軸の注記は表の行へ移した）。
        foreach (var law in LevelLaws)
        {
            if (law.Open != null && !law.Open(w)) continue;
            int target = law.Target(w, p);
            if (law.Enact != null) law.Enact(w, p, target);
            else law.Set(w, StepLevel(law.Get(w), target, w, law.Axis));
        }

        // スライダー（連続）：変更方向に反対する集団の力で律速
        // 税制：人頭税・関税は常時。土地税は検地台帳（LawPrereq）が前提。⚠専売は税ではなく生産の決断＝2026-09-20に政策軸ごと撤去。
        // ★裁定2026-09-12（FIX1）：所得税・累進の**行政力の天井を廃止**（施行側の Min も LawPrereq の腕も削除＝他の税と同じ Q4 1本）。
        //   行政の弱さは「その税率を選べない」ことではなく「取り立てた金が国庫へ届かない」ことで出る＝Fiscal の汚職漏れ
        //   （revenue × CorruptionMax × EffectiveCorruption、EffectiveCorruption=(1−AdminEfficiency)(1−OversightV2)）が唯一の担い手。
        //   天井はその同じ量（AdminPowerSmoothed＝AdminEfficiency の移動平均）を率の側にも掛けていた＝二重計上だった
        //   （土地税の landEff が同じ理由で撤去済み＝下の Fiscal の注記と同文）。
        // 連続のレバー（関税・行政費・金利）は5%刻み（AI/プレイヤー共通）：0..1の目標を0.05に丸めてから適用（政治の漸進ロックは別途 Move が担う）。
        // 治安維持力(BaseCoercion)は200〜4000スケールで武力スライダーに従属（CoercionTarget は軍事費から導出）＝0.05丸めの対象外。
        // ★税・支出の段（作者裁定2026-09-12 R1「税に関するスライダーを廃止し『なし/低/中/高』の4段階のボタンにする。税の挙動は今までと同じ」・2026-09-20 点火）：
        //   <see cref="Q4"/>＝**目標だけ**が4段の格子に乗る（Move は1命令も変えない＝漸進・抵抗・議会の予算協賛はそのまま）。関税は R3 で対象外（下の Tariff は Q5 のまま）。
        // ★作者裁定2026-09-20「専売の率を法律から完全に消す。材を選ぶのみにする」：ここに専売の施行は**無い**。
        //   専売は材ごとの ON/OFF（World.SetStateMonopolyOn）だけで、国ごとの率という状態が存在しない＝Move する連続値が無い。
        //   政治の門は同じ1本（GateInfo の純支持＋政治資本の在庫）を**そのセッターが**引く＝「議会の同意なしに歳入構成は動かない」は保たれる。
        // ★裁定B（2026-09-12「今までの『上限/下限を削る』挙動は全て廃止する」）：未解禁の土地税を実効値0へ叩き落とす三項は 2026-09-20 に削除。
        //   「検地台帳が無い」は Move の後で値を書き潰すことではなく、LawPrereq（case LandTax＝「検地台帳が前提」）が**その段を押させない**こと。
        // 社会保障の旧スライダー枝は第3段3-6で削除（SocialSecurityLawGate=1.0焼き込み＝上の法駆動が正）。
        // ★税・支出の段は表 RateLaws を施行の順に回す（2026-09-28 処理の一元化 段1＝旧は軸ごとの Move の1行）。
        foreach (var law in RateLaws) law.Set(w, Move(law.Get(w), Q4(law.Axis, law.Target(p)), c, w, law.Axis));
        // 武力統合：治安維持力＝**装備された軍の効果**＝兵数×即応度×係数（治安維持力=軍の一本化 2026-06-23）。武器を買えぬ軍（即応度0）は治安維持力も0→革命。較正/単体は false で CoercionTarget を独立保持（単一国較正セル不変）。
        if (w.CoercionFollowsForce)
            p.CoercionTarget = CoercionTargetFromForce(w.SoldierSizeK() * w.MilitaryReadiness * c.CoercionPerSoldier, c);
        w.BaseCoercion      = Move(w.BaseCoercion,      p.CoercionTarget,          c, w, PolicyAxis.Coercion, c.CoercionMax, mandate: !w.CoercionFollowsForce);   // 治安維持力＝武力スライダー(軍事費)から導出された目標へ（5%丸めは軍事費側で実施）。武力統合下は機械追従＝マンデート余白を適用しない（従来の硬いゲート＝専制の保持力ラチェットを保つ）。較正/単体（CoercionFollowsForce=false）は意図した目標＝余白対象。消費の正規化は治安維持力の実スケール＝CoercionMax
        // ★軍事費の床も撤去（同上・2026-08-19）。旧＝政体の国家軍軸×MilitaryFloorScale。⚠**政体から導出していたので同じ理由で消える**。
        // ★陸海の維持費（2026-08-04）：政治ゲートは**合計**に掛ける（軍系ブロックが見るのは軍事費の総額であって内訳ではない）。
        //   合計が決まったら、目標の陸海比でその場で按分する＝ゲートの通り方は旧と同一（合計の動きが同じ）。
        // ★戦時の下限ロック（2026-08-04・作者仕様(5)「政権に加え、戦時ではさらに下限にロックがかかる」）＝戦争中に軍を解散することはできない。
        //   裁定B（2026-09-12）で目標を Max で黙って持ち上げるクリップ（P8違反）をやめ、LawPrereq（case Coercion＝「戦争中は軍を解散できない」）が
        //   **同じ係数 WartimeUpkeepFloor のまま**「その段には下がれない」門として持つ（旧クリップは 2026-09-20 の段の点火で削除）。
        //   ⚠係数は PowerShares の LockedSpending（鉄の予算＝年次審議の外の歳出）も読み取り専用の比として読む。
        double milArmyQ = Q5(p.ArmyUpkeepTarget), milNavyQ = Q5(p.NavyUpkeepTarget);
        double milRawTotal = milArmyQ + milNavyQ;   // クランプ前の生和＝下の戦時段の失効の段の判定が読む（(151) から陸海比は丸めない目標の比＝下の navShareTarget）
        // ★軍事費の段（作者裁定2026-09-12 R2「低/中/高/戦時の4段。戦時経済は総動員とセットで、戦後は下の値に戻る」）。
        //   段は**合計にだけ**掛かる＝陸海比（navShareTarget）は連続のまま（D4：別々に段化すると平時0.25×海軍比上限0.60=0.15＜第1境界で
        //   全AIの海軍が段0・海洋国は陸も段0＝**英国に軍が無い**＝Diplomacy.cs が記録する2026-08-04のバグの再生産）。
        double milTotalTarget = MilitaryTotalTarget(w, p);
        // ★戦時段の戦費の失効（2026-10-03 (181)・推奨で進めた・作者の承認は未確認）＝作者裁定 R2（2026-09-12）「戦時経済は総動員とセットで、戦後は下の値に戻る」の施行の側。
        //   解禁（交戦中かつ総動員中＝MilitaryTotalTarget が戦時段を開く述語と同じ）の外では、施行も段3（高）を超えて残らない＝戦費の議決が失効する
        //   （英 1815-20 に軍事費を約3分の1へ【推】）。旧＝目標だけが段3 へ戻り、施行は下の Move で軍の側の抵抗に逆らって四半期 0.011〜0.016 しか下りず、
        //   平時の国×四半期の 16〜19%（1784-1913）が段4 の水準に残った（実装ログ (175) §1-①）。失効は Move の前＝政治資本を燃やさない（議決が切れるだけ）。
        //   段3 より下の施行は今のまま（Move＝軍の抵抗）。AI とプレイヤーは同じ1か所（IsPlayer を読まない）。新しい数・新しい状態は無い。否定形＝NaN の施行も段3。
        //   ⚠思想⑧の天井ではない＝解禁の外では選べる目標が既に段3 以下（MilitaryTotalTarget）＝選んだ値は削らない（施行に残った戦時の上乗せが切れるだけ）。
        double milFrom = w.MilitarySpending, milHigh = (MilitaryLawStages - 1) / (double)MilitaryLawStages;
        if (!WarStageOpen(w) && !(milFrom <= milHigh)) milFrom = milHigh;
        // ★(270) 戦時段の解禁の間（交戦中かつ総動員中）の軍事費の一歩は政治予算の係数（戦線の数 N で (1/N)^0.5）で薄めない＝fullPush。計器で名指した（実装ログ (270) §1）：
        //   英の解禁の四半期 460 で純支持の門・政治資本の門に固定された一歩は 0 件・抵抗の中央は 0 だが、政治予算の係数が中央 0.354（N の中央 8）で歩幅が基本の約 0.35 倍＝登りの律速。
        //   純支持と政治資本の門は掛けたまま（反対を押し切るなら代価を払う）。解禁の外・他の軸は旧と同じ。AI もプレイヤーもこの1行を通る（IsPlayer を読まない）。
        double milTotal = Move(milFrom, milTotalTarget, c, w, PolicyAxis.Coercion, fullPush: WarStageOpen(w));  // 武力スライダー＝軍隊規模と治安維持力を一本で駆動。軍系ブロックの政治力で律速
        // ★(151)：陸海比は目標の生の比（0.05格子に丸めない）＝上の「連続のまま」のとおり。旧＝格子に丸めた2本の比＝合計0.75 なら比は 0.067 刻みで、
        //   陸と海の必要量で分けた小さい海の比（清の 0.03 など）が0へ丸められて艦隊の乗員ごと消えた（陸海比は導出量＝2026-09-23 作者裁定・レバーは無い）。
        double navRawA = p.ArmyUpkeepTarget, navRawN = p.NavyUpkeepTarget;
        double navShareTarget = navRawA + navRawN > 1e-9 ? navRawN / (navRawA + navRawN) : w.NavalSpendShare;
        w.SetMilitarySpending(milTotal, navShareTarget);
        // ★戦時段の失効（作者裁定2026-09-19 M2「戦後は高へ戻る」）：段4の前提（交戦中かつ総動員中）は登れなさでなく**保てなさ**
        //   （LawPrereq の case Coercion には土地税・教育費にある「現在段以下は通す」の免除が無い）＝解禁が落ちた四半期に保存目標も段3へ戻す。
        //   さもなくば保存4・施行3・表示3（法律タブは MilitaryTotalTarget を読む）の隠れた食い違いが残り、次の総動員で選び直さずに黙って段4へ登る。
        //   全国が四半期頭に通る1箇所＝AI とプレイヤーの共通経路（IsPlayer を読まない）。陸海比は上で算出済み＝この四半期の施行は変わらない。
        //   新しい数・新しい状態はゼロ（段の写像と書き込み口は既存の1本）。
        if (MilitaryStage(milRawTotal) > MilitaryStage(milTotalTarget)) p.MilitarySpendingTarget = milTotalTarget;
        // ★旧「海軍偏重」の漸進（Move(w.NavalEmphasis, …)）は撤去（2026-08-10 作者裁定）＝陸海の配分は上の2本の維持費だけが決める。
        //   政治ゲートは合計に1回だけ掛かる（PolicyAxis.Coercion）＝内訳に2枚目のゲートは無い。
        // 関税目標＝国内政治由来(p.TariffTarget)＋外交層の報復/相互偏倚(TariffRetaliationBias)。偏倚は既定0＝従来。多国かつTariffRetaliation>0でのみ非0
        w.Tariff            = Move(w.Tariff,            Math.Clamp(Q5(p.TariffTarget) + w.TariffRetaliationBias, c.TariffFloor, 1), c, w, PolicyAxis.Tariff);   // 下限＝負の関税（ダンピング）。AIも産業スタンスの輸出攻勢で負値を使う（SelfPlay側のダンピング＝条件を外れると0へ解除・監査2026-07-11 他6）
        // 金利＝中央銀行の金融政策レバー（利益集団の抵抗は受けない）。目標へ寄る。
        // ★トリレンマを閉じる②（2026-08-13）：**「兌換維持＋資本自由」のときだけ金利は自分のものでなくなる**。
        //   このとき目標は政策の意思でなく**正貨のカバー率を守るための帰結**＝カバー率が目標を下回れば上げ、十分なら下げる。
        //   ⚠新しい概念は1つも置かない：目標カバー率は既存の再加入条件（GoldCoverMin×GoldRejoinCoverMargin）をそのまま読む。
        //   自己安定する：金利↑→PolicyRateCapitalDraw で正貨が来る→カバー率が戻る→金利↓（★史実＝誰も金融政策の自律を選ばなかった）。
        //   TrilemmaRateGate=0 なら下の分岐に入らず＝厳密恒等（★ 2026-08-13 点火＝既定1.0）。
        double rateTarget = Q5(PoliticalQuantities.Clamp01(p.InterestRateTarget));
        // ★銀の段の軽い規律（SilverLightDiscipline・2026-08-21・**live=1**）：金利防衛（バンクレート）は発券銀行制度＝金の段の制度。
        //   1783年の銀正貨国に金利レバーの自動化を課すと約60カ国のレバーが死ぬ（19-6差し戻しの根①）＝銀の段では自動化しない。
        //   0に戻せば旧来（銀の段も自動化）と厳密恒等。
        if (c.TrilemmaRateGate > 0 && w.OnGoldStandard && !w.CapitalControlLaw && (c.SilverLightDiscipline <= 0 || w.GoldStandardLaw))
        {
            double coverage = w.BroadMoney.Value > 1e-9 ? w.BaseMoney / w.BroadMoney : 1.0;
            double coverTarget = c.GoldCoverMin * c.GoldRejoinCoverMargin;
            double gap = coverTarget > 1e-9 ? (coverTarget - coverage) / coverTarget : 0;
            gap = Math.Clamp(gap, -1, 1);
            if (!(gap >= -1 && gap <= 1)) gap = 0;   // NaNは中立へ倒す（否定形で書く＝Clampは NaN を消さない）
            rateTarget = PoliticalQuantities.Clamp01(w.InterestRate + c.TrilemmaRateResponse * gap);
        }
        w.InterestRate += c.PolicyBaseSpeed * (rateTarget - w.InterestRate);
        // 行政費＝官僚機構の予算編成（行政府の内部事項＝利益集団の抵抗は受けない）。目標へ寄る。
        w.AdminSpending += c.PolicyBaseSpeed * (Q5(PoliticalQuantities.Clamp01(p.AdminSpendingTarget)) - w.AdminSpending);
        // ★撤去2026-08-16（C2-C5）：汚職根絶費の予算編成（AntiCorruptionSpending←AntiCorruptionTarget平滑）を物理削除＝汚職V2恒久ONで下流（Fiscal請求・清廉度更新）が全て死んでいた。
        // P9改（2026-07-19ユーザー裁定）：国民形成費レバーは廃止＝NationBuildingSpending は Fiscal の需要駆動ブロック
        // （「選挙を望む層」の自費拠出＝実支払い/満額）が毎月導出する。ここでは動かさない。

        // 債務緊縮＝**債権者が強いるハード制約**。借金がかさむほど、貸し手（債券保有＝大半は貴族・ブルジョア）に響かない形で赤字を閉じる。
        // 抜け出す唯一の道は債務/GDPを閾値以下へ減らすこと＝借金に牙を持たせる。
        // これにより「対外債務不履行→債権国の介入」の外科的な例外は不要（同じ帰結を一般機構で出す）。
        // ★牙の形（裁定B・2026-09-12）＝**「削る」から「選ばせない」へ**（旧の Min/Max の3本は 2026-09-20 の段の点火で削除）：
        //   旧＝弱者救済の実効値に上限を敷き、逆進課税の実効値に下限を敷いていた（選んだ値を毎期書き換える＝P8違反）。
        //   新＝<see cref="LawPrereq"/> の門（「緊縮の下限」「緊縮の上限」）＝債権者が奪うのは**法を選ぶ自由**であって値ではない。
        //   ⚠門は「上げる力」を持たない＝緊縮下で逆進税を自動で引き上げる手は消える（自発的に上げるのは SelfPlay の財政ストレス／プレイヤーの判断）。
        //     債務スパイラルは未払い（Fiscal の利払い・維持費の Min）→FiscalFundingRatio の劣化→デフォルト（払えなかった大きさの累計が DefaultArrearsMonths に届く・(205)）で閉じる。
        //   ⚠w.AusterityIntensity の計算は残る＝三部会の召集信号（CrownParliament が >0.5 を読む）と門の材料。
        //   軍事費の緊縮は**不採用**（2026-06-18 検証）：負債国だけが動員解除し財政健全な覇権国は軍を維持＝非対称な武装解除で力の格差が拡大した。
        // ★値は施行の前（上の austerity）で導出し、置くのはここ＝この四半期の法の門は前の四半期の値を読む（2026-09-30 (119) で導出だけを上へ移した・並びは旧と同じ）。
        w.AusterityIntensity = austerity;
    }

    // スライダーの5%刻み：0..1の目標を0.05単位に丸める（UI/AI共通の「つまみは20段」仕様）。
    // ⚠internal（2026-09-20）：World.StateMonopolyBreadth が専売の広さを同じ格子へ乗せる（新しい丸め規則も新しい数も作らない）。
    internal static double Q5(double x) => Math.Round(x / SliderStep, MidpointRounding.AwayFromZero) * SliderStep;

    /// <summary>★税・支出の段（作者裁定2026-09-12 R1・錨2026-09-18）＝0..<see cref="TaxLawStages"/>。0.05格子の値を、その軸の錨
    /// （<see cref="TaxLawTop"/>）で正規化してから4段へ落とす。境界＝M/6・M/2・5M/6（所得税なら 0.0583／0.175／0.2917）。
    /// ⚠中点（k+0.5）は <see cref="MidpointRounding.AwayFromZero"/> で常に上段＝決定論（丸め規則を明示しているので「ちらつき」は生じない）。
    /// ⚠NaN は (int)Math.Round(NaN)→0 で段0＝課さない側（fail-closed）。</summary>
    internal static int TaxStage(PolicyAxis a, double x) => Math.Clamp((int)Math.Round(Q5(x) / TaxLawTop(a) * TaxLawStages, MidpointRounding.AwayFromZero), 0, TaxLawStages);

    /// <summary>★段序数→率（錨2026-09-18）＝<see cref="TaxStage"/> の逆写像。**段序数から値を作る式はこの1本だけ**
    /// ＝旧は6箇所が別々に <c>段/3</c> と書き、錨が1.0だったので偶然一致していた（型の統合が起こす静かな破損と同型）。
    /// 読み手＝<see cref="Q4"/>／SelfPlay.GateAllLawTargets／Parliament.TargetDemand／Politics.ApplyDemand／法律タブ（GameViewModel.Law）。</summary>
    /// <remarks>⚠最上段だけは錨を**そのまま**返す（<c>3×0.35/3</c> は 0.34999999999999992 で 0.35 と1ulpずれる＝
    /// 「最上段＝その軸の教義最大」が浮動小数で偽になり、教義値を置いた国が最上段に居ないと読まれる）。
    /// 錨1.0（教育費）では <c>段×1.0/3</c> が旧 <c>段/3.0</c> と全ビット一致＝この軸は錨の前後で1ビットも変わらない。</remarks>
    internal static double TaxValue(PolicyAxis a, int stage)
    {
        int s = Math.Clamp(stage, 0, TaxLawStages);
        return s == TaxLawStages ? TaxLawTop(a) : s * TaxLawTop(a) / TaxLawStages;
    }

    /// <summary>★軍事費の段（同 R2）＝1..<see cref="MilitaryLawStages"/>。**「なし」が無い**＝最下段でも 0.25（AIの平時既定＝SelfPlay.Apply 冒頭の MilitarySpendingTarget=0.25 と厳密一致・行番号は腐るので関数名で指す）。
    /// ⚠NaN は (int)Math.Round(NaN)→0→Clamp で段1＝最低限の常備軍（fail-safe＝軍が消えない側）。</summary>
    internal static int MilitaryStage(double x) => Math.Clamp((int)Math.Round(x * MilitaryLawStages, MidpointRounding.AwayFromZero), 1, MilitaryLawStages);

    /// <summary>★税・支出の目標の量子化（作者裁定2026-09-12 R1・2026-09-20 点火）＝段の格子へ落とした目標の率。
    /// ⚠量子化するのは**目標だけ**で <see cref="Move"/> は触らない（法の段から実効値を直書きすると、四半期ごとの階段が Fiscal の
    /// 貯蓄上限の連鎖に入り、抵抗の段階性＝<see cref="PolicyPreferences.OpposingPower"/> が二値の門に潰れる）。</summary>
    internal static double Q4(PolicyAxis a, double x) => TaxValue(a, TaxStage(a, x));

    /// <summary>★戦時段（軍事費の段4＝戦時経済）の解禁の述語の1本（2026-10-04 (198)・削減＝旧は同じ式 <c>AtWarNow &amp;&amp; Mobilized</c> を5か所に書いていた）
    /// ＝作者裁定 R2（2026-09-12）「戦時経済は総動員とセットで、戦後は下の値に戻る」＝交戦中かつ総動員中。読み手＝<see cref="MilitaryTotalTarget"/>（梯子の上限）・
    /// <see cref="Step"/>（解禁の外の施行の失効）・<see cref="LawPrereq"/>（段4 の門）・<see cref="Military.WarStageShare"/>（戦時段の器＝借入）・AI の戦時段の選択（SelfPlay）。</summary>
    internal static bool WarStageOpen(World w) => w.AtWarNow && w.Mobilized;

    /// <summary>★軍事費の合計目標（作者裁定2026-09-12 R2）＝**施行（<see cref="Step"/>）・戦線の数え上げ（<see cref="CountOpenFronts"/>）・
    /// 解隊の脅威（<see cref="PoliticalQuantities.MilitaryPush"/>）が読む唯一の式**。
    /// ⚠3箇所が別々の式を持つと、軍備教義の目標0.70に対し段は0.75＝恒久的なずれ0.05が居座り、(i) 戦線判定の閾値0.026を超えて
    /// 軍事の幽霊戦線が開きっぱなしになり (ii) 解隊の脅威が恒久的に +0.067＝軍国教義の閾値0.25の27%を丸め誤差が自己供給する。
    /// ★作者裁定2026-09-19 M2：総動員は戦時段を**解禁するだけ**で、選んだ値を押し上げない。<c>AtWarNow &amp;&amp; Mobilized</c> の間だけ梯子の上限が段4になり、
    /// それ以外は段3（高）で止まる＝段は選ぶもの（戦費は兵の動員とは別の予算決定＝英1914-15は志願制のまま巨額を支出）。
    /// 旧（2026-09-12 R2）は同じ述語で段4を**強制**していた＝選んだ値を黙って最上段へ上げる（P8 天井の鏡像）。
    /// 「戦後は下の値に戻る」＝新しいタイマーを作らない：交戦か総動員が落ちた時点で、この関数の返す上限はその場で段3（読んだ時点の述語）。施行と保存目標は次の四半期の頭の <see cref="Step"/> で段3（高）以下になる（戦時段の戦費の失効＝(181)・(182) の反証役 vm2#8 で注記を直した）。
    /// ⚠(181) の前は施行に失効が無かった＝Move が軍の抵抗で四半期 0.011〜0.016 しか下りなかった（この注記は「施行は Min で」と書いていたが、git の初めのコミット 2026-09-21 から施行に Min は無い）。
    /// 保存されている目標（段4）も同じ <see cref="Step"/>（次の四半期の頭）が段3へ書き戻す（戦時段の失効・2026-09-19）＝次の総動員で選び直さずに段4へ登らない。</summary>
    internal static double MilitaryTotalTarget(World w, NationPolicy p)
    {
        double raw = Q5(p.ArmyUpkeepTarget) + Q5(p.NavyUpkeepTarget);
        int stage = Math.Min(MilitaryStage(raw), WarStageOpen(w) ? MilitaryLawStages : MilitaryLawStages - 1);
        return stage / (double)MilitaryLawStages;
    }

    /// <summary>緊縮（債権者が強いる逆進フロア）が人頭税に敷く最下段（専売は 2026-09-20 に段の法から外した＝この床を持たない）。
    /// ⚠裁定B（2026-09-12）で <see cref="Step"/> の実効値への <c>Max</c> は廃止（2026-09-20 削除）＝床はこの段表現1本だけが持つ。
    /// ⚠切り上げ＝床を満たす最も低い段（切り捨てだと床を割る段が「押せる」ことになり、施行で押し戻されて表示が嘘になる）。
    /// ⚠錨2026-09-18：床は**率**なので、その軸の錨で正規化してから段へ写す（旧は錨1.0を前提に率をそのまま×3していた＝
    ///   人頭税なら満額緊縮の床0.5に対し段2＝0.333＜0.5 で床が床でなくなる）。新しい数はゼロ＝同じ <see cref="Coefficients.AusterityTaxHike"/>。</summary>
    private static int AusterityStage(World w, Coefficients c, PolicyAxis a)
        => (int)Math.Ceiling(PoliticalQuantities.Clamp01(Math.Min(1.0, w.AusterityIntensity * c.AusterityTaxHike) / TaxLawTop(a)) * TaxLawStages);

    /// <summary>★段になった財政の軸（作者裁定2026-09-12 R1/R2・2026-09-20 点火）。
    /// 関税は対象外（R3「関税はスライダーを維持してよい」）・行政費/金利は利益集団の抵抗を受けない内部事項なので元から軸を持たない。
    /// 軍事費は <see cref="PolicyAxis.Coercion"/>（武力スライダー＝軍事費の合計）。</summary>
    private static bool FiscalStagedAxis(PolicyAxis a) => RateLawByAxis.ContainsKey(a) || a == PolicyAxis.Coercion;   // 税・支出（表 RateLaws）と軍事費。★専売は 2026-09-20 に率ごと廃した（材の ON/OFF だけ＝段に乗る率が無い）

    // 押し切りの可否（名前は旧のまま＝マンデート余白 θ）。★2026-09-21 裁定S1で θ の算術は退役し、㉖の焼き込みで削除した＝戻り値は 0 か +∞ だけ。
    //   旧＝θ=MandateWeight×(Legitimacy−0.5)（2026-07-09〜09-21）：正統性の高い政権は純支持がやや負でも押し切れ（明治維新型）、正統性<0.5 では
    //   人気のある法さえ滞った（旧体制の罠・負側は旧 MandateNegativeScale で半減）。今は反対を押し切る一歩の門は政治資本の在庫だけ（PoliticalCapitalAffords）で、
    //   明治維新型の強行は「b の大きい政権が貯めてから払う」、旧体制の罠は「b（正統性＋支持）が小さく払えない」として同じ史実が出る。
    // PoliticsEnabled=false（財政較正セル・機構単体テスト・助走）では 0＝従来の硬いゲート（政治が凍結された世界で政治資本を払わせない）。
    /// <param name="targetLevel">その一歩で到達する段（bool法は1/0・スライダーは段の概念がないので −1）。
    /// ★憲法級の判定を**軸単位から段単位へ**下ろすために要る（2026-08-10・女性法の統合）：
    /// 統合した「女性の権利」は最上段（選挙権）だけが憲法級で、財産権・教育権の段は普通の政策として政治資本で押せる。
    /// 軸単位のままだと財産権の段まで政治資本不可になり、統合が政治の難度を勝手に上げてしまう。</param>
    /// <remarks>★裁定S1（2026-09-21）：<b>余白の門 θ は退役した</b>。反対を押し切る一歩の門は<b>政治資本の在庫ひとつだけ</b>
    /// （<see cref="PoliticalCapitalAffords"/>＝S+代価 ≤ b）＝門を2つから1つへ減らす（削減の向き）。
    /// 退役の理由は係数ではなく構造：旧 θ は1歩の代価を <c>θ×MandateSpendRate</c> で頭打ちにし、b も同じ2係数
    /// （MandateWeight×MandateSpendRate）で作られるので<b>両辺で打ち消し合い</b>、満タンなら必ず最も高い一歩を6本以上払えた
    /// ＝在庫の門が一度も噛まなかった（2026-09-21 ㉓検証＝selfplay-batch 10 の4腕と eduprobe で在庫の否決0件／同じ eduprobe で余白の否決100件）。
    /// <para>実装＝この関数が非憲法級に <c>+∞</c> を返すだけ。θ を読む4本の脚（施行:<c>MandateGate</c>／照会:<c>CanStep</c>／
    /// 点灯:<c>GateInfo</c>／スライダー:<c>Move</c>）は<b>すべて比較</b>なので同時に「通す側」へ倒れ、残るのは在庫の門だけになる。</para>
    /// <para>⚠<b>この戻り値は比較にしか使ってはいけない</b>：算術に混ぜると <c>∞−∞</c>／<c>0×∞</c> で NaN が出る。
    /// 唯一の除算 <c>−θ/costMul</c> は安全＝<see cref="ReversalCostMul"/> は必ず<b>有限かつ ≥1.0</b> を返す
    /// （<c>IrreversibleLaws ≥ 1.0</c> の禁止段でも 1.0 を返し、禁止そのものは <see cref="BlockedByIrreversibility"/> が先に止める）。</para>
    /// <para>結果（裁定が望んだもの）：代価が θ で頭打ちされないので<b>強く反対される改革は高く、貯めてから通す</b>ことになり、
    /// b の小さい国（低正統性・発言権を持つ層の支持が薄い）は<b>永久に通せない</b>＝S2「床の保証の廃止」。
    /// 旧体制の罠（正統性&lt;0.5 で人気の法さえ滞る非対称＝旧 MandateNegativeScale・係数は㉖で削除）も θ ごと退役し、
    /// その役目は <c>b = MandateWeight×MandateSpendRate×(正統性+支持)</c> の小ささが負う＝同じ史実を係数1本少なく出す。</para>
    /// <para>⚠憲法級（参政権・秘密投票・女性の権利の選挙権段）は <b>0</b> を返す＝硬い「純支持&gt;0」のまま
    /// ＝2026-08-04 の裁定「参政権は政治資本で押せない」を<b>そのまま保存</b>する。切り離し弁は
    /// 2026-09-26 に削除した（棚卸し dorm-50＝0が正しい値として確定した旗）。</para>
    /// <para>★焼き込み（2026-09-21 ㉖）：旗 PoliticalCapitalStock と θ の算術（正統性の傾き・負側の減衰・押し予算・憲法級倍率）を削除。</para></remarks>
    private static double MandateMargin(World w, PolicyAxis a, int targetLevel)
    {
        if (!w.PoliticsEnabled) return 0;
        // ★S1：門は在庫だけ＝非憲法級は +∞（θ を読む4本の脚の比較が「通す側」へ倒れる）。
        // ★憲法級の法（2026-08-04・作者指示「少なくとも参政権は固く」）：**政治資本では押し切れない**＝0（硬い純支持>0）。
        //   誰が票を持つかは統治の土台であって、政権の政治資本で動かせる普通の政策ではない。史実でも参政権の拡大は
        //   **力の均衡が実際に動いたとき**に通った（1832年の大改革法は革命寸前の圧力の産物・1867/1884も同じ）。
        return IsConstitutional(a, targetLevel) ? 0 : double.PositiveInfinity;
    }

    /// <summary>憲法級＝「誰が票を持つか・票がどう数えられるか」を決める法（2026-08-04）。
    /// 普通の政策と違い、政権の政治資本（在庫）では押し切れない＝実際の支持がなければ通らない。
    /// ★段単位（2026-08-10・女性法の統合）：女性の権利の梯子は**最上段（選挙権）だけ**が憲法級。
    /// 財産権・教育権の段は有権者の範囲を変えないので普通の政策（統合前の WomenProperty/WomenEducation と同じ扱い）。</summary>
    private static bool IsConstitutional(PolicyAxis a, int targetLevel) =>
        a is PolicyAxis.Suffrage or PolicyAxis.SecretBallot
        or PolicyAxis.ColonialSuffrage or PolicyAxis.MigrantNativeSuffrage   // 参政権の範囲2法（2026-08-16）＝「誰が票を持つか」を決める法＝定義どおり憲法級（参政権と同型）
        || (a == PolicyAxis.WomenRights && targetLevel >= (int)WomenRightsLaw.Suffrage);

    /// <summary>その向きの「次の一歩」が到達する段（憲法級の段単位判定に渡す）。段を持たない軸は 1/0 を返す（bool法と同じ扱い）。</summary>
    internal static int NextStage(World w, PolicyAxis a, bool increasing)   // internal（2026-09-11）＝番人テストが「法律タブが押せる段」を同じ式で照会する
    {
        int cur = CurrentStage(w, a);
        return cur == int.MaxValue ? (increasing ? 1 : 0) : cur + (increasing ? 1 : -1);
    }

    /// <summary>第6段6-1：開いた戦線数＝目標≠現在の政治軸（マンデートゲート/政治ロックを通る軸のみ。機械追従・無抵抗レバー
    /// ＝金利/行政費/治安維持力の武力統合は数えない）。スライダーは5%量子化を跨ぐ差のみ戦線と数える。</summary>
    private static int CountOpenFronts(World w, NationPolicy p)
    {
        int n = 0;
        // ⚠**呼び手が施行と同じ最終目標を渡す**（段の軸は段の値・連続の軸は Q5 の値）。2026-09-12 の段（R1/R2）で
        //   「施行は段・戦線の数え上げは0.05刻み」の食い違いが恒久的に開いた幽霊戦線を作るのを構造で封じる
        //   （旧＝この関数の中で Q5 を掛けていたので、呼び手が段を渡しても 1/3 が 0.35 へ丸め直されていた）。
        void Front(double cur, double target) { if (Math.Abs(target - cur) > 0.026) n++; }
        // ★段の法＝表 LevelLaws の1本（施行と同じ行の目標と門を読む＝2026-09-28 処理の一元化 段1。旧は軸ごとの1行＝施行と手で対応させていた）。
        //   ⚠戦線だけの門（FrontOpen）＝国教（プロレタリア独裁では国家無神論の強制＝戦線にしない）の1本だけ（通貨の門は 2026-09-28 に削除＝施行と同じく数える）。
        foreach (var law in LevelLaws)
            if ((law.FrontOpen ?? law.Open)?.Invoke(w) != false && law.Get(w) != law.Target(w, p)) n++;
        // ★天井の廃止（2026-09-12 FIX1）以後、所得税/累進は他の税と**同じ Q4 1本**＝施行（Step:145-146）と同一式（幽霊戦線が立たない）。
        foreach (var law in RateLaws) Front(law.Get(w), Q4(law.Axis, law.Target(p)));   // ★税・支出の段＝表 RateLaws（施行と同じ Q4）
        // ★専売の戦線は無い（作者裁定2026-09-20）＝施行する連続値が無いので「目標≠現在」が定義できない。
        //   ⚠意図した副作用＝軸が1本減り PoliticalPushBudget=(1/N)^Gate が他軸を薄めなくなる（旧 NavalEmphasis の撤去と同文）。
        // ★段（R2・2026-09-12）：施行と同じ MilitaryTotalTarget を渡す（wave2b の教訓＝施行と戦線判定は同じ式）
        Front(w.MilitarySpending, MilitaryTotalTarget(w, p));
        // ★旧「海軍偏重」の戦線は撤去（2026-08-10 作者裁定）＝陸海の内訳は独立の政治戦線を持たない（軍事費の合計だけが戦線）。
        //   ⚠意図した副作用：軸が1本減る＝目標が動かない幽霊戦線が消え、PoliticalPushBudget=(1/N)^Gate が他軸を薄めなくなる。
        if (!w.CoercionFollowsForce) Front(w.BaseCoercion / Math.Max(1e-9, w.Coef.CoercionMax), Q5(p.CoercionTarget / Math.Max(1e-9, w.Coef.CoercionMax)));
        Front(w.Tariff, Q5(Math.Clamp(Q5(p.TariffTarget) + w.TariffRetaliationBias, w.Coef.TariffFloor, 1)));   // 戦線数の側も施行と同じ式。関税は R3 で段の対象外＝Q5 のまま   // wave2b：施行側（L157相当＝報復偏倚＋関税床込み）と同じ目標式へ。旧は偏倚/床を無視していた
        return n;
    }

    // マンデートゲート（法整備の離散一歩用）：純支持≥0 の一歩は無償で通る。純支持<0 の一歩は政治資本の在庫が代価に届くときだけ通る（裁定S1＝門は在庫1本）。
    // **押し切った一歩は政治資本を燃やす**＝|純支持|×MandateSpendRate×後退倍率を目減り S（MandateSpent）と今期増分（MandateSpentNew＝Politics.Step が正統性から1回減算）へ記帳。
    // 憲法級だけは MandateMargin=0 で硬い純支持>0（純支持0も否決）。決定論：RNGなし（純支持・正統性・支持だけの関数）。
    private static bool MandateGate(World w, PolicyAxis a, bool increasing, int targetLevel, double costMul = 1.0)
    {
        // ★政治ロックの隔離弁を法にも通す（2026-08-04）：旧＝迂回弁が Move（スライダー）にだけあり法には無い非対称。
        //   土地の政治力（LandedPowerCapitalization）で旧支配層が実際に法を止められるようになった結果、
        //   PoliticalLockEnabled=false で隔離したはずの単体テスト（聖職者の世俗化没落）が政治側の理由で赤になった。
        //   ロックの意味論＝「政治的可否と直交な機構の単体検証」＝スライダーと法で同じであるべき。実ゲームは常に true＝挙動不変。
        double net = w.PoliticalLockEnabled ? PolicyPreferences.NetSupport(w, a, increasing) : 1.0;
        // §13：後退のコスト倍率（既定1.0＝恒等）＝押し切りの政治資本が倍率で燃える
        // ★否定形（2026-09-21 ㉖）：旧 `net <= 閾値` は NaN で偽＝否決されず、次の `net < 0` も偽＝**NaN の一歩が無償で施行されていた**
        //   （在庫の門が唯一の門になった世界では、その門ごと素通り）。`!(net > 閾値)` は NaN を否決の側へ倒す。
        //   閾値は −0 か −∞（MandateMargin は 0/+∞・costMul は有限≥1）＝NaN にならない＝有限の net では旧と同じ真偽。
        if (!(net > -MandateMargin(w, a, targetLevel) / costMul)) return false;
        if (net < 0)
        {
            double cost = -net * w.Coef.MandateSpendRate * costMul;   // 反対を押し切った代償（−net=|純支持|）＝正統性の全幅
            if (!PoliticalCapitalAffords(w, cost)) return false;      // ★在庫の門（N2）＝残り a が代価に届くときだけ押し切れる
            Spend(w, cost);
        }
        return true;
    }

    /// <summary>★政治資本の在庫の門（作者裁定N2・2026-09-21）＝残り <c>a ＝ max(0, b − S)</c> がこの一歩の代価に届くか。
    /// ⚠**押し切り（純支持&lt;0）の枝の中でだけ**呼ぶこと。外に置くと代価0の（世論が既に押している）法にまで S≤b を要求し、
    ///   門を通らない <see cref="Politics"/> の内戦講和の譲歩（RevoltConcessionSpend×段数）で S&gt;b になった国が
    ///   **人気のある法すら通せなくなる＝進行不能**（P4の破綻）。
    /// ⚠否定形：NaN が混じれば <c>&lt;=</c> が偽＝門は閉じる側へ倒れる（fail-open にしない）。</summary>
    internal static bool PoliticalCapitalAffords(World w, double cost)
        => w.MandateSpent + cost <= w.PoliticalCapitalMax;

    // ★裁定C2（2026-09-12「法の変更そのものは闘争度に関与しない。実税が生活を苦しくし、結果として不満度を上げて反乱が増える形にする」）：
    //   押し切り遺恨／阻止の不満のラッパ2本は 2026-09-20 の段の点火で削除。税から闘争度への経路は実税→生活水準→不満度（Politics の力学）1本。
    //   ★2026-09-26（棚卸し pol-27）：残っていた §13 後退の怒り（ReversalAnger）と本体 Grievance も削除した＝IrreversibleLaws=1.0（後退は禁止）と
    //     PrivilegeStripMilitancy=0 で**二重に不到達**だった。点火すれば裁定C2 に反し、しなければ死蔵＝どちらに倒しても残す理由が無い。

    /// <summary>法整備の一歩が現在の政治均衡（純支持＋政治資本の在庫）で通るかの照会（UIの法律タブ通知用・裁定2026-07-09）。
    /// MandateGate と同じ判定式だが**副作用なし**＝政治資本を消費しない（実際の施行は Government.Step の通常経路のみ）。
    /// ★2026-08-17：**第4の脚**＝MandateGate・GateInfo・Move と同じ隔離弁を通す。
    /// 照会専用でも式が他の3本と違えば「施行は通るのに照会は通らない」非対称が生まれる（GateInfo で実際に踏んだ罠・同日修正）。
    /// 実ゲームは常に PoliticalLockEnabled=true ＝挙動不変・較正中立。</summary>
    /// <remarks>★N2（2026-09-21）：在庫の門をここにも1行足した。⚠<see cref="GateInfo"/> へ畳むのは**採らない**＝
    /// 畳むと前提法DAG・不可逆も見るようになり点灯の条件が変わる。式は施行側と同じ1本を読む。</remarks>
    public static bool CanStep(World w, PolicyAxis a, bool increasing)
    {
        double net = w.PoliticalLockEnabled ? PolicyPreferences.NetSupport(w, a, increasing) : 1.0;
        if (!(net > -MandateMargin(w, a, NextStage(w, a, increasing)))) return false;
        return !(net < 0) || PoliticalCapitalAffords(w, -net * w.Coef.MandateSpendRate * ReversalCostMul(w, a, Regressive(a, increasing)));
    }

    /// <summary>★(178) 国民皆兵の門＝身分の特権が無いこと＝領主権の残高（<see cref="Privileges.SeigneurialDegree"/>＝農奴解放 なし1／限定0.5／全面0・法から導く＝新しい状態なし）が0。
    /// 作者 2026-10-03「全て推奨でよい。(1)は良いと思う。」で採った推奨①（封建の兵から国民の兵へ＝身分の特権〔農奴制・武士など戦う身分の特権〕がある間は徴兵は領主や共同体を通した選抜まで・
    /// 廃止した国は国民皆兵を目標にできる＝法の上の平等＝等しい義務・国家が母数に直接届く）＝AI の整理。武士の特権は日本の開幕の農奴解放の法「なし」が持つ（world.json＝国名の分岐なし）。
    /// 史実（身分の解放→国民皆兵）＝普 1807 十月勅令→1813/14 一般兵役義務・仏 1789 封建的特権の廃止→1798 ジュルダン法・露 1861 農奴解放令→1874 国民皆兵・墺 1848 農民解放→1868 一般兵役・
    /// オスマン 1839 ギュルハネ勅令→1843 予備軍（1848 籤）・日 1871 廃藩置県と身分の解放→1873 徴兵令。英米は置き換える封建の兵が無く大戦の時だけ徴兵（門は開幕から開いている＝皆兵を止めるのは不安の側）。
    /// 読み手＝<see cref="LawPrereq"/>（皆兵へ登る門）と AI の目標の上限（UtilityAgent.DecideModernLaws）。
    /// ⚠ 門をプレイヤーの法（法律タブ・勢力の要求）の前提にも掛ける形は AI の設計（AI とプレイヤーは同じ関数＝思想⑤）＝作者の承認は未確認（(192) 反証役 vm1#5）。</summary>
    internal static bool UniversalConscriptionOpen(World w) => Privileges.SeigneurialDegree(w) <= 0;

    /// <summary>前提法DAG（1934拡張E4・2026-07-19）：ある法軸をある目標段(0基点)へ上げる前提が満たされているか＋不足理由。
    /// 非循環（前提は必ず「より基礎の別法」を指す・相互ロックしない）。UIの可否表示・施行・バイパス（勢力譲歩）の**単一ソース**。
    /// targetLevel＝目標のenum序数（bool法は1）。低い段への後退は常に可（前提は上げる時だけ）。E5/E6が新法軸の前提をここへ追加する。</summary>
    public static (bool Ok, string Reason) LawPrereq(World w, PolicyAxis a, int targetLevel, Coefficients c)
    {
        // ★段階の前提（2026-08-04・作者指示「参政権含めいくつかの法は1段階ずつしか進まないように（1つ前の段階を前提条件にする）」）。
        //   多段の法は**1つ前の段を踏んでいることを前提**にする＝段の飛び越しを禁じる。
        //   ⚠StepEnum は元々1段ずつ進める（ci+1 を照会する）ので、施行の速さ自体はこれでは変わらない。
        //     変わるのは**遠い段を目標に置けなくなる**こと＝プレイヤーもAIも各段を別々の決断として払う
        //     （UIは遠い段が灰色になり理由が出る／AIの要求も飛び越しを目標にできない）。
        //   （旗 StagePrereq は 2026-09-26 に焼き込み）
        if (StagedAxis(a) && targetLevel > CurrentStage(w, a) + 1)
            return (false, "前段階が必要");
        // ★税・支出・軍事費の段（作者裁定2026-09-12 R1/R2）＝**押せなさの根拠をここへ集める**＝法律タブの可否とエンジンの施行が同じ1本を読む。
        //   新しい定数を1つも置かない（検地台帳・教育法・緊縮の逆進フロア・総動員は全て既存の量）。
        switch (a)
        {
            // ⚠**天井型の前提は登る側にしか掛けない**（2026-09-12 反証）：GateInfo は段の財政軸だけ**下げる側でも**
            //   LawPrereq を照会する（床＝緊縮の逆進フロア・軍事費の最下段を上下同じ1本から出すため）。そこへ天井をそのまま
            //   掛けると「天井より上に居る国が1段も降りられない」＝行政力が落ちた国の所得税・公教育を失った国の教育費が
            //   その段に凍りつき、UIには下げるボタンに「行政力が足りない」という逆の理由が出る（所得税の例は 2026-09-12 に天井ごと廃止＝教育費など残る軸の話）。
            //   現在段以下（＝降りる先）は常に通す＝天井は登れなさであって降りられなさではない。新しい量は置かない（CurrentStage は既存）。
            // ⚠所得税・累進は行政力の腕を持たない（2026-09-12 FIX1 で行政力の天井を廃止）＝弱い行政は「課せない」のではなく
            //   「取り立てた金が国庫に届かない」（Fiscal の汚職漏れ）。議会の噛みつき（PowerShares.ParliamentGrip の分子＝所得税＋累進）を
            //   立法で先に0にしてしまうのが天井の実害だった。★所得税の門は行政力ではなく理由（払えない請求＝下の case IncomeTax・2026-09-30 (127)）。
            case PolicyAxis.LandTax:
                if (targetLevel >= 1 && !w.LandTaxUnlocked && targetLevel > CurrentStage(w, a)) return (false, "検地台帳が前提");   // 測量・登記なしに地租は課せない（土地改革と同文・地租改正1873）
                break;
            case PolicyAxis.IncomeTax:
                // ★所得税の**導入**は払えない請求が起こす（2026-09-30 (127)）＝所得税の無い国（率が0）が段を上げられるのは国家の支払い率 r＜1
                //   （借入と切り下げの後にも払えない請求がある）の時だけ。いったん課した国（率＞0＝査定の仕組みと先例がある）は政治の門だけで上げ下げする。
                //   史実＝所得税の導入は戦費と債務の非常の時：英1799（ピット＝対仏戦争の借入が細った）・普1811/1820（敗戦の賠償）・墺1849（革命とハンガリー・イタリアの戦役）・
                //   米1861（南北戦争）・伊1864（統一の債務）・日1887（軍拡）・仏1914・露1916＝平時に財政が回る国は置かなかった。導入の後は平時にも残り・上がった
                //   （英1842 ピールの再導入〔先例の上〕・普1891 ミーケル・日1899）。
                //   ⚠日付ではなく理由（盤面の r と率）＝AI（教義の目標は GateAllLawTargets がこの門で畳む）とプレイヤー（法律タブの段のボタン）が同じ1本を読む。新しい状態は無い
                //   （導入の印＝今の率＞0＝率そのものが持つ・要求の決着が段0へ戻せば導入前へ戻る＝英1816 の廃止）。
                //   ⚠天井型の前提は登る側にしか掛けない（上の注記）＝払えない請求が消えても課している段はそのまま（下げるのは自由）。
                //   ⚠行政力の門にはしない＝2026-09-12 FIX1（行政の弱さは Fiscal の汚職の漏れが額で表す＝同じ量を率の側にも掛けると二重計上）。否定形＝NaN の r・NaN の率は門を閉じる側。
                if (targetLevel >= 1 && targetLevel > CurrentStage(w, a) && !(w.TaxRate.Value > 0) && !(w.FiscalFundingRatio.Value < 1.0)) return (false, "払えない請求が前提");
                break;
            case PolicyAxis.EducationSpending:
                // 教育費＝公教育の制度が先（学校の無い国に教育予算の段は無い）。Step も EducationLaw が下段のままなら効果を持たない。
                if (targetLevel >= 1 && w.EducationLaw < EducationLaw.Public && targetLevel > CurrentStage(w, a)) return (false, "教育法（公教育以上）が前提");
                break;
            case PolicyAxis.PollTax:
                // 緊縮＝債権者が強いる逆進フロアより下の段は選べない＝「権力を無視して課税できない」の裏返し（課税をやめる自由も債権者が奪う）。
                // ⚠専売は 2026-09-20 に率そのものを廃した（材ごとの ON/OFF だけ）＝この門を持たない。
                // ⚠裁定B（2026-09-12）以後、床を持つのは**この門だけ**（Step は実効値を持ち上げない）。下げる側の照会は GateInfo が段の軸だけ通す。
                if (targetLevel < AusterityStage(w, c, a)) return (false, "緊縮の下限");
                break;
            case PolicyAxis.Coercion:
                // 軍事費（武力スライダーの合計）。最上段＝戦時経済＝総動員中かつ交戦中だけ選べる（R2・M2 2026-09-19＝総動員は解禁であって強制ではない）。最下段より下は存在しない＝「なし」が無い。
                if (targetLevel >= MilitaryLawStages && !WarStageOpen(w)) return (false, "総動員が前提");
                if (targetLevel < 1) return (false, "軍事費に「なし」は無い");
                // ★裁定B（2026-09-12）：戦時の下限ロック（旧＝Step が p.ArmyUpkeepTarget/p.NavyUpkeepTarget を Max で黙って持ち上げていた）を門へ移す。
                //   新しい量も新しい数もゼロ＝同じ c.WartimeUpkeepFloor を同じ現在値に掛けて段へ写すだけ（係数は PowerShares の LockedSpending と共有＝単一ソース）。
                //   ⚠値は1ミリも書き換えない＝「その段には下がれない」。GateInfo が段の財政軸だけ**下げる側でも**ここを照会するので実効。
                if (w.AtWarNow && c.WartimeUpkeepFloor > 0 && targetLevel < MilitaryStage(w.MilitarySpending * c.WartimeUpkeepFloor))
                    return (false, "戦争中は軍を解散できない");
                break;
            case PolicyAxis.SocialSecurity:
                // ★裁定B（2026-09-12）：緊縮の福祉天井（旧＝Step が w.SocialSecurity を毎期 1−緊縮強度 で削る＝法を1ミリも動かさず実効値だけを削る残骸）を門へ移す。
                //   新しい量はゼロ＝同じ 1−AusterityIntensity を段（救貧/労災/失業/年金）へ写すだけ。切り捨て＝天井を跨ぐ段は選べない（段の値が天井を超えると名乗りが嘘になる）。
                //   ⚠天井型の前提は**登る側にしか掛けない**（2026-09-12 反証と同文）＝緊縮は既に制定された福祉法を剥がす力を持たない（剥がれなさは ProtectedLaw が別に持つ）。
                if (targetLevel > (int)Math.Floor(PoliticalQuantities.Clamp01(1.0 - w.AusterityIntensity) * SocialSecurityMaxLevel)
                    && targetLevel > CurrentStage(w, a)) return (false, "緊縮の上限");
                break;
        }
        switch (a)
        {
            case PolicyAxis.Suffrage:
                // 男女普通選挙(Universal=3) ← 女性の権利が選挙権の段。男性普通選挙(AdultMale=2)までは前提なし。（第3段3-6：旧・財産権前提の到達不能枝を焼き込み削除）
                if (targetLevel >= (int)Suffrage.Universal && !w.WomenSuffrage) return (false, "女性の権利（選挙権）が前提");
                break;
            // ★新しい前提（2026-08-04・作者指示「いくつかの法には前提条件を用意する」）。史実の順序に沿う。
            case PolicyAxis.ConscriptionLaw:
                // ★(178) 封建の兵から国民の兵へ（作者 2026-10-03「全て推奨でよい。(1)は良いと思う。」で採った推奨①＝00一覧の軍の裁定の表・AI の整理）：
                //   国民皆兵（国家が適齢の男子の全てに直接届く等しい兵役の義務）は身分の特権がある間は登れない＝領主と共同体を通した選抜（割当て・籤）まで（<see cref="UniversalConscriptionOpen"/>）。
                //   ⚠天井型の前提は登る側にしか掛けない（上の注記）＝既に皆兵の国はそのまま・下げはいつでも可。プレイヤー（法律タブ＝GateInfo）・AI（GateAllLawTargets と施行の StepEnum）・
                //   勢力の要求（Parliament.TargetDemand＝GateInfo・Politics.ApplyDemand＝ここ）が同じ1本。
                if (targetLevel >= (int)ConscriptionLaw.Universal && targetLevel > CurrentStage(w, a) && !UniversalConscriptionOpen(w)) return (false, "農奴解放（全面）が前提");
                break;
            case PolicyAxis.SecretBallot:
                // 秘密投票 ← 選挙があること。投票そのものが無い国で投票の秘密を論じられない（英1872年の秘密投票法は1832/1867の拡大の後）。
                if (targetLevel >= (int)SecretBallotLaw.Secret && w.Suffrage < Suffrage.Census) return (false, "参政権（制限選挙以上）が前提");
                break;
            case PolicyAxis.ColonialSuffrage:
            case PolicyAxis.MigrantNativeSuffrage:
                // 参政権の範囲2法（作者裁定2026-08-16）← 選挙があること。本国に選挙の無い国で植民地/移民の投票を論じられない（秘密投票と同文）。
                if (targetLevel >= 1 && w.Suffrage < Suffrage.Census) return (false, "参政権（制限選挙以上）が前提");
                break;
            case PolicyAxis.SocialSecurity:
                // 社会保障の高い段（失業保険=2以上） ← 官僚制度（資格任用以上）。給付を配るには受給資格を判定できる行政が要る
                // （ビスマルクの疾病/災害/年金保険は帝国の官僚機構の上に載った）。救貧法・労災までは前提なし＝教区や雇用主でも回せる。
                if (targetLevel >= (int)SocialSecurityLaw.Unemployment && w.Bureaucracy < BureaucracyLaw.MeritExam) return (false, "官僚制度（資格任用以上）が前提");
                break;
            case PolicyAxis.Antitrust:
                // 独占禁止 ← 官僚制度（資格任用以上）。カルテルを摘発するには縁故でない執行機関が要る（米ICC1887→シャーマン法1890の順）。
                if (targetLevel >= (int)AntitrustLaw.CartelBan && w.Bureaucracy < BureaucracyLaw.MeritExam) return (false, "官僚制度（資格任用以上）が前提");
                break;
            case PolicyAxis.Nationalization:
            case PolicyAxis.NationalizationAll:
                // 国有化 ← 官僚制度（資格任用以上）。国が現業を経営するには官僚機構が要る（縁故任用の国有化は略奪になる）。
                if (targetLevel >= 1 && w.Bureaucracy < BureaucracyLaw.MeritExam) return (false, "官僚制度（資格任用以上）が前提");
                // ★『全て』（作者裁定2026-08-14 §11-6d）＝5部門以外の全産業の一括国有化（計画経済）はソ連型以外で操作不能。
                //   国家無神論（StateAtheism ゲート）と同型＝政体の属性。UIのグレーアウト・施行（StepBool）・譲歩（StepNationalization）が全てこの1本を読む。
                if (a == PolicyAxis.NationalizationAll && w.Polity != Polity.ProletarianDictatorship) return (false, "プロレタリア独裁のみ");
                break;
            case PolicyAxis.ProgressiveTax:
                // 累進課税 ← 官僚制度（資格任用以上）。累進は所得の把握（申告と査察）を要する＝近代税務行政の産物。
                if (targetLevel >= 1 && w.Bureaucracy < BureaucracyLaw.MeritExam) return (false, "官僚制度（資格任用以上）が前提");
                break;
            case PolicyAxis.LandReform:
                if (targetLevel >= 1 && !w.LandTaxUnlocked) return (false, "検地台帳が前提");   // 土地改革は測量・登記（行政力で解禁）が要る
                break;
            case PolicyAxis.WomenRights:
                // ★女性の権利の梯子（統合法・2026-08-10）。旧「女性参政権法←女性教育権法」の前提は**段の並びそのもの**が担う
                //   （選挙権の段へ登るには必ず教育権の段を踏む＝上の StagePrereq が保証）＝前提条件を1本書く必要が消えた。
                // ★2026-08-04 の前提は残す：男性普通選挙。男に普通選挙の無い国が女に与えた例は史実にない（順序＝男子普選→女性参政権）。
                //   他の同日追加の前提（秘密投票・社会保障・独禁…）と同じく StagePrereq のゲートは外す＝段飛び越し禁止を0に戻しても消えない（既定1.0では恒等）。
                if (targetLevel >= (int)WomenRightsLaw.Suffrage && w.Suffrage < Suffrage.AdultMale) return (false, "男性普通選挙が前提");
                break;
            case PolicyAxis.StateReligion:   // 例外①（2026-07-28）：国家無神論はプロレタリア独裁のみ（§13-7＝法でなく政体の属性。AI/プレイヤー対称＝StepEnumも本DAGを読む）
                if (targetLevel >= (int)StateReligionLaw.StateAtheism && w.Polity != Polity.ProletarianDictatorship) return (false, "プロレタリア独裁のみ");
                break;
        }
        // ★★★政体レバー上限は撤去（作者裁定 2026-08-19「**もはや政体は意味を持たない。絶対王政と立憲君主と共和制は連続的**。
        //   政体ごとの上限などは全て廃止し、逆に**権力円グラフから政体を算出**し、これは**外交パネルでの表示のみ**に使う」）。
        // ⚠旧＝参政権(参加軸)・報道/組合(1−強権軸)の3つの上限。**幕府も絶対王政も参加軸0.00で参政権の上限が0**だったため、
        //   実測で**秩禄処分の1876年に日本の参政権拡大の純支持が+0.01とプラスでも構造で否決**されていた（docs/202608 の日本調査）。
        //   しかも倒幕の行き先（Absolutist）も参加軸0.00＝**明治維新に相当する唯一のイベントが制度を1ミリも動かさない**。
        // ★史実＝この時代のあらゆる専制が制限選挙を上から与えている（日1889/90・普1849三級選挙法・露1906ドゥーマ・土1876/1908・清1909諮議局）。
        // ★★実測（selfplay-batch 10・2026-08-19）＝撤去して**7次元が改善**：法 59.9→65.8／人口 12.9→28.6（日本40→70・清10→40）／
        //   **列強・再現率 25.0→32.5**（★最大の逸脱に直接効いた）／特異度 91.1→92.3／経済 22.5→25.0／領土本国 83.3→84.2。硬性ゲートは緑。
        //   ⚠下がったのは植民地 44.2→40.8 と貿易 1.1→0.0（★元から既知の崩壊水準）。
        // ★★★**政体exact は 41.5→41.6 でほぼ不動**＝この上限は忠実度を1ミリも支えていなかった（作者裁定を数字が裏づけた）。
        return (true, "");
    }

    /// <summary>段階の前提を課す軸（2026-08-04）＝多段で、1段ずつ積み上がるべき制度。
    /// bool法（女性財産権など）と、段を持たない連続スライダー（関税・行政費・金利）は対象外＝段の概念がない。</summary>
    /// <remarks>★段（R1/R2・2026-09-12・2026-09-20 点火）：税・支出・軍事費も段を持つ（<see cref="FiscalStagedAxis"/>）。</remarks>
    private static bool StagedAxis(PolicyAxis a)   // ★段の法は表 LevelLaws の Stage=2（2026-09-28 処理の一元化 段1）
        => LevelLawByAxis.TryGetValue(a, out var law) && law.Stage == 2
        || FiscalStagedAxis(a);   // ★R1/R2（2026-09-12）：税・支出・軍事費も段＝遠い段を目標に置けない（各段を別々の決断として払う）

    /// <summary>その軸の現在段（段階の前提の判定用・2026-08-04）。段を持たない軸は int.MaxValue を返して前提を不発にする。</summary>
    private static int CurrentStage(World w, PolicyAxis a) => a switch
    {
        // ★税・支出・軍事費の段（R1/R2・2026-09-12）＝**実効値の段**を返す（目標ではない＝他の軸と同じ「今どこに立っているか」）。
        PolicyAxis.Coercion => MilitaryStage(w.MilitarySpending),   // 武力スライダー＝軍事費の合計（1..4）
        _ => RateLawByAxis.TryGetValue(a, out var rate) ? TaxStage(a, rate.Get(w))   // ★税・支出の段＝表 RateLaws の実効値の段
           : LevelLawByAxis.TryGetValue(a, out var law) && law.Stage > 0 ? law.Get(w) : int.MaxValue,   // ★段の法＝表 LevelLaws の現在段（Stage>0 の行・2026-09-28 処理の一元化 段1）
    };

    /// <summary>★段になった税の軸を「政府が今その向きの段を目標に置いている」か＝**施行中**の述語（作者裁定2026-09-12 C1の判定座標）。
    /// 危機の要求の「動いたか」だけがこれを読む（<see cref="Politics"/>）。満足の物差し（<see cref="FactionSnapshot.Headroom"/>／
    /// <see cref="PolicyPreferences.LawGap"/>）は**段のまま1本**＝「要求が消える点と不満が消える点が同じ座標」の不変条件は動かさない。
    /// なぜ要るか：enum法は <c>StepEnum</c> が1期1段まるごと動くので翌四半期に段が動いたかを読めるが、段になった税は
    /// <c>Move</c> の指数接近（1期に埋まるのはギャップの2〜18%）で、段の境界を跨ぐには42.5〜57.5%が要る＝**評価の四半期には構造的に1段も動かない**
    /// ＝要求に載せた瞬間「必ず蜂起」になる。政府が段を目標に置き**その一歩が今も通る**なら毎月 <see cref="Fiscal"/> が新しい率で取り立てている＝譲歩は実体を伴って進行中
    /// （通らない目標は <c>Move</c> が1ミリも動かさない＝空手形なので「施行中」と読まない・末尾の <see cref="GateInfo"/> 照会）。
    /// ⚠「目標が動いたか」（差分）ではなく「目標が現在段の向こう側にあるか」（状態）で見る＝新しいタイマーでも待ち行列でもない。
    /// ⚠実効値の差分で見ない理由：<c>Move</c> は目標へ永久に漸近するので、段が変わらなくても実効値は毎期わずかに動き続ける
    /// ＝その向きの要求が恒久的に「譲歩された」と誤読される（差分の閾値を新設するのは新しい数）。
    /// ⚠治安維持力（軍事費）は対象外＝要求にも載せない（<see cref="FactionSnapshot.IsInstantAxis"/>）＝現在位置が別量（BaseCoercion）で毎期揺れる。</summary>
    internal static bool EnactingStage(World w, PolicyAxis a, bool increasing)
    {
        var p = w.Policy;
        int t = RateLawByAxis.TryGetValue(a, out var rate) ? TaxStage(a, rate.Target(p)) : -1;   // 段を持たない軸（表 RateLaws の外）＝述語を持たない（enum法は1期1段で動くので段の差分がそのまま読める）
        if (t < 0) return false;
        int cur = CurrentStage(w, a);
        if (increasing ? t <= cur : t >= cur) return false;
        // ★空手形の封じ（2026-09-12 反証）：目標を置いただけでは「施行中」ではない＝**その一歩が今も通ること**を同じ門（GateInfo）に訊く。
        //   なぜ要るか：Move は純反対の向き（net ≤ −余白）では実効値を1ミリも動かさず、そのとき目標 p.X は書き換わらない
        //   ＝置いた瞬間だけ通った目標が**永久に真**を返し、その軸の危機が恒久的に受諾（Rejections=0 の毎期リセット）へ倒れる。
        //   ★2026-09-18：旧注記は「AI は SelfPlay.GateLaw が毎期畳むので自動で閉じる／プレイヤー国は畳まれない＝等価の破れ」だったが、
        //   その畳み込み（支持不足でも目標＝現行法）自体が P5 の破れ＝**0の吸収状態**として除去された。以後は AI の目標もプレイヤーと同じく残るので、
        //   空手形を閉じているのは**この GateInfo 照会1本だけ**＝両者に同じ規則が掛かる（この行の存在理由がむしろ強まった）。
        //   新しい量も新しいタイマーも足さない（法律タブ・AI・議会・勢力が共有する既存の1本を照会するだけ）。
        //   ⚠訊くのは**次の一歩**（NextStage）＝2段以上離れた目標（勝った運動の n 段譲歩）を段飛ばし禁止（StagePrereq）で誤って否と読まない。
        return GateInfo(w, a, increasing, NextStage(w, a, increasing)).Ok;
    }

    /// <summary>法律タブUI（E4）：一歩の可否・必要な政治資本コスト・不可の理由を1回で返す。副作用なし。
    /// increasing 方向へ一歩上げる前提として、前提DAG（LawPrereq）→純支持ゲート（CanStep）を順に照会。
    /// cost＝押し切りに要する政治資本（純支持が負の分×消費率。正なら0）。reason＝不可/前提不足の説明（可なら空）。</summary>
    public static (bool Ok, double Cost, string Reason) GateInfo(World w, PolicyAxis a, bool increasing, int targetLevel)
    {
        PolicyPreferences.Profile? none = null;
        return GateInfo(w, a, increasing, targetLevel, ref none);
    }

    /// <summary>★立場の土台（<see cref="PolicyPreferences.Build"/>）を呼び手と分け合う読み口（2026-10-01 (146) 計算量の予算・恒等）：
    /// <paramref name="shared"/> が空なら純支持を読むときに1回だけ取って入れ、次の照会はそれを使う＝**間に立場の土台の入力を書かない呼び手だけ**が渡す
    /// （AI の法目標の整形 UtilityAgent.GateAllLawTargets＝法目標〔NationPolicy〕を書くだけ・軍事費の目標の書き込みは最後の照会の後）。</summary>
    internal static (bool Ok, double Cost, string Reason) GateInfo(World w, PolicyAxis a, bool increasing, int targetLevel, ref PolicyPreferences.Profile? shared)
    {
        // ★段（R1/R2・2026-09-12）：段になった財政の軸だけは**下げる側でも**前提を照会する＝床（緊縮の逆進フロア・軍事費の最下段）が
        //   「押せない段」として上下とも同じ1本（LawPrereq）から出る。他の軸は従来どおり上げる時だけ。
        if (increasing || FiscalStagedAxis(a))
        {
            var (pok, preq) = LawPrereq(w, a, targetLevel, w.Coef);
            if (!pok) return (false, 0, preq);
        }
        // 第6段6-7：不可逆の理由をUIへ（施行側StepEnum/StepBoolと同一判定＝表示と物理の一致）。労働時間のみ後退＝増加方向。
        if (BlockedByIrreversibility(w, a, Regressive(a, increasing)))
            return (false, 0, a is PolicyAxis.ShorterHours or PolicyAxis.ChildLabor ? "戦時のみ緩和可" : "後退不能（支持層が確立済み）");
        // ★政治ロックの隔離弁を照会側にも通す（2026-08-17・第3の脚）：MandateGate（施行:303）と Move（スライダー:533）は
        //   PoliticalLockEnabled=false を読むのに、**照会専用のこの関数だけが読んでいなかった**。
        //   AiLawGateUnified（2026-08-16点火）でAIの法目標が GateInfo を通るようになった結果、その非対称が実挙動に出た
        //   ＝施行側は弁で開いているのに、AIが目標を「通らない」と判断して現行法へ畳む→法が永久に動かない
        //   （実測＝聖職者の世俗化テストで日本の教育法が400四半期 Religious のまま・reason「政治資本が届かず否決」net≈−0.8）。
        //   実ゲームは常に true＝挙動不変。弁の意味論（政治的可否と直交な機構の単体検証）を施行・スライダー・照会で揃える。
        double net = w.PoliticalLockEnabled ? PolicyPreferences.NetSupport(w, a, increasing, shared ??= PolicyPreferences.Build(w)) : 1.0;
        double costMul = ReversalCostMul(w, a, Regressive(a, increasing));   // §13：照会も施行と同じ倍率
        double cost = net < 0 ? -net * w.Coef.MandateSpendRate * costMul : 0;   // 上限廃止（2026-07-26ユーザー裁定）：反対が強いほど青天井に高くつく
        bool ok = net > -MandateMargin(w, a, targetLevel) / costMul;
        // ★在庫の門（N2・2026-09-21）＝純支持が負で**残りが代価に届かない**場合（S1 以後、非憲法級の否決はすべてこれ）。
        //   理由は別の文字列にする＝計器（Runner の法の門）が「賛成不足で否決（憲法級の硬い門）」と「残高で否決」を数え分けられる。
        //   ⚠SelfPlay.GateLaw の barred（前提DAG・不可逆）には**入らない**文言＝AIは目標を畳まない（0の吸収状態を作らない・P5）。
        if (ok && net < 0 && !PoliticalCapitalAffords(w, cost)) return (false, cost, ReasonNoCapitalStock);
        return (ok, cost, ok ? "" : ReasonNoMandate);
    }

    /// <summary>否決の理由（法律タブが定型句として扱う2本・作者指示2026-08-22）。★生成側と比較側で文字列が散らないよう**ここが単一ソース**。
    /// ★2026-09-21 ㉖：この理由が出るのは**硬い「純支持&gt;0」の門**だけ（憲法級の段・政治が凍結された世界・純支持が NaN）＝余白 θ は退役し、
    ///   非憲法級の反対は在庫の門（下の <see cref="ReasonNoCapitalStock"/>）が止める。旧文言「政治資本が届かず否決」は政治資本をいくら貯めても
    ///   通らない門を「政治資本の不足」と言っていた＝実際の理由（賛成が反対を上回らない）へ直した。名前（NoMandate＝押し切る委任が無い）は据え置き。</summary>
    public const string ReasonNoMandate = "賛成不足";
    /// <summary>★N2（2026-09-21）在庫の門の否決＝支持では通るが残り a が代価に届かない。法律タブは上と同じく定型句として扱う（必要量はホバーの数字が出す）。</summary>
    public const string ReasonNoCapitalStock = "政治資本の残りが不足";

    // 政治資本の消費を両台帳へ記帳：MandateSpent＝目減り S（Politics.Step が b/MandateRefillQuarters×実行度で線形に回復）／MandateSpentNew＝今期増分
    // （ストック正統性の一回限り減算用＝LegitimacyIsStock有効時の二重計上排除。Politics.Step が読んで毎期ゼロ化）。
    // 上限1.0クランプは廃止（2026-07-26ユーザー裁定）＝政治資本は青天井に積める。代償は正統性減算（Politics側で0クランプ）＝乱発の死のスパイラルは実コストとして残る。
    // ⚠private へ戻した（2026-09-20）：外部の唯一の呼び手だった World.SetStateMonopolyOn は、専売を政治から切り離したので
    //   もう政治資本を払わない。内部（Move と MandateGate）の2本だけが記帳する＝「誰でも政治資本を燃やせる口」を開けたままにしない。
    private static void Spend(World w, double amount)
    {
        w.MandateSpent += amount;
        w.MandateSpentNew += amount;
    }

    // スライダー：目標へ漸進。純支持が負の向きへは**政治資本の在庫で払える間だけ動く**（下の ★裁定S1）＝法整備のマンデートゲートと同型。
    // 旧（〜2026-09-21）＝変更方向の純支持が −θ を超えるときだけ動く方向ロック（反対勢力が支持＋余白を上回る向きには動かせない）。
    // 純支持が正でも、反対勢力の力が大きいほど遅い（OpposingPower で律速）。＝政治の均衡がスライダーの到達域を縛る。
    // スライダーの政治資本消費は**実際に動いた量に比例**（|Δ|/spendScale・上限1＝0..1スライダーの全走破が法1段と等価）：
    // 微調整（関税の報復偏倚ジッタ＝目標が毎期揺れる）が一歩満額を毎期燃やして正統性を壊す実測バグの対処。
    // mandate=false は**機械導出の従属目標**（治安維持力←軍規模。CoercionFollowsForce で毎期再導出＝政府の意図した押し切りでない）：
    // 余白も消費も適用せず従来の硬い純支持>0ゲート＝実測で余白が初期治安維持力916→軍規模相当250への滑落を解禁し
    // 専制の保持力を崩壊させた（露が1811年に革命→立憲化＝宗教教育停滞の動学が消える）ため、余白は意図した政策押し切りに限る。
    // ★裁定S1（2026-09-21・㉖で焼き込み）：**方向ロックは無くなる**（θ=+∞＝下の比較が偽）。代わりに反対の向きへ這い進む
    //   1ミリごとに在庫から払い、残りが代価に届かなくなった瞬間に止まる＝ロックが「向きの禁止」から「値段」へ変わる。
    //   ⚠実挙動の最大の変化はここ：実測（隔離計器・seed1・604四半期）でスライダーの押しは 4,788回 → 102,167回（21.3倍）・代価計 0.08 → 54.83
    //     ＝1国期あたり 0.00142（回復 b/40×実行度 ≈0.00441 の32%）＝在庫が常に空に近い状態で運転される。
    //   ⚠**旧の硬いゲートへは戻さない**：税4軸と軍事費は 2026-09-20 に段になり**施行が Move 側**なので、ここだけ硬く戻すと
    //     「税の段が反対を押し切って上がる」経路が消える＝現行より後退する。門は世界に1つ（S1 の字義）＝法もスライダーも在庫で払う。
    //   ⚠法は同じ四半期の先に課金される（Government.Step は法→スライダーの順）＝スライダーの這い進みが法を飢えさせることはない。
    private static double Move(double cur, double target, Coefficients c, World w, PolicyAxis a, double spendScale = 1.0, bool mandate = true, bool fullPush = false)
    {
        bool milProbe = Instruments.On && a == PolicyAxis.Coercion && spendScale == 1.0;   // 計器（読むだけ）：軍事費の合計の施行の一歩（Step の milTotal）だけを写す
        if (Math.Abs(target - cur) < 1e-12) { if (milProbe) Instruments.MilitaryMove(w, cur, target, double.NaN, double.NaN, double.NaN, 0, default, false); return cur; }
        bool increasing = target > cur;
        // ★純支持と反対の力は同じ瞬間の立場の土台を読む＝Build は1回（2026-10-01 (146) 計算量の予算・恒等）：この2つの間は何も書かない（余白の照会だけ）。
        //   旧＝それぞれが Build（POP の数回の走査）を取った＝全長の1種で約24万回（Build の20%）。
        var e = PolicyPreferences.Build(w);
        double net = w.PoliticalLockEnabled ? PolicyPreferences.NetSupport(w, a, increasing, e) : 1.0;   // ロック無効（財政機構の単体テスト）＝ゲート/消費とも不発の番人値
        if (!(net > -(mandate ? MandateMargin(w, a, -1) : 0))) { if (milProbe) Instruments.MilitaryMove(w, cur, target, net, double.NaN, double.NaN, 1, e, increasing); return cur; }   // ★否定形（2026-09-21 ㉖）＝NaN の押しは動かさない（旧 `<=` は NaN で素通りし、下の `net < 0` も偽＝無償で動いた）。純反対＝その向きへロック（mandate=false の機械追従だけ。政策の押しは MandateMargin=+∞ で倒れず下の在庫で払う）。スライダーは段の概念がない＝targetLevel=−1（憲法級の段判定に掛からない）
        double resist = PolicyPreferences.OpposingPower(w, a, increasing, e);
        // P1-C 予算闘争（憲法紛争1862型）：非協力的な議会（CoopParliamentCrown<0）×高い統制力＝予算軸（所得税）のスライダーが
        // 議会の拒否で動きにくい。免責＝協力度は紛争記憶（MandateSpent）の減衰と議席の構成の変化で回復（2026-09-26 回5-1 Q16＝旧「与党交代」）。既定0で恒等。
        // 機能2-1（2026-07-28）：予算協賛の対象を所得税のみ→全税軸へ（旗 ParliamentBudgetScope は 2026-09-26 に焼き込み）。議会の同意なしに予算（歳入構成）は動かない
        // ＝憲法紛争1862の一般化。機能1-像1（絶対王政は同意コスト0）は非議会政体で ParliamentControl≈0＝この項が消えることで相対的に成立（新規実装ゼロ）。
        if (w.Coef.ParliamentBudgetVeto > 0 && (a == PolicyAxis.IncomeTax
            || a is PolicyAxis.PollTax or PolicyAxis.LandTax or PolicyAxis.ProgressiveTax))   // [PORT:P6] 予算闘争→法ゲートの抵抗への加算（★専売は Move を持たない＝2026-09-20 に腕を削除。門は World.SetStateMonopolyOn 側の GateInfo）
            resist += w.Coef.ParliamentBudgetVeto * Math.Max(0, w.ParliamentControl) * Math.Max(0, -w.CoopParliamentCrown);
        double speed = c.PolicyBaseSpeed / (1.0 + c.PolicyResistanceWeight * resist) * (mandate && !fullPush ? w.PoliticalPushBudget : 1.0);   // 第6段6-1：政治の押しのみ戦線数で正規化（機械追従＝武力統合の治安維持力は対象外）。★(270) fullPush＝戦時段の解禁の間の軍事費の一歩は戦線数で薄めない（Step の milTotal だけが真を渡す）
        // 政体フレーバー レバー2（2026-07-24）：勅令は速い＝非選挙政体（絶対/専制/幕府/軍政）はスライダー歩幅×(1+これ)（議会審議を経ない＝
        // 農奴解放勅令・太政官布告）。独立乗算・0で恒等。
        // ⚠代償の在処（裁定C2・2026-09-12）：旧＝押し切り遺恨 PushedLawGrievance が「速いが恨まれる」を価格付けしていた。遺恨は 2026-09-20 に削除＝
        //   残る代償は押し切りの政治資本（下の Spend＝正統性の減算）だけ＝専制は相対的に速くなる。新しい代償は発明しない（規律）＝
        //   計測で「アジア過近代化」側が悪化したら AutocraticPolicyAgility の値そのものが裁定対象。
        // ※StepEnum/StepBool（1段/期に量子化された法軸）には掛けない＝1.5歩が表現できず端数ストック新設は過剰。速さの実体は連続スライダー側。
        if (c.AutocraticPolicyAgility > 0 && !(w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic))
            speed *= 1.0 + c.AutocraticPolicyAgility;
        double next = cur + speed * (target - cur);
        if (mandate && net < 0)   // 反対を押し切って動いた＝動いた量の分だけ政治資本を燃やす
        {
            double moved = Math.Min(1.0, Math.Abs(next - cur) / spendScale);
            double cost = -net * w.Coef.MandateSpendRate * moved;
            if (!PoliticalCapitalAffords(w, cost)) { if (milProbe) Instruments.MilitaryMove(w, cur, target, net, resist, speed, 2, e, increasing); return cur; }   // ★在庫の門（N2）＝払えない押しはスライダーも1ミリも動かない
            Spend(w, cost);
        }
        if (milProbe) Instruments.MilitaryMove(w, cur, target, net, resist, speed, mandate && net < 0 ? 4 : 3, e, increasing);
        return next;
    }

    /// <summary>第6段6-7（2026-07-28）法の不可逆性の唯一実装（中核ルールdoc §6）：「支持層を作る法」は平時に戻らない
    /// （参政権=有権者・社会保険の制度=受給者と官僚機構・財産権=法主体・農奴解放=自作農）。水準（スライダー）は自由＝制度と水準で割れる史実。
    /// 労働時間・児童労働は不可逆だが**戦時のみ緩和可**（英1915軍需品法・1934国民労働秩序法）。報道・国教は可逆（検閲は復活する）。
    /// 断裂の窓（6-8・RuptureWindowQuarters>0）＝革命鎮圧・内戦制圧・弾圧直後だけ全面解除（仏1850選挙法・ストルイピン1907・普三級選挙法）。
    /// regressive＝その軸の「支持層を壊す向き」（参政権等は減少・労働時間は増加＝軸ごとの向きは呼び手が判定）。</summary>
    /// <summary>その一歩が軸の「後退の向き」か＝労働時間（無制限へ）と児童労働（合法化へ）だけ**増加**が後退・他は減少（唯一の定義）。
    /// ★2026-09-18：GateInfo（2箇所）・StepEnum・StepBool に同じ三項が4本写経されていたのを畳んだ（新しい規則も新しい数もゼロ・厳密恒等）。
    /// bool法は StepBool が increasing=target を渡す（cur≠target が保証済み＝false→true が増加）。</summary>
    internal static bool Regressive(PolicyAxis a, bool increasing) =>
        a is PolicyAxis.ShorterHours or PolicyAxis.ChildLabor ? increasing : !increasing;

    internal static bool BlockedByIrreversibility(World w, PolicyAxis a, bool regressive)
    {
        if (!regressive) return false;
        var c = w.Coef;
        // 政4（既存・恒久）：農奴解放・土地改革＝再農奴化の封じ（旗 IrreversibleEmancipation は 2026-09-26 に焼き込み・窓too＝再隷農化した国は存在しない）。
        if (a is PolicyAxis.SerfEmancipation or PolicyAxis.LandReform) return true;
        if (c.IrreversibleLaws <= 0) return false;
        if (w.RuptureWindowQuarters > 0) return false;   // 断裂の窓＝反動の後退はここだけ
        if (!ProtectedLaw(w, a)) return false;           // 報道・国教・税・関税等＝可逆
        // ★支配度doc §13（作者追記裁定2026-09-06・器2026-09-07）：IrreversibleLaws は 0..1 の連続。1.0＝現行の bool 禁止（厳密恒等）。
        //   <1 では禁止せず、後退は同じ純支持ゲート（MandateGate＝余白÷倍率・押し切りの政治資本×倍率）で通る（既得層の怒りは置かない＝裁定C2・pol-27）。
        return c.IrreversibleLaws >= 1.0;
    }

    /// <summary>支持層を作る法（§13）＝参政権系（統合法・範囲2法）・社会保障は制度として守られる。労働時間・児童労働は平時のみ（戦時は緩和可＝英1915軍需品法）。</summary>
    internal static bool ProtectedLaw(World w, PolicyAxis a) => a switch
    {
        PolicyAxis.Suffrage or PolicyAxis.WomenRights or PolicyAxis.SocialSecurity or PolicyAxis.ColonialSuffrage or PolicyAxis.MigrantNativeSuffrage => true,
        PolicyAxis.ShorterHours or PolicyAxis.ChildLabor => !AtWar(w),
        _ => false,
    };

    /// <summary>§13 後退のコスト倍率＝1/(1−IrreversibleLaws)。1.0（禁止）は BlockedByIrreversibility が先に止める・0 で倍率1（純支持だけ）・断裂の窓と非保護軸は1。</summary>
    internal static double ReversalCostMul(World w, PolicyAxis a, bool regressive)
    {
        var c = w.Coef;
        if (!regressive || !(c.IrreversibleLaws > 0) || c.IrreversibleLaws >= 1.0 || w.RuptureWindowQuarters > 0 || !ProtectedLaw(w, a)) return 1.0;
        if (a is PolicyAxis.SerfEmancipation or PolicyAxis.LandReform) return 1.0;
        return 1.0 / (1.0 - c.IrreversibleLaws);
    }

    internal static bool AtWar(World w)
    {
        if (w.Diplomacy is not { } dip || w.SelfPower is not { } sp) return false;
        foreach (var war in dip.Wars) if (war.A == sp || war.B == sp) return true;
        return false;
    }

    // 順序付き法整備：目標へ一歩だけ。MandateGate（純支持≥0 は無償・負は政治資本の在庫で払える時だけ・憲法級は純支持＞0）が許すときのみ進む。
    // ★段の序数で1本（2026-09-28 処理の一元化 段1＝旧 StepEnum と StepBool の2本。bool の法は 0/1 の段＝cur≠target なら target が増加の向き・前提は1段目・門は 1/0＝旧 StepBool と同じ引数）。
    private static int StepLevel(int ci, int ti, World w, PolicyAxis a)
    {
        if (ci == ti) return ci;
        bool increasing = ti > ci;
        // 第6段6-7：不可逆判定を BlockedByIrreversibility へ一元化。軸の「後退の向き」＝Regressive 1本（2026-09-18に4箇所の写経を畳んだ）。
        //   bool の法の後退（true→false＝制度の撤回）も同じ判定（撤回例は史実に存在しない制度が対象・ChildLabor だけ軸の増＝合法化が後退向き＝Regressive が持つ規約）。
        bool regressive = Regressive(a, increasing);
        if (BlockedByIrreversibility(w, a, regressive)) return ci;
        // 前提法DAG（E4）：一段上げる先(ci+1)の前提が未達なら据え置き（段階スキップ禁止＝1段ずつ・前提の連鎖を一歩ごと再確認）。
        //   bool の法も同じ（E4/レビュー2026-07-20＝旧 StepBool が LawPrereq を読まず、GateInfo 経由の人間プレイヤーにだけ前提が効いた非対称の是正）。
        if (increasing && !LawPrereq(w, a, ci + 1, w.Coef).Ok) return ci;
        if (MandateGate(w, a, increasing, ci + (increasing ? 1 : -1), ReversalCostMul(w, a, regressive)))
            return ci + (increasing ? 1 : -1);
        return ci;   // 力の均衡（＋余白）が許さない＝据え置き（例：参政権はブルジョワが制限選挙で止める）
    }
}
