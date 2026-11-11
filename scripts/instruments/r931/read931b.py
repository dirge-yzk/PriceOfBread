# (931) 読むだけ：0 の続きの入り口（前の 12 四半期）と出口（再点火の前後）の州のお金の出入りを cearn_s で読む。
#   区間の定義は read931a.py と同じ（region_s の工場＋採取の産出＝0・人口 100 千人以上・4 四半期以上）。
#   使い方: python -X utf8 -I read931b.py <ins フォルダ> <種の起点> <名札>
import sys, os, statistics as st, collections as C
M = sys.argv[1]; SB = int(sys.argv[2]); TAG = sys.argv[3]
POP = 100.0; K = 4
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
INS = ['inMarketPerQuarter', 'inExportPerQuarter', 'inStatePerQuarter', 'inCreditPerQuarter', 'inFromPopsPerQuarter', 'inMigrationPerQuarter', 'inOtherPerQuarter']
OUTS = ['outMarketPerQuarter', 'outTaxPerQuarter', 'outBondPerQuarter', 'outDuesPerQuarter', 'outColonialRentPerQuarter', 'outCapitalPerQuarter', 'outImportPerQuarter', 'outMigrationPerQuarter', 'outOtherPerQuarter']
traj = C.defaultdict(list)          # 相対の四半期 -> 1 人あたり貯蓄（入り口 q0 を 0）
pre_sum = C.Counter(); pre_in = C.Counter(); route = C.Counter(); pre_n = 0
ign_in = C.Counter(); ign_nz = C.Counter(); ign_n = 0; ign_sav = []; ign_sav_prev = []
run_in = C.Counter(); run_out = C.Counter(); run_q = 0
for s in range(SB, SB + 10):
    p = os.path.join(M, f'region_s{s}.tsv')
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    d = C.defaultdict(dict)
    for l in f:
        r = l.rstrip('\n').split('\t')
        if len(r) < len(h): continue
        d[r[ix['region']]][int(r[ix['quarter']])] = (fl(r[ix['factoryOutputRefValuePerMonth']]) + fl(r[ix['extractionOutputRefValuePerMonth']]), fl(r[ix['populationK']]))
    f.close()
    runs = []
    for reg, dd in d.items():
        run = []
        for q in sorted(dd):
            v = dd[q]
            if v[0] == 0.0 and v[1] >= POP:
                if run and run[-1] == q - 1: run.append(q)
                else:
                    if len(run) >= K: runs.append((reg, run[0], run[-1]))
                    run = [q]
            else:
                if len(run) >= K: runs.append((reg, run[0], run[-1]))
                run = []
        if len(run) >= K: runs.append((reg, run[0], run[-1]))
    del d
    want = C.defaultdict(list)   # (region, q) -> [(役割, run)]
    for reg, q0, q1 in runs:
        for k in range(-12, 3): want[(reg, q0 + k)].append(('t', q0, k))
        want[(reg, q1 + 1)].append(('ign', q0, 0)); want[(reg, q1)].append(('ignprev', q0, 0))
        for q in range(q0, q1 + 1): want[(reg, q)].append(('run', q0, 0))
    p = os.path.join(M, f'cearn_s{s}.tsv')
    if not os.path.exists(p): print('欠け', p); continue
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    for l in f:
        r = l.rstrip('\n').split('\t')
        if len(r) < len(h): continue
        k = (r[ix['region']], int(r[ix['quarter']]))
        if k not in want: continue
        pop = fl(r[ix['popK']]); sav = fl(r[ix['savings']])
        for role, q0, rel in want[k]:
            if role == 't':
                traj[rel].append(sav / pop if pop > 0 else float('nan'))
                if rel < 0:
                    pre_n += 1
                    for c in OUTS: pre_sum[c] += fl(r[ix[c]])
                    for c in INS: pre_in[c] += fl(r[ix[c]])
                    route[r[ix['outTop1Route']]] += fl(r[ix['outTop1PerQuarter']])
            elif role == 'ign':
                ign_n += 1; ign_sav.append(sav / pop if pop > 0 else float('nan'))
                for c in INS:
                    v = fl(r[ix[c]]); ign_in[c] += v
                    if v > 0: ign_nz[c] += 1
            elif role == 'ignprev':
                ign_sav_prev.append(sav / pop if pop > 0 else float('nan'))
            else:
                run_q += 1
                for c in INS: run_in[c] += fl(r[ix[c]])
                for c in OUTS: run_out[c] += fl(r[ix[c]])
    f.close()
o = [f'== {TAG}  {M}']
o.append('入り口の前後の 1 人あたり貯蓄（入り口＝0・中央）: ' + ' '.join(f'{k}:{med(traj[k]):.3g}' for k in sorted(traj)))
o.append(f'入り口の前 12 四半期の流出の和（{pre_n} 州×四半期・比率）:')
tot = sum(pre_sum.values()) or 1
for c, v in pre_sum.most_common(): o.append(f'  {c}\t{v:.6g}\t{v / tot:.3f}')
tin = sum(pre_in.values()) or 1
o.append('同じ期間の流入の和:')
for c, v in pre_in.most_common(): o.append(f'  {c}\t{v:.6g}\t{v / tin:.3f}')
o.append(f'流出 − 流入（前 12 四半期の和）: {sum(pre_sum.values()) - sum(pre_in.values()):.6g}')
o.append('前 12 四半期の最大の流出の経路（額の和・上位 10）:')
for k, v in route.most_common(10): o.append(f'  {k}\t{v:.6g}')
o.append(f'続きの中（{run_q} 州×四半期）の流入の和: ' + ' '.join(f'{c}:{v:.4g}' for c, v in run_in.most_common()))
o.append('続きの中の流出の和: ' + ' '.join(f'{c}:{v:.4g}' for c, v in run_out.most_common()))
o.append(f'再点火の四半期（{ign_n}）の流入の和: ' + ' '.join(f'{c}:{v:.4g}' for c, v in ign_in.most_common()))
o.append('再点火の四半期に正の流入があった件数: ' + ' '.join(f'{c}:{v}' for c, v in ign_nz.most_common()))
o.append(f'再点火の直前の四半期の 1 人あたり貯蓄の中央 {med(ign_sav_prev):.3g}・再点火の四半期 {med(ign_sav):.3g}')
open(os.path.join(os.path.dirname(os.path.abspath(__file__)), f'read931b_{TAG}_out.txt'), 'w', encoding='utf-8').write('\n'.join(o) + '\n')
print('\n'.join(o[:3]))
