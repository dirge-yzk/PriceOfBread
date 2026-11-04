# (815) c14fl 紙の台本（読むだけ・今の盤面＝keep_813/base_ins の divhost_s と nation_s）
# 使い方：python paper_b.py <ins フォルダ> <種 例 1-10>
# 読む物：divhost_s の全ての呼び出しの行（助走の呼び出しも CapitalMarket.Step が走った回＝(b) が落とす回）。
#   claim＝divOldIndex（旧の固定の率の請求＝CapitalDividendRatePerQuarter×fci）・paid＝paidIndex（実際に移った額）・fci・fxRate。
# 出す物：
#  [1] 旗 0 の盤面の未払い：未払い＝claim−paid（>1e-9 の事象）・事象の数・額の割合（金建て）。
#  [2] 門 G1 の旗 0 と突然変異の値：旗 0 は落とした額 0＝比 0（下限 0.999 で落ちる）・2 倍は比 2（上限 1.001 で落ちる・fci で頭打ちの事象は数える）。
#  [3] (b) の 1 次の見込み（粗い）：同じ国の系列で、落とした後の fci' を 2 通りに追う（上下の幅）。
#      加法＝fci'＝max(0, fci−累積の落とした額)（流入・逃避の額は旧のまま）・乗法＝fci'＝fci×Π(1−落とした額÷fci')（流入も逃避も比で縮む）。
#      落とした額＝max(0, 0.02×fci'−払える額)・払える額＝旧で未払いがあった回は paid（投資家の貯蓄が尽きた）・無かった回は上限なし。
#      外資の負債÷年 GDP（nation_s の nominalGdpPerYear・fci>0 の国・1850／1900／1949）の中央と p90。
#  [4] (c) の大きさ：払った利回り（四半期）÷fci を年へ（×4）・政策の目盛へ（÷PolicyRateBondWeight 0.02＝Fiscal.cs の国債の式の唯一の換算）。
#      同じ四半期の fci>0 の国の中の幅（p90−p10）の中央と、政策の目盛の項の幅（bondPolicyRateTermPerYear÷0.02＝InterestRate×兌換の割引）の同じ幅。
import sys, csv, os, collections
d = sys.argv[1]; spec = sys.argv[2]
seeds = list(range(int(spec.split('-')[0]), int(spec.split('-')[1]) + 1)) if '-' in spec else [int(x) for x in spec.split(',')]
RATE = 0.02; PRBW = 0.02
def q(a, p):
    a = sorted(a)
    return a[min(len(a) - 1, int(p * len(a)))] if a else float('nan')
