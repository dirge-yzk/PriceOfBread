namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (791) 崩壊の講和の表 cpeace_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCess.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（月ごとの行の一覧は計器の中だけ）。
// 目的＝旗 CollapsePeaceOneShare（崩壊の講和の取り分を 1 回分にする）の旧新を同じ盤面で読む。旗 0 でも旗 1 でも同じ列を書く（旗 0 は反実仮想の〈1 回分〉を式で並べる）。
// 取り方＝講和 1 本ごとに Diplomacy.ConcludeWar が講和の前後で 1 行ずつ呼ぶ（CpBefore＝講和の前の入力・CpAfter＝ResolvePeace が書いた実際の値）。
//   ⚠期待の側（rep_旧・rep_新・予算の旧新・取り分）は ResolvePeace を呼ばず、前の入力から式で書く＝エンジンの実装の写しにならない。実際の側（rep_実・予算の実・割譲の実）だけ ResolvePeace が書いた値。
//   崩壊の判定は月の歩の入口（CpIter）が戦争ごとに控えた値（その戦争の歩で aCollapse||bCollapse だったか）＝保留の判断（cpLoser）とは別の経路で読むので、集合に崩壊でない講和が混ざれば U2 に出る。
// 行＝崩壊の事象（月×敗者）ごとに 1 行。事象＝その月にその敗者が負けた外交の講和で、白紙でなく賠償の枝（hit）まで進んだ物の全て。nCollapse＝うち崩壊と書き直せた物の数。
// 列：month／year／loser／nCollapse／nOther（崩壊でない講和＝押しで決まった物の数）／nGrouped（旗 1 の集合に入った講和の数）／u2Count（集合の中の崩壊でない講和の数）／u3Count（押しが正なのに取り分 0 の勝者の数）／
//   saMaxIndex（崩壊の講和の |クランプ後の戦勝点| の最大）／repOldIndex（Σ 賠償の元の単位×sa〔戦争ごと〕）／repNewIndex（Σ 賠償の元の単位×saMax×式の取り分〔1 回分〕）／repRealIndex（実際の Σ 賠償の元×支払い率〔会議の削りと条約港の倍率の前〕）／
//   budOldIndex／budNewIndex／budRealIndex（植民地の奪取の予算＝Σ sa×ColonyCaptureBudget／Σ saMax×ColonyCaptureBudget×式の取り分／実際の Σ 予算）／capturedCount（実際に奪った植民地の数）／
//   cedeEligCount（本国の割譲の門〔|戦勝点|≧TerritoryCessionScore〕を通る崩壊の講和の数＝旧の割譲の最大回数）／cedeRealCount（実際に州が移った講和の数）／shares（勝者ごとの取り分〔押しの累計の比・式〕と実際に使った取り分）。
// ⚠限界（計器が自分で申告する）：①repUnit は講和の時点の敗者の GdpPerMonth×支払い率 r を戦争ごとに読む（割譲で GDP が動けば戦争ごとにずれる＝repNew は最初の講和の値）②会議の削りと条約港の倍率は repReal から除いてある
//   （期待の側にも入れていない＝比に出ない）。repNew は戦争ごとの講和の時点の単位を使う（割譲で敗者の支払い率が動いても期待が実際とずれない）③旗 0 では nGrouped と実際の取り分は 0／nan。
public static partial class Instruments
{
    internal const string CpeaceHeaderText = "month\tyear\tloser\tnCollapse\tnOther\tnGrouped\tu2Count\tu3Count\tsaMaxIndex\trepOldIndex\trepNewIndex\trepRealIndex\tbudOldIndex\tbudNewIndex\tbudRealIndex\tcapturedCount\tcedeEligCount\tcedeRealCount\tshares";

    internal sealed class CpRec
    {
        public int Month, Year, Gid; public string Loser = "", Winner = "";
        public bool Collapsed, Hit, Ceded, Valid;
        public double SaJ, AbsJ, WinPush, LosePush, RepUnit, CapBudget, TerritoryScore, ShareReal;
        public double RepRealR, Budget0, Fr; public int Captured;
    }
    sealed class CpState
    {
        public int Month, Year, GidSeq;
        public readonly System.Collections.Generic.Dictionary<Diplomacy.CollapseGroup, int> Gids = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        public readonly System.Collections.Generic.List<CpRec> Recs = new();
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, CpState> CpStates = new();
    static CpState CpOf(Diplomacy d) => CpStates.GetValue(d, _ => new CpState());

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<War, System.Runtime.CompilerServices.StrongBox<bool>> CpIterFlags = new();
    internal static void CpIter(War war, bool collapsed) { CpIterFlags.Remove(war); CpIterFlags.Add(war, new System.Runtime.CompilerServices.StrongBox<bool>(collapsed)); }

    internal static void CpMonth(Diplomacy d, int month, int year) { var st = CpOf(d); st.Month = month; st.Year = year; }

    internal sealed class CpPre { public CpRec Rec = new(); }

