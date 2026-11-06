# (895) c76ls 突然変異の置き換え（バイトで読み書き＝改行と BOM を保つ・ちょうど 1 か所でなければ失敗）。事前登録 measurements/m895_20261106_prereg_ls.txt [3] の突然変異の表。
#   python -X utf8 mutapply895.py <木の根> <番号>
import sys, os

E = 'PriceOfBread.SimCore/EconomyStep.cs'
S = 'PriceOfBread.SimCore/Simulation.cs'
L = 'PriceOfBread.SimCore/InstrumentsLandshare.cs'
C = 'PriceOfBread.SimCore/InstrumentsCornint.cs'
A = 'PriceOfBread.SimCore/InstrumentsCearn.cs'

MUT = {
    # M1a 割合 0 の枠も輸出の並びに入れる
    'M1a': [(E, "                if (ls[k] > 0) { to.Add(rr.Pops[LandShareRecipient(rr, pr, k)]); wt.Add(rent * ls[k]); }",
                "                { to.Add(rr.Pops[LandShareRecipient(rr, pr, k)]); wt.Add(rent * ls[k]); }")],
    # M1b 割合 0 の枠も国内の繰り返しに入れる（予言：T2 は緑＝rent×0.0 の足し算はビット不変）
    'M1b': [(E, "            if (ls is not null && !(ls[k] > 0)) continue;   // 割合が 0（と NaN）の枠は並べない\n", "")],
    # M2 輸出の側だけ割らない
    'M2': [(E, "        if (pr.LandShares is { } ls && !Colonial.AbsenteeRgo(rr, pr))", "        if (pr.LandShares is { } ls && false)")],
    # M3 領主権の記録を割らない（総額を記録）
    'M3': [(E, "Privileges.Record(world, Privileges.Seigneurial, rk * Privileges.SeigneurialDegree(world));", "Privileges.Record(world, Privileges.Seigneurial, rent * Privileges.SeigneurialDegree(world));")],
    # M4 種を割らない（欄ありでも持ち主へ全額）
    'M4': [(S, "for (int k = 0, nk = pr.LandShares is { } ls0 ? ls0.Length : 1; k < nk; k++)", "for (int k = 0, nk = 1; k < nk; k++)"),
           (S, "new MoneyPerMonth(pr.LandShares is { } lsk ? pr.RentShare.Value * pr.Output * Catalog.RefPrices[pr.GoodId] * InitialRentSeedScale * lsk[k] :", "new MoneyPerMonth(pr.LandShares is { } lsk && false ? 0.0 :")],
    # M5 null を 1.0 でなく 0 と読む
    'M5': [(E, "            double rk = ls is null ? rent : rent * ls[k];", "            double rk = ls is null ? 0.0 : rent * ls[k];")],
    # M6 賦存の RGO にも当てる
    'M6': [(S, "                if (pr.IsRgo && System.Array.IndexOf(resources, pr.GoodId) < 0 && pr.OwnerPopIndex >= 0", "                if (pr.IsRgo && pr.OwnerPopIndex >= 0")],
    # M7 生産者で同じ配列を共有する
    'M7': [(S, "                    pr.LandShares = (double[])lo.Clone();", "                    pr.LandShares = lo;")],
    # M8 受け手ごとに EarnedPerMonth += の行を足す
    'M8': [(E, "            region.Pops[rp].EarnedPerMonth += new MoneyPerMonth(rk);\n",
               "            if (k == 0) region.Pops[rp].EarnedPerMonth += new MoneyPerMonth(rk); else region.Pops[rp].EarnedPerMonth += new MoneyPerMonth(rk);\n")],
    # M9 訂正 1 の (a)（送金の項）を外す
    'M9': [(L, "            if (remit > 0)\n", "            if (false && remit > 0)\n")],
    # M10b 計器の比べを壊す（相対の差を常に 0）
    'M10b': [(L, "        return tot > 0 ? d / tot : d > 0 ? double.PositiveInfinity : d == 0 ? 0.0 : double.NaN;", "        return 0.0 * tot;")],
    # M11 期待を受け手の ClassId でなく枠の番号（null は枠 0＝貴族）で積む
    'M11': [(L, "        if (ls is null) g.Exp[ownerCls] += rent;", "        if (ls is null) g.Exp[0] += rent;")],
    # M12 期待に rent≦0 も積む（受け口の門を外す）
    'M12': [(L, "        if (!(rent > 0)) return;   // LandRent が立つのは rent>0 の分だけ\n", "")],
    # M13 data の側で賦存の RGO にもデータを掛ける
    'M13': [(L, "        if (d is not null && !endow && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble)", "        if (d is not null && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble)")],
    # M14 実際の側に植民地格の州を含める
    'M14': [(L, "                        for (int k = 0; k < pre.Length && k < r.Pops.Count; k++) remit += System.Math.Max(0, pre[k] - r.Pops[k].LandRentPerMonth.Value);\n                    continue;\n",
                "                        for (int k = 0; k < pre.Length && k < r.Pops.Count; k++) remit += System.Math.Max(0, pre[k] - r.Pops[k].LandRentPerMonth.Value);\n")],
    # M15 欄を持つ州だけの列を全ての州で積む
    'M15': [(L, "                if (field) { LsAdd(actF, actR); if (g is not null) LsAdd(expF, g.Exp); }", "                { LsAdd(actF, actR); if (g is not null) LsAdd(expF, g.Exp); }")],
    # M16 欄の和のずれを常に 0
    'M16': [(L, "                    if (dev > sumDev) sumDev = dev;\n", "")],
    # M17 非賦存の持ち主の階級の列に賦存も積む
    'M17': [(L, "        if (endow) a.MEndow += rent; else { a.MNonEndow += rent; a.MByOwner[ownerCls] += rent; }", "        a.MByOwner[ownerCls] += rent; if (endow) a.MEndow += rent; else { a.MNonEndow += rent; }")],
    # M19 データ無しの data の側を持ち主に積まない
    'M19': [(L, "        else g.Dat[ownerCls] += rent;\n", "")],
    # M20 実際の側に LandRent でなく所得を読む
    'M20': [(L, "                foreach (var p in r.Pops) actR[LsClass(p.ClassId)] += p.LandRentPerMonth.Value;", "                foreach (var p in r.Pops) actR[LsClass(p.ClassId)] += p.EarnedPerMonth.Value;")],
    # M21 計器の受け手の写しを壊す（教会の枠を持ち主へ）
    'M21': [(L, "        int i = k == 0 ? s.OwnerPopIndex : k == 1 ? r.ClergyPopIndex :", "        int i = k == 0 ? s.OwnerPopIndex : k == 1 ? s.OwnerPopIndex :")],
    # M22 エンジンの受け手を壊す（教会の枠を投資家へ）＝計器の写しと食い違う
    'M22': [(E, "        int i = slot switch { 0 => s.OwnerPopIndex, 1 => region.ClergyPopIndex,", "        int i = slot switch { 0 => s.OwnerPopIndex, 1 => region.InvestorPopIndex,")],
    # M23 cornint の所有の値の持ち分化を外す（欄ありでも持ち主だけ）
    'M23': [(C, "                        for (int k = 0; k < lso.Length; k++) if (lso[k] > 0) ownerTot[LsRecipient(r, s, k, ref fbo)] += v0 * lso[k];", "                        ownerTot[s.OwnerPopIndex] += v0;")],
    # M24 cornint の按分の走査の持ち分化を外す（欄ありでも OwnerPopIndex==pi）
    'M24': [(C, "if (lsp[k] > 0 && LsRecipient(r, s, k, ref fbp) == pi) wpi += lsp[k];", "if (k == 0 && s.OwnerPopIndex == pi) wpi += 1.0;")],
    # M25 cornint の輸出の層の持ち分化を外す
    'M25': [(C, "                        if (s.LandShares is { } lsx)   // ★(895)", "                        if (s.LandShares is { } lsx && false)   // ★(895)")],
    # M26 cearn の地代の受け手の行の持ち分化を外す
    'M26': [(A, "            if (s.LandShares is { } lsc)   // ★(895)", "            if (s.LandShares is { } lsc && false)   // ★(895)")],
    # M27 cearn の売上を割合で割らない
    'M27': [(A, "oLines[ri]++; oRev[ri] += s.LastRevenue * wr;", "oLines[ri]++; oRev[ri] += s.LastRevenue;")],
}


def main():
    root, idx = sys.argv[1], sys.argv[2]
    for f, a, c in MUT[idx]:
        p = os.path.join(root, f)
        with open(p, 'rb') as fh:
            b = fh.read()
        ab, cb = a.encode('utf-8'), c.encode('utf-8')
        n = b.count(ab)
        if n != 1:
            print('mut', idx, '一致の数', n, f, a[:70]); sys.exit(1)
        b = b.replace(ab, cb)
        with open(p, 'wb') as fh:
            fh.write(b)
    print('mut', idx, '当てた')


if __name__ == '__main__':
    main()
