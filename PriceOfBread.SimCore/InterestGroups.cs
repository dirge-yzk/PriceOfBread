namespace PriceOfBread.SimCore;

/// <summary>
/// 利益集団の力（§9-E 拡張）。各階級の力 ＝ 富のシェア × 政治的発言権（参政権で決まる）＋軍の政治的優位の補正（旧支配層＝将校貴族）。
/// 力ある集団は自らを害する政策に抵抗する（<see cref="Government"/> のスライダー律速）。
/// 旧支配層（貴族・聖職＝Closed）は参政権に関わらず力を持ち、近代エリート（ブルジョワ・専門職）は
/// 制限選挙で参入、大衆（労働者・農民）は成人男性/普通選挙で力を得る。＝参政権がマスターキー。
/// </summary>
public static class InterestGroups
{
    /// <summary>旧支配層 / 近代エリート / 大衆 の3ブロックの力（合計≒1 付近）。</summary>
    public static (double Traditional, double Modern, double Mass) Blocs(World w)
    {
        var (t, m, s, _, _) = BlocsSplit(w, null);   // petty=null＝従来の3束分類そのもの（プチブル利益束のリファクタ・算術の順序は不変＝ビット恒等）
        return (t, m, s);
    }

    /// <summary>プチブル利益束（作者承認2026-08-15・休眠 PettyInterestGate=0）：<see cref="Blocs"/> と同一の per-pop 重み計算で、
    /// petty（小営業＝職人+商人の集合。null=旧3束分類）だけを第4束の2成分へ振り替えた力。成分は旧束別
    /// （商人=旧近代由来 PettyModern／職人=旧大衆由来 PettyMass）に分けて持つ＝非対象軸のフォールバック集計
    /// （PolicyPreferences）が「旧束の値」へ厳密に戻せる形。petty=null では分類分岐が旧3束と同一＝ビット恒等
    /// （**束の判定切替が唯一の分岐点**・設計書§2）。読み手＝PolicyPreferences.Build のみ（Blocs の他の読者＝教義/開戦意欲等は3束のまま＝対象を広げない）。</summary>
    internal static (double Traditional, double Modern, double Mass, double PettyModern, double PettyMass) BlocsSplit(World w, HashSet<Pop>? petty)
    {
        // ★土地＝旧支配層の政治力（2026-08-04・統一根）。1783年の現実＝土地が国富の過半で、それを握るのが貴族
        //   （改革法1832は地主支配への闘争・ユンカーは1918年まで・仏は革命前夜）。旧＝富の基盤が流動資産（Savings）のみで、
        //   貴族3000/人 vs 資本家3200/人・頭数20倍差から旧支配層の政治力が**英3%・仏21%**に消えていた（blocprobe実測）
        //   ＝法の門番（伝統教義のみ発動）が全員素通り＝全国1783年に近代法＝工業化の全球収束、の起点。
        //   土地の価値＝**地代（既存の物理フロー LandRent）の資本化**＝新しい状態を置かない。Closed階級のみ＝
        //   資本家の工場は流動資産側が既に表す。⚠地代は月次でゼロ化→再集計される生きたフロー＝土地改革・農奴解放で
        //   地代が削られれば政治力も一緒に痩せる（貴族の没落が法から創発する）。LandedPowerCapitalization=0 で厳密恒等。
        double LandedWealth(Pop p) => Math.Max(0, p.Savings)
            + (p.Closed && w.Coef.LandedPowerCapitalization > 0 ? w.Coef.LandedPowerCapitalization * Math.Max(0, p.LandRent) : 0);
        // 参政権の範囲2法（作者裁定2026-08-16）：政治的国民への算入率でpopを加重（植民地格の州＝植民地参政権・本国州の非中核分＝移民原住民参政権）。
        // ループは AllPops と同じ走査順（w.Regions→r.Pops・Seceded スキップ）の入れ子＝州の格が読める形。
        // SuffrageScopeGate=0 では乗算パス自体に入らない＝旧挙動と厳密恒等（加算順も同一＝ビット恒等）。⚠既定は1.0（点火2026-09-06）＝この乗算は下の SuffrageScopeVoice=0 が閉じている。
        // ★J-001の範囲（2026-09-04・運転規則§9で作者裁定を差し戻し）：参政権の範囲が縛るのは**本国議会の代表と課税への同意**（Fiscal/Government）まで。
        //   発言権（この関数＝利益集団の重み）は票の前提にしない＝票の無い層にも声はあった（チャーティスト1839/反穀物法同盟1838/1848年）。
        //   ここだけ別の腕 SuffrageScopeVoice（既定0）＝主フラグ点火時もこの乗算は走らない。両方>0 で従来のV6挙動。
        bool scopeOn = w.Coef.SuffrageScopeGate > 0 && w.Coef.SuffrageScopeVoice > 0;
        double totalWealth = 0, totalPop = 0;
        foreach (var r in w.Regions)
        {
            if (r.Seceded) continue;
            foreach (var p in r.Pops)
            {
                double lw = LandedWealth(p), sz = p.Size;
                if (scopeOn) { double inc = SuffrageInclusion(w, r, p); lw *= inc; sz *= inc; }
                totalWealth += lw; totalPop += sz;
            }
        }

        // 実効参政権＝法律（World.Suffrage）。政体が含意する参政権は遷移/初期化の一回 floor-raise で法へ畳み込み済（倍率排除②）＝毎期の max(法,政体) 写像を廃止。
        var eff = w.Suffrage;
        // 政11：力の按分基準。InterestGroupHeadcountWeight=0（既定）は純富シェア（従来恒等）。>0で人口シェアを β 加重合成
        // base=(1−β)×富シェア+β×人口シェア＝普通選挙下で貧しい多数派に頭数由来の力を与える（プロ独裁で労働者＞資本家）。
        // 参政権のない大衆は PoliticalWeight（FranchiseVoice）が0で pw=0＝頭数を増やしても議会外では効かない＝FranchiseVoice を持つ層にのみ実効。
        double beta = w.Coef.InterestGroupHeadcountWeight;
        // 閉鎖階級の政治力の乗数＝宮廷の権力＋軍の政治的優位（固定1.3を排除）×P7①制度疲労（硬直した国ほど旧支配層が強い・×1.0で恒等）。
        // ②軍の政治的優位（2026-08-18・親軍法の撤去）：旧＝親軍法ONのときだけ MilitarismClout×軍事費 を足す門つきだった。
        // ⚠**給与では買えない**（西1931アサーニャ改革は満額俸給付き退役で最大のクーデタを招いた）＝将校団の政治力は
        // 「軍の圧力（組織の大きさ＋解隊の脅威＋粛清/人事介入の記憶＋対外屈辱）×（1−文民制度の弁）」から出る（Finer/Huntington）。
        // ループ不変（World の純関数）＝1回だけ計算して pop ごとに掛ける。
        double closedMul = (1.0 + w.Coef.MilitarismClout * PoliticalQuantities.MilitaryPoliticalRatio(w)) * (1.0 + w.Coef.DecayReformResistWeight * w.DecayForReaders);
        double trad = 0, modern = 0, mass = 0, pettyM = 0, pettyS = 0;
        foreach (var r in w.Regions)
        {
            if (r.Seceded) continue;
            foreach (var p in r.Pops)
            {
                double lw = LandedWealth(p), sz = p.Size;
                if (scopeOn) { double inc = SuffrageInclusion(w, r, p); lw *= inc; sz *= inc; }   // 算入率＝分子側も同じ加重（分母と同時に痩せる＝比の整合）
                double wealthShare = totalWealth > 1e-9 ? lw / totalWealth : 0;
                double share = beta > 0 && totalPop > 1e-9
                    ? (1.0 - beta) * wealthShare + beta * (sz / totalPop)
                    : wealthShare;
                double pw = share * PoliticalWeight(p, eff, closedMul, w.Coef);
                if (p.Closed) trad += pw;
                else if (petty != null && petty.Contains(p)) { if (p.IsModernElite) pettyM += pw; else pettyS += pw; }   // 第4束「小営業」（petty=null＝この枝は不走行＝従来分類）
                else if (p.IsModernElite) modern += pw;
                else mass += pw;
            }
        }
        return (trad, modern, mass, pettyM, pettyS);
    }

