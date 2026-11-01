namespace PriceOfBread.SimCore;

/// <summary>
/// 技術（§2.8）＋教育（§A-1）。月次で識字を伸ばし革新能力を再計算、四半期に技術水準を連続的に積む（<see cref="GrowTech"/>）。
/// 革新≠採用：自由＋公教育のみ世界の技術水準天井へ、それ以外は採用天井で頭打ち。Freedom＝Politics.FreedomFromLaws（参政権＋報道法）の導出量。
/// </summary>
public static class TechEducation
{
    /// <summary>女性の識字の比＝FemaleLitFloor から 1 へ賃金保持（連続）で補間（教育権の枝は呼び手が別に掛ける＝形が違う）。GrowLiteracy の母の識字の比と Simulation.DemographicStep の出生の女性識字が共有する 1 本。</summary>
    internal static double MotherLiteracyRatio(Coefficients c, double womenWageRetention) => c.FemaleLitFloor + (1.0 - c.FemaleLitFloor) * womenWageRetention;

    // 識字の伸び速度（教育法別）＝買えた教育を天井に漸近する月次レート
    private const double LiteracyRateNone = 0.0001;        // 学校なし＝ほぼ伸びない（教育財の供給も無い）
    private const double LiteracyRateReligious = 0.004;    // 宗教学校＝伸びる（ただし天井＝買えた教育）
    private const double LiteracyRatePublic = 0.005;       // 公教育
    private const double LiteracyRateCompulsory = 0.007;   // 義務教育＝下層まで拡大
    private const double CensorshipPressFactor = 0.75;     // 検閲下の技術（吸収/革新）デバフ（自由=1.0・国家統制=0.5に対する中間）

    /// <summary>吸収能力＝識字×世俗水路（Elite）×報道の自由。**技術と資本で同じ写像**（2026-08-02に共有化）。
    /// コピーにも読める技師と自由な情報流通が要る、という同一の理屈を2箇所に書き分けない。</summary>
    internal static double AbsorptionOf(World w, Coefficients c)
    {
        double pressAbs = PressFactor(w.Censorship);
        // ★(383) 作者 2026-10-16「行政効率→廃止」：穴③（2026-08-31）の行政の脚〔×(1−AbsorptionAdminWeight×(1−行政効率))〕は物理削除した（行政の質は既に経済側〔行政費の充足・汚職漏れ〕で効く＝吸収能力へ重ねて掛けるのは二重計上）。
        double breadth = PoliticalQuantities.Clamp01(w.Elite / c.EliteForFullTech);
        return breadth * pressAbs;
    }

