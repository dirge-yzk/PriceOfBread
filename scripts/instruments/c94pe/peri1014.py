# (1014) c94pe 周辺の点検（読むだけ）：植民地格で商人が空の行の merchantDebt・錠 B の比（労働者の 1 人あたり貯蓄÷州の平均）・錠 A の行の商人の空の稼ぎの大きさ。
# 使い方：python peri1014.py <生のフォルダ>
import os, sys
d = sys.argv[1]
debt_pos = debt_n = 0; debt_max = 0.0; ratios = []; earn = []; mmeritoff = 0
for s in range(1, 11):
    with open(os.path.join(d, f'merch_s{s}.tsv'), 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            x = ln.decode('utf-8').rstrip('\r\n').split('\t')
            if x[ix['tier']] != 'Colonial' or x[ix['merchantEmpty']] != '1': continue
            debt_n += 1; dv = float(x[ix['merchantDebt']])
            if dv > 0: debt_pos += 1; debt_max = max(debt_max, dv)
            if x[ix['meritActive']] != '1': mmeritoff += 1; continue
            a = float(x[ix['avgPerCap']]); w = float(x[ix['workerSavPerCap']])
            if a > 1e-9 and w == w: ratios.append(w / a)
            if x[ix['lockABlockedMerchant']] == '1': earn.append(float(x[ix['emptyMerchantEarnedPerMonth']]) / max(1e-9, float(x[ix['workerIncomePerCap']]) * float(x[ix['promotionPremium']])))
q = lambda a, p: sorted(a)[int(p * (len(a) - 1))] if a else float('nan')
print(f"植民地格・商人が空 {debt_n} 行：merchantDebt>0 {debt_pos}（最大 {debt_max:.4g}）・meritActive=0 {mmeritoff}")
print(f"錠 B の比（労働者の 1 人あたり貯蓄÷州の平均）：行 {len(ratios)}・中央 {q(ratios,.5):.3f}・25% {q(ratios,.25):.3f}・75% {q(ratios,.75):.3f}・≥1 の割合 {sum(r>=1 for r in ratios)/max(1,len(ratios)):.4f}")
print(f"錠 A の行の 空の稼ぎ÷(労働者の 1 人あたり所得×割増)：行 {len(earn)}・最小 {q(earn,0):.4g}・1% {q(earn,.01):.4g}・中央 {q(earn,.5):.4g}（エンジンの attract はこの値÷1e-9＝Size 0 の分母の床）")
