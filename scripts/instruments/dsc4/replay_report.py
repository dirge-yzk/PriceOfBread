# 発見の式の作り直し（dsc4）：census の再生の報告（K の規則・狙いの数字の紙の上の値）。使い方：python replay_report.py <dscx_s2.tsv> <dscx_s3.tsv>
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from metrics import *
paths=sys.argv[1:]
Ds=[Data(p) for p in paths]
def wgap(op,child,parent):
    a=[q for (n,r),q in op.items() if r==child]; b=[q for (n,r),q in op.items() if r==parent]
    return (min(a)-min(b)) if a and b else None
print('# 再生（紙の上の計算）：census の系列を外から与えた量として、(技術＋必要性＋伝わり)÷K の積み上げを回す。盤面の応答は無い＝腕の結果の代わりではない。')
print('# 設定：技術 T=70*(0.5*識字+0.5*D/(D+0.03))・必要性=70*S/(S+100)（物を生む行：S=(代替する物の値段-新しいレシピの費用)/換算*国の需要・機関車と新兵器と制度と思想は旧の値）・伝わり=招聘 70*y/(y+0.005)+取引 70*x/(x+0.25)（和は上限 70）')
print('## K の規則：世界で最初の内燃機関から世界で最初の戦車までの四半期数（種 2・3 の平均）を 124（ダイムラー 1885→Mark I 1916）に合わせる')
best=None
for K in range(84,100):
    gs=[wgap(run(D,K=K,Hr=0.005,Ha=0.25,transmode='rec+any')[0],'戦車','内燃機関') for D in Ds]
    m=sum(gs)/len(gs)
    print('K=%d gap=%s mean=%.1f'%(K,gs,m))
    if best is None or abs(m-124)<abs(best[1]-124): best=(K,m)
print('→ K=%d'%best[0])
K=best[0]
for tm in ('rec','rec+any'):
    print('\n## 伝わりの経路：%s（K=%d）'%(tm,K))
    for i,D in enumerate(Ds):
        op,known=run(D,K=K,Hr=0.005,Ha=0.25,transmode=tm)
        m=metrics(D,op)
        print('種%d %s'%(i+2,{k:v for k,v in m.items() if k not in('firsts','cn_first')}),'清が最初の行=',m['cn_first'])
        if tm=='rec+any':
            print('  蒸気機関:',sorted((D.year[q],n[:3]) for (n,r),q in op.items() if r=='蒸気機関' and n in MAJOR))
            for r in ('機関車','汽船','転炉鋼','内燃機関','戦車','石炭火力','電動機','弩級戦艦'):
                li=sorted((D.year[q],n[:3]) for (n,rr),q in op.items() if rr==r and n in MAJOR)
                print('  ',r,li[:3],'..',li[-1:],'主要国で開いた数=',len(li))
            f=m['firsts']
            print('  最初に開いた年が最も早い 10 行:',sorted(((v[0],v[1][:3],r) for r,v in f.items()))[:10])
            print('  1790 年までに開いた国×行:',[(D.year[q],n[:3],r) for (n,r),q in op.items() if D.year[q]<=1790])
