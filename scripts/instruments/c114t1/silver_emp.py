# (1159) 周辺の点検：銀の産地 2 州（ペルー・ヌエバ・エスパーニャ銀地帯）の雇用が 0.3 未満の四半期（(1156) の第 3 の鎖・B-1＝0 で崩れが 0 のままか）。読むだけ。
#   使い方: python -X utf8 -I silver_emp.py 名=<ins のフォルダ> ...
import sys, os, collections
REG = ['ペルー', 'ヌエバ・エスパーニャ銀地帯']
for a in sys.argv[1:]:
    name, d = a.split('=', 1)
    lt = collections.Counter(); long40 = 0; seeds = 0
    for s in range(1, 11):
        p = os.path.join(d, f'region_s{s}.tsv')
        if not os.path.exists(p):
            continue
        seeds += 1; seen = set(); per = collections.Counter()
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                c = line.rstrip('\n').split('\t')
                r = c[ix['region']]
                if r not in REG:
                    continue
                k = (r, c[ix['quarter']])
                if k in seen:
                    continue
                seen.add(k)
                try:
                    e = float(c[ix['employmentShare']])
                except Exception:
                    continue
                if e < 0.3:
                    per[r] += 1
        for r in REG:
            lt[r] += per[r]
            if per[r] >= 40:
                long40 += 1
    print(f'{name} 種 {seeds}：雇用 0.3 未満の四半期 ' + ' '.join(f'{r} {lt[r]}' for r in REG) + f'・40 四半期以上の州×種 {long40}')
