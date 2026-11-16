# (1183) 旗 0（m1178）で E2・G2 の量を数える：開いた鉱床の州の品位（mintreg_s・年末）を開いて 5 年・30 年の後に読み、同じ年の全ての州の品位の中央と比べる。
import csv, statistics as S
csv.field_size_limit(10**9)
RAW = 'D:/PoB_work/keep_1178/base_ins'
e5 = []; g2 = []; n = 0
for sd in range(1, 11):
    opens = []
    with open(f'{RAW}/site_s{sd}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
        for r in rd:
            if r[ix['kind']] == 'O' and r[ix['siteKind']] == 'D': opens.append((r[ix['region']], int(r[ix['year']])))
    gr = {}
    with open(f'{RAW}/mintreg_s{sd}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
        for r in rd:
            if int(r[ix['q']]) % 4 != 3: continue
            try: gr[(int(r[ix['year']]), r[ix['region']])] = float(r[ix['grade']])
            except ValueError: pass
    med = {}
    for (y, reg), g in gr.items(): med.setdefault(y, []).append(g)
    for reg, y in opens:
        n += 1
        if (y+5, reg) in gr: e5.append(gr[(y+5, reg)])
        if (y+30, reg) in gr: g2.append(gr[(y+30, reg)] - S.median(med[y+30]))
print(f'開いた鉱床 {n}・開いて 5 年の州の品位 ≧0.5 の割合 {sum(1 for g in e5 if g>=0.5)/len(e5):.3f}（行 {len(e5)}・中央 {S.median(e5):.3f}）')
print(f'開いて 30 年の州の品位−同じ年の全ての州の中央：中央 {S.median(g2):.3f}（行 {len(g2)}・最小 {min(g2):.3f}・最大 {max(g2):.3f}）')
