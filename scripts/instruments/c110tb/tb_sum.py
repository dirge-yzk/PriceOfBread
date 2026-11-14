import glob,collections,sys
d=sys.argv[1]
fs=sorted(glob.glob(d+'/tb_s*.tsv'))
tot=collections.Counter(); rows=0; mx=0
for f in fs:
    L=open(f,encoding='utf-8').read().split('\n'); H=L[0].split('\t')
    for l in L[1:]:
        if not l: continue
        r=dict(zip(H,l.split('\t'))); rows+=1
        for k in H[3:]:
            if k=='u1MaxRel':
                try: mx=max(mx,float(r[k]))
                except: pass
            else: tot[k]+=float(r[k])
print('files',len(fs),'tablerows',rows)
for k in ['rows','unclassified','sk0','sk1','sk2','ps0','ps1','ps2','a1Close','smallD','smallDSkip','evalSkip','evalAll','evalDone','u1Deals','u1Rel','u1Abs','u1SmallD','u1SmallDRel','u1Gt1pct','u1Gt10pct','u1ExcessQty','u1Qty','impQ','rexQ','r4q','r4qImpGoods']:
    print(k,'%.6g'%tot[k],'perseed %.6g'%(tot[k]/max(1,len(fs))))
print('D-share of rows (dsk0,dsk1,dsk2,dps0,dps1,dps2 sums)',[ '%.6g'%tot[k] for k in ['dsk0','dsk1','dsk2','dps0','dps1','dps2']],'a1CloseD %.6g'%tot['a1CloseD'],'u1MaxRel',mx)
