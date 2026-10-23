using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (512) 線 pv2：州のデータの追加の番人（段0＝座標とマスクの規則）。事前登録 measurements/pv2_20261023_prereg.txt。
///
/// ── 守るもの ──────────────────────────────────────────────────────────
/// 地図の州の当たり（App の MapFill.HitAt と種の吸着 Spread）は、場所の座標を〈海路用マスクの陸〉へ 5 環以内で吸着させる
/// （環の走査は上の行から・左から。見つからなければ種は捨てられる）。座標が海にあると、吸着先が環の運に依る
/// （コペンハーゲンの旧座標 55.68,12.57 は 3 環でスコーネ〔ユーラシアの陸続き〕に吸着し、デンマークの色がスコーネに塗られた）。
/// この規則を App を起動せずに Runner/テストから確かめるため、規則の写しをここに置く（App は触らない＝UI の作り直しが別に走っている）。
/// ⚠写しなので App の MapFill.Nearest が変わったらここも直す（環の半径 5・走査順・海路用マスク）。
/// ⚠格子の添字は Landmask.IsLand と同じ式（浮動小数のまま切り捨て）＝格子の境の座標はずれる。新しく置く座標は格子の中心にする。
/// </summary>
public class PlaceAdditionTests
{
    private const double Step = Landmask.Step;

