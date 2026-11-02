# r789: 1 州の年ごとの推移（region の計器）
import sys
sys.stdout.reconfigure(encoding='utf-8')
f, reg, y0, y1 = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
with open(f, encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    print('year\tnation\ttier\tpopK\turbK\tipc\tfacOut\tfacN\tfacBk\textOut\templSh\twageF\topp\thold\tpush\thous\tmigOut\txbOut\twar\tfood')
    for l in fh:
        c = l.rstrip('\n').split('\t')
        if c[ix['region']] != reg: continue
        q = int(c[0]); y = int(c[1])
        if q % 4 or y < y0 or y > y1: continue
        g = lambda k: float(c[ix[k]])
        pop = g('populationK')
        print(f"{y}\t{c[ix['nation']]}\t{c[ix['tier']]}\t{pop:.0f}\t{pop-g('ruralPopulationK'):.0f}\t{g('earnedPerMonth')/max(pop,1e-9):.3f}\t{g('factoryOutputRefValuePerMonth'):.0f}\t{c[ix['factoryActiveCount']]}\t{c[ix['factoryBankruptCount']]}\t{g('extractionOutputRefValuePerMonth'):.0f}\t{g('employmentShare'):.3f}\t{g('wageFactor'):.2f}\t{g('opportunityIndex'):.2f}\t{g('holdingFactor'):.2f}\t{g('pushFactor'):.2f}\t{g('housingFactor'):.2f}\t{g('migrationOutKPerQuarter'):.1f}\t{g('crossBorderOutKPerQuarter'):.1f}\t{g('warIntensity'):.2f}\t{g('foodFillShare'):.2f}")
