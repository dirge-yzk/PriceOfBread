"""[計測の写しだけ] (171) 線M 後半＝戦争の規模の計器（読むだけ）。環境変数 POB_W171_DIR の下に w<seed>.txt。未設定なら1命令も走らない。
  WS 行＝戦争1本の始まり（AddBelligerent の Wars.Add の後）：通し番号・月・年・段（同盟の連鎖の深さ）・内戦・分離・両者の名・国か・ランク・兵士・総動員・徴兵法・軍事費の段
  WE 行＝戦争1本の終わり（EndWar の頭）：通し番号・終わった年・月数・理由（0 国の除去・1 決定的〔押しと運〕・2 A の崩壊・3 B の崩壊・4 両方の崩壊・5 双方の厭戦・6 調停）・
         戦勝点・押しの累計 A/B・押しの項の累計・運の累計・|運|の累計・終わりの厭戦 A/B・崩壊の線（cut）A/B・戦死 A/B・
         この戦争の間の厭戦の項の増分 A/B（基礎・経済・封鎖・財政・空襲・化学・戦死）・終わりの兵士 A/B・兵士の月和 A/B・総動員の月 A/B・装備の充足の月和 A/B・支払い率の月和 A/B・軍事費の段の月和 A/B・
         戦死の上限に張り付いた月 A/B（国の単位＝その月の全戦線）
  Q 行＝国×四半期の軍の財布（MonthSettle の国家の支払い率 r が決まった直後の月の和）：器・給与の満額・規定の給与の満額・給与の充足・装備の上限（給与の後の器）・
         装備の請求（首都・r の前）・軍需の必要額（首都）・幹部の必要額・幹部の請求・請求の総額・払える額・r・装備の充足（前月）・戦時の倍率（首都）・兵士・
         交戦の月・内戦の月・総動員の月・軍事費の段・歳入・国庫と国債と台帳（四半期末の累計：借入・切り下げ・陸・海）
力学は読まない（加算と記録だけ）。使い方：python inj_war171.py <木の根>（inj_cas を当てた木に当てる）"""
import sys
root = sys.argv[1]


def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    crlf = (CR + LF) in s
    for old, new in pairs:
        if crlf: old = old.replace(LF, CR + LF); new = new.replace(LF, CR + LF)
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail.replace(LF, CR + LF) if crlf else tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


