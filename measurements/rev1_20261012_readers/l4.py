import sys, os, glob, re
sys.stdout.reconfigure(encoding='utf-8')
# L4：英の不履行（defaultCount の最大）と兌換停止（onGold が 1→0 に落ちた月が1つでもある種＝金本位の停止）を種ごとに数える。全期と Q1〜Q40。
# convertibilityShare の最小は記録。事前登録の語（兌換停止）の定義を、腕の出力を見る前にここで固定した。
d = sys.argv[1]
UK = 'イギリス'
res = {}
for p in sorted(glob.glob(os.path.join(d, 'fiscal_s*.tsv')), key=lambda s: int(re.search(r'fiscal_s(\d+)', s).group(1))):
    sd = int(re.search(r'fiscal_s(\d+)', p).group(1))
    with open(p, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        ix = {h: i for i, h in enumerate(head)}
        mi, cn, dc, nt, og = ix['monthIndex'], ix['convertibilityShare'], ix['defaultCount'], ix['nation'], ix['onGold']
        dmax = 0.0; conv_all = 0; conv_40q = 0; n = 0; prev = None; cmin = 9.0
        for ln in fh:
            f = ln.rstrip('\n').split('\t')
            if f[nt] != UK: continue
            m = int(f[mi]); n += 1
            try: c = float(f[cn]); dcv = float(f[dc]); g = float(f[og])
            except ValueError: continue
            dmax = max(dmax, dcv); cmin = min(cmin, c)
            if prev is not None and prev >= 0.5 and g < 0.5:
                conv_all += 1
                if m < 120: conv_40q += 1
            prev = g
        res[sd] = (n, dmax, conv_all, conv_40q, cmin)
print('seed\tUK月数\t不履行の累計max\t金本位停止の回数(全期)\t同(Q1〜Q40)\tconvertibilityShare最小')
for sd, (n, dm, ca, c40, cm) in res.items():
    print(f'{sd}\t{n}\t{dm:.0f}\t{ca}\t{c40}\t{cm:.3f}')
print('不履行のある種:', sum(1 for v in res.values() if v[1] > 0), '/', len(res), '  金本位停止のある種(全期):', sum(1 for v in res.values() if v[2] > 0), '  (Q1〜Q40):', sum(1 for v in res.values() if v[3] > 0))
