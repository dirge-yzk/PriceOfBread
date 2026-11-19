# (1228) c124bk 同じ盤面の新÷旧（記録）：国営持分の赤字の請求（fiscal_s claimStateLossPerMonth）と払い下げの件数（nation_s stateFactorySoldCount）を
#   国ごと・種の和で、腕と基準を並べる。paysale_s があれば月の損益が負の生産者×月（negOld・negNew）も。
#   使い方: python -X utf8 -I cmp_bk.py <腕の ins> <基準の ins> [旗 0 の paysale の ins] [種の始め=1] [種の終わり=10]
import sys, os, collections
MISSING = []   # (1241) 表が無い・列が無いときは黙って 0 にせず、最後に非 0 で終える
A, B = sys.argv[1], sys.argv[2]; Z = sys.argv[3] if len(sys.argv) > 3 and sys.argv[3] != '-' else None
S0 = int(sys.argv[4]) if len(sys.argv) > 4 else 1; S1 = int(sys.argv[5]) if len(sys.argv) > 5 else 10
def col(d, tab, c, s):
    p = os.path.join(d, f'{tab}{s}.tsv'); r = collections.Counter()
    if not os.path.exists(p): MISSING.append(p); return None
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        if c not in ix or 'nation' not in ix: MISSING.append(p + f'（列 {c} か nation が無い）'); return None
        for l in f:
            x = l.rstrip('\n').split('\t')
            try: v = float(x[ix[c]])
            except: continue
            if v == v: r[x[ix['nation']]] += v
    return r
L = []
for tab, c in (('fiscal_s', 'claimStateLossPerMonth'), ('nation_s', 'stateFactorySoldCount')):
    ta, tb = collections.Counter(), collections.Counter()
    for s in range(S0, S1 + 1):
        a = col(A, tab, c, s); b = col(B, tab, c, s)
        if a: ta.update(a)
        if b: tb.update(b)
    L.append(f'== {tab}:{c}（種 {S0}〜{S1} の和・国ごと）  腕 {sum(ta.values()):.6g}  基準 {sum(tb.values()):.6g}  新÷旧 {(sum(ta.values())/sum(tb.values())) if sum(tb.values()) else float("nan"):.4g}')
    for n in sorted(set(ta) | set(tb), key=lambda k: -(ta[k] + tb[k]))[:12]:
        L.append(f'  {n}\t腕 {ta[n]:.6g}\t基準 {tb[n]:.6g}\t新÷旧 {(ta[n]/tb[n]) if tb[n] else float("nan"):.4g}')
if Z:
    for tag, d in (('腕（旗 1）', A), ('旗 0（腕 Z′）', Z)):
        t = collections.Counter()
        for s in range(S0, S1 + 1):
            p = os.path.join(d, f'paysale_s{s}.tsv')
            if not os.path.exists(p):
                p = os.path.join(d, f'paysale2_s{s}.tsv')   # (1241) 改名後の表
                if not os.path.exists(p): MISSING.append(p); continue
            with open(p, encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                for l in f:
                    x = l.rstrip('\n').split('\t')
                    for k in ('prodMonths', 'negOld', 'negNew', 'flipNegToPos', 'payToProducers', 'payExport', 'payMint', 'payDomestic'): t[k] += float(x[ix[k]])
        L.append(f'== paysale_s {tag}：' + '・'.join(f'{k} {t[k]:.6g}' for k in ('prodMonths', 'negOld', 'negNew', 'flipNegToPos', 'payToProducers', 'payExport', 'payMint', 'payDomestic')))
print('\n'.join(L))
if MISSING:
    print('表が無い・列が無い：' + ' / '.join(MISSING[:6]) + (f' ほか {len(MISSING) - 6} 件' if len(MISSING) > 6 else ''), file=sys.stderr)
    sys.exit(2)
