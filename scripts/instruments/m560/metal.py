import csv,glob,os,sys,statistics as st
def rows(p):
    with open(p,encoding='utf-8',newline='') as f:
        r=csv.reader(f,delimiter='\t');h=next(r)
        for x in r:
            if len(x)==len(h): yield dict(zip(h,x))
Y=[1800,1820,1840,1860,1880,1900,1913,1930]
for d in sys.argv[1:]:
    acc={y:[] for y in Y}; accS={y:[] for y in Y}
    for p in glob.glob(os.path.join(d,'metal_s*.tsv')):
        for x in rows(p):
            y=int(x['year'])
            if y in acc and int(x['quarter'])%4==3: acc[y].append(int(x['nGoldLaw'])); accS[y].append(int(x['nSilverLaw']))
    print(d,' 金の法の国（中央値）', ' '.join(f'{y}:{st.median(acc[y]) if acc[y] else "-"}' for y in Y))
    print('   銀の法の国', ' '.join(f'{y}:{st.median(accS[y]) if accS[y] else "-"}' for y in Y))
