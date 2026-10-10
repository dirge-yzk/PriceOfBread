"""(171) 戦死の10年ごとの分解（読むだけ）＝cas156/inj_cas の Y 行（国×年）から：交戦の兵士の月（種の中央・百万人月）×戦死の率（戦死÷兵士の月・%/月）＝戦死（千人・種の中央）。
接触の割合（接触した兵÷兵士の月の平均）も並べる。python an_casdec.py <out_dir(cas を含む)> [--seeds N]"""
import sys, glob, os, collections, statistics as st
args = sys.argv[1:]; maxsd = 99
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
D = args[0]
per = collections.defaultdict(lambda: collections.defaultdict(lambda: [0.0] * 5))
for f in sorted(glob.glob(os.path.join(D, 'cas', 'c*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    if sd > maxsd: continue
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] != 'Y': continue
        a = per[int(t[1]) // 10 * 10][sd]
        a[0] += float(t[4]); a[1] += float(t[7]); a[2] += float(t[5]); a[3] += float(t[3]); a[4] += float(t[8])
print('年代 | 交戦の兵士の月（百万人月・種の中央） | 戦死÷兵士の月（%/月・種の中央） | 接触した兵÷兵士（種の中央） | 戦死（千人・種の中央）')
for d in sorted(per):
    v = per[d].values()
    sm = st.median([a[0] / 1e3 for a in v]); rate = st.median([100 * a[1] / a[0] for a in v if a[0] > 0]); ct = st.median([a[2] / a[0] for a in v if a[0] > 0]); dd = st.median([a[1] for a in v])
    print(f'{d}年代 | {sm:.0f} | {rate:.3f} | {ct:.3f} | {dd:.0f}')
