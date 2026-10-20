# (478) 線 dz：事前登録の [8] G1-3（働き手・老年・子の割合の B÷A のセルの数）・G1-5（最終四半期の人口の B÷A）と、追記 2 の記録の読み
#   （児童労働 10〜14 歳の割合・10 歳まで生き残る確率・死の種類の割合）を出す。比べる側（A）には、揺れの目安としてヌル腕（A と 1e-8 だけ違う腕）も同じ読みで並べられる。
# 使い方: PYTHONUTF8=1 python -I gates_dz478.py <B の計器の生のフォルダ> <A（m475_ins）の計器の生のフォルダ> [出力のファイル] [ヌル腕のフォルダ ...]
#   ヌル腕を渡すと、同じ G1-3・G1-5 を〈ヌル腕÷A〉でも出す（カオス性〔軌道の散らばり〕の目安＝採否に使わない・登録 [5] ヌル腕の項）。
# 層の数が 11 でない B を渡されたら止める（age_s の bands 列が 11 でなければ終了コード 2）。
import csv, sys, glob, os, statistics as st, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import age0_read as R
R.COLS = R.COLS + ["childLaborRateShare", "s10Share"]   # 記録の列（採否に使わない）
N6 = ["イギリス", "フランス", "プロイセン", "ロシア", "日本", "清"]
N8 = N6 + ["アメリカ", "オーストリア"]
ERAS = R.ERAS
B, A = sys.argv[1], sys.argv[2]
out = open(sys.argv[3], "w", encoding="utf-8", newline="\n") if len(sys.argv) > 3 and sys.argv[3] != "-" else sys.stdout
NULLS = sys.argv[4:]
def w(s=""): out.write(s + "\n")

# ---- 層の数の確かめ（B の age_s が 11 層であること）
bands = set(); nfiles = 0
for fp in sorted(glob.glob(f"{B}/age_s*.tsv")):
    nfiles += 1
    with open(fp, encoding="utf-8") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["scope"] == "N": bands.add(int(r["bands"]))
if bands != {11} or nfiles == 0:
    w(f"!! B の age_s の層の数が 11 でない（{sorted(bands)}・ファイル {nfiles}）＝読まない"); sys.exit(2)
w(f"B の age_s: 層の数 {sorted(bands)}・ファイル {nfiles}")

Ar = R.read(A)
NEED = {"adultShare": 22, "share60plus": 18, "share0to14": 20}

def g13(X, label):
    Xr = R.read(X)
    w(f"\nG1-3（{label}÷A が 0.9〜1.1 の内のセルの数・セル＝6 国×4 期間）")
    for c in ["adultShare", "share60plus", "share0to14", "fertileWomenShare"]:
        ok = 0; tot = 0; vals = []
        for n in N6:
            for i in range(4):
                r = [Xr[s][(n, i)][c] / Ar[s][(n, i)][c] for s in Ar if s in Xr and (n, i) in Ar[s] and (n, i) in Xr[s] and c in Ar[s][(n, i)] and c in Xr[s][(n, i)] and Ar[s][(n, i)][c] != 0]
                if not r: continue
                m = st.median(r); tot += 1; vals.append(m)
                if 0.9 <= m <= 1.1: ok += 1
        thr = NEED.get(c)
        if not vals: w(f"  {c}: セル無し"); continue
        verdict = f"（登録の線 {thr}/24 以上 → {'通る' if ok >= thr else '外れ'}）" if (thr and label == "B") else "（記録）"
        w(f"  {c}: {ok}/{tot}{verdict}　{label}÷A の範囲 {min(vals):.3f}〜{max(vals):.3f}")
    return Xr

def last_pop(D):
    res = {}
    for s in range(1, 11):
        fp = f"{D}/nation_s{s}.tsv"
        if not os.path.exists(fp): continue
        idx_name = {}; last = {}
        with open(fp, encoding="utf-8") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                q = int(r["quarter"]); ni = r["nationIndex"]
                if q == 0 or ni not in idx_name: idx_name[ni] = r["nation"]
                last[ni] = (q, float(r["populationK"]))
        res[s] = {idx_name[ni]: v[1] for ni, v in last.items() if idx_name[ni] in N8}
        res[s]["_q"] = max(v[0] for v in last.values())
        res[s]["_qn"] = {idx_name[ni]: v[0] for ni, v in last.items() if idx_name[ni] in N8}   # 国ごとの最後の行の四半期（併合・消滅の旗）
    return res
