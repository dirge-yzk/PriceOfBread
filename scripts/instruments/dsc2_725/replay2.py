# (725) 線 dsc2 段0′：改良度 L の再生（盤面の応答なし）・§10-3 の形・§10-4 の go／no-go（N1〜N4）
# 規則は設計書 docs/202610/20261030/20261030_発見の連続化_設計.md §10-10-2（canary の前にコミット dff6e5ab で固定・結果の後に変えない）。
# 入力＝旗 0 の canary の計器 dsc2_s<種>.tsv（PriceOfBread.SimCore/InstrumentsDsc.cs・INS=1 と POB_INSTRUMENTS_DSC=1）。
# 使い方：python replay2.py <ins のフォルダ> <種,種,...>
# 読むだけ（書き出しは標準出力）。
import sys, math, statistics, argparse
from collections import defaultdict

NINE = ["清", "ロシア", "オスマン", "フランス", "イギリス", "プロイセン", "オーストリア", "日本", "アメリカ"]
RHO = 2.1
B_EXP = 1.0 / (RHO + 1.0)   # b＝1÷(ρ＋1)
A_OPEN = 400.0              # ★(719) ④・§10-10-2（固定）
A_NEW = 4.0
A_STEAM = 28.0              # 蒸気機関の行（ワット 1776・knownDiscoveries）
STEAM_ROW = "蒸気機関"
LEV = 2.4                   # IndTechLeverage（I の式）
STEAM_BOOST = 0.3; ELEC_BOOST = 0.3
COTTON = "綿布/既定"


def load(path):
    N = {}                      # (q, uid) -> dict
    R = defaultdict(dict)       # q -> {(uid, col): tuple}
    C = defaultdict(lambda: defaultdict(dict))  # q -> buyer -> {seller: value}
    years = {}
    colname = {}
    with open(path, 'rb') as f:
        hdr = f.readline().decode('utf-8').rstrip('\r\n').split('\t')
        ix = {h: i for i, h in enumerate(hdr)}
        for line in f:
            a = line.decode('utf-8').rstrip('\r\n').split('\t')
            k = a[0]; q = int(a[1]); y = int(a[2]); uid = int(a[3])
            years[q] = y
            if k == 'N':
                N[(q, uid)] = dict(name=a[4], tl=float(a[ix['techLevel']]), br=float(a[ix['bracket']]), rep3=float(a[ix['replacement3']]),
                                   homePop=float(a[ix['homePopK']]), pop=float(a[ix['popK']]), gdp=float(a[ix['nominalGdpPerMonth']]), fx=float(a[ix['fxRate']]))
            elif k == 'R':
                col = int(a[5]); colname[col] = a[6]
                R[q][(uid, col)] = (float(a[ix['qPerMonth']]), float(a[ix['valuePerMonth']]), float(a[ix['dgPerMonth']]),
                                     float(a[ix['engHomePerMonth']]), float(a[ix['homePopK']]), a[ix['isRgo']] == '1',
                                     float(a[ix['valueRefPerMonth']]), float(a[ix['steamFillMeanShare']]), float(a[ix['elecFillMeanShare']]))
            elif k == 'C':
                C[q][int(a[5])][int(a[6])] = float(a[ix['qPerMonth']])
    return N, R, C, years, colname


def nine_uids(N):
    out = {}
    for (q, uid), v in sorted(N.items()):
        if v['name'] in NINE and v['name'] not in out:
            out[v['name']] = uid
    return out


def a_of(col, colname):
    nm = colname.get(col, "")
    if nm.endswith("/既定"):
        return A_OPEN
    if nm == STEAM_ROW:
        return A_STEAM
    return A_NEW


def d_values(R, years, uids, y0, y1):
    ds = []
    for q, rows in R.items():
        if not (y0 <= years[q] <= y1):
            continue
        for (uid, col), r in rows.items():
            eng, hp = r[3], r[4]
            if uid in uids and eng > 0 and hp > 0:
                ds.append(eng / hp)
    return ds


