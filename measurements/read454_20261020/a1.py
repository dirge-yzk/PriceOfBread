import csv,sys,statistics as st,collections,pickle
D='D:/PoB_work/keep_451/m451_ins/'
T=[];E=[]
for s in range(1,11):
    with open(D+f'engineer_s{s}.tsv',encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t');h=next(r)
        for x in r:
            def fl(v):
                try: return float(v)
                except: return float('nan')
            rec=(s,int(x[1]),int(x[2]),x[3],x[4],fl(x[6]),fl(x[7]),int(x[8]) if x[8] in('0','1') else -1,fl(x[9]),fl(x[10]),fl(x[11]))
            (T if x[0]=='T' else E).append(rec)
pickle.dump((T,E),open(sys.argv[1],'wb'))
print(len(T),len(E))
