namespace PriceOfBread.SimCore;

public static class Climate
{
    private const double RiceTropicLatMax = 32.0;
    private const double RiceMonsoonMinLon = 60.0;
    private const double RiceMonsoonMaxLat = 41.0;
    private const double PotatoBeltMinLat = 45.0;
    private const double MaizeBeltMaxLon = -30.0;

    public static bool RiceSuitable(double lat, double lon) => System.Math.Abs(lat) <= RiceTropicLatMax || (lon >= RiceMonsoonMinLon && lat >= 0.0 && lat <= RiceMonsoonMaxLat);

    public static bool PotatoBelt(double lat) => lat >= PotatoBeltMinLat;

    public static bool MaizeBelt(double lon) => lon <= MaizeBeltMaxLon;

    public static bool CottonSuitable(double lat) => System.Math.Abs(lat) <= CottonBeltLatMax;
    private const double CottonBeltLatMax = 37.0;

    public static bool StapleSelfSufficient(int goodId, double lat, double lon) => goodId switch
    {
        Catalog.WheatId or Catalog.VegetableId => true,
        Catalog.RiceId => RiceSuitable(lat, lon),
        Catalog.PotatoId => PotatoBelt(lat),
        Catalog.MaizeId => MaizeBelt(lon),
        _ => false,
    };

    public static int WinterFrozenQuarters(double lat, double lon)
    {
        if (lat >= 63 && lon >= 36 && lon <= 70) return 2;
        if (lat is >= 59 and <= 66 && lon >= 12.5 && lon <= 31) return 2;
        if (lat is >= 54 and <= 66 && lon >= 12.5 && lon <= 31) return 1;
        if (lat >= 42 && lon >= 128 && lon <= 165) return 1;
        if (lat >= 45 && lon >= -75 && lon <= -60) return 1;
        return 0;
    }

    public static bool PortFrozen(double lat, double lon, int quarterOfYear)
    {
        int n = WinterFrozenQuarters(lat, lon);
        return n >= 2 ? quarterOfYear is 0 or 3 : n == 1 && quarterOfYear == 0;
    }

    public static (double Lat, double Lon) CityCoord(string city) => Places.Coord(city);
}
