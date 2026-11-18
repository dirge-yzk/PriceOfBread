# (1218) c122r2 段0：負の地代の行（ColonialPopTests の読み口）を分ける（コミットしない・合流が写す）。
#   python -X utf8 -I r1218class.py <flag0.tsv> <flag1.tsv> <出力.txt>
# 区分（行＝種・月・州・財の大農園・月 1〜12）：
#   穴＝rentTest<0 だが 輸出の地代の近似（expRentApprox）を足すと ≥0（LastRevenue に載らない輸出・買い上げの代金で説明がつく〈推〉）
#   真＝足しても負。そのうち 規模↑（前の月より Scale が増えた）と 在庫↑（前の月より Stock が増えた）を数える。
#   死＝値段×産出が 0 に近く（|rev|<1e-9 かつ out<1e-9）・需要 0 の行（MarketFlow.cs:64 で値段が動かない市場）。
import sys, csv, collections
f0, f1, outp = sys.argv[1], sys.argv[2], sys.argv[3]
def fl(x):
    try: return float(x)
    except: return float('nan')
L = []
for tag, p in (('旗0', f0), ('旗1', f1)):
    with open(p, encoding='utf-8', newline='') as fh:
        fh.readline(); rows = [r for r in csv.DictReader(fh, delimiter='\t') if r['kind'] == 'P']
    prev = {}
    c = collections.Counter(); byreg = collections.Counter()
    for r in rows:
        k = (r['seed'], r['region'], r['good']); m = int(r['month'])
        pv = prev.get(k); prev[k] = r
        if m == 0 or pv is None: continue
        c['行'] += 1
        rt, er = fl(r['rentTest']), fl(r['expRentApprox'])
        if fl(r['out']) < 1e-9 and abs(fl(r['rev'])) < 1e-9 and fl(r['lastDemand']) < 1e-9: c['死'] += 1
        if not (rt < -1e-9): continue
        if rt + er >= 0: c['負・穴'] += 1; continue
        c['負・真'] += 1; byreg[(r['nation'], r['region'], r['good'])] += 1
        if fl(r['scale']) > fl(pv['scale']): c['負・真・規模↑'] += 1
        if fl(r['stock']) > fl(pv['stock']): c['負・真・在庫↑'] += 1
        if fl(r['scale']) > fl(pv['scale']) and fl(r['stock']) > fl(pv['stock']): c['負・真・規模↑在庫↑'] += 1
    L.append(f'{tag}: ' + '・'.join(f'{k} {v}' for k, v in c.items()))
    for k, v in byreg.most_common(8): L.append(f'  {tag} 負・真の多い大農園: {k[0]} {k[1]} {k[2]} {v} 行')
with open(outp, 'w', encoding='utf-8', newline='\n') as fh: fh.write('\n'.join(L) + '\n')
print('\n'.join(L))
