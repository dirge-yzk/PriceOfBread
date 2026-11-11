# (1055) c100mx 紙の台本（読むだけ・腕の前）：今の盤面（keep_1053）の merch_s から、F1（商人の所得の出口）の門 W1〜W4 の見込みを数える。
# 式（事前登録の形）：attract＝労働者の所得÷(商人の稼ぎ×PromotionPremium)・流量＝率×商人の人数×min(attract−1,1)（attract＞1 のときだけ・率 0.01）。
# 限界：merch_s の前の 21 列は社会移動の前の値＝枝の位置の人数・所得とは違う（近似）。W3 の新は〈商人の稼ぎの総額が人数に依らない〉の楽観の上界の模型。
# 使い方：python -X utf8 -I paper_mx.py <生のフォルダ> <種 例 1-10> <出力>
import sys, os
from collections import defaultdict
d = sys.argv[1]; lo, hi = (int(x) for x in sys.argv[2].split('-')); outp = sys.argv[3]
RATE, EPS = 0.01, 1e-9
def rd(path):
    with open(path, 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f: yield ix, ln.decode('utf-8').rstrip('\r\n').split('\t')
def fl(v):
    try: return float(v)
    except ValueError: return float('nan')
K = lambda t: 'Colonial' if t == 'Colonial' else 'Home'
C = defaultdict(float); runsOld = defaultdict(list); runsNew = defaultdict(list); aHist = defaultdict(lambda: defaultdict(int))
for s in range(lo, hi + 1):
    ser = defaultdict(list)
    for ix, x in rd(os.path.join(d, f'merch_s{s}.tsv')):
        ser[(x[ix['nation']], x[ix['region']])].append((int(x[ix['quarter']]), K(x[ix['tier']]), fl(x[ix['merchantK']]), fl(x[ix['merchEarnedPerCap']]),
                     fl(x[ix['workerIncomePerCap']]), fl(x[ix['promotionPremium']]), fl(x[ix['merchPettyIn']])))
    for key, L in ser.items():
        L.sort()
        entryQ = [q for (q, k, mk, me, wi, pr, pin) in L if pin == pin and pin > EPS]
        eset = set(entryQ)
        runO = runN = 0; prevQ = None; r = 1.0; cutN = False
        def close(k):
            global runO, runN
            if runO: runsOld[k].append(runO)
            if runN: runsNew[k].append(runN)
        for (q, k, mk, me, wi, pr, pin) in L:
            cont = prevQ is not None and q == prevQ + 1
            prevQ = q
            if not (mk > EPS) or me != me or wi != wi or pr != pr:
                close(k); runO = runN = 0; r = 1.0; cutN = False; continue
            C[(k, 'live')] += 1
            a = wi / max(EPS, max(0.0, me) * pr)
            flow = min(RATE * mk * min(a - 1.0, 1.0), mk) if a > 1.0 else 0.0
            if a > 1.0 and not (flow > EPS): C[(k, 'excl')] += 1
            if not (a > 1.0):
                close(k); runO = runN = 0; r = 1.0; cutN = False; continue
            if not cont:
                close(k); runO = runN = 0; r = 1.0; cutN = False
            if flow > EPS:
                C[(k, 'pop')] += 1
                b = '>2' if a > 2 else ('1.22-2' if a > 1.22 else ('1+1e-5..1.22' if a - 1 > 1e-5 else '<=1+1e-5'))
                aHist[k][b] += 1
                if any((q + dq) in eset for dq in range(-3, 4)): C[(k, 'w4')] += 1
            runO += 1
            # 楽観の模型：商人の稼ぎの総額は人数に依らない＝1 人あたりの稼ぎは 1/r 倍＝attract は r 倍
            an = a * r
            if an > 1.0 and not cutN:
                runN += 1; r *= 1.0 - RATE * min(an - 1.0, 1.0)
            else:
                if not cutN and runN: runsNew[k].append(runN); runN = 0
                cutN = True
        close(k)
out = open(outp, 'w', encoding='utf-8', newline='\n')
def P(*a): print(*a, file=out)
P(f'紙 paper_mx・生 {d} 種 {lo}〜{hi}')
for k in ('Colonial', 'Home'):
    pop = C[(k, 'pop')]
    P(f'[{k}] 空でない行 {C[(k, "live")]:.0f}・母集合（式の流量＞1e-9）{pop:.0f}・除外（attract＞1 かつ流量≦1e-9）{C[(k, "excl")]:.0f}')
    P(f'  attract の分布 ' + '・'.join(f'{b} {aHist[k][b]}' for b in ('<=1+1e-5', '1+1e-5..1.22', '1.22-2', '>2')))
    P(f'  W4 の見込み（出口の行のうち ±3 四半期に入口 merchPettyIn＞1e-9）{C[(k, "w4")]:.0f}／{pop:.0f}＝{C[(k, "w4")] / max(1, pop):.4f}')
    for nm, R in (('旧', runsOld[k]), ('新（楽観の模型）', runsNew[k])):
        tot = sum(R); big = sum(x for x in R if x >= 20)
        P(f'  {nm}：≧20 の続きに入る行の割合 {big / max(1, tot):.4f}（行 {tot}・続き {len(R)}）')
out.close()
