import csv,collections
d='D:/PoB_work/ident_c104cp/out_ib_10/ins'
c=collections.Counter(); names=collections.Counter(); imm=0; immv=0
for s in range(1,11):
    for r in csv.DictReader(open(f'{d}/center_s{s}.tsv'),delimiter='\t'):
        busy=r['inCivilWarFlag']=='1' or r['pendingRebelFlag']=='1'
        g=float(r['gMinusWIndex']); fp=float(r['frontPowerIndex'])
        if r['ignitionViolentFlag']=='1' and not busy:
            c['viol']+=1; c['viol_G0' if g==0 else 'viol_Gpos']+=1; c['viol_front' if fp>0 else 'viol_nofront']+=1
            if r['immediateFlag']=='1': immv+=1
        if r['ignitionPeacefulFlag']=='1' and not busy:
            c['peace']+=1; c['peace_G0' if g==0 else 'peace_Gpos']+=1
        if r['quarter']=='0' and float(r['sharePart'])>float(r['shareExec']): names[(r['nation'],r['polityCode'])]+=1
print(dict(c),'immediate&violent',immv); print(sorted(names.items()))
