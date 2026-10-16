namespace PriceOfBread.SimCore;

/// <summary>
/// 利益集団の力（§9-E 拡張）。各階級の力 ＝ 富のシェア × 政治的発言権（参政権で決まる）＋軍の政治的優位の補正（旧支配層＝将校貴族）。
/// 力ある集団は自らを害する政策に抵抗する（<see cref="Government"/> のスライダー律速）。
/// 旧支配層（貴族・聖職＝Closed）は参政権に関わらず力を持ち、近代エリート（ブルジョワ・専門職）は
/// 制限選挙で参入、大衆（労働者・農民）は成人男性/普通選挙で力を得る。＝参政権がマスターキー。
/// </summary>
public static class InterestGroups
{
    /// <summary>旧支配層 / 近代エリート / 大衆 の3ブロックの力（合計≒1 付近）。
    /// 〔2026-10-11 (271)〕プチブル利益束（第4束「小営業」・旧 PettyInterestGate＝既定 0 の枝）と分割版 BlocsSplit・PettyPops を削除＝3束のこの1本に戻した。</summary>
    public static (double Traditional, double Modern, double Mass) Blocs(World w)
    {
        // ★土地＝旧支配層の政治力（2026-08-04・統一根）。1783年の現実＝土地が国富の過半で、それを握るのが貴族
        //   （改革法1832は地主支配への闘争・ユンカーは1918年まで・仏は革命前夜）。旧＝富の基盤が流動資産（Savings）のみで、
        //   貴族3000/人 vs 資本家3200/人・頭数20倍差から旧支配層の政治力が**英3%・仏21%**に消えていた（blocprobe実測）
        //   ＝法の門番（伝統教義のみ発動）が全員素通り＝全国1783年に近代法＝工業化の全球収束、の起点。
        //   土地の価値＝**地代（既存の物理フロー LandRent）の資本化**＝新しい状態を置かない。Closed階級のみ＝
        //   資本家の工場は流動資産側が既に表す。⚠地代は月次でゼロ化→再集計される生きたフロー＝土地改革・農奴解放で
        //   地代が削られれば政治力も一緒に痩せる（貴族の没落が法から創発する）。LandedPowerCapitalization=0 で厳密恒等。
        double LandedWealth(Pop p) => LandedWealthOf(w, p);
        // 参政権の範囲2法（作者裁定2026-08-16）：政治的国民への算入率でpopを加重（植民地格の州＝植民地参政権・本国州の非中核分＝移民原住民参政権）。
        // ループは AllPops と同じ走査順（w.Regions→r.Pops・Seceded スキップ）の入れ子＝州の格が読める形。
        // ★J-001の範囲（2026-09-04・運転規則§9で作者裁定を差し戻し）：参政権の範囲が縛るのは**本国議会の代表と課税への同意**（Fiscal/Government）まで。
        //   発言権（この関数＝利益集団の重み）は票の前提にしない＝票の無い層にも声はあった（チャーティスト1839/反穀物法同盟1838/1848年）
        //   ＝算入率はここでは掛けない（測定専用の腕 SuffrageScopeVoice は 2026-09-26 に削除＝dorm-49）。
        //   ★第 1 走査（富と人口の分母）は POP だけに依る＝窓の中（<see cref="PopWindow"/>）は使い回す。第 2 走査（重み）は法（参政権）・軍の優位に依る＝毎回求める。
        var (totalWealth, totalPop) = w.PopSums is { } win ? win.LandedTotals(w) : ComputeLandedTotals(w);

        // 実効参政権＝法律（World.Suffrage）。政体が含意する参政権は遷移/初期化の一回 floor-raise で法へ畳み込み済（倍率排除②）＝毎期の max(法,政体) 写像を廃止。
        var eff = w.Suffrage;
        // 政11：力の按分基準。InterestGroupHeadcountWeight=0 なら純富シェア（従来恒等）。>0で人口シェアを β 加重合成（★ 2026-07-07 有効化＝既定0.25）
        // base=(1−β)×富シェア+β×人口シェア＝普通選挙下で貧しい多数派に頭数由来の力を与える（プロ独裁で労働者＞資本家）。
        // 参政権のない大衆は PoliticalWeight（FranchiseVoice）が0で pw=0＝頭数を増やしても議会外では効かない＝FranchiseVoice を持つ層にのみ実効。
        double beta = w.Coef.InterestGroupHeadcountWeight;
        // 閉鎖階級の政治力の乗数＝宮廷の権力＋軍の政治的優位（固定1.3を排除）（★(373) P7①制度疲労の乗数は物理削除）。
        // ②軍の政治的優位（2026-08-18・親軍法の撤去）：旧＝親軍法ONのときだけ MilitarismClout×軍事費 を足す門つきだった。
        // ⚠**給与では買えない**（西1931アサーニャ改革は満額俸給付き退役で最大のクーデタを招いた）＝将校団の政治力は
        // 「軍の圧力（組織の大きさ＋解隊の脅威＋粛清/人事介入の記憶＋対外屈辱）×（1−文民制度の弁）」から出る（Finer/Huntington）。
        // ループ不変（World の純関数）＝1回だけ計算して pop ごとに掛ける。
        double closedMul = (1.0 + w.Coef.MilitarismClout * PoliticalQuantities.MilitaryPoliticalRatio(w));
        double trad = 0, modern = 0, mass = 0;
        foreach (var (_, p) in w.AllPopsWithRegion())
        {
            double lw = LandedWealth(p), sz = p.Size;
            double wealthShare = totalWealth > 1e-9 ? lw / totalWealth : 0;
            double share = beta > 0 && totalPop > 1e-9
                ? (1.0 - beta) * wealthShare + beta * (sz / totalPop)
                : wealthShare;
            double pw = share * PoliticalWeight(p, eff, closedMul, w.Coef);
            if (p.Closed) trad += pw;
            else if (p.IsModernElite) modern += pw;
            else mass += pw;
        }
        return (trad, modern, mass);
    }

