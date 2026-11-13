namespace PriceOfBread.SimCore;

public enum Suffrage { None, Census, AdultMale, Universal }

public enum ReformLevel { None, Limited, Full }

public enum CensorshipLaw { StatePressOnly, Censorship, FreePress }

public enum LaborUnionLaw { Illegal, Controlled, Tolerated }

public enum WorkHours { Eight, Ten, Twelve, Unlimited }

public enum ConscriptionLaw { Volunteer, Selective, Universal }

public enum SecretBallotLaw { Open, Secret }

public enum BureaucracyLaw { Spoils, MeritExam, Professional }

public enum BudgetLaw { Decree, Partial, Parliamentary }

public enum CommandLaw { Crown, Split, Parliament }

public enum WomenRightsLaw { None, Property, Education, Suffrage }

public enum ImmigrationLaw { Open, Selective, Closed }

public enum ColonialSuffrageLaw { None, OwnNationals, Full }

public enum MigrantNativeSuffrageLaw { None, Granted }

public enum SocialSecurityLaw { PoorLaw, WorkersComp, Unemployment, Pension }

public enum StateReligionLaw { StateChurch, InnerFreedom, ReligiousFreedom, StateAtheism }

public enum AntitrustLaw { None, CartelBan, Trustbust }

public enum GuildLaw { Full, Limited, None }

public enum MinimumWageLaw { None, Subsistence, Living, Sufficiency }

public enum InheritanceLaw { None, Low, Mid, High, All }

public enum MonetaryStandardLaw { Fiat, Silver, Gold }

public enum NationalizedSector { Railways = 0, Mines = 1, Military = 2, Oil = 4, Electricity = 5, All = 6, None = -1 }

public static class Nationalization
{
    public static NationalizedSector SectorOf(int goodId) => goodId switch
    {
        Catalog.LocomotiveId => NationalizedSector.Railways,
        Catalog.IronOreId or Catalog.CoalId or Catalog.CopperOreId or Catalog.LeadOreId or Catalog.GoldOreId or Catalog.SulfurId or Catalog.LimestoneId or Catalog.SilicaSandId or Catalog.ClayId or Catalog.SilverId => NationalizedSector.Mines,
        Catalog.ArtilleryId or Catalog.RifleId or Catalog.AmmunitionId or Catalog.WarshipId or Catalog.TankId or Catalog.DreadnoughtId or Catalog.MilitaryAircraftId or Catalog.ChemicalWeaponId => NationalizedSector.Military,
        Catalog.NewspaperId or Catalog.RadioId => NationalizedSector.All,
        Catalog.CrudeOilId => NationalizedSector.Oil,
        Catalog.ElectricityId => NationalizedSector.Electricity,
        _ => goodId >= 0 && goodId < Catalog.Goods.Length
             && Catalog.Goods[goodId].Tier is GoodTier.Staple or GoodTier.Daily or GoodTier.Luxury or GoodTier.Intermediate or GoodTier.Producer
            ? NationalizedSector.All : NationalizedSector.None,
    };
    public static bool IsNationalized(int goodId, int mask)
    {
        var s = SectorOf(goodId);
        return s != NationalizedSector.None && (mask & (1 << (int)s)) != 0;
    }
}

public static class WorkHoursExtensions
{
    public static double Hours(this WorkHours wh) => wh switch
    {
        WorkHours.Eight => 8.0, WorkHours.Ten => 10.0, WorkHours.Twelve => 12.0, _ => 14.0,
    };
}

public sealed class NationPolicy
{
    public EducationLaw EducationLaw = EducationLaw.None;
    public Suffrage Suffrage = Suffrage.None;
    public CensorshipLaw Censorship = CensorshipLaw.StatePressOnly;
    public LaborUnionLaw LaborUnion = LaborUnionLaw.Illegal;
    public bool ChildLaborAllowed = true;
    public WorkHours WorkHours = WorkHours.Unlimited;
    public WomenRightsLaw WomenRights = WomenRightsLaw.None;
    public ReformLevel CornLaws = ReformLevel.None;
    public ReformLevel LandReform = ReformLevel.None;
    public ReformLevel SerfEmancipation = ReformLevel.None;
    public ConscriptionLaw ConscriptionLaw = ConscriptionLaw.Selective;
    public SecretBallotLaw SecretBallot = SecretBallotLaw.Open;
    public BureaucracyLaw Bureaucracy = BureaucracyLaw.MeritExam;
    public ImmigrationLaw ImmigrationRestriction = ImmigrationLaw.Open;
    public SpecialistExitLaw SpecialistExitLaw = SpecialistExitLaw.Free;
    public ColonialSuffrageLaw ColonialSuffrage = ColonialSuffrageLaw.None;
    public MigrantNativeSuffrageLaw MigrantNativeSuffrage = MigrantNativeSuffrageLaw.None;
    public SocialSecurityLaw SocialSecurityLaw = SocialSecurityLaw.PoorLaw;
    public StateReligionLaw StateReligion = StateReligionLaw.StateChurch;
    public AntitrustLaw Antitrust = AntitrustLaw.None;
    public GuildLaw Guild = GuildLaw.Full;
    public MinimumWageLaw MinimumWage = MinimumWageLaw.None;
    public InheritanceLaw Inheritance = InheritanceLaw.None;
    public bool WartimeRationing = false;
    public bool MilitaryExportBan = false;
    public bool CivilianArmsBan = false;
    public bool NationalizeRailways = false;
    public bool NationalizeMines = false;
    public bool NationalizeMilitary = false;
    public bool NationalizeOil = false;
    public bool NationalizeElectricity = false;
    public bool NationalizeAll = false;

