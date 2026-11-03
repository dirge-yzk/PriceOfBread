namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (395) 州の分離の内訳の表 secession_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsBur.cs／InstrumentsCapacity.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 目的＝(391) の読む線〈英の人口の倍率 0.72→0.42 の帰属＝中核ベンガル・内陸ベンガル・マドラスが 10/10 種で 1788〜1808 年に分離独立した。落とした先が正統性の床の往復か・緩和か・保持力の式かは、
//   州ごとの闘争度・分離の圧力・保持力の内訳の列が無いと割れない〉への答え。CheckSecession の起点（毎四半期の離脱圧力と保持力と支配度の積分）を、州ごとに 1 行で読む。
// 取り方＝本体の CheckSecession が支配度を積分した直後に 1 行（<c>if (Instruments.On) Instruments.SecObserve(…)</c>）で呼ぶ。本体のローカル変数（姿勢・保持力・共有分母・不穏の件数・州ごとの（引き留め, 圧力））は本体が使った値そのまま、
//   圧力の因数（闘争度・文化の距離・民族の少数派・国民意識・参政権の算入・大衆の飽和）は <see cref="SecRebuild"/> で同じ式・同じ順で組み直して、組み直した圧力と本体の圧力の差を列で申告する（`pressureRebuildGapIndex`＝0 でなければ写しが本体とずれた）。
// 行＝国×四半期×州（首都と活きていない州を除く＝CheckSecession が走る州）。挙げた国は毎四半期・他の全ての国は年末。1 試合 約 数十MB。
// 列（単位は接尾辞：Index＝無次元の量・Share＝0〜1・K＝千人・Count＝件数・Flag＝0/1）：
//   国の列（その国の全ての州の行に同じ値）：legitimacyIndex＝正統性／totalPopK＝国の総人口／coerceIndex＝呼び手が渡した軍・警察の保持力（人あたり強度）／holdBaseIndex＝投射と共有の前の中央の保持力
//     （正統性÷(1＋総人口×SecessionHoldPerPop)＋SecessionHoldCoercion×SecessionThreshold×coerce÷(coerce＋基準人口)）の組み直し／holdBaseRebuildGapIndex＝組み直しと本体の hold×共有分母の差（相対・0 に近いはず）／
//     stanceIndex＝国の関税の姿勢（−1 自由貿易..+1 保護）／sharedDivIndex＝共有分母（1＋k×(不穏の件数−1)）／restiveCount＝閾値を越えた運動の数（反乱国の戦線を足した後）。
//   州の列：tier／popK＝州の全 POP／crowdK＝州の大衆（閉鎖階級と兵士を除く）／crowdFactorShare＝大衆の飽和 N÷(N＋N0)／militancyMeanIndex＝州の平均闘争度（人口加重・閉鎖階級と兵士を除く）／
//     cultureDistanceIndex＝文化と宗教の距離（民族の寄せを含む最終値）／coreShareMeanShare＝州の平均中核度／consciousnessMeanIndex＝州の国民意識の平均／suffrageInclusionShare＝参政権の算入率／
//     tariffInterestIndex＝州の関税利害（−1..+1）／tariffGapIndex＝|姿勢−州の関税利害|／tariffTermIndex＝関税の項（乖離×闘争度×圧力の尺度×(1＋文化の重み×距離)）／ethnicTermIndex＝民族の加算の項／
//     consciousnessFactorShare・suffrageFactorShare＝国民意識の割引・参政権の割引（掛ける因数）／pressureIndex＝本体の離脱圧力／pressureRebuiltIndex＝組み直した圧力／pressureRebuildGapIndex＝組み直しと本体の差（相対）／
//     effHoldBeforeShareIndex＝その州に届く保持力（投射済み・共有分母の前）／holdShareIndex＝共有分母で割った保持力（支配度の積分に使った引き留め）／marginIndex＝holdShareIndex−pressureIndex（支配度の増分は marginIndex×DominanceSpeed）／
//     dominanceShare＝積分の後の支配度／goFlag＝支配度が 0 になった州（分離独立の戦争へ入る）／capitalDistanceKm＝首都（国の座標）からの大圏距離（座標未登録は nan）。
// ⚠限界（計器が自分で申告する）：
//   ①圧力の因数は組み直した値＝本体が因数を取り直す式を変えたら組み直しがずれる（pressureRebuildGapIndex で見える・番人が 0 を見る）。②共有分母を掛ける前の保持力は pressureIndex の隣の列＝holdShareIndex の分母は国で 1 つ。
//   ③行の値は CheckSecession を呼んだ時点（四半期の政治の段）の値＝行を書く四半期の末より前の瞬間（その後の移民・人口の段で州の POP が動いた後の値ではない）。④1 四半期に CheckSecession が複数回走れば最後の 1 回の行だけ。
//   ⑤支配度の積分の前の値は列に無い（前の四半期の行の dominanceShare を読み手が使う・新しい州は前の行が無い）。⑥capitalDistanceKm は国の座標と州の座標の大圏距離＝投射の入力であって、投射そのもの（距離減衰）の率ではない。
public static partial class Instruments
{
    internal const string SecessionHeaderText = "quarter\tyear\tnation\tregion\ttier\t"
        + "legitimacyIndex\ttotalPopK\tcoerceIndex\tholdBaseIndex\tholdBaseRebuildGapIndex\tstanceIndex\tsharedDivIndex\trestiveCount\t"
        + "popK\tcrowdK\tcrowdFactorShare\tmilitancyMeanIndex\tcultureDistanceIndex\tcoreShareMeanShare\tconsciousnessMeanIndex\tsuffrageInclusionShare\t"
        + "tariffInterestIndex\ttariffGapIndex\ttariffTermIndex\tethnicTermIndex\tconsciousnessFactorShare\tsuffrageFactorShare\t"
        + "pressureIndex\tpressureRebuiltIndex\tpressureRebuildGapIndex\teffHoldBeforeShareIndex\tholdShareIndex\tmarginIndex\tdominanceShare\tgoFlag\tcapitalDistanceKm";

