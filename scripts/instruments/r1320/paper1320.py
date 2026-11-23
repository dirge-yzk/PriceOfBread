# (1320) c137sd 紙：段 3-1 の v の中 1・中 2 の行の割合（keep_1315/base_ins 種 1〜10・読むだけ・エンジンの関数を呼ばない）
# 使い方: python -X utf8 -I paper1320.py <base_ins> <出力 txt>
# 定義：subst_s の s2 の列（四半期の末）・s2BasisKind＝2 の行。sraw＝s2UEnd÷s2LneedEnd（＝ρ×U×T÷(必要量×頭数)）。
#   f̄＝fd_s の harvestMean（主食の必要量加重の収穫係数＝EconomyStep の StapleHarvestFactor の加重平均・同じ四半期・同じ州で結ぶ）。
#   ΔD÷必要量＝min(1, sraw×f̄)−min(1,sraw)×f̄（新しい D と今の sub の差・財ごとの f の違いは f̄ で近似＝限界）。
#   重みは subPopK（自給の農民の人口）。
import sys, os
from collections import defaultdict
base, out = sys.argv[1], sys.argv[2]
def fnum(x):
    try:
        v = float(x); return v if v == v else None
    except Exception: return None
W = {'all': None, '1850': 1850, '1913': 1913}
acc = defaultdict(lambda: defaultdict(float))
nofd = 0
for sd in range(1, 11):
    fd = {}
    with open(os.path.join(base, f'fd_s{sd}.tsv'), 'r', encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            fd[(c[ix['quarter']], c[ix['nation']], c[ix['region']])] = fnum(c[ix['harvestMean']])
    with open(os.path.join(base, f'subst_s{sd}.tsv'), 'r', encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            q, y, tier = c[ix['quarter']], int(c[ix['year']]), c[ix['tier']]
            sp = fnum(c[ix['subPopK']]); kind = c[ix['s2BasisKind']]
            if not sp or sp <= 0: continue
            fb = fd.get((q, c[ix['nation']], c[ix['region']]))
            sig = fnum(c[ix['subRateWeighted']])
            wins = ['all'] + ([str(y)] if (y in (1850, 1913) and int(q) % 4 == 0) else [])
            for w in wins:
                for tk in ('all', tier):
                    a = acc[(w, tk)]
                    a['rows'] += 1; a['pop'] += sp
                    if kind == '2':
                        U0, Ln0 = fnum(c[ix['s2UEnd']]), fnum(c[ix['s2LneedEnd']])
                        if U0 is not None and Ln0 is not None and Ln0 > 0:
                            a['rk2all'] += 1; a['pk2all'] += sp
                            if U0 / Ln0 > 1: a['rhiall'] += 1; a['phiall'] += sp
                            if fb is not None and U0 / Ln0 > 1: a['rhiF'] += 1; a['phiF'] += sp
                    if fb is None: a['nofd'] += 1; continue
                    a['rowsF'] += 1; a['popF'] += sp; a['need'] += sp
                    if kind != '2':
                        if sig is not None and sig * fb > 1: a['r_open_E'] += 1; a['p_open_E'] += sp
                        continue
                    U, Ln = fnum(c[ix['s2UEnd']]), fnum(c[ix['s2LneedEnd']])
                    if U is None or Ln is None or Ln <= 0: continue
                    sraw = U / Ln
                    a['r2'] += 1; a['p2'] += sp
                    dD = min(1.0, sraw * fb) - min(1.0, sraw) * fb
                    if sraw > 1 and fb < 1:
                        a['r_hi_bad'] += 1; a['p_hi_bad'] += sp; a['dD_hi_bad'] += sp * dD
                    if sraw > 1 and fb >= 1: a['r_hi_good'] += 1; a['p_hi_good'] += sp
                    if sraw <= 1 and sraw * fb > 1: a['r_lo_E'] += 1; a['p_lo_E'] += sp; a['dD_lo_E'] += sp * dD
                    if sraw > 1 and sraw * fb <= 1: a['r_hi_E0'] += 1; a['p_hi_E0'] += sp
                    a['dD_all'] += sp * dD
                    if sraw * fb > 1: a['E_all'] += sp * (sraw * fb - 1)
L = []
def P(s): L.append(s)
P('# (1320) 紙：σraw＞1 で f̄＜1 の行（v の中 1）ほか（keep_1315/base_ins 種 1〜10・s2BasisKind＝2 の行）')
P('窓\ttier\t行\tfd で結べた行\tkind2 の行\t[1] σraw>1∧f̄<1 行\t人口割合\tΔD÷必要量（その行の人口加重の平均）\t[2] σraw>1∧f̄≥1 行\t人口割合\t[3] σraw≦1∧σraw×f̄>1 行\t人口割合\t[4] σraw>1∧σraw×f̄≦1（凶作で E＝0）行\t人口割合\tΣΔD÷Σ必要量（kind2）\tΣE÷Σ必要量（kind2）\t開幕の型で σ×f̄>1 の行\t[全] kind2 の行（fd を問わない）\tσraw>1 行\t人口割合\t[結] σraw>1 行（fd と結べた行）\t人口割合（結べた kind2 の人口で割る）')
for (w, tk), a in sorted(acc.items()):
    if tk not in ('all', 'Core', 'Integrated', 'Colonial'): continue
    p2 = a['p2'] or float('nan')
    def pr(k): return f"{a['r_'+k]:.0f}\t{a['p_'+k]/p2:.4f}"
    P(f"{w}\t{tk}\t{a['rows']:.0f}\t{a['rowsF']:.0f}\t{a['r2']:.0f}\t{pr('hi_bad')}\t{(a['dD_hi_bad']/a['p_hi_bad'] if a['p_hi_bad'] else float('nan')):.4f}\t{pr('hi_good')}\t{pr('lo_E')}\t{pr('hi_E0')}\t{a['dD_all']/p2:.5f}\t{a['E_all']/p2:.5f}\t{a['r_open_E']:.0f}\t{a['rk2all']:.0f}\t{a['rhiall']:.0f}\t{(a['phiall']/a['pk2all'] if a['pk2all'] else float('nan')):.4f}\t{a['rhiF']:.0f}\t{a['phiF']/p2:.4f}")
open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))