    /// <summary>1 つの POP の政治力の素の富＝貯蓄＋（閉鎖階級なら）地代の資本化。<see cref="Blocs"/> の第 1・第 2 走査が読む唯一の定義。</summary>
    private static double LandedWealthOf(World w, Pop p)
        => Math.Max(0, p.Savings.Value)
            + (p.Closed && w.Coef.LandedPowerCapitalization > 0 ? w.Coef.LandedPowerCapitalization * Math.Max(0, p.LandRentPerMonth.Value) : 0);

    /// <summary><see cref="Blocs"/> の第 1 走査＝国家会計の集合（<see cref="World.AllPopsWithRegion"/>）の富と人口の分母（同じ順の和）。窓の中の使い回しの求め直し（<see cref="PopWindow"/>）も同じ本体を通る。</summary>
    internal static (double Wealth, double Pop) ComputeLandedTotals(World w)
    {
        double totalWealth = 0, totalPop = 0;
        foreach (var (_, p) in w.AllPopsWithRegion())
        {
            totalWealth += LandedWealthOf(w, p); totalPop += p.Size;
        }
        return (totalWealth, totalPop);
    }

    /// <summary>参政権の範囲2法（作者裁定2026-08-16）＝popの「政治的国民への算入率」0..1。**SuffrageScopeGate>0 の枝からのみ呼ぶ**（呼ばないことが休眠の恒等）。
    /// ・植民地格の州（<see cref="RegionTier.Colonial"/>）＝植民地参政権：なし=0／自国民のみ=CoreEthnicShare／全面=1（英モデル／コロン限定／同化主義）。
    /// ・本国州（Core/Integrated）＝中核分は常に算入・非中核分(1−CoreEthnicShare)は移民原住民参政権「あり」でのみ算入。
    /// 両法が最大（全面＋あり）で全pop=1.0＝ゲート点火前と同じ集計＝連続性。読み手＝Blocs（発言権）・Fiscal consent（課税への同意）。</summary>
    public static double SuffrageInclusion(World w, Region r, Pop p)
    {
        double core = p.CoreEthnicShare.Value;
        if (!(core >= 0 && core <= 1)) core = 1.0;   // NaN/域外は中立（全算入＝点火前と同じ）へ倒す（否定形＝Clampは NaN を消さない）
        if (r.Tier == RegionTier.Colonial)
            return w.ColonialSuffrage switch
            {
                ColonialSuffrageLaw.Full => 1.0,
                ColonialSuffrageLaw.OwnNationals => core,
                _ => 0.0,
            };
        return w.MigrantNativeSuffrage == MigrantNativeSuffrageLaw.Granted ? 1.0 : core;
    }

    /// <summary>利益集団の政治力：閉鎖階級（貴族・聖職）は身分の宮廷権力 closedMul（票でなく富・席次・軍の政治的優位＝<see cref="Blocs"/> が1回計算・旧 BlocsSplit は 2026-10-11 (271) に削除）、近代/大衆は実効参政権の票の発言権 <see cref="FranchiseVoice"/>。基準0.5で正規化＝既定等倍。</summary>
    private static double PoliticalWeight(Pop p, Suffrage s, double closedMul, Coefficients c)
        => p.Closed ? closedMul
                    : FranchiseVoice(p, s, c) / 0.5;   // 近代/大衆＝票（女性比で欠ける・普通選挙で倍増）。基準0.5で正規化

    /// <summary>実効参政権 s の下での pop の票の発言権（票の有無・倍率排除②の単一ソース）。
    /// 閉鎖階級は身分として常に発言権／近代エリートは制限選挙(Census)以上＋未満は富の clout で <see cref="Coefficients.ModernEliteSubCensusVoice"/>／大衆は男性普通選挙(AdultMale)以上。
    /// 女性は普通選挙(Universal)まで投票できない＝票は女性比ぶん欠ける（普通選挙で倍増＝地殻変動）。
    /// Politics の voiceless・Ideology の国民集計重み・Parliament の議席・InterestGroups の力 が全てこの一関数から導かれる。</summary>
    public static double FranchiseVoice(Pop p, Suffrage s, Coefficients c)
    {
        double gate = p.Closed ? 1.0
                    : p.IsModernElite ? (s >= Suffrage.Census ? 1.0 : c.ModernEliteSubCensusVoice)
                    : (s >= Suffrage.AdultMale ? 1.0 : 0.0);
        // ★年齢の層（2026-09-30 段4e 後半）：投票年齢（21歳以上＝男子普通選挙の史実の年齢）のうちの男。旧＝成人（16歳以上）の男の割合。
        //   ⚠子供を票に数えない直し（設計書 §1-5 d）は読み手18か所の意味が2つ（成人のうちの割合／頭数）に割れる＝1本の正規化で [0,1] を守れない＝利害の回へ申し送り。
        double a21 = s >= Suffrage.Universal ? 0 : p.AgeShare(21, double.PositiveInfinity);
        double fem = s >= Suffrage.Universal ? 1.0 : a21 > 1e-300 ? 1.0 - p.AgeShare(21, double.PositiveInfinity, 1) / a21 : 0.5;
        return gate * fem;
    }
}
