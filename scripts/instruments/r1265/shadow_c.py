# (1265) 紙の影：基線の不履行の窓（導火線が最後に 0 だった月の次から不履行の月まで）を fiscal_s から読み、直す形の候補ごとに累計を置き直す。
#   旧＝エンジンの累計（金本位の月は 1−r・金本位でない月は形 (b)＝max(0,(1−r)−d×兌換)＝(1250) の影と同じ式）。
#   C1（順序）＝停止の門（r＜0.7）を不履行の判定の前に：r＜0.7 の金本位の月は、その月から形 (b)。
#   C2（門の物差しを 1 本に）＝停止の門を〈導火線が積もり始めた（insolvencyMonths＞0）〉へ：窓の最初の金本位の月は 1−r、2 か月目から形 (b)。
#   兌換は記録の値のまま（停止の後に兌換が下がり印刷が増える軌道の変化は読めない＝近似・積む量を多めに読む側＝消える件を少なめに数える）。銀の法は (1250) と同じく少なめに読む近似。
# 使い方: python -X utf8 -I shadow_c.py <生> <種0> <種1> <ラベル>
import sys, csv
raw, s0, s1, lab = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
print(f'# {lab} 種 {s0}〜{s1}')
print('seed\tnation\tyear\tmonth\tmonths\tminR\tconvMean\tbase\tc1\tc2\tc1_gone\tc2_gone')
cnt = {'n': 0, 'c1': 0, 'c2': 0}; dec = {}
for s in range(s0, s1 + 1):
    hist = {}
    for r in csv.DictReader(open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        h = hist.setdefault(r['nation'], []); h.append(r)
        if len(h) < 2 or not num(r['defaultCount']) > num(h[-2]['defaultCount']): continue
        k = len(h) - 2
        while k >= 0 and num(h[k]['insolvencyMonths']) > 0: k -= 1
        w = h[k + 1:]
        base = c1 = c2 = 0.0; off1 = off2 = False; minr = 9; cs = 0
        for i, x in enumerate(w):
            prev = w[i - 1] if i > 0 else h[k]
            gold = num(prev['onGold']) > 0
            c0, d, rr = num(x['convertibilityShare']), num(x['unpaidBeforeDevaluationShare']), num(x['payRateShare'])
            if d != d: d = 0.0
            add = 1 - rr; b = max(0.0, add - d * c0); minr = min(minr, rr); cs += c0
            base += add if gold else b
            if gold and rr < 0.7: off1 = True
            c1 += b if (off1 or not gold) else add
            c2 += b if (off2 or not gold) else add
            if add > 0: off2 = True
        g1 = c1 < 1.0; g2 = c2 < 1.0
        cnt['n'] += 1; cnt['c1'] += g1; cnt['c2'] += g2
        dk = int(r['year']) // 10 * 10; dd = dec.setdefault(dk, [0, 0, 0]); dd[0] += 1; dd[1] += g1; dd[2] += g2
        print('\t'.join([str(s), r['nation'], r['year'], r['monthIndex'], str(len(w)), f'{minr:.3f}', f'{cs / len(w):.3f}', f'{base:.3f}', f'{c1:.3f}', f'{c2:.3f}', str(int(g1)), str(int(g2))]))
print(f'# 計 {cnt["n"]}・C1 で消える {cnt["c1"]}・C2 で消える {cnt["c2"]}・年代ごと［件・C1 消・C2 消］ {dict(sorted(dec.items()))}')
