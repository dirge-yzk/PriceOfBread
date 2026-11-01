using System;
using System.Collections.Generic;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(754) 距離の測り方を 1 本に（線 c4pl）の機構の番人。①内陸州の最寄り港を陸路で選ぶ純関数 <see cref="Simulation.PortChoice"/>（byLand を引数で明示＝旗の読みに依らない）：
/// 手の盤面（開幕の紙の台本 measurements/m754_20261102_paper.py が routedump から出した選び）と同じ港を選ぶ・陸路が無限の港は選ばない・有限の港が 1 つも無ければ大圏へ倒す・byLand=false は旧と同じ港。
/// ②旗 ArcKmAtan2（既定 1）の <see cref="PathGraph.ArcKm"/> が Geo.DistanceKm（haversine の atan2）と全ノード対で相対 1e-13 以内に合う（旧の acos は 3.2e-12）。</summary>
public class PortByLandTests
{
    static (double Lat, double Lon, string Name) P(string name) { var c = Climate.CityCoord(name); return (c.Lat, c.Lon, name); }

    [Fact]
    public void ラサは陸路なら天津を選び大圏なら広州を選ぶ()
    {
        var lhasa = Climate.CityCoord("ラサ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("広州"), P("天津") };
        // 紙の台本（routedump）：ラサ→広州 大圏 2,317 km・陸路 3,231 km／ラサ→天津 大圏 2,603 km・陸路 2,754 km
        Assert.Equal("広州", Simulation.PortChoice(lhasa.Lat, lhasa.Lon, ports, byLand: false).Name);
        var byLand = Simulation.PortChoice(lhasa.Lat, lhasa.Lon, ports, byLand: true);
        Assert.Equal("天津", byLand.Name);
        Assert.InRange(byLand.LandKm, 2700, 2800);
        Assert.InRange(byLand.GcKm, 2550, 2650);
        // 港の並びを入れ替えても同じ港（同率でない限り並びに依らない）
        ports.Reverse();
        Assert.Equal("天津", Simulation.PortChoice(lhasa.Lat, lhasa.Lon, ports, byLand: true).Name);
    }

    [Fact]
    public void 陸路が無限の港は選ばず有限の港へ行く()
    {
        var paris = Climate.CityCoord("パリ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("ロンドン"), P("マルセイユ") };   // ロンドンは大圏 344 km だが陸路 ∞（海を渡る）・マルセイユは大圏 659 km・陸路 668 km
        Assert.Equal("ロンドン", Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: false).Name);
        var r = Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: true);
        Assert.Equal("マルセイユ", r.Name);
        Assert.InRange(r.LandKm, 660, 680);
    }

    [Fact]
    public void 有限の陸路の港が一つも無ければ大圏の最小へ倒す()
    {
        var paris = Climate.CityCoord("パリ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("ダブリン"), P("ロンドン") };   // どちらも陸路 ∞（大圏 781 km と 344 km）
        var old = Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: false);
        var nw = Simulation.PortChoice(paris.Lat, paris.Lon, ports, byLand: true);
        Assert.Equal("ロンドン", old.Name);
        Assert.Equal(old, nw);                          // 旧と同じ港・同じ km
        Assert.Equal(nw.GcKm, nw.LandKm);               // 陸路 ∞ は大圏で退避（0 を残さない）
    }

    [Fact]
    public void 旗ArcKmAtan2のArcKmは全ノード対でGeoの距離と相対1e_13以内に合う()
    {
        var nodes = PathGraph.Nodes;
        Assert.Equal(1070, PathGraph.Edges.Length);
        (double X, double Y, double Z) U(double lat, double lon)
        {
            double la = lat * Math.PI / 180.0, lo = lon * Math.PI / 180.0, c = Math.Cos(la);
            return (c * Math.Cos(lo), c * Math.Sin(lo), Math.Sin(la));
        }
        double worst = 0; int pairs = 0;
        for (int i = 0; i < nodes.Length; i++)
            for (int j = i + 1; j < nodes.Length; j++)
            {
                double g = Geo.DistanceKm(nodes[i].Lat, nodes[i].Lon, nodes[j].Lat, nodes[j].Lon);
                if (g < 1e-3) continue;   // 同座標の対は比べない
                double a = PathGraph.ArcKm(U(nodes[i].Lat, nodes[i].Lon), U(nodes[j].Lat, nodes[j].Lon));
                worst = Math.Max(worst, Math.Abs(a - g) / g); pairs++;
            }
        Assert.True(pairs > 80000, $"対の数 {pairs}");
        Assert.True(worst <= 1e-13, $"最大の相対差 {worst:E3}");
    }
}
