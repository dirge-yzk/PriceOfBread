# (1416) c151fl 旗 RepayFloorOnly の門と記録の台本（生を読むだけ・列名で引く）。
# 使い方: python fl_gate.py <計器の生のフォルダ> [種の範囲 1-10] [出力 tsv]
# 門（fsx_s の q 行・repayGateMonths>0 の国×四半期）:
#   G1 分子＝excessWouldSum>repayFloorSum の行（旧の規則なら床より多く返した月を含む行）のうち |repayAboveFloorSum|<=tol の割合。各の種で 1・行は 1 以上。
#   G2 母集合の外＝excessWouldSum<=repayFloorSum の行のうち |repayAboveFloorSum|<=tol の割合。各の種で 1（外へ回った割合＝0）。
#   G3 上限＝repayDoneSum<=repayFloorSum+tol の行の割合（床より多く返した行が 0）。各の種で 1。
# 記録: 国庫÷豊かさの線が 2 倍を超えた国×四半期の割合・線の上の国×四半期の数（RelieveOpulence が税を下げうる四半期の近似）・英の国債の型（uk_debt.py と同じ定義）・英の不履行の種。
import sys, os
D = sys.argv[1]
a, b = (sys.argv[2] if len(sys.argv) > 2 else '1-10').split('-')
SEEDS = range(int(a), int(b) + 1)
OUT = sys.argv[3] if len(sys.argv) > 3 else None
ZERO = 1.0
def tab(p, filt=None):
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for l in f:
        r = l.rstrip('\n').split('\t')
        if filt is None or filt(ix, r): yield ix, r
rows = []
allok = {'G1': True, 'G2': True, 'G3': True}
ledger_only = False
for s in SEEDS:
    p = f'{D}/fsx_s{s}.tsv'
    if not os.path.exists(p):
        # (1427) c153fb：旗を焼いた後の木は fsx_s が無くても読む＝台帳 treasledger の DebtRepay（返済の額・符号は出る側が負）だけで数える（G1〜G3 は fsx_s が要る＝飛ばす）
        tp = f'{D}/treasledger_s{s}.tsv'
        if not os.path.exists(tp): print('欠け', p, tp); allok = {k: False for k in allok}; continue
        qs = set(); tot = 0.0
        for ix, r in tab(tp, lambda ix, r: r[ix['item']] == 'DebtRepay'):
            v = float(r[ix['value']])
            if v != 0: qs.add((r[ix['quarter']], r[ix['nation']])); tot += -v
        ledger_only = True   # (1434) fsx_s が無い種がある＝G1〜G3 は読めていない（〈通る〉と出さない）
        print('台帳のみ 種', s, '返済のある国×四半期', len(qs), '返済額の合計', f'{tot:.6f}')
        continue
    n1 = k1 = n2 = k2 = n3 = k3 = 0; over2 = lineq = above = 0
    for ix, r in tab(p, lambda ix, r: r[ix['time']].startswith('q')):
        lineE = float(r[ix['lineEnd']]); treE = float(r[ix['treasuryEnd']])
        if lineE > 0:
            lineq += 1
            if treE > lineE: above += 1
            if treE > 2 * lineE: over2 += 1
        if int(r[ix['repayGateMonths']]) <= 0: continue
        fl = float(r[ix['repayFloorSum']]); dn = float(r[ix['repayDoneSum']]); ab = float(r[ix['repayAboveFloorSum']]); ex = float(r[ix['excessWouldSum']])
        tol = 1e-9 * max(1.0, abs(fl))
        exact = abs(ab) <= tol
        if ex > fl: n1 += 1; k1 += exact
        else: n2 += 1; k2 += exact
        n3 += 1; k3 += dn <= fl + tol
    # 英の国債の型（fiscal_s）
    first0 = None; defy = []; last = 0
    fp = f'{D}/fiscal_s{s}.tsv'
    if os.path.exists(fp):
        for ix, r in tab(fp, lambda ix, r: r[ix['nation']] == 'イギリス'):
            y = int(r[ix['year']]); g = float(r[ix['govDebtStock']]); dc = float(r[ix['defaultCount']])
            if dc > last: defy.append(y); last = dc
            if g < ZERO and first0 is None: first0 = y
    t = '-' if first0 is None else ('D' if any(first0 - 5 <= y <= first0 for y in defy) else 'R')
    g1 = k1 / n1 if n1 else float('nan'); g2 = k2 / n2 if n2 else float('nan'); g3 = k3 / n3 if n3 else float('nan')
    allok['G1'] &= n1 >= 1 and k1 == n1; allok['G2'] &= k2 == n2; allok['G3'] &= n3 >= 1 and k3 == n3
    rows.append((s, n1, k1, f'{g1:.6f}', n2, k2, f'{g2:.6f}', n3, k3, f'{g3:.6f}', lineq, above, over2, f'{over2 / lineq if lineq else 0:.6f}', t, first0, len(defy), ','.join(map(str, defy))))
H = 'seed\tg1Rows\tg1Exact\tg1Share\tg2Rows\tg2Exact\tg2Share\tg3Rows\tg3NotAbove\tg3Share\tlineQuarters\taboveLineQuarters\tover2xQuarters\tover2xShare\tukType\tukFirstZeroYear\tukDefaults\tukDefaultYears'
lines = [H] + ['\t'.join(str(x) for x in r) for r in rows]
if OUT:
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(lines) + '\n')
for l in lines: print(l)
if False: print('G1 判定なし  G2 判定なし  G3 判定なし（fsx_s が無い種がある＝台帳の DebtRepay だけでは読めない）')
else: print('G1', '通る' if allok['G1'] else '落ちる', ' G2', '通る' if allok['G2'] else '落ちる', ' G3', '通る' if allok['G3'] else '落ちる')
print('英の型 R', sum(r[14] == 'R' for r in rows), '/', len(rows), ' D', sum(r[14] == 'D' for r in rows), ' 不履行の種', [r[0] for r in rows if r[16] > 0])
print('線の上の国×四半期 計', sum(r[11] for r in rows), ' 2 倍超 計', sum(r[12] for r in rows), '/', sum(r[10] for r in rows))
