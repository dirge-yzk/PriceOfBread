import sys, os, glob, re
sys.stdout.reconfigure(encoding='utf-8')
d = sys.argv[1]; UK = 'イギリス'
W = [8, 40, 80, 160, 240, 10**6]
out = {w: [0, 0, 0, 0] for w in W}   # 不履行のある種, 停止のある種, 停止の総回数, 種数
first_def = {}; first_sus = {}
for p in glob.glob(os.path.join(d, 'fiscal_s*.tsv')):
    sd = int(re.search(r'fiscal_s(\d+)', p).group(1))
    with open(p, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t'); ix = {h: i for i, h in enumerate(head)}
        prev = None; dq = {w: 0 for w in W}; sq = {w: 0 for w in W}
        for ln in fh:
            f = ln.rstrip('\n').split('\t')
            if f[ix['nation']] != UK: continue
            m = int(f[ix['monthIndex']]); q = 0 if m < 2 else 1 + (m - 2) // 3
            g = float(f[ix['onGold']]); dcv = float(f[ix['defaultCount']])
            for w in W:
                if q < w:
                    if dcv > 0: dq[w] = 1
                    if prev is not None and prev >= .5 and g < .5: sq[w] += 1
            if dcv > 0 and sd not in first_def: first_def[sd] = q + 1
            if prev is not None and prev >= .5 and g < .5 and sd not in first_sus: first_sus[sd] = q + 1
            prev = g
        for w in W:
            out[w][0] += dq[w]; out[w][1] += 1 if sq[w] > 0 else 0; out[w][2] += sq[w]; out[w][3] += 1
print('窓(四半期未満) 不履行のある種 停止のある種 停止の総回数 種数')
for w in W: print(w if w < 10**6 else '全期', *out[w])
print('最初の不履行の四半期(Qn):', dict(sorted(first_def.items())))
print('最初の金本位停止の四半期(Qn):', dict(sorted(first_sus.items())))