    /// <summary>講和の前（ResolvePeace を呼ぶ前）：入力から式で書ける物を記録する。</summary>
    internal static CpPre CpBefore(Diplomacy d, Coefficients c, War war, Diplomacy.CollapseGroup? cg, double share)
    {
        var st = CpOf(d);
        d.CpHit = false; d.CpCeded = false;
        double s = PoliticalQuantities.ClampOr(war.Score, -1, 1, 0);
        bool aWins = s > 0;
        Power loser = aWins ? war.B : war.A, winner = aWins ? war.A : war.B;
        var r = new CpRec
        {
            Month = st.Month, Year = st.Year, Loser = NameOf(loser), Winner = NameOf(winner),
            SaJ = System.Math.Abs(s), AbsJ = System.Math.Abs(war.Score), WinPush = aWins ? war.PushA : war.PushB, LosePush = aWins ? war.PushB : war.PushA,
            CapBudget = c.ColonyCaptureBudget, TerritoryScore = c.TerritoryCessionScore,
        };
        r.Collapsed = CpIterFlags.TryGetValue(war, out var cf) && cf.Value;   // その戦争の月の歩で崩壊と判定されたか（CpIter が記録した値＝保留の判断とは別の経路）
        r.Valid = loser.Nation is not null && winner.Nation is not null;       // 賠償は貨幣経済を持つ 2 国の間だけ
        double gdp = loser.GdpPerMonth.Value, fr = loser.Nation?.FiscalFundingRatio.Value ?? double.NaN;
        r.Fr = fr; r.RepUnit = c.ReparationGdpFraction.Value * gdp * fr;   // 戦勝点 1 の賠償の元×支払い率（会議の削りと条約港の倍率の前）
        if (cg is not null) { if (!st.Gids.TryGetValue(cg, out int gid)) st.Gids[cg] = gid = ++st.GidSeq; r.Gid = gid; r.ShareReal = share; }
        else r.ShareReal = double.NaN;
        return new CpPre { Rec = r };
    }

    /// <summary>講和の後：ResolvePeace が書いた実際の値を足して行を積む。</summary>
    internal static void CpAfter(Diplomacy d, War war, CpPre pre)
    {
        var r = pre.Rec;
        r.Hit = d.CpHit;
        if (!r.Hit || !r.Valid) return;   // 白紙・内戦・賠償の枝の前で終わった講和は事象に入れない
        r.Budget0 = d.CpBudget0; r.Captured = d.CpCaptured; r.Ceded = d.CpCeded;
        r.RepRealR = d.CpRepRaw * r.Fr;   // 賠償の元（戦勝点は集合の 1 回分×取り分）×講和の前の支払い率（会議の削りと条約港の倍率の前）
        CpOf(d).Recs.Add(r);
    }

    static void CpTable(Sink sk, Diplomacy d)
    {
        if (!CpStates.TryGetValue(d, out var st)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        using var wr = Open("cpeace", sk.Seed);
        wr.WriteLine(CpeaceHeaderText);
        var groups = new System.Collections.Generic.Dictionary<(int, string), System.Collections.Generic.List<CpRec>>();
        var order = new System.Collections.Generic.List<(int, string)>();
        foreach (var r in st.Recs)
        {
            var key = (r.Month, r.Loser);
            if (!groups.TryGetValue(key, out var lst)) { groups[key] = lst = new(); order.Add(key); }
            lst.Add(r);
        }
        foreach (var key in order)
        {
            var lst = groups[key];
            int nCol = 0, nOth = 0, nGrp = 0, u2 = 0, u3 = 0, capt = 0, elig = 0, cede = 0;
            double saMax = 0, repOld = 0, repNew = 0, repReal = 0, budOld = 0, budNew = 0, budReal = 0;
            double wsum = 0;
            foreach (var r in lst) if (r.Collapsed) { wsum += r.WinPush > 0 ? r.WinPush : 0; saMax = System.Math.Max(saMax, r.SaJ); }
            var sh = new System.Text.StringBuilder();
            foreach (var r in lst)
            {
                if (r.Collapsed) nCol++; else nOth++;
                if (r.Gid > 0) { nGrp++; if (!r.Collapsed) u2++; }
                if (!r.Collapsed) continue;
                double fsh = wsum > 0 ? (r.WinPush > 0 ? r.WinPush : 0) / wsum : 1.0 / System.Math.Max(1, nCol);   // 式で書いた取り分（勝者の押しの累計の比・エンジンの取り分を読まない）
                repOld += r.RepUnit * r.SaJ; budOld += r.SaJ * r.CapBudget;               // 旧＝戦争ごとに自分の戦勝点で満額
                repNew += r.RepUnit * saMax * fsh; budNew += r.CapBudget * saMax * fsh;   // 新＝1 回分の戦勝点（最大）×式の取り分
                repReal += r.RepRealR; budReal += r.Budget0; capt += r.Captured;
                if (r.AbsJ >= r.TerritoryScore) elig++;
                if (r.Ceded) cede++;
                double used = r.Gid > 0 ? r.ShareReal : fsh;
                if (r.WinPush > 0 && !(used > 0)) u3++;
                if (sh.Length > 0) sh.Append('/');
                sh.Append(r.Winner).Append(':').Append(F(fsh)).Append(':').Append(F(r.Gid > 0 ? r.ShareReal : double.NaN));
            }
            if (nCol == 0) continue;
            wr.WriteLine(string.Join('\t', I(key.Item1), I(lst[0].Year), key.Item2, I(nCol), I(nOth), I(nGrp), I(u2), I(u3), F(saMax), F(repOld), F(repNew), F(repReal), F(budOld), F(budNew), F(budReal), I(capt), I(elig), I(cede), sh.ToString()));
        }
    }
}
