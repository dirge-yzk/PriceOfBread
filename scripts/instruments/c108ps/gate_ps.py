# (1118) c108ps 門の台本（読むだけ）：事前登録 measurements/m1118_20261114_prereg_ps.txt の [5] の門と [11] の記録を、計器の生（parlseat_s・monlaw_s・prds_s）から数える。
# 呼び方：python -I -X utf8 scripts/instruments/c108ps/gate_ps.py <腕の ins> <旗 0 の ins（G4 の分母・parlseat_s のある生）> [--seeds 1-10]
# 母集合 P＝parlHeadCode≥0 で 8（選挙政体）が立ち 16（早い戻り）が立たない行。距離 1＝末(q−1) が選挙・末(q−2) が非選挙（同じ nationUid）。
import sys, os, re, collections
ELECT = {3, 4}
args = [a for a in sys.argv[1:] if not a.startswith("--")]
rng = next((sys.argv[i + 1] for i, a in enumerate(sys.argv) if a == "--seeds"), None)
ARM, BASE = args[0], (args[1] if len(args) > 1 else None)
def seeds(d):
    s = sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r"parlseat_s(\d+)\.tsv", f)] if m)
    if rng: a, b = map(int, rng.split("-")); s = [x for x in s if a <= x <= b]
    return s
def rows(path):
    with open(path, encoding="utf-8", newline="") as f:
        h = f.readline().rstrip("\r\n").split("\t")
        for line in f:
            p = line.rstrip("\r\n").split("\t")
            if len(p) < len(h): continue
            yield dict(zip(h, p))
def parl(d, s):
    R = {}
    for r in rows(os.path.join(d, f"parlseat_s{s}.tsv")):
        R[(int(r["quarter"]), r["nationUid"])] = (int(r["polityCode"]), int(r["parlHeadCode"]), int(r["parlUpdPops"]), float(r["seatSumEnd"]), float(r["seatConsEnd"]))
    return R
def count_parl(d):
    C = collections.Counter(); A = set(); B = set(); q0P = set()
    for s in seeds(d):
        R = parl(d, s)
        for (q, u), (pol, code, upd, ss, sc) in R.items():
            if code < 0: C["code_missing"] += 1
            inP = code >= 0 and (code & 8) and not (code & 16)
            if upd > 0: A.add((s, q, u))
            if not inP: continue
            C["P"] += 1
            empty = ss < 1e-9
            C["P_empty"] += empty
            if (code & 1) and not (code & 4): B.add((s, q, u))
            if q == 0:
                q0P.add((s, q, u)); C["q0"] += 1; C["q0_seated"] += not empty
                if not empty and sc / ss >= 0.99: C["G4b"] += 1
            p1 = R.get((q - 1, u)); p2 = R.get((q - 2, u))
            d1 = p1 is not None and p2 is not None and p1[0] in ELECT and p2[0] not in ELECT
            if d1: C["d1"] += 1; C["d1_empty"] += empty
            if (code & 1) and not (code & 2):   # 頭で席が空・旧の規則なら開院しない＝旧≠新の行
                cw1 = p1 is not None and p1[1] >= 0 and (p1[1] & 16) != 0   # 前の四半期の頭が早い戻り（内戦など）
                cls = "O" if q == 0 else "T" if d1 else "C" if cw1 else "X"
                C["diff_" + cls] += 1
    return C, A, B, q0P
def count_law(d):
    C = collections.Counter(); opp = collections.Counter(); up = collections.Counter()
    for s in seeds(d):
        by = collections.defaultdict(list); keyc = collections.Counter()
        for r in rows(os.path.join(d, f"monlaw_s{s}.tsv")):
            by[r["nation"]].append((int(r["quarter"]), int(r["polityCode"]), int(r["MinimumWage"]), int(r["LaborUnion"])))
            keyc[(r["quarter"], r["nation"])] += 1
            if r["quarter"] == "0" and r["nation"] == "アメリカ": C["us_q0_mw1"] += int(r["MinimumWage"]) >= 1
            C["mw_enacted_rows"] += int(r["MinimumWage"]) >= 1
        for nat, seq in by.items():   # G2：r1108/mw_ctrl.py と同じ数え方（名前で結ぶ）
            seq.sort(); last = None; kind = None; prev = None
            for (q, pol, mw, lu) in seq:
                if prev is not None and pol != prev[1] and pol in ELECT:
                    last = q; kind = "NE" if prev[1] not in ELECT else "EE"
                if pol not in ELECT: last = None
                if prev is not None and pol in ELECT and prev[1] in ELECT:
                    dq = q - last if last is not None else None
                    cell = kind if dq == 1 else "other" if dq is None or dq > 4 or dq < 1 else "d24"
                    opp[cell] += 1; up[(cell, "mw")] += mw > prev[2]; up[(cell, "lu")] += lu > prev[3]
                prev = (q, pol, mw, lu)
        for nat, seq in by.items():   # G3：0→1 以上（名前が四半期に 1 国だけの行・距離 1 の移行を除く）
            m = {q: (pol, mw) for (q, pol, mw, lu) in seq if keyc[(str(q), nat)] == 1}
            for q, (pol, mw) in m.items():
                pr = m.get(q - 1); p2 = m.get(q - 2)
                if pr is None: continue
                if pr[1] == 0 and mw >= 1:
                    d1 = pr[0] in ELECT and p2 is not None and p2[0] not in ELECT
                    C["G3_all"] += 1; C["G3"] += not d1
    return C, opp, up