    /// <summary>
    /// 月次：識字の伸び（§A-1）＋ エリート水準の再計算（教育2段階：識字→エリート）。
    /// 教育＝**財**：教育者POP（宗教＝聖職者／公教育・義務＝教師）が生産し、POPが市場で買う（EconomyStep）。
    /// 識字は**買えた教育（EducationFill）を天井として**伸びる＝消費が識字を生む。
    /// 聖職者は少数＝供給が乏しく下層に行き渡らない／貧しいPOPは買えない＝識字の天井が需給から創発する。
    /// 教育財の無い小シナリオは Fill=1.0（既定）＝従来挙動。
    /// </summary>
    public static void GrowLiteracy(World w, Coefficients c)
    {
        // 教育のコストは**教育財の市場**が直接表現する（授業料＝pop負担／義務教育＝国庫負担）。
        // 旧来の「教育費×充足率」の速度乗数は廃止＝教育費スライダーは義務教育の財源にのみ効く。
        // 倍率排除④：児童労働→識字を**時間配分から創発**。働く子は学校へ行けない＝児童労働率が高いほど識字が遅い。
        // 旧 法の2値倍率（ChildLaborAllowed?1:1+penalty）を、LaborSupply.ChildLaborRate（家計困窮×合法度×就学）の per-pop 値へ置換。
        // 較正harness（学校なし・許可）では率=1→litFactor=1.0＝旧「許可」に恒等一致／禁止で率=0→1+penalty＝旧「禁止」。
        // 開始前の助走（<see cref="World.WarmupFrozen"/>・2026-08-10）は識字を**伸ばさない**＝就学は時間の経過そのもの
        //（暦を止めたまま10年回すと「10年ぶんの就学が時間経過なしに起きる」）。
        // ★ただし下の Elite の再計算は助走中も必ず通す＝あれは蓄積ではなく**現在の識字と教育者構成からの導出**であり、
        //   止めると Elite=0 のまま GrowTech の入口ガードに弾かれ、技術の助走（WarmupTech）が丸ごと不発になる（実測）。
        double baseSpeed = w.WarmupFrozen ? 0 : LiteracyRate(w.EducationLaw);
        // 母の識字の比（家庭で伝わる割合 τ）＝人口動態の女性の識字の式と同じ（Simulation.DemographicStep の femaleLit÷平均識字）。
        double motherLit = MotherLiteracyRatio(c, w.WomenWageRetention);
        if (w.WomenEducationLaw) motherLit = Math.Min(1.0, motherLit * (1.0 + c.WomenEduLitBonus));
        if (baseSpeed > 0)
            foreach (var r in w.Regions)
            {
                foreach (var p in r.Pops)
                {
                    if (p.IsCriminal) continue;   // ★(366) 犯罪者は教育を買っても識字へ効かせない（教育の供給を吸うだけ＝設計 doc §4）。犯罪者の POP が無い世界では偽＝恒等
                    if (p.Literacy < p.EducationFill)
                    {
                        double litFactor = 1.0 + c.ChildLaborLiteracyPenalty * (1.0 - LaborSupply.ChildLaborRate(p, r, c, p.SupplyFill));   // 児童労働率0＝学校へ（旧 禁止＝1+penalty）／率1＝全員労働（旧 許可＝1.0）
                        p.Literacy += baseSpeed * litFactor * (p.EducationFill - p.Literacy);   // 買えた教育の水準へ漸近（買えない者は伸びない）
                    }
                    // ★識字の下がる側＝世代の流れ（2026-09-30 段4e 後半 4e-3・設計書 §5-2・作者裁定 Q6＝推奨）：識字は一生もの＝大人は字を忘れない。
                    //   変わるのは20歳になって大人に入る若者が持ち込む値だけ＝伝わった識字 x_in＝max(学校＝教育の充足, 家庭＝母の識字の比 τ×大人の識字)。
                    //   τ＝女性の識字の比（人口動態の出生率の女性の識字の式と同じ＝FemaleLitFloor＋(1−Floor)×賃金の保持・女性教育権で×(1＋WomenEduLitBonus)・1が上）。
                    //   x_in は識字を超えない（教育の充足＜識字・τ≦1）＝支えの無い枝で識字が上がることはない（上限の切り詰めでなく式の形）。
                    //   旧＝月0.002×(老年比＋子供比＝在庫の割合)×(識字−教育の充足)（LiteracyGenerationalDecay・LiteracyDecayIsGenerational）＝支えの無い識字の実効の半減期 約65〜95年。
                    //   新＝置き換わり率×(1−τ)＝τ＝0.6 の国で約50年（家庭で覚える識字＝スウェーデンの家庭の読み・プロテスタントの聖書の読み）。伸びる枝（教育法の月率）は触らない（二重を作らない＝思想⑨）。
                    else if (p.Literacy > p.EducationFill)
                        p.Literacy = p.GenerationalMix(p.Literacy, Math.Max(p.EducationFill, motherLit * p.Literacy), 1);
                }
            }

        // 教育2段階：識字 → エリート＝**平均識字 × 世俗水路**。世俗水路は法のswitchでなく
        // **誰が実際に教育を生産しているか**（教師＝世俗1.0／聖職者等＝法の乗数）から創発する。
        // ※旧定義（知識専門職の人口シェア×倍率）は、職業を給与均衡（枠なし）にした際に鶏卵で死んだ：
        //   技師の雇用は工業需要→工業は技術→技術はエリート→エリートは技師シェア、と一周して起動装置がない。
        //   識字は教育の購入から独立に立ち上がるため、この定義は起動に他者を必要としない。
        // 教育市場の無い小世界は従来式（平均識字×法乗数）で挙動を温存。
        w.Elite = EmergentElite(w) ?? PoliticalQuantities.AvgLiteracy(w) * EliteMultiplier(w.EducationLaw);
        // ★本国だけの同じ式（2026-08-26）。制度の閾値が読む＝植民地の非識字人口で本国の制度が止まらない。
        //   植民地格の州が無ければ AllPops と同じ集合＝厳密に同値。
        w.EliteCore = EmergentElite(w, coreOnly: true) ?? PoliticalQuantities.AvgLiteracy(w, coreOnly: true) * EliteMultiplier(w.EducationLaw);
    }

