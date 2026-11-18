# (1202) c120t2 紙の台本（今の盤面＝基線の計器つき 10 種の生）：門 H の基準・記録 G-能 の母集合・産地の開いた年・品位の分布。
# 使い方：python -X utf8 -I paper.py <生のフォルダ（prodcap_s<種>.tsv・mintreg_s<種>.tsv）> <種の起点> <種の終点> [<腕の生>]
#   腕の生を渡すと、門 H を腕÷基準で読む（同じ台本で両方を数える）。
#   読む行：prodcap_s の isRgo=1・recipe=鉱山（鉱物の RGO）・年 1784 と 1913（各の年の最後の q の断面）。
#   H＝世界の産出（output の和）1913÷1784。史実（AI の一般の知識・頁は確かめていない）：石炭 110〜130・鉄鉱石 110〜150（銑鉄の倍率で代えた）・金 35〜45・銀 約 8。
#   10 倍の線：比÷史実の中点が [0.1, 10] の外なら〈外〉。門＝腕で新しく外へ出た財が 0（基準で既に外の財は向きを記録）。
import sys, os, csv, collections, statistics
csv.field_size_limit(1 << 30)
RAW = sys.argv[1]; S0 = int(sys.argv[2]); S1 = int(sys.argv[3]); ARM = sys.argv[4] if len(sys.argv) > 4 else None
HIST = {'石炭': (110, 130), '鉄鉱石': (110, 150), '金': (35, 45), '銀': (8, 8)}
YEARS = (1784, 1850, 1880, 1913)

def scan(d, s):
    """種 s の prodcap_s を 1 回読む：年ごとの最後の q・財ごとの産出の和・産地の行・G-能 の母集合（州の品位は mintreg_s の同じ年の最後の q）"""
    p = os.path.join(d, 'prodcap_s%d.tsv' % s)
    out = collections.defaultdict(float)          # (year, q, good) -> output
    lastq = {}
    sites = {}                                     # (region, good) -> 最初に siteFound=1 の年
    nprod = collections.defaultdict(int)           # (year, q, good, isSite) -> 件数（capacity>0）
    reg = collections.defaultdict(lambda: collections.defaultdict(float))   # (year,q) -> region -> 鉱物の産出の和
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        iY, iQ, iR, iG, iRec, iRgo, iCap, iOut, iSite, iFound = (ix[k] for k in ('year', 'q', 'region', 'good', 'recipe', 'isRgo', 'capacity', 'output', 'siteRow', 'siteFound'))
        for row in r:
            if row[iRgo] != '1' or row[iRec] != '鉱山': continue
            y = int(row[iY]); q = int(row[iQ])
            if y not in YEARS: continue
            lastq[y] = max(lastq.get(y, -1), q)
            g = row[iG]; o = float(row[iOut]); cap = float(row[iCap]); site = int(row[iSite]) >= 0
            out[(y, q, g)] += o
            reg[(y, q)][row[iR]] += o
            if cap > 0: nprod[(y, q, g, site)] += 1
            if site and row[iFound] == '1': sites.setdefault((row[iR], g), y)
    return out, lastq, sites, nprod, reg

def grades(d, s, want):
    """mintreg_s の州の品位（year, q）→ region→grade（同じ州が複数の国の行に出たら最初の行）"""
    p = os.path.join(d, 'mintreg_s%d.tsv' % s); res = collections.defaultdict(dict)
    if not os.path.exists(p): return res
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            k = (int(row[ix['year']]), int(row[ix['q']]))
            if k in want: res[k].setdefault(row[ix['region']], float(row[ix['grade']]))
    return res

