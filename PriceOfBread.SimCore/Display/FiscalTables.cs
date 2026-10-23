namespace PriceOfBread.SimCore.Display;

/// <summary>財政の表が読む標本（App が四半期と月の境で採る差分）。教育費・専売益・外資の流れの実績は州の累積の差分＝今の盤面からは導けないので、採る側（App）が渡す。
/// <see cref="LedgerDiff"/>＝国庫の台帳の月の差分（<see cref="TreasuryCat"/> の添字）。</summary>
public sealed record FiscalFlows(double EduMonthly, double StateMonopolyMonthly, double ForeignInMonthly, double ForeignOutMonthly, IReadOnlyList<double> LedgerDiff)
{
    public static FiscalFlows Zero { get; } = new(0, 0, 0, 0, []);
}

/// <summary>予算の表の 1 行（支出か収入の 1 項）。<see cref="Forecast"/>＝予測（目標の段ベース）・<see cref="Actual"/>＝実効（今いま実際に流れている額）。
/// <see cref="Stage"/>＝その項の段の押し釦（無ければ <see cref="StageCell.None"/>）。<see cref="Tip"/>＝項の名前にカーソルを合わせた時の文。
/// <see cref="NoForecast"/>＝予測を出さない項（実績だけの項）。<see cref="NoAmounts"/>＝金額を出さない項（段だけの項）。</summary>
public sealed record BudgetLine(string Key, string Name, double Forecast, double Actual, StageCell Stage, string? Tip, bool NoForecast = false, bool NoAmounts = false);

/// <summary>予算の予想（支出と収入の項・合計）。<see cref="FiscalTables.Budget"/> が盤面を読んで組む。</summary>
public sealed record BudgetResult(IReadOnlyList<BudgetLine> Spend, IReadOnlyList<BudgetLine> Income,
    double SpendForecast, double SpendActual, double IncomeForecast, double IncomeActual)
{
    public static BudgetResult Empty { get; } = new([], [], 0, 0, 0, 0);
    /// <summary>予測の収支（収入−支出）。</summary>
    public double BalanceForecast => IncomeForecast - SpendForecast;
}