    /// <summary>
    /// 四半期：技術を**連続的に**伸ばす（§2.8・カード廃止＝社会の連続創発）。2つの機構：
    /// ①**採用（後追い）**＝世界の技術水準へギャップ比例で寄る（**遅れているほど速い＝後発のブースト**）。
    ///   暗黙の世界水準(AdoptionCeiling)までは厳しくエリート律速（孤立した乏しい社会＝失敗型を温存＝較正）、
    ///   それを超える後追いは緩いエリート要件（`EliteForCatchup`＝コピーは発明より易しい）。
    /// ②**革新（世界の技術水準押し上げ）**＝自由＋公教育のみ。逓減（`InnovationScale`＝高いほど伸びにくい＝先端の張り付き防止）。
    /// → 先端は固定天井に飽和せず伸び続け、後発は世界の技術水準の一定割合まで素早く追い上げる。プレイヤーの離散選択は制度の施行（<see cref="Institutions"/>）。
    /// </summary>
    public static void GrowTech(World w, Coefficients c)
    {
        if (!(w.Elite > 0)) return;   // 教育が無ければ伸びない（否定形＝NaN も不走行側へ）。旧 TechPower（Elite の写し）は2026-09-11に削除

        // ① 採用。基礎＝暗黙の世界水準(AdoptionCeiling)・厳しくエリート律速。多国では世界先端の一定割合まで後追い目標が伸びる
        //    （超過分は緩いエリート要件＝コピーは発明より易しい）。単一国は WorldTechLevel=0 なので後追い目標＝基礎（＝旧来）。
        //    後追いで届くのは先端×CatchupFraction まで＝**永遠の二番手**（§2.8 革新≠採用：コピーで先端には並べない。
        //    並ぶには自由＋公教育で自ら革新するしかない）。※教育が万国で機能するようになった（教師崩壊の修正）際、
        //    後追いが先端へ完全収束し国の差別化が消えたため、コピーの天井を明示した。
        double baseTarget = c.AdoptionCeiling * PoliticalQuantities.Clamp01(w.Elite / c.EliteForFullTech);
        // 後発国のキャッチアップ（最終調整2026-06-29）：近代化国（FrontierCapable＝自由＋公教育＝吸収能力あり）は前線の高い比率(CatchupFractionAdvanced)まで・速い速度で追い上げる。
        // 非近代化（検閲/宗教教育の巨人＝清/印）は従来の CatchupFraction 0.5 で頭打ち＝置いていかれる。両 Advanced 係数を基礎値と同値にすれば旧挙動に恒等。
        bool absorptive = FrontierCapable(w, c);
        double catchFrac = absorptive ? c.CatchupFractionAdvanced.Value : c.CatchupFraction.Value;
        double adoptRate = absorptive ? c.AdoptionRateAdvanced : c.AdoptionRate;
        if (c.CatchupAbsorptionBlend > 0)
        {
            // 収束是正（2026-07-10）：資格国の天井ジャンプ（0.5→0.97）を吸収能力でスケール＝**資格×吸収の積**。
            // 吸収能力＝識字×世俗水路（Elite）×報道の自由（革新項と同じ写像）＝コピーにも読める技師と自由な情報流通が要る。
            // 非資格国（宗教教育）は旧0.5天井のまま（初版の「非資格も吸収分持ち上げ」は宗教教育の日本が公教育の英を抜く逆転を実測＝資格側だけ連続化）。
            // 公教育化しても識字低×検閲の大国（清・印）は天井が中間に留まり1914年に技術2前後（史実の後置）・英独仏は吸収≈1で従来どおり＝全球収束の是正。
            double absorption = AbsorptionOf(w, c);
            double a = absorptive ? 1.0 + (absorption - 1.0) * c.CatchupAbsorptionBlend : 0.0;   // blend=0で二値（旧恒等）
            catchFrac = c.CatchupFraction.Value + (c.CatchupFractionAdvanced.Value - c.CatchupFraction.Value) * a;
            adoptRate = c.AdoptionRate + (c.AdoptionRateAdvanced - c.AdoptionRate) * a;
        }
        double adoptionCeil = Math.Max(c.AdoptionCeiling, w.WorldTechLevel * catchFrac);
        double catchupTarget = baseTarget + Math.Max(0, adoptionCeil - c.AdoptionCeiling) * PoliticalQuantities.Clamp01(w.Elite / c.EliteForCatchup);
        if (catchupTarget > baseTarget + 1e-9)
        {
            // 多国：世界の技術水準へギャップ比例で後追い（遅れているほど速い＝後発のブースト）。近代化国はより高い天井・速い速度（Gerschenkron）
            if (w.TechLevel < catchupTarget)
                w.TechLevel += adoptRate * (catchupTarget - w.TechLevel);
        }
        else if (w.TechLevel < baseTarget)
        {
            // 単一国（または先端と同水準）：基礎まで旧来の線形（挙動・較正を温存）
            w.TechLevel += Math.Min(c.TechGrowthRate * w.Elite, baseTarget - w.TechLevel);
        }

        // ③ 革新（世界の技術水準押し上げ）：自由＋公教育のみ。逓減で張り付きを防ぐ（上限なし）。特許制度で加速。
        // 報道の自由が革新の自由な交換を可能にする＝検閲は革新を殺す（§2.8）。国家報道のみ＝ほぼ革新不能。
        if (absorptive)   // ＝FrontierCapable（その入力は①採用の間に変わらない）
        {
            double press = PressFactor(w.Censorship);   // 検閲のデバフ上限−50%（国家統制でも技術速度は半減まで）。初期が国家統制の国を序盤から壊滅させない
            // 特許＝発見の表の制度の行の2値（2026-10-01 (152)・設計 §1A-6）＝見つけた国は革新×PatentInnovationBonus
            //   （旧＝中核エリートの傾斜 Discovery.Unlock〔0.35〜0.45＝2026-08-31 段2〕＝世界のどこでも同じ量の閾値）。
            double patentMul = w.PatentLaw ? c.PatentInnovationBonus : 1.0;
            double innov = c.InnovationRate * w.Elite * press * patentMul
                         / (1.0 + w.TechLevel / c.InnovationScale);
            w.TechLevel += innov;
            // （研究の分野時計 World.DiscoveryClocks と按分の器 DiscoveryFieldSplit は 2026-09-26 に削除＝書くだけで読み手が無かった・x-03）
        }

        w.TechLevel = Math.Min(w.TechLevel, c.FrontierCeiling);   // 安全上限（逓減で実際は届かない）
    }

