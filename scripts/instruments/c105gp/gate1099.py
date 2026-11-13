"""(1099) c105gp 門 G1〜G4 と記録（読むだけ・エンジンの関数を呼ばない）。
入力＝計器つきの腕の生（sf_s・scalars_s・nation_s）。腕は引数で渡す：名前=フォルダ:種の始め-種の終わり
使い方: python -X utf8 -I gate1099.py <出力> m1096=D:/PoB_work/keep_1096/base_ins:1-10 n1=D:/PoB_work/keep_1087/n1:1-10 ...

定義（事前登録 measurements/m1099_20261113_prereg_gp.txt と同じ文）
  選挙の政体＝polityCode が 3（立憲君主制）か 4（共和制）。与党の規則の札 RulingParty＝5→1・6→2・他→0（Parliament.cs の RulingParty を表で写した）。
  議席が空＝seatCons+seatLib+seatSoc ≦ 1e-9。議席の第一党＝3 値の最大（同じ値は小さい番号＝Winner と同じ厳密な > の比べ方）。
  内戦の四半期＝nation_s の civilWarMonths がその四半期かその前の四半期に 1 以上（Parliament.Step は内戦中は走らない・月の数えと Step の時点のずれを 1 期で覆う）。
  開幕＝国の系列の最初の 2 行（開幕の国も途中で生まれた国も同じ）。
  9 国＝sf_s に 600 行より多くある国（毎四半期の行がある計器の国）。G2・G3・G4 は 9 国の毎四半期の行だけ。G1 は sf_s の全ての行（9 国の毎四半期＋他の国の年末）。
  移行＝9 国の続いた 2 行で、前の行が非選挙・今の行が選挙。
  G1 働いた：選挙の政体の行で、議席が空でなく、与党が議席の第一党と違う行の数（開幕と内戦も含めて全て数える）。
  G2：9 国で、選挙の政体で議席が空の行（開幕と内戦の行を除く）の数 E と、移行の数 T。門は E ≦ 2T。
  G3：9 国で、続いた 2 行が共に選挙の政体で議席が空でなく、議席の 3 値の文字列が同じで、与党が違う行の数。
  G4：9 国で、非選挙の行（内戦の行・開幕の行・前の行と政体が違う行を除く）で、与党が RulingParty と違う行の数。
"""
import sys, csv, collections

ELECTED = {3, 4}
RULING = {5: 1, 6: 2}


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


def one_seed(d, s, c, ep):
    pol = {}
    for q, n, pc in read(f"{d}/scalars_s{s}.tsv", ["quarter", "nation", "polityCode"]):
        pol[(int(q), n)] = int(float(pc))
    cw = set()
    for q, n, m in read(f"{d}/nation_s{s}.tsv", ["quarter", "nation", "civilWarMonths"]):
        if float(m) > 0: cw.add((int(q), n))
    rows = collections.defaultdict(list)
    for q, n, a, b, cc, g in read(f"{d}/sf_s{s}.tsv", ["quarter", "nation", "seatCons", "seatLib", "seatSoc", "governingParty"]):
        rows[n].append((int(q), (a, b, cc), [float(a), float(b), float(cc)], int(float(g))))
    for n, rs in rows.items():
        rs.sort()
        q9 = len(rs) > 600
        prev = None
        for i, (q, sstr, seats, gp) in enumerate(rs):
            pc = pol.get((q, n))
            if pc is None:
                c["nopolity"] += 1; prev = None; continue
            el = pc in ELECTED
            empty = sum(seats) <= 1e-9
            civ = (q, n) in cw or (q - 1, n) in cw
            opening = i < 2
            # G1（全ての行）
            if el:
                c["g1_rows"] += 1
                if not empty and gp != argmax3(seats):
                    c["g1"] += 1
                    if q9: c["g1_q9"] += 1
            if not q9:
                prev = None; continue
            c["q9_rows"] += 1
            pp = prev[1] if prev is not None and prev[0] == q - 1 else None
            if pp is not None and pp not in ELECTED and el:
                c["trans"] += 1
                ep.append(("T", s, n, q))
            if pp is not None and pp in ELECTED and not el:
                c["trans_out"] += 1
            if el and empty:
                c["empty_el_all"] += 1
                if opening: c["empty_el_open"] += 1
                elif civ: c["empty_el_civ"] += 1
                else:
                    c["g2_E"] += 1
                    ep.append(("E", s, n, q))
            # G3
            if el and prev is not None and prev[0] == q - 1 and prev[1] in ELECTED and not empty and not prev[4]:
                c["g3_pairs"] += 1
                if sstr == prev[2] and gp != prev[3]:
                    c["g3"] += 1
                    ep.append(("G3", s, n, q))
            # G4
            if not el:
                c["ne_rows"] += 1
                if gp != RULING.get(pc, 0):
                    if civ: c["g4_civ"] += 1
                    elif opening: c["g4_open"] += 1
                    elif pp is None or pp != pc: c["g4_lag"] += 1
                    else:
                        c["g4"] += 1
                        ep.append(("G4", s, n, q, pc, gp))
                    if not empty: pass
                if not empty: c["ne_nonempty"] += 1
            prev = (q, pc, sstr, gp, empty)


def main():
    out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
    w = lambda x: out.write(x + "\n")
    keys = ["g1", "g1_q9", "g1_rows", "g2_E", "trans", "empty_el_all", "empty_el_open", "empty_el_civ", "g3", "g3_pairs",
            "g4", "g4_lag", "g4_civ", "g4_open", "ne_rows", "ne_nonempty", "trans_out", "q9_rows", "nopolity"]
    w("arm\t" + "\t".join(keys) + "\tG1\tG2\tG3")
    detail = []
    for spec in sys.argv[2:]:
        name, rest = spec.split("=", 1)
        d, rng = rest.rsplit(":", 1)
        lo, hi = map(int, rng.split("-"))
        c = collections.Counter(); ep = []
        for s in range(lo, hi + 1):
            one_seed(d, s, c, ep)
        g1 = "緑" if c["g1"] == 0 else "赤"
        g2 = "緑" if c["g2_E"] <= 2 * c["trans"] else "赤"
        g3 = "緑" if c["g3"] == 0 else "赤"
        w(name + "\t" + "\t".join(str(c[k]) for k in keys) + f"\t{g1}\t{g2}\t{g3}")
        for e in ep:
            if e[0] in ("G3", "G4"): detail.append((name,) + e)
    w("")
    w("# G3・G4 の行（腕 種別 種 国 四半期 [政体 与党]）")
    for e in detail[:200]:
        w("\t".join(map(str, e)))
    out.close()


main()
