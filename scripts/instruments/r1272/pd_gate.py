# (1272) PopDecision.Run の旧の式（UrbanFlowReal=0）を州の四半期の表から近似で組み直す（読むだけ・稼ぎは earnedSettlePerQuarter÷3＝月の近似）
# 引数: cpop の抽出 出力 年の始め 年の終わり
import sys,csv,collections,math
T,MOB,GATE=0.15,0.02,0.8
rows=collections.defaultdict(dict)
for r in csv.DictReader(open(sys.argv[1],encoding='utf-8'),delimiter='\t'):
    rows[int(r['quarter'])][r['popIndex']]=r
o=open(sys.argv[2],'w',encoding='utf-8',newline='\n')
o.write('quarter\tyear\truralK\turbanK\tincR\tincU\ttargetUrban\tcurUrban\tffR\tffU\tgateOn\tflowPerMonthK\n')
cnt=collections.Counter()
for q in sorted(rows):
    y=1783+q//4
    if not (int(sys.argv[3])<=y<=int(sys.argv[4])): continue
    R=rows[q].get('1'); U=rows[q].get('2')
    if not R or not U: continue
    sr,su=float(R['popK']),float(U['popK'])
    if sr<=0 or su<=0: continue
    ir=float(R['earnedSettlePerQuarter'])/3/sr; iu=float(U['earnedSettlePerQuarter'])/3/su
    z=max(-40,min(40,(iu-ir)/T)); tgt=1/(1+math.exp(-z)); cur=su/(sr+su)
    fl=MOB*(tgt-cur)*(sr+su); fr,fu=float(R['foodFill']),float(U['foodFill'])
    gate=fl>0 and fu<GATE and fu<fr
    if gate: fl=0
    cnt['q']+=1; cnt['gate']+=gate; cnt['toRural']+=fl<0; cnt['toUrban']+=fl>0; cnt['fuBelow']+=fu<GATE
    o.write(f"{q}\t{y}\t{sr:.4f}\t{su:.4f}\t{ir:.4f}\t{iu:.4f}\t{tgt:.4f}\t{cur:.4f}\t{fr:.4f}\t{fu:.4f}\t{int(gate)}\t{fl:.5f}\n")
o.close()
print(sys.argv[1],dict(cnt))