    private static double LiteracyRate(EducationLaw law) => law switch
    {
        EducationLaw.None => LiteracyRateNone,
        EducationLaw.Religious => LiteracyRateReligious,
        EducationLaw.Public => LiteracyRatePublic,
        EducationLaw.Compulsory => LiteracyRateCompulsory,
        _ => 0.0,
    };

    // 報道の自由の乗数（吸収と革新で同じ写像＝2箇所に書き分けない）。国家統制=0.5＝技術速度は半減まで
    private static double PressFactor(CensorshipLaw law) => law switch { CensorshipLaw.FreePress => 1.0, CensorshipLaw.Censorship => CensorshipPressFactor, _ => 0.5 };

    /// <summary>
    /// エリートの創発（§A-1 創発化）：平均識字 × 世俗水路。世俗水路＝教育の産出のうち教師（Region.TeacherPopIndex＝世俗）が
    /// 担う割合（聖職者等の授業は識字を出すが世俗の専門知へ通じにくい＝法の乗数で減衰）。
    /// 法はswitchとしてではなく「誰が教壇に立つか」（教育生産者の切替＋教師階級の盛衰）を通して効く。
    /// 教育の産出が無い世界（教育財の無い小シナリオ）は null＝従来式（法の乗数）へ。
    /// </summary>
    private static double? EmergentElite(World w, bool coreOnly = false)
    {
        double secular = 0, total = 0;
        foreach (var r in w.Regions)
        {
            if (coreOnly && r.Tier == RegionTier.Colonial) continue;   // ★本国のみ（2026-08-26）
            foreach (var p in r.Producers)
                if (p.GoodId == Catalog.EducationId)
                {
                    total += p.Output;
                    if (p.LaborPopIndex >= 0 && p.LaborPopIndex == r.TeacherPopIndex)   // 世俗の教師（公教育）が担い手＝役務インデックスで判定（旧 ClassId==7 直書きは pop構成変更で静かに壊れる）
                        secular += p.Output;
                }
        }
        if (total <= 1e-9) return null;
        // ★段6（2026-09-08・doc §6の潜在バグ）：非世俗の担い手（聖職者等）の水路に掛ける乗数は**1.0で頭打ち**にする。
        //   旧＝義務教育（乗数1.2）で channel = 1.2 − 0.2×世俗シェア＝**教会が教えるほど世俗エリートが増える**倒錯
        //   （世俗の水路が100%のとき 1.0、0%のとき 1.2）。1.2 は「識字そのものが厚くなる」ボーナスであって、
        //   非世俗の授業が世俗の専門知へ**通じやすくなる**理由にはならない。⚠現行4法すべてでビット恒等
        //   （担い手は州ごとに聖職者か教師の二値＝Simulation.cs の月次切替なので、乗数1.2が掛かる項 (total−secular) は
        //   義務教育では必ず0。0.0/0.1/1.0 の3法は Min が素通り）＝挙動が変わるのは担い手が混在する異常状態のみ。
        double channel = (secular + (total - secular) * Math.Min(1.0, EliteMultiplier(w.EducationLaw))) / total;
        // ★引数の渡し忘れ（wave2b G11#4）：coreOnly を AvgLiteracy へ転送していなかった＝EliteCore(coreOnly:true) 経由でも
        //   植民地格の州の識字が混ざる（本国のみに絞る2026-08-26の意図が半分しか効かない）。植民地格の州を持たない国では厳密同値。
        return PoliticalQuantities.AvgLiteracy(w, coreOnly) * channel;
    }

    // 宗教学校はエリート（専門職・技師・教師）が出にくい＝低い革新能力（§A-1）
    // ※教育財の無い小世界の従来式（平均識字×法乗数）と、EmergentElite の非世俗水路（聖職者等の授業）の乗数に使う
    private static double EliteMultiplier(EducationLaw law) => law switch
    {
        EducationLaw.None => 0.0,
        EducationLaw.Religious => 0.1,       // 識字はあるが世俗の水路が開かない
        EducationLaw.Public => 1.0,          // 世俗化＝エリートが出る
        EducationLaw.Compulsory => 1.2,
        _ => 0.0,
    };

    // 世界の技術水準解禁：自由が閾値以上（または#22＝高エリート技術力で代替＝権威主義的近代化。(383) で行政効率の連言は物理削除） かつ 世俗の水路（公教育以上）
    private static bool FrontierCapable(World w, Coefficients c) =>
        (w.Freedom >= c.FrontierFreedomThreshold
         || (c.InnovationEliteSubstitute > 0 && w.Elite >= c.EliteForFullTech)) &&
        w.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory;

}
