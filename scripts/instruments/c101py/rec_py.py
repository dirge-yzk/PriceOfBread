# (1063) c101py 記録（門ではない・ヌル腕の幅と並べる・事前登録 §5 の記録）：
#   ①貿易÷GDP（6 大国の各国と世界）＝nation_s の exportsPerClearing×clearingsPerYear ÷（nominalGdpPerYear×fxRate）〈推：輸出は世界価格建て・GDP は自国の通貨＝fxRate で金へ〉
#   ②調達の財（石炭・原油・キニーネ・缶詰・帆船・汽船）の値段 priceMeanGold（nationgood_s・6 大国）の 腕÷基準（種ごとの比の中央）
#   ③エネルギーの取り分 share4Energy（score_s・6 大国）
#   年＝1850・1880・1913・1950（その年の最後の四半期の行）。種ごとの値の中央。
#   使い方：python -X utf8 -I rec_py.py <出力> base=<ins> n1=<ins> n2=<ins> B=<ins> ...（最初の腕が比の分母）
import sys, os, statistics as st
from collections import defaultdict
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
YEARS = ['1850', '1880', '1913', '1950']
GOODS = ['石炭', '原油', 'キニーネ', '缶詰', '帆船', '汽船']
OUT = sys.argv[1]; ARMS = [a.split('=', 1) for a in sys.argv[2:]]


def rows(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')


def fl(x):
    try: return float(x)
    except ValueError: return float('nan')


def arm_data(d):
    tg = defaultdict(dict); px = defaultdict(dict); en = defaultdict(dict)
    for s in range(1, 11):
        p = os.path.join(d, 'nation_s%d.tsv' % s)
        if not os.path.exists(p): continue
        last = {}
        for ix, v in rows(p):
            y = v[ix['year']]
            if y in YEARS: last[(y, v[ix['nation']])] = v; lix = ix
        ex = defaultdict(float); gd = defaultdict(float)
        for (y, n), v in last.items():
            e = fl(v[lix['exportsPerClearing']]) * fl(v[lix['clearingsPerYear']]); g = fl(v[lix['nominalGdpPerYear']]) * fl(v[lix['fxRate']])
            if not (g > 0) or e != e: continue
            if n in MAJ: tg[(y, n)][s] = e / g
            ex[y] += e; gd[y] += g
        for y in YEARS:
            if gd[y] > 0: tg[(y, '世界')][s] = ex[y] / gd[y]
        p = os.path.join(d, 'nationgood_s%d.tsv' % s)
        if os.path.exists(p):
            for ix, v in rows(p):
                if v[ix['year']] in YEARS and v[ix['nation']] in MAJ and v[ix['good']] in GOODS:
                    px[(v[ix['year']], v[ix['nation']], v[ix['good']])][s] = fl(v[ix['priceMeanGold']])
        p = os.path.join(d, 'score_s%d.tsv' % s)
        if os.path.exists(p):
            for ix, v in rows(p):
                if v[ix['year']] in YEARS and v[ix['nation']] in MAJ: en[(v[ix['year']], v[ix['nation']])][s] = fl(v[ix['share4Energy']])
    return tg, px, en


D = [(nm, arm_data(p)) for nm, p in ARMS]
med = lambda xs: st.median(xs) if xs else float('nan')
with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('# (1063) rec_py.py %s\n' % ' '.join(sys.argv[2:]))
    f.write('\n== ① 貿易÷GDP（種の中央）\n')
    for y in YEARS:
        for n in MAJ + ['世界']:
            f.write('%s %s\t%s\n' % (y, n, '\t'.join('%s %.4f' % (nm, med(list(dd[0][(y, n)].values()))) for nm, dd in D)))
    f.write('\n== ② 調達の財の値段 priceMeanGold の 腕÷%s（種ごとの比の中央・6 大国をまとめた中央）\n' % D[0][0])
    b = D[0][1][1]
    for y in YEARS:
        for g in GOODS:
            cells = []
            for nm, dd in D[1:]:
                rs = []
                for n in MAJ:
                    a = dd[1].get((y, n, g), {}); bb = b.get((y, n, g), {})
                    rs += [a[s] / bb[s] for s in a if s in bb and bb[s] > 0 and a[s] == a[s]]
                cells.append('%s %.4f(%d)' % (nm, med(rs), len(rs)))
            f.write('%s %s\t%s\n' % (y, g, '\t'.join(cells)))
    f.write('\n== ③ エネルギーの取り分 share4Energy（種の中央）\n')
    for y in YEARS:
        for n in MAJ:
            f.write('%s %s\t%s\n' % (y, n, '\t'.join('%s %.4f' % (nm, med(list(dd[2][(y, n)].values()))) for nm, dd in D)))
print('書いた', OUT)
