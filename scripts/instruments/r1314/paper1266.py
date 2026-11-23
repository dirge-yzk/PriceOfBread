# (1266) c129dh 紙の影：自給の世帯の売る割合（盤面 m1260・keep_1260/base_ins 種 1〜10・読むだけ）
# 使い方: python -X utf8 -I paper1266.py <base_ins のフォルダ> <出力 tsv> <要約 txt>
# 定義（労働の単位・州の農民の POP・subst_s の s2 の列＝エンジンが書いた値）：
#   cap＝人力の農業労働の能力・sold＝売れた量・U＝cap−sold・Lneed＝必要量×人数÷(ρ×T)（食べる分を作るのに要る労働）
#   soc＝sold÷cap（今の形の〈売る割合〉＝市場へ出した労働の割合）
#   kH＝1−min(U,Lneed)÷cap（形 H1′ の影＝売った労働＋自分の畑の余り〈U−Lneed が正の分〉）
#   sig＝食べる割合（subRateWeighted を subPopK で重み）・sraw＝ρUT÷(必要量×人数)（切る前）
import sys, os, math, statistics as st
from collections import defaultdict

base, out_tsv, out_txt = sys.argv[1], sys.argv[2], sys.argv[3]
WIN = {'open': None, '1850': 1850, '1913': 1913}

def fnum(x):
    try:
        v = float(x)
        return v if math.isfinite(v) else None
    except ValueError:
        return None

# acc[(seed, nation, win)] = [subPop, subPop*sig, cap, sold, minUL, surplus, rows, rowsSurplus, capCore, soldCore, minULCore]
acc = defaultdict(lambda: [0.0] * 11)
for seed in range(1, 11):
    p = os.path.join(base, f'subst_s{seed}.tsv')
    with open(p, 'r', encoding='utf-8', newline='') as f:
        hdr = f.readline().rstrip('\r\n').split('\t')
        ix = {h: i for i, h in enumerate(hdr)}
        need = ['quarter', 'year', 'nation', 'tier', 'subPopK', 'subRateWeighted', 's2BasisKind', 's2CapEnd', 's2SoldEnd', 's2UEnd', 's2LneedEnd']
        for k in need:
            if k not in ix: raise SystemExit('列が無い: ' + k)
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            q, y = c[ix['quarter']], c[ix['year']]
            if q == '0': win = 'open'
            elif y == '1850': win = '1850'
            elif y == '1913': win = '1913'
            else: continue
            a = acc[(seed, c[ix['nation']], win)]
            sp, sr = fnum(c[ix['subPopK']]), fnum(c[ix['subRateWeighted']])
            if sp is not None and sr is not None and sp > 0:
                a[0] += sp; a[1] += sp * sr
            if c[ix['s2BasisKind']] != '2': continue
            cap, sold, u, ln = (fnum(c[ix[k]]) for k in ('s2CapEnd', 's2SoldEnd', 's2UEnd', 's2LneedEnd'))
            if None in (cap, sold, u, ln) or not cap > 0: continue
            m = min(u, ln) if ln > 0 else u
            a[2] += cap; a[3] += sold; a[4] += m; a[5] += max(0.0, u - ln); a[6] += 1
            if u > ln: a[7] += 1
            if c[ix['tier']] == 'Core':
                a[8] += cap; a[9] += sold; a[10] += m

# 農民の割合（class_s）
peas = defaultdict(lambda: [0.0, 0.0])
for seed in range(1, 11):
    p = os.path.join(base, f'class_s{seed}.tsv')
    with open(p, 'r', encoding='utf-8', newline='') as f:
        f.readline()
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            t, y = c[0], c[2]
            if t == 'open': win = 'open'   # q0 の行は 9 国だけ＝開幕は open の行（助走の前の構成）
            elif y == '1850' and t.startswith('q'): win = '1850'
            elif y == '1913' and t.startswith('q'): win = '1913'
            else: continue
            v = fnum(c[5])
            if v is None: continue
            a = peas[(seed, c[1], win)]
            a[1] += v
            if c[4] == '農民': a[0] += v

nations = sorted({k[1] for k in acc} | {k[1] for k in peas})
def med(xs):
    xs = [x for x in xs if x is not None]
    return st.median(xs) if xs else None
def stat(seed, n, w):
    a = acc.get((seed, n, w))
    if a is None: return None
    sig = a[1] / a[0] if a[0] > 0 else None
    soc = a[3] / a[2] if a[2] > 0 else None
    kh = 1 - a[4] / a[2] if a[2] > 0 else None
    sur = a[5] / a[2] if a[2] > 0 else None
    socC = a[9] / a[8] if a[8] > 0 else None
    khC = 1 - a[10] / a[8] if a[8] > 0 else None
    pz = peas.get((seed, n, w)); ps = pz[0] / pz[1] if pz and pz[1] > 0 else None
    return dict(sig=sig, soc=soc, kh=kh, sur=sur, socC=socC, khC=khC, peas=ps, rows=a[6], rowsS=a[7])

