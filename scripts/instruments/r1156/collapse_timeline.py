# (1156) 雇用の崩れの時点と持ち主の替わり（読むだけ）
# 使い方: python -X utf8 -I collapse_timeline.py <出力フォルダ>
import sys, os
OUT = sys.argv[1]
ARM = 'D:/PoB_work/keep_1150_rej/c112t1_arms/out_c112t1{}_10/ins'
DIRS = {'Aa': ARM.format('Aa'), 'Ab': ARM.format('Ab'), 'B': ARM.format('B'), 'base': 'D:/PoB_work/keep_1150/base_ins',
        'n1': 'D:/PoB_work/keep_1150/n1', 'n2': 'D:/PoB_work/keep_1150/n2'}
REG = ['ペルー', 'ヌエバ・エスパーニャ銀地帯']
def d(a):
    p = DIRS[a]; return p if os.path.exists(os.path.join(p, 'region_s1.tsv')) else os.path.join(p, 'ins')
def rows(a, s):
    out = {}
    with open(os.path.join(d(a), f'region_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if c[ix['region']] not in REG: continue
            k = (c[ix['region']], int(c[ix['quarter']]))
            if k in out: continue
            out[k] = (int(c[ix['year']]), c[ix['nation']], c[ix['tier']], float(c[ix['employmentShare']]), float(c[ix['populationK']]),
                      float(c[ix['extractionOutputRefValuePerMonth']]), float(c[ix['migrationOutKPerQuarter']]) + float(c[ix['crossBorderOutKPerQuarter']]),
                      float(c[ix['extractionActiveCount']]), float(c[ix['extractionBankruptCount']]))
    return out
L = ['# 州×種×腕：持ち主の替わりの四半期・雇用が初めて 0.3 未満の四半期・その前後の列（年・持ち主・格・雇用・人口・採取の産出の参照値・出て行く人）']
L.append('arm\tseed\tregion\townerChanges(q:year:to)\tfirstEmpLt03(q:year:owner)\tempLt03Q\tempLt03Q_underSpain\tempLt03Q_underOther')
summ = {}
for a in DIRS:
    for s in range(1, 11):
        R = rows(a, s)
        for r in REG:
            qs = sorted(q for (rr, q) in R if rr == r)
            ch = []; prev = None; first = None; ls = lo = 0
            for q in qs:
                y, n, t, e, p, x, m, act, bk = R[(r, q)]
                if prev is not None and n != prev: ch.append(f'{q}:{y}:{n}')
                prev = n
                if e < 0.3:
                    if first is None: first = f'{q}:{y}:{n}'
                    if n == 'スペイン': ls += 1
                    else: lo += 1
            L.append(f'{a}\t{s}\t{r}\t{",".join(ch[:4]) or "-"}\t{first or "-"}\t{ls+lo}\t{ls}\t{lo}')
            summ.setdefault(a, [0, 0]); summ[a][0] += ls; summ[a][1] += lo
L.append('\n# 腕ごとの雇用 0.3 未満の四半期（2 州×10 種の和）：持ち主がスペインの間／他の国の間')
for a, (x, y) in summ.items(): L.append(f'{a}\t{x}\t{y}')
L.append('\n# 崩れた例の前後（初めて 0.3 未満になった四半期の −6〜+6）')
for a, s, r in [('Aa', 8, 'ペルー'), ('Aa', 4, 'ペルー'), ('Aa', 9, 'ペルー'), ('Aa', 2, 'ヌエバ・エスパーニャ銀地帯'), ('Aa', 5, 'ヌエバ・エスパーニャ銀地帯'), ('B', 8, 'ペルー')]:
    R = rows(a, s); qs = sorted(q for (rr, q) in R if rr == r)
    f0 = next((q for q in qs if R[(r, q)][3] < 0.3), None)
    if f0 is None: continue
    L.append(f'## {a} s{s} {r}')
    for q in range(max(0, f0 - 6), f0 + 7):
        if (r, q) in R:
            y, n, t, e, p, x, m, act, bk = R[(r, q)]
            L.append(f'{q}\t{y}\t{n}\t{t}\temp={e:.3f}\tpop={p:.0f}\textr={x:.1f}\tout={m:.2f}\tact={act:.0f}\tbk={bk:.0f}')
with open(os.path.join(OUT, 'collapse_timeline.txt'), 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
print('ok')
