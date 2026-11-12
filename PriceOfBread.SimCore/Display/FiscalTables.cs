namespace PriceOfBread.SimCore.Display;

/// <summary>財政の表が読む標本（App が四半期と月の境で採る差分）。教育費・専売益・外資の流れの実績は州の累積の差分＝今の盤面からは導けないので、採る側（App）が渡す。
/// <see cref="LedgerDiff"/>＝国庫の台帳の月の差分（<see cref="TreasuryCat"/> の添字）。</summary>
public sealed record FiscalFlows(double EduMonthly, double StateMonopolyMonthly, double ForeignInMonthly, double ForeignOutMonthly, IReadOnlyList<double> LedgerDiff)
{
    public static FiscalFlows Zero { get; } = new(0, 0, 0, 0, []);
}

/// <summary>予算の表の 1 行（支出か収入の 1 項）。<see cref="Forecast"/>＝予測（目標の段ベース）・<see cref="Actual"/>＝実効（今いま実際に流れている額）。
/// <see cref="Stage"/>＝その項の段の押し釦（無ければ <see cref="StageCell.None"/>）。<see cref="Tip"/>＝項の名前にカーソルを合わせた時の文。
/// <see cref="NoForecast"/>＝予測を出さない項（実績だけの項）。<see cref="NoAmounts"/>＝金額を出さない項。
/// <see cref="Child"/>＝上の行の内訳（陸軍と海軍は軍事費の内訳＝合計には足さない）。</summary>
public sealed record BudgetLine(string Key, string Name, double Forecast, double Actual, StageCell Stage, string? Tip, bool NoForecast = false, bool NoAmounts = false, bool Child = false);

/// <summary>予算の予想（支出と収入の項・合計）。<see cref="FiscalTables.Budget"/> が盤面を読んで組む。</summary>
public sealed record BudgetResult(IReadOnlyList<BudgetLine> Spend, IReadOnlyList<BudgetLine> Income,
    double SpendForecast, double SpendActual, double IncomeForecast, double IncomeActual)
{
    public static BudgetResult Empty { get; } = new([], [], 0, 0, 0, 0);
    /// <summary>予測の収支（収入−支出）。</summary>
    public double BalanceForecast => IncomeForecast - SpendForecast;
    /// <summary>実績の収支（入った額−払った額）＝国庫の台帳の収支＝ヘッダーの国庫の月の増減。</summary>
    public double BalanceActual => IncomeActual - SpendActual;
}

/// <summary>金融の表の 1 行（名前・値の文字・段の押し釦）。</summary>
public sealed record FinanceLine(string Key, string Name, string Value, StageCell Stage, string? Tip);

/// <summary>国債と外資信用の表の 1 行。</summary>
public sealed record DebtLine(string Key, string Group, string Name, string Value);

/// <summary>財政と貨幣のページの表（設計 §3-3）。支出・収入・国庫の台帳・金融・国債と外資信用。
/// 予測と実効の額は旧 App の予算予想（<c>UpdateForecast</c>）を本体の目録へ移した物＝式は同じ（税の予想は <see cref="Fiscal.ForecastTax"/>・軍事費の器は <see cref="Military.EnvelopeAt"/> ほか全てエンジンの 1 本を読む）。
/// 盤面は読むだけ（書き込みは段の押し釦を押した時だけ。行き先は <see cref="NationPolicy"/> の目標の欄・近代化投資の段だけは <see cref="World.ModernizationFrac"/>）・状態を持たない。</summary>
public static class FiscalTables
{
    /// <summary>関税の上限（下限は <see cref="Coefficients.TariffFloor"/>・負＝ダンピング）。</summary>
    public const double TariffCeil = 0.75;
    /// <summary>政策金利の上限。</summary>
    public const double InterestCeil = 0.5;
    private const int LeverStages = 4;   // レバーの段の区間数（ボタンは 5 個）

    // ===== レバーの 5 段（法ではない 4 本＝行政費・近代化投資・関税・政策金利）=====

