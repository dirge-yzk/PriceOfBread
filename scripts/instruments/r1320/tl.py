import re,sys,os
P=re.compile(r'SubsistenceShare|SubsistenceStage2|s2Unemp|s2Sigma|FoodOvf|StapleLedger|LastSubsistenceOutput|SubsistenceValuePerMonth|FarmUnemploymentStage2|HarvestShortfallToMarket|FamineGateReturnsRural')
root=sys.argv[1]
for f in sorted(os.listdir(root)):
    if not f.endswith('.cs'): continue
    L=open(os.path.join(root,f),'rb').read().decode('utf-8-sig').split('\n')
    cur=None;start=0;hits={}
    for i,l in enumerate(L):
        m=re.search(r'public (?:async )?(?:void|Task) (\w+)\(',l)
        if m: cur=m.group(1);start=i+1
        if P.search(l): hits.setdefault(cur,[]).append(i+1)
    if hits: print(f, '; '.join(f'{k}@{v[0]}..{v[-1]}({len(v)})' for k,v in hits.items()))
