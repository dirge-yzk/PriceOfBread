# (950) 読むだけ：r950_type.py の E∧F の国（type_all.mark.log の行）ごとに、E と F の重なりの区間（±8 四半期）で
#   国の州の和（cearn_s）の 貯蓄の最小・その四半期の 相続人の返済（heirSet）・輸入の払い（outImport）・資本の取り立て（outCapital）を、その四半期の貯蓄と並べる。
#   底の比＝（heirSet＋outImport）÷貯蓄（貯蓄が最小の四半期）。G の有無と並べる。エンジンの関数を呼ばない。
#   python -X utf8 -I r950_bottom.py <type_all.mark.log> <名>=<ins フォルダ> ...
import sys, os, re
log = sys.argv[1]; dirs = dict(a.split('=', 1) for a in sys.argv[2:])
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
want = {}; cur = None
for line in open(log, encoding='utf-8'):
    if line.startswith('# '): cur = line.split()[1]; continue
    if line.startswith('  ') and cur in dirs:
        x = line.rstrip('\n').split('\t')
        s, n, sec, pk, e, f_, g = int(x[1]), x[2], x[3], x[5], eval(x[6]), eval(x[7]), x[8]
        lo, hi = min(e[0], f_[0]) - 8, max(e[1], f_[1]) + 8
        want.setdefault((cur, s), []).append((n, sec, pk, lo, hi, g))
print('名\t種\t国\t分\t人口の最大\t区間\tG\t貯蓄の最小の四半期\t貯蓄\theirSet\toutImport\toutCapital\t底の比（heirSet＋outImport）÷貯蓄')
for (nm, s), lst in sorted(want.items()):
    d = dirs[nm]; names = {n for n, *_ in lst}; agg = {}
    with open(os.path.join(d, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iq, inn, isv, ihs, ioi, ioc = ix['quarter'], ix['nation'], ix['savings'], ix['heirDebtSettledPerQuarter'], ix['outImportPerQuarter'], ix['outCapitalPerQuarter']
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[inn] not in names: continue
            a = agg.setdefault((x[inn], int(x[iq])), [0.0, 0.0, 0.0, 0.0])
            for j, i in enumerate((isv, ihs, ioi, ioc)):
                v = fl(x[i]); a[j] += v if v == v else 0.0
    for n, sec, pk, lo, hi, g in lst:
        qs = [(q, agg[(n, q)]) for q in range(lo, hi + 1) if (n, q) in agg]
        if not qs: print(nm, s, n, '行なし', sep='\t'); continue
        q, a = min(qs, key=lambda t: t[1][0])
        ratio = (a[1] + a[2]) / a[0] if a[0] > 0 else float('inf')
        print(nm, s, n, sec, pk, f'{lo}〜{hi}', g or '-', q, f'{a[0]:.4g}', f'{a[1]:.4g}', f'{a[2]:.4g}', f'{a[3]:.4g}', f'{ratio:.4g}', sep='\t')
