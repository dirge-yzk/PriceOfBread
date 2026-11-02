# r789: 盤面の再現の確かめ＋州の年表（region_s の標準の計器・四半期の末 Q4 の断面と年平均）。引数：<ins> <種> <州> <年0> <年1> [国の物価の州＝r789r があれば物価で割る]
import sys, os, collections
sys.stdout.reconfigure(encoding='utf-8')
D, sd, REG, y0, y1 = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
f = float
Y = collections.defaultdict(list)
with open(f'{D}/region_s{sd}.tsv', encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t')
    for l in fh:
        d = dict(zip(h, l.rstrip('\n').split('\t')))
        if d['region'] != REG: continue
        y = int(d['year'])
        if y0 <= y <= y1: Y[y].append(d)
PL = collections.defaultdict(list)
rp = f'{D}/r789r_s{sd}.tsv'
if os.path.exists(rp):
    with open(rp, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for l in fh:
            d = dict(zip(h, l.rstrip('\n').split('\t')))
            if d['region'] == REG: PL[int(d['year'])].append(f(d['priceLevel']))
print('year\tnation\ttier\tpopK\turbanK\tearned/pop\treal(÷P)\tP\twagePrem\topp\thold\tpush\temploy\thousing\tfoodFill\tfacOutRef\tfacAct\tfacBk\tmigOut\tmigIn\txbOut')
for y in sorted(Y):
    L = Y[y]; m = lambda k: sum(f(x[k]) for x in L) / len(L)
    pop = m('populationK'); e = m('earnedPerMonth'); p = sum(PL[y]) / len(PL[y]) if PL[y] else float('nan')
    print(y, L[-1]['nation'], L[-1]['tier'], f'{pop:.0f}', f"{pop - m('ruralPopulationK'):.0f}", f'{e / pop:.3f}', f'{e / pop / p:.3f}' if p == p else '-', f'{p:.3f}' if p == p else '-',
          *[f'{m(k):.3f}' for k in ('wagePremium', 'opportunityIndex', 'holdingFactor', 'pushFactor', 'employmentShare', 'housingFactor', 'foodFillShare')],
          f"{m('factoryOutputRefValuePerMonth'):.0f}", f"{m('factoryActiveCount'):.0f}", f"{m('factoryBankruptCount'):.0f}",
          f"{sum(f(x['migrationOutKPerQuarter']) for x in L):.1f}", f"{sum(f(x['migrationInKPerQuarter']) for x in L):.1f}", f"{sum(f(x['crossBorderOutKPerQuarter']) for x in L):.1f}", sep='\t')