    /// <summary>レバーの 5 段。値＝そのレバー自身の下限と上限を 4 等分してエンジンの 0.05 格子へ乗せた 5 点。最上段も数（B31）。
    /// 現在段＝施行値・目標段＝目標値の写像（税の段と同じ）。新しい上限・下限は作らない。</summary>
    /// <param name="names">段の名（5 個）。無ければ割合の文字（最上段だけ「最大」）。無次元の値を百分率で出さない行（政策金利）が渡す。</param>
    /// <param name="offGridDark">真＝段の値の格子に乗らない値（開幕の近代化投資 8% など）は最寄りの段を光らせず、どの段も光らせない（−1）。</param>
    private static StageCell Lever(World w, string title, double lo, double hi, Func<double> cur, Func<double> tgt, Action<double> set, string[]? names = null, bool offGridDark = false)
    {
        double Value(int k) => Government.Q5(lo + (hi - lo) * k / (double)LeverStages);
        int Nearest(double v) => !(hi - lo > 1e-9) ? 0 : Math.Clamp((int)Math.Round((v - lo) / (hi - lo) * LeverStages, MidpointRounding.AwayFromZero), 0, LeverStages);   // 否定形＝NaN/退化した幅は最下段へ
        int Stage(double v) { int k = Nearest(v); return offGridDark && !(Math.Abs(v - Value(k)) < 1e-6) ? -1 : k; }
        var labels = new string[LeverStages + 1];
        for (int i = 0; i < labels.Length; i++) labels[i] = names is not null ? names[i] : DisplayFormat.Share(Value(i));   // 割合の段は最後も数（行政費・近代化投資は 100%・関税は 75%）
        return StageGate.Make(w, null, title, labels, 0, () => Stage(cur()), () => Stage(tgt()), v => set(Value(v)));
    }

    /// <summary>0〜1 の割合の 5 段（0・25・50・75・100%）の格子に乗らない値か（開幕の近代化投資 8% など）。乗らない値は段を光らせず、名に値を添える。</summary>
    private static bool OffGrid(double v) => !(Math.Abs(v * LeverStages - Math.Round(v * LeverStages)) < 1e-6);

    /// <summary>政策金利の 5 段の名（緩い方から）。</summary>
    private static readonly string[] RateStageNames = ["大幅な緩和", "緩和", "中立", "引締め", "大幅な引締め"];

    /// <summary>段の名（段の外は「—」）。</summary>
    private static string StageName(StageCell st, int level) => level - st.LevelBase >= 0 && level - st.LevelBase < st.Labels.Count ? st.Labels[level - st.LevelBase] : DisplayFormat.NoValue;

    private static StageCell FiscalStageOf(World w, string key) => StageGate.Of(w, FiscalCatalog.Stages.First(s => s.Key == key));

    // ===== 予算（支出と収入）=====

