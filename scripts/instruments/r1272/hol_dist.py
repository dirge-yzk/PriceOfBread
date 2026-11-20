# (1272) 州ごとのピーク→ピークより後の最小の比を種ごとに数える（an_h2 の型の近似・自前の定義）
# 引数: 生のフォルダ 種の始め 種の終わり 出力
import sys
d,s0,s1,out=sys.argv[1],int(sys.argv[2]),int(sys.argv[3]),sys.argv[4]
o=open(out,'w',encoding='utf-8',newline='\n')
o.write('seed\tregion\tnationLast\ttierLast\tpeakK\tpeakYear\tminK\tminYear\tratio\tfoodMean\tnationsSeen\n')
for s in range(s0,s1+1):
    ser={}
    with open(f'{d}/region_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
        for line in f:
            c=line.rstrip('\n').split('\t')
            ser.setdefault(c[ix['region']],[]).append((int(c[ix['quarter']]),int(c[ix['year']]),float(c[ix['populationK']]),float(c[ix['foodFillShare']]),c[ix['nation']],c[ix['tier']]))
    for r,v in ser.items():
        v.sort(); pk=max(range(len(v)),key=lambda i:v[i][2])
        if v[pk][2]<1.0: continue
        mn=min(range(pk,len(v)),key=lambda i:v[i][2])
        fm=sum(x[3] for x in v[pk:mn+1])/(mn-pk+1)
        ns=len({x[4] for x in v})
        o.write(f"{s}\t{r}\t{v[-1][4]}\t{v[-1][5]}\t{v[pk][2]:.4f}\t{v[pk][1]}\t{v[mn][2]:.4f}\t{v[mn][1]}\t{v[mn][2]/v[pk][2]:.5f}\t{fm:.4f}\t{ns}\n")
o.close()
