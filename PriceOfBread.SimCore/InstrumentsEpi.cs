namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (629) 疫病 1 件ごとの表 epi_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsMa.cs・InstrumentsSecession.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。
// 目的＝事前登録 measurements/urb_629_20261028_prereg.txt の観測量（疫病が当たった州・件数・州の都市の割合と国の都市の割合と最大の州の割合・死者）を読む。
// 行＝疫病に当たった州 1 件ごと（World.TriggerEpidemic の内部）。**全ての国**を出す（POB_INSTRUMENTS_NATIONS で絞らない）。助走（WarmupFrozen の間）は書かない。
// 列：quarter／year／nation／region／tier／origin（0＝国内の発生・1＝他国からの伝播・2＝同じ疫病の二次の波＝1 つ目を除いた都市の人口の重みで抽選した州）／regionUrbanShare（その州の都市の割合・死の前）／
//   nationUrbanShare（国の都市の人口÷国の人口〔World.UrbanShare〕・死の前）／maxUrbanShare（最も都市化率の高い州の割合〔州の割合の最大〕・死の前）／
//   regionPopKBefore（その州の人口〔千人〕・死の前）／deadK（その州が失った人数〔千人〕）。
// ⚠限界（計器が自己申告する）：①quarter は MultiNationSim が四半期の末に Fold へ渡す番号（月次の途中で起きた疫病は、その四半期の行に載る）②死の前の人口は SumPopulation（active 州の POP の和）③3 つの割合は全て書く。
public static partial class Instruments
{
    internal const string EpiHeaderText = "quarter	year	nation	region	tier	origin	regionUrbanShare	nationUrbanShare	maxUrbanShare	regionPopKBefore	deadK";

    /// <summary>疫病が 1 つの州に当たった所（<see cref="World.TriggerEpidemic"/> の <c>ApplyEpidemicDeaths</c> の直後）：行を溜める。読むだけ。四半期の末の <see cref="EpiFold"/> が書いて消す。</summary>
    internal static void EpidemicHit(World w, Region r, int origin, double regionShare, double maxShare, double nationShare, double popBefore, double dead)
    {
        var st = S(w);
        st.EpiRows ??= new List<string>();
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        st.EpiRows.Add(string.Join('	', NameOf(w), r.Name, r.Tier.ToString(), I(origin), F(regionShare), F(nationShare), F(maxShare), F(popBefore), F(dead)));
    }

    static void EpiFold(Sink sk, ulong seed, World w, WState st, int q)
    {
        var rows = st.EpiRows;
        st.EpiRows = null;
        if (rows is null) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Epi ??= Open("epi", seed);
        if (!sk.EpiHeader) { sk.Epi.WriteLine(EpiHeaderText); sk.EpiHeader = true; }
        string qs = I(q), year = I(w.Year);
        foreach (var row in rows)
        {
            // 行は nation から始まる＝quarter・year を前に足す（列の順は見出しのとおり）
            sk.Epi.WriteLine(qs + "	" + year + "	" + row);
        }
    }
}
