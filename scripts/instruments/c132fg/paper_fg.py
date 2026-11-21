# (1283) c132fg 紙の台本（読むだけ）：飢えの門（PopDecision.cs の flow>0・都市の充足<0.8・都市<農村）が掛かる州×月の数と、その中の g<0 の割合。
#  本国格の州＝urxpd_s（エンジンの式の写し UrxPdDrive の月ごとの数・famineGated・9 国）。g の符号の内訳は urxpd に無い＝植民地格の州だけ cpop_s から四半期末の値で近似（r1272/pd_gate.py の型）。
#  近似の名乗り：稼ぎ＝earnedSettlePerQuarter÷3・自給の値＝subsistenceValuePerQuarter÷3・家賃は表に無い＝0（vu を大きく見る＝g<0 の割合は下側の見積もり）・対＝popIndex 1（農民）と 2（工場労働者）。
# 使い方: python paper_fg.py <ins の生> <種...>
import sys,math,collections
T,MOB,GATE=0.15,0.02,0.8
D=sys.argv[1]; seeds=[int(s) for s in sys.argv[2:]]
def per(y):
    return '1783-1799' if y<1800 else '1800-1849' if y<1850 else '1850-1899' if y<1900 else '1900-1950'
tot=collections.Counter()
for s in seeds:
    # 本国格（urxpd）
    f=open(f'{D}/urxpd_s{s}.tsv',encoding='utf-8'); h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
    for line in f:
        c=line.rstrip('\n').split('\t'); y=int(c[ix['year']]); p=per(y)
        rm=int(c[ix['regionMonths']]); fg=int(c[ix['famineGated']])
        tot[('core_rm',p)]+=rm; tot[('core_gate',p)]+=fg; tot[('core_rm','all')]+=rm; tot[('core_gate','all')]+=fg
    f.close()
    # 植民地格（cpop・四半期末の近似）
    f=open(f'{D}/cpop_s{s}.tsv',encoding='utf-8'); h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
    iq,iy,ir,ip,ik,ie,isb,iff=ix['quarter'],ix['year'],ix['region'],ix['popIndex'],ix['popK'],ix['earnedSettlePerQuarter'],ix['subsistenceValuePerQuarter'],ix['foodFill']
    cur={}; curq=None
    def flush(q,y,rows):
        p=per(y)
        for reg,(R,U) in rows.items():
            if R is None or U is None: continue
            sr,su=R[0],U[0]
            if not(sr>0 and su>0): continue
            er,eu=R[1]/3/sr,U[1]/3/su; sub=R[2]/3/sr
            zz=max(-40,min(40,(eu-er)/T)); tgt=1/(1+math.exp(-zz)); cu=su/(sr+su)
            fl=MOB*(tgt-cu)*(sr+su); fr,fu=R[3],U[3]
            tot[('col_q',p)]+=1; tot[('col_q','all')]+=1
            if fl>0 and fu<GATE and fu<fr:
                vr=max(er,0)+max(sub,0); vu=max(eu,0); sm=vr+vu; g=2*(vu-vr)/sm if sm>1e-9 else 0.0
                for k in (p,'all'):
                    tot[('col_gate',k)]+=1
                    if g<0: tot[('col_gneg',k)]+=1; tot[('col_retK',k)]+=MOB*(-g)*su*3; tot[('col_gsum',k)]+=g
                    tot[('col_urbK',k)]+=su
    for line in f:
        c=line.rstrip('\n').split('\t')
        pi=c[ip]
        if pi!='1' and pi!='2': continue
        q=int(c[iq])
        if q!=curq:
            if curq is not None: flush(curq,cy,cur)
            cur={}; curq=q; cy=int(c[iy])
        try: v=(float(c[ik]),float(c[ie]),float(c[isb]),float(c[iff]))
        except ValueError: continue
        reg=(c[ix['nation']],c[ir]); a=cur.setdefault(reg,[None,None])
        a[0 if pi=='1' else 1]=v
    if curq is not None: flush(curq,cy,cur)
    f.close()
    print(f'seed {s} done',file=sys.stderr)
print(f'# paper_fg 種 {seeds[0]}..{seeds[-1]}（{D}）')
print('期間\t本国の州×月\t門の州×月\t門の割合\t植民地の州×四半期\t門の州×四半期\tうち g<0\tg<0 の割合\t戻る量の見積もり千人（四半期×3 か月）\t門の四半期の都市の規模の和千人\tg<0 の平均 g')
for p in ['1783-1799','1800-1849','1850-1899','1900-1950','all']:
    rm=tot[('core_rm',p)]; fg=tot[('core_gate',p)]; cq=tot[('col_q',p)]; cg=tot[('col_gate',p)]; gn=tot[('col_gneg',p)]
    print(f"{p}\t{rm}\t{fg}\t{fg/rm if rm else float('nan'):.4f}\t{cq}\t{cg}\t{gn}\t{gn/cg if cg else float('nan'):.4f}\t{tot[('col_retK',p)]:.1f}\t{tot[('col_urbK',p)]:.1f}\t{tot[('col_gsum',p)]/gn if gn else float('nan'):.4f}")
