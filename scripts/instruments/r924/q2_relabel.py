# (924) 問い1・2：立憲→王権の再ラベル（P2 の床の下抜け・政変の印なし）の前の立憲の区間で、PC を動かした項の和と門の開き
# 使い方: python -X utf8 -I q2_relabel.py <out>
import csv, sys, math, os
csv.field_size_limit(10**8)
ROOT = 'D:/PoB_work/keep_919'
ARMS = [('base', 'base_ins', range(1, 11)), ('n1', 'n1', range(1, 11)), ('n2', 'n2', range(1, 11)), ('a11', 'a11', range(11, 21))]
NATS = ['イギリス', '清', 'ロシア', 'プロイセン', '日本', 'フランス', 'オーストリア', 'オスマン']
POL = {0: '絶対', 1: '専制', 2: '幕府', 3: '立憲', 4: '共和'}
T = ['summonTermShare', 'rollbackTermShare', 'term1Share', 'term2Share', 'term2pShare', 'residualShare']
def f(x):
    try: return float(x)
    except Exception: return math.nan
def nz(v): return 0.0 if math.isnan(v) else v
def ci(x):
    v = f(x); return -1 if math.isnan(v) else int(v)
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
agg = {}
for arm, sub, seeds in ARMS:
    for s in seeds:
        rows = {}
        with open(os.path.join(ROOT, sub, f'crownpc_s{s}.tsv'), encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                if r['nation'] in NATS: rows.setdefault(r['nation'], []).append(r)
        for n, rs in rows.items():
            a = agg.setdefault((arm, n), {'ev': 0, 'toDesp': 0, 'toAbs': 0, 'cmQ': 0, 'cmDespCrownQ': 0, 'g1s': 0, 'g1c': 0, 'g1both': 0, 'aus': 0, 'oldSummon': 0.0,
                                         **{t: 0.0 for t in T}, 'cmPc1': 0, 'cmPcMid': [], 'grantsInCm': 0, 'despEntryImposed': 0, 'despEntryRelabel': 0, 'despEntryOther': 0})
            start = None
            for i, r in enumerate(rs):
                pol = ci(r['polityCode']); prev = ci(r['polityPrevCode'])
                if pol == 3:
                    a['cmQ'] += 1
                    if ci(r['crownCode']) == 2: a['cmDespCrownQ'] += 1
                    pc = f(r['pcNowShare'])
                    if pc >= 0.999999: a['cmPc1'] += 1
                    a['cmPcMid'].append(pc)
                    if start is None or prev != 3: start = i
                    g1s = r['term1SupportGateFlag'] == '1'; g1c = r['term1CoerceGateFlag'] == '1'
                    a['g1s'] += g1s; a['g1c'] += g1c; a['g1both'] += (g1s and g1c)
                    A = f(r['austerityIndex'])
                    if A > 0.5: a['aus'] += 1; a['oldSummon'] += 0.02 * (A - 0.5) * 2.0
                    if r['grantFlag'] == '1': a['grantsInCm'] += 1
                if pol == 1 and prev != 1 and r['firstRowFlag'] != '1':
                    if r['regimeEventFlag'] == '1': a['despEntryImposed'] += 1
                    elif r['relabelFlag'] == '1' and prev == 3: a['despEntryRelabel'] += 1
                    else: a['despEntryOther'] += 1
                if prev == 3 and pol in (0, 1, 2) and r['regimeEventFlag'] != '1' and r['relabelFlag'] == '1':
                    a['ev'] += 1
                    if pol == 1: a['toDesp'] += 1
                    if pol == 0: a['toAbs'] += 1
                    j0 = start if start is not None else i
                    for k in range(j0, i + 1):
                        for t in T: a[t] += nz(f(rs[k][t]))
                if pol != 3: start = None
out.write('arm\tnation\t立憲→王権の再ラベル\t→専制\t→絶対\t立憲の四半期\tうち王冠=専制\tPC=1の割合\tPCの中央\t①の支持の門の開き\t①の保持力の門の開き\t両方\t緊縮>0.5の四半期\t旧召集の仮の和\t下賜の印\t専制の入口(押し付け/再ラベル/他)\t'
          + '\t'.join(t.replace('Share', 'Σ(再ラベル前の区間)') for t in T) + '\n')
for n in NATS:
    for arm, _, _ in ARMS:
        a = agg.get((arm, n))
        if not a: continue
        m = sorted(a['cmPcMid']); med = m[len(m) // 2] if m else math.nan
        cq = max(1, a['cmQ'])
        out.write(f"{arm}\t{n}\t{a['ev']}\t{a['toDesp']}\t{a['toAbs']}\t{a['cmQ']}\t{a['cmDespCrownQ']}\t{a['cmPc1'] / cq:.3f}\t{med:.3f}\t{a['g1s'] / cq:.3f}\t{a['g1c'] / cq:.3f}\t{a['g1both'] / cq:.3f}\t{a['aus']}\t{a['oldSummon']:.2f}\t{a['grantsInCm']}\t"
                  f"{a['despEntryImposed']}/{a['despEntryRelabel']}/{a['despEntryOther']}\t" + '\t'.join(f'{a[t]:.2f}' for t in T) + '\n')
out.close()
