import csv,sys,glob,os,statistics as st,collections
sys.stdout.reconfigure(encoding='utf-8')
root=sys.argv[1]
res=[]
for f in sorted(glob.glob(os.path.join(root,'nation_s*.tsv'))):
    fh=open(f,encoding='utf-8',newline=''); r=csv.reader(fh,delimiter='\t'); h=next(r)
    i=h.index('openFrontStates'); j=h.index('tariffPaidInQuarter')
    n=op=paid=0; tot=0.0; codes=collections.Counter()
    for row in r:
        n+=1
        for tok in row[i].split(';'):
            if tok.startswith('T.Tariff:'):
                op+=1; codes[tok.split(':')[1]]+=1
        v=float(row[j]); tot+=v; paid+= v>0
    res.append((op/n,paid/n,tot/n,codes))
print(os.path.basename(root),'T戦線が開いていた割合(9国×全四半期・種の中央値) %.4f'%st.median([a for a,_,_,_ in res]),
      '| 関税に政治資本を払った四半期の割合 %.5f'%st.median([b for _,b,_,_ in res]),
      '| 払った額の平均/四半期 %.3e'%st.median([c for _,_,c,_ in res]),
      '| 開いていた戦線の結果コード(全種の和)',dict(sum((x[3] for x in res),collections.Counter())))
