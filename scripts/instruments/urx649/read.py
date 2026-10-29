import csv, glob, os, statistics as st, sys
# (649) urx_s／urxpd_s の読み手（読むだけ）。使い方：
#   python read.py <腕0の計器フォルダ> <腕1の計器フォルダ> [Q=388] [国,国,...]   ＝K1−K0 の同じ種の対の差を区間別に分解（事前登録 [4]①②④・追記 2 ⓐ）
#   python read.py <腕0の計器フォルダ> -               [Q=388] [国,...]            ＝1 腕の絶対の分解（基準からの水準の変化）
# 群は Core（本国）。水準＝_level、区間の寄与＝その四半期までの区間の差の累積。単位は千人。割合＝都市÷(都市＋農村)。
Q_DEFAULT = 388
NATS_DEFAULT = ['イギリス', 'プロイセン', 'フランス', 'ネーデルラント']

def load(d):
    out = {}
    for f in sorted(glob.glob(os.path.join(d, 'urx_s*.tsv'))):
        seed = int(os.path.basename(f)[5:-4])
        base, lvl, sec = {}, {}, {}
        with open(f, encoding='utf-8') as fh:
            r = csv.reader(fh, delimiter='\t'); next(r)
            for row in r:
                q, nat, grp, name = int(row[0]), row[2], row[3], row[4]
                du, dr = float(row[6]), float(row[7])
                if name == '_base': base[(nat, grp)] = (du, dr)
                elif name == '_level': lvl[(nat, grp, q)] = (du, dr)
                else:
                    a = sec.setdefault((nat, grp, name), {}); a[q] = (du, dr)
        out[seed] = (base, lvl, sec)
    return out

def cum(sec, nat, grp, name, Q):
    a = sec.get((nat, grp, name), {})
    return (sum(v[0] for q, v in a.items() if q <= Q), sum(v[1] for q, v in a.items() if q <= Q))

def share(u, r): return u / (u + r) if (u + r) > 0 else float('nan')

def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')

def window_share(lvl, nat, lo=380, hi=396):
    xs = [share(*lvl[(nat, 'Core', q)]) for q in range(lo, hi + 1) if (nat, 'Core', q) in lvl]
    return sum(xs) / len(xs) if xs else float('nan')

def main():
    d0 = sys.argv[1]; d1 = sys.argv[2] if len(sys.argv) > 2 and sys.argv[2] != '-' else None
    Q = int(sys.argv[3]) if len(sys.argv) > 3 else Q_DEFAULT
    nats = sys.argv[4].split(',') if len(sys.argv) > 4 else NATS_DEFAULT
    A0 = load(d0); A1 = load(d1) if d1 else None
    seeds = sorted(set(A0) & set(A1)) if A1 else sorted(A0)
    print(f'# 腕0={d0} 腕1={d1} Q={Q} 種={seeds}')
    for nat in nats:
        names = []
        for s in seeds:
            for (n, g, name) in A0[s][2]:
                if n == nat and g == 'Core' and name not in names: names.append(name)
            if A1:
                for (n, g, name) in A1[s][2]:
                    if n == nat and g == 'Core' and name not in names: names.append(name)
        if A1 is None:
            print(f'\n== {nat}（Core）1 腕の絶対の分解：基準から四半期 {Q} まで')
            tot = [A0[s][1].get((nat, 'Core', Q)) for s in seeds]
            base = [A0[s][0].get((nat, 'Core')) for s in seeds]
            ok = [(t, b) for t, b in zip(tot, base) if t and b]
            if not ok: print('  行なし'); continue
            print(f'  水準の変化 都市 {med([t[0]-b[0] for t,b in ok]):+.1f}K・農村 {med([t[1]-b[1] for t,b in ok]):+.1f}K  割合 {med([share(*b) for t,b in ok]):.3f}→{med([share(*t) for t,b in ok]):.3f}')
            for name in names:
                cs = [cum(A0[s][2], nat, 'Core', name, Q) for s in seeds]
                print(f'  {name:<10} 都市 {med([c[0] for c in cs]):+9.1f}  農村 {med([c[1] for c in cs]):+9.1f}')
            continue
        # 対の差
        per = []
        for s in seeds:
            l0 = A0[s][1].get((nat, 'Core', Q)); l1 = A1[s][1].get((nat, 'Core', Q))
            if not l0 or not l1: continue
            dsec = {name: tuple(a - b for a, b in zip(cum(A1[s][2], nat, 'Core', name, Q), cum(A0[s][2], nat, 'Core', name, Q))) for name in names}
            per.append((s, l1[0] - l0[0], l1[1] - l0[1], share(*l1) - share(*l0), dsec, window_share(A1[s][1], nat) - window_share(A0[s][1], nat)))
        if not per: print(f'\n== {nat}: 行なし'); continue
        n = len(per)
        DU = med([p[1] for p in per]); DR = med([p[2] for p in per]); DS = med([p[3] for p in per]); DW = med([p[5] for p in per])
        neg = sum(1 for p in per if p[3] < 0)
        print(f'\n== {nat}（Core）K1−K0 四半期 {Q}：種 {n}')
        print(f'  D_全体 都市 {DU:+.1f}K（中央値）・農村 {DR:+.1f}K・割合 {DS:+.4f}（種ごと負 {neg}/{n}・絶対値の中央 {med([abs(p[3]) for p in per]):.4f}）｜四半期 380〜396 の平均の差 {DW:+.4f}')
        print(f'  種ごとの割合の差：' + ' '.join(f'{p[0]}:{p[3]:+.3f}' for p in per))
        rows = []
        for name in names:
            xs = [p[4][name][0] for p in per]
            rows.append((name, med(xs), sum(1 for x in xs if x * DU > 0) if DU == DU else 0, xs))
        same = [r for r in rows if r[1] * DU > 0]; opp = [r for r in rows if r[1] * DU < 0]
        sumsame = sum(r[1] for r in same); sumopp = sum(r[1] for r in opp)
        print(f'  区間の寄与（都市 K・中央値）と、D_全体 と同じ符号の種の数／{n}：')
        for name, m, k, xs in sorted(rows, key=lambda r: -abs(r[1])):
            if abs(m) < 0.05 and k in (0, n): continue
            print(f'    {name:<10} {m:+9.1f}  {k}/{n}')
        if DU < 0:
            car = max(same, key=lambda r: abs(r[1])) if same else None
            if car:
                share_car = abs(car[1]) / abs(DU); share_opp = abs(sumopp) / abs(DU)
                print(f'  担い手＝{car[0]}（{car[1]:+.1f}K・種 {car[2]}/{n} が同じ符号）｜担い手の寄与 {share_car*100:.0f}% of |D_全体|・逆符号の和 {share_opp*100:.0f}%｜(ii′) 担い手≥50% かつ 逆符号≤50%：{"通る" if share_car>=0.5 and share_opp<=0.5 else "通らない"}')
        # C1：区間の和（中央値でなく種ごと）と水準の差の一致
        worst = 0
        for p in per:
            tot = sum(v[0] for v in p[4].values())
            worst = max(worst, abs(tot - p[1]))
        print(f'  C1（種ごとの 区間の和−水準の差 の最大 K）：{worst:.3g}')

if __name__ == '__main__': main()
