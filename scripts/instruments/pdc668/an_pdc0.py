# (668) pdc 段0 の読み手：PopDecision の都市の天井を州全体の割合で比べたときの向きと大きさの予言（読むだけ・腕は回さない）。
# 使い方：python -I an_pdc0.py <計器つき10種の出力フォルダ>  （urxpd_s*.tsv と urx_s*.tsv を読む。UTF-8 で標準出力へ）
# 書いたモデル Claude Sonnet 5.5。限界：国×時代の窓ごとの集計の上で式を当てる近似（州×月の min の凸性は無視）＝向きと桁だけを読む。
import sys, glob, collections
sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1]
MOB = 0.02   # Coefficients.Mobility（読むだけ・リテラルの写し）
NM = {'イギリス': 'UK', 'プロイセン': 'PRU', 'フランス': 'FRA', 'ロシア': 'RUS', 'オーストリア': 'AUT', 'アメリカ': 'USA', '日本': 'JPN', '清': 'QING', 'オスマン': 'OTT'}
ORDER = ['UK', 'PRU', 'FRA', 'RUS', 'AUT', 'USA', 'JPN', 'QING', 'OTT']
WINS = [(1783, 1800), (1800, 1830), (1830, 1860), (1860, 1890), (1890, 1920), (1920, 1951)]
acc = collections.defaultdict(lambda: collections.defaultdict(lambda: collections.defaultdict(float)))
pdp = collections.defaultdict(float); pdn = collections.defaultdict(float); smu = collections.defaultdict(float)
files = sorted(glob.glob(D + '/urxpd_s*.tsv')); ns = len(files)
for f in files:
    L = open(f, encoding='utf-8').read().splitlines(); h = L[0].split('\t')
    for ln in L[1:]:
        r = dict(zip(h, ln.split('\t'))); y = int(r['year']); n = NM[r['nation']]
        i = next(k for k, (a, b) in enumerate(WINS) if a <= y < b)
        A = acc[n][i]; A['m'] += float(r['regionMonths']); A['cb'] += float(r['capBound'])
        for k in ['sumFlowPos', 'sumFlowNeg', 'sumTotal', 'sumPairUrban', 'sumRegionPop', 'sumRegionUrban', 'wTargetPre', 'wTargetCapped', 'wCur', 'wCap']:
            A[k] += float(r[k])
        pdp[n] += float(r['sumFlowPos']); pdn[n] += float(r['sumFlowNeg'])
for f in sorted(glob.glob(D + '/urx_s*.tsv')):
    for ln in open(f, encoding='utf-8').read().splitlines()[1:]:
        c = ln.split('\t')
        if c[3] == 'Core' and c[4] == '社会移動': smu[NM[c[2]]] += float(c[6])
print(f'# 種の数 {ns}（{D}）')
print('# [1] 国×窓：capBound＝天井に当たった州×月の割合／cap＝天井（Total 重み）／S＝州全体の都市の割合／cur＝対の都市の割合／pf＝対が州の人口に占める割合')
print('#     F1＝天井を州全体の割合で比べ流れを人数の残枠で動かす形（目標＝cur+(cap−S)/pf）の対の目標・流れ（千人／州×月・ゲート無し）／F2＝州全体で当たりを決め流入だけ止める形の流れ／old＝今の流れ（ゲート込みの実測）')
print('nation window | capBound cap S cur pf tPre | F1target F1flow | F2flow | oldflow')
nover = ntot = nneg = nf1neg = 0
for n in ORDER:
    for i, (a, b) in enumerate(WINS):
        A = acc[n][i]; T = A['sumTotal']
        if T == 0: continue
        cap = A['wCap'] / T; cur = A['wCur'] / T; tp = A['wTargetPre'] / T
        S = A['sumRegionUrban'] / A['sumRegionPop']; pf = T / A['sumRegionPop']
        t1 = min(tp, cur + (cap - S) / pf); f1 = MOB * (t1 - cur) * T / A['m']
        t2 = min(tp, cur) if S >= cap else min(tp, cap); f2 = MOB * (t2 - cur) * T / A['m']
        old = (A['sumFlowPos'] - A['sumFlowNeg']) / A['m']
        ntot += 1; nover += S > cap; nneg += t1 < 0; nf1neg += f1 < 0
        print(f'{n} {a}-{b} | {A["cb"]/A["m"]:.2f} {cap:.2f} {S:.2f} {cur:.2f} {pf:.2f} {tp:.2f} | {t1:+.2f} {f1:+.2f} | {f2:+.2f} | {old:+.2f}')
print(f'# 集計：S>cap の国×窓 {nover}／{ntot}・F1 の対の目標が負（対の都市が空になる）{nneg}／{ntot}・F1 の流れが負 {nf1neg}／{ntot}')
print('# [2] 綱引き（本国の州・全期間の和の種平均・千人）：PD＝PopDecision の流れ（pos＝農村→都市・neg＝都市→農村）／SM＝社会移動の都市の増減')
print('nation PD_pos PD_neg PD_net SM_urban net_urban min(PD_neg,SM_urban)')
for n in ORDER:
    pn = pdp[n] / ns; ng = pdn[n] / ns; s = smu[n] / ns
    print(f'{n} {pn:.0f} {ng:.0f} {pn-ng:+.0f} {s:+.0f} {pn-ng+s:+.0f} {min(ng, max(s, 0)):.0f}')
