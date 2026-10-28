import csv,sys,glob,os,statistics as st
sys.stdout.reconfigure(encoding='utf-8')
root=sys.argv[1]; out=[]
for f in sorted(glob.glob(os.path.join(root,'nation_s*.tsv'))):
    fh=open(f,encoding='utf-8',newline=''); r=csv.reader(fh,delimiter='\t'); h=next(r); i=h.index('openFrontStates')
    tot=c2=0; nrows=0; nfr=0
    for row in r:
        nrows+=1
        for tok in row[i].split(';'):
            if not tok or tok.startswith('T.Tariff:'): continue
            tot+=1; nfr+=1; c2+= tok.endswith(':2')
    out.append((c2/max(tot,1), nfr/nrows))
print(os.path.basename(root),'関税以外の戦線のうち在庫不足(コード2)の割合(種の中央値) %.4f'%st.median([a for a,_ in out]),'| 関税以外の開いた戦線の数/四半期 %.2f'%st.median([b for _,b in out]))