def prds_q0(d):
    n = 0
    for s in seeds(d):
        p = os.path.join(d, f"prds_s{s}.tsv")
        if not os.path.exists(p): return None
        n += sum(1 for r in rows(p) if r["quarter"] == "0")
    return n
C, A, B, q0P = count_parl(ARM)
L, opp, up = count_law(ARM)
print(f"腕 {ARM}・種 {seeds(ARM)}")
pct = lambda a, b: f"{100.0 * a / b:.2f}%" if b else "—"
print(f"G1 距離 1 の行で席が空 {C['d1_empty']}/{C['d1']} = {pct(C['d1_empty'], C['d1'])}（門 ≦2%）：{'緑' if C['d1'] and C['d1_empty'] <= 0.02 * C['d1'] else '赤'}")
print(f"G1′ q＝0 の行で席が有る {C['q0_seated']}/{C['q0']} = {pct(C['q0_seated'], C['q0'])}（門 ≧98%）：{'緑' if C['q0'] and C['q0_seated'] >= 0.98 * C['q0'] else '赤'}")
bm = up[("other", "mw")] / opp["other"] if opp["other"] else 0
r2 = (up[("NE", "mw")] / opp["NE"]) / bm if bm and opp["NE"] else float("nan")
lim = 10 * bm * opp["NE"]
print(f"G2 非選挙→選挙の距離 1：四半期 {opp['NE']}・最低賃金の上げ {up[('NE','mw')]}・他の率 {bm:.6f}（{up[('other','mw')]}/{opp['other']}）・比 {r2:.1f}（門 ≦10＝上げ ≦{lim:.1f} 回）：{'緑' if bm and up[('NE','mw')] <= lim else ('判定不能' if not bm else '赤')}")
blu = up[("other", "lu")] / opp["other"] if opp["other"] else 0
print(f"   記録：選挙→選挙の距離 1 四半期 {opp['EE']}・最低賃金の上げ {up[('EE','mw')]}・労組の上げ {up[('EE','lu')]}／非選挙→選挙の距離 1 の労組の上げ {up[('NE','lu')]}（比 {((up[('NE','lu')] / opp['NE']) / blu) if blu and opp['NE'] else float('nan'):.1f}）・距離 2〜4 四半期 {opp['d24']} 最低賃金の上げ {up[('d24','mw')]}")
print(f"G3 最低賃金の 0→1 以上（距離 1 の移行を除く）{L['G3']}（全て {L['G3_all']}）（門 ≧176）：{'緑' if L['G3'] >= 176 else '赤'}")
if BASE:
    Cb, Ab, Bb, q0b = count_parl(BASE)
    print(f"G4 P で席が空の行 {C['P_empty']}（旗 0 の生 {Cb['P_empty']}・門 ≦{0.02 * Cb['P_empty']:.1f}）：{'緑' if C['P_empty'] <= 0.02 * Cb['P_empty'] else '赤'}")
else:
    print(f"G4 P で席が空の行 {C['P_empty']}（旗 0 の生が無い＝門は読まない）")
print(f"G4b q＝0 で席が有り保守 ≧0.99 の行 {C['G4b']}（門 0）：{'緑' if C['G4b'] == 0 else '赤'}")
uok = A == B and all(k[1] == 0 for k in A) and len(A) == len(q0P)
print(f"U 思想の 1 回が走った行 {len(A)}・頭で席が空で思想が無い P の行 {len(B)}・一致 {A == B}・全て q＝0 {all(k[1] == 0 for k in A)}・P の q＝0 の行 {len(q0P)}：{'緑' if uok else '赤'}")
if A - B: print(f"   走ったが B に無い（先頭 5）：{sorted(A - B)[:5]}")
if B - A: print(f"   B だが走らない（先頭 5）：{sorted(B - A)[:5]}")
print(f"記録：P の行 {C['P']}・旧≠新（頭で席が空・QuartersSinceElection＝0）O {C['diff_O']}・T {C['diff_T']}・C 内戦明け {C['diff_C']}・X その他 {C['diff_X']}・符号が無い行 {C['code_missing']}")
print(f"記録：アメリカの q＝0 の最低賃金 ≧1 の種 {L['us_q0_mw1']}・最低賃金の施行の行 {L['mw_enacted_rows']}・prds_s の q＝0 の行 {prds_q0(ARM)}")
