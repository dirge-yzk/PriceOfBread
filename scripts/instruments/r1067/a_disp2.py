"""(1067) A の 2：同じ国の州どうしの値段の散らばり（cearn_s＝四半期×州の全ての国・全期間）。読むだけ。
量＝staplePrice（州の主食の値段）と regionPriceLevel（州の物価水準）。国×四半期ごとに州 2 以上で
変動係数（州の単純・母標準偏差÷平均）と 最大÷最小。範囲＝全ての州／本国格（Core）だけ。各年の第 1 四半期（quarter%4==0）だけ読む。
時期＝1783-1849・1850-1913・1914-1950。国×四半期×種の行の中央と 90 百分位。
python -X utf8 a_disp2.py <ins> <出力>"""
import sys, os, glob, collections, statistics, math
ins, out = sys.argv[1], sys.argv[2]
NAT9 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']


def era(y): return '1783-1849' if y < 1850 else '1850-1913' if y < 1914 else '1914-1950'


res = collections.defaultdict(list)   # (量, 範囲, 時期, 国 or '*') -> [(cv, mm)]
for p in sorted(glob.glob(os.path.join(ins, 'cearn_s*.tsv'))):
    grp = collections.defaultdict(list)
    with open(p, encoding='utf-8') as f:
        h = next(f).rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        iq, iy, inn, it, il, isp = ix['quarter'], ix['year'], ix['nation'], ix['tier'], ix['regionPriceLevel'], ix['staplePrice']
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            q = int(c[iq])
            if q % 4: continue
            try: lv = float(c[il]); sp = float(c[isp])
            except ValueError: continue
            grp[(q, int(c[iy]), c[inn])].append((c[it], lv, sp))
    for (q, y, n), rs in grp.items():
        for scope in ('全ての州', '本国格'):
            sel = rs if scope == '全ての州' else [r for r in rs if r[0] == 'Core']
            for qi, name in ((1, 'regionPriceLevel'), (2, 'staplePrice')):
                xs = [r[qi] for r in sel if r[qi] > 0 and math.isfinite(r[qi])]
                if len(xs) < 2: continue
                m = sum(xs) / len(xs); cv = statistics.pstdev(xs) / m; mm = max(xs) / min(xs)
                res[(name, scope, era(y), '*')].append((cv, mm))
                if n in NAT9: res[(name, scope, era(y), n)].append((cv, mm))


def qt(xs, p):
    xs = sorted(xs); return xs[min(len(xs) - 1, int(p * (len(xs) - 1) + 0.5))]


with open(out, 'w', encoding='utf-8') as fo:
    fo.write('# 量\t範囲\t時期\t国（*＝州 2 以上の全ての国）\t行（国×年×種）\t変動係数の中央\t変動係数の90百分位\t最大÷最小の中央\t最大÷最小の90百分位\n')
    for k in sorted(res, key=lambda k: (k[0], k[1], k[3] != '*', k[3], k[2])):
        v = res[k]; cv = [a for a, _ in v]; mm = [b for _, b in v]
        fo.write('\t'.join(k) + f'\t{len(v)}\t{statistics.median(cv):.3f}\t{qt(cv, .9):.3f}\t{statistics.median(mm):.3f}\t{qt(mm, .9):.3f}\n')
print('keys', len(res))
