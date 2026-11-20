# (1272) 州の農民（と他の階級）の食べ物の充足・自給・稼ぎを 2 本の腕で並べ、飢えの死を式から見積もる
# 引数: al の cpop 抽出 基準の cpop 抽出 出力 階級名
import sys,csv,collections
FL,EX,K=0.8,0.9,4.0
def rd(p,cls):
    d={}
    for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t'):
        if r['className']!=cls or r['group']=='closed' and float(r['popK'])==0: continue
        q=int(r['quarter']); d.setdefault(q,[]).append(r)
    return d
cls=sys.argv[4]
A=rd(sys.argv[1],cls); B=rd(sys.argv[2],cls)
cols=['popK','foodFill','subsistenceRate','unemploymentRate','savings','earnedSettlePerQuarter','subsistenceValuePerQuarter','outMarketPerQuarter','outTaxPerQuarter','outColonialRentPerQuarter','laborOutput','topLaborGood','topLaborPrice']
def agg(rows):
    if not rows: return None
    w=sum(float(r['popK']) for r in rows)
    o={}
    for c in cols:
        if c in('popK','savings','earnedSettlePerQuarter','subsistenceValuePerQuarter','outMarketPerQuarter','outTaxPerQuarter','outColonialRentPerQuarter','laborOutput'):
            o[c]=sum(float(r[c]) for r in rows if r[c] not in('nan','-',''))
        elif c in('topLaborGood',): o[c]=rows[0][c]
        else:
            v=[(float(r[c]),float(r['popK'])) for r in rows if r[c] not in('nan','-','')]
            o[c]=sum(a*b for a,b in v)/w if w>0 and v else float('nan')
    ff=o['foodFill']; o['famEstK']=3*EX/12*((FL-ff)/FL)**K*o['popK'] if ff==ff and ff<FL else 0.0
    return o
out=open(sys.argv[3],'w',encoding='utf-8',newline='\n')
hdr=cols+['famEstK']
out.write('quarter\t'+'\t'.join(h+'_al\t'+h+'_b' for h in hdr)+'\n')
cumA=cumB=0.0
for q in sorted(set(A)|set(B)):
    a=agg(A.get(q,[])); b=agg(B.get(q,[]))
    def g(o,h): return '' if o is None else (o[h] if isinstance(o[h],str) else f"{o[h]:.6g}")
    out.write(str(q)+'\t'+'\t'.join(g(a,h)+'\t'+g(b,h) for h in hdr)+'\n')
    cumA+=a['famEstK'] if a else 0; cumB+=b['famEstK'] if b else 0
print('飢えの死の見積もりの累計 al',round(cumA,3),'基準',round(cumB,3))