    /// <summary>州の離脱圧力を組み直した結果（<see cref="SecRebuild"/>）。番人（テスト）が世界から直に呼べるよう値の組で返す。</summary>
    internal readonly record struct SecParts(double Pressure, double TariffTerm, double EthnicTerm, double ConsciousnessFactor, double SuffrageFactor, double CrowdFactor, double CrowdK,
        double CultureDistance, double Militancy, double CoreShare, double Consciousness, double Inclusion);

    /// <summary>Politics.CheckSecession の州ごとの圧力の式を、同じ式・同じ順で組み直す（読むだけ）。cap＝首都の州（Regions[0]）・stance＝国の関税の姿勢。
    /// 本体が圧力の因数を足す・順を変えたらここが追いつかない＝<c>pressureRebuildGapIndex</c> が 0 でなくなる。</summary>
    internal static SecParts SecRebuild(World w, Coefficients c, Region r, Region cap, double stance)
    {
        double cultDist = ((r.Culture.Length > 0 && cap.Culture.Length > 0 && r.Culture != cap.Culture) ? 0.5 : 0)
                        + ((r.Religion.Length > 0 && cap.Religion.Length > 0 && r.Religion != cap.Religion) ? 0.5 : 0);
        double core = Politics.AvgRegionCoreShare(r);
        if (c.CultureDistanceFromEthnic > 0)
            cultDist = PoliticalQuantities.Toward(cultDist, 1.0 - core, c.CultureDistanceFromEthnic);
        double mil = Politics.AvgRegionMilitancy(r);
        double tariffTerm = Math.Abs(stance - r.TariffInterest) * mil * c.SecessionPressureScale * (1 + c.CultureSecessionWeight * cultDist);
        double pressure = tariffTerm, ethnic = 0;
        if (c.SecessionCultureAdditive > 0 && w.EthnicEnabled)
        {
            ethnic = c.SecessionCultureAdditive * c.MinorityMilitancy * cultDist * (1.0 - core) * mil;
            pressure += ethnic;
        }
        double consF = 1.0, nc = Politics.AvgRegionConsciousness(r);
        if (c.SecessionConsciousnessWeight > 0)
        {
            consF = Math.Max(0, 1.0 - c.SecessionConsciousnessWeight * nc);
            pressure *= consF;
        }
        double incl = PoliticalQuantities.Clamp01(Politics.RegionAvgSuffrageInclusion(w, r));
        double suffF = 1.0 - incl;
        pressure *= suffF;
        double crowd = Politics.RegionCrowd(r);
        double crowdF = crowd > 0 ? crowd / (crowd + c.ColonyPromotionMinPop) : 0.0;
        pressure *= crowdF;
        return new SecParts(pressure, tariffTerm, ethnic, consF, suffF, crowdF, crowd, cultDist, mil, core, nc, incl);
    }

