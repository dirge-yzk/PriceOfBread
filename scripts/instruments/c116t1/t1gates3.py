# (1172) c116t1 の門の台本（事前登録 measurements/m1172_20261116_prereg_t1.txt [1][2]・列名で読む＝★(749)）
# 使い方: python -X utf8 -I t1gates3.py <ins フォルダ> <種の起点> <種の終わり> [floorSum floorN]
#   床を渡さなければ数えるだけ（旗 0 の追記の材料）。渡せば G1・G2・G3 の判定を出す。
import sys, os, csv, math
ins, sb, se = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
floors = (float(sys.argv[4]), float(sys.argv[5])) if len(sys.argv) >= 6 else None
csv.field_size_limit(1 << 30)
YEARS = (1830, 1870, 1913)
NEW = ['rowsDealt', 'rowsDealtX', 'retreatRowN', 'retreatRowSum', 'retreatRowInsSum', 'retreatRowInsN', 'excessQtyX', 'excessQtyAll',
       'errNegQ', 'errOverIns', 'errOverBound', 'excessQtyInsX', 'excessQtyInsPopA']
OLD = ['rows', 'dealsX', 'qtyX', 'u1Deals', 'u1Viol', 'gateMismatch', 'selfN', 'selfDiff', 'uBelowMin', 'popA', 'popADealX', 'popAQtyX', 'retreatQtySum', 'retreatDealsTol']
tot = {k: 0.0 for k in NEW + OLD}
yr = {y: {k: 0.0 for k in ('qtyX', 'excessQtyX')} for y in YEARS}
seeds = 0; nrows = 0
for s in range(sb, se + 1):
    p = os.path.join(ins, f't1buy_s{s}.tsv')
    if not os.path.exists(p):
        print(f'欠け: {p}'); continue
    seeds += 1
    with open(p, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        miss = [k for k in NEW + OLD if k not in ix]
        if miss:
            print(f'列が無い {p}: {miss}'); sys.exit(2)
        for r in rd:
            if len(r) < len(h): continue
            nrows += 1
            for k in NEW + OLD:
                v = r[ix[k]]
                if v != 'nan': tot[k] += float(v)
            y = int(r[ix['year']])
            if y in yr:
                for k in ('qtyX', 'excessQtyX'): yr[y][k] += float(r[ix[k]])
def rho(q, e): return q / (q - e) if q - e > 0 else float('nan')
print(f'種 {sb}..{se}（そろった {seeds}）・行 {nrows}')
for k in NEW + OLD: print(f'  {k}\t{tot[k]:.10g}')
rx = rho(tot['qtyX'], tot['excessQtyX'])
rins = (tot['qtyX'] + tot['excessQtyInsX'] + tot['excessQtyInsPopA']) / tot['qtyX'] if tot['qtyX'] > 0 else float('nan')
rinsDealt = (tot['qtyX'] + tot['excessQtyInsX']) / tot['qtyX'] if tot['qtyX'] > 0 else float('nan')
print(f'rho_X（全期間）\t{rx:.6f}')
for y in YEARS: print(f'rho_X@{y}\t{rho(yr[y]["qtyX"], yr[y]["excessQtyX"]):.6f}\t（記録）')
print(f'rho_ins（旗 0 の盤面の上側・門にしない）\t{rins:.6f}\t（成約のある行だけ {rinsDealt:.6f}）')
print(f'dealsX − rowsDealtX\t{tot["dealsX"] - tot["rowsDealtX"]:.0f}')
print(f'床の材料：floor(retreatRowInsSum÷4)\t{math.floor(tot["retreatRowInsSum"] / 4)}\tfloor(retreatRowInsN÷4)\t{math.floor(tot["retreatRowInsN"] / 4)}')
if floors is not None:
    fs, fn = floors
    g1 = tot['retreatRowSum'] >= fs and tot['retreatRowN'] >= fn and fs > 0 and fn > 0
    errs = {k: tot[k] for k in ('errNegQ', 'errOverIns', 'errOverBound', 'uBelowMin', 'u1Viol', 'gateMismatch', 'selfDiff')}
    g2 = all(v == 0 for v in errs.values())
    g3 = rx >= 1.01
    print(f'G1 働き：retreatRowSum {tot["retreatRowSum"]:.6g} ≧ {fs:.0f} かつ retreatRowN {tot["retreatRowN"]:.0f} ≧ {fn:.0f} → {"緑" if g1 else "赤"}')
    print(f'G2 誤り：{errs} → {"緑" if g2 else "赤"}')
    print(f'G3 移った量：rho_X {rx:.6f} ≧ 1.01 → {"緑" if g3 else "赤"}')
    print('判定:', '門は全て緑' if g1 and g2 and g3 else '門の赤あり')
