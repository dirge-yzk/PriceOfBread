# (1299) 問い4の2：国の主流の札が空になる国×四半期（labeltier_s）と、C 型の潜在（本国格の州が全て world.json の culture "" の州）を数える。
# 使い方: python -X utf8 -I q4_label.py <生のフォルダ> <種の最初> <種の最後> <出力>
import sys, csv, os, collections
raw, s0, s1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
C12 = {'蝦夷地', 'セントポール', 'ウラジオストク', 'ヤクーツク', 'オホーツク', 'ペトロパヴロフスク', 'スレドネコリムスク', 'アナディリ', 'トゥルハンスク', 'オブドルスク', 'グリエフ', 'レイキャヴィク'}
def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: yield ix, row
lines = []; tot = collections.Counter(); ex = []
for s in range(s0, s1 + 1):
    lp = os.path.join(raw, f'labeltier_s{s}.tsv'); rp = os.path.join(raw, f'region_s{s}.tsv')
    if not os.path.exists(lp): lines.append(f'seed {s}: labeltier_s が無い'); continue
    c = collections.Counter()
    for ix, row in rd(lp):
        col = int(row[ix['colonialRegions']]); ec = row[ix['powerCultureEmptyFlag']] == '1'; er = row[ix['powerReligionEmptyFlag']] == '1'
        hc = float(row[ix['homeLabelledPopC']] or 0)
        side = 'col>0' if col > 0 else 'col=0'
        c[('rows', side)] += 1
        if ec: c[('emptyC', side)] += 1
        if er: c[('emptyR', side)] += 1
        if hc <= 0: c[('homeLabelC=0', side)] += 1
        if ec and col == 0: ex.append(f"{s}\t{row[ix['quarter']]}\t{row[ix['year']]}\t{row[ix['nation']]}\t{row[ix['regions']]}")
    # C 型の潜在：本国格の州（人口>0）が全て C12
    home = collections.defaultdict(list); colN = collections.Counter()
    for ix, row in rd(rp):
        k = (row[ix['quarter']], row[ix['nation']])
        if row[ix['tier']] == 'Colonial': colN[k] += 1
        elif float(row[ix['populationK']] or 0) > 0: home[k].append(row[ix['region']])
    pot = [(k, v) for k, v in home.items() if v and all(x in C12 for x in v)]
    for k, v in pot:
        side = 'col>0' if colN[k] > 0 else 'col=0'
        c[('C12only', side)] += 1
        if len(ex) < 400: ex.append(f"{s}\t{k[0]}\t-\t{k[1]}\tC12only:{'/'.join(v)}\t{side}")
    # 本国格の州の中に C12 があって、C12 の人口が本国格の札の付いた人口の過半の国×四半期は数えない（札は人口の多数で決まる＝C12 は空で飛ぶだけ）
    lines.append(f"seed {s}: " + '・'.join(f'{a}[{b}] {v}' for (a, b), v in sorted(c.items())))
    tot.update(c)
lines.append('合計: ' + '・'.join(f'{a}[{b}] {v}' for (a, b), v in sorted(tot.items())))
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(lines) + '\n\n# 例（種・四半期・年・国・州）\n' + '\n'.join(ex[:400]) + '\n')
print(lines[-1])
