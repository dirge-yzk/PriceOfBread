# (1394) rv：国の国庫の台帳を年ごとに平均する（読むだけ）
# 使い方: python q2_ledger.py <RAW> <seed> <国名> <年の始め> <年の終わり> [月の始め 月の終わり]
import sys, os, collections
raw, seed, nat, y0, y1 = sys.argv[1], int(sys.argv[2]), sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
m0 = int(sys.argv[6]) if len(sys.argv) > 6 else None; m1 = int(sys.argv[7]) if len(sys.argv) > 7 else None
COLS = ['claimTotalPerMonth','claimSoldierPayPerMonth','claimAdminPerMonth','claimArmyPerMonth','adminSpendableAtBudgetPerMonth','adminBureaucratCoverShare','adminFillShare','revenueBasePerMonth','payRateShare',
        'ledgerTaxPerMonth','ledgerTariffPerMonth','ledgerTributeInPerMonth','ledgerOtherInPerMonth','ledgerStateFactoryPerMonth','ledgerBorrowPerMonth',
        'ledgerArmyPerMonth','ledgerAdminPerMonth','ledgerWelfarePerMonth','ledgerPensionPerMonth','ledgerColonialUpkeepPerMonth','ledgerOtherOutPerMonth','ledgerWarCostPerMonth','ledgerInterestPerMonth',
        'treasuryEndStock','treasuryDeltaPerMonth','ledgerResidualPerMonth']
def fl(x):
    try: return float(x)
    except: return float('nan')
acc = collections.defaultdict(lambda: collections.defaultdict(float)); n = collections.Counter()
with open(os.path.join(raw, f'fiscal_s{seed}.tsv'), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t')
    for ln in fh:
        v = dict(zip(h, ln.rstrip('\n').split('\t')))
        if v['nation'] != nat: continue
        y = int(v['year']); mi = int(v['monthIndex'])
        if not (y0 <= y <= y1): continue
        if m0 is not None and not (m0 <= mi <= m1): continue
        key = mi if m0 is not None else y
        n[key] += 1
        for c in COLS: acc[key][c] += fl(v.get(c, 'nan'))
print('key\t' + '\t'.join(COLS))
for k in sorted(acc):
    print(str(k) + '\t' + '\t'.join(f'{acc[k][c]/n[k]:.4g}' for c in COLS))
