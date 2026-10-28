"""(639) 線 fsc 段0（続き）：税の下げの一歩が門を通っているか。読むだけ。
入力＝計器つき10種の生の1フォルダ。使い方：python an_fsc_gate.py <ins フォルダ> [国名,...]
対象＝国庫÷線が4以上で、実効の税（土地税・人頭税）が目標より高い（＝下げたい）国×四半期。次の四半期との実効の差を見る：
  down＝次の四半期に下がった割合・flat＝動かなかった割合・up＝上がった割合（種の中央値）。flat が大きいほど〈門が通さない〉。
併せて、同じ帯で実効が目標以下（下げ切っている）の割合。"""
import csv, sys, glob, re, os, io, statistics as st
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline=None)
csv.field_size_limit(1 << 30)
d = sys.argv[1]
NAT = ['フランス', 'オーストリア', '日本', 'イギリス', 'アメリカ', 'プロイセン', 'ロシア', 'オスマン', '清']
if len(sys.argv) > 2: NAT = sys.argv[2].split(',')
def rows(f):
    with open(f, 'rb') as fh:
        rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd)
        ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            if len(r) == len(h): yield ix, r
def fl(x):
    try: return float(x)
    except: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
res = {}
for sd in range(1, 11):
    f1 = os.path.join(d, f'fsx_s{sd}.tsv')
    if not os.path.exists(f1): continue
    tl = {}
    for ix, r in rows(f1):
        if not r[ix['time']].startswith('q'): continue
        n = r[ix['nation']]
        if n not in NAT: continue
        t, l = fl(r[ix['treasuryEnd']]), fl(r[ix['lineEnd']])
        if l == l and l > 0 and t == t: tl[(n, int(r[ix['time']][1:]))] = t / l
    ser = {}
    for ix, r in rows(os.path.join(d, f'nation_s{sd}.tsv')):
        n = r[ix['nation']]
        if n not in NAT: continue
        q = r[ix['quarter']]
        if not q.lstrip('-').isdigit(): continue
        ser[(n, int(q))] = (fl(r[ix['landTaxEndShare']]), fl(r[ix['landTaxTargetEndShare']]), fl(r[ix['pollTaxEndShare']]), fl(r[ix['pollTaxTargetEndShare']]))
    for tag, ia, it in (('land', 0, 1), ('poll', 2, 3)):
        acc = {}
        for (n, q), t in tl.items():
            if t < 4: continue
            a, b = ser.get((n, q)), ser.get((n, q + 1))
            if a is None or b is None or not (a[ia] == a[ia] and a[it] == a[it] and b[ia] == b[ia]): continue
            x = acc.setdefault(n, [0, 0, 0, 0, 0])   # 4以上の四半期数, 下げたい, down, flat, up
            x[0] += 1
            if a[ia] > a[it] + 1e-9:
                x[1] += 1
                dv = b[ia] - a[ia]
                if dv < -1e-9: x[2] += 1
                elif dv > 1e-9: x[4] += 1
                else: x[3] += 1
        for n, x in acc.items():
            res.setdefault((n, tag), []).append(x)
print('# 入力', d)
print('国 税 : 国庫÷線≥4 の四半期数（種の中央値）／そのうち実効＞目標 の割合／（その四半期の次の四半期で）down flat up の割合')
for n in NAT:
    for tag in ('land', 'poll'):
        L = res.get((n, tag))
        if not L: continue
        c = med([x[0] for x in L])
        if not c or c < 8: continue
        print(f'{n} {tag}: n={c:.0f} wantdown={med([x[1] / x[0] for x in L if x[0]]):.2f} down={med([x[2] / x[1] for x in L if x[1]]):.2f} flat={med([x[3] / x[1] for x in L if x[1]]):.2f} up={med([x[4] / x[1] for x in L if x[1]]):.2f}')
