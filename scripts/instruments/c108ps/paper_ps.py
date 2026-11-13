# (1118) c108ps 紙の台本（読むだけ・腕 0）：開院の空白の及ぶ範囲と門の基線を、旗 0 の生（monlaw_s・sf_s・center_s）から数える。
# 呼び方：python -I scripts/instruments/c108ps/paper_ps.py M=D:/PoB_work/keep_1116/base_ins N1=D:/PoB_work/keep_1103/n1 N2=D:/PoB_work/keep_1103/n2
# 頭の盤面の読み方（Parliament.Step は四半期の頭・行は四半期の末）：
#   頭(q) の政体＝末(q-1) の polityCode・頭(q) の内戦＝末(q-1) の center_s.inCivilWarFlag・頭(q) の席＝末(q-1) の sf_s の席（席の書き手は Parliament.cs だけ）。
#   四半期 0 の頭：席は 0・内戦なし・政体は末(0) の政体（開幕は四半期 0 の Politics.Step で政体が動かない前提＝四半期 0 の移行は数えて名乗る）。
# 類（旧≠新の行＝頭で選挙政体・内戦でない・席が空・末でも席が空〔旧の規則で開院しない〕）：O 開幕（q=0）／T 移行の距離 1（末(q-2) が非選挙）／C 内戦明け（末(q-2) が内戦）／X その他。
# 席は sf_s の国だけ（名前がその四半期に 1 国だけの行）。O・T の全ての国の数は monlaw_s から。
import sys, os, re, collections
ELECT = {3, 4}
def seeds(d):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r"monlaw_s(\d+)\.tsv", f)] if m)
def table(path, cols):
    out = {}; dup = set()
    with open(path, encoding="utf-8", newline="") as f:
        h = f.readline().rstrip("\r\n").split("\t")
        iq, inn = h.index("quarter"), h.index("nation"); ix = [h.index(c) for c in cols]
        for line in f:
            p = line.rstrip("\r\n").split("\t")
            k = (int(p[iq]), p[inn])
            if k in out: dup.add(k)
            out[k] = [p[i] for i in ix]
    for k in dup: out.pop(k, None)
    return out
for a in sys.argv[1:]:
    name, d = a.split("=", 1)
    C = collections.Counter()
    for s in seeds(d):
        ml = table(os.path.join(d, f"monlaw_s{s}.tsv"), ["polityCode", "MinimumWage", "MinimumWageTarget"])
        cw = table(os.path.join(d, f"center_s{s}.tsv"), ["inCivilWarFlag"])
        sf = table(os.path.join(d, f"sf_s{s}.tsv"), ["seatCons", "seatLib", "seatSoc"])
        pol = {k: int(v[0]) for k, v in ml.items()}; mw = {k: int(v[1]) for k, v in ml.items()}; mwt = {k: int(v[2]) for k, v in ml.items()}
        civ = {k: float(v[0]) > 0 for k, v in cw.items()}
        seat = {k: sum(float(x) for x in v) for k, v in sf.items()}
        for (q, n), pc in pol.items():
            # O：四半期 0 の選挙政体（全ての国）
            if q == 0:
                if pc in ELECT:
                    C["O_rows"] += 1; C["O_mw1"] += mw[(q, n)] >= 1; C["O_mwt1"] += mwt[(q, n)] >= 1
                    if (q, n) in seat: C["O_sf"] += 1; C["O_sf_empty"] += seat[(q, n)] < 1e-9
                else:
                    C["O_nonelect"] += 1; C["O_nonelect_mwt1"] += mwt[(q, n)] >= 1; C["O_nonelect_mw1"] += mw[(q, n)] >= 1
                continue
            hp = pol.get((q - 1, n)); hc = civ.get((q - 1, n))
            if hp is None or hc is None: continue
            # G3 の母集合：最低賃金の 0→1 以上（距離 1 の移行と四半期 0 を除く）
            t_d1 = hp in ELECT and pol.get((q - 2, n)) is not None and pol[(q - 2, n)] not in ELECT
            up0 = mw[(q - 1, n)] == 0 and mw[(q, n)] >= 1
            if up0: C["G3_all"] += 1; C["G3_excl_d1"] += not t_d1
            if t_d1 and not hc: C["T_rows_allnat"] += 1; C["T_mw_up"] += mw[(q, n)] > mw[(q - 1, n)]
            # 旧≠新の行（席の分かる国だけ）
            if hp not in ELECT or hc: continue
            hs = seat.get((q - 1, n)); es = seat.get((q, n))
            if hs is None or es is None: continue
            C["H_rows_sf"] += 1
            if hs >= 1e-9: continue
            C["H_empty_head"] += 1
            if es >= 1e-9: C["H_opened_old"] += 1; continue
            if t_d1: cls = "T"
            elif civ.get((q - 2, n)): cls = "C"
            else: cls = "X"
            C["D_" + cls] += 1
    print(f"== {name}（種 {len(seeds(d))}）")
    print(f"   O 開幕の選挙政体の行 {C['O_rows']}（MinimumWage≥1 {C['O_mw1']}・Target≥1 {C['O_mwt1']}）・sf_s にある {C['O_sf']}・うち末で席が空 {C['O_sf_empty']}・非選挙 {C['O_nonelect']}（Target≥1 {C['O_nonelect_mwt1']}・MinimumWage≥1 {C['O_nonelect_mw1']}）")
    print(f"   T 非選挙→選挙の距離 1（頭で内戦でない・全ての国）{C['T_rows_allnat']}・最低賃金の上げ {C['T_mw_up']}")
    print(f"   G3 最低賃金の 0→1 以上：全て {C['G3_all']}・距離 1 の移行を除く {C['G3_excl_d1']}（四半期 0 は前の行が無く数えない）")
    print(f"   sf_s の国：頭で選挙政体・内戦でない行 {C['H_rows_sf']}・頭で席が空 {C['H_empty_head']}・旧で開院 {C['H_opened_old']}・旧≠新 T {C['D_T']}・C 内戦明け {C['D_C']}・X その他 {C['D_X']}")
