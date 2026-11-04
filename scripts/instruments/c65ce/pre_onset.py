"""(822) 先に読む（keep_818/base_ins・読むだけ・記録）：凍結の 12 州の入り（稼ぎが 1e-6 未満へ落ちた四半期）の前後 8 四半期の持ち主・人口・稼ぎ・持ち主の国の人頭税の段・交戦の月。"""
import csv, os
B = 'D:/PoB_work/keep_818/base_ins'
T = [(4, 'カナダ'), (4, 'セントマイケル'), (10, 'イキトス'), (10, 'チロエ'), (10, 'ロレート'), (10, 'カルメン・デ・パタゴネス'), (10, 'エルパソ'), (10, 'パラワン'),
     (10, 'ボゴタ'), (10, 'キト'), (10, 'ラパス'), (10, 'ヌエバ・エスパーニャ銀地帯')]
def rows(p):
    with open(p, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r: yield ix, x
out = open('D:/PoB_work/c65ce_work/pre_onset.tsv', 'w', encoding='utf-8', newline='\n')
out.write('seed\tregion\tonsetQ\tonsetYear\towner\tpollAtOnset\tpoll8QBefore\twarMonths8QBefore\tearnPC8QBefore\tearnPCOnset\tpopOnset\n')
for sd in (4, 10):
    reg = {}
    for ix, x in rows(os.path.join(B, 'region_s%d.tsv' % sd)):
        reg.setdefault(x[ix['region']], []).append((int(x[ix['quarter']]), int(x[ix['year']]), x[ix['nation']], float(x[ix['populationK']]), float(x[ix['earnedPerMonth']])))
    nat = {}
    for ix, x in rows(os.path.join(B, 'nation_s%d.tsv' % sd)):
        try: nat[(x[ix['nation']], int(x[ix['quarter']]))] = (float(x[ix['pollTaxEndShare']]), float(x[ix['atWarMonths']]))
        except Exception: pass
    for s, rg in T:
        if s != sd: continue
        rr = sorted(reg.get(rg, []))
        onset = None
        for i in range(1, len(rr)):
            q, y, n, p, e = rr[i]
            if p > 0 and e / p < 1e-6 and all(rr[j][3] > 0 and rr[j][4] / rr[j][3] < 1e-6 for j in range(i, min(i + 8, len(rr)))):
                onset = i; break
        if onset is None: out.write('%d\t%s\t-\n' % (sd, rg)); continue
        q, y, n, p, e = rr[onset]
        b = rr[max(0, onset - 8)]
        pa = nat.get((n, q), (float('nan'), 0))[0]; pb = nat.get((b[2], b[0]), (float('nan'), 0))[0]
        war = sum(nat.get((n, qq), (0, 0))[1] for qq in range(q - 8, q))
        out.write('\t'.join(map(str, [sd, rg, q, y, n, '%.3f' % pa, '%.3f' % pb, '%.0f' % war, '%.3g' % (b[4] / b[3] if b[3] > 0 else float('nan')), '%.3g' % (e / p), '%.1f' % p])) + '\n')
out.close()
print(open('D:/PoB_work/c65ce_work/pre_onset.tsv', encoding='utf-8').read())