    /// <summary>MapFill.Spread の吸着の写し。戻り値＝(吸着先の格子の中心の緯度, 経度, 環の番号)・捨てられるなら null。</summary>
    internal static (double Lat, double Lon, int Ring)? Seed(double lat, double lon)
    {
        int y0 = (int)((90.0 - lat) / Step), x0 = (int)Math.Floor((lon + 180.0) / Step);
        static double CLat(int y) => 90.0 - (y + 0.5) * Step;
        static double CLon(int x) => -180.0 + (x + 0.5) * Step;
        bool Land(int y, int x)
        {
            if (y < 0 || y >= Landmask.Ny) return false;
            x %= Landmask.Nx; if (x < 0) x += Landmask.Nx;
            return Landmask.IsLand(CLat(y), CLon(x));
        }
        if (Land(y0, x0)) return (CLat(y0), CLon(x0), 0);
        for (int r = 1; r <= 5; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Abs(dy) != r && Math.Abs(dx) != r) continue;
                    if (Land(y0 + dy, x0 + dx)) return (CLat(y0 + dy), CLon(x0 + dx), r);
                }
        return null;
    }

    /// <summary>基線で種が捨てられる 3 件（海路用マスクに島が無い）。マスクの焼き直し（App --bakemask）は UI の側＝この回は直さない。直ったらこの名簿を空にする。</summary>
    private static readonly string[] KnownDropped = ["ウィンドワード諸島", "小アンティル", "琉球"];
    /// <summary>基線で同じ格子に乗る組（先着の種が当たりを取る）。新しい場所はこの組を増やさない。</summary>
    private static readonly string[][] KnownCollisions = [["ケープタウン", "南アフリカ"], ["コンゴ", "ボマ"], ["ハーグ", "ロッテルダム"], ["中核ベンガル", "内陸ベンガル"]];

    [Fact]
    public void 種が捨てられる場所は既知の名簿だけ_新しい場所は必ず島に乗る()
    {
        var dropped = Places.All.Where(p => Seed(p.Lat, p.Lon) is null).Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(KnownDropped.OrderBy(n => n, StringComparer.Ordinal).ToArray(), dropped);
    }

    [Fact]
    public void 同じ格子に乗る種の組は既知の4組だけ()
    {
        var by = new Dictionary<(double, double), List<string>>();
        foreach (var p in Places.All.OrderBy(p => p.Name, StringComparer.Ordinal))
            if (Seed(p.Lat, p.Lon) is { } s) { if (!by.TryGetValue((s.Lat, s.Lon), out var l)) by[(s.Lat, s.Lon)] = l = []; l.Add(p.Name); }
        var found = by.Values.Where(l => l.Count > 1).Select(l => string.Join("/", l.OrderBy(n => n, StringComparer.Ordinal))).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        var known = KnownCollisions.Select(a => string.Join("/", a.OrderBy(n => n, StringComparer.Ordinal))).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        Assert.Equal(known, found);
    }

    /// <summary>吸着の写しが規則どおり（手で作った 3 点）：陸の格子は環 0・海に隣る点は環 1 以上・遠い外洋は捨てられる。
    /// 期待は Landmask でなく既知の地点（アジャクシオ＝陸・太平洋の真ん中＝捨てる・旧コペンハーゲン＝環 3 でスコーネ側）から作る。</summary>
    [Fact]
    public void 吸着の写しが規則どおり()
    {
        Assert.Equal(0, Seed(41.95, 8.75)!.Value.Ring);                    // コルシカの島の中
        Assert.Null(Seed(0.0, -140.0));                                    // 太平洋の真ん中
        var cph = Seed(55.68, 12.57)!.Value;                               // 旧コペンハーゲン
        Assert.Equal(3, cph.Ring);
        Assert.True(cph.Lon > 12.6, $"旧座標の吸着先はスコーネ側（経度 12.6 より東）のはず：{cph.Lon}");
    }

    // ───────────────────────── 足した場所の表（事前登録 measurements/pv2_20261023_prereg.txt [2][3]） ─────────────────────────
    // 期待はデータのファイルでなくここ（事前登録の表）から作る。足す部品ごとに 1 行ずつ増やす。

    /// <summary>Kind＝state（州・国の人口の中の配分 Share）／colony（国の人口の外から足す PopK）。Nation＝所有国の world.json の名前。</summary>
    private sealed record NewPlace(string Name, string Kind, string Nation, double Lat, double Lon, string Landmass, double Share, double PopK, int[] Res, string Culture);

    private static readonly NewPlace[] Added =
    [
        new NewPlace("アジャクシオ", "state", "フランス", 41.95, 8.75, "corsica", 0.0046, 0, [11, 16, 19], "コルシカ"),
        new NewPlace("ラス・パルマス", "state", "スペイン", 28.15, -15.75, "gran-canaria", 0.0168, 0, [16, 24], "スペイン"),
        new NewPlace("モルッカ諸島", "colony", "ネーデルラント", -3.05, 128.15, "seram", 0, 100, [26], ""),
        new NewPlace("バハマ諸島", "colony", "イギリス", 24.95, -78.15, "andros", 0, 4, [6, 20], ""),
        new NewPlace("パラワン", "colony", "スペイン", 9.95, 118.35, "palawan", 0, 10, [11], ""),
        new NewPlace("チロエ", "colony", "スペイン", -41.85, -73.85, "chiloe", 0, 27, [11, 20, 105], ""),
        // @ADDED
    ];

    /// <summary>コペンハーゲン：座標を海から島の中（シェランの格子の中心）へ。places・landmass の 2 行・world.json の国の 2 値の 4 箇所が同じ。旧座標はスコーネへ吸着した。</summary>
    [Fact]
    public void コペンハーゲンの種はシェランの島に乗りスコーネに吸着しない()
    {
        var (lat, lon) = Places.Coord("コペンハーゲン");
        Assert.Equal((55.85, 12.15), (lat, lon));
        Assert.True(Landmask.IsLand(lat, lon) && Landmask.IsLandForRoads(lat, lon), "自分の座標が両方のマスクで陸の格子");
        var s = Seed(lat, lon)!.Value;
        Assert.Equal(0, s.Ring);
        Assert.True(s.Lon < 12.3, $"シェランは経度 12.2 より西・スコーネは 12.7 より東：{s.Lon}");
        Assert.Equal((lat, lon), Landmass.CoordOf("コペンハーゲン"));
        Assert.Equal((lat, lon), Landmass.CoordOf("デンマーク"));   // capital 行（最初に一致する行）
        var dk = WorldData.ByName("デンマーク");
        Assert.Equal((lat, lon), (dk.Latitude, dk.Longitude));
        Assert.Equal("zealand-dk", Landmass.LandmassOf("コペンハーゲン"));
        // 旧座標からの移動は 35km 未満（座標の規則：本当の位置に最も近い両マスクの陸の格子の中心）
        Assert.InRange(Geo.DistanceKm(55.68, 12.57, lat, lon), 30.0, 35.0);
    }

    [Fact]
    public void 足した場所は座標表と陸塊表で同じ座標_両マスクの陸の格子の中心_別の種と同じ格子にならない()
    {
        foreach (var n in Added)
        {
            var (lat, lon) = Places.Coord(n.Name);
            Assert.Equal((n.Lat, n.Lon), (lat, lon));
            Assert.Equal((lat, lon), Landmass.CoordOf(n.Name));
            Assert.Equal(n.Landmass, Landmass.LandmassOf(n.Name));
            Assert.True(Landmask.IsLand(lat, lon) && Landmask.IsLandForRoads(lat, lon), $"{n.Name}：両方のマスクで陸の格子");
            Assert.Equal(0, Seed(lat, lon)!.Value.Ring);
            // 格子の中心＝小数 2 桁で 0.05 の奇数倍（境の座標は浮動小数でずれる）
            Assert.Equal(5, (int)Math.Round(Math.Abs(lat) * 100) % 10);
            Assert.Equal(5, (int)Math.Round(Math.Abs(lon) * 100) % 10);
            Assert.Equal(n.Kind, Places.All.Single(p => p.Name == n.Name).Kind);
            // 10km の番人の再掲（最寄りの別の場所まで 100km 以上＝島の中で他の種を食わない）
            double near = Places.All.Where(p => p.Name != n.Name).Min(p => Geo.DistanceKm(lat, lon, p.Lat, p.Lon));
            Assert.True(near > 100.0, $"{n.Name}：最寄りの別の場所まで {near:F0}km");
        }
    }

    /// <summary>盤面に一意にあり、開幕の人口が表どおり。州は〈本国の州の人口の和×share〉・植民地は popK。本国の州の人口の和は、州を足しても変わらない
    /// （基準＝州の分割も植民地も無い同じ国〔Provinces=[]・InitialColonies=[]〕の人口。州の share の和が 1 なら一致）。国の外から足すのは植民地の popK だけ。</summary>
    [Fact]
    public void 足した場所は盤面に一意にあり人口が表どおり_本国の人口の和は州を足しても変わらない()
    {
        var nations = Added.Select(a => a.Nation).Distinct().ToArray();
        if (nations.Length == 0) return;
        foreach (var name in nations)
        {
            var cfg = WorldData.ByName(name);
            var flat = cfg.ShallowClone(); flat.Provinces = []; flat.InitialColonies = [];
            double reference = new MultiNationSim([flat], seed: 83) { DiplomacyFrozen = true }.Nations[0].World.Regions.Sum(r => r.SumPopulation());
            var w = new MultiNationSim([cfg], seed: 83) { DiplomacyFrozen = true }.Nations[0].World;
            var home = cfg.Provinces.Select(p => p.City).ToHashSet();
            double homeSum = w.Regions.Where(r => home.Contains(r.Name)).Sum(r => r.SumPopulation());
            Assert.Equal(reference, homeSum, reference * 1e-9);
            Assert.Equal(1.0, cfg.Provinces.Sum(p => p.Share), 1e-9);
            Assert.Equal(reference + cfg.InitialColonies.Sum(c => c.PopK), w.Regions.Sum(r => r.SumPopulation()), reference * 1e-9);
            foreach (var a in Added.Where(x => x.Nation == name))
            {
                var rs = w.Regions.Where(r => r.Name == a.Name).ToArray();
                Assert.Single(rs);
                double want = a.Kind == "state" ? reference * a.Share : a.PopK;
                Assert.Equal(want, rs[0].SumPopulation(), want * 1e-9);
                if (a.Kind == "colony") Assert.Equal(RegionTier.Colonial, rs[0].Tier);
                if (a.Culture.Length > 0) Assert.Equal(a.Culture, rs[0].Culture);
            }
        }
    }

    /// <summary>コルシカの文化の札はラテンの群（サルデーニャ・カタルーニャの前例）。群に無い札は独自の群＝仏の自州が仏と〈穏やかな反目〉になる。</summary>
    [Fact]
    public void コルシカの文化の札はラテンの群に入る()
    {
        Assert.Equal("ラテン", Diplomacy.CultureGroup("コルシカ"));
        Assert.Equal(Diplomacy.CultureGroup("フランス"), Diplomacy.CultureGroup("コルシカ"));
        Assert.Equal(Diplomacy.CultureGroup("サルデーニャ"), Diplomacy.CultureGroup("コルシカ"));
        Assert.NotEqual(Diplomacy.CultureGroup("ドイツ"), Diplomacy.CultureGroup("コルシカ"));   // 対照：他の群は混ざらない
    }
}
