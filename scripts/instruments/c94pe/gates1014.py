# (1014) c94pe 門の台本（読むだけ・事前登録 measurements/m1014_20261110_prereg_pe.txt [5] の門をそのまま書いた物）。
# 使い方：python gates1014.py <腕の生（merch_s）> <基準の生（keep_1011/base_ins）> [種の起点=1] [種の数=10]
#   基準の生に merch_s が無いとき（種 11〜20）は、腕の値だけを記録として出し、門は読まない。
# 門（腕の前に固定）：G1 新÷旧≦0.50・G1′≧0.80・G2 種の中央≦40・G3≦0.4258（基準の種の上端 0.2839×1.5）。
# 記録：④ の両向き（i）（ii）・職人の空（植民地格）・本国格と統合格の錠 A（商人）・植民地格の商人の頭数の和（1850・1913 の年末）・新しく空でなくなった行の merchantDebt>0。
import os, sys
from collections import defaultdict
def read(d, sb, ns):
    per = []
    for s in range(sb, sb + ns):
        p = os.path.join(d, f'merch_s{s}.tsv')
        if not os.path.exists(p): return None
        P = defaultdict(float); ser = defaultdict(list)
        with open(p, 'rb') as f:
            h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for ln in f:
                x = ln.decode('utf-8').rstrip('\r\n').split('\t')
                q = int(x[0]); tier = x[ix['tier']]; reg = x[ix['region']]; yr = int(x[ix['year']])
                me = x[ix['merchantEmpty']] == '1'; la = x[ix['lockABlockedMerchant']] == '1'
                mk = float(x[ix['merchantK']])
                ser[reg].append((q, tier, me, la, mk))
                if la and tier != 'Colonial': P['lockA_nonCol'] += 1
                if tier == 'Colonial':
                    P['colRows'] += 1; P['colEmpty'] += me; P['colLockA'] += la
                    P['artEmpty'] += x[ix['artisanEmpty']] == '1'
                    if q % 4 == 3 and yr in (1851, 1914): P[f'mk{yr}'] += mk
                    if not me:
                        P['colNonEmpty'] += 1
                        ep = float(x[ix['merchEarnedPerCap']]); wi = float(x[ix['workerIncomePerCap']]); pr = float(x[ix['promotionPremium']])
                        if wi == wi:
                            P['g3den'] += 1
                            if ep < wi * pr: P['g3num'] += 1
                        else: P['g3_wi_nan'] += 1
                        if float(x[ix['merchantDebt']]) > 0: P['nonEmptyDebtPos'] += 1
        beng = 0
        for reg, L in ser.items():
            L.sort()
            for i, (q, tier, me, la, mk) in enumerate(L):
                if reg == '中核ベンガル' and me: beng += 1
                if i + 1 < len(L) and L[i + 1][0] == q + 1:
                    nk = L[i + 1][4]
                    if la:
                        P['g1p_den'] += 1; P['g1p_num'] += nk > 1e-9
                    elif me and nk > 1e-9: P['ii'] += 1
        P['beng'] = beng
        per.append((s, P))
    return per
def summ(per):
    T = defaultdict(float)
    for _, P in per:
        for k, v in P.items(): T[k] += v
    b = sorted(P['beng'] for _, P in per); med = (b[len(b)//2] + b[(len(b)-1)//2]) / 2
    return dict(g1=T['colEmpty']/max(1,T['colRows']), g1p=T['g1p_num']/max(1,T['g1p_den']), g1p_den=T['g1p_den'], i=T['g1p_den']-T['g1p_num'], ii=T['ii'],
                g2=med, g2s=b, g3=T['g3num']/max(1,T['g3den']), g3nan=T['g3_wi_nan'], art=T['artEmpty']/max(1,T['colRows']),
                lockAnon=T['lockA_nonCol'], colLockA=T['colLockA'], mk1851=T['mk1851'], mk1914=T['mk1914'], debt=T['nonEmptyDebtPos']/max(1,T['colNonEmpty']), rows=T['colRows'])
arm, base = sys.argv[1], sys.argv[2]; sb = int(sys.argv[3]) if len(sys.argv) > 3 else 1; ns = int(sys.argv[4]) if len(sys.argv) > 4 else 10
A = read(arm, sb, ns); B = read(base, sb, ns)
a = summ(A)
print(f"腕 {arm}（種 {sb}〜{sb+ns-1}）")
for s, P in A: print(f"  s{s}: G1 {P['colEmpty']/max(1,P['colRows']):.4f} G1′ {P['g1p_num']}/{P['g1p_den']} G2 {P['beng']} G3 {P['g3num']/max(1,P['g3den']):.4f}")
if B is None:
    print("基準に merch_s が無い＝門は読まない（記録だけ）"); print(a); sys.exit(0)
b = summ(B)
def row(name, ka, fmt='{:.4f}'):
    x, y = a[ka], b[ka]; r = x / y if y else float('nan')
    print(f"  {name}: 旧 {fmt.format(y)} 新 {fmt.format(x)} 新÷旧 {r:.4f}")
print("同じ盤面の新÷旧（★(919) 全ての列）")
for nm, k in [('G1 植民地格の商人が空の割合', 'g1'), ('G1′ 錠 A の行の翌四半期に商人>0', 'g1p'), ('G2 中核ベンガルの空の四半期（種の中央）', 'g2'), ('G3 入り過ぎの割合', 'g3'),
              ('記録 職人が空の割合（植民地格）', 'art'), ('記録 錠 A の行（商人・植民地格）', 'colLockA'), ('記録 錠 A の行（商人・植民地格の外）', 'lockAnon'),
              ('記録 ④(i) 錠 A の行で翌四半期も空', 'i'), ('記録 ④(ii) 錠 A でない空の行で翌四半期に商人>0', 'ii'),
              ('記録 植民地格の商人の頭数の和（千人・ラベル 1851 の年末）', 'mk1851'), ('記録 同（ラベル 1914 の年末）', 'mk1914'),
              ('記録 空でない植民地格の行で merchantDebt>0 の割合', 'debt'), ('記録 G3 の除外（労働者の所得 nan）', 'g3nan')]:
    row(nm, k)
print(f"  G2 種：旧 {b['g2s']} 新 {a['g2s']}")
g1r = a['g1'] / b['g1']
gates = [('G1 新÷旧≦0.50', g1r <= 0.50, f'{g1r:.4f}'), ('G1′≧0.80', a['g1p'] >= 0.80, f"{a['g1p']:.4f}（{a['g1p_den']:.0f} 行）"),
         ('G2 種の中央≦40', a['g2'] <= 40, f"{a['g2']}"), ('G3≦0.4258', a['g3'] <= 0.4258, f"{a['g3']:.4f}")]
for n, ok, v in gates: print(f"門 {n}: {v} → {'通る' if ok else '落ちる'}")
print("門の総合:", '全て通る' if all(g[1] for g in gates) else '落ちた門あり')