    /// <summary>小営業（職人＋商人）popの集合（プチブル利益束・作者承認2026-08-15）。判定は構造マーカー＝
    /// <see cref="Region.ArtisanPopIndex"/>/<see cref="Region.MerchantPopIndex"/>（自営＝OwnerPopIndex==LaborPopIndex の所得を持つ階級）
    /// ＝固有名詞・階級名の文字列判定なし（設計書§3）。**PettyInterestGate>0 の枝からのみ呼ぶ**（呼ばないことが休眠の恒等＝SuffrageInclusion と同じ作法）。
    /// ⚠所属判定（Contains）のみに使い**列挙しない**（HashSet の列挙順＝参照ハッシュ順は非決定＝決定論を破る）。</summary>
    public static HashSet<Pop> PettyPops(World w)
    {
        var set = new HashSet<Pop>();
        foreach (var r in w.Regions)
        {
            if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count) set.Add(r.Pops[r.MerchantPopIndex]);
            if (r.ArtisanPopIndex >= 0 && r.ArtisanPopIndex < r.Pops.Count) set.Add(r.Pops[r.ArtisanPopIndex]);
        }
        return set;
    }

    /// <summary>参政権の範囲2法（作者裁定2026-08-16）＝popの「政治的国民への算入率」0..1。**SuffrageScopeGate>0 の枝からのみ呼ぶ**（呼ばないことが休眠の恒等）。
    /// ・植民地格の州（<see cref="RegionTier.Colonial"/>）＝植民地参政権：なし=0／自国民のみ=CoreEthnicShare／全面=1（英モデル／コロン限定／同化主義）。
    /// ・本国州（Core/Integrated）＝中核分は常に算入・非中核分(1−CoreEthnicShare)は移民原住民参政権「あり」でのみ算入。
    /// 両法が最大（全面＋あり）で全pop=1.0＝ゲート点火前と同じ集計＝連続性。読み手＝Blocs（発言権）・Fiscal consent（課税への同意）。</summary>
    public static double SuffrageInclusion(World w, Region r, Pop p)
    {
        double core = p.CoreEthnicShare;
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

    /// <summary>利益集団の政治力：閉鎖階級（貴族・聖職）は身分の宮廷権力 closedMul（票でなく富・席次・軍の政治的優位＝BlocsSplit が1回計算）、近代/大衆は実効参政権の票の発言権 <see cref="FranchiseVoice"/>。基準0.5で正規化＝既定等倍。</summary>
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
        double fem = s >= Suffrage.Universal ? 1.0 : 1.0 - p.FemaleShare;
        return gate * fem;
    }
}
