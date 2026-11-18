# 紙の台本：基線の生の ideology_s から、国ごとに 1848〜1871 の四半期の行（picked の国）の民族主義・自由主義・ファシズムの国民の取り分の種の中央を読む。
import sys,glob,statistics as st,os
raw=sys.argv[1]; nations=sys.argv[2].split(','); y0,y1=int(sys.argv[3]),int(sys.argv[4])
res={}
for s in range(1,11):
    p=os.path.join(raw,f'ideology_s{s}.tsv')
    with open(p,encoding='utf-8') as fh:
        h=fh.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
        rows={}
        for line in fh:
            c=line.rstrip('\n').split('\t')
            n=c[ix['nation']]
            if n not in nations: continue
            y=int(c[ix['year']])
            if y<y0 or y>y1: continue
            rows.setdefault(n,[]).append((float(c[ix['ideoNationalNationalismShare']]),float(c[ix['ideoNationalLiberalismShare']]),float(c[ix['ideoNationalFascismShare']])))
    for n,v in rows.items():
        res.setdefault(n,[]).append((st.mean(x[0] for x in v),st.mean(x[1] for x in v),max(x[0] for x in v),len(v)))
print('nation\tseeds\trowsPerSeed\tnatMeanMed\tlibMeanMed\tnatMaxMed')
for n in nations:
    v=res.get(n,[])
    if not v: print(n,'none'); continue
    print(f"{n}\t{len(v)}\t{v[0][3]}\t{st.median(x[0] for x in v):.4f}\t{st.median(x[1] for x in v):.4f}\t{st.median(x[2] for x in v):.4f}")
