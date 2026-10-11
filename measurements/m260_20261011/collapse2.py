import sys, collections
sys.path.insert(0,'D:/PoB_work/wt_e/scripts/instruments')
from instr import rows, num
D=sys.argv[1]; n=collections.Counter(); allq=collections.Counter()
for sd,r in rows(D,'nation'):
    f=num(r['supplyFillShare']); p=num(r['populationK'])
    if f==f and f<0.3:
        allq[sd]+=1
        if p>=100: n[sd]+=1
print('人口100以上×充足<0.3 国×四半期', sum(n.values()), '種別', dict(sorted(n.items())), '千件級', sum(1 for v in n.values() if v>=1000), '/10  （全ての国 計', sum(allq.values()), '）')
