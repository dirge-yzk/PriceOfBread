using System.Collections.Generic;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 地理テーブルの網羅検査（2026-08-01・アフリカ勢力の追加で実際に踏んだ穴の番人）。
///
/// ⚠**きっかけ**：ダホメ王国を world.json に足したとき、`Climate.CityCoord` と `landmass.json` への登録を忘れた。
/// どちらも**例外も警告も出さない**：
///   ・`Climate.CityCoord` の既定は `_ => (0, 0)` ＝**緯度0経度0＝ギニア湾の真上**。州が静かに「海の上」に置かれる
///     （割譲の距離判定・気候帯の主食・海盆の帰属が全部狂うが、どこにも赤が出ない）。
///   ・`Landmass.Of` は最近傍で拾うので**たまたま正しい大陸に落ちることがある**＝運に依存する。
/// ★史実参照表は `HistoricalReferenceTests.参照表はworldjsonの全国家と一対一` が番人になっていて、追加漏れを即座に落とした。
///   地理テーブルにも同じ番人を置く＝**州を足したら地理も足さないとテストが赤になる**。
/// </summary>
public class GeographyCoverageTests
{
    [Fact]
    public void 全州の座標がClimateに登録されている()
    {
        foreach (var cfg in WorldData.Nations())
            foreach (var p in cfg.Provinces)
            {
                var (lat, lon) = Climate.CityCoord(p.City);
                Assert.False(lat == 0 && lon == 0,
                    $"州「{p.City}」（{cfg.Name}）の座標が Climate.CityCoord に無い＝既定の (0,0)＝ギニア湾の真上に置かれる。" +
                    "world.json に州を足したら Climate.cs の CityCoord にも必ず足すこと");
            }
    }

    [Fact]
    public void 全州と全首都が陸塊テーブルに登録されている()
    {
        foreach (var cfg in WorldData.Nations())
        {
            Assert.True(Landmass.IsRegistered(cfg.Name),
                $"国「{cfg.Name}」が landmass.json に無い＝海越え判定（IsSeaCrossing）が最近傍の他国の陸塊を継ぐ");
            foreach (var p in cfg.Provinces)
                Assert.True(Landmass.IsRegistered(p.City),
                    $"州「{p.City}」（{cfg.Name}）が landmass.json に無い＝割譲の海越え判定が最近傍を継ぐ（運で正しくなる）");
        }
    }

    /// <summary>★首都座標の番人（2026-09-03・キューA-01／出典 トリアージ 2-A 1-3）。
    /// ⚠**2026-09-03 の作者裁定（J-003）で番人の意味が変わった**：「**首都（史実に基づく・表示のみ）と、経済中心地は別でよい**」。
    /// ⇒ 2つの座標が**食い違うこと自体は仕様**になった（エンジンの距離は経済中心地＝1番目の州を読み、
    /// world.json の首都は表示専用に降格＝エンジンは1番目の州の座標を中心にする（旗 CenterIsFirstProvince は (1089) で焼いた）。表示専用の写し `World.DisplayCapitalLat/Lon` は読み手0で 2026-09-26 に削除）。
    /// ★よってこの番人が今なお守っているのは「**表示用の首都が地点表に載っていること**」＝地図に描ける、という一点だけである
    /// （⚠モロッコのラバトが places.json に登録すらされていなかった、という実害を捕まえたのがこの検査）。</summary>
    /// <summary>（旧説明）
    /// world.json は国に首都座標（NationConfig.Latitude/Longitude）を持ち、同じ地点が places.json（Places.Coord）にも州として載る。
    /// ⚠現状の食い違いは0.0km＝完全な重複。片方だけ直したときに**黙って**ずれる型（このプロジェクトで最も再発している事故）を固定する。
    /// ⚠★2026-09-03 の実測で分かったこと＝**首都の概念が2つある**。エンジンは Regions[0] を首都として読む（露＝モスクワ）が、
    /// world.json の首都座標は露＝サンクトペテルブルク（634km 離れる）。よって「Provinces[0] と一致」では番人が即赤になる。
    /// ⚠★さらに実測＝コーカンドは**首都都市が自国の州でない**（唯一の州はタシュケント・167km）。places.json では コーカンド は
    /// 植民地化の標的として登録されている。よって「自国の州と一致」でも赤になる。
    /// ★番人の最終形＝「首都座標は places.json の**登録地点のどれか**と一致する」＝どちらの表を片方だけ動かしても赤（ドリフト検知）で、
    /// 2つの首都概念の併存（露＝モスクワ/ペテルブルク）と標的都市の首都（コーカンド）は許す。どちらを正とするかは作者の裁定＝J-003。
    /// ⚠実測の最悪はモロッコ166km（首都座標＝ラバトだが places.json にラバトが無い＝最近傍はフェズ）＝閾値200km はこの実測から決めた
    /// （100km にすると導入時点で赤＝番人として機能しない）。★places.json への地点追加は PathGraph の節点を増やす＝挙動が動くので、
    /// 「番人を緑にするために地点を足す」ことはしない（データの是正は裁定 J-003 の後）。</summary>
    [Fact]
    public void 首都座標がworldjsonとplacesで一致する()
    {
        foreach (var cfg in WorldData.Nations())
        {
            if (cfg.Provinces.Length == 0) continue;
            double best = double.MaxValue; string bestPlace = "";
            foreach (var pl in Places.All)
            {
                double d = Geo.DistanceKm(cfg.Latitude, cfg.Longitude, pl.Lat, pl.Lon);
                if (d < best) { best = d; bestPlace = pl.Name; }
            }
            if (bestPlace.Length == 0) continue;
            Assert.True(best < 200.0,   // ★200km＝実測の最悪（モロッコ166km・コーカンド167km）を通し、片方を動かせば必ず超える幅
                $"国「{cfg.Name}」の首都座標 world.json（{cfg.Latitude},{cfg.Longitude}）に一致する地点が places.json に無い" +
                $"（最も近い地点「{bestPlace}」でも {best:F0}km）＝片方だけ動かした痕跡");
        }
    }

