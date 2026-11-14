# (1144) W2(b)′：越境の輸出の額のうち級 E（賦存の豊かな州の産出）に配った額の割合。エンジンの関数は呼ばない・生の表を自分で読む。
#   国×財×年の crossExportValue（nationgood_s）を、同じ国×財×年の recipeout_s の outputSum の級 E の割合で配る。
#   W2b'(年) = Σ_{国,財} crossExportValue × E の割合 ÷ Σ_{国,財} crossExportValue（10 種を合わせた和）。種ごとの値も出す。
#   門＝(腕 − 基線) が幅の 2 倍を超える（幅＝基線・N1・N2 の 3 本の最大−最小）。
# 使い方: python -X utf8 -I w2b.py 基線=<dir> N1=<dir> N2=<dir> [腕=<dir> ...] [--years 1870,1913] [--mut noE|allE]
#   --mut noE  : 突然変異（E の割合を 0 と読む＝値が 0 になるはず）
#   --mut allE : 突然変異（級を見ず割合 1＝値が 1 になるはず）
import sys, glob, os, collections

def read_tsv(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for line in f:
            v = line.rstrip('\n').split('\t')
            if len(v) < len(h): continue
            yield dict(zip(h, v))

def fnum(x):
    try:
        v = float(x)
        return v if v == v and v not in (float('inf'), float('-inf')) else None
    except Exception:
        return None

def arm(d, years, mut):
    per_seed = {}
    tot = {y: [0.0, 0.0] for y in years}
    for ng in sorted(glob.glob(os.path.join(d, 'nationgood_s*.tsv'))):
        seed = ng.rsplit('_s', 1)[1][:-4]
        ro = os.path.join(d, f'recipeout_s{seed}.tsv')
        if not os.path.exists(ro): continue
        out = collections.defaultdict(lambda: [0.0, 0.0])   # (年,国,財) → [E の産出, 全ての産出]
        for r in read_tsv(ro):
            y = int(r['year'])
            if y not in years: continue
            o = fnum(r['outputSum'])
            if o is None or o <= 0: continue
            k = (y, r['nation'], r['good'])
            out[k][1] += o
            if r['class'] == 'E': out[k][0] += o
        s = {y: [0.0, 0.0] for y in years}
        for r in read_tsv(ng):
            y = int(r['year'])
            if y not in years: continue
            x = fnum(r['crossExportValue'])
            if x is None or x <= 0: continue
            e, a = out.get((y, r['nation'], r['good']), (0.0, 0.0))
            sh = (e / a) if a > 0 else 0.0
            if mut == 'noE': sh = 0.0
            elif mut == 'allE': sh = 1.0
            s[y][0] += x * sh; s[y][1] += x
        per_seed[seed] = {y: (s[y][0] / s[y][1] if s[y][1] > 0 else float('nan')) for y in years}
        for y in years: tot[y][0] += s[y][0]; tot[y][1] += s[y][1]
    return {y: (tot[y][0] / tot[y][1] if tot[y][1] > 0 else float('nan')) for y in years}, per_seed

def main(argv):
    years = [1870, 1913]; mut = None; arms = []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--years': years = [int(x) for x in argv[i + 1].split(',')]; i += 2; continue
        if a == '--mut': mut = argv[i + 1]; i += 2; continue
        k, v = a.split('=', 1); arms.append((k, v)); i += 1
    res = {}
    for k, d in arms:
        res[k], ps = arm(d, years, mut)
        print(k, ' '.join(f'{y} {res[k][y]:.5f}' for y in years), ' 種の数', len(ps))
    names = [k for k, _ in arms]
    if all(n in res for n in ('基線', 'N1', 'N2')):
        for y in years:
            vals = [res[n][y] for n in ('基線', 'N1', 'N2')]
            w = max(vals) - min(vals)
            line = f'幅 {y} {w:.5f}  門の閾値（基線＋2×幅）{res["基線"][y] + 2 * w:.5f}'
            for n in names:
                if n in ('基線', 'N1', 'N2'): continue
                dlt = res[n][y] - res['基線'][y]
                line += f'  {n}: 差 {dlt:+.5f} → {"通る" if dlt > 2 * w else "通らない"}'
            print(line)
    return 0

if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
