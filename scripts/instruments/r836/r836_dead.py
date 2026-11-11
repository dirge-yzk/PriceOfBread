# (836) 追加の読み（事前登録の外・記録）：人数が厳密に 0 の POP が持つ在庫（〈人の居ない在庫〉）の州の在庫に対する割合を、出来事 15 件と対照で、
# 四半期 0・入りの 8 四半期前・入りの四半期に読む。最大の持ち手の階級と、その POP の四半期 0 からの精算の所得の累計（どこから入ったか）。
import csv, sys, os, statistics
from collections import defaultdict
RAW, EV, CT, OUT = sys.argv[1:5]
csv.field_size_limit(10**9)
def fl(x):
    try: return float(x)
    except: return float('nan')
events = list(csv.DictReader(open(EV, encoding='utf-8'), delimiter='\t'))
controls = list(csv.DictReader(open(CT, encoding='utf-8'), delimiter='\t'))
ctl = defaultdict(list)
for c in controls: ctl[(c['seed'], c['event'])].append(c['control'])
want = defaultdict(set)   # seed -> regions
for e in events:
    want[e['seed']].add(e['region'])
    for c in ctl[(e['seed'], e['region'])]: want[e['seed']].add(c)
hist = {}   # (seed, region, q) -> list rows（必要な列だけ）
for s in sorted(want, key=int):
    with open(os.path.join(RAW, f'cpop_s{s}.tsv'), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['region'] in want[s]:
                hist.setdefault((s, r['region'], int(r['quarter'])), []).append((r['nation'], r['className'], fl(r['popK']), fl(r['savings']), fl(r['inMarketPerQuarter']), fl(r['outMarketPerQuarter']), r['popIndex']))
def dead(seed, reg, nat, q):
    rs = [x for x in hist.get((seed, reg, q), []) if nat is None or x[0] == nat]
    tot = sum(x[3] for x in rs)
    d = [x for x in rs if x[2] == 0 and x[3] > 0]
    ds = sum(x[3] for x in d)
    top = max(d, key=lambda x: x[3])[1] if d else '-'
    return (ds / tot if tot > 0 else float('nan')), top, tot
L = ['(836) 追加：人の居ない在庫（人数が厳密に 0 の POP の在庫）の州の在庫に対する割合（事前登録の外＝記録）', '',
     '種\t州\t国\t入り\t割合@0\t割合@入り−8\t割合@入り\t最大の持ち手\t州の在庫@0\t州の在庫@入り\t対照の割合@入り（中央・数）']
evs, cts = [], []
for e in events:
    s, reg, nat, oq = e['seed'], e['region'], e['nation'], int(e['onsetQ'])
    a0, t0, s0 = dead(s, reg, nat, 0); a8, _, _ = dead(s, reg, nat, oq - 8); a1, t1, s1 = dead(s, reg, nat, oq)
    cv = [dead(s, c, nat, oq)[0] for c in ctl[(s, reg)]]
    cv = [x for x in cv if x == x]
    evs.append(a1); cts.extend(cv)
    L.append(f"{s}\t{reg}\t{nat}\t{oq}\t{a0:.3f}\t{a8:.3f}\t{a1:.3f}\t{t1}\t{s0:.4g}\t{s1:.4g}\t{statistics.median(cv) if cv else float('nan'):.3f}・{len(cv)}")
L.append('')
L.append(f"出来事の入りの四半期の割合：中央 {statistics.median(evs):.3f}・最小 {min(evs):.3f}・0.5 以上 {sum(1 for x in evs if x >= 0.5)}/{len(evs)}")
L.append(f"対照（同じ種・同じ国・同じ四半期）の割合：中央 {statistics.median(cts):.3f}・0.5 以上 {sum(1 for x in cts if x >= 0.5)}/{len(cts)}")
open(os.path.join(OUT, 'dead_stock.txt'), 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
