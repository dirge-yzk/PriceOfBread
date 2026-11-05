import sys
reg=sys.argv[1]; seed=sys.argv[2]
for name,d in [('B','D:/PoB_work/c71po_work/out_c71poB_10/ins'),('M852','D:/PoB_work/keep_852/base_ins')]:
    p='%s/rule_s%s.tsv'%(d,seed)
    with open(p,'rb') as f:
        h=f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix={c:i for i,c in enumerate(h)}
        out=[]
        for raw in f:
            r=raw.decode('utf-8').rstrip('\r\n').split('\t')
            if r[ix['region']]==reg:
                y=float(r[ix['year']])
                if abs(y-round(y/10)*10)<0.01 or y>=1950: out.append('%d %s pop %s state %.3f ungov %.3f ordReg %s mil %s/%s' % (y, r[ix['nation']], r[ix['popK']], float(r[ix['ruleStateShare']]), float(r[ix['ruleUngovShare']]), r[ix['orderRegionIndex']], r[ix['militancyRegionIndex']], r[ix['militancyNationIndex']]))
    seen=set(); print('==',name)
    for o in out:
        k=o.split()[0]
        if k in seen: continue
        seen.add(k); print(' ',o)