def ratios(d):
    res = {}; extra = {}
    for s in range(S0, S1 + 1):
        out, lastq, sites, nprod, reg = scan(d, s)
        y0, y1 = (1784, lastq[1784]), (1913, lastq[1913])
        res[s] = {g: (out[(1913, y1[1], g)] / out[(1784, y0[1], g)] if out[(1784, y0[1], g)] > 0 else float('nan')) for g in HIST}
        want = {(y, lastq[y]) for y in YEARS if y in lastq}
        gr = grades(d, s, want)
        e = {'sites': sites, 'n': {}, 'floorShare': {}}
        for y in YEARS:
            if y not in lastq: continue
            k = (y, lastq[y]); gm = gr.get(k, {})
            tot = sum(reg[k].values()); low = sum(o for rg, o in reg[k].items() if gm.get(rg, 1.0) <= 0.2)
            wgrade = sum(o * gm.get(rg, 1.0) for rg, o in reg[k].items()) / tot if tot > 0 else float('nan')
            e['floorShare'][y] = (low / tot if tot > 0 else float('nan'), wgrade)
            e['n'][y] = (sum(v for (yy, qq, g, st), v in nprod.items() if (yy, qq) == k and not st),
                         sum(v for (yy, qq, g, st), v in nprod.items() if (yy, qq) == k and st))
        extra[s] = e
    return res, extra

def med(v):
    v = sorted(x for x in v if x == x)
    return statistics.median(v) if v else float('nan')

def side(x, g):
    lo, hi = HIST[g]; mid = (lo + hi) / 2
    q = x / mid
    return '外' if not (0.1 <= q <= 10) else '内', q

base, bx = ratios(RAW)
print('# (1202) c120t2 紙の台本・生 %s・種 %d〜%d' % (RAW, S0, S1))
print('== H 世界の鉱物の産出 1913÷1784（種ごと・中央）・比÷史実の中点（10 倍の線） ==')
for g in HIST:
    vs = [base[s][g] for s in sorted(base)]
    m = med(vs); sd, q = side(m, g)
    print('%s\t種ごと %s\t中央 %.3f\t史実 %s\t比÷史実 %.4f\t%s' % (g, ' '.join('%.2f' % v for v in vs), m, HIST[g], q, sd))
print('== 記録：鉱物の RGO の州の品位（mintreg_s の grade）≦0.2 の州の産出の割合・産出の重みの品位（種の中央） ==')
for y in YEARS:
    a = [bx[s]['floorShare'][y][0] for s in bx if y in bx[s]['floorShare']]
    b = [bx[s]['floorShare'][y][1] for s in bx if y in bx[s]['floorShare']]
    n0 = [bx[s]['n'][y][0] for s in bx if y in bx[s]['n']]; n1 = [bx[s]['n'][y][1] for s in bx if y in bx[s]['n']]
    print('%d\t品位≦0.2 の州の産出の割合 中央 %.3f\t重みの品位 中央 %.3f\t1÷重みの品位 %.2f\tふつうの生産者（能力＞0）中央 %s\t産地 中央 %s' % (y, med(a), med(b), 1 / med(b) if med(b) > 0 else float('nan'), med(n0), med(n1)))
print('== 記録：産地が開いた年（siteFound=1 が最初に出た断面の年・断面は 1784・1850・1880・1913 だけ＝粗い） ==')
for s in sorted(bx):
    print('種 %d\t%s' % (s, '・'.join('%s %s %d' % (rg, g, y) for (rg, g), y in sorted(bx[s]['sites'].items(), key=lambda t: t[1]))))
if ARM:
    arm, ax = ratios(ARM)
    print('== 門 H：腕÷基準・新しく 10 倍の外へ出た財 ==')
    bad = 0
    for g in HIST:
        mb = med([base[s][g] for s in base]); ma = med([arm[s][g] for s in arm])
        sb, qb = side(mb, g); sa, qa = side(ma, g)
        new = sb == '内' and sa == '外'; back = sb == '外' and sa == '内'
        bad += new
        print('%s\t基準 %.3f（%s）\t腕 %.3f（%s）\t腕÷基準 %.3f\t%s' % (g, mb, sb, ma, sa, ma / mb if mb > 0 else float('nan'), '新しく外＝赤' if new else ('戻った' if back else '変わらず')))
    print('門 H：%s（新しく外 %d 財）' % ('赤' if bad else '緑', bad))
