# (1417) 旗 0 の盤面の強さの比（結束の門を越えた勢力の行の最大）を k 倍に縮めたら、比>1 の行が何行残るか（強さの道だけの反実仮想・足切りと結束は旗 0 のまま）
import csv
def load(d, seeds):
    out=[]
    for s in seeds:
        for x in csv.DictReader(open(f'{d}/dcont_s{s}.tsv', newline='', encoding='utf-8'), delimiter='\t'):
            out.append((s, x['nation'], float(x['facCohGateCount']), float(x['strengthRatioMaxIndex']), float(x['factionRevoltsCum'])))
    return out
b=load('D:/PoB_work/keep_1411/base_ins', range(1,11)); a=load('D:/PoB_work/keep_1411_rej/c150sdB/ins', range(1,11))
rb=[r for _,_,g,r,_ in b if g>0]; ra=[r for _,_,g,r,_ in a if g>0]
print('flag0 strong rows', sum(1 for r in rb if r>1), ' flag1 strong rows', sum(1 for r in ra if r>1))
for k in (0.25,0.31,0.37,0.5,0.69,1.0):
    print(f'k={k}: flag0 gate rows with ratio*k>1 : {sum(1 for r in rb if r*k>1)}')
# 国ごとの蜂起の最終値（factionRevoltsCum の国の最大）の和
def rev(rows):
    m={}
    for s,n,_,_,c in rows: m[(s,n)]=max(m.get((s,n),0),c)
    return sum(m.values()), sum(1 for v in m.values() if v>0)
print('revolts sum, nations>0 : flag0', rev(b), ' flag1', rev(a))
# 結束の門を越えた行（国×四半期）を国で数える：旗 0 で門の行があった（種,国）が旗 1 で残ったか
def gn(rows):
    m={}
    for s,n,g,_,_ in rows:
        if g>0: m[(s,n)]=m.get((s,n),0)+1
    return m
gb=gn(b); ga=gn(a)
print('seed-nations with gate rows: flag0', len(gb), ' flag1', len(ga), ' both', len(set(gb)&set(ga)))
top=sorted(gb.items(), key=lambda kv:-kv[1])[:8]
print('top flag0 (seed,nation):rows -> flag1 rows', [(k, v, ga.get(k,0)) for k,v in top])
# 追記：強さを k 倍に〈増やす〉形の紙の数（強さの式を変える案の桁の目安・足切りと結束は旗 0 のまま）
for k in (2,3,4.7,7.7,10):
    print(f'k={k}: flag0 gate rows with ratio*k>1 : {sum(1 for r in rb if r*k>1)}')
