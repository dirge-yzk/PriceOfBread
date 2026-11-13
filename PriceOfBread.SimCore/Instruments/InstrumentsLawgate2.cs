using System.Runtime.CompilerServices;

namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (908) 施行の否決の理由の表 lawgate2_s（読むだけ・事前登録 measurements/m908_20261107_prereg_sm.txt [3]・素案 measurements/m900_20261106/s19/draft_00list_and_prereg900.md §3）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（pending は計器の側の表）。INS=1 のときだけ動く。
// 段の法の施行（Government.StepLevel）が〈前提で固定（符号 6）〉か〈不可逆で固定（符号 5）〉だった一歩を 1 行ずつ、四半期の末に書く（国×四半期×段の法の表の行）。
// 理由は計器が自分で導く＝Government.LawPrereq・GateInfo・UniversalConscriptionOpen を呼ばない。StepLevel の分岐の結果（符号）と、その時の盤面の欄（段・緊縮・官僚制度・参政権・政体・農奴解放）だけを読む。
//   LawPrereq の順序と同じ：第 1 の switch（社会保障の緊縮の上限）→第 2 の switch（参政権・農奴解放・官僚制度・政体・男性普選）。
// 列：quarter・year・nation・axis（PolicyAxis の名）・engineCode（5 不可逆／6 前提）・reasonCode・curStage・targetStage・austerityIndex・bureaucracyCode。
// reasonCode：8 緊縮の上限／9 参政権（制限選挙以上）／10 農奴解放（全面）／11 官僚制度（資格任用以上）／12 プロレタリア独裁のみ／13 男性普通選挙／14 女性の権利（選挙権）／
//   20 後退不能（符号 5 の農奴解放・土地改革・保護された法）／21 戦時のみ緩和可（符号 5 の労働時間・児童労働）／0 計器が導けなかった（導出の欠け＝試験が 0 行を要求する）。
//   素案の 1〜7（前段階・払えない請求・教育法・緊縮の下限・総動員・軍事費になし・戦時の解散）は StepLevel の呼び手（段の法の表 Government.LevelLaws の行）に届かない理由（税・支出・軍事費は Move）＝構造的に 0 行。
// ⚠限界（計器が自己申告する）：①理由は計器が自分で導く写し＝LawPrereq の前提が増えれば reasonCode 0 の行が立つ（0 の行数が欠けの検知）。②農奴解放の判定（農奴解放が全面でない）は
//   Privileges.SeigneurialDegree＞0 の写し（なし＝1・限定＝0.5・全面＝0）。③1 四半期に StepLevel が同じ軸へ複数回来れば最後の結果だけ（戦線の符号 openFrontStates と同じ規則）。
//   ④行は nation_s の戦線の符号と同じ述語（開いた戦線に数えられ、四半期の末にその符号が残る）で出す＝下側の門（行数＝戦線の数）は述語が同じなので、理由の導出の欠けだけを拾う。
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
