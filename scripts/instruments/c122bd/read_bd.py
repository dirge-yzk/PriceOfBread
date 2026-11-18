# (1215) c122bd 腕 B の機構の観測量 G1 と記録：fiscal_s の defaultCount が増えた月を全ての国で拾い、前の月の金本位で群 A・B に分ける。
# G1＝群 B の不履行のうち、離脱の後（金本位でない月の続き）の全ての月で devaluedPerMonth＞0 か兌換 1（余地 0＝取る余地が無い）だった物の数（旗 1 の形では 0 のはず）。
# 記録：群ごとの件数・英の件数・離脱から不履行までの月・兌換の停止（onGold 1→0）の数。
# 使い方: python -X utf8 -I read_bd.py <生> <種0> <種1>
import sys, csv, collections
raw, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
G = collections.Counter(); uk = collections.Counter(); g1 = []; lag = []; susp = 0; ukseeds = set()
rows = []
for s in range(s0, s1 + 1):
    hist = {}
    for r in csv.DictReader(open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        h = hist.setdefault(r['nation'], []); h.append(r)
        if len(h) < 2: continue
        if num(h[-2]['onGold']) > 0 and not num(r['onGold']) > 0: susp += 1
        if not num(r['defaultCount']) > num(h[-2]['defaultCount']): continue
        grp = 'A' if num(h[-2]['onGold']) > 0 else 'B'
        G[grp] += 1
        if r['nation'] == 'イギリス': uk[grp] += 1; ukseeds.add(s)
        if grp == 'B':
            k = 2; off = []
            while k <= len(h) and not num(h[-k]['onGold']) > 0: off.append(h[-k]); k += 1
            allprint = len(off) > 0 and all(num(x['devaluedPerMonth']) > 0 or num(x['convertibilityShare']) >= 1 - 1e-9 for x in off)
            lag.append(len(off))
            if allprint: g1.append((s, r['nation'], r['year'], r['monthIndex']))
        rows.append((s, r['nation'], r['year'], r['monthIndex'], grp))
print(f'# {raw} 種 {s0}〜{s1}')
for x in rows: print('\t'.join(map(str, x)))
print(f'不履行 群A {G["A"]}・群B {G["B"]}・英 群A {uk["A"]}・群B {uk["B"]}（英の不履行のある種 {len(ukseeds)}/{s1-s0+1}）・兌換の停止 {susp}')
print(f'群B の離脱から不履行までの月（金本位でない月の続き） {sorted(lag)}')
print(f'G1 印刷が毎月働いたのに不履行した群B {len(g1)} {g1}\tG1 {"赤" if g1 else "緑"}')
