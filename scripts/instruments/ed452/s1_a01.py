import csv, statistics as st
NAT8 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']
P = {'P2': (68, 267), 'P3': (268, 467), 'P4': (468, 667)}
for tag in ('A', 'B'):
    tot = hit = 0; w = t = 0.0; wq = tq = 0.0
    per = {}
    with open(f'D:/PoB_work/ed_452/out_{tag}_1/ins/ed_s1.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['nation'] not in NAT8: continue
            q = int(r['quarter'])
            p = next((k for k, (a, b) in P.items() if a <= q <= b), None)
            if p is None: continue
            fs = float(r['fundingShare'])
            if fs > 1e-9:
                tot += 1; sh = float(r['shortRegionShare'])
                if sh == sh and sh > 0: hit += 1
                w += float(r['stateWantMoneyPerMonth']); t += float(r['stateTakeMoneyPerMonth'])
                wq += float(r['stateWantPerMonth']); tq += float(r['stateTakePerMonth'])
                d = per.setdefault(p, [0, 0, 0.0, 0.0]); d[0] += 1; d[1] += (sh == sh and sh > 0); d[2] += float(r['stateWantMoneyPerMonth']); d[3] += float(r['stateTakeMoneyPerMonth'])
    print(tag, f'a0 場面割合={hit/tot:.3f} ({hit}/{tot})  a1 Σtake÷Σwant(金額)={t/w:.3f} (数量)={tq/wq:.3f}')
    for p, d in per.items(): print('   ', p, f'場面={d[1]/d[0]:.3f} 買えた割合={d[3]/d[2]:.3f}')
