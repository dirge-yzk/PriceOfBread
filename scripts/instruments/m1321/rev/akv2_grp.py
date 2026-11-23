import sys, csv, os, statistics
raw=sys.argv[1]
g={'1':[], '0':[]}; z=[]; share=[]
SB=int(sys.argv[2]) if len(sys.argv)>2 else 1  # (1321) 種の起点を引数に（S11 で読むため・合流が足した）
for s in range(SB,SB+10):
    for r in csv.DictReader(open(os.path.join(raw,f'sectreas_s{s}.tsv'),encoding='utf-8'),delimiter='\t'):
        o=float(r['oldRuleT']); n=float(r['newRuleT']); pt=float(r['parentTreasury'])
        if o>0: g[r['capitalRisingFlag']].append(n/o)
        if pt>0: share.append((r['capitalRisingFlag'], n/pt, float(r['movedMoney'])/float(r['parentMoney']) if float(r['parentMoney'])>0 else -1))
        if float(r['actualT'])==0: z.append((s,r['quarter'],r['year'],r['parent'],r['firstRegion'],r['movedMoney'],r['parentTreasury'],r['oldRuleT']))
for k,v in g.items():
    v.sort(); print('capRising',k,'n',len(v),'median',statistics.median(v) if v else None,'min',v[0] if v else None,'max',v[-1] if v else None)
sh=[x[1] for x in share if x[0]=='1']; sh.sort()
print('capRising share of treasury: n',len(sh),'median',statistics.median(sh),'max',sh[-1], '>0.9:',sum(1 for x in sh if x>0.9))
print('zero rows',z)
