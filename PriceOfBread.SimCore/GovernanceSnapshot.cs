namespace PriceOfBread.SimCore;

/// <summary>
/// Runner の freshboard 診断専用：経済を助走で整えつつ、法律・財政・国庫を「開始年初期」へ戻すための退避/復元
/// （本番の <see cref="MultiNationSim.WarmupEconomy"/> は <see cref="World.WarmupFrozen"/> で暦・人口・識字・技術・政治を止めるので本クラスを呼ばない）。
/// 助走の直前に <see cref="Capture"/> し、助走の直後に <see cref="RestoreTo"/> する。
/// 政治（意識・闘争度・政体）は助走中 PoliticsEnabled=false で凍結、外交（戦争・緊張・植民地）は
/// 助走中 <see cref="MultiNationSim.DiplomacyFrozen"/> で凍結されるため、ここでは対象外（不変なので戻す必要がない）。
/// 経済（価格・在庫・生産能力・工業化・制度）は助走後の状態を保持する。
/// </summary>
public sealed class GovernanceSnapshot
{
    // 法律・政治（現在値）
    private EducationLaw _eduLaw; private Suffrage _suffrage;   // ★親軍法（_militarism）は2026-08-18に撤去＝軍国主義は PoliticalQuantities の4実体へ分解（法ではなくなったので退避対象そのものが消えた）
    // ~~⚠申し送り：4実体が読む ConscriptionLaw / Bureaucracy は**ここで退避していない**（助走で両者がドリフトすると練度と文民制度の弁が助走後も戻らない）。~~
    //   → 2026-09-30 (119) 助走そのものが法を動かさなくなった（Government.Step の施行は World.WarmupFrozen の間は走らない）＝両者も助走で動かない＝穴は閉じた。
    //   以後この退避の法の欄は助走の前後で同じ値を書き戻すだけ（目標＝Policy と国庫・銀行・検地台帳と行政力は助走で動く＝ここの復元に意味が残る）。
    private CensorshipLaw _censorship; private LaborUnionLaw _laborUnion; private WomenRightsLaw _womenRights;   // ★女性の権利（統合法・2026-08-10）＝旧・財産権/参政権の2boolを1段へ（教育権は旧は捕捉漏れ＝統合で自動的に含まれる）
    private ReformLevel _cornLaws, _serfEmancipation, _landReform;   // 改革法の現在値（穀物法・農奴解放・土地改革）＝初期の史実差別化を助走後も保持
    private WorkHours _workHours;   // 労働時間の現在値（A-1#2）＝助走後も初期値を保持
    private bool _childLaborAllowed;   // 児童労働法の目標（Policy）＝助走後の連続合法度の復元アンカー（倍率排除④）
    private double _freedom; private Polity _polity; private double _baseCoercion, _coercion, _legitimacy;
    private CrownForm _crown; private SeizureForm _seizure; private double _parliamentControl;   // P2登録簿＋議会統制力（助走整合2026-07-11）
    // 財政（現在値）
    private double _navalUpkeepShare;   // ★陸海の維持費の按分（2026-08-04）＝軍事費だけ戻すと海軍が消える
    private double _social, _admin, _pollTax, _landTax, _military, _tariff,
                   _interestRate, _eduSpending, _taxRate, _fiscalFunding, _progressiveTax;
    // 税制の解禁・行政力
    private bool _landTaxUnlocked; private double _adminPowerSmoothed;
    // 国庫
    private double _treasury;
    // 政策（スライダーの目標値）
    private NationPolicy _policy = new();

    public static GovernanceSnapshot Capture(World w)
    {
        var s = new GovernanceSnapshot
        {
            _eduLaw = w.EducationLaw, _suffrage = w.Suffrage,
            _censorship = w.Censorship, _laborUnion = w.LaborUnion, _womenRights = w.WomenRights,
            _cornLaws = w.CornLaws, _serfEmancipation = w.SerfEmancipation, _landReform = w.LandReform, _workHours = w.WorkHours,
            _childLaborAllowed = w.Policy.ChildLaborAllowed,
            _freedom = w.Freedom, _polity = w.Polity, _baseCoercion = w.BaseCoercion, _coercion = w.Coercion, _legitimacy = w.Legitimacy,
            _crown = w.Crown, _seizure = w.Seizure, _parliamentControl = w.ParliamentControl,
            _social = w.SocialSecurity, _admin = w.AdminSpending, _pollTax = w.PollTax, _landTax = w.LandTax, _progressiveTax = w.ProgressiveTax,
            _military = w.MilitarySpending, _navalUpkeepShare = w.NavalSpendShare, _tariff = w.Tariff, _interestRate = w.InterestRate,
            _eduSpending = w.EducationSpending, _taxRate = w.TaxRate, _fiscalFunding = w.FiscalFundingRatio,
            _landTaxUnlocked = w.LandTaxUnlocked, _adminPowerSmoothed = w.AdminPowerSmoothed,
            _treasury = w.Treasury,
            _policy = w.Policy.Clone(),
        };
        return s;
    }

    public void RestoreTo(World w)
    {
        w.EducationLaw = _eduLaw; w.Suffrage = _suffrage;
        w.Censorship = _censorship; w.LaborUnion = _laborUnion; w.WomenRights = _womenRights;
        w.CornLaws = _cornLaws; w.SerfEmancipation = _serfEmancipation; w.LandReform = _landReform; w.WorkHours = _workHours;
        w.WorkHoursValue = _workHours.Hours();   // 実労働時間も法定上限のアンカーへ戻す（助走中は政治凍結で不動だが整合のため・倍率排除②）
        w.WomenWageRetention = w.WomenPropertyLaw ? 1.0 : 0.0;        // 賃金保持も財産権の段のアンカーへ戻す（倍率排除④・派生プロパティ＝上の復元で確定済み）
        w.ChildLaborLegal = _childLaborAllowed ? 1.0 : 0.0;          // 児童労働の合法度も法のアンカーへ戻す（倍率排除④）
        w.Freedom = _freedom; w.Polity = _polity; w.BaseCoercion = _baseCoercion; w.Coercion = _coercion; w.Legitimacy = _legitimacy;
        w.Crown = _crown; w.Seizure = _seizure; w.ParliamentControl = _parliamentControl;
        w.SocialSecurity = _social; w.AdminSpending = _admin; w.PollTax = _pollTax; w.LandTax = _landTax; w.ProgressiveTax = _progressiveTax;
        w.SetMilitarySpending(_military, _navalUpkeepShare); w.Tariff = _tariff; w.InterestRate = _interestRate;
        w.EducationSpending = _eduSpending; w.TaxRate = _taxRate; w.FiscalFundingRatio = _fiscalFunding;
        w.LandTaxUnlocked = _landTaxUnlocked; w.AdminPowerSmoothed = _adminPowerSmoothed;
        w.RebaseTreasury(_treasury);   // 助走で動いた分は台帳ごと捨てる（暦の外＝歴史に属さない）
        w.Policy.CopyFrom(_policy);
        if (w.Bank is { } bank) { bank.Loans = 0; bank.HouseholdLoans = 0; bank.Confidence = 1.0; bank.Heat = 0; }   // 1783＝負債なし・健全な信用（HouseholdLoansも同じOutstandingLoans恒等式の片割れ＝wave2b）
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.Debt = 0;   // 債務の記録も同じく（家計の与信＝POP の債務の和・S3b の輸入の為替手形）
    }
}
