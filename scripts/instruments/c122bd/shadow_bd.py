# (1215) 段0 の紙の影：不履行の月までの 12 か月を fiscal_s から読み、形 (a)・(b) と突然変異 M1 で導火線の累計を置き直す。
# 前提（生で確かめた式）：払えた率 r = 1 − d·兌換（d＝切り下げ前の払えない割合・切り下げは余地を全て取る＝DevaluationCapReliefExp 1）。
#  (a) 金本位でない月の次の月から、兌換を月 1−(1−0.34)^(1/3) で 0 へ（四半期の末の下げはしない）。r' = 1 − d·兌換'。
#  (M1) 同じく月 0.34。
#  (b) 前の月に金本位でない月は、累計に max(0,(1−r) − d·兌換) だけを積む（兌換の目標 0 との隔たりの分を数えない）。
# 群：前の月の onGold が 1＝A・0＝B。累計は払えた率が 1 の月に 0 へ戻る（Fiscal.cs:193 と同じ）。不履行の月の累計が 1 未満なら〈その月は消える〉。
# 使い方: python -X utf8 -I shadow_bd.py <生> <種0> <種1> <ラベル>
import sys, csv
raw, s0, s1, lab = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
DM = 1 - (1 - 0.34) ** (1 / 3)
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
print(f'# {lab} 種 {s0}〜{s1}・月の等価の下げ {DM:.5f}')
print('seed\tnation\tyear\tmonth\tgroup\tbase_cum\ta_cum\tM1_cum\tb_cum\ta_gone\tM1_gone\tb_gone')
for s in range(s0, s1 + 1):
    hist = {}
    for r in csv.DictReader(open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        h = hist.setdefault(r['nation'], []); h.append(r)
        if len(h) < 2 or not num(r['defaultCount']) > num(h[-2]['defaultCount']): continue
        w = h[-13:]
        grp = 'A' if num(h[-2]['onGold']) > 0 else 'B'
        res = []
        for form in ('base', 'a', 'M1', 'b'):
            cum = 0.0; conv = None
            for i, x in enumerate(w):
                prevGold = num(w[i - 1]['onGold']) > 0 if i > 0 else True
                c0, d, rr = num(x['convertibilityShare']), num(x['unpaidBeforeDevaluationShare']), num(x['payRateShare'])
                if d != d: d = 0.0
                if form in ('a', 'M1'):
                    if prevGold or conv is None: conv = c0
                    else: conv = conv * (1 - (DM if form == 'a' else 0.34))
                    if not prevGold: conv = min(conv, c0)
                    rr2 = rr if prevGold else min(1.0, 1 - d * conv)
                    add = 1 - rr2
                elif form == 'b':
                    add = (1 - rr) if prevGold else max(0.0, (1 - rr) - d * c0)
                else:
                    add = 1 - rr
                if not add > 1e-12: cum = 0.0
                else: cum += add
            res.append(cum)
        print('\t'.join([str(s), r['nation'], r['year'], r['monthIndex'], grp] + [f'{v:.3f}' for v in res] + ['1' if v < 1 else '0' for v in res[1:]]))
