# (1297) c134bi 段 0：プローブの表の要約（銀・金・鉄鉱石・石炭の世界の産出の和を、播種の直後・助走の四半期・助走の後で並べる）。
# 使い方：python probe_sum.py <probe のフォルダ>
import csv, sys, os, statistics as st
D = sys.argv[1]
ARMS = ['f0', 'f1', 'f1s', 'f1B', 'f1L', 'f0Bo', 'f1Bn']
rows = {}
for a in ARMS:
    p = os.path.join(D, a + '.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        lines = [l for l in f if not l.startswith('#')]
    for r in csv.DictReader(lines, delimiter='\t'):
        rows[(a, int(r['seed']), r['phase'], int(r['q']), r['good'])] = r
seeds = sorted({k[1] for k in rows})
def pts(a):
    qs = sorted({(k[2], k[3]) for k in rows if k[0] == a}, key=lambda x: ({'seed': 0, 'bake': 1, 'warm': 2, 'run': 3}[x[0]], x[1]))
    want = []
    for ph, q in qs:
        if ph in ('seed', 'bake') or (ph == 'warm' and q in (1, 2, 4, 8, 12, 16, 20, 40, 60, 80, 100, 120)) or (ph == 'run' and q in (4, 8)): want.append((ph, q))
    return want
for g in ['銀', '金', '鉄鉱石', '石炭']:
    print(f'== {g}（種 {seeds} の中央）＝産出 Output の和・括弧は能力 Capacity の和・Scale の和・在庫の和')
    for a in ARMS:
        if not any(k[0] == a for k in rows): continue
        cells = []
        for ph, q in pts(a):
            v = [rows[(a, s, ph, q, g)] for s in seeds if (a, s, ph, q, g) in rows]
            if not v: continue
            o = st.median(float(x['sumOut']) for x in v); c = st.median(float(x['sumCap']) for x in v)
            sc = st.median(float(x['sumScale']) for x in v); sk = st.median(float(x['sumStock']) for x in v)
            cells.append(f'{ph}{q}:{o:.0f}({c:.0f}・{sc:.1f}・{sk:.0f})')
        print(f' {a}: ' + ' '.join(cells))
print('== 旗 1 ÷ 旗 0（産出の和・種ごと）')
for g in ['銀', '金', '鉄鉱石', '石炭']:
    for ph, q in [('seed', 0), ('warm', 1), ('warm', 4), ('warm', 20), ('run', 8)]:
        r = []
        for s in seeds:
            a0 = rows.get(('f0', s, ph, q, g)); a1 = rows.get(('f1', s, ph, q, g))
            if a0 and a1 and float(a0['sumOut']) > 0: r.append(float(a1['sumOut']) / float(a0['sumOut']))
        print(f' {g} {ph}{q}: ' + ' '.join(f'{x:.3f}' for x in r))
