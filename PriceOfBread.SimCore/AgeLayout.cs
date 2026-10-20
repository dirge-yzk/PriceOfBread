namespace PriceOfBread.SimCore;

/// <summary>★年齢の層の器（2026-10-21 (478)・死の側の段1・設計 docs/202610/20261015/20261015_人口の作り替え_出生と死亡_設計.md §2-2・事前登録 measurements/dz478_20261021_prereg.txt）
/// ＝層の数・下端・幅・標準のピラミッド・大人の境を1か所に持つ。層の番号を使う述語は全て年齢の述語として書く（この器の値から引く）。
/// <see cref="Bands8"/>＝今の10歳刻み8層（0〜9・10〜19・…・60〜69・70以上）／<see cref="Bands11"/>＝作者の刻み（0／1〜5／6〜10／11〜15／16〜20／21〜30／31〜40／41〜50／51〜60／61〜70／71以上）。
/// どちらを使うかはプロセス全体で1つ（環境変数 <c>POB_AGE_BANDS</c>＝未設定か8→<see cref="Bands8"/>・11→<see cref="Bands11"/>・ほかは例外）＝<see cref="Pop.Layout"/>。
/// 計算の読み口（<see cref="Pop.AgeShareOf"/> ほか）は配列の長さ（2×層の数）から器を引く＝グローバルを読まない（11層の算術を同じプロセスの番人が直接叩ける）。
/// ⚠8層の定数は旧の式と**同じ double**にしてある（下端 10.0×b・幅 10.0・幅×12＝120.0）＝旧の `ov / 10.0`・`band / 120.0` と1ビットも違わない（既定の恒等の根）。</summary>
public sealed class AgeLayout
{
    /// <summary>層の数の最大（stackalloc の確保の大きさ＝定数で確保して 2×層の数に切る）。</summary>
    public const int MaxBands = 11;
    /// <summary>層×性の配列の長さの最大。</summary>
    public const int MaxSlots = 2 * MaxBands;

    public readonly int Bands;
    /// <summary>層の下端の年齢（最上層は開いた区間の下端）。</summary>
    public readonly double[] Lower;
    /// <summary>層の上端の年齢（＝下端＋幅・最上層は使わない）。</summary>
    public readonly double[] Upper;
    /// <summary>層の幅（年・最上層は使わない＝0）。</summary>
    public readonly double[] Width;
    /// <summary>幅×12（月）＝加齢の流れ（層÷これ／月）の分母。最上層は使わない＝0。</summary>
    public readonly double[] MonthsPerBand;
    /// <summary>標準の前近代ピラミッド（男女計・割合・和1）＝1783年の全ての POP の初期の構成。</summary>
    public readonly double[] Pyramid;
    /// <summary>20歳以上の最初の層（職業 POP の性比・性別の門が見る「大人」の最初の層）。</summary>
    public readonly int AdultFirstBand;
    /// <summary>大人に入る前の最後の層（この層から上へ出る流れ＝世代の置き換わり）。</summary>
    public readonly int EntryBand;
    /// <summary>大人に入る年齢（世代の置き換わりの「大人」の下端・<see cref="EntryBand"/> の上端）。</summary>
    public readonly double AdultAge;
    /// <summary>兵士の初期の構成（男の20〜39歳）の2層。</summary>
    public readonly int SoldierBandA, SoldierBandB;
    /// <summary>60歳以後の余命の計器が読む2層（幅10年の層とその上の開いた層）。</summary>
    public readonly int OldBandA, OldBandB;

    private AgeLayout(double[] lower, double[] width, double[] pyramid, int adultFirstBand, int entryBand, double adultAge, int soldierA, int soldierB, int oldA, int oldB)
    {
        Bands = lower.Length; Lower = lower; Width = width; Pyramid = pyramid;
        Upper = new double[Bands]; MonthsPerBand = new double[Bands];
        for (int b = 0; b < Bands - 1; b++) { Upper[b] = lower[b] + width[b]; MonthsPerBand[b] = width[b] * 12.0; }
        AdultFirstBand = adultFirstBand; EntryBand = entryBand; AdultAge = adultAge; SoldierBandA = soldierA; SoldierBandB = soldierB; OldBandA = oldA; OldBandB = oldB;
    }

