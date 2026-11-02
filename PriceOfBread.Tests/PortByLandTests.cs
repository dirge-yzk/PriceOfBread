using System;
using System.Collections.Generic;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(770) 港の選び方のやり直し（線 c6pr）の機構の番人。内陸州が使う港を選ぶ純関数 <see cref="Simulation.PortChoice"/>（(785) で旗を焼き込み・引数は無い）：
/// 港ごとの eff＝〈陸路が有限 ? 陸路 : 大圏〉の最小（①島の港は大圏の eff で比べ、遠い陸の港に負けない ②陸路が全て∞なら大圏の最小 ③陸路が有限どうしなら陸路の最小 ④同率は先着）と、
/// 配線 <see cref="Simulation.WirePortDistances"/> が選んだ港と km を欄に書く（⑥）。座標は州と港の実在の座標、陸路は実の陸グラフ（山脈の迂回・海を渡れない）。</summary>
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
        var r = Simulation.PortChoice(paris.Lat, paris.Lon, ports);
        Assert.Equal("島の港", r.Name);
        Assert.InRange(r.GcKm, 330, 360);
        Assert.Equal(r.GcKm, r.LandKm);                 // 陸路 ∞ は大圏で退避（0 を残さない）
    }

    [Fact]
    public void 島の港が陸の港より遠ければ陸の港を選ぶ()
    {
        var paris = Climate.CityCoord("パリ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("ダブリン"), P("マルセイユ") };   // ダブリン＝大圏 781 km・陸路 ∞ ／ マルセイユ＝陸路 668 km
        var r = Simulation.PortChoice(paris.Lat, paris.Lon, ports);
        Assert.Equal("マルセイユ", r.Name);
        Assert.InRange(r.LandKm, 660, 680);
        ports.Reverse();
        Assert.Equal("マルセイユ", Simulation.PortChoice(paris.Lat, paris.Lon, ports).Name);
    }

    [Fact]
    public void 全ての港への陸路が無限なら大圏の最小を選び旧と同じ港と距離になる()
    {
        var paris = Climate.CityCoord("パリ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("ダブリン"), P("ロンドン") };   // どちらも陸路 ∞（大圏 781 km と 344 km）
        var nw = Simulation.PortChoice(paris.Lat, paris.Lon, ports);
        Assert.Equal("ロンドン", nw.Name);
        Assert.Equal(nw.GcKm, nw.LandKm);
    }

    [Fact]
    public void 陸路が有限の港どうしなら陸路の最小を選びラサは天津になる()
    {
        var lhasa = Climate.CityCoord("ラサ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("広州"), P("天津") };
        // 大圏では広州（約 2,317 km）が近いが、陸路では広州が遠回り（約 3,231 km）で天津が短い（約 2,754 km）
        var byLand = Simulation.PortChoice(lhasa.Lat, lhasa.Lon, ports);
        Assert.Equal("天津", byLand.Name);
        Assert.InRange(byLand.LandKm, 2700, 2800);
        Assert.InRange(byLand.GcKm, 2550, 2650);
        ports.Reverse();
        Assert.Equal("天津", Simulation.PortChoice(lhasa.Lat, lhasa.Lon, ports).Name);
    }

    [Fact]
    public void 同率は並びの先の港()
    {
        var paris = Climate.CityCoord("パリ");
        var m = P("マルセイユ");
        var ports = new List<(double Lat, double Lon, string Name)> { (m.Lat, m.Lon, "甲"), (m.Lat, m.Lon, "乙") };   // 同じ座標＝eff も大圏も同値
        Assert.Equal("甲", Simulation.PortChoice(paris.Lat, paris.Lon, ports).Name);
        ports.Reverse();
        Assert.Equal("乙", Simulation.PortChoice(paris.Lat, paris.Lon, ports).Name);
    }

    [Fact]
    public void 配線は旗の示す港と距離を欄に書く()
    {
        var regions = new List<Region>
        {
            new() { Id = 0, Name = "ラサ", IsPort = false },
            new() { Id = 1, Name = "広州", IsPort = true },
            new() { Id = 2, Name = "天津", IsPort = true },
        };
        Simulation.WirePortDistances(regions);
        var lhasa = Climate.CityCoord("ラサ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("広州"), P("天津") };
        var want = Simulation.PortChoice(lhasa.Lat, lhasa.Lon, ports);
        var r = regions[0];
        Assert.Equal(want.Name, r.NearestPortName);
        Assert.Equal(want.LandKm, r.PortLandKm);
        Assert.Equal(want.LandKm, r.NearestPortDistanceKm);   // 鉄道の減衰の km は陸路（PortLandKm と同じ値）
        Assert.Equal("天津", r.NearestPortName);          // 期待は PortChoice の外で書く（陸路の最小の天津）
    }

    // ---------------------------------------------------------------- (777) 港の陸塊の表 portlm_s（線 c7pg・計器と紙だけ） ----------------------------------------------------------------
    // 期待の札は文字列で手で書く（Landmass.Of から作らない＝同語反復にしない）。

    [Fact]
    public void 港の陸塊の判定は札で数え_同じ陸塊は0_別の陸塊は1_陸路が無限はinf()
    {
        var paris = Climate.CityCoord("パリ");
        var havre = Climate.CityCoord("ルアーヴル"); var london = Climate.CityCoord("ロンドン");
        var same = Instruments.PortlmClassify(paris, havre);
        Assert.Equal("eurasia", same.LmRegion);
        Assert.Equal("eurasia", same.LmPort);
        Assert.False(same.Cross);
        Assert.True(double.IsFinite(same.LandKm));
        Assert.InRange(same.GcKm, 170, 185);                 // パリ→ルアーヴル 177.7 km（portch_s の 1784 年の行）
        var cross = Instruments.PortlmClassify(paris, london);
        Assert.Equal("eurasia", cross.LmRegion);
        Assert.Equal("britain", cross.LmPort);
        Assert.True(cross.Cross);
        Assert.True(double.IsPositiveInfinity(cross.LandKm));   // 陸路∞（欄 PortLandKm のように大圏で退避しない）
        var lhasa = Climate.CityCoord("ラサ"); var tianjin = Climate.CityCoord("天津");
        var cn = Instruments.PortlmClassify(lhasa, tianjin);
        Assert.False(cn.Cross);
        Assert.InRange(cn.LandKm, 2700, 2800);                // 陸路は生の値（ラサ→天津と同じ盤面）
    }

    [Fact]
    public void 札が違えば陸路が有限でも別の陸塊と数える()
    {
        var cairo = Climate.CityCoord("カイロ"); var jeru = Climate.CityCoord("エルサレム");
        var r = Instruments.PortlmClassify(jeru, cairo);
        Assert.Equal("eurasia", r.LmRegion);
        Assert.Equal("africa", r.LmPort);
        Assert.True(r.Cross);
        Assert.True(double.IsFinite(r.LandKm), "シナイの陸路が無限＝この試験の前提（札と陸路が別の物差し）が崩れた：" + r.LandKm);
    }

    [Fact]
    public void 形bcdは島の港より陸路の港を選び_形cの字義は形bと同じ港_岸を広げた形は近い岸から海を渡る()
    {
        var paris = Climate.CityCoord("パリ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("ロンドン"), P("マルセイユ") };   // ロンドン＝大圏 344 km・陸路∞（今の形はこちら）／マルセイユ＝陸路 668 km
        var shores = new List<(double Lat, double Lon, string Name)> { P("ルアーヴル"), P("ロンドン"), P("マルセイユ") };   // 岸＝全ての国の港（ルアーヴルは陸路 約 178 km）
        var s = Instruments.PortlmShapes(paris, "eurasia", ports, shores, false, false);
        Assert.Equal(1, s.LmPorts);          // 札が eurasia の港はマルセイユだけ
        Assert.Equal(1, s.Finite);
        Assert.Equal(1, s.SameLmFinite);
        Assert.InRange(s.GcKmMin, 330, 360);
        Assert.Equal("マルセイユ", s.Fin.Port);  Assert.InRange(s.Fin.EffKm, 660, 680);   // (b) 有限だけ
        Assert.Equal("マルセイユ", s.Sea.Port);  Assert.Equal(s.Fin.EffKm, s.Sea.EffKm); // (c) 字義＝∞の港は〈マルセイユまでの陸路＋海路〉＝必ずマルセイユに負ける
        Assert.Equal("マルセイユ", s.Drop.Port); Assert.Equal(s.Fin.EffKm, s.Drop.EffKm); // (d) 札が同じで有限の港がある＝∞を外す
        Assert.Equal("ルアーヴル", s.ShoreName);                                         // (c′) 岸＝陸路の最小の港（どの国でも）
        Assert.Equal("ロンドン", s.Shore.Port);                                           // ルアーヴルから英仏海峡を渡る方がマルセイユの陸路より短い
        Assert.InRange(s.Shore.EffKm, 178, 660);
    }

    [Fact]
    public void 形dは有限の港が全て別の札なら外さず_形bは外す()
    {
        var tls = Climate.CityCoord("トゥールーズ");
        var cairo = Climate.CityCoord("カイロ");
        // 手の盤面（モロッコ領トゥールーズの型）：同じ国の港＝カイロの座標の港「甲」（africa・陸路は有限だが遠い）と島の港アジャクシオ（corsica・陸路∞・大圏は近い）
        var ports = new List<(double Lat, double Lon, string Name)> { (cairo.Lat, cairo.Lon, "甲"), P("アジャクシオ") };
        var s = Instruments.PortlmShapes(tls, "eurasia", ports, ports, false, false);
        Assert.Equal(0, s.LmPorts);
        Assert.Equal(0, s.SameLmFinite);
        Assert.Equal(1, s.Finite);
        Assert.Equal("甲", s.Fin.Port);                                                    // (b)：有限なら札を問わない＝大陸を回る遠い港
        Assert.True(s.Fin.EffKm > 2 * s.GcKmMin, "形 b の陸路が大圏の最小の 2 倍以下（U2 の型の前提が崩れた）：" + s.Fin.EffKm + " / " + s.GcKmMin);
        Assert.Equal("アジャクシオ", s.Drop.Port);                                          // (d)：札が同じ有限の港が無い＝外さず (a) のまま
        var a = Simulation.PortChoice(tls.Lat, tls.Lon, ports);
        Assert.Equal("アジャクシオ", a.Name);
        Assert.Equal(a.LandKm, s.Drop.EffKm);
    }

    // ---------------------------------------------------------------- (779) 港の形 (d)（線 c8pd・旗 PortSameLandmassFirst） ----------------------------------------------------------------
    // 期待の港は手で書く（PortChoice の答えから作らない）。(785) で旗を焼き込み＝形 (d) が PortChoice の唯一の経路（(a) の反実仮想は計器 PortlmAD が持つ）。

    [Fact]
    public void 形d_札が同じで陸路が有限の港があれば陸路無限の港を外す()
    {
        var paris = Climate.CityCoord("パリ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("ロンドン"), P("マルセイユ") };   // ロンドン＝大圏 344 km・陸路∞・札 britain ／ マルセイユ＝陸路 668 km・札 eurasia（パリと同じ）
        var d = Simulation.PortChoice(paris.Lat, paris.Lon, ports);
        Assert.Equal("マルセイユ", d.Name);
        Assert.InRange(d.LandKm, 660, 680);
        Assert.InRange(d.GcKm, 640, 680);
        ports.Reverse();
        Assert.Equal("マルセイユ", Simulation.PortChoice(paris.Lat, paris.Lon, ports).Name);   // 並びに依らない
    }

    [Fact]
    public void 形d_札が同じで陸路が有限の港が無ければ今の形と同じ()
    {
        var paris = Climate.CityCoord("パリ");
        var allInf = new List<(double Lat, double Lon, string Name)> { P("ダブリン"), P("ロンドン") };   // どちらも陸路∞（大圏 781 km と 344 km）
        var d = Simulation.PortChoice(paris.Lat, paris.Lon, allInf);
        Assert.Equal("ロンドン", d.Name);
        var tls = Climate.CityCoord("トゥールーズ"); var cairo = Climate.CityCoord("カイロ");
        var mixed = new List<(double Lat, double Lon, string Name)> { (cairo.Lat, cairo.Lon, "甲"), P("アジャクシオ") };   // 甲＝札 africa で陸路は有限（シナイ）・アジャクシオ＝札 corsica で陸路∞
        Assert.True(double.IsFinite(PathGraph.LandDistanceKm(tls.Lat, tls.Lon, cairo.Lat, cairo.Lon)), "シナイの陸路が無限＝この試験の前提が崩れた（データの前提の赤）");
        Assert.Equal("アジャクシオ", Simulation.PortChoice(tls.Lat, tls.Lon, mixed).Name);   // 有限の港が別の札（africa）だけ＝外さない
    }

    [Fact]
    public void 形d_配線は旗の示す港を欄に書く_計器の独立の書き方とも一致する()
    {
        var regions = new List<Region>
        {
            new() { Id = 0, Name = "パリ", IsPort = false },
            new() { Id = 1, Name = "ロンドン", IsPort = true },
            new() { Id = 2, Name = "マルセイユ", IsPort = true },
        };
        Simulation.WirePortDistances(regions);
        Assert.Equal("マルセイユ", regions[0].NearestPortName);   // 形 (d)＝陸路∞のロンドンを外す（(a) の海を渡る跳びはロンドン）
        var paris = Climate.CityCoord("パリ");
        var ports = new List<(double Lat, double Lon, string Name)> { P("ロンドン"), P("マルセイユ") };
        var ad = Instruments.PortlmAD(paris, "eurasia", ports);   // 計器の独立の書き方（PortChoice を呼ばない）
        Assert.Equal("ロンドン", ad.PortA);
        Assert.Equal("マルセイユ", ad.PortD);
        Assert.True(double.IsPositiveInfinity(ad.LandA));
        Assert.InRange(ad.EffD, 660, 680);
        var tls = Climate.CityCoord("トゥールーズ"); var cairo = Climate.CityCoord("カイロ");
        var mixed = new List<(double Lat, double Lon, string Name)> { (cairo.Lat, cairo.Lon, "甲"), P("アジャクシオ") };
        var ad2 = Instruments.PortlmAD(tls, "eurasia", mixed);
        Assert.Equal("アジャクシオ", ad2.PortA);
        Assert.Equal("アジャクシオ", ad2.PortD);
    }
}

/// <summary>(777) 計器 portlm_s は世界を書かない（計器なし＝計器つき）・表の列が揃い crossLandmass が札の比較と合う。Instruments.On は全体の静的な旗＝別の収集（Instruments）で直列に回す。</summary>
[Collection("Instruments")]
public class PortlmInstrumentTests
{
    private static MultiNationSim MakeMatch() => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());

    private static (long Digest, string? Dir) RunMatch(int quarters, bool instr)
    {
        string? dir = instr ? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_portlm_" + Guid.NewGuid().ToString("N")) : null;
        if (instr) Instruments.Configure(dir, null);
        var sim = MakeMatch();
        try
        {
            if (instr) Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            if (instr) Instruments.Phase(sim, 1789UL, "warm");
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (instr) Instruments.Quarter(sim, 1789UL, q); }
        }
        finally { if (instr) { Instruments.Close(sim); Instruments.Configure(null, null); } }
        long d = 17;
        foreach (var n in sim.Nations)
        {
            d = d * 31 + BitConverter.DoubleToInt64Bits(n.World.Treasury.Value);
            foreach (var r in n.World.Regions)
                d = d * 31 + BitConverter.DoubleToInt64Bits(r.NearestPortDistanceKm) + BitConverter.DoubleToInt64Bits(r.PortLandKm) + StringComparer.Ordinal.GetHashCode(r.NearestPortName) + r.Pops.Count;
        }
        return (d, dir);
    }

    [Fact]
    public void 港の陸塊の表は列が揃い札の比較と合い_計器を出しても軌道は変わらない()
    {
        const int quarters = 4;   // 年末（q%4==3）の 1 回
        var (plain, _) = RunMatch(quarters, instr: false);
        var (withIns, dir) = RunMatch(quarters, instr: true);
        try
        {
            Assert.Equal(plain, withIns);   // 計器は読むだけ＝国庫と州の港の欄がビットで一致（全軌道の一致の根拠は計器つきの成績表が計器なしと全行一致すること）
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir!, "portlm_s1789.tsv"));
            Assert.Equal(Instruments.PortlmHeaderText, lines[0]);
            var h = lines[0].Split('\t');
            var rows = new List<string[]>();
            for (int i = 1; i < lines.Length; i++) rows.Add(lines[i].Split('\t'));
            Assert.True(rows.Count >= 10, "内陸州の行が少ない：" + rows.Count);
            int I(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, "列が無い " + c); return i; }
            foreach (var r in rows)
            {
                Assert.Equal(h.Length, r.Length);
                Assert.Equal(r[I("lmUsed")] != r[I("lmRegion")] ? "1" : "0", r[I("crossLandmass")]);
                Assert.NotEqual("", r[I("lmRegion")]);
            }
            Assert.Contains(rows, r => r[I("nation")] == "フランス" && r[I("region")] == "パリ" && r[I("lmRegion")] == "eurasia" && r[I("lmUsed")] == "eurasia" && r[I("crossLandmass")] == "0");
            Assert.Contains(rows, r => r[I("nation")] == "イギリス" && r[I("lmRegion")] == "britain" && r[I("lmUsed")] == "britain");
        }
        finally { if (dir is not null && System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
    }
}
