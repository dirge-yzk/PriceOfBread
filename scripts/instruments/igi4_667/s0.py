# (667) 線 igi4 の段0 の読み手（読むだけ・新しい走りゼロ）。入力＝基線の計器の生（既定 D:/PoB_work/keep_655/m655_ins）。
# 使い方：PYTHONUTF8=1 python scripts/instruments/igi4_667/s0.py <計器の生のフォルダ> > measurements/igi4_667_20261030_s0.txt
# 出力：[A] 信用（返せる額の上限が縛る日の割合）／[B] 扶養力÷基準・人口÷扶養力・MeanIndustrialization の 30 年区切りと、旗 CapacityIndTermsOff の予想 ΔC/C／[C] 疫病の致死率比 r の算術の表
# 仮置き（[B]）：ΔC/C ≈ −k×meanInd÷(capacity÷capBase)・k＝0.12−0.15×都市化×(1−衛生)＝都市化 0.25・衛生 0.5 で 0.10（扶養力の式 Simulation.cs 2626〜2629 の旗 1 で外れる 2 項）。IndustrializationPopWeighted=1.0 なので avgInd は人口加重＝MeanIndustrialization と同じ重み。
import csv, statistics as st, collections, sys
D = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/keep_655/m655_ins'
def rd(name, s):
    f = open(f'{D}/{name}{s}.tsv', encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = next(r); return f, r, h
print('# igi4 (667) 段0 の読み  入力=' + D)
# [A] 信用
rows = unb_rows = 0; days = unb_days = 0.0; ratios_big = ratios_n = 0; minratio = 1e99; era = collections.Counter()
for s in range(1, 11):
    f, r, h = rd('nation_s', s)
    j = h.index('creditJudgedDaysCount'); b = h.index('creditRepayableBoundDaysCount'); rc = h.index('creditReserveCapMeanStock'); pc = h.index('creditRepayableCapMeanStock'); yi = h.index('year')
    for row in r:
        J = float(row[j]); B = float(row[b])
        if J <= 0: continue
        rows += 1; days += J; unb_days += J - B
        if J - B > 0: unb_rows += 1; era[int(row[yi]) // 20 * 20] += 1
        elif float(row[pc]) > 0:
            q = float(row[rc]) / float(row[pc]); ratios_n += 1; minratio = min(minratio, q); ratios_big += q > 10
print(f'[A] 信用  国×四半期の行={rows} 判定の日={days:.0f} 返せる額が縛らない日={unb_days:.0f} ({unb_days/days:.5f})  縛らない行={unb_rows}  20年区切り別={dict(sorted(era.items()))}')
print(f'    縛る日の行={ratios_n} 準備÷返せる額 最小={minratio:.3f} 10倍超の割合={ratios_big/ratios_n:.4f}')
# [B] 扶養力
M = collections.defaultdict(list); R = collections.defaultdict(list); I = collections.defaultdict(list)
for s in range(1, 11):
    ind = {}
    f, r, h = rd('modsig_s', s)
    for row in r: ind[(row[h.index('quarter')], row[h.index('nation')])] = float(row[h.index('meanIndustrialization')])
    f, r, h = rd('anchor_s', s)
    for row in r:
        q = row[0]; y = int(row[1])
        if int(q) % 4: continue
        cap = float(row[h.index('capacity')]); cb = float(row[h.index('capBase')]); pop = float(row[h.index('popK')])
        if cb <= 0 or cap <= 0: continue
        k = ind.get((q, row[h.index('nation')]))
        if k is None: continue
        d = y // 30 * 30; M[d].append(cap / cb); R[d].append(pop / cap); I[d].append(k)
print('[B] 扶養力  30年区切り（1770=1783〜99・国×年末×10種の中央値）')
for d in sorted(M):
    m = st.median(M[d]); i = st.median(I[d]); rr = sorted(R[d])
    print(f'    {d}  n={len(M[d])}  capacity/capBase={m:.3f}  pop/capacity 中央={st.median(R[d]):.3f} p90={rr[int(.9*len(rr))]:.3f}  meanInd={i:.3f}  予想ΔC/C={-0.10*i/m:+.3f}')
# [C] 疫病の致死率比
print('[C] 疫病の致死率比 r=(1-0.7S)/(1-0.7(0.4D+0.6S))  D=ind/(ind+1.5)（World.cs 1189〜1193・EpidemicSanitationWeight=0.6・SanitationMortalityReduction=0.7）')
for ind in (0.0, 0.75, 1.5, 1.8):
    D_ = ind / (ind + 1.5) if ind > 0 else 0.0
    print(f'    ind={ind}  ' + '  '.join(f'S={S}:{(1-0.7*S)/(1-0.7*(0.4*D_+0.6*S)):.3f}' for S in (0.0, 0.3, 0.6, 0.9)))
