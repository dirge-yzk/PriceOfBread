# (833) 分母の痩せの 5 件の前後（四半期・人口・totalPopulationK・名目 GDP・内戦の月・戦争の月）を写す（記録）
import csv, sys
P = 'D:/PoB_work/'
C = [('N2n', P+'keep_826/n2', 2, 'ポルトガル', 518), ('N2n', P+'keep_826/n2', 8, 'スペイン', 622),
     ('DWS', P+'keep_832_rej/mS_s11/ins', 11, 'ポルトガル', 644), ('DWS', P+'keep_832_rej/mS_s11/ins', 18, 'バタヴィア王国', 404),
     ('MD', P+'keep_826_rej/c65mdB_10/ins', 3, 'バタヴィア共和国', 452)]
for nm, d, s, nat, q0 in C:
    print(f'== {nm} 種 {s} {nat} 跳び q{q0}\tq\tpopulationK\ttotalPopulationK\tnominalGdpPerYear\tcivilWarMonths\tatWarMonths\tcoreRegions')
    with open(f'{d}/nation_s{s}.tsv', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        for x in r:
            q = int(x[ix['quarter']])
            if x[ix['nation']] == nat and q0 - 5 <= q <= q0 + 1:
                print('\t' + '\t'.join(x[ix[c]] for c in ['quarter', 'populationK', 'totalPopulationK', 'nominalGdpPerYear', 'civilWarMonths', 'atWarMonths', 'coreRegions']))
