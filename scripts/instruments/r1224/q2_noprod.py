# (1224) 問い 2：作り手の無い日用品（食料でない日用品）が満足度の分母に占める割合〈推＝必要の重みは goods.json の baseNeed・門は世界の誰かが作るか買う財で近似・習慣の在庫は数えない〉
#   python -X utf8 -I q2_noprod.py <rawdir> <goods.json> <out> <種:国>...
import sys, json, collections
raw, gj, outp = sys.argv[1], sys.argv[2], sys.argv[3]
pairs = [a.split(':') for a in sys.argv[4:]]
G = {g['name']: g for g in json.load(open(gj, encoding='utf-8-sig'))}
daily = {n for n, g in G.items() if g['tier'] == 'Daily' and not (g.get('nutrition', -1) > 0)}
def fl(x):
    try: return float(x)
    except: return float('nan')
L = []
for seed, nat in pairs:
    ng = collections.defaultdict(dict); alive = collections.defaultdict(set)
    with open(f'{raw}/nationgood_s{seed}.tsv', encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for l in f:
            r = dict(zip(h, l.rstrip('\n').split('\t'))); y = int(r['year'])
            if fl(r['producedQty']) > 0 or fl(r['demandQty']) > 0: alive[y].add(r['good'])
            if r['nation'] == nat: ng[y][r['good']] = r
    sat = collections.defaultdict(list)
    with open(f'{raw}/satneeds_s{seed}.tsv', encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for l in f:
            r = dict(zip(h, l.rstrip('\n').split('\t')))
            if r['nation'] == nat: sat[int(r['year'])].append(r)
    L.append(f'== 種 {seed} {nat}（年・日用品の必要の重みのうち国内の産出 0 の割合・そのうち輸入も 0 の割合・分母に占める日用品の割合〔satneeds の年平均〕・積＝作り手の無い日用品の分母の割合〈推〉・作り手の無い財の名）')
    for y in sorted(ng):
        if y < 1900: continue
        A = [g for g in daily if g in alive[y]]
        w = {g: G[g]['baseNeed'] for g in A}
        tot = sum(w.values())
        nop = [g for g in A if fl(ng[y].get(g, {}).get('producedQty', '0')) <= 0]
        noimp = [g for g in nop if fl(ng[y].get(g, {}).get('crossImportQty', '0')) <= 0]
        S = sat.get(y, [])
        dt = sum(fl(r['dailyTotal']) for r in S); ot = sum(fl(r['otherTotal']) for r in S); lm = sum(fl(r['luxuryMet']) for r in S)
        dsh = dt / (dt + ot + lm) if dt + ot + lm > 0 else float('nan')
        a = sum(w[g] for g in nop) / tot if tot else float('nan'); b = sum(w[g] for g in noimp) / tot if tot else float('nan')
        L.append(f'  {y}: 産出0 {a:.3f}（{len(nop)}/{len(A)} 財）・うち輸入も0 {b:.3f}・日用品の分母の割合 {dsh:.3f}・積 {a*dsh:.3f}・{"・".join(sorted(nop))}')
open(outp, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n'); print('\n'.join(L))
