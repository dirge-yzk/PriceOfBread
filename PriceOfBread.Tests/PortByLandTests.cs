using System;
using System.Collections.Generic;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(770) 港の選び方のやり直し（線 c6pr）の機構の番人。内陸州が使う港を選ぶ純関数 <see cref="Simulation.PortChoice"/>（byLand を引数で明示＝旗の読みに依らない）：
/// 港ごとの eff＝〈陸路が有限 ? 陸路 : 大圏〉の最小（①島の港は大圏の eff で比べ、遠い陸の港に負けない ②陸路が全て∞なら大圏の最小 ③陸路が有限どうしなら陸路の最小 ④同率は先着 ⑤byLand=false は旧と同じ港）と、
/// 配線 <see cref="Simulation.WirePortDistances"/> が旗に従って選んだ港と km を欄に書く（⑥）。座標は州と港の実在の座標、陸路は実の陸グラフ（山脈の迂回・海を渡れない）。</summary>
public class PortByLandTests
{
    static (double Lat, double Lon, string Name) P(string name) { var c = Climate.CityCoord(name); return (c.Lat, c.Lon, name); }

    [Fact]
    public void 島の港は大圏の距離で比べ遠い陸の港に負けない()
    {
        var paris = Climate.CityCoord("パリ");
        var london = P("ロンドン");
        var ports = new List<(double Lat, double Lon, string Name)> { (london.Lat, london.Lon, "島の港"), P("マルセイユ") };   // 島の港＝ロンドンの座標（大圏 344 km・陸路 ∞）・マルセイユは大圏 659 km・陸路 668 km
        Assert.True(double.IsPositiveInfinity(PathGraph.LandDistanceKm(paris.Lat, paris.Lon, london.Lat, london.Lon)));
        var r = Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: true);
        Assert.Equal("島の港", r.Name);
        Assert.InRange(r.GcKm, 330, 360);
        Assert.Equal(r.GcKm, r.LandKm);                 // 陸路 ∞ は大圏で退避（0 を残さない）
        Assert.Equal("島の港", Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: false).Name);
    }

    [Fact]
    public void 島の港が陸の港より遠ければ陸の港を選ぶ()
    {
        var paris = Climate.CityCoord("パリ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("ダブリン"), P("マルセイユ") };   // ダブリン＝大圏 781 km・陸路 ∞ ／ マルセイユ＝陸路 668 km
        var r = Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: true);
        Assert.Equal("マルセイユ", r.Name);
        Assert.InRange(r.LandKm, 660, 680);
        ports.Reverse();
        Assert.Equal("マルセイユ", Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: true).Name);
    }

    [Fact]
    public void 全ての港への陸路が無限なら大圏の最小を選び旧と同じ港と距離になる()
    {
        var paris = Climate.CityCoord("パリ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("ダブリン"), P("ロンドン") };   // どちらも陸路 ∞（大圏 781 km と 344 km）
        var old = Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: false);
        var nw = Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: true);
        Assert.Equal("ロンドン", old.Name);
        Assert.Equal(old, nw);                          // 旧と同じ港・同じ km
        Assert.Equal(nw.GcKm, nw.LandKm);
    }

    [Fact]
    public void 陸路が有限の港どうしなら陸路の最小を選びラサは天津になる()
    {
        var lhasa = Climate.CityCoord("ラサ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("広州"), P("天津") };
        // 大圏では広州（約 2,317 km）が近いが、陸路では広州が遠回り（約 3,231 km）で天津が短い（約 2,754 km）
        Assert.Equal("広州", Simulation.PortChoice(lhasa.Lat, lhasa.Lon, ports, byLand: false).Name);
        var byLand = Simulation.PortChoice(lhasa.Lat, lhasa.Lon, ports, byLand: true);
        Assert.Equal("天津", byLand.Name);
        Assert.InRange(byLand.LandKm, 2700, 2800);
        Assert.InRange(byLand.GcKm, 2550, 2650);
        ports.Reverse();
        Assert.Equal("天津", Simulation.PortChoice(lhasa.Lat, lhasa.Lon, ports, byLand: true).Name);
    }

    [Fact]
    public void 同率は並びの先の港()
    {
        var paris = Climate.CityCoord("パリ");
        var m = P("マルセイユ");
        var ports = new List<(double Lat, double Lon, string Name)> { (m.Lat, m.Lon, "甲"), (m.Lat, m.Lon, "乙") };   // 同じ座標＝eff も大圏も同値
        Assert.Equal("甲", Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: true).Name);
        Assert.Equal("甲", Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: false).Name);
        ports.Reverse();
        Assert.Equal("乙", Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: true).Name);
        Assert.Equal("乙", Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: false).Name);
    }

    [Fact]
    public void 配線は旗の示す港と距離を欄に書く()
    {
        bool on = new Coefficients().PortByLandRoute > 0;
        var regions = new List<Region>
        {
            new() { Id = 0, Name = "ラサ", IsPort = false },
            new() { Id = 1, Name = "広州", IsPort = true },
            new() { Id = 2, Name = "天津", IsPort = true },
        };
        Simulation.WirePortDistances(regions);
        var lhasa = Climate.CityCoord("ラサ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("広州"), P("天津") };
        var want = Simulation.PortChoice(lhasa.Lat, lhasa.Lon, ports, on);
        var r = regions[0];
        Assert.Equal(want.Name, r.NearestPortName);
        Assert.Equal(want.LandKm, r.PortLandKm);
        Assert.Equal(on ? want.LandKm : want.GcKm, r.NearestPortDistanceKm);   // 旗が正＝鉄道の減衰の km は陸路（PortLandKm と同じ値）・0＝大圏
        Assert.Equal(on ? "天津" : "広州", r.NearestPortName);          // 期待は PortChoice の外で書く（旗が正＝陸路の最小の天津・0＝大圏の最小の広州）
    }
}
