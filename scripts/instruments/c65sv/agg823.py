import csv, glob, statistics as st, collections
tot=0; mintshare=[]; law2_t1=0; t1rank1=0; nat1=collections.Counter(); natR=collections.Counter(); nrows=0
enterRows=collections.Counter(); bigEnter=[]
for f in sorted(glob.glob('D:/PoB_work/ident_c65sv/out_ib_10/ins/silverst_s*.tsv')):
    with open(f,encoding='utf-8') as fh:
        for r in csv.DictReader(fh,delimiter='\t'):
            nrows+=1
            k=r['kind']
            if k=='T1' and r['rank']=='1':
                t1rank1+=1; nat1[(r['nation'],r['law'])]+=1
            if k=='TR' and r['rank']=='1':
                natR[(r['nation'],r['law'])]+=1
            if k in('T1',) and r['law']=='2' and float(r['dAu'])>0 and float(r['mintMonthsAu'])>0:
                mintshare.append(float(r['mintQtyAu'])/max(1,float(r['mintMonthsAu']))/float(r['dAu']))
            if k=='E':
                enterRows[r['law']]+=1
                if float(r['w1Share'])>0.1: bigEnter.append((f[-8:],r['seq'],r['year'],r['nation'],r['region'],round(float(r['w1Share']),3),float(r['minRefShare']),float(r['dAg'])))
print('行',nrows)
print('T1 の 1 位の国と法（法 2=金本位 1=銀本位 …）',nat1.most_common(8))
print('TR の 1 位の国と法',natR.most_common(8))
print('金本位の州の T1 の行で 造幣の月平均の引き取り÷金鉱の需要: 件数',len(mintshare),'p10',st.quantiles(mintshare,n=10)[0],'中央',st.median(mintshare),'p90',st.quantiles(mintshare,n=10)[8])
print('入った州の法の別',enterRows)
print('入った州で w1 の票の割合が 0.1 超',len(bigEnter))
c=collections.Counter((b[3],b[4]) for b in bigEnter); print(c.most_common(10))
print('その dAg の中央',st.median(b[7] for b in bigEnter),'最大',max(b[7] for b in bigEnter),' A′票の割合 最大',max(b[6] for b in bigEnter))
yrs=collections.Counter(b[2] for b in bigEnter); print(sorted(yrs.items())[:12], '...')
