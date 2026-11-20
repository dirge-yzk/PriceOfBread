# (1272) 紙の見積もり：PopDecision の飢えの門が掛かった月に flow=0 の代わりに MigrationValues の g（自給の値を含む・住居は 0 と近似）で都市→農村へ戻すと、
# 四半期の表の上で何千人が戻るか（一次の近似・戻った人の後の効きは数えない）。引数: cpop の抽出 年の始め 年の終わり
import sys,csv,collections,math
T,MOB,GATE=0.15,0.02,0.8
rows=collections.defaultdict(dict)
for r in csv.DictReader(open(sys.argv[1],encoding='utf-8'),delimiter='\t'):
    rows[int(r['quarter'])][r['popIndex']]=r
back=0.0; n=0; nrev=0; fam=0.0
for q in sorted(rows):
    y=1783+q//4
    if not (int(sys.argv[2])<=y<=int(sys.argv[3])): continue
    R=rows[q].get('1'); U=rows[q].get('2')
    if not R or not U: continue
    sr,su=float(R['popK']),float(U['popK'])
    if sr<=0 or su<=0: continue
    ir=float(R['earnedSettlePerQuarter'])/3/sr; iu=float(U['earnedSettlePerQuarter'])/3/su; sub=float(R['subsistenceValuePerQuarter'])/3/sr
    z=max(-40,min(40,(iu-ir)/T)); fl=MOB*(1/(1+math.exp(-z))-su/(sr+su))*(sr+su)
    fr,fu=float(R['foodFill']),float(U['foodFill'])
    if fl>0 and fu<GATE and fu<fr:
        n+=1; vr=max(0,ir)+max(0,sub); vu=max(0,iu); s=vr+vu; gsym=2*(vu-vr)/s if s>1e-9 else 0
        if gsym<0: nrev+=1; back+=3*MOB*(-gsym)*su
        fam+=3*0.9/12*((GATE-fu)/GATE)**4*su
print(f"{sys.argv[1]} {sys.argv[2]}〜{sys.argv[3]}: 門の四半期 {n}・g<0（戻る向き）{nrev}・戻る量の和（千人・一次）{back:.2f}・同じ窓の工場労働者の飢えの死の式の和（千人・四半期末の充足で）{fam:.2f}")
