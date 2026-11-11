# (1034) c97op：nationgood_s で量の側を読む（行の頻度でなく量＝作りすぎの罰の手がかり・読むだけ）
# 使い方: python op_ng.py <pre> <出力>
# 値の重み＝priceMeanGold（金建ての値段）。量の単位は表の名乗りのまま（producedQty・demandQty・shortageQty は四半期の最後の清算の月の量・cross*Qty は清算 1 回分＝同じ月）。
import sys, os, statistics as st
from collections import defaultdict
PRE, OUT = sys.argv[1], sys.argv[2]
ARMS = ['base_ins', 'n1', 'n2']; MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
res = defaultdict(list)
for arm in ARMS:
    for s in range(1, 11):
        p = os.path.join(PRE, 'ng_%s_s%d.tsv' % (arm, s))
        acc = defaultdict(lambda: defaultdict(float))
        with open(p, encoding='utf-8') as f:
            for ln in f:
                v = ln.rstrip('\n').split('\t')
                y, nat, g, tier = v[0], v[1], v[2], v[3]
                if nat not in MAJ: continue
                P, D, S, St = float(v[4]), float(v[5]), float(v[6]), float(v[7])
                IQ, EQ, px = float(v[10]), float(v[11]), float(v[12])
                if not (px == px) or px <= 0: continue
                for sc in ('6大国', nat):
                    a = acc[(y, sc)]
                    a['P'] += P * px; a['D'] += D * px; a['S'] += S * px; a['I'] += IQ * px; a['E'] += EQ * px
                    if D > 1e-9:
                        a['cells'] += 1
                        r = P / D
                        if 0.8 <= r <= 1.25: a['near1'] += 1
                        if r > 1.25: a['over'] += 1
                        if EQ > 0.05 * D: a['exp5'] += 1
                        if IQ > 0.05 * D: a['imp5'] += 1
        for (y, sc), a in acc.items():
            D = a['D'] or float('nan'); P = a['P'] or float('nan'); c = a['cells'] or float('nan')
            res[(y, sc, arm)].append({'作った÷使った(額)': a['P'] / D, '不足÷使った(額)': a['S'] / D, '越境の輸入÷使った(額)': a['I'] / D,
                                     '越境の輸出÷作った(額)': a['E'] / P, 'ます:作った÷使った0.8〜1.25': a['near1'] / c, 'ます:作った÷使った>1.25': a['over'] / c,
                                     'ます:輸出>使った5%': a['exp5'] / c, 'ます:輸入>使った5%': a['imp5'] / c})
with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('year\tscope\tmeasure\tbase_med\tbase_min\tbase_max\tn1_med\tn2_med\n')
    for y in ('1870', '1913'):
        for sc in ['6大国'] + MAJ:
            b = res.get((y, sc, 'base_ins'))
            if not b: continue
            for m in b[0]:
                def md(L):
                    L = [x[m] for x in L if x[m] == x[m]]; return (st.median(L), min(L), max(L)) if L else (float('nan'),) * 3
                bb = md(b); n1 = md(res.get((y, sc, 'n1'), [])); n2 = md(res.get((y, sc, 'n2'), []))
                f.write('%s\t%s\t%s\t%.4g\t%.4g\t%.4g\t%.4g\t%.4g\n' % (y, sc, m, bb[0], bb[1], bb[2], n1[0], n2[0]))
print('done')
