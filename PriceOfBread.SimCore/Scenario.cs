namespace PriceOfBread.SimCore;

public enum ScenarioKind
{
    SelfSufficientFarm,
    AgrarianToUrban,
    FrontierDevelopment,
    RegionalFamine,
    LaissezFaire,
    ProductionChain,
    FullChain,
    France,
    Nation,
}

public sealed class Scenario
{
    public ulong Seed { get; init; }
    public int StartYear { get; init; } = Calendar.StartYear;
    public ScenarioKind Kind { get; init; } = ScenarioKind.SelfSufficientFarm;

    public EducationLaw EducationLaw { get; init; } = EducationLaw.None;
    public double Freedom { get; init; } = 0.0;
    public double EducationSpending { get; init; } = 0.0;

    public bool PoliticsEnabled { get; init; } = false;
    public Polity InitialPolity { get; init; } = Polity.Absolutist;
    public double ExternalPressure { get; init; } = 0.0;
    public double BaseCoercion { get; init; } = 0.0;

    public int ShockQuarterIndex { get; init; } = -1;
    public double ShockMagnitude { get; init; } = 0.0;

    public bool FiscalEnabled { get; init; } = false;
    public double TaxRate { get; init; } = 0.0;
    public double InitialPollTax { get; init; } = 0.0;
    public double InitialLandTax { get; init; } = 0.0;

    public double InitialTreasury { get; init; } = 0.0;
    public double InitialGovDebt { get; init; } = 0.0;
    public BureaucracyLaw InitialBureaucracy { get; init; } = BureaucracyLaw.MeritExam;
    public double InitialMassMilitancy { get; init; } = 0;

    public double FactoryRentShare { get; init; } = 0.7;

    public NationConfig? NationConfig { get; init; }
    public double InitialInterestRate { get; init; }
    public double InitialTariff { get; init; }
    public Suffrage InitialSuffrage { get; init; } = Suffrage.None;
    public CensorshipLaw InitialCensorship { get; init; } = CensorshipLaw.StatePressOnly;
    public ReformLevel InitialCornLaws { get; init; } = ReformLevel.None;
    public ImmigrationLaw InitialImmigration { get; init; } = ImmigrationLaw.Open;
    public ConscriptionLaw InitialConscription { get; init; } = ConscriptionLaw.Selective;
    public MonetaryStandardLaw InitialMonetaryLaw { get; init; } = MonetaryStandardLaw.Fiat;
    public string[] InitialDiscoveries { get; init; } = [];
    public ReformLevel InitialSerfEmancipation { get; init; } = ReformLevel.Full;

    public List<Region>? PrebuiltRegions { get; init; }

    public Coefficients Coef { get; init; } = new();
    public bool SharedWorld { get; init; }
}
