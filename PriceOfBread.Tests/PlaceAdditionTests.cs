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
}
