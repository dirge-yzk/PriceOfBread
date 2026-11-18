# (1218) c122r2 段0 の読み手（コミットしない・合流が写す）。
#   python -X utf8 -I r1218read.py <flag0.tsv> <flag1.tsv> <出力.txt> [州の名=ジャマイカ]
# 読むもの：①名乗り行（旗の上書き）②エンジンとの突き合わせ（invChk＝前の月末の在庫から組んだ InventoryAdjust と、Output の式から逆算した値の一致・pfChk＝月末の値段で組み直した採算と DesiredOutput÷Capacity の一致）
#   ③ColonialPopTests:117 と同じ読み口の地代＝Σ(LastRevenue−LastCost)（不在地主の大農園）④輸出の代金の地代の近似（LastExport×州の値段×産出の取り分×地代の率×(1−国営持分)＝〈推〉の近似）
#   ⑤州の大農園の月ごとの値段・採算・在庫の調整・産出・売れた量・在庫・規模。
import sys, csv, collections
f0, f1, outp = sys.argv[1], sys.argv[2], sys.argv[3]
focus = sys.argv[4] if len(sys.argv) > 4 else 'ジャマイカ'

def load(p):
    with open(p, encoding='utf-8', newline='') as fh:
        head = fh.readline().strip()
        rows = list(csv.DictReader(fh, delimiter='\t'))
    return head, rows

def fl(x):
    try: return float(x)
    except: return float('nan')

L = []
data = {}
for tag, p in (('旗0', f0), ('旗1', f1)):
    head, rows = load(p)
    data[tag] = rows
    L.append(f'{tag} 名乗り: {head}')
    flags = collections.Counter(r['flag'] for r in rows)
    L.append(f'{tag} 行の旗の値（World.Coef.EndowmentCostFollowsAbundance）: {dict(flags)}')
    ic = collections.Counter(r['invChk'] for r in rows if r['kind'] == 'P' and r['month'] != '0')
    pc = collections.Counter(r['pfChk'] for r in rows if r['kind'] == 'P' and r['month'] != '0')
    L.append(f'{tag} invChk（1＝一致・0＝不一致・-1＝比べられない〔産出 0・採算 0〕）: {dict(ic)}  pfChk: {dict(pc)}')

L.append('')
L.append(f'■ {focus} の地代（ColonialPopTests の読み口）＝種ごとの 1・6・12 か月目と 12 か月の和・輸出の代金の地代の近似の 12 か月の和')
for tag in ('旗0', '旗1'):
    R = [r for r in data[tag] if r['kind'] == 'R' and r['region'] == focus]
    by = collections.defaultdict(dict)
    for r in R: by[int(r['seed'])][int(r['month'])] = (fl(r['rentTest']), fl(r['expRentApprox']))
    neg = 0; sums = []
    for s in sorted(by):
        d = by[s]
        tot = sum(d[m][0] for m in d if m > 0); ex = sum(d[m][1] for m in d if m > 0)
        nneg = sum(1 for m in d if m > 0 and d[m][0] < 0)
        neg += nneg; sums.append(tot)
        L.append(f'  {tag} 種{s:>2}: 月1 {d.get(1,(0,0))[0]:+.4g} 月6 {d.get(6,(0,0))[0]:+.4g} 月12 {d.get(12,(0,0))[0]:+.4g} 和 {tot:+.5g} 負の月 {nneg}/12 ・輸出の地代の近似の和 {ex:+.5g}')
    L.append(f'  {tag} 負の月の合計 {neg}/{12*len(by)}・12 か月の和の最小 {min(sums):+.5g} 最大 {max(sums):+.5g}')

L.append('')
L.append('■ 英仏の植民地格の州ぜんぶ（不在地主の大農園つき）の地代の 12 か月の和（種 1〜10 の和）と負の月の数')
for tag in ('旗0', '旗1'):
    agg = collections.defaultdict(lambda: [0.0, 0, 0, 0.0])
    for r in data[tag]:
        if r['kind'] != 'R' or r['month'] == '0': continue
        a = agg[(r['nation'], r['region'])]; v = fl(r['rentTest'])
        a[0] += v; a[1] += 1; a[2] += v < 0; a[3] += fl(r['expRentApprox'])
    for k in sorted(agg):
        a = agg[k]
        if abs(a[0]) < 1e-9 and abs(a[3]) < 1e-9: continue
        L.append(f'  {tag} {k[0]} {k[1]}: 地代の和 {a[0]:+.5g}・負の月 {a[2]}/{a[1]}・輸出の地代の近似の和 {a[3]:+.5g}')

L.append('')
L.append(f'■ {focus} の大農園の月ごと（種 1・旗0 と旗1）')
cols = ['price', 'mcNow', 'pfEng', 'invAdjPre', 'out', 'soldDom', 'export', 'stock', 'lastDemand', 'rev', 'cost', 'rentTest', 'scale', 'smMargin']
for tag in ('旗0', '旗1'):
    P = [r for r in data[tag] if r['kind'] == 'P' and r['region'] == focus and r['seed'] == '1']
    for r in P:
        if r['month'] not in ('0', '1', '2', '3', '6', '9', '12'): continue
        L.append(f'  {tag} 月{r["month"]:>2} {r["good"]}: ' + ' '.join(f'{c}={fl(r[c]):.4g}' for c in cols))

L.append('')
L.append(f'■ {focus} の大農園の 12 か月目（種 1〜10）＝旗1÷旗0（同じ種）')
key = lambda r: (r['seed'], r['good'])
m12 = {tag: {key(r): r for r in data[tag] if r['kind'] == 'P' and r['region'] == focus and r['month'] == '12'} for tag in data}
for c in ('price', 'pfEng', 'invAdjPre', 'out', 'soldDom', 'export', 'stock', 'scale'):
    for g in sorted({k[1] for k in m12['旗0']}):
        rs = []
        for k in m12['旗0']:
            if k[1] != g or k not in m12['旗1']: continue
            a, b = fl(m12['旗0'][k][c]), fl(m12['旗1'][k][c])
            rs.append(b / a if a else float('nan'))
        fin = sorted(x for x in rs if x == x)
        if fin: L.append(f'  {g} {c}: 旗1÷旗0 中央 {fin[len(fin)//2]:.4g} 最小 {fin[0]:.4g} 最大 {fin[-1]:.4g}（{len(fin)} 種）')

with open(outp, 'w', encoding='utf-8', newline='\n') as fh:
    fh.write('\n'.join(L) + '\n')
print(f'wrote {outp} lines={len(L)}')
