import sys
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, 'D:/PoB_work/wt_o1/scripts/instruments')
from instr import rows, num
D = 'D:/PoB_work/o1_315/out_B_10/ins'
tot = bor = 0; rev0 = bor_rev0 = 0; int0 = bor_int0 = 0; both = 0
by = {}
for sd, r in rows(D, 'nation'):
    m = num(r.get('gateOpenedByGdpOnlyMonthsCount', 'nan'))
    if not (m == m and m > 0): continue
    b = num(r['gateOpenedByGdpOnlyBorrowedMonthsCount'])
    rev = num(r.get('revenueBaseMeanPerMonth', 'nan')); it = num(r.get('interestMeanPerMonth', 'nan'))
    tot += m; bor += b
    if not (rev > 0):
        rev0 += m; bor_rev0 += b; by[r['nation']] = by.get(r['nation'], 0) + m
    if it == 0:
        int0 += m; bor_int0 += b
print('第2条件だけで開いた月 合計', tot, '借りた', bor)
print('  うち 四半期の歳入の月平均 <=0（または nan）の行', rev0, '借りた', bor_rev0)
print('  うち 利払い==0 の行', int0, '借りた', bor_int0)
print('  歳入<=0の行の国別', sorted(by.items(), key=lambda kv: -kv[1])[:8])
cnt = {}
for sd, r in rows(D, 'nation'):
    m = num(r.get('gateOpenedByGdpOnlyMonthsCount', 'nan'))
    if m == m and m > 0: cnt[r['nation']] = cnt.get(r['nation'], 0) + m
print('国別 第2条件だけで開いた月 上位:', sorted(cnt.items(), key=lambda kv: -kv[1])[:10], ' 国数', len(cnt))
