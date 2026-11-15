namespace PriceOfBread.SimCore;

public sealed class NationConfig
{
    public NationConfig ShallowClone() => (NationConfig)MemberwiseClone();

    public string Name = "";
    public double Nobility, Peasant, Worker, Bourgeois, Professional;
    public double Merchant = 400;
    public double Artisan = 800;
    public double Literacy = 0.23;
    public double ClergyFrac = 0.40, IntelligentsiaFrac = 0.35;
    public double PeasantSubsistence = 0.7;
    public Polity Polity = Polity.Absolutist;
    public EducationLaw Education = EducationLaw.Religious;
    public double Coercion = 400;
    public double Treasury = 200_000;
    public double TreasuryMonths = 0;
    public double GovDebtToGdpYear = 0;
    public double GovDebt = 0;
    public int PollTaxStage = 1;
    public double LandTaxStage = 1;
    public string? LandTaxBasis = null;
    public int OpeningStateMonopolyTier = 0;
    public BureaucracyLaw Bureaucracy = BureaucracyLaw.Spoils;
    public double NationalConsciousness0 = -1;
    public double InitialMassMilitancy = 0;
    public double InitialTariff = 0.4;
    public double Industrialization = 0.0;
    public (string Name, int[] Resources, double PopK)[] InitialColonies = [];
    public Suffrage InitialSuffrage = Suffrage.None;
    public CensorshipLaw InitialCensorship = CensorshipLaw.StatePressOnly;
    public ReformLevel InitialCornLaws = ReformLevel.Full;
    public ReformLevel InitialSerfEmancipation = ReformLevel.Full;
    public ImmigrationLaw InitialImmigration = ImmigrationLaw.Open;
    public ConscriptionLaw InitialConscription = ConscriptionLaw.Selective;
    public MonetaryStandardLaw InitialMonetaryLaw = MonetaryStandardLaw.Silver;
    public string[] KnownDiscoveries = [];
    public ProvinceSpec[] Provinces = [];
    public double[]? Rule = null;
    public double InitialInterestRate = 0.25;
    public double Latitude, Longitude;
    public double FarmLordShare = 0.6;
    public double[]? LandOwnership = null;
    public int CulturalGoodId = Catalog.BreadId;

    public Scenario ToScenario(ulong seed, bool sharedWorld = false) => new()
    {
        SharedWorld = sharedWorld, Seed = seed, StartYear = Calendar.StartYear, Kind = ScenarioKind.Nation, NationConfig = this,
        PoliticsEnabled = true, InitialPolity = Polity, FiscalEnabled = true,
        InitialPollTax = Government.TaxValue(PolicyAxis.PollTax, PollTaxStage), InitialLandTax = Government.TaxValue(PolicyAxis.LandTax, LandTaxStage), InitialTreasury = Treasury, InitialGovDebt = GovDebt, InitialBureaucracy = Bureaucracy, InitialNationalConsciousness = NationalConsciousness0, InitialMassMilitancy = InitialMassMilitancy, BaseCoercion = Coercion,
        EducationLaw = Education, ExternalPressure = 0.3,
        InitialInterestRate = InitialInterestRate,
        InitialTariff = InitialTariff,
        InitialSuffrage = InitialSuffrage, InitialCensorship = InitialCensorship,
        InitialCornLaws = InitialCornLaws, InitialSerfEmancipation = InitialSerfEmancipation, InitialImmigration = InitialImmigration,
        InitialConscription = InitialConscription,
        InitialMonetaryLaw = InitialMonetaryLaw,
        InitialDiscoveries = KnownDiscoveries,
    };


    public static NationConfig France() => WorldData.ByName("フランス");

    public static NationConfig Japan() => WorldData.ByName("日本");

    public static NationConfig Britain() => WorldData.ByName("イギリス");

    public static NationConfig America() => WorldData.ByName("アメリカ");

    public static NationConfig Russia() => WorldData.ByName("ロシア");

    public static NationConfig Prussia() => WorldData.ByName("プロイセン");

    public static NationConfig Ottoman() => WorldData.ByName("オスマン");

    public static NationConfig Austria() => WorldData.ByName("オーストリア");

    public static NationConfig Netherlands() => WorldData.ByName("ネーデルラント");

    public static NationConfig Iberia() => WorldData.ByName("スペイン");

    public static NationConfig Sardinia() => WorldData.ByName("サルデーニャ＝ピエモンテ");
}

public sealed record ProvinceSpec(string City, double Share, int[] Resources, string Culture = "", string Religion = "", bool Coastal = false,
    double ArableLand = 0, double IndShare = -1, double[]? Rule = null);