KEYS = ['sig', 'soc', 'kh', 'sur', 'socC', 'khC', 'peas']
rows = []
for n in nations:
    r = {'nation': n}
    for w in WIN:
        ss = [stat(s, n, w) for s in range(1, 11)]
        ss = [x for x in ss if x is not None]
        r[w + '_n'] = len(ss)
        for k in KEYS: r[w + '_' + k] = med([x[k] for x in ss])
        r[w + '_rows'] = sum(x['rows'] for x in ss); r[w + '_rowsS'] = sum(x['rowsS'] for x in ss)
    rows.append(r)

def F(v): return 'nan' if v is None else f'{v:.4f}'
cols = ['nation'] + [f'{w}_{k}' for w in WIN for k in ['n'] + KEYS + ['rows', 'rowsS']]
with open(out_tsv, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\t'.join(cols) + '\n')
    for r in rows:
        f.write('\t'.join(r['nation'] if c == 'nation' else (str(r[c]) if c.endswith(('_n', '_rows', '_rowsS')) else F(r[c])) for c in cols) + '\n')

L = []
def P(s): L.append(s)
P('# (1266) 紙の影（m1260・keep_1260/base_ins 種 1〜10・国ごとに種の中央）')
P(f'国の数（subst_s か class_s に現れた）: {len(nations)}')
for w in WIN:
    have = [r for r in rows if r[w + '_sig'] is not None]
    sig = [r[w + '_sig'] for r in have]
    P(f'[{w}] 自給の行がある国 {len(have)}・食べる割合 sig の国の中央 {F(med(sig))}・p10 {F(sorted(sig)[len(sig)//10]) if sig else "nan"}・p90 {F(sorted(sig)[(9*len(sig))//10]) if sig else "nan"}')
    for k in ('soc', 'kh', 'sur', 'peas'):
        xs = [r[w + '_' + k] for r in rows if r[w + '_' + k] is not None]
        P(f'    {k}: 国 {len(xs)}・中央 {F(med(xs))}・最小 {F(min(xs)) if xs else "nan"}・最大 {F(max(xs)) if xs else "nan"}')
    P(f'    余りの立つ行（U＞Lneed）: {sum(r[w + "_rowsS"] for r in rows)} / {sum(r[w + "_rows"] for r in rows)} 行')
op = [r for r in rows if r['open_sig'] is not None]
for lo, hi in ((0.68, 0.72), (0.65, 0.75), (0.6, 0.8)):
    P(f'開幕の sig が [{lo},{hi}] の国: {sum(1 for r in op if lo <= r["open_sig"] <= hi)} / {len(op)}')
for k in ('soc', 'kh', 'socC', 'khC'):
    a = [r for r in rows if r['open_' + k] is not None and r['1850_' + k] is not None]
    b = [r for r in rows if r['open_' + k] is not None and r['1913_' + k] is not None]
    c = [r for r in rows if r['open_' + k] is not None and r['1850_' + k] is not None and r['1913_' + k] is not None]
    P(f'{k}: 開幕→1850 で増えた国 {sum(1 for r in a if r["1850_"+k] > r["open_"+k])}/{len(a)}・開幕→1913 {sum(1 for r in b if r["1913_"+k] > r["open_"+k])}/{len(b)}・両方とも単調に増えた {sum(1 for r in c if r["open_"+k] < r["1850_"+k] < r["1913_"+k])}/{len(c)}')
a = [r for r in rows if r['open_sig'] is not None and r['1913_sig'] is not None]
P(f'sig: 開幕→1913 で下がった国 {sum(1 for r in a if r["1913_sig"] < r["open_sig"])}/{len(a)}')
a = [r for r in rows if r['open_peas'] is not None and r['1913_peas'] is not None]
P(f'農民の割合: 開幕→1913 で下がった国 {sum(1 for r in a if r["1913_peas"] < r["open_peas"])}/{len(a)}')
P('')
P('国\t開幕 sig/soc/kH/農民\t1850 sig/soc/kH/農民\t1913 sig/soc/kH/農民')
for r in rows:
    P(r['nation'] + '\t' + '\t'.join('/'.join(F(r[w + '_' + k]) for k in ('sig', 'soc', 'kh', 'peas')) for w in WIN))
with open(out_txt, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(L) + '\n')
print('\n'.join(L[:40]))
