"""(1099) c105gp 紙の台本：基線（旗 0）の生の政体・内戦・議席の列に、旗 1 の規則と突然変異 M1・M5 を当てはめた見立て（読むだけ）。
使い方: python -X utf8 -I paper1099.py <出力> 名前=フォルダ:種の始め-種の終わり ...
9 国の毎四半期の行だけ。移行＝続いた 2 行で前が非選挙・今が選挙（gate1099.py と同じ定義）。
  L＝移行の行 T から、選挙の政体のまま議席の 3 値の文字列が T の行と同じで続く行の数（旗 0 では T の議席は非選挙の写し＝最初の選挙〔片づけ〕で動く）。
  旗 1 の見立て：空の行＝T と T+1 の 2 行（Parliament.Step は政体が替わる前に走る＝T は非選挙の枝で空・T+1 は QuartersSinceElection＝0 で開院しない・T+2 で開院）。L＜2 ならその L 行。内戦の行（その期か前の期に civilWarMonths≧1）は数えない。
  M1（開院の分岐が空の議席で発火しない）の見立て：空の行＝L 行（最初の選挙まで空のまま）。
  M5（写さないだけで空にしない）の見立て：前に選挙の政体の期間がある移行（再入）では、前の選挙の政体の最後の行の議席が残る＝開院しない＝与党は政体の札（保守 0）のまま。
       その議席の第一党が 0 でない再入の数＝G1 が赤になる道の数（行は L 行ずつ）。
"""
import sys, csv, collections

ELECTED = {3, 4}


def read(path, cols):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        ix = [h.index(c) for c in cols]
        for row in r:
            yield [row[i] for i in ix]


def argmax3(a):
    b = 0
    if a[1] > a[b]: b = 1
    if a[2] > a[b]: b = 2
    return b


def main():
    out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
    w = lambda x: out.write(x + "\n")
    w("arm\ttrans\tflag1_E\tflag1_bound2T\tM1_E\tM5_reentry\tM5_reentry_nonCons\tM5_rows\tL_eq1\tL_eq2\tL_3to4\tL_5to15\tL_16plus")
    for spec in sys.argv[2:]:
        name, rest = spec.split("=", 1)
        d, rng = rest.rsplit(":", 1)
        lo, hi = map(int, rng.split("-"))
        c = collections.Counter()
        for s in range(lo, hi + 1):
            pol = {}
            for q, n, pc in read(f"{d}/scalars_s{s}.tsv", ["quarter", "nation", "polityCode"]):
                pol[(int(q), n)] = int(float(pc))
            cw = set()
            for q, n, m in read(f"{d}/nation_s{s}.tsv", ["quarter", "nation", "civilWarMonths"]):
                if float(m) > 0: cw.add((int(q), n))
            rows = collections.defaultdict(list)
            for q, n, a, b, cc in read(f"{d}/sf_s{s}.tsv", ["quarter", "nation", "seatCons", "seatLib", "seatSoc"]):
                rows[n].append((int(q), (a, b, cc), [float(a), float(b), float(cc)]))
            for n, rs in rows.items():
                if len(rs) <= 600: continue
                rs.sort()
                last_el_seats = None
                for i in range(1, len(rs)):
                    q, sstr, seats = rs[i]
                    pq = rs[i - 1][0]
                    pc = pol.get((q, n)); ppc = pol.get((pq, n))
                    if pc is None or ppc is None or pq != q - 1: continue
                    if ppc in ELECTED and pc not in ELECTED:
                        last_el_seats = rs[i - 1][2]
                    if not (ppc not in ELECTED and pc in ELECTED): continue
                    c["trans"] += 1
                    L = 0
                    for j in range(i, len(rs)):
                        qj, sj, _ = rs[j]
                        if pol.get((qj, n)) not in ELECTED or sj != sstr: break
                        L += 1
                    civ = lambda k: (rs[k][0], n) in cw or (rs[k][0] - 1, n) in cw
                    c["flag1_E"] += sum(1 for k in range(i, min(i + min(2, L), len(rs))) if not civ(k))
                    c["M1_E"] += sum(1 for k in range(i, min(i + L, len(rs))) if not civ(k))
                    if L == 1: c["L1"] += 1
                    elif L == 2: c["L2"] += 1
                    elif L <= 4: c["L34"] += 1
                    elif L <= 15: c["L515"] += 1
                    else: c["L16"] += 1
                    if last_el_seats is not None:
                        c["re"] += 1
                        if sum(last_el_seats) > 1e-9 and argmax3(last_el_seats) != 0:
                            c["reNC"] += 1; c["M5rows"] += L
        w(f"{name}\t{c['trans']}\t{c['flag1_E']}\t{2 * c['trans']}\t{c['M1_E']}\t{c['re']}\t{c['reNC']}\t{c['M5rows']}\t{c['L1']}\t{c['L2']}\t{c['L34']}\t{c['L515']}\t{c['L16']}")
    out.close()


main()
