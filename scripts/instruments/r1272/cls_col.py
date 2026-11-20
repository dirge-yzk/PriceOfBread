# (1272) 州×2 年ごとに 階級（工場労働者・農民・兵士）の人数と食べ物の充足を書き出す（州の格は region の同じ四半期から）
# 引数: 生のフォルダ 種の始め 種の終わり 出力
import sys
d,s0,s1,out=sys.argv[1],int(sys.argv[2]),int(sys.argv[3]),sys.argv[4]
CL={'工場労働者':'fw','農民':'pe','兵士':'so'}
o=open(out,'w',encoding='utf-8',newline='\n')
o.write('seed\tquarter\tyear\tregion\tnation\ttier\tpopK\tfwK\tpeK\tsoK\tffFw\tffPe\tffAll\n')
for s in range(s0,s1+1):
    tier={}; popr={}
    with open(f'{d}/region_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
        for line in f:
            c=line.rstrip('\n').split('\t'); q=int(c[0])
            if q%8!=3: continue
            tier[(q,c[ix['region']])]=(c[ix['nation']],c[ix['tier']]); popr[(q,c[ix['region']])]=float(c[ix['populationK']])
    acc={}
    with open(f'{d}/cpop_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
        iq,ir,ic,ip,iff=ix['quarter'],ix['region'],ix['className'],ix['popK'],ix['foodFill']
        for line in f:
            p=line.find('\t')
            if int(line[:p])%8!=3: continue
            c=line.rstrip('\n').split('\t')
            k=(int(c[iq]),c[ir]); a=acc.setdefault(k,{'fw':[0,0],'pe':[0,0],'so':[0,0],'all':[0,0]})
            sz=float(c[ip])
            try: ff=float(c[iff])
            except ValueError: ff=float('nan')
            if sz<=0: continue
            g=CL.get(c[ic])
            if g: a[g][0]+=sz
            if ff==ff:
                if g: a[g][1]+=sz*ff
                if g!='so': a['all'][0]+=sz; a['all'][1]+=sz*ff
    for (q,r),a in sorted(acc.items()):
        if (q,r) not in tier: continue
        n,t=tier[(q,r)]
        fr=lambda g:(a[g][1]/a[g][0]) if a[g][0]>0 else float('nan')
        o.write(f"{s}\t{q}\t{1783+q//4}\t{r}\t{n}\t{t}\t{popr[(q,r)]:.4f}\t{a['fw'][0]:.4f}\t{a['pe'][0]:.4f}\t{a['so'][0]:.4f}\t{fr('fw'):.4f}\t{fr('pe'):.4f}\t{fr('all'):.4f}\n")
o.close()
