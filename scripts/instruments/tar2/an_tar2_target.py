import sys,csv,statistics as st,collections,glob,os
sys.stdout.reconfigure(encoding='utf-8')
root=sys.argv[1]
nat={'英':'イギリス','仏':'フランス','普':'プロイセン','露':'ロシア','墺':'オーストリア','土':'オスマン','日':'日本','米':'アメリカ','清':'清'}
files=sorted(glob.glob(os.path.join(root,'scalars_s*.tsv')))
data=[]
for f in files:
    with open(f,encoding='utf-8',newline='') as fh:
        r=csv.reader(fh,delimiter='\t'); hdr=next(r)
        iy=hdr.index('year'); inn=hdr.index('nation'); it=hdr.index('tariffTargetIndex'); itr=hdr.index('tradeToGdpRatio')
        d=collections.defaultdict(list)
        for row in r:
            d[row[inn]].append((int(row[iy]),float(row[it]),float(row[itr])))
        data.append(d)
eras=[(1783,1840),(1840,1860),(1860,1880),(1880,1914),(1914,1951)]
def agg(v): return st.median(v) if v else float('nan')
print(root,len(files),'seeds')
for k,nm in nat.items():
    line=[]
    for (a,b) in eras:
        meds=[];f9=[];fd=[];tg=[]
        for d in data:
            seq=[(t,g) for (y,t,g) in d.get(nm,[]) if a<=y<b]
            if seq:
                xs=[t for t,g in seq]
                meds.append(st.median(xs)); f9.append(sum(1 for x in xs if x>=0.9)/len(xs))
                fd.append(sum(1 for x in xs if abs(x-0.2)<1e-9 or abs(x-0.3)<1e-9)/len(xs)); tg.append(st.median([g for t,g in seq]))
        line.append('%d-%d T=%.2f f9=%.2f fdoc=%.2f tg=%.3f'%(a,b,agg(meds),agg(f9),agg(fd),agg(tg)))
    print(k,' | '.join(line))
up=dn=eq=tot=0; nn=0
for d in data:
    for nm,seq in d.items():
        if nm not in nat.values(): continue
        for i in range(1,len(seq)):
            x=seq[i][1]-seq[i-1][1]; tot+=1
            if x>1e-9: up+=1
            elif x<-1e-9: dn+=1
            else: eq+=1
print('9国 target changes: up',up,'down',dn,'flat',eq,'total',tot, 'down/up=%.2f'%(dn/max(up,1)))
# 追加：目標が 0.28 付近（0.3×潤沢の倍率の教義の床）にいる割合・9国の era 中央値の散らばり（母標準偏差）・欧州主要国の 1860-79 年の中央値
def frac28(d):
    n=k=0
    for nm in nat.values():
        for (y,t,g) in d.get(nm,[]):
            n+=1; k+= (0.27<=t<=0.29)
    return k/max(n,1)
print('target in [0.27,0.29] share (9国, per seed median): %.3f'%agg([frac28(d) for d in data]))
for (a,b) in eras:
    sds=[]
    for d in data:
        meds=[st.median([t for (y,t,g) in d[nm] if a<=y<b]) for nm in nat.values() if any(a<=y<b for (y,t,g) in d.get(nm,[]))]
        sds.append(st.pstdev(meds))
    print('spread of 9 era-medians %d-%d: %.3f'%(a,b,agg(sds)))