def replay(N, R, C, years, colname, D0):
    L = {}
    traj = {}                   # (q, uid, col) -> (L, E)
    contrib = defaultdict(lambda: [0.0, 0.0, 0.0])   # (decade) -> [T, E, X] の和（9 国以外も含む全ての国×列）
    sigc_max = (0.0, None)
    q0_disc = 0
    for q in sorted(years):
        rows = R.get(q, {})
        flows = C.get(q, {})
        if q == 0:
            q0_disc = sum(1 for (u, c) in rows if not colname.get(c, "").endswith("/既定") and colname.get(c, "") != STEAM_ROW)
        for key in rows:
            if key not in L:
                L[key] = 1.0
        # 世界の和（E の分母）
        Qw = defaultdict(float)
        for (uid, col), r in rows.items():
            Qw[col] += r[0]
        newL = {}
        for (uid, col), l in L.items():
            r = rows.get((uid, col))
            nv = N.get((q, uid))
            rep3 = nv['rep3'] if nv else 0.0
            T = E = X = 0.0
            if r:
                qq, v, dg, eng, hp = r[0], r[1], r[2], r[3], r[4]
                D = eng / hp if hp > 0 else 0.0
                T = D / (D + D0) if D > 0 else 0.0
                Pn = nv['pop'] if nv else 0.0
                if Pn > 0 and Qw[col] > 0 and PW[q] > 0 and qq > 0:
                    x = (qq / Pn) / (Qw[col] / PW[q])
                    E = x / (1.0 + x)
                if (col + q) % 4 == 0 and nv and nv['gdp'] > 0 and nv['fx'] > 0:
                    f = flows.get(uid)
                    if f:
                        den = nv['gdp'] * nv['fx'] * 3.0
                        sc = 0.0
                        for s, val in f.items():
                            c = val / den
                            sc += c
                            lp = L.get((s, col))
                            if lp and lp > 0:
                                X += c * max(0.0, 1.0 - l / lp)
                        if sc > sigc_max[0]:
                            sigc_max = (sc, (q, uid))
                    X *= 4.0
            A = a_of(col, colname)
            dl = B_EXP * (T + E + X) / (A * l ** RHO) - rep3 * (1.0 - E) * (l - 1.0)
            newL[(uid, col)] = l + dl
            traj[(q, uid, col)] = (l + dl, E)
            dec = (years[q] // 10) * 10
            cc = contrib[dec]; cc[0] += T; cc[1] += E; cc[2] += X
        L = newL
    return traj, contrib, sigc_max, q0_disc


def by_year(N, R, years, traj, uids):
    out = {}
    for q in sorted(years):
        if q % 4 != 3:
            continue
        y = years[q]
        rows = R.get(q, {})
        for name, uid in uids.items():
            num = den = vref = 0.0
            for (u, col), r in rows.items():
                if u != uid:
                    continue
                t = traj.get((q, u, col))
                if t and r[1] > 0:
                    num += r[1] * t[0]; den += r[1]
                vref += r[6]
            nv = N.get((q, uid))
            if nv and den > 0:
                out[(name, y)] = (num / den, nv['br'], vref)
    return out


def spearman(xs, ys):
    def rank(v):
        o = sorted(range(len(v)), key=lambda i: v[i]); rk = [0.0] * len(v); i = 0
        while i < len(o):
            j = i
            while j + 1 < len(o) and v[o[j + 1]] == v[o[i]]:
                j += 1
            for t in range(i, j + 1):
                rk[o[t]] = (i + j) / 2.0
            i = j + 1
        return rk
    if len(xs) < 3:
        return float('nan')
    a = rank(xs); b = rank(ys); ma = sum(a) / len(a); mb = sum(b) / len(b)
    num = sum((x - ma) * (y - mb) for x, y in zip(a, b))
    den = math.sqrt(sum((x - ma) ** 2 for x in a) * sum((y - mb) ** 2 for y in b))
    return num / den if den > 0 else float('nan')


PW = {}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('ins'); ap.add_argument('seeds')
    a = ap.parse_args()
    seeds = [int(s) for s in a.seeds.split(',')]
    data = {s: load(f"{a.ins}/dsc2_s{s}.tsv") for s in seeds}
    ds = []
    for s, (N, R, C, years, cn) in data.items():
        ds += d_values(R, years, set(nine_uids(N).values()), 1830, 1849)
    D0 = statistics.median(ds)
    print(f"[D0] 9 国×列×四半期（1830〜1849・D>0）の D の中央値＝{D0:.6g}（件数 {len(ds)}・種 {seeds}）")
    print(f"[係数] A_open＝{A_OPEN}・A_new＝{A_NEW}・蒸気機関＝{A_STEAM}・ρ＝{RHO}・b＝{B_EXP:.4f}（§10-10-2 で固定）")
    W = defaultdict(dict); I = defaultdict(dict); LB = {}
    n3 = defaultdict(dict); n4 = {}
    for s, (N, R, C, years, cn) in data.items():
        PW.clear()
        for (q, u), v in N.items():
            PW[q] = PW.get(q, 0.0) + v['pop']
        uids = nine_uids(N)
        traj, contrib, sigc, q0d = replay(N, R, C, years, cn, D0)
        lb = by_year(N, R, years, traj, uids); LB[s] = lb
        print(f"\n== 種 {s}：国ごとの L̄／括弧 B（年末）")
        YS = (1790, 1800, 1810, 1830, 1850, 1870, 1890, 1913, 1930, 1950)
        print("国\t" + "\t".join(str(y) for y in YS))
        for name in NINE:
            print(name + "\t" + "\t".join((f"{lb[(name, y)][0]:.3f}/{lb[(name, y)][1]:.3f}" if (name, y) in lb else "-") for y in YS))
        print(f"[記録] 開幕 q0 に産出が正の蒸気機関以外の発見の行（国×行）の件数＝{q0d}")
        print(f"[記録] 国×四半期の Σc の最大＝{sigc[0]:.4g}（q, uid＝{sigc[1]}）")
        print("[記録] 項の寄与の和（全ての国×列・十年ごと T/E/X と X の割合）：" + "・".join(
            f"{d} {c[0]:.0f}/{c[1]:.0f}/{c[2]:.1f}({c[2] / max(1e-12, sum(c)):.3f})" for d, c in sorted(contrib.items())))
        # 英の綿布の総倍率（記録）
        uk = uids.get("イギリス"); ccol = next((c for c, nm in cn.items() if nm == COTTON), None)
        q25 = next((q for q in years if years[q] == 1825 and q % 4 == 3), None)
        if uk is not None and ccol is not None and q25 is not None:
            t = traj.get((q25, uk, ccol)); r = R.get(q25, {}).get((uk, ccol))
            if t and r:
                print(f"[記録] 英の綿布/既定 1825 年末：L＝{t[0]:.4f}・SteamFill＝{r[7]:.3f}・ElecFill＝{r[8]:.3f}・総倍率 L×(1+0.3·S)×(1+0.3·E)＝{t[0] * (1 + STEAM_BOOST * r[7]) * (1 + ELEC_BOOST * r[8]):.3f}（史実 2.98〈推〉と並べる・記録）")
        for y in sorted(set(y for (_, y) in lb)):
            num = den = numi = 0.0
            for name in NINE:
                t = lb.get((name, y))
                if not t or not (t[2] > 0):
                    continue
                lbar, br, V = t
                num += V * lbar / br; den += V
                numi += V * (1 + LEV * max(0.0, lbar - 1)) / (1 + LEV * max(0.0, br - 1))
            if den > 0:
                W[y][s] = num / den; I[y][s] = numi / den
        for y in (1830, 1850):
            ls = [lb[(n, y)][0] for n in NINE if (n, y) in lb]; bs = [lb[(n, y)][1] for n in NINE if (n, y) in lb]
            if len(ls) >= 3:
                dl = math.log(max(ls) / min(ls)); db = math.log(max(bs) / min(bs))
                n3[y][s] = (dl / db if db > 0 else float('nan'), spearman(ls, bs), dl, db)
        taus = []
        for name in NINE:
            ys = sorted(y for (nm, y) in lb if nm == name and y <= 1913)
            drop = next((y for y in ys if lb[(name, y)][0] / lb[(name, y)][1] < 0.8), None)
            if drop is None:
                taus.append((name, 0)); continue
            rec = next((y for y in ys if y > drop and lb[(name, y)][0] / lb[(name, y)][1] >= 0.8), None)
            taus.append((name, (rec - drop) if rec is not None else (1913 - drop + 1)))
        n4[s] = taus
    med = lambda d: statistics.median(d.values())
    print("\n[W・I] 年末の値（種ごと・中央値）")
    for y in sorted(W):
        if y in (1790, 1795, 1800, 1805, 1810) or (y % 10 == 0) or y == 1913:
            print(f"  {y}\tW {[round(W[y][s], 3) for s in seeds]} 中央 {med(W[y]):.3f}\tI {[round(I[y][s], 3) for s in seeds]} 中央 {med(I[y]):.3f}")
    n1w = [(y, med(W[y])) for y in sorted(W) if 1790 <= y <= 1810]
    n1_lo = [p for p in n1w if p[1] < 0.8]; n1_hi = [p for p in n1w if p[1] > 1.25]
    N1 = not n1_lo and not n1_hi
    print(f"[N1] 1790〜1810 の W（種の中央値）の最小 {min(v for _, v in n1w):.3f}・最大 {max(v for _, v in n1w):.3f}・下側の外れ {len(n1_lo)} 年（最初 {n1_lo[0][0] if n1_lo else '-'}）・上側の外れ {len(n1_hi)} 年 → {'通る' if N1 else '外れ'}")
    n2y = [y for y in sorted(W) if 1850 <= y <= 1950]
    n2_wlo = [y for y in n2y if med(W[y]) < 0.5]; n2_ilo = [y for y in n2y if med(I[y]) < 0.5]; n2_whi = [y for y in n2y if med(W[y]) > 2]
    N2 = bool(n2y) and not n2_wlo and not n2_ilo and not n2_whi
    print(f"[N2] 1850〜1950：W＜0.5 {len(n2_wlo)} 年（最初 {n2_wlo[0] if n2_wlo else '-'}）・I＜0.5 {len(n2_ilo)} 年（最初 {n2_ilo[0] if n2_ilo else '-'}）・W＞2 {len(n2_whi)} 年・W の最小 {min((med(W[y]) for y in n2y), default=float('nan')):.3f}・I の最小 {min((med(I[y]) for y in n2y), default=float('nan')):.3f} → {'通る' if N2 else '外れ'}")
    N3 = True
    for y in (1830, 1850):
        for s in seeds:
            r, sp, dl, db = n3[y].get(s, (float('nan'),) * 4)
            ok = r >= 0.5 and sp >= 0.5
            N3 = N3 and ok
            print(f"[N3] {y} 種 {s}：ln(L̄ 最大÷最小)＝{dl:.4f}・ln(B 最大÷最小)＝{db:.4f}・比 {r:.3f}・Spearman {sp:+.3f} → {'通る' if ok else '外れ'}")
    N4 = True
    for s in seeds:
        m = statistics.median([t for _, t in n4[s]])
        N4 = N4 and m <= 20
        print(f"[N4] 種 {s}：τ（年）＝{n4[s]}・9 国の中央値 {m} → {'通る' if m <= 20 else '外れ'}")
    go = N1 and N2 and N3 and N4
    print(f"\n[判定] N1 {'○' if N1 else '×'}（上側 {len(n1_hi)}・下側 {len(n1_lo)}）・N2 {'○' if N2 else '×'}・N3 {'○' if N3 else '×'}・N4 {'○' if N4 else '×'} → {'go' if go else 'no-go'}")


if __name__ == '__main__':
    main()
