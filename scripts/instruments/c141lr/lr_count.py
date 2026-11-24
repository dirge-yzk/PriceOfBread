# (1344) c141lr：守られた法 7 軸の後退の件数を monlaw_s*.tsv から数える（読むだけ・エンジンの関数を呼ばない）。
# 後退の向き：ShorterHours と ChildLabor は段が上がる側・他の 5 軸は段が下がる側（Government.Regressive と同じ向きを表から独立に書く）。
# 使い方：python lr_count.py <ins フォルダ> [--years]
import sys, glob, os, re, statistics
AX_DOWN = ['Suffrage', 'WomenRights', 'SocialSecurity', 'ColonialSuffrage', 'MigrantNativeSuffrage']
AX_UP = ['ShorterHours', 'ChildLabor']
SIX = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ']
def run(d, years=False):
    out = {}
    for f in sorted(glob.glob(os.path.join(d, 'monlaw_s*.tsv')), key=lambda p: int(re.search(r'_s(\d+)', p).group(1))):
        s = int(re.search(r'_s(\d+)', f).group(1))
        with open(f, 'rb') as fh:
            hdr = fh.readline().decode('utf-8').rstrip('\r\n').split('\t')
            ix = {h: i for i, h in enumerate(hdr)}
            prev = {}; per_ax = {a: 0 for a in AX_DOWN + AX_UP}; six = 0; six_ax = {a: 0 for a in AX_DOWN + AX_UP}; ev = []
            for line in fh:
                c = line.decode('utf-8').rstrip('\r\n').split('\t')
                uid = c[ix['nationUid']]; nm = c[ix['nation']]
                vals = {a: int(float(c[ix[a]])) for a in AX_DOWN + AX_UP}
                if uid in prev:
                    for a in AX_DOWN + AX_UP:
                        p = prev[uid][a]; v = vals[a]
                        reg = (v < p) if a in AX_DOWN else (v > p)
                        if reg:
                            per_ax[a] += 1
                            if nm in SIX:
                                six += 1; six_ax[a] += 1; ev.append((c[ix['year']], nm, a, p, v))
                prev[uid] = vals
        out[s] = (per_ax, six, six_ax, ev)
    return out
if __name__ == '__main__':
    d = sys.argv[1]; r = run(d)
    print('seed\tsixTotal\t' + '\t'.join('all:' + a for a in AX_DOWN + AX_UP) + '\t' + '\t'.join('six:' + a for a in AX_DOWN + AX_UP))
    for s, (pa, six, sa, ev) in r.items():
        print(f'{s}\t{six}\t' + '\t'.join(str(pa[a]) for a in AX_DOWN + AX_UP) + '\t' + '\t'.join(str(sa[a]) for a in AX_DOWN + AX_UP))
    print('median_sixTotal\t' + str(statistics.median([v[1] for v in r.values()])) if r else 'median_sixTotal\tNA')
    if '--years' in sys.argv:
        for s, (_, _, _, ev) in r.items():
            for e in ev: print('EV', s, *e, sep='\t')
