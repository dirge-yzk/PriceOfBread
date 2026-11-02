# (778) 合流の記録の読み手（読むだけ）：c7sw の同じ盤面の新÷旧（全ての行と工事中でない行）と工事中の下げ・c7ag の英の国債の不履行と金本位国の数。
#   python -X utf8 -I rec_lines.py <名前>=<計器の出力 ins>[,<種の頭>] ...
import sys, os, csv, statistics
def rows(p):
    with open(p, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) < len(h): x += [''] * (len(h) - len(x))
            yield dict(zip(h, x))
def seeds(d, s0):
    out = []
    for s in range(s0, s0 + 10):
        if os.path.exists(os.path.join(d, f'nation_s{s}.tsv')): out.append(s)
    return out
for arg in sys.argv[1:]:
    name, rest = arg.split('=', 1); d, _, s0 = rest.partition(','); s0 = int(s0 or 1)
    ss = seeds(d, s0)
    print(f'## {name}（{d}・種 {ss[0]}〜{ss[-1]}）')
    # c7sw
    if os.path.exists(os.path.join(d, f'inheritintent_s{ss[0]}.tsv')):
        so = sn = so0 = sn0 = 0; dn = dn0 = dc = 0; l2 = l2c0 = 0; how = {}
        for s in ss:
            hit = hit0 = False
            for r in rows(os.path.join(d, f'inheritintent_s{s}.tsv')):
                if r['scope'] != 'E': continue
                o, n, u = int(r['stateTargetOld']), int(r['stateTargetNew']), int(r['stateTargetUsed'])
                cl = float(r['constructionLeftDays'] or 0)
                how[r['how']] = how.get(r['how'], 0) + 1
                so += o; sn += n
                if u < o: dn += 1; hit = True
                if cl > 0:
                    if u < o: dc += 1
                else:
                    so0 += o; sn0 += n
                    if u < o: dn0 += 1; hit0 = True
            l2 += hit; l2c0 += hit0
        print(f'c7sw：E 行の how {how}・Σ新÷Σ旧 全ての行 {sn/so:.3f}（{sn}/{so}）・工事中でない行 {sn0/so0:.3f}（{sn0}/{so0}）')
        print(f'c7sw：実際に下げた行 {dn}（うち工事中 {dc}＝{dc/max(dn,1):.1%}・工事中でない {dn0}）・L2（下げた行のある種）全ての行 {l2}/{len(ss)}・工事中でない行だけ {l2c0}/{len(ss)}')
    # c7ag
    if os.path.exists(os.path.join(d, f'silverpar_s{ss[0]}.tsv')):
        gy = {}
        for s in ss:
            for r in rows(os.path.join(d, f'silverpar_s{s}.tsv')):
                if r['law'] != 'G': continue
                y = int(r['year']); q = int(r['quarter'])
                if q % 4 != 3 or y not in (1800, 1840, 1860, 1880, 1900, 1913): continue
                gy.setdefault(y, []).append(float(r['nationCount']))
        print('c7ag：金本位国の数（年末・10 種の中央値）' + '・'.join(f'{y} {statistics.median(v):g}' for y, v in sorted(gy.items())))
    dflt = []
    for s in ss:
        mx = 0; first = None
        for r in rows(os.path.join(d, f'nation_s{s}.tsv')):
            if r['nation'] != 'イギリス': continue
            c = float(r['sovereignDefaultCumulativeCount'] or 0)
            if c > mx:
                mx = c
                if first is None: first = r['year']
        dflt.append((s, int(mx), first))
    k = sum(1 for x in dflt if x[1] > 0)
    print(f'英（BaseName イギリス）の国債の不履行の累計＞0 の種 {k}/{len(ss)}・' + ' '.join(f's{s}:{m}@{y}' for s, m, y in dflt if m > 0))
