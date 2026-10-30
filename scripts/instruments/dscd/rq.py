import glob,re,statistics as st
spread={'特許','株式会社','社会主義','共産主義','帝国主義','ファシズム'}
for base in ['m685_ins','n1','n2']:
    out=[]
    for s in range(1,11):
        p=f'D:/PoB_work/keep_685/{base}/disc_s{s}.tsv'
        try: f=open(p,encoding='utf-8')
        except: continue
        rq=0; rq_all=0; per={}
        for line in f:
            x=line.rstrip('\n').split('\t')
            if x[0]!='E': continue
            q=int(x[1]); row=x[4]; nat=x[10]
            rq_all+=668-q
            if row in spread: continue
            rq+=668-q; per[nat]=per.get(nat,0)+1
        out.append((s,rq,rq_all,st.median(per.values()) if per else 0))
    print(base,' '.join(f's{s}:{rq/1e3:.0f}k/{ra/1e3:.0f}k/med{m}' for s,rq,ra,m in out))
    print('  mean rowxq(non-spread)',round(st.mean(o[1] for o in out)/1e3),'k')
