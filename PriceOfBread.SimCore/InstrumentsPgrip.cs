namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (756) 議会の掌握の起債の項の表 pgrip_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（(748) InstrumentsMigform.cs の先例・その計器は (753) で削除）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（四半期の末に純関数を読むだけ）。
// 目的＝事前登録 measurements/m756_20261102_prereg_dg.txt の D1（実装）・D2（張り付き）・D3（掌握の新÷旧）を同じ盤面で読む。
// 行＝国×四半期（四半期の末）。debtGripOld＝旧（月の名目GDP・床 1.0）／debtGripNew＝年の名目GDP／Used＝今の旗（ParliamentDebtGripYearGdp）が選ぶ側。
//   gripOld・gripNew＝ParliamentGripOf を両方の分母で数えた値／gripUsed＝公開の PowerShares.ParliamentGrip（旗で選ぶ本物の経路）／peOld・peNew＝議会の実効統制／civOld・civNew＝文民制度の厚み。
//   debtToGdpYearRatio＝国債÷年の名目GDP（Clamp なし・年の名目GDP が正でないとき 0）。
// ⚠限界（計器が自己申告する）：旧と新のうち旗が選ばない側は反実仮想（その軌道では走らない世界の値を同じ盤面から数えただけ）。下流（peOld/civOld を読む政治資本・クーデタ）の反実仮想は数えない。
public static partial class Instruments
{
    internal const string PgripHeaderText = "quarter	year	nation	debtToGdpYearRatio	taxGrip	debtGripOld	debtGripNew	debtGripUsed	gripOld	gripNew	gripUsed	peOld	peNew	civOld	civNew";

    sealed class PgripSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PgripSink> PgripSinks = new();

    /// <summary>四半期の末（<see cref="Quarter"/>）：全ての国の掌握の旧と新を 1 行ずつ書く。</summary>
    static void PgripTable(MultiNationSim sim, ulong seed, int q)
    {
        var sk = PgripSinks.GetValue(sim, _ => new PgripSink());
        sk.W ??= Open("pgrip", seed);
        if (!sk.Header) { sk.W.WriteLine(PgripHeaderText); sk.Header = true; }
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            double gdpYear = Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value;
            double ratio = !(gdpYear > 0) ? 0 : w.GovDebt.Value / gdpYear;
            double dOld = PowerShares.DebtGrip(w, false), dNew = PowerShares.DebtGrip(w, true);
            double dUsed = w.Coef.ParliamentDebtGripYearGdp > 0 ? dNew : dOld;
            double gOld = PowerShares.ParliamentGripOf(w, false), gNew = PowerShares.ParliamentGripOf(w, true);
            double peOld = PowerShares.ParliamentEffectiveOf(w, gOld), peNew = PowerShares.ParliamentEffectiveOf(w, gNew);
            sk.W.WriteLine(string.Join('	', q.ToString(inv), w.Year.ToString(inv), NameOf(w), F(ratio), F(PowerShares.TaxGrip(w)), F(dOld), F(dNew), F(dUsed),
                F(gOld), F(gNew), F(PowerShares.ParliamentGrip(w)), F(peOld), F(peNew),
                F(PowerShares.CivilianInstitutionsOf(w, peOld)), F(PowerShares.CivilianInstitutionsOf(w, peNew))));
        }
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：書き出しを閉じる。</summary>
    static void PgripClose(MultiNationSim sim)
    {
        if (PgripSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