    public double IncomeTaxTarget;
    public double PollTaxTarget;
    public double LandTaxTarget;
    public double ProgressiveTaxTarget;
    public double SocialSecurityTarget;
    public double EducationSpendingTarget;
    public double AdminSpendingTarget = 0.5;
    public ReformLevel OpiumPossession = ReformLevel.None;
    public bool OpiumExportBan;
    public double CoercionTarget;
    public double ArmyUpkeepTarget = 0.27;
    public double NavyUpkeepTarget = 0.03;
    public double MilitarySpendingTarget
    {
        get => PoliticalQuantities.Clamp01(ArmyUpkeepTarget + NavyUpkeepTarget);
        set
        {
            double t = PoliticalQuantities.Clamp01(value), cur = ArmyUpkeepTarget + NavyUpkeepTarget;
            double s = cur > 1e-9 ? NavyUpkeepTarget / cur : 0.1;
            NavyUpkeepTarget = t * s; ArmyUpkeepTarget = t - NavyUpkeepTarget;
        }
    }
    public double TariffTarget;
    public double InterestRateTarget;
    public int MonetaryTarget;
    public bool CapitalControlTarget;

    public NationPolicy Clone() { var c = new NationPolicy(); c.CopyFrom(this); return c; }

    public void CopyFrom(NationPolicy o)
    {
        EducationLaw = o.EducationLaw; Suffrage = o.Suffrage; Censorship = o.Censorship; LaborUnion = o.LaborUnion;
        ChildLaborAllowed = o.ChildLaborAllowed; WorkHours = o.WorkHours; WomenRights = o.WomenRights;
        IncomeTaxTarget = o.IncomeTaxTarget; PollTaxTarget = o.PollTaxTarget; LandTaxTarget = o.LandTaxTarget; ProgressiveTaxTarget = o.ProgressiveTaxTarget;
        SocialSecurityTarget = o.SocialSecurityTarget; EducationSpendingTarget = o.EducationSpendingTarget;
        AdminSpendingTarget = o.AdminSpendingTarget; OpiumPossession = o.OpiumPossession; OpiumExportBan = o.OpiumExportBan; ImmigrationRestriction = o.ImmigrationRestriction; SpecialistExitLaw = o.SpecialistExitLaw; CoercionTarget = o.CoercionTarget; ArmyUpkeepTarget = o.ArmyUpkeepTarget; NavyUpkeepTarget = o.NavyUpkeepTarget;
        ColonialSuffrage = o.ColonialSuffrage; MigrantNativeSuffrage = o.MigrantNativeSuffrage;

        TariffTarget = o.TariffTarget; InterestRateTarget = o.InterestRateTarget;
        MonetaryTarget = o.MonetaryTarget; CapitalControlTarget = o.CapitalControlTarget;
        CornLaws = o.CornLaws; LandReform = o.LandReform; SerfEmancipation = o.SerfEmancipation;
        ConscriptionLaw = o.ConscriptionLaw; SecretBallot = o.SecretBallot; Bureaucracy = o.Bureaucracy; SocialSecurityLaw = o.SocialSecurityLaw; StateReligion = o.StateReligion; Antitrust = o.Antitrust; Guild = o.Guild; MinimumWage = o.MinimumWage; Inheritance = o.Inheritance; WartimeRationing = o.WartimeRationing; MilitaryExportBan = o.MilitaryExportBan;
        CivilianArmsBan = o.CivilianArmsBan;
        NationalizeRailways = o.NationalizeRailways; NationalizeMines = o.NationalizeMines; NationalizeMilitary = o.NationalizeMilitary;
        NationalizeOil = o.NationalizeOil; NationalizeElectricity = o.NationalizeElectricity; NationalizeAll = o.NationalizeAll;
    }
}
