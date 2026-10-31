# (705) 線 dsc1 段0：改良度 L の再生（盤面の応答なし）・D0 と K_L の決め・go／no-go（G-a〜G-c）・U2〜U4 の予言
# 事前登録 measurements/dsc1_705_20261031_prereg.txt [1][2][3]。入力＝旗 0 の canary の計器 dsc1_s<種>.tsv（InstrumentsDsc.cs）。
# 使い方：python replay.py <ins のフォルダ> <種,種,...> [--c5 <括弧を外した canary の ins のフォルダ> --c5seed 1]
# 読むだけ。バイトで読む（CR を保つ必要は無い＝書き出しは標準出力）。
import sys, math, statistics, argparse
from collections import defaultdict

NINE = ["清", "ロシア", "オスマン", "フランス", "イギリス", "プロイセン", "オーストリア", "日本", "アメリカ"]
RHO = 2.1
H = 1.0
ANCHOR = 3.0          # 英の綿布の L の 1825 年の値（McCloskey 1981 の 年 2.6%×42 年〈推・頁は未確認〉）
ANCHOR_COL = "綿布/既定"
LEV = 2.4             # IndTechLeverage


def load(path):
    N = {}                      # (q, uid) -> (name, tl, br, rep3, homePop)
    R = defaultdict(dict)       # q -> {(uid, col): (q, v, dg, eng, homePop, isRgo)}
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
                N[(q, uid)] = (a[4], float(a[ix['techLevel']]), float(a[ix['bracket']]), float(a[ix['replacement3']]), float(a[ix['homePopK']]))
            elif k == 'R':
                col = int(a[5]); colname[col] = a[6]
                R[q][(uid, col)] = (float(a[ix['qPerMonth']]), float(a[ix['valuePerMonth']]), float(a[ix['dgPerMonth']]),
                                     float(a[ix['engHomePerMonth']]), float(a[ix['homePopK']]), a[ix['isRgo']] == '1')
            elif k == 'C':
                C[q][int(a[5])][int(a[6])] = float(a[ix['qPerMonth']])
    return N, R, C, years, colname


def nine_uids(N):
    # 計器 9 国の uid（名前で引く・最初に見た uid＝開幕の国）
    out = {}
    for (q, uid), v in sorted(N.items()):
        if v[0] in NINE and v[0] not in out:
            out[v[0]] = uid
    return out


def d_values(R, years, uids, y0, y1):
    ds = []
    for q, rows in R.items():
        if not (y0 <= years[q] <= y1):
            continue
        for (uid, col), (qq, v, dg, eng, hp, rg) in rows.items():
            if uid in uids and eng > 0 and hp > 0:
                ds.append(eng / hp)
    return ds


def replay(N, R, C, years, K, D0, record=False, only=None):
    L = {}                      # (uid, col) -> L
    traj = {}                   # (q, uid, col) -> (L, E)   (record のときだけ・年末と全ての列)
    quarters = sorted(years)
    zero_run = defaultdict(int); run_start = {}
    u4 = []
    for q in quarters:
        rows = R.get(q, {})
        flows = C.get(q, {})
        # 新しく見た列は L=1（受容）
        for key in rows:
            if key not in L and (only is None or key[1] == only):
                L[key] = 1.0
        newL = {}
        bynation = defaultdict(list)
        for (uid, col) in L:
            bynation[uid].append(col)
        for (uid, col), l in L.items():
            r = rows.get((uid, col))
            nv = N.get((q, uid))
            rep3 = nv[3] if nv else 0.0
            if r:
                qq, v, dg, eng, hp, rg = r
                D = eng / hp if hp > 0 else 0.0
                T = D / (D + D0) if D > 0 else 0.0
                E = qq / (qq + H * dg) if (qq + H * dg) > 0 else 0.0
            else:
                T = 0.0; E = 0.0
            X = 0.0
            if (col + q) % 4 == 0:
                f = flows.get(uid)
                if f:
                    tot = sum(f.values())
                    if tot > 0:
                        for s, val in f.items():
                            lp = L.get((s, col))
                            if lp and lp > 0:
                                X += (val / tot) * max(0.0, 1.0 - l / lp)
                X *= 4.0
            dl = (T + E + X) / (3.0 * K * l ** RHO) - rep3 * (1.0 - E) * (l - 1.0)
            newL[(uid, col)] = l + dl
            if record:
                traj[(q, uid, col)] = (l + dl, E)
            # U4：q=0 の連続
            key = (uid, col)
            if r is None:
                if zero_run[key] == 0:
                    run_start[key] = (l, 1.0)
                zero_run[key] += 1
                s0, prod = run_start[key]
                run_start[key] = (s0, prod * (1.0 - rep3))
            else:
                if zero_run[key] >= 40:
                    s0, prod = run_start[key]
                    if s0 - 1.0 > 1e-6:
                        actual = 1.0 - (l - 1.0) / (s0 - 1.0)
                        pred = 1.0 - prod
                        if pred > 0:
                            u4.append(actual / pred)
                zero_run[key] = 0
        L = newL
    return L, traj, u4


