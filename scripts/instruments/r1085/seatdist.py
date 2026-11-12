"""(1085) 選挙政体の年末の行の議席の分布と参政権の段（読むだけ）。出力＝標準出力（out_seatdist.txt へ写した）。
使い方: python -X utf8 -I seatdist.py > out_seatdist.txt
"""
import csv
ARMS=[("m1069","D:/PoB_work/keep_1078/base_ins",range(1,11)),("n1","D:/PoB_work/keep_1069/n1",range(1,11)),("n2","D:/PoB_work/keep_1069/n2",range(1,11)),("s11","D:/PoB_work/keep_1069/a11",range(11,21))]
def rd(p,cols):
    with open(p,encoding="utf-8",newline="") as f:
        r=csv.reader(f,delimiter="\t");h=next(r);ix=[h.index(c) for c in cols]
        for row in r: yield [row[i] for i in ix]
def qt(x,p):
    x=sorted(x);return x[min(len(x)-1,int(p*len(x)))]
print("arm\tlib_med\tlib_p10\tmax_med\tmax_gt_0.6\tmax_gt_0.5\tsuff_by_el(0/1/2)")
for arm,d,seeds in ARMS:
    lib=[];mx=[];sf={}
    for s in seeds:
        pol={};suf={}
        for q,n,pc,su in rd(f"{d}/scalars_s{s}.tsv",["quarter","nation","polityCode","suffrage"]):
            if int(q)%4==3: pol[(int(q),n)]=int(float(pc));suf[(int(q),n)]=int(float(su))
        for q,n,a,b,c in rd(f"{d}/sf_s{s}.tsv",["quarter","nation","seatCons","seatLib","seatSoc"]):
            if int(q)%4!=3: continue
            k=(int(q),n)
            if pol.get(k) not in (3,4): continue
            st=(float(a),float(b),float(c))
            if sum(st)<=1e-9: continue
            lib.append(st[1]);mx.append(max(st));sf[suf[k]]=sf.get(suf[k],0)+1
    t=sum(sf.values())
    print(f"{arm}\t{qt(lib,.5):.3f}\t{qt(lib,.1):.3f}\t{qt(mx,.5):.3f}\t{sum(1 for v in mx if v>0.6)/len(mx):.3f}\t{sum(1 for v in mx if v>0.5)/len(mx):.3f}\t"+"/".join(f"{sf.get(i,0)/t:.3f}" for i in range(4)))
