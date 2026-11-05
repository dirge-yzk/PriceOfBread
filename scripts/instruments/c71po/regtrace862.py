import sys
reg=sys.argv[1]; seed=sys.argv[2]
for name,d in [('B','D:/PoB_work/c71po_work/out_c71poB_10/ins'),('M852','D:/PoB_work/keep_852/base_ins'),('N1_852','D:/PoB_work/keep_852/n1'),('N2_852','D:/PoB_work/keep_852/n2'),('N1_839','D:/PoB_work/keep_839/n1'),('N2_839','D:/PoB_work/keep_839/n2')]:
    p='%s/region_s%s.tsv'%(d,seed)
    with open(p,'rb') as f:
        h=f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix={c:i for i,c in enumerate(h)}
        ser=[]
        for raw in f:
            r=raw.decode('utf-8').rstrip('\r\n').split('\t')
            if r[ix['region']]==reg: ser.append((float(r[ix['year']]),r[ix['nation']],float(r[ix['populationK']])))
    if not ser: print(name,'none'); continue
    pk=max(ser,key=lambda t:t[2]); after=[t for t in ser if t[0]>=pk[0]]; mn=min(after,key=lambda t:t[2])
    last=ser[-1]
    nat_at={y:n for y,n,_ in ser}
    print('%s s%s ピーク %.1f(%d・%s) その後の最小 %.1f(%d・%s) 比 %.3f 最後 %.1f(%d・%s)'%(name,seed,pk[2],pk[0],pk[1],mn[2],mn[0],mn[1],mn[2]/pk[2],last[2],last[0],last[1]))
