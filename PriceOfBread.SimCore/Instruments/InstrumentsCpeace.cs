namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（月ごとの行の一覧は計器の中だけ）。〔履歴 InstrumentsCpeace.cs#1〕
public static partial class Instruments
{
    internal const string CpeaceHeaderText = "month\tyear\tloser\tnCollapse\tnOther\tnGrouped\tu2Count\tu3Count\tsaMaxIndex\trepOldIndex\trepNewIndex\trepRealIndex\tbudOldIndex\tbudNewIndex\tbudRealIndex\tcapturedCount\tcedeEligCount\tcedeRealCount\tshares\trepMovedIndex\trepExpectIndex\trepMovedOverExpect\trepFactors";

    internal sealed class CpRec
    {
        public int Month, Year, Gid; public string Loser = "", Winner = "";
        public bool Collapsed, Hit, Ceded, Valid;
        public double SaJ, AbsJ, WinPush, LosePush, RepUnit, CapBudget, TerritoryScore, ShareReal;
        public double RepRealR, RepMoved, Budget0, Fr; public int Captured;
        public double RepUnit0, RepCutW = double.NaN, TpMultW = double.NaN, PayRW = double.NaN;   // RepCutW・TpMultW・PayRW＝ResolvePeace が実際の rep に掛けた会議の削り・条約港の倍率・支払い率（戦争ごと）〔履歴 InstrumentsCpeace.cs#2〕
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
    internal static void CpIter(War war, bool collapsed) { if (!Tg.Cede) return; CpIterFlags.Remove(war); CpIterFlags.Add(war, new System.Runtime.CompilerServices.StrongBox<bool>(collapsed)); }

    internal static void CpMonth(Diplomacy d, int month, int year) { CweMonth(d, month, year); if (!Tg.Cede && !Tg.War) return; var st = CpOf(d); st.Month = month; st.Year = year; }

    internal sealed class CpPre { public CpRec Rec = new(); }

    /// <summary>講和の前（ResolvePeace を呼ぶ前）：入力から式で書ける物を記録する。</summary>
    internal static CpPre? CpBefore(Diplomacy d, Coefficients c, War war, Diplomacy.CollapseGroup? cg, double share)
    {
        d.CpHit = false; d.CpCeded = false;
        if (!Tg.Cede) return null;   // (1329) 書く表（CpAfter 以後）が選ばれていなければ CpStates を作らない＝入口を閉じる（呼び側は null を読み飛ばす）
        var st = CpOf(d);
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
        r.Fr = fr; r.RepUnit0 = r.Valid ? c.ReparationGdpFraction.Value * gdp : 0; r.RepUnit = r.Valid ? c.ReparationGdpFraction.Value * gdp * fr : 0;   // 国を持たない側がいる講和は賠償が無い（単位 0）。取り分の分母〔勝者の押し〕には残す＝エンジンの取り分と同じ   // 戦勝点 1 の賠償の元×支払い率（会議の削りと条約港の倍率の前）
        if (cg is not null) { if (!st.Gids.TryGetValue(cg, out int gid)) st.Gids[cg] = gid = ++st.GidSeq; r.Gid = gid; r.ShareReal = share; }
        else r.ShareReal = double.NaN;
        return new CpPre { Rec = r };
    }

    /// <summary>講和の後：ResolvePeace が書いた実際の値を足して行を積む。</summary>
    internal static void CpAfter(Diplomacy d, War war, CpPre pre)
    {
        if (!Tg.Cede) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        var r = pre.Rec;
        r.Hit = d.CpHit;
        if (!r.Hit) return;   // 白紙・内戦・賠償の枝の前で終わった講和は事象に入れない（国を持たない側がいる講和は入れる＝取り分の分母に押しが残る）
        r.Budget0 = d.CpBudget0; r.Captured = d.CpCaptured; r.Ceded = d.CpCeded;
        r.RepCutW = d.CpRepCut; r.TpMultW = d.CpTpMult; r.PayRW = d.CpPayR;   // RepCutW〔履歴 InstrumentsCpeace.cs#3〕
        r.RepMoved = d.CpRep;   // 実際に移った賠償（ResolvePeace の rep＝会議の削り・条約港の倍率・支払い率 r の後）
        r.RepRealR = r.Valid ? d.CpRepRaw * r.Fr : 0;   // 賠償の元（戦勝点は集合の 1 回分×取り分）×講和の前の支払い率（会議の削りと条約港の倍率の前）
        CpOf(d).Recs.Add(r);
    }

