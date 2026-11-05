import sys
arms=[('B','D:/PoB_work/c71po_work/out_c71poB_10/ins'),('M852','D:/PoB_work/keep_852/base_ins'),('N1_852','D:/PoB_work/keep_852/n1'),('N2_852','D:/PoB_work/keep_852/n2')]
cols=['ruleStateShare','ruleLocalShare','ruleForeignShare','ruleUngovShare']
res={}
for name,d in arms:
    acc=[0.0]*4; w=0.0
    for s in range(1,11):
        with open('%s/rule_s%d.tsv'%(d,s),'rb') as f:
            h=f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix={c:i for i,c in enumerate(h)}
            for raw in f:
                r=raw.decode('utf-8').rstrip('\r\n').split('\t')
                try: p=float(r[ix['popK']])
                except ValueError: continue
                if not (p>0): continue
                v=[float(r[ix[c]]) for c in cols]
                if any(x!=x for x in v): continue
                w+=p
                for i in range(4): acc[i]+=p*v[i]
    res[name]=[a/w for a in acc]
    print('%s\t%s'%(name,'\t'.join('%.6f'%x for x in res[name])))
b=res['M852']
print('# 差（腕−M852）と、ヌル腕の幅 max(|N−M852|)')
wd=[max(abs(res['N1_852'][i]-b[i]),abs(res['N2_852'][i]-b[i])) for i in range(4)]
print('B−M852\t%s'%'\t'.join('%+.6f'%(res['B'][i]-b[i]) for i in range(4)))
print('幅\t%s'%'\t'.join('%.6f'%x for x in wd))
print('B÷M852\t%s'%'\t'.join('%.4f'%(res['B'][i]/b[i]) if b[i] else 'nan' for i in range(4)))
