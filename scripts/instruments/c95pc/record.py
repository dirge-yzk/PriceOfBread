# (1019) c95pc 記録の列（門にしない）：prodcap_s と recipeout_s の生から
#   (1) 段0″a の門 N1 の基線＝足す予定の 15 行（hole.tsv の class=D・conf=中・dataHas=なし）の (州, 財) の開幕（最初の年）の productivity と級
#   (2) 段0″b の 3 財（石灰石・珪砂・粘土）の級 N の行の数・産出（年ごと）
#   (3) recipeout_s の siteFound の混在の行（0 でも 1 でもない行）の数
# 使い方：python record.py <生のフォルダ> <hole.tsv> <出力>  （生＝prodcap_s<種>.tsv と recipeout_s<種>.tsv のあるフォルダ。種 1〜10）
import sys, glob, os, collections

RAW, HOLE, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
MINERAL_NAMES = ['石灰石', '珪砂', '粘土']

def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        if h[0].startswith('#'): h = f.readline().rstrip('\n').split('\t')   # hole.tsv の先頭は注の行
        for l in f:
            yield dict(zip(h, l.rstrip('\n').split('\t')))

want = []
for r in rows(HOLE):
    if r.get('class') == 'D' and r.get('conf', '').startswith('中') and r.get('dataHas') == 'なし':
        want.append((r['nation'], r['good'], r['simProvince']))

out = []
out.append('# N1 の基線：足す予定の %d 行の (州, 財) の最初の年の productivity（種ごと・RGO の行の合計と個数）' % len(want))
out.append('seed\tnation\tgood\tprovince\tfirstYear\tn\tproductivitySum\tclasses\tsiteFound')
seeds = sorted(int(os.path.basename(p)[len('prodcap_s'):-4]) for p in glob.glob(os.path.join(RAW, 'prodcap_s*.tsv')))
three = collections.defaultdict(lambda: [0, 0.0, 0.0])
mixed = collections.Counter()
xrows = collections.Counter()
for s in seeds:
    first = None
    acc = collections.defaultdict(lambda: [0, 0.0, set(), 0])
    for r in rows(os.path.join(RAW, 'prodcap_s%d.tsv' % s)):
        y = r['year']
        if first is None: first = y
        if r['isRgo'] == '1':
            acc[(y, r['region'], r['good'])][0] += 1
            acc[(y, r['region'], r['good'])][1] += float(r['productivity'])
            acc[(y, r['region'], r['good'])][2].add(r['class'])
            acc[(y, r['region'], r['good'])][3] += int(r['siteFound'])
        if r['good'] in MINERAL_NAMES and r['class'] == 'N':
            k = (s, y, r['good'])
            three[k][0] += 1; three[k][1] += float(r['output']); three[k][2] += float(r['capacity'])
    for (nat, g, prov) in want:
        a = acc.get((first, prov, g))
        if a is None: out.append('%d\t%s\t%s\t%s\t%s\t0\tnan\t-\t-' % (s, nat, g, prov, first))
        else: out.append('%d\t%s\t%s\t%s\t%s\t%d\t%.10g\t%s\t%d' % (s, nat, g, prov, first, a[0], a[1], '/'.join(sorted(a[2])), a[3]))
out.append('')
out.append('# 段0″b：3 財の級 N の行（prodcap_s の生産者の数・産出の和・能力の和）')
out.append('seed\tyear\tgood\tn\toutputSum\tcapacitySum')
for k in sorted(three):
    n, o, c = three[k]
    out.append('%d\t%s\t%s\t%d\t%.10g\t%.10g' % (k[0], k[1], k[2], n, o, c))
out.append('')
out.append('# recipeout_s の siteFound：級 X の行の数と、混在（0 でも 1 でもない）の行の数')
out.append('seed\txRows\tmixedRows\tallFound\tnoneFound')
for s in seeds:
    p = os.path.join(RAW, 'recipeout_s%d.tsv' % s)
    if not os.path.exists(p): continue
    nx = nm = n1 = n0 = 0
    for r in rows(p):
        if r['class'] != 'X': continue
        nx += 1
        v = float(r['siteFound'])
        if v == 0.0: n0 += 1
        elif v == 1.0: n1 += 1
        else: nm += 1
    out.append('%d\t%d\t%d\t%d\t%d' % (s, nx, nm, n1, n0))
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