    /// <summary>今の10歳刻み8層。標準のピラミッド＝設計書 §3-1 の表（平均寿命30前後・年0.5%増の安定人口・15歳未満 35.3%・60歳以上 7.5%）。</summary>
    public static readonly AgeLayout Bands8 = new(
        [0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0],
        [10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 0.0],
        [0.255, 0.195, 0.160, 0.130, 0.105, 0.080, 0.050, 0.025],
        adultFirstBand: 2, entryBand: 1, adultAge: 20.0, soldierA: 2, soldierB: 3, oldA: 6, oldB: 7);

    /// <summary>作者の11層（0／1〜5／6〜10／11〜15／16〜20／21〜30／31〜40／41〜50／51〜60／61〜70／71以上）。
    /// 標準のピラミッド（データ）＝今の8層の累積分布（10歳刻みの境で厳密に通る）を単調な3次補間（Fritsch–Carlson）で新しい境へ引いた物。0歳の層だけ 0.030
    /// （粗出生 0.035÷(1＋乳児の死亡率＋増加率 0.005)の安定人口の形・乳児死亡 約14%）。71以上は70の境の密度で0.7年延ばして切る。導出の script＝scripts/instruments/dz478/std_readers.py
    /// （AI の決め〈確証なし〉＝出典を持つ生命表から導いた物ではない）。和＝1・死の年齢の重み μ は全層で正（最小 0.0102）。</summary>
    public static readonly AgeLayout Bands11 = new(
        [0.0, 1.0, 6.0, 11.0, 16.0, 21.0, 31.0, 41.0, 51.0, 61.0, 71.0],
        [1.0, 5.0, 5.0, 5.0, 5.0, 10.0, 10.0, 10.0, 10.0, 10.0, 0.0],
        [0.0300, 0.1317, 0.1150, 0.1005, 0.0903, 0.1568, 0.1273, 0.1025, 0.0770, 0.0474, 0.0215],
        adultFirstBand: 5, entryBand: 4, adultAge: 21.0, soldierA: 5, soldierB: 6, oldA: 9, oldB: 10);

    /// <summary>配列（層×性の割合か人数・長さ 2×層の数）の長さから器を引く。</summary>
    public static AgeLayout OfLength(int length)
        => length == 2 * 8 ? Bands8 : length == 2 * 11 ? Bands11 : throw new ArgumentException($"年齢の層の配列の長さが不正（{length}）＝16（8層）か22（11層）");

    /// <summary>環境変数 POB_AGE_BANDS を1回だけ読む（<see cref="Pop.Layout"/>）。未設定か空か 8 → 8層／11 → 11層／ほかは例外。</summary>
    internal static AgeLayout FromEnvironment() => Parse(Environment.GetEnvironmentVariable("POB_AGE_BANDS"));

    /// <summary>環境変数の値の読み（純関数＝番人がプロセスの環境を触らずに叩く）：null・空・"8" → 8層／"11" → 11層／ほかは例外（空白つきも不可）。</summary>
    internal static AgeLayout Parse(string? v)
        => string.IsNullOrEmpty(v) || v == "8" ? Bands8 : v == "11" ? Bands11 : throw new InvalidOperationException($"POB_AGE_BANDS は 8 か 11（{v}）");

    /// <summary>層 <paramref name="b"/> と [lo, hi) の重なり（層の幅に対する割合・層の中は一様）。最上層は分けない（lo が下端以下で hi が無限大のときだけ全部）。</summary>
    internal double Overlap(int b, double lo, double hi)
    {
        if (b == Bands - 1) return lo <= Lower[b] && double.IsPositiveInfinity(hi) ? 1.0 : 0.0;
        double bl = Lower[b], ov = Math.Min(hi, Upper[b]) - Math.Max(lo, bl);
        return ov > 0 ? ov / Width[b] : 0.0;
    }

    /// <summary>★加齢の流れの1本（月次）＝層 <paramref name="b"/> から上の層へ1か月に出ていく人数＝層の人数 <paramref name="band"/> ÷（幅×12）（層の中は一様・設計書 §2-2）。
    /// 加齢（StepAges・AgeOneMonth）・死の年齢の重みの導出（MortalityWeights）・世代の置き換わり（EnteringAdultsMonthlyOf）が同じ1本を読む。</summary>
    internal double Outflow(int b, double band) => band / MonthsPerBand[b];
}