/// <summary>国庫の台帳の 1 行（区分の月の差分の絶対値と向き）。</summary>
public sealed record LedgerLine(string Key, string Name, double Amount, FiscalSide Side, bool Placeholder);

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

    /// <summary>レバーの 5 段。値＝そのレバー自身の下限と上限を 4 等分してエンジンの 0.05 格子へ乗せた 5 点。最上段だけ「最大」。
    /// 現在段＝施行値・目標段＝目標値の写像（税の段と同じ）。新しい上限・下限は作らない。</summary>
    private static StageCell Lever(World w, string title, double lo, double hi, Func<double> cur, Func<double> tgt, Action<double> set)
    {
        double Value(int k) => Government.Q5(lo + (hi - lo) * k / (double)LeverStages);
        int Stage(double v) => !(hi - lo > 1e-9) ? 0 : Math.Clamp((int)Math.Round((v - lo) / (hi - lo) * LeverStages, MidpointRounding.AwayFromZero), 0, LeverStages);   // 否定形＝NaN/退化した幅は最下段へ
        var labels = new string[LeverStages + 1];
        for (int i = 0; i < labels.Length; i++) labels[i] = i == LeverStages ? "最大" : Value(i).ToString("P0");
        return StageGate.Make(w, null, title, labels, 0, () => Stage(cur()), () => Stage(tgt()), v => set(Value(v)));
    }

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
        double tariffActMonthly = w.LastTariffRevenuePerClearing / (double)c.MonthsPerClearing;
        double interest = Calendar.PerYearToPerMonth(w.GovDebt.Value * w.GovBondRatePerYear.Value);
        // 軍事費の器（段×器の割合×使える歳入＋戦時段の割合×借入の余地）から兵士の給与の請求を引いた残り＝装備の上限（エンジンの Military.EnvelopeAt と同じ 1 本）
        double mil = Math.Max(0, Military.EnvelopeAt(w, c, Government.MilitaryTotalTarget(w, w.Policy)) - w.LastSoldierPayPerMonth.Value);
        double admin = c.AdminBudgetFrac.Value * w.Policy.AdminSpendingTarget * net * Fiscal.AdminDistanceMean(w);
        double welCap = (c.WelfareRevenueShare * w.FiscalCapacityPerMonth).Value;
        double wel = Math.Min(c.WelfareCostRatePerMonth * w.Policy.SocialSecurityTarget * massSize, welCap);
        double penLevel = w.Pension switch { ReformLevel.Full => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
        double pen = Math.Min(c.PensionCostRatePerMonth * penLevel * oldSize, welCap);
        double ind = Calendar.PerQuarterToPerMonth(w.ModernizationBudget());

        // 実効＝エンジンが予算表向けに書く実績診断（税は種類ごとの実際の税収・支出は当月の請求と国家の支払い率）
        double aIncome = w.LastTaxIncomePerMonth.Value, aProg = w.LastTaxProgressivePerMonth.Value, aPoll = w.LastTaxPollPerMonth.Value, aLand = w.LastTaxLandPerMonth.Value;
        double netA = aIncome + aProg + aPoll + aLand;
        double aMilGoods = w.LastMilitaryClaimPerMonth.Value;
        double aSoldier = (w.LastSoldierPayPerMonth * w.FiscalFundingRatio).Value;   // 兵の給与＝請求×国家の支払い率 r＝陸軍の行だけ
        double aMil = aMilGoods + aSoldier;
        double aAdmin = w.LastAdminClaimPerMonth.Value;
        double aWel = (w.LastWelfareCostPerMonth * w.FiscalFundingRatio).Value;
        double aPen = (w.LastPensionCostPerMonth * w.FiscalFundingRatio).Value;
        double aInterest = w.LastInterestPerMonth.Value;

        double navShare = w.NavalSpendShare.Value;   // 陸海の配分はエンジン自身の 1 本（NavyUpkeep/(Army+Navy)）
        double pSoldier = w.LastSoldierPayPerMonth.Value;
        double colonialUp = (w.LastColonialUpkeepPerMonth * w.FiscalFundingRatio).Value;
        double tribOut = Calendar.PerQuarterToPerMonth(w.LastTributeOutPerQuarter * w.FiscalFundingRatio.Value);
        double tribIn = Calendar.PerQuarterToPerMonth(w.LastTributeInPerQuarter);

        double lag = w.WorldTechLevel > 1e-9 ? Math.Clamp((w.WorldTechLevel - w.TechLevel) / w.WorldTechLevel, 0, 1) : 0;
        string indTip;
        {
            var (trad, modern, mass) = InterestGroups.Blocs(w);
            double tot = trad + modern + mass; double tradShare = tot > 1e-9 ? trad / tot : 0;
            indTip = $"効率 +{lag:P0}\n"
                   + $"技術(対世界最先端) {(1.0 - lag >= 0.995 ? "最先端" : (1.0 - lag).ToString("P0"))}\n"
                   + $"コスト倍率 ×{1.0 + c.ModernizationTechLagCost * lag:F2}　技術の遅れ +{c.ModernizationTechLagCost * lag:P0}\n"
                   + $"旧体制の反対 -{Math.Min(1.0, c.ModernizationOldGuardResist * tradShare):P0}　旧支配層の政治力 {tradShare:P0}";
        }
        string adminTip = $"行政力　{w.AdminEfficiency:P0}\n行政費　目標 {w.Policy.AdminSpendingTarget:P0}　実効 {w.AdminSpending:P0}"
            + string.Concat(PlayText.ExplainTerms(w, "AdminEfficiency").Select(t => "\n" + PlayText.SignedLine(t.Delta, t.Reason, v => v.ToString("P0"))));
        string creditTip = $"外資信用　{w.CreditRating:P0}\n" + CreditLines(w);
        string Pair(double target, double eff) => $"目標 {target:P0}　実効 {eff:P0}";

        BudgetLine Line(string key, string name, double fc, double ac, StageCell? st = null, string? tip = null, bool noFc = false, bool noAmounts = false)
            => new(key, name, fc, ac, st ?? StageCell.None, tip, noFc, noAmounts);

        var spend = new List<BudgetLine>
        {
            Line("army", "陸軍維持費", mil * (1 - navShare) + pSoldier, aMilGoods * (1 - navShare) + aSoldier, tip: Pair(w.Policy.ArmyUpkeepTarget, w.ArmyUpkeep.Value)),
            Line("navy", "海軍維持費", mil * navShare, aMilGoods * navShare, tip: Pair(w.Policy.NavyUpkeepTarget, w.NavyUpkeep.Value)),
            // 軍事費の段は合計だけに掛かる（陸海の 2 行は地理が決めた比で合計が割れた結果）＝金額は上の 2 行が名乗る
            Line("military", "軍事費", 0, 0, FiscalStageOf(w, nameof(NationPolicy.MilitarySpendingTarget)), noAmounts: true),
            Line("admin", "行政費", admin, aAdmin, Lever(w, "行政費", 0, 1, () => w.AdminSpending, () => w.Policy.AdminSpendingTarget, v => w.Policy.AdminSpendingTarget = v), adminTip),
            Line("education", "教育費", f.EduMonthly, f.EduMonthly, FiscalStageOf(w, nameof(NationPolicy.EducationSpendingTarget)), PlayText.EducationSpendingText(w)),
            Line("welfare", "社会保障", wel, aWel, StageGate.Of(w, LawCatalog.In(LawSlot.Welfare).First()), Pair(w.Policy.SocialSecurityTarget, w.SocialSecurity)),
            Line("modernization", "近代化投資", ind, ind, Lever(w, "近代化投資", 0, 1, () => w.ModernizationFrac, () => w.ModernizationFrac, v => w.ModernizationFrac = v), indTip),
            Line("interest", "国債の利払い", interest, aInterest),
            Line("pension", "年金", pen, aPen),
        };
        if (colonialUp > 1e-9) spend.Add(Line("colonial", "植民地の維持", 0, colonialUp, noFc: true));
        if (tribOut > 1e-9) spend.Add(Line("tributeOut", "宗主への貢納", 0, tribOut, noFc: true));
        spend.Add(Line("foreignOut", "外資の流出", 0, f.ForeignOutMonthly, noFc: true));

        var income = new List<BudgetLine>
        {
            Line("incomeTax", "所得税", gIncome, aIncome, FiscalStageOf(w, nameof(NationPolicy.IncomeTaxTarget)), Pair(w.Policy.IncomeTaxTarget, w.TaxRate.Value)),
            Line("progressiveTax", "累進課税", gProg, aProg, FiscalStageOf(w, nameof(NationPolicy.ProgressiveTaxTarget)), Pair(w.Policy.ProgressiveTaxTarget, w.ProgressiveTax)),
            Line("pollTax", "人頭税", gPoll, aPoll, FiscalStageOf(w, nameof(NationPolicy.PollTaxTarget)), Pair(w.Policy.PollTaxTarget, w.PollTax)),
            Line("landTax", "土地税", gLand, aLand, FiscalStageOf(w, nameof(NationPolicy.LandTaxTarget)), Pair(w.Policy.LandTaxTarget, w.LandTax)),
            // 専売に率のレバーは無い（材を選ぶのみ＝生産ページの財ごと）。ここは対象の広さ・実効率・実績
            Line("monopoly", "専売", f.StateMonopolyMonthly, f.StateMonopolyMonthly, tip: $"負担 {w.StateMonopolyBreadth:P0}　実効 {w.StateMonopolyBreadth * c.StateMonopolyMargin:P0}"),
            Line("tariff", "関税", tariffGross, tariffActMonthly,
                 Lever(w, "関税", c.TariffFloor, TariffCeil, () => w.Tariff, () => w.Policy.TariffTarget, v => w.Policy.TariffTarget = Math.Clamp(v, c.TariffFloor, TariffCeil)),
                 Pair(w.Policy.TariffTarget, w.Tariff)),
            Line("foreignIn", "外資の流入", 0, f.ForeignInMonthly, tip: creditTip, noFc: true),
        };
        if (tribIn > 1e-9) income.Add(Line("tributeIn", "属国からの貢納", tribIn, tribIn));

        double spendTotal = interest + mil + pSoldier + admin + wel + pen + ind + f.EduMonthly + f.ForeignOutMonthly + colonialUp + tribOut;
        double revTotal = net + tariffGross + f.StateMonopolyMonthly + f.ForeignInMonthly + tribIn;
        double spendActT = aInterest + aMil + aAdmin + aWel + aPen + ind + f.EduMonthly + f.ForeignOutMonthly + colonialUp + tribOut;
        double revActT = netA + tariffActMonthly + f.StateMonopolyMonthly + f.ForeignInMonthly + tribIn;
        return new BudgetResult(spend, income, spendTotal, spendActT, revTotal, revActT);
    }

    /// <summary>対外信用の内訳の 4 項（信用は 4 項の積＝どれが足を引っ張っているかは各項を並べれば読める）。</summary>
    private static string CreditLines(World w)
        => $"国債残高　{w.CreditDebtTerm:P0}\n兌換　{w.CreditGoldTerm:P0}\n為替　{w.CreditFxTerm:P0}\n国債の利回り　{w.CreditBondTerm:P0}";

    private static IReadOnlyList<Column<BudgetLine>> BudgetColumns { get; } =
    [
        Col.Text<BudgetLine>("name", "項目", l => l.Name, l => l.Tip),
        Col.Stage<BudgetLine>("stage", "段階", l => l.Stage),
        Col.Number<BudgetLine>("forecast", "予測", Unit.MoneyPerMonth, l => l.Forecast, hide: l => l.NoAmounts || l.NoForecast),
        Col.Number<BudgetLine>("actual", "実効", Unit.MoneyPerMonth, l => l.Actual, hide: l => l.NoAmounts),
    ];

    /// <summary>支出の表（予算の予想の支出の項）。</summary>
    public static Table<BudgetLine> Spend(BudgetResult r)
        => new("fiscal.spend", "支出", Page.FiscalMoney, (_, _) => r.Spend, l => l.Key, l => l.Name, BudgetColumns);

    /// <summary>収入の表（予算の予想の収入の項）。</summary>
    public static Table<BudgetLine> Income(BudgetResult r)
        => new("fiscal.income", "収入", Page.FiscalMoney, (_, _) => r.Income, l => l.Key, l => l.Name, BudgetColumns);

    // ===== 国庫の台帳 =====

    /// <summary>国庫の台帳の月の差分の合計（＝収支。ヘッダーの収支と厳密に一致する）。</summary>
    public static double LedgerBalance(FiscalFlows f) => f.LedgerDiff.Sum();

    private static IReadOnlyList<LedgerLine> LedgerLines(FiscalFlows f)
    {
        var outLines = new List<LedgerLine>(); var inLines = new List<LedgerLine>();
        for (int i = 0; i < f.LedgerDiff.Count && i < FiscalCatalog.Items.Count; i++)
        {
            double d = f.LedgerDiff[i];
            if (Math.Abs(d) < 50.0) continue;   // 表示の解像度（£0.1k 刻み）で 0 になる額は行にしない
            var item = FiscalCatalog.Items.First(x => (int)x.Cat == i);
            (d >= 0 ? inLines : outLines).Add(new LedgerLine("cat." + item.Cat, item.Name, Math.Abs(d), d >= 0 ? FiscalSide.Income : FiscalSide.Outlay, false));
        }
        // 開幕直後（最初の月の差分が立つ前）は両側が空＝「機能していない」ように見える。空の側は「—」の 1 行を置く
        if (outLines.Count == 0) outLines.Add(new LedgerLine("none.out", "—", 0, FiscalSide.Outlay, true));
        if (inLines.Count == 0) inLines.Add(new LedgerLine("none.in", "—", 0, FiscalSide.Income, true));
        return outLines.Concat(inLines).ToList();
    }

    /// <summary>国庫の台帳の表（群＝国庫の支出・国庫の収入）。区分の名前と向きは <see cref="FiscalCatalog"/>。</summary>
    public static Table<LedgerLine> Ledger(FiscalFlows f)
        => new("fiscal.ledger", "国庫の台帳", Page.FiscalMoney, (_, _) => LedgerLines(f), l => l.Key, l => l.Name,
        [
            Col.Text<LedgerLine>("name", "項目", l => l.Name),
            Col.Number<LedgerLine>("amount", "金額", Unit.MoneyPerMonth, l => l.Amount, hide: l => l.Placeholder),
        ], l => l.Side == FiscalSide.Outlay ? "国庫の支出" : "国庫の収入");

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
            && w.CurrencyReformCooldown <= 0 && w.NominalGdpPerMonth.Value > 1e-9
            && w.CumulativeDevaluation / w.NominalGdpPerMonth.Value > c0.CapitalFlightDistrustThreshold
            && w.NoDevaluationMonths >= c0.CurrencyReformCalmMonths
            && w.FiscalFundingRatio >= c0.GoldStandardMinFunding;
    }

    private static IReadOnlyList<FinanceLine> FinanceLines(World w)
    {
        var rows = new List<FinanceLine>
        {
            new("price", "物価", PlayText.PriceLevelText(w), StageCell.None, null),
            new("fx", "為替", PlayText.ExchangeRateText(w), StageCell.None, null),
        };
        if (LoanRateVisible(w)) rows.Add(new("loan", "貸出金利", $"{w.LoanRatePerYear:P1}", StageCell.None, null));
        rows.Add(new("lossFinanced", "損失の負債化", DisplayFormat.Money(w.LossFinanced), StageCell.None, null));
        if (HouseholdLoanVisible(w)) rows.Add(new("householdLoan", "家計債務", w.Bank is { } bank ? DisplayFormat.Money(bank.HouseholdLoans.Value) : "—", StageCell.None, null));
        if (CoverVisible(w))
            rows.Add(new("cover", "兌換カバー率", PlayText.CoverText(w), StageCell.None,
                w.BroadMoney.Value <= 1e-9 ? null : $"基礎貨幣 {DisplayFormat.Money(w.BaseMoney.Value)}\n広義貨幣 {DisplayFormat.Money(w.BroadMoney.Value)}"));
        // 政策金利＝5 段（兌換維持＋資本自由で自分のものでない時は段ごと掴めなくする。行は消さない＝いま金利がいくらかは見えている必要がある）
        var rate = Lever(w, "政策金利", 0, InterestCeil, () => w.InterestRate, () => w.Policy.InterestRateTarget, v => w.Policy.InterestRateTarget = Math.Min(v, InterestCeil));
        if (!InterestLeverEnabled(w)) rate = rate with { CanSelect = _ => false };
        rows.Add(new("policyRate", "政策金利", $"{w.InterestRate:P0}", rate, $"目標 {w.Policy.InterestRateTarget:P0}　実効 {w.InterestRate:P0}"));
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

    private static IReadOnlyList<DebtLine> DebtLines(World w)
    {
        var rows = new List<DebtLine>();
        double gdpYear = w.NominalGdpPerMonth.Value > 1e-9 ? Math.Min(9.99, Calendar.ToGdpYear(w.GovDebt, Calendar.PerMonthToPerYear(w.NominalGdpPerMonth)).Value) : 0;
        rows.Add(new("debt", "国債", "残高", DisplayFormat.Money(w.GovDebt.Value)));
        rows.Add(new("debtGdp", "国債", "対年GDP", $"{gdpYear:P0}"));
        rows.Add(new("bondRate", "国債", "金利", $"{w.GovBondRatePerYear:P0}"));
        rows.Add(new("bondPrice", "国債", "債券価格", $"{w.BondPrice:P0}"));
        if (w.ForeignDebt.Value > 1) rows.Add(new("foreignDebt", "国債", "うち対外", DisplayFormat.Money(w.ForeignDebt.Value)));
        if (w.AusterityIntensity > 0) rows.Add(new("austerity", "国債", "緊縮", $"{w.AusterityIntensity:P0}"));
        if (w.CumulativeDevaluation > 1) rows.Add(new("devaluation", "国債", "切下げ累計", DisplayFormat.Money(w.CumulativeDevaluation)));
        if (w.FiscalFundingRatio.Value < 1) rows.Add(new("funding", "国債", "支払い率", $"{w.FiscalFundingRatio:P0}"));
        rows.Add(new("credit", "外資信用", "総合", $"{w.CreditRating:P0}"));
        rows.Add(new("creditDebt", "外資信用", "国債残高", $"{w.CreditDebtTerm:P0}"));
        rows.Add(new("creditGold", "外資信用", "兌換", $"{w.CreditGoldTerm:P0}"));
        rows.Add(new("creditFx", "外資信用", "為替", $"{w.CreditFxTerm:P0}"));
        rows.Add(new("creditBond", "外資信用", "国債の利回り", $"{w.CreditBondTerm:P0}"));
        return rows;
    }

    /// <summary>国債と外資信用の表（群＝国債・外資信用）。</summary>
    public static Table<DebtLine> Debt { get; } = new("fiscal.debt", "国債と外資信用", Page.FiscalMoney, (_, w) => DebtLines(w), l => l.Key, l => l.Name,
    [
        Col.Text<DebtLine>("name", "項目", l => l.Name),
        Col.Text<DebtLine>("value", "現在", l => l.Value),
    ], l => l.Group);
}
