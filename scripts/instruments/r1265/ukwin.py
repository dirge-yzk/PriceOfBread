# (1265) 英の不履行の窓：defaultCount が増えた月ごとに、導火線（insolvencyMonths＞0）の窓と前後の月を fiscal_s から並べる。
# 使い方: python -X utf8 -I ukwin.py <生> <種0> <種1> <国名> <前の月数>
import sys, csv
raw, s0, s1, nat, pre = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], int(sys.argv[5])
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
K = ['year','monthIndex','onGold','convertibilityShare','payRateShare','insolvencyMonths','defaultCount','interestToRevenueShareNow','interestToRevenueShareAtGate','interestAtLimitFlag','borrowGateOpenFlag','borrowBinder','govDebtStock','revenueBasePerMonth','claimTotalPerMonth','claimInterestPerMonth','claimArmyPerMonth','claimNavyPerMonth','borrowedPerMonth','devaluedPerMonth','treasuryAtClaimStock','spareBeforeBorrowPerMonth']
for s in range(s0, s1 + 1):
    h = [r for r in csv.DictReader(open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8'), delimiter='\t') if r['nation'] == nat]
    for i in range(1, len(h)):
        if not num(h[i]['defaultCount']) > num(h[i-1]['defaultCount']): continue
        k = i - 1
        while k >= 0 and num(h[k]['insolvencyMonths']) > 0: k -= 1
        a = max(0, k - pre)
        print(f'## 種 {s} 不履行 {h[i]["year"]} 月 {h[i]["monthIndex"]} 窓 {i-k} か月（導火線 0 の月 {h[k]["year"] if k>=0 else "-"}）')
        print('\t'.join(K))
        for x in h[a:i+3]: print('\t'.join(str(x.get(c,'')) [:10] for c in K))
