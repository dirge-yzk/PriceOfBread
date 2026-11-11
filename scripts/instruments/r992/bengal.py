# 使い方: python bengal.py <出力> <州名> <名前>=<insフォルダ> ...   州の産出（工場＋採取の参照価格の値）と人口を年ごとに（q%40==0）
import csv,glob,os,sys
out=open(sys.argv[1],'w',encoding='utf-8'); reg=sys.argv[2]
for arg in sys.argv[3:]:
    name,d=arg.split('=',1)
    for p in sorted(glob.glob(os.path.join(d,'region_s*.tsv')), key=lambda p:int(os.path.basename(p)[8:-4])):
        s=int(os.path.basename(p)[8:-4]); L=[]
        with open(p,encoding='utf-8',newline='') as f:
            r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
            for x in r:
                if x[ix['region']]==reg and int(x[0])%60==0:
                    out_=float(x[ix['factoryOutputRefValuePerMonth']])+float(x[ix['extractionOutputRefValuePerMonth']])
                    L.append(f"{x[1]}:{x[ix['nation']]}:{x[ix['tier']]}:pop{float(x[ix['populationK']]):.0f}:out{out_:.0f}")
        out.write(f"{name} s{s} "+' '.join(L)+'\n')
out.close()
