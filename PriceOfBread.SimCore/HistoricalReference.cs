namespace PriceOfBread.SimCore;

public sealed record HistoricalEntry(
    string Nation,
    string Capital,
    Polity[] Exact,
    Polity[] Near,
    Suffrage[]? SuffrageOk = null,
    EducationLaw[]? EducationOk = null,
    ReformLevel[]? SerfOk = null,
    ReformLevel[]? PensionOk = null,
    ReformLevel[]? CornLawsOk = null,
    ReformLevel[]? LandReformOk = null,
    bool? MilitarismOk = null,
    bool SubjugatedOk = false,
    string[]? SuzerainsOk = null,
    string Note = "",
    (double Lo, double Hi)? PopGrowthOk = null,
    bool? GreatPowerOk = null,
    int? GdpRank1913 = null,
    int? HomeProvinces1914 = null,
    int? ColonyProvinces1914 = null,
    Polity[]? Exact1820 = null, Polity[]? Exact1870 = null, int? GdpRank1820 = null, int? GdpRank1870 = null,
    Polity[]? Exact1920 = null,
    Polity[]? Exact1950 = null, Polity[]? Near1950 = null, string[]? SuzerainsOk1950 = null);

public sealed record NationFidelity(
    string Nation, string Capital, int Seeds, double Exact, double Near, double LawMatch, int LawAxes, string Verdict,
    double PopMatch = double.NaN, double PopGrowthMean = 0,
    double GreatMatch = double.NaN, double RankMean = 0,
    bool GreatExpected = false,
    double GreatHit = double.NaN,
    double EconMatch = double.NaN, double GdpRankMean = 0,
    double HomeMatch = double.NaN, double HomeFracMean = 0,
    double ColonyMatch = double.NaN, double ColonyMean = 0,
    double TradeMatch = double.NaN, double TradeMean = 0,
    double LabelExact = double.NaN,
    double PopGrowthGrossMean = 0,
    double Exact1820 = double.NaN, double Exact1870 = double.NaN, double Econ1820 = double.NaN, double Econ1870 = double.NaN,
    double Exact1920 = double.NaN,
    double Exact1950 = double.NaN, double Near1950 = double.NaN);

public static class HistoricalReference
{
    private const double FixationNearThreshold = 0.90;
    private const double DeviationNearThreshold = 0.50;
    private const double IdealBandLow = 0.55;
    private const double IdealBandHigh = 0.85;
    public const int GdpRankTolerance = 3;
    public const double HomeProvinceMatchFrac = 0.7;
    public const double ColonyCountTolerance = 0.5;
    public const double MilitarismDegreeOk = 0.13;

    public const double DimensionTargetStrict = 0.5;

    public static string VerdictOf(double near) =>
        near > FixationNearThreshold ? "固着" : near < DeviationNearThreshold ? "逸脱過多" : near is >= IdealBandLow and <= IdealBandHigh ? "◎7割帯" : "△境界";

    public static int AxesOf(HistoricalEntry e) =>
        (e.SuffrageOk is null ? 0 : 1) + (e.EducationOk is null ? 0 : 1) + (e.SerfOk is null ? 0 : 1) +
        (e.PensionOk is null ? 0 : 1) + (e.CornLawsOk is null ? 0 : 1) + (e.LandReformOk is null ? 0 : 1) +
        (e.MilitarismOk is null ? 0 : 1);

    public static (int Matched, int Axes) LawScore(HistoricalEntry e, NationStanding s)
    {
        int axes = 0, ok = 0;
        void Ax<T>(T[]? accepted, T value) { if (accepted is null) return; axes++; if (System.Array.IndexOf(accepted, value) >= 0) ok++; }
        Ax(e.SuffrageOk, s.Suffrage);
        Ax(e.EducationOk, s.Education);
        Ax(e.SerfOk, s.Serf);
        Ax(e.PensionOk, s.Pension);
        Ax(e.CornLawsOk, s.CornLaws);
        Ax(e.LandReformOk, s.LandReform);
        if (e.MilitarismOk is { } m) { axes++; if ((s.Militarism >= MilitarismDegreeOk) == m) ok++; }
        return (ok, axes);
    }

