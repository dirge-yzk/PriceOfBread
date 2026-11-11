# (1038) c98wr 門 G1〜G4 の集計（事前登録 measurements/m1038_20261111_prereg_wr.txt）。
# 呼び方：python -X utf8 -I gates.py <生のフォルダ（warentry_s*.tsv ほか）> [種の数 10]
# 読む表：warentry_s（末尾 11 列）・warinv_s（侵略 1 件ごと）・warsub_s（屈服の判定 1 件ごと）・invade_s（記録の件数）。
import sys, csv, os
root = sys.argv[1]; nseed = int(sys.argv[2]) if len(sys.argv) > 2 else 10
F1, F2, F3, TAUPOS = 205353, 1351, 2327, 593370
def rows(name, s):
    p = os.path.join(root, f"{name}_s{s}.tsv")
    if not os.path.exists(p): return None, []
    with open(p, encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        return ix, list(r)
T = {}
def add(k, v): T[k] = T.get(k, 0) + v
seeds = 0
for s in range(1, nseed + 1):
    ix, rs = rows("warentry", s)
    if ix is None: continue
    seeds += 1
    for row in rs:
        for k in ["r_will", "g1Pass", "g1Viol", "willReach", "willReachTauPos", "g2Rows", "g2Bad", "g2Zero", "mWillFalse", "mWillTrue", "nonMinorPass", "calls", "evals", "r_weak"]:
            add(k, int(row[ix[k]]))
    ix, rs = rows("warinv", s)
    inv = viol = nonMinor = nonMinorBad = submitted = unknownSubmit = 0
    for row in rs:
        inv += 1
        S, D0, tau = float(row[ix["surplusS"]]), float(row[ix["needD0"]]), float(row[ix["tau"]])
        dn = D0 * (1 - tau * tau)
        bad = S < dn - 1e-9 * max(1.0, D0)
        viol += bad
        code = int(row[ix["submitCode"]]); submitted += code == 1; unknownSubmit += code == -1
        if row[ix["targetRank"]] != "Minor":
            nonMinor += 1
            milBad = float(row[ix["invaderMilitary"]]) < 1.2 * float(row[ix["targetMilitary"]])
            nonMinorBad += bad or milBad
    add("inv", inv); add("invViol", viol); add("invNonMinor", nonMinor); add("invNonMinorBad", nonMinorBad); add("invSubmitted", submitted); add("invSubmitUnknown", unknownSubmit)
    ix, rs = rows("warsub", s)
    n = agree = disc = discOld = 0; ent = {1: 0, 2: 0}; entWar = {1: 0, 2: 0}
    for row in rs:
        n += 1; e = int(row[ix["entrance"]]); ent[e] += 1
        eng, uni, old = int(row[ix["engineCode"]]), int(row[ix["unifiedCode"]]), int(row[ix["oldCode"]])
        entWar[e] += eng == 0
        agree += eng == uni
        if uni != old:
            disc += 1; discOld += eng == old
    add("subRows", n); add("subAgree", agree); add("subDisc", disc); add("subDiscEngOld", discOld)
    add("subEnt1", ent[1]); add("subEnt2", ent[2]); add("subEnt1War", entWar[1]); add("subEnt2War", entWar[2])
    ix, rs = rows("invade", s); add("invadeS", len(rs))
print(f"seeds={seeds}")
print(" ".join(f"{k}={v}" for k, v in T.items()))
def g(name, ok, txt): print(f"{name} {'緑' if ok else '赤'}  {txt}")
g("G1a", T.get("g1Viol", 1) == 0 and T.get("invViol", 1) == 0, f"g1Viol={T.get('g1Viol')} invViol={T.get('invViol')}（＝0）")
g("G1b", T.get("inv", 0) >= F2 // 10, f"warinv の行={T.get('inv')}（≧{F2 // 10}）")
g("G1c", T.get("r_will", 0) >= F1 // 10, f"r_will={T.get('r_will')}（≧{F1 // 10}）")
g("G2a", T.get("g2Rows", 0) >= TAUPOS // 100, f"g2Rows={T.get('g2Rows')}（≧{TAUPOS // 100}）")
g("G2b", T.get("g2Bad", 1) == 0, f"g2Bad={T.get('g2Bad')}（＝0）")
g("G2c", T.get("g2Zero", 1) == 0, f"g2Zero={T.get('g2Zero')}（＝0）")
g("G2d", T.get("mWillFalse", 1) + T.get("mWillTrue", 1) == 0, f"mWillFalse={T.get('mWillFalse')} mWillTrue={T.get('mWillTrue')}（＝0）")
g("G3a", T.get("invNonMinor", 0) >= 20, f"非 Minor の標的への侵略={T.get('invNonMinor')}（≧20）")
g("G3b", T.get("invNonMinorBad", 1) == 0, f"その行で S<D′ か兵力の門の外={T.get('invNonMinorBad')}（＝0）")
g("G4a", T.get("subRows", 0) >= F3, f"warsub の行={T.get('subRows')}（≧{F3}）")
g("G4b", T.get("subRows", 0) > 0 and T.get("subAgree", 0) == T.get("subRows", -1), f"エンジン＝1 本の式 {T.get('subAgree')}/{T.get('subRows')}（100%）")
g("G4c", T.get("subDisc", 0) >= 10 and T.get("subDiscEngOld", 1) == 0, f"判別の行={T.get('subDisc')}（≧10）・そのうちエンジン＝旧={T.get('subDiscEngOld')}（＝0）")
