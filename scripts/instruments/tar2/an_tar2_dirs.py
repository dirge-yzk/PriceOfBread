import sys,csv,statistics as st,collections,glob,os
sys.stdout.reconfigure(encoding='utf-8')
root=sys.argv[1]
nat9=['イギリス','フランス','プロイセン','ロシア','オーストリア','オスマン','日本','アメリカ','清']
eras=[(1783,1840),(1840,1860),(1860,1880),(1880,1914),(1914,1951)]
files=sorted(glob.glob(os.path.join(root,'scalars_s*.tsv')))
hi=lo=n=0; per=[]; both=[]; dips=[]
for f in files:
    d=collections.defaultdict(list)
    with open(f,encoding='utf-8',newline='') as fh:
        r=csv.reader(fh,delimiter='\t'); hdr=next(r)
        iy=hdr.index('year'); inn=hdr.index('nation'); it=hdr.index('tariffTargetIndex')
        for row in r: d[row[inn]].append((int(row[iy]),float(row[it])))
    h=l=m=0
    for nm,seq in d.items():
        for (y,t) in seq:
            m+=1; h+= t>=0.9; l+= t<=0.01
    per.append((h/m,l/m))
    # 9国×era に上げと下げの両方がある数
    c=0
    for nm in nat9:
        seq=d[nm]
        for (a,b) in eras:
            xs=[t for (y,t) in seq if a<=y<b]
            u=sum(1 for i in range(1,len(xs)) if xs[i]-xs[i-1]>1e-9); dn=sum(1 for i in range(1,len(xs)) if xs[i]-xs[i-1]<-1e-9)
            c+= (u>0 and dn>0)
    both.append(c)
    # 欧州6国の 1860-79 谷＋揺り戻し
    k=0
    for nm in ['イギリス','フランス','プロイセン','オーストリア','ロシア','オスマン']:
        med=[st.median([t for (y,t) in d[nm] if a<=y<b]) for (a,b) in eras]
        k+= (med[2]<med[1] and med[3]>med[2])
    dips.append(k)
print(os.path.basename(root),'target>=0.9 share (全国・全行) median %.4f  <=0.01 share median %.4f'%(st.median([x for x,_ in per]),st.median([y for _,y in per])),
      '| 9国x5時代=45マスで上げと下げ両方あり（種の中央値）',st.median(both),'min',min(both),'| 欧州6国で谷＋揺り戻し（種の中央値）',st.median(dips))
