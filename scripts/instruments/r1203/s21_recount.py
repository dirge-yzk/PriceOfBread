# (1203) 問い③ 台帳 S21 の数の行を keep の生で数え直す（エンジンの閾値の式で：S21-07 は interestAtLimitFlag〔Fiscal.cs:13 の underLimit の否定と同じ不等式〕・
#  S21-19 は借入の後に払えない額が残った月〔unpaidBeforeDevaluationShare>0〕のうち devaluedPerMonth>0 の月・S21-16 は govBondRatePerYear≥0.30〔BondRateMaxPerYear〕・
#  S21-26 は defaultCount の増えた月と前の月の onGold・S21-22 は money_s の cumulativeFiatIssuedStock が減った四半期・S21-05 は nation_s の 2 列の和）
# 計器が書かない行（空・数でない）は数えない。
# 使い方: python -X utf8 -I s21_recount.py <生> <種0> <種1> <出力>
import sys, csv, collections, os
raw, s0, s1, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return None
C = collections.Counter(); byNat = collections.Counter(); reformNat = collections.Counter(); grpA = collections.Counter(); grpB = collections.Counter()
for s in range(s0, s1 + 1):
    prev = {}
    with open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            n = r['nation']; lim = num(r['interestAtLimitFlag']); pa = num(r['unpaidBeforeDevaluationShare']); dv = num(r['devaluedPerMonth']); br = num(r['govBondRatePerYear']); dc = num(r['defaultCount'])
            if lim is not None: C['months'] += 1; C['atLimit'] += lim > 0
            if pa is not None and dv is not None and pa > 1e-9: C['unpaidAfterBorrow'] += 1; C['devaluedOfThose'] += dv > 0
            if br is not None and br >= 0.30 - 1e-12: C['rateAtMax'] += 1
            p = prev.get(n)
            if p is not None and dc is not None and p[0] is not None and dc > p[0]:
                C['defaults'] += 1; byNat[n] += 1
                (grpA if (p[1] or 0) > 0 else grpB)[n] += 1
            prev[n] = (dc, num(r['onGold']))
    prevF = {}
    if not os.path.exists(f'{raw}/money_s{s}.tsv'): C['noMoney'] += 1
    else:
     with open(f'{raw}/money_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            n = r['nation']; v = num(r['cumulativeFiatIssuedStock'])
            if v is None: continue
            if n in prevF and v < prevF[n] - 1e-9: C['fiatBurnQuarters'] += 1; reformNat[n] += 1
            prevF[n] = v
    with open(f'{raw}/nation_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            a = num(r.get('borrowPoolBoundMonthsCount')); b = num(r.get('borrowPoolBoundForeignCutByInvestorsMonthsCount'))
            if a is not None and b is not None: C['poolBound'] += a; C['poolCutByInvestors'] += b
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f"種 {s0}〜{s1}・生 {raw}\n")
    m = max(1, C['months'])
    o.write(f"S21-07 上限に届いた月 {C['atLimit']}/{C['months']}（{C['atLimit']/m:.2%}）\n")
    o.write(f"S21-16 金利が 0.30 に当たった月 {C['rateAtMax']}/{C['months']}\n")
    o.write(f"S21-19 借入の後に払えない額が残った月 {C['unpaidAfterBorrow']}・うち切り下げた月 {C['devaluedOfThose']}（{C['devaluedOfThose']/max(1,C['unpaidAfterBorrow']):.1%}）\n")
    o.write(f"S21-26 不履行 {C['defaults']}＝" + '・'.join(f'{k} {v}' for k, v in byNat.most_common()) + f"／前の月に金本位（群 A）{sum(grpA.values())}＝" + '・'.join(f'{k} {v}' for k, v in grpA.most_common()) + f"／離脱の後（群 B）{sum(grpB.values())}＝" + '・'.join(f'{k} {v}' for k, v in grpB.most_common()) + "\n")
    if C['noMoney']: o.write(f"S21-22 money_s の無い種 {C['noMoney']}（数えない）\n")
    o.write(f"S21-22 不換の台帳が減った四半期 {C['fiatBurnQuarters']}＝" + '・'.join(f'{k} {v}' for k, v in reformNat.most_common()) + "\n")
    o.write(f"S21-05 プールが縛った月 {C['poolBound']:.0f}・他国の投資家の貯蓄で切られた月 {C['poolCutByInvestors']:.0f}\n")
