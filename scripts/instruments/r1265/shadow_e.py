# (1265) 紙の影・形 (e)＝停止の門を導火線と同じ述語へ（窓の最初の月から本位の外）＋本位の外の月の累計＝(余地−切り下げ額)÷請求 ＋ 印刷の前に払えない対外の利払い÷請求。
#   (余地−切り下げ額)÷請求＝max(0,(1−r)−d×兌換)（(1250) の影の式・金の法・銀の法は少なめに読む）。対外の利払い＝claimInterestPerMonth×対外の割合（nation_s の foreignDebt÷govDebtStock・同じ四半期）。
#   印刷の前に払えない割合＝d（unpaidBeforeDevaluationShare）。兌換・r・d は記録の値のまま（停止の後の軌道の変化は読めない＝近似）。
#   窓は基線の窓だけ（窓の外へ延びる累計は読めない＝消える側に寄る近似）。
# 使い方: python -X utf8 -I shadow_e.py <生> <種0> <種1> <ラベル>
import sys, csv
raw, s0, s1, lab = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
print(f'# {lab} 種 {s0}〜{s1}')
print('seed\tnation\tyear\tmonths\tbase\te_room\te_foreign\te_cum\te_gone\tforeignShareMed')
n = g = 0; dec = {}
for s in range(s0, s1 + 1):
    fs = {}
    for r in csv.DictReader(open(f'{raw}/nation_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        d = num(r['govDebtStock']); fs[(r['nation'], int(r['quarter']))] = num(r['foreignDebt']) / d if d > 0 else 0.0
    hist = {}
    for r in csv.DictReader(open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        h = hist.setdefault(r['nation'], []); h.append(r)
        if len(h) < 2 or not num(r['defaultCount']) > num(h[-2]['defaultCount']): continue
        k = len(h) - 2
        while k >= 0 and num(h[k]['insolvencyMonths']) > 0: k -= 1
        w = h[k + 1:]
        base = er = ef = 0.0; fl = []
        for i, x in enumerate(w):
            prev = w[i - 1] if i > 0 else h[k]
            c0, d, rr = num(x['convertibilityShare']), num(x['unpaidBeforeDevaluationShare']), num(x['payRateShare'])
            if d != d: d = 0.0
            add = 1 - rr; b = max(0.0, add - d * c0)
            base += add if num(prev['onGold']) > 0 else b
            q = int(x['monthIndex']) // 3; f = fs.get((x['nation'], q - 1), 0.0)   # 前の四半期の末（不履行の四半期は国債 0 で割合が壊れる）
            ct = num(x['claimTotalPerMonth']); phi = num(x['claimInterestPerMonth']) * f / ct if ct > 0 else 0.0
            er += b; ef += d * phi; fl.append(f)
        cum = er + ef; gone = cum < 1.0; n += 1; g += gone
        dk = int(r['year']) // 10 * 10; dd = dec.setdefault(dk, [0, 0]); dd[0] += 1; dd[1] += gone
        print('\t'.join([str(s), r['nation'], r['year'], str(len(w)), f'{base:.3f}', f'{er:.3f}', f'{ef:.3f}', f'{cum:.3f}', str(int(gone)), f'{sorted(fl)[len(fl)//2]:.2f}']))
print(f'# 計 {n}・形 (e) の窓の中で消える {g}・年代ごと［件・消える］ {dict(sorted(dec.items()))}')
