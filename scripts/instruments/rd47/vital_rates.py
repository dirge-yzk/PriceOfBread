"""(698) rd47：1 国の国全体（pop_s の N 行）の粗出生率・粗死亡率・年齢の層の死亡率・合計出生率の代理を 20 四半期ごとに並べる（読むだけ）。
使い方：python -X utf8 -I vital_rates.py <計器の出力フォルダ> <種> <国の名（開幕の名）>
"""
import csv,sys,collections
csv.field_size_limit(10**9)
d,s,nat=sys.argv[1],sys.argv[2],sys.argv[3]
cols=['popK','cbrPerMille','cdrPerMille','cdrNormalPerMille','share0to14','share60plus','muFrom0PerYear','muFrom21PerYear','muFrom41PerYear','muFrom61PerYear','l60Years','tfrProxy','basketPerMonth','yAdultPerMonth']
U=None
with open(f'{d}/pop_s{s}.tsv',encoding='utf-8',newline='') as f:
    r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
    print('year\t'+'\t'.join(c[:10] for c in cols))
    for x in r:
        if len(x)!=len(h) or x[0]!='N': continue
        if U is None and x[ix['nation']]==nat and x[ix['quarter']]=='0': U=x[ix['nationUid']]
        if x[ix['nationUid']]!=U: continue
        q=int(x[ix['quarter']])
        if q%20==0: print(str(1783+q//4)+'\t'+'\t'.join(x[ix[c]][:7] for c in cols))
