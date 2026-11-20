# (1250) c127bd 段0 の紙の影（(1215) の shadow_bd.py の直し・反証役 v2 の中）：不履行の月ごとに、基線の導火線が最後に 0 だった月の次から
#   不履行の月までを fiscal_s から読み、旧（1−r）と形 (b) の累計を置き直す。
# 直した所：旧の影は〈積む量 0 の月〉に累計を 0 へ戻していた。エンジン（Fiscal.cs:193）が 0 へ戻すのは条件が偽の月
#   （r＝1・国債 0・利払いが歳入の上限の内・助走の凍結）だけ＝基線の insolvencyMonths が 0 の行。窓をその行の次から取れば、
#   窓の中に 0 へ戻る月は無い（同じ軌道なら旗 1 でも同じ月に戻る）。形 (b) は積む量だけを変える。
# 形 (b) の 1 か月の量（月の請求の割合）＝(1−r)×(余地−切り下げ額)÷払えない額 ＝ (余地−切り下げ額)÷請求
#   ＝ (1−r) − d×兌換（金の法の式：余地＝deficit×(1−兌換)）。銀の法の国は余地＝deficit×(1−兌換×(1−正貨))＝この影は積む量を
#   少なめに読む（消える側に寄る＝近似・名乗る）。d＝unpaidBeforeDevaluationShare（切り下げ前の払えない割合）。
# 群：前の月の onGold が 1＝A（形は触れない）・0＝B。
# 使い方: python -X utf8 -I shadow_bd.py <生> <種0> <種1> <ラベル>
import sys, csv
raw, s0, s1, lab = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
print(f'# {lab} 種 {s0}〜{s1}')
print('seed\tnation\tyear\tmonth\tgroup\tmonths\tbase_cum\tengine_cum\tb_cum\tb_gone')
tot = {'A': [0, 0], 'B': [0, 0]}; uk = {'A': [0, 0], 'B': [0, 0]}; ukseeds = set(); ukseeds_b = set(); chk = 0
for s in range(s0, s1 + 1):
    hist = {}
    for r in csv.DictReader(open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        h = hist.setdefault(r['nation'], []); h.append(r)
        if len(h) < 2 or not num(r['defaultCount']) > num(h[-2]['defaultCount']): continue
        k = len(h) - 2
        while k >= 0 and num(h[k]['insolvencyMonths']) > 0: k -= 1
        w = h[k + 1:]                         # 基線の導火線が 0 だった月の次から不履行の月まで
        grp = 'A' if num(h[-2]['onGold']) > 0 else 'B'
        base = 0.0; b = 0.0
        for i, x in enumerate(w):
            prev = w[i - 1] if i > 0 else h[k]
            prevGold = num(prev['onGold']) > 0
            c0, d, rr = num(x['convertibilityShare']), num(x['unpaidBeforeDevaluationShare']), num(x['payRateShare'])
            if d != d: d = 0.0
            add = 1 - rr
            base += add
            b += add if prevGold else max(0.0, add - d * c0)
        eng = num(h[-2]['insolvencyMonths'])   # 不履行の前の月の累計（エンジン）＋不履行の月の 1−r が base と合うかの確かめ
        if abs((eng + (1 - num(r['payRateShare']))) - base) > 1e-6: chk += 1
        gone = b < 1.0
        tot[grp][0] += 1; tot[grp][1] += gone
        if r['nation'] == 'イギリス':
            uk[grp][0] += 1; uk[grp][1] += gone; ukseeds.add(s)
            if not gone: ukseeds_b.add(s)
        print('\t'.join([str(s), r['nation'], r['year'], r['monthIndex'], grp, str(len(w)), f'{base:.4f}', f'{eng:.4f}', f'{b:.4f}', '1' if gone else '0']))
n = s1 - s0 + 1
print(f'全ての国 群A {tot["A"][0]}（形で消える {tot["A"][1]}）・群B {tot["B"][0]}（形で消える {tot["B"][1]}）')
print(f'英 群A {uk["A"][0]}（消える {uk["A"][1]}）・群B {uk["B"][0]}（消える {uk["B"][1]}）・英の不履行のある種 基線 {len(ukseeds)}/{n} → 形 (b) の影 {len(ukseeds_b)}/{n}')
print(f'確かめ：影の旧の累計がエンジンの累計と食い違う不履行 {chk} 件（0 なら窓と式がエンジンと同じ）')
