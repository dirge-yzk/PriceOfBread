namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (480) 最低賃金法の AI の判断の表 ma_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsLw.cs・InstrumentsSf.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。旗 MinimumWageAiFromFloor・MinimumWageLawGate に依らず出す。
// 目的＝事前登録 measurements/ma480_20261021_prereg.txt の G4（下げが働く）・G5（上げが働く）・G6（読みの整合）・G7（往復）を読む。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。
// 列：
//   lawCode（World.MinimumWage の序数 0＝なし〜3＝充足）／urbanHome（本国の都市の割合＝今の規則の入力）／
//   unemployment（本国の州の担い手 POP の失業率の人数重み付け平均・読めなければ nan）／
//   bindCur（今の段の床が縛る広がり＝本国の能力のうち値段が床の 1.05 倍以下の州の割合・なしなら nan）／bindCurCapped（同じ読みを天井つきの床(AbsFloor)で・(480) 追記 3）／bindNext（次の段・充足なら nan）／
//   wantOld（今の規則〈都市の割合の線〉の目標の序数・書かない＝−1）／wantNew（新しい規則の目標の序数・書かない＝−1・旗に依らず仮想の値を出す）／
//   ruleTargetOrd（この四半期に AI の規則が実際に p へ書いた目標の序数〔World.DiagMaTargetOrd・書かない＝−1〕＝旗に依る）／policyTarget（四半期の末の p.MinimumWage の序数）。
// ⚠限界（計器が自己申告する）：①unemployment・bindCur・bindNext・wantNew は四半期の末の断面（AI の判断は四半期の初め）／②本国の州だけ（labor_s は全ての州）／③旗 0 のとき wantNew は使われない仮想の値／④ruleTargetOrd は直近の AI の判断の 1 回（プレイヤー国は −1 のまま）。
public static partial class Instruments
{
    internal const string MaHeaderText = "quarter\tyear\tnation\tlawCode\turbanHome\tunemployment\tbindCur\tbindCurCapped\tbindNext\twantOld\twantNew\truleTargetOrd\tpolicyTarget";

    /// <summary>bindCur の天井つき版＝labor_s の enactedNearFloorShare と同じ読み（床は <see cref="MinimumWageFloor.AbsFloor"/>＝機構〔MarketFlow.StepDay〕と同じ天井で削る）。AI の読み（BindShare）は天井なしの PerUnit＝両者の差が天井の効いた州（事前登録 追記 3 の限界 ⑨）。読むだけ。</summary>
    static double BindShareCapped(World w, MinimumWageFloor.HomeLabor h, MinimumWageLaw level)
    {
        if (level == MinimumWageLaw.None || h.N == 0) return double.NaN;
        double bound = 0, total = 0;
        for (int i = 0; i < h.N; i++)
        {
            var r = h.Regions[i];
            double ceil = r.GoodOf(Catalog.LaborId)!.RefPrice * r.PriceLevel * MarketFlow.EffWageMax(r, w.Coef);
            double f = MinimumWageFloor.AbsFloor(r, Catalog.LaborId, ceil, level);
            total += h.Cap[i];
            if (f > 0 && h.Price[i] <= MinimumWageFloor.NearFloorMargin * f) bound += h.Cap[i];
        }
        return total > 1e-9 ? bound / total : double.NaN;
    }

    static void MaFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var cur = w.MinimumWage;
        var h = MinimumWageFloor.ReadHomeLabor(w);
        double bCur = cur > MinimumWageLaw.None ? MinimumWageFloor.BindShare(h, cur) : double.NaN;
        double bCurCapped = BindShareCapped(w, h, cur);
        double bNext = cur < MinimumWageLaw.Sufficiency ? MinimumWageFloor.BindShare(h, cur + 1) : double.NaN;
        var wantNew = UtilityAgent.MinimumWageFloorWant(cur, h.Unemployment, bCur, bNext);
        var wantOld = UtilityAgent.MinimumWageTargetFromBoard(w);
        sk.Ma ??= Open("ma", seed);
        if (!sk.MaHeader) { sk.Ma.WriteLine(MaHeaderText); sk.MaHeader = true; }
        sk.Ma.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), I((int)cur), F(UtilityAgent.HomeUrbanShare(w)), F(h.Unemployment), F(bCur), F(bCurCapped), F(bNext),
            I(wantOld is { } o ? (int)o : -1), I(wantNew is { } n ? (int)n : -1), I(w.DiagMaTargetOrd), I((int)w.Policy.MinimumWage)));
    }
}
