# (1042) 問2：機会の因子を国ごと・期間ごとに人口で重みを付けて並べる（B 10 種）＋米の出る州・清の入る州の上位
import sys, collections
ARMS = {'B': 'D:/PoB_work/keep_1035/base_ins', 'N1': 'D:/PoB_work/keep_1027/n1', 'N2': 'D:/PoB_work/keep_1027/n2'}
NAT = ['アメリカ', 'イギリス', '清', '日本', 'ロシア', 'フランス', 'プロイセン', 'オーストリア']
COLS = ['opportunityIndex', 'holdingFactor', 'wageFactor', 'landFactor', 'employmentShare', 'pushFactor', 'housingFactor', 'foodFillShare']
def per(y): return '1783-1849' if y < 1850 else ('1850-1899' if y < 1900 else '1900-1950')
acc = collections.defaultdict(float)
regout = collections.Counter(); regin = collections.Counter()
for s in range(1, 11):
    with open(f"{ARMS['B']}/region_s{s}.tsv", 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.decode('utf-8').rstrip('\r\n').split('\t'); n = x[ix['nation']]
            if x[ix['activeFlag']] != '1': continue
            out = float(x[ix['crossBorderOutKPerQuarter']]); inn = float(x[ix['crossBorderInKPerQuarter']])
            if n == 'アメリカ': regout[x[ix['region']]] += out
            if n == '清': regin[x[ix['region']]] += inn
            if n not in NAT: continue
            p = per(int(x[ix['year']])); pk = float(x[ix['populationK']])
            if not (pk > 0): continue
            acc[(n, p, 'w')] += pk
            for c in COLS:
                v = float(x[ix[c]])
                if v == v: acc[(n, p, c)] += pk * v
            # 残る値打ち（出る側）＝機会×持ち分×食料
            o = float(x[ix['opportunityIndex']]); hd = float(x[ix['holdingFactor']]); ff = float(x[ix['foodFillShare']])
            if o == o and hd == hd and ff == ff: acc[(n, p, 'stay')] += pk * o * hd * ff
            acc[(n, p, 'out')] += out; acc[(n, p, 'in')] += inn
print('# 国×期間の人口で重みを付けた平均（B・10 種）・out/in は千人の和')
print('国\t期間\t' + '\t'.join(COLS) + '\tstay\tout\tin')
for n in NAT:
    for p in ['1783-1849', '1850-1899', '1900-1950']:
        w = acc[(n, p, 'w')]
        if not w: continue
        print(n + '\t' + p + '\t' + '\t'.join(f'{acc[(n, p, c)] / w:.3f}' for c in COLS) + f'\t{acc[(n, p, "stay")] / w:.3f}\t{acc[(n, p, "out")]:.0f}\t{acc[(n, p, "in")]:.0f}')
print('# 米の出る州の上位（越境の出・千人・10 種の和）', [(k, round(v)) for k, v in regout.most_common(8)])
print('# 清の入る州の上位（越境の入り）', [(k, round(v)) for k, v in regin.most_common(8)])
