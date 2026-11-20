# 州の四半期の軌道と内訳（region 表だけ）。使い方: traj.py <region_sN.tsv> <州名> [年の始め] [年の終わり] [刻み]
import sys,csv
p,name=sys.argv[1],sys.argv[2]
y0=int(sys.argv[3]) if len(sys.argv)>3 else 0; y1=int(sys.argv[4]) if len(sys.argv)>4 else 9999
step=int(sys.argv[5]) if len(sys.argv)>5 else 1
f=open(p,encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
rows=[x for x in r if x[ix['region']]==name]
prev=None; out=[]
for x in rows:
    q=int(x[ix['quarter']]); y=int(x[ix['year']]); P=float(x[ix['populationK']])
    mi=float(x[ix['migrationInKPerQuarter']]); mo=float(x[ix['migrationOutKPerQuarter']])
    xi=float(x[ix['crossBorderInKPerQuarter']]); xo=float(x[ix['crossBorderOutKPerQuarter']])
    d=None if prev is None else P-prev[1]
    res=None if d is None else d-(mi-mo+xi-xo)
    own=x[ix['nation']]; chg='' if prev is None or prev[2]==own else '*持ち主の移り'
    if y0<=y<=y1 and (q%step==0 or chg):
        out.append(f"{q}\t{y}\t{own}\t{x[ix['tier']]}\t{P:.2f}\t{'' if d is None else f'{d:.3f}'}\t{mi:.3f}\t{mo:.3f}\t{xi:.3f}\t{xo:.3f}\t{'' if res is None else f'{res:.3f}'}\t{x[ix['ruralPopulationK']][:7]}\t{x[ix['foodFillShare']][:5]}\t{x[ix['warIntensity']][:5]}\t{x[ix['employmentShare']][:5]}\t{chg}")
    prev=(q,P,own)
sys.stdout.write("q\tyear\towner\ttier\tpopK\tdP\tmigIn\tmigOut\txIn\txOut\tresid(自然増+他)\trural\tfood\twar\temp\t印\n"+"\n".join(out)+"\n")