    /// <summary>CheckSecession が支配度を積分した直後に呼ぶ（Instruments.On のときだけ）。行は Quarter の SecFold が書く。
    /// hold＝共有分母で割った後の国の保持力（本体の変数そのまま）・margins＝州ごとの（共有分母の前の引き留め, 本体の圧力）。</summary>
    internal static void SecObserve(World w, Coefficients c, double coerce, double stance, double hold, double sharedDiv, int restive, List<(Region R, double Hold, double Pressure)> margins)
    {
        var st = S(w);
        var rows = new List<string>(margins.Count);
        var cap = w.Regions[0];
        double holdBase = w.Legitimacy / (1 + w.TotalPopulation * c.SecessionHoldPerPop)
                        + c.SecessionHoldCoercion * c.SecessionThreshold * (coerce / (coerce + c.PoliticalReferencePop));
        double holdBaseObserved = hold * sharedDiv;
        string head = string.Join('\t', F(w.Legitimacy), F(w.TotalPopulation), F(coerce), F(holdBase), F(Rel(holdBase, holdBaseObserved)), F(stance), F(sharedDiv),
            restive.ToString(System.Globalization.CultureInfo.InvariantCulture));
        foreach (var (r, effBefore, pressure) in margins)
        {
            var p = SecRebuild(w, c, r, cap, stance);
            double pop = 0; foreach (var pp in r.Pops) pop += pp.Size;
            double holdShare = effBefore / sharedDiv;
            double km = double.NaN;
            var rc = Places.Coord(r.Name);
            if (!(rc.Lat == 0 && rc.Lon == 0)) km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
            rows.Add(string.Join('\t', r.Name, r.Tier.ToString(), head,
                F(pop), F(p.CrowdK), F(p.CrowdFactor), F(p.Militancy), F(p.CultureDistance), F(p.CoreShare), F(p.Consciousness), F(p.Inclusion),
                F(r.TariffInterest), F(Math.Abs(stance - r.TariffInterest)), F(p.TariffTerm), F(p.EthnicTerm), F(p.ConsciousnessFactor), F(p.SuffrageFactor),
                F(pressure), F(p.Pressure), F(Rel(p.Pressure, pressure)), F(effBefore), F(holdShare), F(holdShare - pressure), F(r.Dominance), r.Dominance > 0 ? "0" : "1", F(km)));
        }
        st.SecRows = rows;
    }

    /// <summary>番人（テスト）が CheckSecession を直に踏んだ後の行（Quarter の前・SecFold が消す前）を読む口。</summary>
    internal static List<string>? SecRowsPending(World w) => S(w).SecRows;

    /// <summary>相対の差 (a−b)÷max(|b|, 1e-12)。両方 0 なら 0。</summary>
    static double Rel(double a, double b) => a == b ? 0.0 : (a - b) / Math.Max(Math.Abs(b), 1e-12);

    static void SecFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        var rows = st.SecRows;
        st.SecRows = null;
        if (rows is null || !(picked || yearEnd)) return;
        if (!(w.TotalPopulation > 0)) return;   // 死んだ国（人口 0）は行を出さない
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Sec ??= Open("secession", seed);
        if (!sk.SecHeader) { sk.Sec.WriteLine(SecessionHeaderText); sk.SecHeader = true; }
        string nation = NameOf(w), year = I(w.Year), qs = I(q);
        foreach (var row in rows)
        {
            // 行は region・tier から始まる＝quarter／year／nation を前に足して列の順を見出しに合わせる
            sk.Sec.WriteLine(qs + "\t" + year + "\t" + nation + "\t" + row);
        }
    }
}
