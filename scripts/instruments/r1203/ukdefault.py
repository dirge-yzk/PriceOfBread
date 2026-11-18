# (1203) 問い③ 英（ほか 9 国）の国債の不履行の入口：fiscal_s の defaultCount が増えた月と、その前 12 か月の入口の列（金本位か・兌換・利払い÷歳入・借入の門・切り下げ・払えた率・借入の縛り）
# leftGold_monthsBefore＝不履行の月から遡って最後に onGold>0 だった月までの月数（-1＝記録の中で一度も金本位でない）
# 使い方: python -X utf8 -I ukdefault.py <生> <種0> <種1> <出力> [国名（既定 イギリス・ALL で 9 国）]
import sys, csv
raw, s0, s1, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
who = sys.argv[5] if len(sys.argv) > 5 else 'イギリス'
COLS = ['onGold','convertibilityShare','interestToRevenueShareNow','interestAtLimitFlag','borrowGateOpenFlag','borrowCooldownMonths','borrowBinder','payRateShare','devaluedPerMonth','unpaidBeforeDevaluationShare','insolvencyMonths','govDebtStock']
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return None
o = open(outp, 'w', encoding='utf-8', newline='\n')
o.write('seed\tnation\tmonthIndex\tyear\tdefaultNo\tonGold_at\tonGold_prev\tonGold_months12\tleftGold_monthsBefore\tconv_at\tint2rev_max12\tatLimit_months12\tgateOpen_months12\tcooldown_at\tbinder_mode12\tpayRate_min12\tdevalued_months12\tinsolv_at_prev\tdebt_at\n')
for s in range(s0, s1 + 1):
    hist = {}
    with open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            n = r['nation']
            if who != 'ALL' and n != who: continue
            h = hist.setdefault(n, []); h.append(r)
            if len(h) < 2: continue
            d0, d1 = num(h[-2]['defaultCount']), num(r['defaultCount'])
            if d0 is None or d1 is None or not d1 > d0: continue
            w12 = h[-13:-1]
            g = lambda c: [num(x[c]) for x in w12 if num(x[c]) is not None]
            binders = [x['borrowBinder'] for x in w12]
            mode = max(set(binders), key=binders.count) if binders else ''
            o.write('\t'.join(str(v) for v in [s, n, r['monthIndex'], r['year'], int(d1), r['onGold'], h[-2]['onGold'], int(sum(1 for v in g('onGold') if v > 0)), next((k for k in range(1, len(h)) if (num(h[-1-k]['onGold']) or 0) > 0), -1), r['convertibilityShare'],
                f"{max(g('interestToRevenueShareNow') or [float('nan')]):.3f}", int(sum(g('interestAtLimitFlag'))), int(sum(g('borrowGateOpenFlag'))), r['borrowCooldownMonths'], mode,
                f"{min(g('payRateShare') or [float('nan')]):.3f}", sum(1 for v in g('devaluedPerMonth') if v > 0), h[-2]['insolvencyMonths'], f"{num(r['govDebtStock']) or float('nan'):.4g}"]) + '\n')