def lbar_by_year(N, R, years, traj, uids):
    # 年末の四半期で、国ごとの値段の重みの L の平均と括弧
    out = {}
    for q in sorted(years):
        if q % 4 != 3:
            continue
        y = years[q]
        rows = R.get(q, {})
        for name, uid in uids.items():
            num = den = 0.0
            for (u, col), r in rows.items():
                if u != uid:
                    continue
                v = r[1]
                t = traj.get((q, u, col))
                if t and v > 0:
                    num += v * t[0]; den += v
            nv = N.get((q, uid))
            if nv and den > 0:
                out[(name, y)] = (num / den, nv[2], den)
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


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('ins'); ap.add_argument('seeds')
    a = ap.parse_args()
    seeds = [int(s) for s in a.seeds.split(',')]
    data = {s: load(f"{a.ins}/dsc1_s{s}.tsv") for s in seeds}
    # D0
    ds = []
    for s, (N, R, C, years, cn) in data.items():
        ds += d_values(R, years, set(nine_uids(N).values()), 1830, 1849)
    D0 = statistics.median(ds)
    print(f"[D0] 9 国×列×四半期（1830〜1849・D>0）の D の中央値＝{D0:.6g}（件数 {len(ds)}・種 {seeds}）")
    # 錨の列
    anchor_col = None
    for s, (N, R, C, years, cn) in data.items():
        for c, nm in cn.items():
            if nm == ANCHOR_COL:
                anchor_col = c
    print(f"[錨] 列 {anchor_col}（{ANCHOR_COL}）・英の 1825 年末の L＝{ANCHOR}")

    def anchor_L(K):
        vals = []
        for s, (N, R, C, years, cn) in data.items():
            uk = nine_uids(N)["イギリス"]
            qs = [q for q in years if years[q] == 1825 and q % 4 == 3]
            L, traj, _ = replay(N, R, C, {q: y for q, y in years.items() if q <= qs[0]}, K, D0, record=True, only=anchor_col)
            vals.append(traj.get((qs[0], uk, anchor_col), (float('nan'),))[0])
        return statistics.mean(vals), vals

    lo, hi = 0.05, 50.0
    for _ in range(40):
        mid = math.sqrt(lo * hi)
        v, _ = anchor_L(mid)
        if v > ANCHOR:
            lo = mid
        else:
            hi = mid
    K = math.sqrt(lo * hi)
    v, vals = anchor_L(K)
    print(f"[K_L] 二分探索＝{K:.4g}（英の綿布の 1825 年の L＝{v:.4f}・種ごと {[round(x, 3) for x in vals]}）")
    c = 2.0 / K
    print(f"[上界] c＝2÷K_L＝{c:.4g}・t＝668 の離散の上界 L≦{(1 + (RHO + 1) * c * (1 + c) ** RHO * 668) ** (1 / (RHO + 1)):.3f}")

    ga = defaultdict(list); gb = defaultdict(list); gc = defaultdict(list)
    u2 = defaultdict(list); u3 = []; u4all = []
    lmax = []
    for s, (N, R, C, years, cn) in data.items():
        uids = nine_uids(N)
        L, traj, u4 = replay(N, R, C, years, K, D0, record=True)
        u4all += u4
        lb = lbar_by_year(N, R, years, traj, uids)
        print(f"\n== 種 {s}：国ごとの Lbar÷括弧（R）と投資の信号の比（I）")
        print("国\t" + "\t".join(str(y) for y in (1800, 1830, 1850, 1870, 1890, 1913, 1930, 1950)))
        for name in NINE:
            cells = []
            for y in (1800, 1830, 1850, 1870, 1890, 1913, 1930, 1950):
                t = lb.get((name, y))
                cells.append(f"{t[0]:.2f}/{t[1]:.2f}" if t else "-")
            print(name + "\t" + "\t".join(cells))
        # G-a
        for name in NINE:
            ys = sorted(y for (nm, y) in lb if nm == name)
            y0 = next((y for y in ys if lb[(name, y)][1] > 1.1), None)
            if y0 is None:
                ga[name].append(0.0); continue
            lags = []
            for y in ys:
                if y < y0 or y > 1913:
                    continue
                Bt = lb[(name, y)][1]
                hit = next((y2 for y2 in ys if y2 >= y and lb[(name, y2)][0] >= Bt), None)
                lags.append((hit - y) if hit is not None else (1950 - y + 1))
            ga[name].append(statistics.median(lags) if lags else 0.0)
        # G-b・G-c
        for y in sorted(set(y for (_, y) in lb)):
            num = den = numi = 0.0
            for name in NINE:
                t = lb.get((name, y))
                if not t:
                    continue
                lbar, br, V = t
                num += V * lbar / br; den += V
                numi += V * (1 + LEV * max(0.0, lbar - 1)) / (1 + LEV * max(0.0, br - 1))
            if den > 0:
                gb[y].append(num / den); gc[y].append(numi / den)
        # U2
        yq = {years[q]: q for q in years if q % 4 == 3}
        for y in (1880, 1913):
            q = yq.get(y)
            if q is None:
                continue
            xs = []; es = []
            for (qq, uid, col), (l, e) in traj.items():
                if qq == q and uid in uids.values():
                    xs.append(l - 1.0); es.append(e)
            u2[y].append(spearman(xs, es))
        # U3
        def ratio_med(q):
            out = {}
            bycol = defaultdict(dict)
            for (qq, uid, col), (l, e) in traj.items():
                if qq == q and uid in uids.values():
                    bycol[col][uid] = l
            return bycol
        q50, q13 = yq.get(1850), yq.get(1913)
        if q50 is not None and q13 is not None:
            b50 = ratio_med(q50); b13 = ratio_med(q13)
            incs = []
            for col, d in b50.items():
                lead = max(d.values())
                if lead < 1.5 or len(d) < 2:
                    continue
                lu = max(d, key=d.get)
                r50 = statistics.median([l / lead for u, l in d.items() if u != lu])
                d13 = b13.get(col, {})
                if len(d13) < 2:
                    continue
                lead13 = max(d13.values()); lu13 = max(d13, key=d13.get)
                r13 = statistics.median([l / lead13 for u, l in d13.items() if u != lu13])
                incs.append(r13 - r50)
            u3.append((len(incs), statistics.median(incs) if incs else float('nan')))
        # L の分布（1913・9 国）
        if q13 is not None:
            ls = sorted(l for (qq, uid, col), (l, e) in traj.items() if qq == q13 and uid in uids.values())
            if ls:
                lmax.append((s, ls[len(ls) // 2], ls[int(len(ls) * 0.9)], ls[-1]))

    print("\n[G-a] 遅れ τ（年・国ごと・種の値）と 9 国の中央値")
    meds = []
    for name in NINE:
        print(f"  {name}\t{ga[name]}")
    for i in range(len(seeds)):
        m = statistics.median([ga[n][i] for n in NINE]); meds.append(m)
    print(f"  9 国の τ の中央値（種ごと）＝{meds}・線＝20 年")
    wmin = min((statistics.median(v), y) for y, v in gb.items())
    imin = min((statistics.median(v), y) for y, v in gc.items())
    print(f"[G-b] W の最小（種の中央値）＝{wmin[0]:.3f}（{wmin[1]} 年）・線＝0.5")
    print("      十年ごと：" + "・".join(f"{y} {statistics.median(gb[y]):.2f}" for y in sorted(gb) if y % 10 == 0))
    print(f"[G-c] 投資の信号の比の最小（種の中央値）＝{imin[0]:.3f}（{imin[1]} 年）・線＝0.5")
    print("      十年ごと：" + "・".join(f"{y} {statistics.median(gc[y]):.2f}" for y in sorted(gc) if y % 10 == 0))
    print(f"[U2 予言] (L−1) と E の順位相関：" + "・".join(f"{y} {[round(x, 3) for x in v]}" for y, v in u2.items()) + "（床 0.30）")
    print(f"[U3 予言] 1850 年に先頭 L≧1.5 の列の数と比の増えの中央値（種ごと）＝{u3}（床 +0.10）")
    print(f"[U4 予言] 40 四半期以上 q＝0 の休みの件数 {len(u4all)}・実際の減り÷式の予言の中央値＝{statistics.median(u4all) if u4all else float('nan')}（1/2〜2 倍）")
    print(f"[L の分布 1913・9 国] 種・中央・90%・最大＝{lmax}")
    nogo = statistics.median(meds) > 20 or wmin[0] < 0.5 or imin[0] < 0.5
    print(f"\n[判定] {'no-go（段1 の腕を回さず記録だけ）' if nogo else 'go'}")


if __name__ == '__main__':
    main()
