namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsEpi.cs#1〕
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
