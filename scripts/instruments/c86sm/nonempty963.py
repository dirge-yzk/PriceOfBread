# (963) 列が空でない対照：旗 0 の cearn_s の新 2 列が 0 でない州×四半期の数（種ごと）。python nonempty963.py <ins のフォルダ>
import sys, csv, glob, re
T = chr(9)
tot = [0, 0, 0]
for p in sorted(glob.glob(sys.argv[1] + '/cearn_s*.tsv'), key=lambda x: int(re.findall(r'(\d+)\.tsv$', x)[0])):
    n = a = b = 0
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter=T); h = next(r)
        i, j = h.index('heirDebtAfterSettleSum'), h.index('billWithHeirDebtClearings')
        for x in r:
            n += 1
            try:
                if float(x[i]) > 1e-9: a += 1
            except ValueError: pass
            if int(x[j]) > 0: b += 1
    print(p.split('/')[-1], 'rows', n, 'sum>1e-9', a, 'billWithHeir>0', b)
    tot[0] += n; tot[1] += a; tot[2] += b
print('合計 rows', tot[0], 'sum>1e-9', tot[1], 'billWithHeir>0', tot[2])