ev = 0; evAll = 0; clG = 0.0; unG = 0.0; capHit2 = 0
ratio = {m: collections.defaultdict(list) for m in ('base', 'add', 'mul')}
wsum = {m: collections.defaultdict(float) for m in ('base', 'add', 'mul')}
unp = {'add': [0.0, 0.0], 'mul': [0.0, 0.0]}
spreadC = []; spreadP = []
for s in seeds:
    gdp = {}; pol = collections.defaultdict(dict)
    with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            gdp[(row[ix['quarter']], row[ix['nation']])] = float(row[ix['nominalGdpPerYear']])
            pol[row[ix['quarter']]][row[ix['nation']]] = float(row[ix['bondPolicyRateTermPerYear']]) / PRBW
    st = {}   # nation -> [W_add, fci_mul, fci_prev_base]
    lastq = {}
    yq = collections.defaultdict(dict)
    with open(os.path.join(d, f'divhost_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            qq = row[ix['quarter']]; nat = row[ix['nation']]; y = int(row[ix['year']])
            e = float(row[ix['fxRate']]); fci = float(row[ix['fci']]); cl = float(row[ix['divOldIndex']]); pd = float(row[ix['paidIndex']])
            if cl > 1e-9:
                evAll += 1; clG += cl * e
                u = cl - pd
                if u > 1e-9:
                    ev += 1; unG += u * e
                    if 2 * u > fci: capHit2 += 1
            # [3] 1 次の見込み
            sv = st.setdefault(nat, [0.0, None, 0.0])
            if sv[1] is None: sv[1] = fci
            else:
                prev = sv[2]
                sv[1] = sv[1] * (fci / prev) if prev > 0 else (sv[1] + fci)
            sv[2] = fci
            short = cl - pd > 1e-9
            for k, fnew in ((0, max(0.0, fci - sv[0])), (1, sv[1])):
                cn = RATE * fnew
                wo = max(0.0, cn - pd) if short else 0.0
                wo = min(wo, fnew)
                if cn > 1e-9:
                    um = max(0.0, cn - pd) if short else 0.0
                    unp['add' if k == 0 else 'mul'][0] += cn * e; unp['add' if k == 0 else 'mul'][1] += um * e
                if k == 0: sv[0] += wo
                else: sv[1] = fnew - wo
            lastq[(qq, nat)] = (y, fci, max(0.0, fci - sv[0]), sv[1], e)
            if fci > 0: yq[qq][nat] = pd / fci * 4 / PRBW
    for (qq, nat), (y, fb, fa, fm, e) in lastq.items():
        if y not in (1850, 1900, 1949): continue
        wsum['base'][(s, y)] += fb * e; wsum['add'][(s, y)] += fa * e; wsum['mul'][(s, y)] += fm * e
        g = gdp.get((qq, nat))
        if not g or g <= 0: continue
        if fb > 0: ratio['base'][y].append(fb / g)
        if fa > 0: ratio['add'][y].append(fa / g)
        if fm > 0: ratio['mul'][y].append(fm / g)
    for qq, m in yq.items():
        if len(m) >= 5:
            v = list(m.values()); spreadC.append(q(v, 0.9) - q(v, 0.1))
            pv = [pol[qq][k] for k in m if k in pol.get(qq, {})]
            if len(pv) >= 5: spreadP.append(q(pv, 0.9) - q(pv, 0.1))
print(f'[1] 旗 0 の盤面：請求のある事象 {evAll}・未払いの事象 {ev}（{ev / evAll:.4f}）・未払いの額の割合（金建て）{unG / clG:.4f}')
print(f'[2] 門 G1（落とした額÷未払い）：旗 0＝0（全 {ev} 事象で下限 0.999 を割る）・2 倍の突然変異＝2（fci で頭打ちの事象 {capHit2}・その事象は比 fci÷未払い）')
for m, lab in (('base', '旗 0（今の盤面）'), ('add', '(b) 加法の見込み'), ('mul', '(b) 乗法の見込み')):
    print(f'[3] {lab}：' + '・'.join(f'{y} 中央 {q(ratio[m][y], 0.5):.4g} p90 {q(ratio[m][y], 0.9):.4g} n {len(ratio[m][y])}' for y in (1850, 1900, 1949)))
for m in ('add', 'mul'):
    print(f'[3b] {m}：未払いの額の割合（見込み）{unp[m][1] / unp[m][0]:.4f}・世界の外資の負債の和（金建て・その年の 4 四半期の和）÷旗 0 の種ごとの比 ' + '・'.join(f"{y} 中央 {q([wsum[m][(s, y)] / wsum['base'][(s, y)] for s in seeds if wsum['base'][(s, y)] > 0], 0.5):.3f} 最小 {min([wsum[m][(s, y)] / wsum['base'][(s, y)] for s in seeds if wsum['base'][(s, y)] > 0]):.3f}" for y in (1850, 1900, 1949)))
print(f'[4] (c) 払った利回りの目盛換算の国の幅（p90−p10・四半期ごと）の中央 {q(spreadC, 0.5):.4g}（p90 {q(spreadC, 0.9):.4g}・四半期 {len(spreadC)}）／政策の目盛の項の幅の中央 {q(spreadP, 0.5):.4g}（p90 {q(spreadP, 0.9):.4g}）＝比 {q(spreadC, 0.5) / q(spreadP, 0.5) if q(spreadP, 0.5) > 0 else float("nan"):.3g}')
