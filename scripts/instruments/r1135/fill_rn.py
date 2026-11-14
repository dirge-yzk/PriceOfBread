# (1135) c110rn〈追加〉門 G-R（国の充足 ΣT÷目標）の M・N1・N2 の 10 年ごとの中央と、ヌル腕の幅からの帯 1＋2×(w−1)。発見の後の州×年の割合（.cost の見込み用）も数える。
# 呼び方：python -X utf8 -I fill_rn.py <出力の txt> <ラベル=生> ...
import sys, os, glob
from collections import defaultdict
NAT = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'スペイン', 'アメリカ', '日本', '清']
DEC = [1850, 1870, 1890, 1910, 1930, 1950]
outp = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
res = {}; lines = []
for lab, ins in arms:
    vals = defaultdict(list); allrows = 0; postrows = 0
    for path in sorted(glob.glob(os.path.join(ins, 'trrail_s*.tsv'))):
        sd = path.rsplit('_s', 1)[1].split('.')[0]
        tg = {}
        with open(os.path.join(ins, f'trnation_s{sd}.tsv'), encoding='utf-8') as f:
            hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
            for line in f:
                c = line.rstrip('\n').split('\t')
                try: tg[(int(c[ix['year']]), c[ix['nation']])] = float(c[ix['railTargetIndex']])
                except (ValueError, IndexError, KeyError): pass
        tsum = defaultdict(float); gate = {}
        with open(path, encoding='utf-8') as f:
            hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
            for line in f:
                c = line.rstrip('\n').split('\t')
                y = int(c[ix['year']]); nn = c[ix['nation']]
                allrows += 1
                if float(c[ix['needEnd']]) > 0: postrows += 1; gate[(y, nn)] = True
                tsum[(y, nn)] += float(c[ix['railTrackEnd']])
        for (y, nn), s in tsum.items():
            if nn not in NAT or not gate.get((y, nn)): continue
            t = tg.get((y, nn))
            if not (t and t > 0): continue
            d = min(DEC, key=lambda z: abs(z - y))
            if abs(d - y) <= 5: vals[(nn, d)].append(s / t)
    med = {}
    for k, v in vals.items(): v.sort(); med[k] = v[len(v) // 2]
    res[lab] = med
    lines.append(f'{lab}：発見の後の州×年 {postrows}／全 {allrows}（{postrows / max(1, allrows):.3f}）')
ws = []
for nn in NAT:
    row = []
    for d in DEC:
        v = [res[l].get((nn, d)) for l, _ in arms]
        if any(x is None or not (x > 0) for x in v): row.append(f'{d} —'); continue
        w = max(v) / min(v); ws.append(w)
        row.append(f'{d} ' + '／'.join(f'{x:.3f}' for x in v) + f'（帯 ×{1 + 2 * (w - 1):.3f}）')
    lines.append(nn + '：' + '・'.join(row))
ws.sort()
if ws: lines.append(f'幅 w の 50／90 点 {ws[len(ws) // 2]:.3f}／{ws[int(0.9 * len(ws))]:.3f}・最大 {ws[-1]:.3f}（帯 1＋2×(w−1) の 50 点 ×{1 + 2 * (ws[len(ws) // 2] - 1):.3f}）')
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(lines) + '\n')
print('\n'.join(lines))
