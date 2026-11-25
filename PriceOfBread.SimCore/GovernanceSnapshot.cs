namespace PriceOfBread.SimCore;

public sealed class GovernanceSnapshot
{
    private EducationLaw _eduLaw; private Suffrage _suffrage;
    private CensorshipLaw _censorship; private LaborUnionLaw _laborUnion; private WomenRightsLaw _womenRights;
    private ReformLevel _cornLaws, _serfEmancipation, _landReform;
    private WorkHours _workHours;
    private bool _childLaborAllowed;
    private double _freedom; private Polity _polity; private double _baseCoercion, _coercion, _legitimacy;
    private CrownForm _crown; private SeizureForm _seizure; private double _parliamentControl;
    private Share _navalUpkeepShare;
    private Share _military;
    private double _social, _admin, _pollTax, _landTax, _tariff,
                   _interestRate, _eduSpending, _progressiveTax;
    private Share _taxRate, _fiscalFunding;
    private double _treasury;
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
            _treasury = w.Treasury.Value,
            _policy = w.Policy.Clone(),
        };
        return s;
    }

    public void RestoreTo(World w)
    {
        Instruments.LawstepSideBegin(w);   // (1372) 計器 lawstep_s
        w.EducationLaw = _eduLaw; w.Suffrage = _suffrage;
        w.Censorship = _censorship; w.LaborUnion = _laborUnion; w.WomenRights = _womenRights;
        w.CornLaws = _cornLaws; w.SerfEmancipation = _serfEmancipation; w.LandReform = _landReform; w.WorkHours = _workHours;
        w.WorkHoursValue = _workHours.Hours();
        w.WomenWageRetention = w.WomenPropertyLaw ? 1.0 : 0.0;
        w.ChildLaborLegal = _childLaborAllowed ? 1.0 : 0.0;
        w.Freedom = _freedom; w.Polity = _polity; w.BaseCoercion = _baseCoercion; w.Coercion = _coercion; w.Legitimacy = _legitimacy;
        w.Crown = _crown; w.Seizure = _seizure; w.ParliamentControl = _parliamentControl;
        if (Instruments.On) Instruments.CrownSource(w, Instruments.CrownSrcUnknown);
        w.SocialSecurity = _social; w.AdminSpending = _admin; w.PollTax = _pollTax; w.LandTax = _landTax; w.ProgressiveTax = _progressiveTax;
        w.SetMilitarySpending(_military, _navalUpkeepShare); w.Tariff = _tariff; w.InterestRate = _interestRate;
        w.EducationSpending = _eduSpending; w.TaxRate = _taxRate; w.FiscalFundingRatio = _fiscalFunding;
        w.RebaseTreasury(_treasury);
        w.Policy.CopyFrom(_policy);
        if (w.Bank is { } bank) { bank.Loans = default; bank.HouseholdLoans = default; bank.Confidence = 1.0; bank.Heat = 0; }
        foreach (var (_, p) in w.AllPopsWithRegion()) p.Debt = default;
        Instruments.LawstepSideEnd(w, 5);   // (1372)
    }
}
