# (1270) c130rc 紙の台本：招聘の着いた州（popclose_s の specialistInK）を region_s の同じ四半期の州と突き合わせる。読むだけ。
# 使い方: paper_rc.py <生のフォルダ> <種の始め> <種の終わり>
# 出す物: 格ごとの着いた量・着いた州に工場が無い割合（空席の近似＝工場の数 0）・着いた州が国の工場の産出の最大の州か・国に工場が 1 つも無い四半期の着いた量・
#         国ごとの技師（class_s の技師の sizeK・1851・1913 年・種の中央）。空席そのもの（高度労働力の LastShortage）は表に無い＝近似と名乗る。
import sys,csv,statistics
d,s0,s1=sys.argv[1],int(sys.argv[2]),int(sys.argv[3])
def rd(p):
    f=open(p,encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); return {c:i for i,c in enumerate(h)},r,f
tot={'Core':0.0,'Integrated':0.0,'Colonial':0.0}; nofac=0.0; atmax=0.0; natnofac=0.0; allin=0.0; ev=0; regs=set()
eng={}
for s in range(s0,s1+1):
    ix,r,f=rd(f"{d}/region_s{s}.tsv")
    R={}; NM={}; NF={}
    for x in r:
        q=int(x[ix['quarter']]); n=x[ix['nation']]; g=x[ix['region']]
        fo=float(x[ix['factoryOutputRefValuePerMonth']]); fc=int(float(x[ix['factoryActiveCount']]))
        R[(q,g)]=(fo,fc); k=(q,n)
        if k not in NM or fo>NM[k][0]: NM[k]=(fo,g)
        NF[k]=NF.get(k,0)+fc
    f.close()
    ix,r,f=rd(f"{d}/popclose_s{s}.tsv")
    for x in r:
        v=float(x[ix['specialistInK']])
        if not v>0: continue
        q=int(x[ix['quarter']]); n=x[ix['nation']]; g=x[ix['region']]; t=x[ix['tier']]
        tot[t]=tot.get(t,0)+v; allin+=v; ev+=1; regs.add((s,g))
        fo,fc=R.get((q,g),(float('nan'),-1))
        if fc==0: nofac+=v
        if NM.get((q,n),(0,None))[1]==g: atmax+=v
        if NF.get((q,n),1)==0: natnofac+=v
    f.close()
    ix,r,f=rd(f"{d}/class_s{s}.tsv")
    for x in r:
        if x[ix['className']]!='技師': continue
        y=int(x[ix['year']])
        if y in (1851,1913) and x[ix['time']].startswith('q'): eng.setdefault((x[ix['nation']],y),{})[s]=float(x[ix['sizeK']])
    f.close()
print(f"# paper_rc 生={d} 種={s0}..{s1}")
print(f"着いた量の合計K\t{allin:.1f}\t行の数\t{ev}\t着いた州（種×州）\t{len(regs)}")
for t,v in tot.items(): print(f"格\t{t}\t{v:.1f}\t割合\t{v/allin if allin>0 else float('nan'):.4f}")
print(f"着いた州に工場が無い（空席 0 の近似）\t{nofac:.1f}\t割合\t{nofac/allin if allin>0 else float('nan'):.4f}")
print(f"着いた州が国の工場の産出の最大の州\t{atmax:.1f}\t割合\t{atmax/allin if allin>0 else float('nan'):.4f}")
print(f"国に工場が 1 つも無い四半期に着いた\t{natnofac:.1f}\t割合\t{natnofac/allin if allin>0 else float('nan'):.4f}")
print("国\t年\t技師K（種の中央）\t種の数")
for (n,y) in sorted(eng):
    v=list(eng[(n,y)].values()); print(f"{n}\t{y}\t{statistics.median(v):.1f}\t{len(v)}")