    /// <summary>予算の予想。目標（段で置いた値）と今の人口の状態で組む予測と、エンジンが書く実績の診断を読む実効を、項ごとに並べる。</summary>
    public static BudgetResult Budget(World w, FiscalFlows f)
    {
        var c = w.Coef;
        double massSize = 0, oldSize = 0;
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
            {
                if (!p.Closed && !p.IsModernElite) massSize += p.Size;
                oldSize += p.Size * p.AgeShare(60, double.PositiveInfinity);   // 年金の受け手＝60歳以上（エンジン Fiscal と同じ 1 本の述語・年齢の層）
            }
        // 税の予想の 4 列はエンジンの徴税の走査と同じ査定の関数を目標の率で呼ぶ（課税への同意・身分の免税・前の月の所得・国庫へ届く割合・仲介者の取り分・払える割合・犯罪者の除外）
        var (gIncome, gProg, gPoll, gLand) = Fiscal.ForecastTax(w, c, w.Policy.IncomeTaxTarget, w.Policy.ProgressiveTaxTarget, w.Policy.PollTaxTarget, w.Policy.LandTaxTarget);
        double net = gIncome + gProg + gPoll + gLand;
        // 関税：直近の貿易額（世界価格建て・清算単位を MonthsPerClearing で月割）×率。正＝輸入関税収入／負＝輸出補助（ダンピング＝コスト）
        double tImpVal = w.LastImportValue / (double)c.MonthsPerClearing, tExpVal = w.LastExportValue / (double)c.MonthsPerClearing;
        double TariffRev(double rate) => (rate >= 0 ? tImpVal : tExpVal) * rate;
        double tariffGross = TariffRev(w.Policy.TariffTarget);
        double interest = Calendar.PerYearToPerMonth(w.GovDebt.Value * w.GovBondRatePerYear.Value);
        // 軍事費の器（段×器の割合×使える歳入＋戦時段の割合×借入の余地）から兵士の給与の請求を引いた残り＝装備の上限（エンジンの Military.EnvelopeAt と同じ 1 本）
        double mil = Math.Max(0, Military.EnvelopeAt(w, c, Government.MilitaryTotalTarget(w, w.Policy)) - w.LastSoldierPayPerMonth.Value);
        double admin = c.AdminBudgetFrac.Value * w.Policy.AdminSpendingTarget * net * Fiscal.AdminDistanceMean(w);
        double welCap = (c.WelfareRevenueShare * w.FiscalCapacityPerMonth).Value;
        double wel = Math.Min(c.WelfareCostRatePerMonth * w.Policy.SocialSecurityTarget * massSize, welCap);
        double penLevel = w.Pension switch { ReformLevel.Full => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
        double pen = Math.Min(c.PensionCostRatePerMonth * penLevel * oldSize, welCap);
        double ind = Calendar.PerQuarterToPerMonth(w.ModernizationBudget());

        // 実績＝国庫の台帳の月の差分（実際に払った額・入った額）。税の 4 行だけは種類ごとの実際の税収（台帳では税収の 1 区分）
        double aIncome = w.LastTaxIncomePerMonth.Value, aProg = w.LastTaxProgressivePerMonth.Value, aPoll = w.LastTaxPollPerMonth.Value, aLand = w.LastTaxLandPerMonth.Value;
        double Paid(TreasuryCat cat) => -LedgerOf(f, cat);   // 支出側の払った額（出る＝負なので反転）
        double In(TreasuryCat cat) => LedgerOf(f, cat);      // 収入側の入った額

        double pSoldier = w.LastSoldierPayPerMonth.Value;
        double navShare = w.NavalSpendShare.Value;   // 陸海の配分はエンジン自身の 1 本（NavyUpkeep/(Army+Navy)）
        double colonialUp = (w.LastColonialUpkeepPerMonth * w.FiscalFundingRatio).Value;
        double tribOut = Calendar.PerQuarterToPerMonth(w.LastTributeOutPerQuarter * w.FiscalFundingRatio.Value);
        double tribIn = Calendar.PerQuarterToPerMonth(w.LastTributeInPerQuarter);

        string indTip;
        {
            var (trad, modern, mass) = InterestGroups.Blocs(w);
            double tot = trad + modern + mass; double tradShare = tot > 1e-9 ? trad / tot : 0;
            indTip = $"旧体制の反対 -{DisplayFormat.Share(Math.Min(1.0, c.ModernizationOldGuardResist * tradShare))}　旧支配層の政治力 {DisplayFormat.Share(tradShare)}";
        }
        string adminTip = $"行政力　{DisplayFormat.Share(w.AdminEfficiency)}\n行政費　目標 {DisplayFormat.Share(w.Policy.AdminSpendingTarget)}　実効 {DisplayFormat.Share(w.AdminSpending)}"
            + string.Concat(PlayText.ExplainTerms(w, "AdminEfficiency").Select(t => "\n" + PlayText.SignedLine(t.Delta, t.Reason, v => DisplayFormat.Share(v))));
        string Pair(double target, double eff) => $"目標 {DisplayFormat.Share(target)}　実効 {DisplayFormat.Share(eff)}";

        BudgetLine Line(string key, string name, double fc, double ac, StageCell? st = null, string? tip = null, bool noFc = false, bool noAmounts = false, bool child = false)
            => new(key, name, fc, ac, st ?? StageCell.None, tip, noFc, noAmounts, child);

        var spend = new List<BudgetLine>
        {
            // 軍事費の段は合計だけに掛かる＝段と合計の額を 1 行に置く。陸海の 2 行は地理が決めた比で合計が割れた結果＝その内訳（合計には足さない）
            Line("military", "軍事費", mil + pSoldier, Paid(TreasuryCat.Army) + Paid(TreasuryCat.Navy), FiscalStageOf(w, nameof(NationPolicy.MilitarySpendingTarget))),
            Line("army", "うち陸軍維持費", mil * (1 - navShare) + pSoldier, Paid(TreasuryCat.Army), tip: Pair(w.Policy.ArmyUpkeepTarget, w.ArmyUpkeep.Value), child: true),
            Line("navy", "うち海軍維持費", mil * navShare, Paid(TreasuryCat.Navy), tip: Pair(w.Policy.NavyUpkeepTarget, w.NavyUpkeep.Value), child: true),
            Line("admin", "行政費", admin, Paid(TreasuryCat.Admin), Lever(w, "行政費", 0, 1, () => w.AdminSpending, () => w.Policy.AdminSpendingTarget, v => w.Policy.AdminSpendingTarget = v), adminTip),
            Line("education", "教育費", f.EduMonthly, Paid(TreasuryCat.Education), FiscalStageOf(w, nameof(NationPolicy.EducationSpendingTarget)), PlayText.EducationSpendingText(w)),
            Line("welfare", "社会保障", wel, Paid(TreasuryCat.Welfare), StageGate.Of(w, LawCatalog.In(LawSlot.Welfare).First()), Pair(w.Policy.SocialSecurityTarget, w.SocialSecurity)),
            // 開幕の 8% は段の格子（0・25・50・75・最大）に乗らない＝どの段も光らせず、ホバーに目標と実効の本当の値を出す
            Line("modernization", OffGrid(w.ModernizationFrac) ? $"近代化投資 {DisplayFormat.Share(w.ModernizationFrac)}" : "近代化投資", ind, Paid(TreasuryCat.Modernization), Lever(w, "近代化投資", 0, 1, () => w.ModernizationFrac, () => w.ModernizationFrac, v => w.ModernizationFrac = v, offGridDark: true), indTip + "\n" + Pair(w.ModernizationFrac, w.ModernizationFrac)),
            Line("interest", "国債の利払い", interest, Paid(TreasuryCat.Interest)),
            Line("pension", "年金の給付", pen, Paid(TreasuryCat.Pension)),
        };
        // 予測の列を持たない項は、予測の合計に足す見込みの額を Forecast に持つ（列は隠す）
        if (colonialUp > 1e-9 || Paid(TreasuryCat.ColonialUpkeep) > 1e-9) spend.Add(Line("colonial", "植民地の維持", colonialUp, Paid(TreasuryCat.ColonialUpkeep)));   // 予測の列も出す＝見出しの予測は見える行の和（V1）
        if (tribOut > 1e-9 || Paid(TreasuryCat.TributeOut) > 1e-9) spend.Add(Line("tributeOut", "宗主への貢納", tribOut, Paid(TreasuryCat.TributeOut)));
        // 国債の元本の出入り（2026-11-11 作者指示「国債の支払いなどもここに入れる」）＝国庫の台帳の実績（月の差分）をそのまま読む
        double repay = Paid(TreasuryCat.DebtRepay), borrow = In(TreasuryCat.Borrow);
        spend.Add(Line("debtRepay", "国債の償還", 0, Math.Max(0, repay), noFc: true));   // 起債と対で常に出す（B30）
        // 民間の外資の流れ（流入・流出）は国庫に入らない＝この表には置かない（国債と外資信用の表へ・V1）

        var income = new List<BudgetLine>
        {
            Line("incomeTax", "所得税", gIncome, aIncome, FiscalStageOf(w, nameof(NationPolicy.IncomeTaxTarget)), Pair(w.Policy.IncomeTaxTarget, w.TaxRate.Value)),
            Line("progressiveTax", "累進課税", gProg, aProg, FiscalStageOf(w, nameof(NationPolicy.ProgressiveTaxTarget)), Pair(w.Policy.ProgressiveTaxTarget, w.ProgressiveTax)),
            Line("pollTax", "人頭税", gPoll, aPoll, FiscalStageOf(w, nameof(NationPolicy.PollTaxTarget)), Pair(w.Policy.PollTaxTarget, w.PollTax)),
            Line("landTax", "土地税", gLand, aLand, FiscalStageOf(w, nameof(NationPolicy.LandTaxTarget)), Pair(w.Policy.LandTaxTarget, w.LandTax)),
            // 専売に率のレバーは無い（材を選ぶのみ＝生産ページの財ごと）。ここは対象の広さ・実効率・実績
            Line("monopoly", "専売", f.StateMonopolyMonthly, In(TreasuryCat.StateMonopoly), tip: $"負担 {DisplayFormat.Share(w.StateMonopolyBreadth)}　実効 {DisplayFormat.Share(w.StateMonopolyBreadth * c.StateMonopolyMargin)}"),
            Line("tariff", "関税", tariffGross, In(TreasuryCat.Tariff),
                 Lever(w, "関税", c.TariffFloor, TariffCeil, () => w.Tariff, () => w.Policy.TariffTarget, v => w.Policy.TariffTarget = Math.Clamp(v, c.TariffFloor, TariffCeil)),
                 Pair(w.Policy.TariffTarget, w.Tariff)),
        };
        if (tribIn > 1e-9 || In(TreasuryCat.TributeIn) > 1e-9) income.Add(Line("tributeIn", "属国からの貢納", tribIn, In(TreasuryCat.TributeIn)));
        income.Add(Line("debtBorrow", "国債の起債", 0, Math.Max(0, borrow), noFc: true));   // 償還と対で常に出す・外資の流入（民間）とは別の量（B30）

        // 台帳の区分のうち上の行に属さない物（工場の建設・補助金・戦費など）は、0 でない時だけ行を足す＝実績の合計が台帳と同じ物を数える
        var covered = new HashSet<TreasuryCat> { TreasuryCat.Army, TreasuryCat.Navy, TreasuryCat.Admin, TreasuryCat.Education, TreasuryCat.Welfare, TreasuryCat.Modernization, TreasuryCat.Interest,
            TreasuryCat.Pension, TreasuryCat.ColonialUpkeep, TreasuryCat.TributeOut, TreasuryCat.DebtRepay, TreasuryCat.Tax, TreasuryCat.StateMonopoly, TreasuryCat.Tariff, TreasuryCat.TributeIn, TreasuryCat.Borrow };
        foreach (var item in FiscalCatalog.Items)
        {
            if (covered.Contains(item.Cat)) continue;
            double d = LedgerOf(f, item.Cat);
            if (!(Math.Abs(d) >= 50.0)) continue;   // 表示の解像度（£0.1k 刻み）で 0 になる額は行にしない（下の「その他」へ畳む）
            if (item.Cat == TreasuryCat.WarCost && d > 0) income.Add(Line("reparationsIn", "賠償の受け取り", 0, d, noFc: true));   // 賠償を受け取った月は戦費の区分が正＝収入の側の行にする（戦費の行は払った額だけ）
            else if (item.Side == FiscalSide.Outlay) spend.Add(Line("cat." + item.Cat, item.Name, 0, -d, noFc: true));
            else income.Add(Line("cat." + item.Cat, item.Name, 0, d, noFc: true));
        }

        // 行にしなかった小さい区分と税の 4 行の食い違いは、台帳の収支との差として側ごとに「その他」へ畳む＝実績の合計の差が国庫の収支（台帳）と厳密に一致する（V1）
        double ActSum(List<BudgetLine> ls) => ls.Where(l => !l.NoAmounts && !l.Child).Sum(l => l.Actual);
        double rest = LedgerBalance(f) - (ActSum(income) - ActSum(spend));
        if (f.LedgerDiff.Any(x => x != 0) && rest >= 0.5) income.Add(Line("restIn", "その他", 0, rest, noFc: true));
        else if (f.LedgerDiff.Any(x => x != 0) && rest <= -0.5) spend.Add(Line("restOut", "その他", 0, -rest, noFc: true));

        // 四半期に 1 回だけ動く区分（貢納の出入り）は、台帳の月の差分では払った月に 3 か月分・他の月は 0 になる＝「/月」の列の単位に合わない。
        //   行の実績は予測と同じ月あたり（直近の四半期の額÷3）で出し、均した分と実際の差は同じ側の「その他」が受ける
        //   ＝側ごとの実績の合計は国庫の台帳の額のまま（ヘッダーの収支・上の「その他」の畳みと一致を保つ）。台帳がまだ立っていない（0 のまま）時は実際の額をそのまま出す。
        if (f.LedgerDiff.Any(x => x != 0))
        {
            Smooth(spend, "tributeOut", "restOut", "cat.OtherOut", tribOut, Paid(TreasuryCat.TributeOut));
            Smooth(income, "tributeIn", "restIn", "cat.OtherIn", tribIn, In(TreasuryCat.TributeIn));
        }

        double Fc(List<BudgetLine> ls) => ls.Where(l => !l.NoAmounts && !l.Child && !l.NoForecast).Sum(l => l.Forecast);
        return new BudgetResult(spend, income, Fc(spend), ActSum(spend), Fc(income), ActSum(income));
    }

    /// <summary>行 <paramref name="key"/> の実績を月あたりに均した額 <paramref name="avg"/> へ置き換え、実際の額 <paramref name="actual"/> との差を同じ側の <paramref name="restKey"/>（その他）へ入れる。行が無ければ何もしない。</summary>
    private static void Smooth(List<BudgetLine> side, string key, string restKey, string otherKey, double avg, double actual)
    {
        int i = side.FindIndex(l => l.Key == key);
        if (i < 0) return;
        double diff = actual - avg;
        side[i] = side[i] with { Actual = avg };
        if (!(Math.Abs(diff) >= 0.5)) { side[i] = side[i] with { Actual = actual }; return; }   // 差が表示の解像度より小さければ均さない（実際の額のまま）
        int r = side.FindIndex(l => l.Key == restKey);
        if (r < 0) r = side.FindIndex(l => l.Key == otherKey);   // 「その他」の行が無くても、同じ側に「その他の収入／支出」の行があればそこが受ける（2 行に分かれて並べない）
        if (r >= 0) side[r] = side[r] with { Actual = side[r].Actual + diff };
        else side.Add(new BudgetLine(restKey, "その他", 0, diff, StageCell.None, null, NoForecast: true));
    }

    /// <summary>国庫の台帳のその区分の月の差分（正＝入り・負＝出）。台帳がまだ立っていなければ 0。</summary>
    private static double LedgerOf(FiscalFlows f, TreasuryCat cat) => (int)cat < f.LedgerDiff.Count ? f.LedgerDiff[(int)cat] : 0;

    /// <summary>支出と収入の表の列。実績の列は国庫の台帳が書いた実際の額（支出＝払った額・収入＝入った額）。</summary>
    private static IReadOnlyList<Column<BudgetLine>> BudgetColumns(string actualName) =>
    [
        Col.Text<BudgetLine>("name", "項目", l => l.Name, l => l.Tip),
        Col.Stage<BudgetLine>("stage", "段階", l => l.Stage),
        Col.Number<BudgetLine>("forecast", "予測", Unit.MoneyPerMonth, l => l.Forecast, hide: l => l.NoAmounts || l.NoForecast),
        Col.Number<BudgetLine>("actual", actualName, Unit.MoneyPerMonth, l => l.Actual, hide: l => l.NoAmounts),
    ];

    /// <summary>支出の表の実績の列の名。</summary>
    public const string SpendActualName = "払った額";
    /// <summary>収入の表の実績の列の名。</summary>
    public const string IncomeActualName = "入った額";

    /// <summary>支出の表（予算の予想の支出の項）。</summary>
    public static Table<BudgetLine> Spend(BudgetResult r)
        => new("fiscal.spend", "支出", Page.FiscalMoney, (_, _) => r.Spend, l => l.Key, l => l.Name, BudgetColumns(SpendActualName));

    /// <summary>収入の表（予算の予想の収入の項）。</summary>
    public static Table<BudgetLine> Income(BudgetResult r)
        => new("fiscal.income", "収入", Page.FiscalMoney, (_, _) => r.Income, l => l.Key, l => l.Name, BudgetColumns(IncomeActualName));

    // ===== 国庫の台帳の合計 =====

    /// <summary>国庫の台帳の月の差分の合計（＝収支。ヘッダーの収支と厳密に一致する）。台帳の表は支出と収入の表へ吸収した（2026-11-12）。</summary>
    public static double LedgerBalance(FiscalFlows f) => f.LedgerDiff.Sum();

    // ===== 金融 =====

    /// <summary>貸出金利を出すか＝年率換算の係数が 0 のあいだは常に 0.0%（エンジンに量が無い）＝行ごと隠す。</summary>
    private static bool LoanRateVisible(World w) => w.Coef.LoanSpreadPerYear.Value > 0 || w.Coef.PolicyRateLoanWeight > 0;
    /// <summary>家計債務を出すか＝個人債務の主フラグが 0 のあいだは残高が常に 0＝行ごと隠す。</summary>
    private static bool HouseholdLoanVisible(World w) => w.Bank is not null && w.Coef.PopDebtDriven > 0;
    /// <summary>兌換カバー率を出すか＝金本位に関わるとき（加入中・志向あり・兌換停止中）だけ意味を持つ。</summary>
    private static bool CoverVisible(World w) => w.MonetaryEnabled && (w.OnGoldStandard || w.Policy.MonetaryTarget >= 2 || w.GoldStandardSuspendedInCrisis);
    /// <summary>政策金利を掴めるか＝兌換を維持しながら資本も自由なら金利は自分のものでない（エンジンの Government と同じ条件式）。</summary>
    private static bool InterestLeverEnabled(World w) => !(w.OnGoldStandard && !w.CapitalControlLaw && w.GoldStandardLaw);
    /// <summary>通貨改革を出すか＝発動条件を満たしたときだけ（押せない釦を置かない）。</summary>
    private static bool CurrencyReformVisible(World w)
    {
        var c0 = w.Coef;
        return !w.OnGoldStandard
            && w.CurrencyReformCooldown <= 0
            && Fiscal.CurrencyMemoryBroken(w.CumulativeDevaluation, w.NominalGdpPerMonth.Value, c0.CapitalFlightDistrustThreshold)
            && w.NoDevaluationMonths >= c0.CurrencyReformCalmMonths
            && w.FiscalFundingRatio >= c0.GoldStandardMinFunding;
    }

    private static IReadOnlyList<FinanceLine> FinanceLines(World w)
    {
        var rows = new List<FinanceLine>
        {
            new("price", "物価（開始時比）", PlayText.PriceLevelText(w), StageCell.None, null),
            new("fx", "為替", PlayText.ExchangeRateText(w), StageCell.None, null),
        };
        if (LoanRateVisible(w)) rows.Add(new("loan", "貸出金利", $"{w.LoanRatePerYear:P1}", StageCell.None, null));
        rows.Add(new("lossFinanced", "損失の負債化の累計", DisplayFormat.Money(w.LossFinanced), StageCell.None, null));
        if (HouseholdLoanVisible(w)) rows.Add(new("householdLoan", "家計債務", w.Bank is { } bank ? DisplayFormat.Money(bank.HouseholdLoans.Value) : "—", StageCell.None, null));
        if (CoverVisible(w))
            rows.Add(new("cover", "兌換カバー率", PlayText.CoverText(w), StageCell.None,
                w.BroadMoney.Value <= 1e-9 ? null : $"基礎貨幣 {DisplayFormat.Money(w.BaseMoney.Value)}\n広義貨幣 {DisplayFormat.Money(w.BroadMoney.Value)}"));
        // 政策金利＝5 段（兌換維持＋資本自由で自分のものでない時は段ごと掴めなくする。行は消さない＝いま金利がいくらかは見えている必要がある）
        // 無次元の値を百分率で出さない＝段の名で出す（現在の列・ホバーも同じ）。格子に乗らない値（金本位の自動の金利など）は最寄りの段の名
        var rate = Lever(w, "政策金利", 0, InterestCeil, () => w.InterestRate, () => w.Policy.InterestRateTarget, v => w.Policy.InterestRateTarget = Math.Min(v, InterestCeil), RateStageNames);
        // 錠の間はエンジンが目標を捨てて金利を自動で動かす（Government）＝段の列の目標は〈効いている値〉（今の金利）で出す。目標の欄（AI や過去の指定）を出し続けない
        if (!InterestLeverEnabled(w)) rate = rate with { Target = rate.Current, CanSelect = _ => false, Why = _ => "兌換維持中" };
        rows.Add(new("policyRate", "政策金利", StageName(rate, rate.Current), rate, $"目標 {StageName(rate, rate.Target)}　実効 {StageName(rate, rate.Current)}"));
        // 通貨制度と資本移動は法（議会を通る）＝法の表の行。金本位は経済の画面で扱う（作者指示 2026-08-11）
        foreach (var def in LawCatalog.In(LawSlot.Econ))
            if (def.IsVisible(w)) rows.Add(new("law." + def.Key, def.Name, "", StageGate.Of(w, def), null));
        if (CurrencyReformVisible(w))
        {
            // 新平価を実力に取るか旧平価（1.00）に戻すかが決断（ポワンカレ 1926 と英 1925 の分岐）。段の押し釦の形で 2 つ並べる（現在の段は無い）
            var reform = new StageCell([$"新平価 {w.ExchangeRate:F2}", "旧平価 1.00"], 0, -1, -1, _ => true, _ => null,
                level => w.RequestCurrencyReform(level == 0 ? w.ExchangeRate : 1.0));
            rows.Add(new("reform", "通貨改革", "", reform, null));
        }
        return rows;
    }

    /// <summary>金融の表（物価・為替・金利・通貨制度・資本移動・通貨改革）。</summary>
    public static Table<FinanceLine> Finance { get; } = new("fiscal.finance", "金融", Page.FiscalMoney, (_, w) => FinanceLines(w), l => l.Key, l => l.Name,
    [
        Col.Text<FinanceLine>("name", "項目", l => l.Name, l => l.Tip),
        Col.Text<FinanceLine>("value", "現在", l => l.Value),
        Col.Stage<FinanceLine>("stage", "段階", l => l.Stage),
    ]);

    // ===== 国債と外資信用 =====

    private static IReadOnlyList<DebtLine> DebtLines(World w, FiscalFlows f)
    {
        var rows = new List<DebtLine>();
        double gdpYear = w.NominalGdpPerMonth.Value > 1e-9 ? Math.Min(9.99, Calendar.ToGdpYear(w.GovDebt, Calendar.PerMonthToPerYear(w.NominalGdpPerMonth)).Value) : 0;
        rows.Add(new("debt", "国債", "残高", DisplayFormat.Money(w.GovDebt.Value)));
        rows.Add(new("debtGdp", "国債", "対年GDP", $"{DisplayFormat.Share(gdpYear)}"));
        rows.Add(new("bondRate", "国債", "金利", $"{DisplayFormat.Share(w.GovBondRatePerYear)}"));
        rows.Add(new("bondPrice", "国債", "債券価格", $"{DisplayFormat.Share(w.BondPrice)}"));
        if (w.ForeignDebt.Value > 1) rows.Add(new("foreignDebt", "国債", "うち対外", DisplayFormat.Money(w.ForeignDebt.Value)));
        if (w.AusterityIntensity > 0) rows.Add(new("austerity", "国債", "債務緊縮の強度", $"{DisplayFormat.Share(w.AusterityIntensity)}"));
        if (w.CumulativeDevaluation > 1) rows.Add(new("devaluation", "国債", "切下げ累計", DisplayFormat.Money(w.CumulativeDevaluation)));
        if (w.FiscalFundingRatio.Value < 1) rows.Add(new("funding", "国債", "支払い率", $"{DisplayFormat.Share(w.FiscalFundingRatio)}"));
        rows.Add(new("credit", "外資信用", "総合の評点", $"{DisplayFormat.Share(w.CreditRating)}"));
        rows.Add(new("creditDebt", "外資信用", "国債残高の評点", $"{DisplayFormat.Share(w.CreditDebtTerm)}"));
        rows.Add(new("creditGold", "外資信用", "兌換の評点", $"{DisplayFormat.Share(w.CreditGoldTerm)}"));
        rows.Add(new("creditFx", "外資信用", "為替の評点", $"{DisplayFormat.Share(w.CreditFxTerm)}"));
        rows.Add(new("creditBond", "外資信用", "国債の利回りの評点", $"{DisplayFormat.Share(w.CreditBondTerm)}"));
        // 民間の外資の流れは国庫に入らない＝支出・収入の表でなくここに置く（V1）。流入と流出は片方が £0
        rows.Add(new("foreignIn", "外資信用", "民間の外資流入", DisplayFormat.Money(f.ForeignInMonthly) + "/月"));
        rows.Add(new("foreignOut", "外資信用", "民間の外資流出", DisplayFormat.Money(f.ForeignOutMonthly) + "/月"));
        return rows;
    }

    /// <summary>国債と外資信用の表（群＝国債・外資信用）。外資の流れの月の標本は App が採る（<see cref="FiscalFlows"/>）。</summary>
    public static Table<DebtLine> DebtOf(FiscalFlows f) => new("fiscal.debt", "国債と外資信用", Page.FiscalMoney, (_, w) => DebtLines(w, f), l => l.Key, l => l.Name,
    [
        Col.Text<DebtLine>("name", "項目", l => l.Name),
        Col.Text<DebtLine>("value", "現在", l => l.Value),
    ], l => l.Group);

    /// <summary>外資の流れの標本が無い（0）時の国債と外資信用の表（表の目録の一覧用）。</summary>
    public static Table<DebtLine> Debt { get; } = DebtOf(FiscalFlows.Zero);
}
