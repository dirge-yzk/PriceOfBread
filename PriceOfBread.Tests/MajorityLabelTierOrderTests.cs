using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1290) c133mr 旗 MajorityLabelTierOrder の機構の試験（事前登録 measurements/m1290_20261122_prereg_mr.txt）。
/// 旗 1（ColonyLabelsFromData 1 の下）：国の主流の札は、本国格の州で札の付いた人口を先に数え、その表が空の札だけ植民地格の州で数える。
/// 文化と宗教は別々に選ぶ。首都の札へ逃がさない。盤面は試験の中で置き、期待値の札は字で書く。</summary>
public class MajorityLabelTierOrderTests
{
    private static Region R(string name, RegionTier tier, string culture, string religion, double pop)
        => new Region { Name = name, Tier = tier, Culture = culture, Religion = religion, Pops = [new Pop { Size = pop }] };

    private static World W(double flag, params Region[] regions)
    {
        var w = new World(regions.ToList(), new Coefficients { ColonyLabelsFromData = 1.0, MajorityLabelTierOrder = flag });
        w.SelfPower = new Power { Name = "試験国", BaseName = "試験国" };
        return w;
    }

    // T1：本国格なし・植民地格 X 100／Y 50（首都＝先頭の州は Y）→ 旗 1 で X・旗 0 で空
    [Fact]
    public void T1_本国格が無い国は植民地格の人口の最多の札()
    {
        Region[] B() => [R("首都の植民地", RegionTier.Colonial, "Y", "y教", 50), R("大きい植民地", RegionTier.Colonial, "X", "x教", 100)];
        Assert.Equal(("X", "x教"), Diplomacy.MajorityCultureReligion(W(1.0, B())));
        Assert.Equal(("", ""), Diplomacy.MajorityCultureReligion(W(0.0, B())));
    }

    // T2：本国格 H 10＋植民地格 X 100 → 旗 1 でも H（(1278) の狙い＝宗主の主流が植民地の人口で入れ替わらない）
    [Fact]
    public void T2_本国格に札が有れば植民地格を数えない()
    {
        var w = W(1.0, R("本国", RegionTier.Core, "H", "h教", 10), R("植民地", RegionTier.Colonial, "X", "x教", 100));
        Assert.Equal(("H", "h教"), Diplomacy.MajorityCultureReligion(w));
    }

    // T3：本国格の州は有るが札が空（2 州・人口 10）＋植民地格 X 100 → X（札の付いた人口の有無で選ぶ・州の数で選ばない）
    [Fact]
    public void T3_本国格の札が空なら植民地格で数える()
    {
        var w = W(1.0, R("本国A", RegionTier.Core, "", "", 10), R("本国B", RegionTier.Integrated, "", "", 10), R("植民地", RegionTier.Colonial, "X", "x教", 100));
        Assert.Equal(("X", "x教"), Diplomacy.MajorityCultureReligion(w));
    }

    // T4：文化と宗教は別々に選ぶ（本国格は文化 H だけ・宗教は空＝宗教だけ植民地格の z教・文化は H のまま）
    [Fact]
    public void T4_宗教も別に選ぶ()
    {
        var w = W(1.0, R("本国", RegionTier.Core, "H", "", 10), R("植民地", RegionTier.Colonial, "X", "z教", 100));
        Assert.Equal(("H", "z教"), Diplomacy.MajorityCultureReligion(w));
        var w2 = W(1.0, R("本国", RegionTier.Core, "", "h教", 10), R("植民地", RegionTier.Colonial, "X", "z教", 100));
        Assert.Equal(("X", "h教"), Diplomacy.MajorityCultureReligion(w2));
    }

    // T5：ColonyLabelsFromData 0（全ての州で数える旧の形）では旗 1 でも同じ（格の順は植民地格を数えない形の中だけ）
    [Fact]
    public void T5_植民地の札の旗0では全ての州で数えるまま()
    {
        Region[] B() => [R("本国", RegionTier.Core, "H", "h教", 10), R("植民地", RegionTier.Colonial, "X", "x教", 100)];
        var w1 = new World(B().ToList(), new Coefficients { ColonyLabelsFromData = 0.0, MajorityLabelTierOrder = 1.0 });
        var w0 = new World(B().ToList(), new Coefficients { ColonyLabelsFromData = 0.0, MajorityLabelTierOrder = 0.0 });
        Assert.Equal(("X", "x教"), Diplomacy.MajorityCultureReligion(w1));
        Assert.Equal(Diplomacy.MajorityCultureReligion(w0), Diplomacy.MajorityCultureReligion(w1));
    }

    // T6：計器 labeltier_s の行は旧と新の規則を自分で数えて並べる（Power の札はエンジンの値・旗 0 でも新の列は旗の規則で数える）
    [Fact]
    public void T6_計器labeltier_sは旧と新の札と格を並べる()
    {
        var h = Instruments.LabelTierHeaderText.Split('\t').Skip(1).ToList();   // quarter は表を書く側が足す
        string Col(string row, string name) => row.Split('\t')[h.IndexOf(name)];
        var w = W(1.0, R("首都の植民地", RegionTier.Colonial, "Y", "y教", 50), R("大きい植民地", RegionTier.Colonial, "X", "x教", 100));
        var row = Instruments.LabelTierRow(w)!;
        Assert.Equal(h.Count, row.Split('\t').Length);
        Assert.Equal("", Col(row, "oldCulture"));
        Assert.Equal("X", Col(row, "newCulture"));
        Assert.Equal("x教", Col(row, "newReligion"));
        Assert.Equal("C", Col(row, "tierC"));
        Assert.Equal("1", Col(row, "changedC"));
        Assert.Equal("1", Col(row, "powerCultureEmptyFlag"));   // Power の札は誰も書いていない＝空
        Assert.Equal("Y", Col(row, "capCulture"));
        Assert.Equal("150", Col(row, "colLabelledPopC"));
        Assert.Equal("0", Col(row, "homeLabelledPopC"));
        var w0 = W(0.0, R("首都の植民地", RegionTier.Colonial, "Y", "y教", 50), R("大きい植民地", RegionTier.Colonial, "X", "x教", 100));
        var row0 = Instruments.LabelTierRow(w0)!;
        Assert.Equal("", Col(row0, "newCulture"));
        Assert.Equal("-", Col(row0, "tierC"));
        Assert.Equal("0", Col(row0, "changedC"));
        var wh = W(1.0, R("本国", RegionTier.Core, "H", "h教", 10), R("植民地", RegionTier.Colonial, "X", "x教", 100));
        var rowh = Instruments.LabelTierRow(wh)!;
        Assert.Equal("H", Col(rowh, "newCulture"));
        Assert.Equal("H", Col(rowh, "tierC"));
        Assert.Equal("0", Col(rowh, "changedC"));
    }
}