    static void CpTable(Sink sk, Diplomacy d)
    {
        if (!Tg.Cede) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!CpStates.TryGetValue(d, out _)) return;
        using var wr = Open("cpeace", sk.Seed);
        wr.WriteLine(CpeaceHeaderText);
        foreach (var line in CpRowsPending(d)) wr.WriteLine(line);
    }

    /// <summary>
    /// CpTable はこれを書くだけ。〔履歴 InstrumentsCpeace.cs#4〕
    /// </summary>
    internal static System.Collections.Generic.List<string> CpRowsPending(Diplomacy d)
    {
        var rows = new System.Collections.Generic.List<string>();
        if (!CpStates.TryGetValue(d, out var st)) return rows;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
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
            double saMax = 0, repOld = 0, repNew = 0, repReal = 0, repMoved = 0, budOld = 0, budNew = 0, budReal = 0, repExpect = 0;
            double wsum = 0;
            foreach (var r in lst) if (r.Collapsed) { wsum += r.WinPush > 0 ? r.WinPush : 0; saMax = System.Math.Max(saMax, r.SaJ); }
            var sh = new System.Text.StringBuilder(); var fac = new System.Text.StringBuilder();
            foreach (var r in lst)
            {
                if (r.Collapsed) nCol++; else nOth++;
                if (r.Gid > 0) { nGrp++; if (!r.Collapsed) u2++; }
                if (!r.Collapsed) continue;
                double fsh = wsum > 0 ? (r.WinPush > 0 ? r.WinPush : 0) / wsum : 1.0 / System.Math.Max(1, nCol);   // 式で書いた取り分（勝者の押しの累計の比・エンジンの取り分を読まない）
                repOld += r.RepUnit * r.SaJ; budOld += r.SaJ * r.CapBudget;               // SaJ〔履歴 InstrumentsCpeace.cs#5〕
                repNew += r.RepUnit * saMax * fsh; budNew += r.CapBudget * saMax * fsh;   // 新＝1 回分の戦勝点（最大）×式の取り分
                if (r.Valid) repExpect += r.RepUnit0 * saMax * fsh * (1.0 - r.RepCutW) * r.TpMultW * r.PayRW;   // repExpect〔履歴 InstrumentsCpeace.cs#6〕
                if (fac.Length > 0) fac.Append('/');
                fac.Append(F(r.RepCutW)).Append(':').Append(F(r.TpMultW)).Append(':').Append(F(r.PayRW));
                repReal += r.RepRealR; repMoved += r.RepMoved; budReal += r.Budget0; capt += r.Captured;
                if (r.AbsJ >= r.TerritoryScore) elig++;
                if (r.Ceded) cede++;
                double used = r.Gid > 0 ? r.ShareReal : fsh;
                if (r.WinPush > 0 && !(used > 0)) u3++;
                if (sh.Length > 0) sh.Append('/');
                sh.Append(r.Winner).Append(':').Append(F(fsh)).Append(':').Append(F(r.Gid > 0 ? r.ShareReal : double.NaN));
            }
            if (nCol == 0) continue;
            rows.Add(string.Join('\t', I(key.Item1), I(lst[0].Year), key.Item2, I(nCol), I(nOth), I(nGrp), I(u2), I(u3), F(saMax), F(repOld), F(repNew), F(repReal), F(budOld), F(budNew), F(budReal), I(capt), I(elig), I(cede), sh.ToString(), F(repMoved), F(repExpect), F(repExpect > 0 ? repMoved / repExpect : double.NaN), fac.ToString()));
        }
        return rows;
    }
}