TAIL = r'''

/// <summary>[計測の写しだけ] (171) 戦争の規模の計器（読むだけ）。</summary>
public static class W171
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_W171_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    const int NE = 7;   // 厭戦の項：基礎・経済・封鎖・財政・空襲・化学・戦死
    public sealed class Rec
    {
        public int Id, T0, Y0, Depth, Reason = -1, YEnd = -1, Months;
        public string A = "", B = "";
        public int Civil, Sec, ANat, BNat, ARank, BRank, AMob0, BMob0, ALaw, BLaw;
        public double ASold0, BSold0, AStage0, BStage0;
        public double[] AEx0 = new double[NE + 2], BEx0 = new double[NE + 2], AExd = new double[NE + 2], BExd = new double[NE + 2];
        public double PushTerm, Luck, LuckAbs, ExA, ExB, CutA = -1, CutB = -1, Score, PushA, PushB, DeadA, DeadB, ASoldE, BSoldE;
        public double[] Side = new double[12];   // A：兵士の月和・総動員の月・装備の充足の月和・r の月和・段の月和・(予備)／B：同じ（+6）
        public bool Ended;
    }
    public sealed class Acc
    {
        public int Month, NextId;
        public readonly System.Collections.Generic.List<Rec> Recs = new();
        public readonly System.Runtime.CompilerServices.ConditionalWeakTable<War, Rec> ByWar = new();
        public readonly System.Text.StringBuilder QBuf = new();
        public readonly System.Collections.Generic.Dictionary<Power, double[]> Ex = new();   // 厭戦の項の累計（国の単位）＋[NE]=戦死の上限に張り付いた月＋[NE+1]=予備
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, Acc> ByDip = new();
    public static Acc For(object dip) => ByDip.GetValue(dip, _ => new Acc());
    static string NameOf(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;
    static double[] ExOf(Acc a, Power p) { if (!a.Ex.TryGetValue(p, out var v)) a.Ex[p] = v = new double[NE + 2]; return v; }
    public static void Tick(object dip, int month) { For(dip).Month = month; }
    public static void Start(Diplomacy dip, War w, int depth, int year)
    {
        var a = For(dip);
        var r = new Rec { Id = a.NextId++, T0 = a.Month, Y0 = year, Depth = depth, A = NameOf(w.A), B = NameOf(w.B),
            Civil = w.CivilWarParent is not null ? 1 : 0, Sec = w.CivilWarSecession ? 1 : 0, ANat = w.A.Nation != null ? 1 : 0, BNat = w.B.Nation != null ? 1 : 0,
            ARank = (int)dip.RankOf(w.A), BRank = (int)dip.RankOf(w.B) };
        if (w.A.Nation is { } wa) { r.ASold0 = wa.SoldierSizeK(); r.AMob0 = wa.Mobilized ? 1 : 0; r.ALaw = (int)wa.ConscriptionLaw; r.AStage0 = wa.MilitarySpending; }
        if (w.B.Nation is { } wb) { r.BSold0 = wb.SoldierSizeK(); r.BMob0 = wb.Mobilized ? 1 : 0; r.BLaw = (int)wb.ConscriptionLaw; r.BStage0 = wb.MilitarySpending; }
        System.Array.Copy(ExOf(a, w.A), r.AEx0, NE + 2); System.Array.Copy(ExOf(a, w.B), r.BEx0, NE + 2);
        a.Recs.Add(r); a.ByWar.AddOrUpdate(w, r);
    }
    public static void Exh(object dip, Power p, double b, double econ, double blk, double fis, double air, double chem)
    {
        var v = ExOf(For(dip), p); v[0] += b; v[1] += econ; v[2] += blk; v[3] += fis; v[4] += air; v[5] += chem;
    }
    public static void Cas(object dip, Power p, double casTerm, double cas, double casForce)
    {
        var v = ExOf(For(dip), p); v[6] += casTerm; if (casForce > cas) v[NE] += 1;
    }
    public static void Month(object dip, War w, double pushTerm, double luck)
    {
        if (!For(dip).ByWar.TryGetValue(w, out var r)) return;
        r.PushTerm += pushTerm; r.Luck += luck; r.LuckAbs += System.Math.Abs(luck);
        for (int s = 0; s < 2; s++)
        {
            var wn = (s == 0 ? w.A : w.B).Nation; if (wn is null) continue;
            int o = s * 6;
            r.Side[o] += wn.SoldierSizeK(); r.Side[o + 1] += wn.Mobilized ? 1 : 0; r.Side[o + 2] += wn.Region.MilitaryEquipFill;
            r.Side[o + 3] += wn.FiscalFundingRatio; r.Side[o + 4] += wn.MilitarySpending;
        }
    }
    public static void Reason(object dip, War w, bool aCol, bool bCol, bool decisive, bool weary, bool forced, double aCut, double bCut)
    {
        if (!For(dip).ByWar.TryGetValue(w, out var r)) return;
        r.Reason = forced ? 6 : (aCol && bCol) ? 4 : aCol ? 2 : bCol ? 3 : decisive ? 1 : weary ? 5 : 7;
        r.CutA = aCut; r.CutB = bCut;
    }
    public static void End(object dip, War w, int year)
    {
        var a = For(dip);
        if (!a.ByWar.TryGetValue(w, out var r)) return;
        if (r.Reason < 0) r.Reason = 0;
        r.Ended = true; r.YEnd = year; r.Months = w.Months; r.Score = w.Score; r.PushA = w.PushA; r.PushB = w.PushB; r.DeadA = w.DeadA; r.DeadB = w.DeadB;
        r.ExA = w.A.WarExhaustion; r.ExB = w.B.WarExhaustion;
        var ea = ExOf(a, w.A); var eb = ExOf(a, w.B);
        for (int k = 0; k < NE + 2; k++) { r.AExd[k] = ea[k] - r.AEx0[k]; r.BExd[k] = eb[k] - r.BEx0[k]; }
        if (w.A.Nation is { } wa) r.ASoldE = wa.SoldierSizeK();
        if (w.B.Nation is { } wb) r.BSoldE = wb.SoldierSizeK();
    }
    // ── 国×四半期の軍の財布（MonthSettle の月の和）──
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, double[]> Q = new();
    const int NQ = 27;
    public static void Fin(World w, double env, double payFull, double regFull, double payFill, double equipRoom, double equipClaim, double trainNeed, double trainClaim,
        double claims, double payable, double payRate, bool atWar, double sold)
    {
        var v = Q.GetValue(w, _ => new double[NQ]);
        v[0] += 1; v[1] += env; v[2] += payFull; v[3] += regFull; v[4] += payFill; v[5] += equipRoom; v[6] += equipClaim;
        v[7] += EconomyStep.MilitaryProcurementNeed(w.Region, w.Coef); v[8] += trainNeed; v[9] += trainClaim; v[10] += claims; v[11] += payable; v[12] += payRate;
        v[13] += w.Region.MilitaryEquipFill; v[14] += w.Region.MilitaryWarMultLast; v[15] += sold; v[16] += atWar ? 1 : 0; v[17] += w.InCivilWar ? 1 : 0;
        v[18] += w.Mobilized ? 1 : 0; v[19] += w.MilitarySpending; v[20] += w.LastRevenuePerMonth;
    }
    public static void Proc(World? w, double want, double got, double wantRation, double affordRaw)
    {
        if (w is null) return;
        var v = Q.GetValue(w, _ => new double[NQ]);
        v[21] += 1; v[22] += want; v[23] += got; v[24] += wantRation; v[25] += System.Math.Min(1.0, affordRaw >= 0 ? affordRaw : 0);
    }
    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var accQ = For(sim.Diplomacy);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            if (!Q.TryGetValue(w, out var v)) continue;
            var sb = accQ.QBuf;
            sb.Append("Q|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(w.SelfPower is { } sp ? NameOf(sp) : "?");
            for (int k = 0; k < 21; k++) sb.Append('|').Append(F(v[k]));
            var L = w.TreasuryLedger;
            sb.Append('|').Append(F(w.Treasury)).Append('|').Append(F(w.GovDebt)).Append('|').Append(F(L[(int)TreasuryCat.Borrow])).Append('|').Append(F(L[(int)TreasuryCat.Devalue]))
              .Append('|').Append(F(L[(int)TreasuryCat.Army])).Append('|').Append(F(L[(int)TreasuryCat.Navy])).Append('|').Append(F(L[(int)TreasuryCat.Tax]));
            for (int k = 21; k < 26; k++) sb.Append('|').Append(F(v[k]));
            sb.Append('\n');
            System.Array.Clear(v);
        }
        if (q != quarters - 1) return;
        using (var QW = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"q{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16))
        {
            QW.Write("#Q|q|yr|idx|name|months|env|payFull|regFull|payFillSum|equipRoom|equipClaim|need|trainNeed|trainClaim|claims|payable|rSum|eqFillSum|warMultSum|soldSum|warM|cwM|mobM|stageSum|revSum|treasury|debt|ledBorrow|ledDevalue|ledArmy|ledNavy|ledTax|procDays|equipWant|equipGot|wantRation|affordSum\n");
            QW.Write(accQ.QBuf.ToString());
        }
        var acc = For(sim.Diplomacy);
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"w{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#WS|id|t0|y0|depth|civil|sec|A|B|aNat|bNat|aRank|bRank|aSold0|bSold0|aMob0|bMob0|aLaw|bLaw|aStage0|bStage0\n");
        W.Write("#WE|id|ended|yEnd|months|reason|score|pushA|pushB|pushTerm|luck|luckAbs|exA|exB|cutA|cutB|deadA|deadB|aExd(base,econ,blk,fis,air,chem,cas,capM,-)|bExd(...)|aSoldE|bSoldE|aSide(soldSum,mobM,eqSum,rSum,stageSum,-)|bSide(...)\n");
        foreach (var r in acc.Recs)
        {
            W.Write($"WS|{r.Id}|{r.T0}|{r.Y0}|{r.Depth}|{r.Civil}|{r.Sec}|{r.A}|{r.B}|{r.ANat}|{r.BNat}|{r.ARank}|{r.BRank}|{F(r.ASold0)}|{F(r.BSold0)}|{r.AMob0}|{r.BMob0}|{r.ALaw}|{r.BLaw}|{F(r.AStage0)}|{F(r.BStage0)}\n");
            W.Write($"WE|{r.Id}|{(r.Ended ? 1 : 0)}|{r.YEnd}|{r.Months}|{r.Reason}|{F(r.Score)}|{F(r.PushA)}|{F(r.PushB)}|{F(r.PushTerm)}|{F(r.Luck)}|{F(r.LuckAbs)}|{F(r.ExA)}|{F(r.ExB)}|{F(r.CutA)}|{F(r.CutB)}|{F(r.DeadA)}|{F(r.DeadB)}|"
                + string.Join(",", System.Array.ConvertAll(r.AExd, F)) + "|" + string.Join(",", System.Array.ConvertAll(r.BExd, F)) + $"|{F(r.ASoldE)}|{F(r.BSoldE)}|"
                + string.Join(",", System.Array.ConvertAll(r.Side[..6], F)) + "|" + string.Join(",", System.Array.ConvertAll(r.Side[6..], F)) + "\n");
        }
    }
}
'''
_dsrc = open(root + '/PriceOfBread.SimCore/Diplomacy.cs', encoding='utf-8-sig').read()
BASE = 'exBase' if 'p.WarExhaustion + exBase + econEx' in _dsrc else 'baseEx' if 'p.WarExhaustion + baseEx + econEx' in _dsrc else 'c.WarExhaustionRate'   # 腕W・(171) を入れた木にも当たる
edit('PriceOfBread.SimCore/Diplomacy.cs', [
    ("        _embargo.Add(Key(war.A, war.B));   // 戦争中は強制禁輸\n",
     "        _embargo.Add(Key(war.A, war.B));   // 戦争中は強制禁輸\n        if (W171.On) W171.Start(this, war, depth, _year);\n"),
    ("            p.WarExhaustion = Math.Min(1, p.WarExhaustion + " + BASE + " + econEx + blockade + fiscal + airRaid + chem);   // 厭戦も1回\n",
     "            p.WarExhaustion = Math.Min(1, p.WarExhaustion + " + BASE + " + econEx + blockade + fiscal + airRaid + chem);   // 厭戦も1回\n            if (W171.On) W171.Exh(this, p, " + BASE + ", econEx, blockade, fiscal, airRaid, chem);\n"),
    ("        p.WarExhaustion = Math.Min(1, p.WarExhaustion + casTerm);   // 戦死率に比例して厭戦↑\n",
     "        p.WarExhaustion = Math.Min(1, p.WarExhaustion + casTerm);   // 戦死率に比例して厭戦↑\n        if (W171.On) W171.Cas(this, p, casTerm, cas, casForce);\n"),
    ("            war.PushA += aPush; war.PushB += bPush;",
     "            war.PushA += aPush; war.PushB += bPush; if (W171.On) W171.Month(this, war, c.WarPushSpeed * pushShare, luck);"),
    ("            if (decisive || weary || war.ForcedPeace)   // ForcedPeace＝列強の調停（Score は0に置かれている）\n",
     "            if (W171.On && (decisive || weary || war.ForcedPeace)) W171.Reason(this, war, aCollapse, bCollapse, decisive, weary, war.ForcedPeace, aCut, bCut);\n            if (decisive || weary || war.ForcedPeace)   // ForcedPeace＝列強の調停（Score は0に置かれている）\n"),
    ("        EndedWars.Add(new EndedWarRecord",
     "        if (W171.On) W171.End(this, war, _year);\n        EndedWars.Add(new EndedWarRecord"),
    ("        StepWars(c);                   // 戦争の運は盤面非依存ハッシュ",
     "        if (W171.On) W171.Tick(this, month);\n        StepWars(c);                   // 戦争の運は盤面非依存ハッシュ"),
], TAIL)
edit('PriceOfBread.SimCore/Simulation.cs', [
    ("        World.FiscalFundingRatio = payRate;\n",
     "        World.FiscalFundingRatio = payRate;\n        if (W171.On) W171.Fin(World, milEnvelope, payFull, regularFull, soldierPayFill, equipRoom, equipClaimNat, trainNeed, trainClaim, claims, payable, payRate, atWarNation, natSoldiers);\n"),
])
edit('PriceOfBread.SimCore/EconomyStep.cs', [
    ("            region.MilitaryEquipFill = EquipFill(equipWant, equipGot);\n",
     "            region.MilitaryEquipFill = EquipFill(equipWant, equipGot);\n            if (W171.On) { double w171r = 0; for (int b2 = 0; b2 < coef.MilitaryGoods.Length; b2++) { int g2 = coef.MilitaryGoods[b2]; double m2 = EconomyStep.LandMul(coef, g2); w171r += region.MilitaryScale * milRates[b2] * m2 * ration[g2]; } W171.Proc(world, equipWant, equipGot, w171r, region.MilitaryAffordRaw); }\n"),
])
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                if (CasDiag.On) CasDiag.Quarter(sim, seed, q, quarters);\n",
     "                if (CasDiag.On) CasDiag.Quarter(sim, seed, q, quarters);\n                if (W171.On) W171.Quarter(sim, seed, q, quarters);\n"),
])
print('ok war171', root)
