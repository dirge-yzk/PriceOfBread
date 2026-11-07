# (950) 読むだけ：cearn_s の州の格ごとの Σ outImport・Σ inExport・Σ heirDebtSettled・Σ importPaid（人の居ない商人が払い手の行＝importPayerKind 2）を盤面ごとに並べる。
import sys, os
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
for a in sys.argv[1:]:
    nm, d = a.split('=', 1); T = {}
    for s in range(1, 11):
        p = os.path.join(d, f'cearn_s{s}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            it, io, ie, ih, ip, ik = ix['tier'], ix['outImportPerQuarter'], ix['inExportPerQuarter'], ix['heirDebtSettledPerQuarter'], ix['importPaidPerQuarter'], ix['importPayerKind']
            for line in f:
                x = line.rstrip('\n').split('\t'); t = T.setdefault(x[it], [0.0] * 4)
                for j, i in enumerate((io, ie, ih)):
                    v = fl(x[i]); t[j] += v if v == v else 0.0
                if x[ik] == '2':
                    v = fl(x[ip]); t[3] += v if v == v else 0.0
    print(nm, *(f'{k}: 輸入の払い {v[0]:.4g}・輸出の受け {v[1]:.4g}・相続人の返済 {v[2]:.4g}・空の商人が払い手の輸入 {v[3]:.4g}' for k, v in sorted(T.items())), sep='\n  ')
