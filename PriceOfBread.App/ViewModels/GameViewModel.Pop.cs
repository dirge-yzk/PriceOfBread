// GameViewModel.Pop.cs ＝ 人口のページ（(501) UI の作り直し 段3）。
// 階級（特権つき）・州・宗教・文化は、本体の目録（SimCore/Display/PopTables）が渡す表を共通の表（Controls/TableView）が描く。年齢の層は人数の横棒（Controls/AgeBars）・出生と死は年次の線。
// ここに残すのは、App だけが採れる標本（規模と失業者の四半期の増減の矢印・年に 1 回の出生と死の累積）と、ヘッダーの人口の内訳（移民・年齢構成）を規模の吹き出しへ載せる口。
using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    /// <summary>人口のページが見えているか（配置から導く）。</summary>
    public bool PopulationShown => Layout.IsShown(Slot.Population);

    // ---- 表（本体の目録が行と列を渡す）----
    [ObservableProperty] private TableSnapshot _classTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _regionTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _religionTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _cultureTable = TableSnapshot.Empty;
    // ---- 年齢の層・児童の就業率・出生と死の線 ----
    [ObservableProperty] private AgePyramid _agePyramid = AgePyramid.Empty;
    [ObservableProperty] private string _cropLine = "";   // 国の作柄（州の表の列ではなく国の 1 行）
    [ObservableProperty] private string _childLaborLine = "";   // 児童の就業率（創発量＝家計困窮×合法度×就学。法の 2 値でなく実態が数年かけて落ちる様子）
    [ObservableProperty] private string _birthRateText = "−";   // 粗出生率の最新の年の値（年に 1 回の標本の差分）
    [ObservableProperty] private string _deathRateText = "−";
    [ObservableProperty] private string _fertilityText = "−";   // 合計出生率の代理

    // ★規模と失業者の増減の矢印（作者指示 2026-08-16）＝四半期スタンプ方式。Refresh は月次でも呼ばれる＝毎回差分を取ると 1 か月ぶんの微動しか出ない
    //   →四半期境界でのみ差分を確定し、境界間は前回確定値を表示。
    private readonly Dictionary<int, double> _prevPopUnemp = new();    // キー＝ClassId・前四半期境界の失業者数（千人）
    private readonly Dictionary<int, double> _popUnempDelta = new();   // キー＝ClassId・確定済みの四半期増減（千人）
    // 満足度・不満度・闘争度の四半期の増減（2026-11-11 作者指示：矢印を添える）。キー＝ClassId×4＋列（0 満足度・1 不満度・2 闘争度）
    private readonly Dictionary<int, double> _prevPopMood = new(), _popMoodDelta = new();
    private const double MoodTrendFlat = 0.005;   // 割合の矢印の平ら（表示の解像度 P0 の半分）
    private static string MoodArrow(double? delta) => delta is not double d ? "" : Math.Abs(d) < MoodTrendFlat ? "" : d > 0 ? " ↑" : " ↓";   // 数と矢印の間に空きを 1 つ（字体を替えて「62%↑」と密着した・点検⑦）
    private int _prevPopQuarterStamp = -1;   // 年×4＋四半期。-1＝未捕捉＝初回は基準化のみ（矢印なし）

    /// <summary>横ばいと見なす幅（千人）＝人数の書式が丸めて 0K になる幅。</summary>
    private const double PopTrendFlatK = 0.5;

    /// <summary>四半期境界でのみ差分を確定し、確定済みの差分を返す（未確定＝null）。規模と失業者で共有する 1 本の物差し。</summary>
    private static double? StampPopDelta(Dictionary<int, double> prev, Dictionary<int, double> delta, int key, double now, bool boundary)
    {
        if (boundary)
        {
            if (prev.TryGetValue(key, out var p)) delta[key] = now - p;
            prev[key] = now;
        }
        return delta.TryGetValue(key, out var d) ? d : null;
    }

    /// <summary>増減の向き＝矢印。未確定と横ばいは空文字（何も出さない・A6）。</summary>
    private static string PopArrow(double? delta)
        => delta is not double d ? "" : Math.Abs(d) < PopTrendFlatK ? "" : d > 0 ? " ↑" : " ↓";

    // ---- 年に 1 回の出生と死の標本（累積の出生・死亡・人口・出産可能な女性）。年は系列に持つ＝ロード後も重ならない ----
    private const string PopYearSeries = "pop.sampleYear", BirthsCumSeries = "pop.birthsCum", DeathsCumSeries = "pop.deathsCum", PopSizeSeries = "pop.size", FertileSeries = "pop.fertileWomen";

    private void SampleDemography()
    {
        var years = _series.Of(PopYearSeries);
        int y = _player.Year;
        if (years.Count > 0 && years[^1] >= y) return;
        var d = PopTables.Demography(_player);
        years.Add(y);
        _series.Of(BirthsCumSeries).Add(d.BirthsCum); _series.Of(DeathsCumSeries).Add(d.DeathsCum);
        _series.Of(PopSizeSeries).Add(d.Population); _series.Of(FertileSeries).Add(d.FertileWomen);
    }

    /// <summary>標本の差分から、粗出生率・粗死亡率（人口に対する年あたり）と合計出生率の代理（出産可能な女性 1 人あたりの年の出生×出産年齢の幅 35 年）の線を組む。</summary>
    private void RebuildDemographyLines()
    {
        var yrs = _series.Of(PopYearSeries); var b = _series.Of(BirthsCumSeries); var dth = _series.Of(DeathsCumSeries);
        var pop = _series.Of(PopSizeSeries); var fert = _series.Of(FertileSeries);
        int n = Math.Min(Math.Min(yrs.Count, b.Count), Math.Min(Math.Min(dth.Count, pop.Count), fert.Count));
        var birth = new List<double>(); var death = new List<double>(); var tfr = new List<double>();
        for (int i = 1; i < n; i++)
        {
            double dy = yrs[i] - yrs[i - 1], pAvg = (pop[i] + pop[i - 1]) / 2, fAvg = (fert[i] + fert[i - 1]) / 2;
            if (!(dy > 0)) continue;
            birth.Add(pAvg > 1e-9 ? (b[i] - b[i - 1]) / dy / pAvg : double.NaN);
            death.Add(pAvg > 1e-9 ? (dth[i] - dth[i - 1]) / dy / pAvg : double.NaN);
            tfr.Add(fAvg > 1e-9 ? (b[i] - b[i - 1]) / dy / fAvg * 35.0 : double.NaN);
        }
        static double? Last(List<double> v) { for (int i = v.Count - 1; i >= 0; i--) if (double.IsFinite(v[i])) return v[i]; return null; }
        BirthRateText = Last(birth) is double b0 ? Fmt.Of(Unit.RatePerYear, b0) : "−";
        DeathRateText = Last(death) is double d0 ? Fmt.Of(Unit.RatePerYear, d0) : "−";
        FertilityText = Last(tfr) is double t0 ? t0.ToString("0.0") : "−";
    }

    /// <summary>人口の更新（月・四半期の Refresh と、人口のページが見えた瞬間）。階級の集約と増減の矢印の差分は、見えているかに依らず毎月採る
    /// （採らないと見えた時の矢印が古い基準に対する差になる）。他の表と線は見えている時だけ組む。</summary>
    private void RefreshPopulation()
    {
        int stamp = _player.Year * Calendar.QuartersPerYear + _player.Quarter;
        bool boundary = stamp != _prevPopQuarterStamp;
        var classes = PopTables.Classes((l, col) => col switch
        {
            "unemployed" => PopArrow(StampPopDelta(_prevPopUnemp, _popUnempDelta, l.ClassId, l.Unemployed, boundary)),
            "satisfaction" => MoodArrow(StampPopDelta(_prevPopMood, _popMoodDelta, l.ClassId * 4, l.Satisfaction, boundary)),
            "discontent" => MoodArrow(StampPopDelta(_prevPopMood, _popMoodDelta, l.ClassId * 4 + 1, l.Discontent, boundary)),
            "militancy" => MoodArrow(StampPopDelta(_prevPopMood, _popMoodDelta, l.ClassId * 4 + 2, l.Militancy, boundary)),
            _ => "",
        });
        var data = classes.Read(_sim, _player);   // 矢印の差分はこの読みの中で採る（全階級の基準化・確定が済んでからスタンプを進める）
        if (boundary) _prevPopQuarterStamp = stamp;
        SampleDemography();
        if (!PopulationShown) return;
        // 規模の吹き出し＝ヘッダーの人口の内訳（保存則の 3 項＋今四半期の移民の流入と流出＋年齢構成）。全国の量なので題の 1 行が「人口 …」と名乗る
        string body = "";
        foreach (var t in PopulationTerms()) body += (body.Length == 0 ? "" : "\n") + t.Text;
        string? sizeTip = body.Length == 0 ? null : $"人口　{Fmt.PopFmt(_player.TotalPopulation)}\n" + body;
        ClassTable = TableSnapshot.From(data).WithColumnTip("size", sizeTip);
        RegionTable = TableSnapshot.From(PopTables.Regions.Read(_sim, _player));
        ReligionTable = TableSnapshot.From(PopTables.Religion.Read(_sim, _player));
        CultureTable = TableSnapshot.From(PopTables.Culture.Read(_sim, _player));
        AgePyramid = PopTables.Pyramid(_player);
        CropLine = $"作柄 {Fmt.Share(PopTables.NationCropFill(_player))}";
        ChildLaborLine = PopTables.ChildLaborRate(_player) is double r ? $"児童の就業率 {Fmt.Share(r)}" : "";
        RebuildDemographyLines();
    }
}