    public static IReadOnlyList<NationFidelity> Evaluate(IReadOnlyList<MatchResult> completed)
    {
        var list = new List<NationFidelity>(Table.Length);
        foreach (var e in Table)
        {
            int seeds = 0, exact = 0, near = 0, lawOk = 0, lawTot = 0;
            int labelExact = 0;
            int popOk = 0, popSeeds = 0; double popSum = 0;
            double popGrossSum = 0;
            int greatOk = 0, greatSeeds = 0; double rankSum = 0;
            bool greatExpected = e.GreatPowerOk == true;
            int econOk = 0, econSeeds = 0; double econRankSum = 0;
            int homeOk = 0, homeSeeds = 0; double homeFracSum = 0;
            int colOk = 0, colSeeds = 0; double colSum = 0;
            int tradeOk = 0, tradeSeeds = 0; double tradeSum = 0;
            double tradeRef = 0;
            int ex1820Ok = 0, ex1820N = 0, ex1870Ok = 0, ex1870N = 0, ec1820Ok = 0, ec1820N = 0, ec1870Ok = 0, ec1870N = 0;
            int ex1920Ok = 0, ex1920N = 0, ecDummyOk = 0, ecDummyN = 0;
            int ex1950Ok = 0, nr1950Ok = 0, ex1950N = 0;
            foreach (var (tn, op) in TradeOpenness1913) if (tn == e.Nation) { tradeRef = op; break; }
            NationStanding? Find(IReadOnlyList<NationStanding> sec)
            {
                foreach (var st in sec) if (st.Nation == e.Nation || (st.Nation.Length == 0 && st.Name == e.Capital)) return st;
                return null;
            }
            foreach (var r in completed)
            {
                var stands = r.StandingsAt1914 ?? r.Standings;
                int rank = 0;
                if (Find(stands) is not { } s) continue;
                seeds++;
                void Section(IReadOnlyList<NationStanding>? sec, Polity[]? okPol, int? okRank, ref int exOk, ref int exN, ref int ecOk, ref int ecN)
                {
                    if (sec is null || Find(sec) is not { } s2) return;
                    if (okPol is not null) { exN++; if (System.Array.IndexOf(okPol, s2.DerivedPolity) >= 0) exOk++; }
                    if (okRank is { } orank)
                    {
                        int g2 = 1; foreach (var st in sec) if (st.Gdp > s2.Gdp) g2++;
                        ecN++; if (System.Math.Abs(g2 - orank) <= GdpRankTolerance) ecOk++;
                    }
                }
                Section(r.StandingsAt1820, e.Exact1820, e.GdpRank1820, ref ex1820Ok, ref ex1820N, ref ec1820Ok, ref ec1820N);
                Section(r.StandingsAt1870, e.Exact1870, e.GdpRank1870, ref ex1870Ok, ref ex1870N, ref ec1870Ok, ref ec1870N);
                Section(r.StandingsAt1920, e.Exact1920, null, ref ex1920Ok, ref ex1920N, ref ecDummyOk, ref ecDummyN);
                if (r.StandingsAt1950 is { } sec50 && e.Exact1950 is { } ok50 && Find(sec50) is { } s50)
                {
                    var (x50, n50) = PolityMatch(s50.DerivedPolity, s50.Suzerain, ok50, e.Near1950, e.SuzerainsOk1950 is not null, e.SuzerainsOk1950);
                    ex1950N++;
                    if (x50) ex1950Ok++;
                    if (n50) nr1950Ok++;
                }
                if (e.GreatPowerOk is { } gp)
                {
                    rank = 1;
                    foreach (var st in stands) if (st.Score > s.Score) rank++;
                    greatSeeds++; rankSum += rank;
                    if ((rank <= Coefficients.GreatPowerCountForFidelity) == gp) greatOk++;
                }
                var (isExact, isNear) = PolityMatch(s.DerivedPolity, s.Suzerain, e.Exact, e.Near, e.SubjugatedOk, e.SuzerainsOk);
                if (isExact) exact++;
                if (isNear) near++;
                if (PolityMatch(s.Polity, s.Suzerain, e.Exact, e.Near, e.SubjugatedOk, e.SuzerainsOk).Exact) labelExact++;
                var (m, a) = LawScore(e, s);
                lawOk += m; lawTot += a;
                if (e.PopGrowthOk is { } pg && s.PopGrowth > 1e-9)
                {
                    popSeeds++; popSum += s.PopGrowth; popGrossSum += s.PopGrowthGross;
                    if (s.PopGrowth >= pg.Lo && s.PopGrowth <= pg.Hi) popOk++;
                }
                if (e.GdpRank1913 is { } gr)
                {
                    int grank = 1;
                    foreach (var st in stands) if (st.Gdp > s.Gdp) grank++;
                    econSeeds++; econRankSum += grank;
                    if (System.Math.Abs(grank - gr) <= GdpRankTolerance) econOk++;
                }
                if (e.HomeProvinces1914 is { } hp && hp > 0)
                {
                    double frac = (double)s.HomeProvinces / hp;
                    homeSeeds++; homeFracSum += frac;
                    if (frac >= HomeProvinceMatchFrac) homeOk++;
                }
                if (e.ColonyProvinces1914 is { } cp)
                {
                    double colVal = s.ColonyMass > 0 ? s.ColonyMass : s.Colonies;
                    colSeeds++; colSum += colVal;
                    double lo = cp * (1.0 - ColonyCountTolerance), hi = cp * (1.0 + ColonyCountTolerance);
                    if (colVal >= lo - 1e-9 && colVal <= hi + 1e-9) colOk++;
                }
                if (tradeRef > 1e-9)
                {
                    tradeSeeds++; tradeSum += s.TradeGdp;
                    if (s.TradeGdp >= tradeRef * (1.0 - TradeOpennessTolerance) - 1e-9
                        && s.TradeGdp <= tradeRef * (1.0 + TradeOpennessTolerance) + 1e-9) tradeOk++;
                }
            }
            if (seeds == 0) continue;
            double nr = (double)near / seeds;
            list.Add(new NationFidelity(e.Nation, e.Capital, seeds, (double)exact / seeds, nr,
                lawTot > 0 ? (double)lawOk / lawTot : double.NaN, AxesOf(e), VerdictOf(nr),
                popSeeds > 0 ? (double)popOk / popSeeds : double.NaN,
                popSeeds > 0 ? popSum / popSeeds : 0,
                greatSeeds > 0 ? (double)greatOk / greatSeeds : double.NaN,
                greatSeeds > 0 ? rankSum / greatSeeds : 0,
                greatExpected,
                greatSeeds > 0 ? (double)greatOk / greatSeeds : double.NaN,
                econSeeds > 0 ? (double)econOk / econSeeds : double.NaN,
                econSeeds > 0 ? econRankSum / econSeeds : 0,
                homeSeeds > 0 ? (double)homeOk / homeSeeds : double.NaN,
                homeSeeds > 0 ? homeFracSum / homeSeeds : 0,
                colSeeds > 0 ? (double)colOk / colSeeds : double.NaN,
                colSeeds > 0 ? colSum / colSeeds : 0,
                tradeSeeds > 0 ? (double)tradeOk / tradeSeeds : double.NaN,
                tradeSeeds > 0 ? tradeSum / tradeSeeds : 0,
                (double)labelExact / seeds,
                popSeeds > 0 ? popGrossSum / popSeeds : 0,
                Exact1820: ex1820N > 0 ? (double)ex1820Ok / ex1820N : double.NaN, Exact1870: ex1870N > 0 ? (double)ex1870Ok / ex1870N : double.NaN,
                Econ1820: ec1820N > 0 ? (double)ec1820Ok / ec1820N : double.NaN, Econ1870: ec1870N > 0 ? (double)ec1870Ok / ec1870N : double.NaN,
                Exact1920: ex1920N > 0 ? (double)ex1920Ok / ex1920N : double.NaN,
                Exact1950: ex1950N > 0 ? (double)ex1950Ok / ex1950N : double.NaN, Near1950: ex1950N > 0 ? (double)nr1950Ok / ex1950N : double.NaN));
        }
        return list;
    }

