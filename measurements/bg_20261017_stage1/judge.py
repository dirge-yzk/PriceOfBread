import sys, os, re
D = 'D:/PoB_work/bg'
def load(a):
    d = {}
    for ln in open(f'{D}/anq_{a}.txt', encoding='utf-8'):
        if ' = ' in ln and not ln.startswith('#'):
            k, v = ln.rstrip('\n').split(' = ', 1); d[k] = v
    return d
def f(d, k):
    try: return float(d[k])
    except Exception: return float('nan')
null = [load(a) for a in ['A', 'N1', 'N2']]
arms = sys.argv[1:]
HIST = {1785: 1.38, 1790: 1.23, 1815: 1.65}
for a in arms:
    d = load(a); print(f'===== {a}')
    ok_years = []
    for y, h in HIST.items():
        k = f'H1 英の債務÷年GDP {y}（中央）'
        w = abs(f(null[1], k) - f(null[2], k))
        lim = max(abs(f(n, k) - h) for n in null) + w
        dist = abs(f(d, k) - h)
        ok = dist <= lim; ok_years.append((y, ok))
        print(f'H1 {y}: B={f(d,k):.3f} 距離 {dist:.3f} 上限 {lim:.3f}（w={w:.3f}）→ {"合格" if ok else "不合格"}  （2.5 の錨との距離 {abs(f(d,k)-2.5):.3f}）' if y == 1815 else f'H1 {y}: B={f(d,k):.3f} 距離 {dist:.3f} 上限 {lim:.3f}（w={w:.3f}）→ {"合格" if ok else "不合格"}')
    h1 = dict(ok_years)[1815] and (dict(ok_years)[1785] or dict(ok_years)[1790])
    print('H1 全体:', '合格' if h1 else '不合格')
    r = [f(d, f'H2 英の国債金利 {y}（中央）') for y in (1785, 1790, 1800, 1815, 1850)]
    print('H2①:', ['%.3f' % x for x in r], '合格' if all(0.03 <= x <= 0.06 for x in r) else '不合格')
    k = 'H3 不履行 世界（10種の和）'; w = abs(f(null[1], k) - f(null[2], k)); mx = max(f(n, k) for n in null)
    lim = mx + max(w, 0.1 * f(null[0], k)); print(f'H3①: {f(d,k):.0f} 上限 {lim:.1f} → {"合格" if f(d,k) <= lim else "不合格"}  下限の疑い {f(d,k) <= 0.6*min(f(n,k) for n in null)}')
    k = 'H3 不履行 英'; mx = max(f(n, k) for n in null); print(f'H3②: {f(d,k):.0f} 上限 {mx+1:.0f} → {"合格" if f(d,k) <= mx+1 else "不合格"}')
    k = 'H4 英の最初の兌換停止の年（中央・停止した種のみ）'; lo = sorted(f(n, k) for n in null)[0] - 2.0
    print(f'H4: {f(d,k):.1f} 下限 {lo:.1f} → {"合格" if f(d,k) >= lo else "不合格"}')
    for g in ['全体', 'イギリス', 'フランス']:
        k = f'H5 金利≧0.30 の国×四半期の割合 {g}'; print(f'H5 {g}: {f(d,k)*100:.2f}% → {"合格" if f(d,k) < 0.01 else "不合格"}')
