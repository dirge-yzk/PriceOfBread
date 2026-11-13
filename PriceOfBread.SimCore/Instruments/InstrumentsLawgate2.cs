using System.Runtime.CompilerServices;

namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（pending は計器の側の表）。〔履歴 InstrumentsLawgate2.cs#1〕
public static partial class Instruments
{
    internal const string Lawgate2HeaderText = "quarter\tyear\tnation\taxis\tengineCode\treasonCode\tcurStage\ttargetStage\tausterityIndex\tbureaucracyCode";

    internal readonly record struct Lawgate2Row(int Idx, PolicyAxis Axis, int EngineCode, int ReasonCode, int Cur, int Target, double Austerity, int Bureaucracy);

    sealed class Lawgate2Pend { public readonly Dictionary<int, Lawgate2Row> Rows = new(); }
    static readonly ConditionalWeakTable<World, Lawgate2Pend> Lawgate2Table = new();

    /// <summary>StepLevel の結果の直後（LevelResult の末尾から）。符号 5・6 の一歩だけ控え、同じ軸に別の結果が来たら消す（最後の結果が戦線に残る規則）。</summary>
    internal static void Lawgate2Note(World w, int code, int ci, int ti)
    {
        var st = S(w);
        int idx = st.GfLevelIdx;
        if (idx < 0 || idx >= Government.LevelLaws.Length) return;
        var pend = Lawgate2Table.GetOrCreateValue(w);
        if (code is not (5 or 6)) { pend.Rows.Remove(idx); return; }
        var axis = Government.LevelLaws[idx].Axis;
        int reason = code == 5 ? Lawgate2ReasonIrreversible(axis) : Lawgate2ReasonPrereq(w, axis, ci + 1, ci);
        pend.Rows[idx] = new Lawgate2Row(idx, axis, code, reason, ci, ti, w.AusterityIntensity, (int)w.Bureaucracy);
    }

    static int Lawgate2ReasonIrreversible(PolicyAxis a) => a is PolicyAxis.ShorterHours or PolicyAxis.ChildLabor ? 21 : 20;

    /// <summary>LawPrereq の順序と同じに、前提が崩れた最初の理由を自分で導く（見つからなければ 0）。</summary>
    static int Lawgate2ReasonPrereq(World w, PolicyAxis a, int target, int cur)
    {
        bool meritOk = w.Bureaucracy >= BureaucracyLaw.MeritExam;
        if (a == PolicyAxis.SocialSecurity)
        {
            double room = PoliticalQuantities.Clamp01(1.0 - w.AusterityIntensity) * Government.SocialSecurityMaxLevel;
            if (target > (int)System.Math.Floor(room) && target > cur) return 8;
        }
        switch (a)
        {
            case PolicyAxis.Suffrage: return target >= (int)Suffrage.Universal && !w.WomenSuffrage ? 14 : 0;
            case PolicyAxis.ConscriptionLaw: return target >= (int)ConscriptionLaw.Universal && target > cur && w.SerfEmancipation != ReformLevel.Full ? 10 : 0;
            case PolicyAxis.SecretBallot: return target >= (int)SecretBallotLaw.Secret && w.Suffrage < Suffrage.Census ? 9 : 0;
            case PolicyAxis.ColonialSuffrage:
            case PolicyAxis.MigrantNativeSuffrage: return target >= 1 && w.Suffrage < Suffrage.Census ? 9 : 0;
            case PolicyAxis.SocialSecurity: return target >= (int)SocialSecurityLaw.Unemployment && !meritOk ? 11 : 0;
            case PolicyAxis.Antitrust: return target >= (int)AntitrustLaw.CartelBan && !meritOk ? 11 : 0;
            case PolicyAxis.Nationalization:
                return target >= 1 && !meritOk ? 11 : 0;
            case PolicyAxis.NationalizationAll:
                if (target >= 1 && !meritOk) return 11;
                return w.Polity != Polity.ProletarianDictatorship ? 12 : 0;
            case PolicyAxis.WomenRights: return target >= (int)WomenRightsLaw.Suffrage && w.Suffrage < Suffrage.AdultMale ? 13 : 0;
            case PolicyAxis.StateReligion: return target >= (int)StateReligionLaw.StateAtheism && w.Polity != Polity.ProletarianDictatorship ? 12 : 0;
        }
        return 0;
    }

    /// <summary>番人（テスト）が 1 回の Government.Step の後の控えを読む口（四半期の表の行と同じ物・戦線に残った符号と合うものだけ）。</summary>
    internal static IReadOnlyList<Lawgate2Row> Lawgate2RowsOf(World w)
    {
        var res = new List<Lawgate2Row>();
        if (!Lawgate2Table.TryGetValue(w, out var pend)) return res;
        var st = S(w);
        foreach (var r in pend.Rows.Values.OrderBy(x => x.Idx))
            if (FindFront(st, 'L', r.Idx) is { } f && f.Code == r.EngineCode) res.Add(r);
        return res;
    }

    static void Lawgate2Fold(Sink sk, ulong seed, World w, int q)
    {
        var rows = Lawgate2RowsOf(w);
        if (Lawgate2Table.TryGetValue(w, out var pend)) pend.Rows.Clear();
        if (rows.Count == 0) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Lawgate2 ??= Open("lawgate2", seed);
        if (!sk.Lawgate2Header) { sk.Lawgate2.WriteLine(Lawgate2HeaderText); sk.Lawgate2Header = true; }
        foreach (var r in rows)
            sk.Lawgate2.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), r.Axis.ToString(), I(r.EngineCode), I(r.ReasonCode), I(r.Cur), I(r.Target),
                r.Austerity.ToString("R", System.Globalization.CultureInfo.InvariantCulture), I(r.Bureaucracy)));
    }
}