    public static (bool Exact, bool Near) PolityMatch(Polity polity, string suzerain, Polity[] exact, Polity[]? near, bool subjugatedOk, string[]? suzerainsOk)
    {
        bool subjugated = subjugatedOk && suzerain.Length > 0;
        bool isExact = (subjugated && (suzerainsOk is null || System.Array.IndexOf(suzerainsOk, suzerain) >= 0)) || System.Array.IndexOf(exact, polity) >= 0;
        return (isExact, isExact || subjugated || (near is not null && System.Array.IndexOf(near, polity) >= 0));
    }

    public static readonly (string Nation, double Openness)[] TradeOpenness1913 =
    [
        ("イギリス", 0.45), ("フランス", 0.35), ("プロイセン", 0.37), ("ロシア", 0.16),
        ("アメリカ", 0.11), ("清", 0.08), ("日本", 0.31), ("オーストリア", 0.22), ("サルデーニャ＝ピエモンテ", 0.28),
    ];
    public const double TradeOpennessTolerance = 0.5;

    public static readonly HistoricalEntry[] Table =
    [
        new("フランス", "パリ", [Polity.Republic], [Polity.BourgeoisDictatorship, Polity.ConstitutionalMonarchy],
            Exact1950: [Polity.Republic], Near1950: [Polity.BourgeoisDictatorship],
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Universal], EducationOk: [EducationLaw.Compulsory], SerfOk: [ReformLevel.Full],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.Full, ReformLevel.Limited],
            Note: "第三共和政。男子普選1848・フェリー義務教育1882・封建的負担は1789廃止。近い＝ブル独裁（ボナパルティズム）/立憲（オルレアン系）",
            PopGrowthOk: (1.25, 1.7),
            GreatPowerOk: true,
            Exact1920: [Polity.Republic], Exact1820: [Polity.ConstitutionalMonarchy], Exact1870: [Polity.Republic], GdpRank1820: 5, GdpRank1870: 6, GdpRank1913: 7, HomeProvinces1914: 8, ColonyProvinces1914: 10),
        new("日本", "江戸", [Polity.ConstitutionalMonarchy], [Polity.Despotic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic], SuzerainsOk1950: ["アメリカ"],
            SuffrageOk: [Suffrage.Census], EducationOk: [EducationLaw.Compulsory], SerfOk: [ReformLevel.Full, ReformLevel.Limited],
            LandReformOk: [ReformLevel.Full, ReformLevel.Limited],
            Note: "明治立憲制（1890帝国議会＝納税制限選挙・男子普選は1925＝枠外）。学制1872→義務教育。身分解放1871。近い＝有司専制（超然内閣の権威側読み）。幕府存続は「稀に残る」枠＝nearに数えない",
            PopGrowthOk: (1.7, 2.4),
            GreatPowerOk: true,
            Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.Shogunate], Exact1870: [Polity.Absolutist], GdpRank1820: 8, GdpRank1870: 8, GdpRank1913: 10, HomeProvinces1914: 7, ColonyProvinces1914: 1),
        new("イギリス", "ロンドン", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],
            SuffrageOk: [Suffrage.Census, Suffrage.AdultMale], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public],
            SerfOk: [ReformLevel.Full], CornLawsOk: [ReformLevel.None],
            LandReformOk: [ReformLevel.None],
            Note: "議会主権の立憲王政。1884第三次選挙法＝成人男子約6割（Census寄り・1918男子普選は境界＝両許容）。義務教育1880。穀物法撤廃1846＝保護なし",
            PopGrowthOk: (2.6, 4.3),
            GreatPowerOk: true,
            Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.ConstitutionalMonarchy], Exact1870: [Polity.ConstitutionalMonarchy], GdpRank1820: 4, GdpRank1870: 2, GdpRank1913: 5, HomeProvinces1914: 5, ColonyProvinces1914: 21),
        new("アメリカ", "ニューヨーク", [Polity.Republic], [Polity.BourgeoisDictatorship],
            Exact1950: [Polity.Republic], Near1950: [Polity.BourgeoisDictatorship],
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Universal], EducationOk: [EducationLaw.Public, EducationLaw.Compulsory], MilitarismOk: false,
            CornLawsOk: [ReformLevel.None], LandReformOk: [ReformLevel.None, ReformLevel.Limited],
            Note: "連邦共和制。白人男子普選は1850年代までに定着。公教育（州営コモンスクール）。常備軍小＝非軍国",
            GreatPowerOk: true, Exact1920: [Polity.Republic], Exact1820: [Polity.Republic], Exact1870: [Polity.Republic], GdpRank1820: 10, GdpRank1870: 3, GdpRank1913: 1, HomeProvinces1914: 3, ColonyProvinces1914: 2),
        new("ロシア", "モスクワ", [Polity.Despotic], [Polity.Absolutist, Polity.ConstitutionalMonarchy],
            Exact1950: [Polity.ProletarianDictatorship],
            SuffrageOk: [Suffrage.None, Suffrage.Census], EducationOk: [EducationLaw.Religious, EducationLaw.Public], SerfOk: [ReformLevel.Full, ReformLevel.Limited],
            CornLawsOk: [ReformLevel.None], LandReformOk: [ReformLevel.None, ReformLevel.Limited],
            Note: "ツァーリ専制（官僚・軍による上からの統治）。1905十月詔書→ドゥーマ＝半立憲（near）。農奴解放1861（買戻金の残滓＝Limited許容）。ドゥーマ選挙は制限＝Census許容",
            GreatPowerOk: true, Exact1920: [Polity.ProletarianDictatorship], Exact1820: [Polity.Absolutist], Exact1870: [Polity.Absolutist], GdpRank1820: 3, GdpRank1870: 4, GdpRank1913: 4, HomeProvinces1914: 13, ColonyProvinces1914: 0),
        new("プロイセン", "ベルリン", [Polity.ConstitutionalMonarchy], [Polity.Despotic, Polity.MilitaryDictatorship],
            Exact1950: [Polity.Republic, Polity.ProletarianDictatorship], SuzerainsOk1950: ["アメリカ", "イギリス", "フランス", "ロシア"],
            SuffrageOk: [Suffrage.Census, Suffrage.AdultMale], EducationOk: [EducationLaw.Compulsory],
            PensionOk: [ReformLevel.Limited, ReformLevel.Full], MilitarismOk: true,
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.None, ReformLevel.Limited],
            Note: "カイザーライヒ＝権威主義的立憲君主制（帝国議会＝男子普選/プロイセン三級選挙＝制限の二重制＝両許容）。義務教育の祖。ビスマルク年金1889＝世界初。軍国主義＝親軍",
            GreatPowerOk: true, Exact1920: [Polity.Republic], Exact1820: [Polity.Absolutist], Exact1870: [Polity.ConstitutionalMonarchy], GdpRank1820: 6, GdpRank1870: 5, GdpRank1913: 3, HomeProvinces1914: 4, ColonyProvinces1914: 3),
        new("オスマン", "イスタンブール", [Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship], [Polity.Absolutist, Polity.Despotic],
            Exact1950: [Polity.Republic, Polity.BourgeoisDictatorship], Near1950: [Polity.MilitaryDictatorship],
            SuffrageOk: [Suffrage.None, Suffrage.Census], EducationOk: [EducationLaw.Religious, EducationLaw.Public],
            LandReformOk: [ReformLevel.None],
            Exact1920: [Polity.Absolutist, Polity.MilitaryDictatorship], Exact1820: [Polity.Absolutist], Exact1870: [Polity.Absolutist], GdpRank1820: 12, GdpRank1870: 11, Note: "1908青年トルコ革命＝立憲復活→1913クーデタ＝CUP三頭の軍事支配（名目スルタン）＝両方exact。近い＝ハミト専制の存続。タンジマート＝世俗学校の併存",
            HomeProvinces1914: 8, ColonyProvinces1914: 0, GreatPowerOk: false),
        new("オーストリア", "ウィーン", [Polity.ConstitutionalMonarchy], [Polity.Absolutist, Polity.Despotic],
            Exact1950: [Polity.Republic], SuzerainsOk1950: ["アメリカ", "イギリス", "フランス", "ロシア"],
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Census], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.None],
            Note: "二重帝国＝立憲君主制。チスライタニア男子普選1907（ハンガリー側は制限＝Census併存）。帝国国民学校法1869＝義務教育",
            PopGrowthOk: (1.8, 2.6),
            GreatPowerOk: true, Exact1920: [Polity.Republic], Exact1820: [Polity.Absolutist], Exact1870: [Polity.ConstitutionalMonarchy], GdpRank1820: 9, GdpRank1870: 9, GdpRank1913: 8, HomeProvinces1914: 11, ColonyProvinces1914: 0),
        new("ネーデルラント", "アムステルダム", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],
            SuffrageOk: [Suffrage.Census, Suffrage.AdultMale], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public], SerfOk: [ReformLevel.Full],
            CornLawsOk: [ReformLevel.None], LandReformOk: [ReformLevel.None, ReformLevel.Limited],
            Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.ConstitutionalMonarchy], Exact1870: [Polity.ConstitutionalMonarchy], Note: "オラニエ立憲王政（1848憲法）。選挙権拡大1887/96＝制限（男子普選1917＝境界）。義務教育1900。ゲーム開始は連邦共和国＝共和残存はnear",
            HomeProvinces1914: 2, ColonyProvinces1914: 3, GreatPowerOk: false),
        new("スペイン", "マドリード", [Polity.ConstitutionalMonarchy], [Polity.MilitaryDictatorship, Polity.Absolutist],
            Exact1950: [Polity.MilitaryDictatorship], Near1950: [Polity.BourgeoisDictatorship],
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Census], SerfOk: [ReformLevel.Full, ReformLevel.Limited],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.None],
            Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.ConstitutionalMonarchy, Polity.Absolutist], Exact1870: [Polity.ConstitutionalMonarchy], GdpRank1820: 11, GdpRank1870: 10, Note: "王政復古体制（カノバス体制＝寡頭立憲）。男子普選1890（カシキスモで形骸だが法は普選）。近い＝プロヌンシアミエントの世紀＝軍事独裁/カルリスタ絶対王政",
            PopGrowthOk: (1.6, 2.2), HomeProvinces1914: 5, ColonyProvinces1914: 2, GreatPowerOk: false),
        new("サルデーニャ＝ピエモンテ", "トリノ", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy],
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Census], EducationOk: [EducationLaw.Public, EducationLaw.Compulsory],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.None],
            Note: "統一イタリア＝サヴォイア立憲王政（アルベルト憲章）。1912準男子普選。カザーティ/コッピーノ法＝公教育。近い＝マッツィーニ/ガリバルディの共和主義",
            GreatPowerOk: true, Exact1920: [Polity.ConstitutionalMonarchy], Exact1820: [Polity.Absolutist], Exact1870: [Polity.ConstitutionalMonarchy], GdpRank1870: 7, GdpRank1913: 9, HomeProvinces1914: 3, ColonyProvinces1914: 2),
        new("清", "上海", [Polity.Republic], [Polity.Despotic, Polity.MilitaryDictatorship],
            Exact1950: [Polity.ProletarianDictatorship], Near1950: [Polity.MilitaryDictatorship, Polity.BourgeoisDictatorship],
            LandReformOk: [ReformLevel.None],
            Exact1920: [Polity.MilitaryDictatorship, Polity.Republic], Exact1820: [Polity.Despotic], Exact1870: [Polity.Despotic], GdpRank1820: 1, GdpRank1870: 1, Note: "辛亥革命1911→1912中華民国＝共和がexact。近い＝清朝専制の存続（Despotic）・袁世凱の軍権支配（MD＝1914年の実態）。法は転換期＝軸なし",
            PopGrowthOk: (1.1, 1.9), HomeProvinces1914: 19, ColonyProvinces1914: 0, GreatPowerOk: false),
        new("朝鮮", "漢城", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.Republic], Near1950: [Polity.ProletarianDictatorship, Polity.BourgeoisDictatorship],
            SubjugatedOk: true, SuzerainsOk: ["日本"], Note: "大韓帝国＝専制皇権（光武改革）→1910日本併合＝主権喪失。従属exact＝宗主日本（宗主同一性2026-07-12）・他宗主はnear", GreatPowerOk: false),
        new("マラータ同盟", "プーナ", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: [],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "1818英併合＝主権喪失（第三次マラータ戦争）。藩王国的専制の残存をexact。列強化・常勝は逸脱＝要因果監査", GreatPowerOk: false),
        new("マイソール", "シュリーランガパトナ", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: [],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "1799英征服→藩王国（ウォデヤル朝復位1881＝模範藩王国）。伝統君主制の存続をexact", GreatPowerOk: false),
        new("ハイデラバード", "ハイデラバード", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: [],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ニザーム藩王国＝英保護下の専制。1914も専制のまま＝exact", GreatPowerOk: false),
        new("シク連邦", "ラホール", [Polity.Despotic, Polity.Absolutist], [Polity.Republic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ランジート・シングの王国→1849英併合。専制王国をexact・ミスル合議制（カールサの伝統）＝共和をnear", GreatPowerOk: false),
        new("ビルマ", "ラングーン", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.Republic], Near1950: [Polity.MilitaryDictatorship],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "コンバウン朝専制→1885英併合＝主権喪失。伝統専制の存続をexact", GreatPowerOk: false),
        new("ネパール", "カトマンズ", [Polity.Despotic, Polity.MilitaryDictatorship], [Polity.Absolutist],
            Exact1950: [Polity.Despotic, Polity.MilitaryDictatorship], Near1950: [Polity.Absolutist, Polity.ConstitutionalMonarchy],
            SerfOk: [ReformLevel.None, ReformLevel.Limited],
            Note: "ラナ家世襲宰相制＝軍門閥の実権支配（名目王）＝Des/MDの両方exact。主権は維持（英同盟）", GreatPowerOk: false),
        new("シャム", "バンコク", [Polity.Despotic], [Polity.Absolutist, Polity.ConstitutionalMonarchy],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship],
            SuffrageOk: [Suffrage.None, Suffrage.Census], SerfOk: [ReformLevel.Full, ReformLevel.Limited],
            Note: "チュラーロンコーン大王＝上からの近代化専制（主権維持＝緩衝国）。奴隷制・賦役の廃止1905＝解放Full許容。立憲革命1932は先＝near", GreatPowerOk: false),
        new("西山朝", "ハノイ", [Polity.Despotic], [Polity.Absolutist],
            Exact1950: [Polity.ProletarianDictatorship], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス"],
            SubjugatedOk: true, SuzerainsOk: ["阮氏", "フランス"], Note: "史実は1802阮氏に滅亡→仏領インドシナ＝宗主は阮氏/フランスを許容。存続時は農民反乱由来の軍事専制＝Despotic", GreatPowerOk: false),
        new("カンボジア", "プノンペン", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Absolutist], SuzerainsOk1950: ["フランス"],
            SubjugatedOk: true, SuzerainsOk: ["フランス", "シャム"], Note: "仏保護国1863＝王制存続（ノロドム王）。前史はシャム宗主権（1794以降）＝両許容。伝統王政をexact", GreatPowerOk: false),
        new("ラオス", "ヴィエンチャン", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Absolutist], SuzerainsOk1950: ["フランス"],
            SubjugatedOk: true, SuzerainsOk: ["フランス", "シャム"], Note: "仏保護領1893＝ルアンパバーン王制存続。前史はシャム宗主権＝両許容。伝統王政をexact", GreatPowerOk: false),
        new("ペルシャ", "テヘラン", [Polity.Despotic, Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Absolutist, Polity.Despotic],
            Note: "カージャール専制→1906立憲革命→1911露介入で議会停止＝名目立憲・実態専制＝両方exact", GreatPowerOk: false),
        new("アラビア", "ディルイーヤ", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic],
            SerfOk: [ReformLevel.None],
            SubjugatedOk: true, SuzerainsOk: ["オスマン"], Note: "第一次サウード国1818滅亡（オスマン＝エジプト遠征）→リヤド再興1902＝1914は名目オスマン宗主権下。伝統専制をexact", GreatPowerOk: false),
        new("イエメン", "サヌア", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic],
            SerfOk: [ReformLevel.None],
            SubjugatedOk: true, SuzerainsOk: ["オスマン"], Note: "ザイド派イマーム国＝神権的君主制（1911ダアーン条約＝オスマン下の自治）", GreatPowerOk: false),
        new("オマーン", "マスカット", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: [],
            SerfOk: [ReformLevel.None],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ブーサイード朝スルタン国（英影響下＝事実上の英保護）", GreatPowerOk: false),
        new("アフガニスタン", "カブール", [Polity.Despotic], [Polity.Absolutist],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic, Polity.ConstitutionalMonarchy],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "アブドゥッラフマーン以降の中央集権専制（鉄のアミール）。外交は英管理（ガンダマク条約）＝英宗主を許容", GreatPowerOk: false),
        new("ブハラ", "ブハラ", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],
            SubjugatedOk: true, SuzerainsOk: ["ロシア"], Note: "露保護国1873＝アミール制存続。伝統専制をexact", GreatPowerOk: false),
        new("ヒヴァ", "ヒヴァ", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],
            SubjugatedOk: true, SuzerainsOk: ["ロシア"], Note: "露保護国1873＝ハン制存続。同上", GreatPowerOk: false),
        new("コーカンド", "タシュケント", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],
            SubjugatedOk: true, SuzerainsOk: ["ロシア"], Note: "1876露併合＝汗国消滅＝主権喪失。伝統専制の存続をexact", GreatPowerOk: false),
        new("モロッコ", "フェズ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス", "スペイン"],
            SubjugatedOk: true, SuzerainsOk: ["フランス", "スペイン"], Note: "アラウィー朝スルタン国→1912仏西保護領（王制存続）。伝統王政をexact", GreatPowerOk: false),
        new("エチオピア", "ゴンダール", [Polity.Despotic], [Polity.Absolutist],
            Exact1950: [Polity.Absolutist, Polity.Despotic], Near1950: [Polity.ConstitutionalMonarchy],
            SerfOk: [ReformLevel.None],
            Note: "メネリク2世＝集権化・近代化の専制（アドワ1896＝独立防衛に成功した非欧州国）", GreatPowerOk: false),
        new("マダガスカル", "アンタナナリヴ", [Polity.Absolutist, Polity.Despotic], [],
            Exact1950: [Polity.Absolutist, Polity.Despotic], SuzerainsOk1950: ["フランス"],
            SubjugatedOk: true, SuzerainsOk: ["フランス"], Note: "メリナ王国＝1896仏併合・王制廃止＝主権喪失。伝統王政の存続をexact", GreatPowerOk: false),
        new("アシャンティ王国", "クマシ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "アカン人の連合王国＝金とカカオの交易国家で常備軍を持つ。1824アサマンコで英軍を破り総督を討つが、5次に及ぶ英アシャンティ戦争の末1902英領ゴールドコーストへ併合＝主権喪失。伝統王政の存続をexact", GreatPowerOk: false),
        new("ソコト", "ソコト", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "フラニのジハードが建てた19世紀最大のアフリカ国家（人口1000〜2000万）。1903年カノ・ソコト陥落まで英に屈しなかった＝主権喪失は世紀末。伝統王政の存続をexact", GreatPowerOk: false),
        new("コンゴ王国", "ンバンザ=コンゴ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["ポルトガル", "ベルギー"],
            SubjugatedOk: true, SuzerainsOk: ["ポルトガル", "ベルギー"], Note: "1491年に王が改宗したアフリカ唯一のキリスト教王国。1783年は内戦後の衰退期で、1914年に葡が王制を廃止＝主権喪失。伝統王政の存続をexact", GreatPowerOk: false),
        new("アルジェ摂政国", "アルジェ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス"],
            SubjugatedOk: true, SuzerainsOk: ["フランス", "オスマン"], Note: "オスマンの名目宗主下の海賊国家。穀物の未払い債務を口実に1830年仏が侵攻（扇の一撃）＝主権喪失。それまでの専制をexact", GreatPowerOk: false),
        new("オヨ帝国", "オヨ=イレ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ヨルバの騎兵帝国＝1783年は全盛期だが1836年に内部崩壊する。崩壊は台本でなく不満と圧力から出るなら出る。専制の存続をexact", GreatPowerOk: false),
        new("ボルヌ帝国", "ンガザルガム", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス", "フランス"],
            SubjugatedOk: true, SuzerainsOk: ["イギリス", "フランス"], Note: "カネム・ボルヌ＝9世紀から続くサハラ縦断交易の塩の帝国。完全内陸＝列強が届くのが最も遅い対照群。専制の存続をexact", GreatPowerOk: false),
        new("ハワイ王国", "ホノルル", [Polity.Absolutist, Polity.ConstitutionalMonarchy], [Polity.Despotic],
            Exact1950: [Polity.Absolutist, Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic], SuzerainsOk1950: ["アメリカ"],
            SubjugatedOk: true, SuzerainsOk: ["アメリカ", "イギリス"], Note: "カメハメハの統一(1795)以降の独立王国。1840年憲法で立憲君主制へ移行し、1893年に米系入植者のクーデタで王制打倒＝主権喪失。立憲化と王制の存続をexact", GreatPowerOk: false),
        new("ダホメ王国", "アボメイ", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["フランス"],
            SubjugatedOk: true, SuzerainsOk: ["フランス"], Note: "フォン人の王国＝常備軍（女性親衛隊）を国制の中心に置いた交易国家。1890/1892仏ダホメ戦争→1894仏領＝主権喪失。伝統王政の存続をexact", GreatPowerOk: false),
        new("スウェーデン", "ストックホルム", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Census], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.Limited, ReformLevel.Full],
            Note: "立憲王政（1866二院制）。男子普選1909。義務教育1842＝最初期", GreatPowerOk: false),
        new("デンマーク", "コペンハーゲン", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.ConstitutionalMonarchy], Near1950: [Polity.Republic],
            SuffrageOk: [Suffrage.AdultMale, Suffrage.Census], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public], PensionOk: [ReformLevel.Limited, ReformLevel.Full],
            CornLawsOk: [ReformLevel.None], LandReformOk: [ReformLevel.Full, ReformLevel.Limited],
            Note: "六月憲法1849＝立憲王政・広範な男子選挙権。義務教育1814＝世界最初期。老齢扶助法1891＝最初期の年金", GreatPowerOk: false),
        new("スイス", "ベルン", [Polity.Republic], [Polity.BourgeoisDictatorship],
            Exact1950: [Polity.Republic], Near1950: [Polity.BourgeoisDictatorship],
            SuffrageOk: [Suffrage.AdultMale], EducationOk: [EducationLaw.Compulsory, EducationLaw.Public],
            Note: "1848連邦憲法＝連邦共和制・男子普選。義務教育1874", GreatPowerOk: false),
        new("ポーランド", "ワルシャワ", [Polity.ConstitutionalMonarchy, Polity.Republic], [Polity.Absolutist],
            Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["ロシア"],
            SubjugatedOk: true, SuzerainsOk: ["ロシア", "プロイセン", "オーストリア"], Note: "史実は1795第三次分割で消滅＝宗主は分割三国を許容。存続時は五月三日憲法1791（立憲）〜1918共和が史実の系", GreatPowerOk: false),
        new("ポルトガル", "リスボン", [Polity.Republic], [Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship],
            Exact1950: [Polity.BourgeoisDictatorship], Near1950: [Polity.MilitaryDictatorship],
            SuffrageOk: [Suffrage.Census, Suffrage.AdultMale],
            CornLawsOk: [ReformLevel.Limited, ReformLevel.Full], LandReformOk: [ReformLevel.None],
            Note: "1910革命＝第一共和政がexact。近い＝立憲王政の存続/軍部介入（1926は先）。第一共和政も識字要件＝制限選挙",
            PopGrowthOk: (1.5, 2.0), GreatPowerOk: false),
        new("ジェノヴァ", "ジェノヴァ", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: ["サルデーニャ＝ピエモンテ"],
            SubjugatedOk: true, SuzerainsOk: ["サルデーニャ＝ピエモンテ"], Note: "1815サルデーニャ併合→統一イタリア＝立憲王政の一部がexact。寡頭共和国の伝統＝near", GreatPowerOk: false),
        new("ライン諸邦", "ケルン", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],
            SubjugatedOk: true, SuzerainsOk: ["プロイセン", "フランス"], Note: "仏併合（ナポレオン期）→1815普領ラインラント→独帝国＝宗主は普/仏を許容。小邦絶対主義の存続＝near", GreatPowerOk: false),
        new("両シチリア", "ナポリ", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: ["サルデーニャ＝ピエモンテ"],
            SubjugatedOk: true, SuzerainsOk: ["サルデーニャ＝ピエモンテ"], Note: "1861統一イタリアへ併合＝立憲王政の一部がexact。ブルボン専制の存続＝near", GreatPowerOk: false),
        new("教皇領", "ローマ", [Polity.Absolutist], [Polity.ConstitutionalMonarchy],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Republic], SuzerainsOk1950: ["サルデーニャ＝ピエモンテ"],
            SubjugatedOk: true, SuzerainsOk: ["サルデーニャ＝ピエモンテ"], Note: "神権的絶対制（教皇庁は1870併合後も絶対制のまま）がexact。伊併合＝立憲王国の一部", GreatPowerOk: false),
        new("トスカーナ", "フィレンツェ", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: ["サルデーニャ＝ピエモンテ"],
            SubjugatedOk: true, SuzerainsOk: ["サルデーニャ＝ピエモンテ"], Note: "1860併合＝統一イタリアの一部がexact。ハプスブルク＝ロレーナ家の開明専制の存続＝near", GreatPowerOk: false),
        new("ヴェネツィア", "ヴェネツィア", [Polity.ConstitutionalMonarchy], [Polity.Republic],
            Exact1950: [Polity.Republic], Near1950: [Polity.ConstitutionalMonarchy], SuzerainsOk1950: ["サルデーニャ＝ピエモンテ"],
            SubjugatedOk: true, SuzerainsOk: ["サルデーニャ＝ピエモンテ", "オーストリア"], Note: "1797共和国滅亡→墺領→1866統一イタリア＝宗主は伊/墺を許容。千年共和国の伝統＝near", GreatPowerOk: false),
        new("バイエルン", "ミュンヘン", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],
            SubjugatedOk: true, SuzerainsOk: ["プロイセン"], Note: "独帝国内の立憲王国（王冠・議会を保持した構成国）がexact", GreatPowerOk: false),
        new("ザクセン", "ドレスデン", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.ProletarianDictatorship], SuzerainsOk1950: ["プロイセン", "ロシア"],
            SubjugatedOk: true, SuzerainsOk: ["プロイセン"], Note: "独帝国内の立憲王国がexact", GreatPowerOk: false),
        new("ハノーファー", "ハノーファー", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],
            SubjugatedOk: true, SuzerainsOk: ["プロイセン"], Note: "1866普墺戦争で普併合→独帝国＝立憲帝政の一部がexact", GreatPowerOk: false),
        new("ヴュルテンベルク", "シュトゥットガルト", [Polity.ConstitutionalMonarchy], [Polity.Absolutist],
            Exact1950: [Polity.Republic], SuzerainsOk1950: ["プロイセン"],
            SubjugatedOk: true, SuzerainsOk: ["プロイセン"], Note: "独帝国内の立憲王国がexact", GreatPowerOk: false),
        new("阮氏", "サイゴン", [Polity.Despotic], [Polity.Absolutist],
            Exact1950: [Polity.Despotic, Polity.Absolutist], Near1950: [Polity.Republic], SuzerainsOk1950: ["フランス"],
            SubjugatedOk: true, SuzerainsOk: ["フランス"], Note: "1802全越統一（阮朝）＝明命帝の中央集権専制→1883-85仏保護国化（王制存続）。中央集権専制をexact", GreatPowerOk: false),
        new("ラヘジ", "アデン", [Polity.Absolutist], [Polity.Despotic],
            Exact1950: [Polity.Absolutist], Near1950: [Polity.Despotic], SuzerainsOk1950: ["イギリス"],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ラヘジ・スルタン国＝英保護領（アデンは1839英直轄）。伝統首長制をexact", GreatPowerOk: false),
        new("デリー", "デリー", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: [],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "ムガル残滓→1857大反乱後に廃絶＝主権喪失。伝統専制の存続をexact", GreatPowerOk: false),
        new("アワド", "ラクナウ", [Polity.Despotic, Polity.Absolutist], [],
            Exact1950: [Polity.ConstitutionalMonarchy, Polity.Republic], SuzerainsOk1950: [],
            SubjugatedOk: true, SuzerainsOk: ["イギリス"], Note: "1856英併合＝主権喪失。太守（ナワーブ）専制の存続をexact", GreatPowerOk: false),
    ];
}
