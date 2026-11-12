# (1073) 生の突き合わせ：tskpair_s の売り手の国の和 ＝ tradeskip_s の行（買い手の国×財の越境の売り手の門の 8 列）・tskshadow_s の組の数 ＝ xWedge＋xPass（年 1870・1913）。
# 使い方: python -X utf8 -I pair_sum_check.py <tradeskip のある ins フォルダ（基線の生でよい＝tradeskip_s は恒等でバイト一致）> <tskpair／tskshadow のある ins フォルダ>
import os, re, sys
from collections import defaultdict
YEARS = ('1870', '1913')
COLS = ['xPairs', 'xSoldOut', 'xCap', 'xWedgeA', 'xWedgeB', 'xWedgeC', 'xWedgeD', 'xPass']


def rd(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f: yield ln.rstrip('\n').split('\t'), ix


a, b = sys.argv[1], sys.argv[2]
seeds = sorted(int(m.group(1)) for f in os.listdir(b) for m in [re.fullmatch(r'tskpair_s(\d+)\.tsv', f)] if m)
bad = rows = 0
for s in seeds:
    ts = {}
    for v, ix in rd(os.path.join(a, 'tradeskip_s%d.tsv' % s)):
        if v[ix['year']] in YEARS and v[ix['nation']] != 'ALL':
            ts[(v[ix['year']], v[ix['nation']], v[ix['good']])] = [float(v[ix[c]]) for c in COLS] + [float(v[ix['xWedge']])]
    sp = defaultdict(lambda: [0.0] * 8)
    for v, ix in rd(os.path.join(b, 'tskpair_s%d.tsv' % s)):
        k = (v[ix['year']], v[ix['nation']], v[ix['good']])
        for i, c in enumerate(COLS): sp[k][i] += float(v[ix[c]])
    ss = defaultdict(float)
    for v, ix in rd(os.path.join(b, 'tskshadow_s%d.tsv' % s)):
        ss[(v[ix['year']], v[ix['nation']], v[ix['good']])] += float(v[ix['pairs']])
    for k, w in ts.items():
        rows += 1
        got = sp.get(k, [0.0] * 8)
        if got != w[:8] or ss.get(k, 0.0) != w[7] + w[8]: bad += 1
    extra = [k for k in sp if k not in ts]
    bad += len(extra)
print('種 %d・tradeskip の行 %d・食い違い %d' % (len(seeds), rows, bad))
sys.exit(1 if bad else 0)