    [Fact]
    public void 陸塊テーブルの座標がworldjsonと矛盾しない()
    {
        // 同じ地点が2つの表に載るので、片方だけ直したときのドリフトを検知する（100km を超えたら別地点）。
        foreach (var cfg in WorldData.Nations())
            foreach (var p in cfg.Provinces)
            {
                if (!Landmass.IsRegistered(p.City)) continue;   // 上のテストが担当
                var (lat, lon) = Climate.CityCoord(p.City);
                if (lat == 0 && lon == 0) continue;             // 同上
                var (mlat, mlon) = Landmass.CoordOf(p.City);
                Assert.True(Geo.DistanceKm(lat, lon, mlat, mlon) < 100.0,
                    $"州「{p.City}」の座標が Climate（{lat},{lon}）と landmass.json（{mlat},{mlon}）で食い違っている");
            }
    }

    /// <summary>★重複地点の番人（2026-08-10・作者「ボスポラスとコンスタンティノープル、東京と江戸のような、完全に重なっている州は統合したい」）。
    ///
    /// ⚠**きっかけ**：植民地「ボスポラス」(41.0,29.0) がオスマンの州「イスタンブール」(41.01,28.98) と2.4km＝実質同一座標で並んでいた
    /// （オスマンの首都行(41,29)とは距離0.0km）。二重登録は例外も警告も出さないまま次の3つを壊す：
    ///   ・陸グラフ（PathGraph）に**双子ノード**が立ち、k近傍の枠を食い合って辺の張り方が変わる。
    ///   ・地図に同じ点が州○と植民地○で二重に描かれる。
    ///   ・州昇格機構（2026-08-09）が「ボスポラスがオスマンの州へ昇格（入植者101）」＝帝国の中枢に入植者が入る絵を出す。
    /// ★閾値10km＝「同じ町」。実測の最短対は横浜↔江戸の27.1km（次いでパドヴァ↔ヴェネツィア36.1km・京都↔大阪43.3km）なので、
    ///   本当に別の町が誤検知される余地はない。⚠capital 行は州行と重なるのが正常（同一テーブルの2種類の行）＝走査から除く。</summary>
    [Fact]
    public void 陸塊テーブルに完全に重なる州や植民地が無い()
    {
        var pts = new List<Landmass.Point>();
        foreach (var p in Landmass.PointsAll)
            if (p.Kind is "state" or "colony") pts.Add(p);   // これが PathGraph のノード源＝重複はそのまま双子ノードになる
        for (int i = 0; i < pts.Count; i++)
            for (int j = i + 1; j < pts.Count; j++)
            {
                double km = Geo.DistanceKm(pts[i].Lat, pts[i].Lon, pts[j].Lat, pts[j].Lon);
                Assert.True(km >= 10.0,
                    $"「{pts[i].Name}」({pts[i].Kind}) と「{pts[j].Name}」({pts[j].Kind}) が {km:F1}km ＝同じ場所に二重登録されている。" +
                    "どちらか一方へ統合すること（扼者の host は州でも解決できる＝スエズ host=カイロ と同型）");
            }
    }

    /// <summary>★扼者の host は州でも成立する（2026-08-10）：ボスポラス海峡の扼者＝オスマンの**州**イスタンブール。
    /// 植民地「ボスポラス」を廃してこの host へ移したので、海峡の所有解決が植民地経路に依存していないことを固定する
    /// （<c>MultiNationSim.ResolveOwner</c> は植民地→州の順で引く。スエズ host="カイロ" が同型の前例）。</summary>
    [Fact]
    public void ボスポラス海峡の扼者はオスマンの州イスタンブールが持つ()
    {
        var sim = new MultiNationSim(WorldData.Nations(), seed: 1789);
        sim.Run(1);   // 扼者の解決は毎ステップ（助走中も通る）
        var cp = sim.Diplomacy.Chokepoints.Find(x => x.Id == "bosporus");
        Assert.NotNull(cp);
        Assert.Equal("イスタンブール", cp!.Host);
        Assert.NotNull(cp.Owner);
        Assert.Equal("オスマン", cp.Owner!.Name);
        // host は州として存在し、植民地としては存在しない（＝統合が効いている）
        var ott = sim.Nations.Find(n => n.World.SelfPower?.Name == "オスマン");
        Assert.NotNull(ott);
        Assert.Contains(ott!.World.Regions, r => r.Name == "イスタンブール");
        foreach (var p in sim.Diplomacy.Powers)
            Assert.DoesNotContain(p.ColonyList, col => col.Name == "ボスポラス");
    }
}
