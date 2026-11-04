# (816) c14sv 読むだけ：silverwt_s と metal_s から、注入ごとの〈票に入った州の数の増え〉〈票の和の跳ね〉〈rel の落ち〉を並べる。
# 入る州の局所の比は、同じ盤面の前の注入との差から推す（続いて居る州の比の変化を 0 と置く近似＝近似と名乗る）。
import sys, math, csv
F1 = 'D:/PoB_work/keep_813/rej/c12svB_10/ins'
F0 = 'D:/PoB_work/keep_813/base_ins'
def rd(p):
    with open(p, 'rb') as f:
        t = f.read().decode('utf-8').splitlines()
    h = t[0].split('\t')
    return [dict(zip(h, l.split('\t'))) for l in t[1:]]
def fl(x):
    try: return float(x)
    except: return float('nan')
out = []
out.append('board\tseed\tseq\tyear\tN\tdN\tW\tdW\tmetalGoldDemand\texcludedDemand\trelEq\trelW1\tusedRel\tw1MaxShare\tlnNewMeanEqEst\tlnNewMeanW1Est\tnewWeightShareEst\tclampHit')
for board, F in (('flag1', F1), ('flag0', F0)):
    for s in range(1, 11):
        sw = rd(f'{F}/silverwt_s{s}.tsv')
        met = {int(r['quarter']): r for r in rd(f'{F}/metal_s{s}.tsv')}
        # 本走の注入 seq と四半期の対応：frozen 0 の最初の行が quarter 0
        first = min(int(r['seq']) for r in sw if r['frozen'] == '0')
        prev = None
        for r in sw:
            seq = int(r['seq']); N = int(r['stateCount']); W = fl(r['usedWeightSum'])
            eq = fl(r['relEqualIndex']); w1 = fl(r['relW1Index'])
            q = seq - first if r['frozen'] == '0' else None
            mg = fl(met[q]['goldDemand']) if q is not None and q in met else float('nan')
            ex = mg - W if board == 'flag1' else float('nan')
            dN = N - prev['N'] if prev else 0
            if prev and dN > 0:
                lnNewEq = (N * math.log(eq) - prev['N'] * math.log(prev['eq'])) / dN
            else: lnNewEq = float('nan')
            if prev and dN > 0 and board == 'flag1':
                dW = W - prev['W']
                lnNewW = (W * math.log(w1) - prev['W'] * math.log(prev['w1'])) / dW if dW > 1e-9 else float('nan')
                shr = dW / W
            else:
                dW = W - prev['W'] if prev else 0; lnNewW = float('nan'); shr = float('nan')
            out.append('\t'.join(str(v) for v in (board, s, seq, r['year'], N, dN, f'{W:.1f}', f'{dW:.1f}', f'{mg:.1f}', f'{ex:.1f}', f'{eq:.4f}', f'{w1:.4f}', r['usedRelIndex'][:7], r['w1MaxShare'][:6], f'{lnNewEq:.3f}', f'{lnNewW:.3f}', f'{shr:.3f}', r['clampHitCount'])))
            prev = {'N': N, 'W': W, 'eq': eq, 'w1': w1}
with open('D:/PoB_work/r816_work/out/a1_entries.tsv', 'wb') as f:
    f.write(('\n'.join(out) + '\n').encode('utf-8'))
print(len(out))