LA = last_pop(A)

def g15(X, label):
    LX = last_pop(X)
    w(f"\nG1-5（最終四半期の人口の {label}÷A・種の中央値・0.5〜2 の内が 8 国のうち 6 国以上）")
    ok = 0
    for n in N8:
        r = [LX[s][n] / LA[s][n] for s in LA if s in LX and n in LA[s] and n in LX[s] and LA[s][n] > 0]
        if not r: w(f"  {n}: データ無し"); continue
        m = st.median(r); inside = 0.5 <= m <= 2
        ok += inside
        early = sorted(s for s in LA if s in LX and (LA[s]["_qn"].get(n, 0) != LA[s]["_q"] or LX[s]["_qn"].get(n, 0) != LX[s]["_q"]))
        w(f"  {n}\t{m:.3f}\t{'内' if inside else '外'}\t（種 {len(r)}・範囲 {min(r):.3f}〜{max(r):.3f}）" + (f"　★最後の行が最終四半期でない種 {early}（併合・消滅の比は発散と読まない）" if early else ""))
    w(f"  内の国: {ok}/8" + (f" → {'通る' if ok >= 6 else '外れ'}" if label == "B" else "（記録）") + f"　最終四半期 A={sorted({v['_q'] for v in LA.values()})} {label}={sorted({v['_q'] for v in LX.values()})}")

Br = g13(B, "B"); g15(B, "B")
NR = []
for nd in NULLS:
    NR.append(g13(nd, "ヌル腕 " + os.path.basename(nd.rstrip('/')))); g15(nd, "ヌル腕 " + os.path.basename(nd.rstrip('/')))

# ---- 記録（採否に使わない・追記 2）：死の種類（平常・飢え・その他）の割合と、児童労働の割合・10 歳まで生き残る確率の A と B の値
def mix(D):
    res = {}
    for s in range(1, 11):
        fp = f"{D}/pop_s{s}.tsv"
        if not os.path.exists(fp): continue
        acc = collections.defaultdict(lambda: collections.defaultdict(float))
        with open(fp, encoding="utf-8") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                if r["scope"] != "N" or r["nation"] not in N6: continue
                y = int(r["year"])
                for i, (a, b) in enumerate(ERAS):
                    if a <= y <= b:
                        for c in ("deathsNormalK", "deathsFamineK", "deathsOtherK"):
                            try: acc[(r["nation"], i)][c] += float(r[c])
                            except ValueError: pass
        res[s] = acc
    return res
MA, MB = mix(A), mix(B)
w("\n記録：死の種類の割合（平常・飢え・その他〔疫病・遠征の病死〕・age_s は平常と飢えだけ数える）と、児童労働・s10 の値（A・B）")
w("国\t期間\tA:平常\tA:飢え\tA:その他\tB:平常\tB:飢え\tB:その他\tA:児童労働\tB:児童労働\t" + "".join(f"ヌル{j + 1}:児童労働\t" for j in range(len(NR))) + "A:s10\tB:s10")
for n in N6:
    for i, (a, b) in enumerate(ERAS):
        def share(M):
            rs = []
            for s in M:
                k = M[s].get((n, i))
                if not k: continue
                t = k["deathsNormalK"] + k["deathsFamineK"] + k["deathsOtherK"]
                if t > 0: rs.append((k["deathsNormalK"] / t, k["deathsFamineK"] / t, k["deathsOtherK"] / t))
            return [st.median(x[j] for x in rs) for j in range(3)] if rs else [float("nan")] * 3
        vals = []
        for c in ("childLaborRateShare", "s10Share"):
            for X in [Ar, Br] + (NR if c == "childLaborRateShare" else []):
                v = [X[s][(n, i)][c] for s in X if (n, i) in X[s] and c in X[s][(n, i)]]
                vals.append(st.median(v) if v else float("nan"))
        w(f"{n}\t{a}-{b}\t" + "\t".join(f"{x:.3f}" for x in share(MA) + share(MB)) + "\t" + "\t".join(f"{x:.4f}" for x in vals))
