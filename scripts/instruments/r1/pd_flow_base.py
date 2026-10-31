"""(704) r1 段0 ④：基線 m702・N1・N2・a11 の urxpd_s から、計器の 9 国の PopDecision の流れ（sumFlowPos・sumFlowNeg の和・千人）を q0〜3 と q0〜19 で種ごとに足した量（読むだけ）。
⚠このファイルの出力は sumFlow を 3 で割った月平均の和＝千人×月ではなく /3 済み。四半期の総量は 3 倍（事前登録の数はこの 3 倍で書いた）。
"""
import csv, statistics as st, collections
B='D:/PoB_work/keep_702/'
foc=['清','ロシア','オスマン','フランス','イギリス','プロイセン','オーストリア','日本','アメリカ']
arms={'m702':('m702_ins',range(1,11)),'N1':('n1',range(1,11)),'N2':('n2',range(1,11)),'a11':('a11',range(11,21))}
def rd(p):
    with open(p,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); return {c:k for k,c in enumerate(h)},list(r)
res={}
for arm,(dr,seeds) in arms.items():
    for s in seeds:
        i,rows=rd(f'{B}{dr}/urxpd_s{s}.tsv')
        pos4=neg4=pos20=neg20=0.0
        for x in rows:
            if x[i['nation']] in foc:
                q=int(x[i['quarter']])
                if q<4: pos4+=float(x[i['sumFlowPos']])/3; neg4+=float(x[i['sumFlowNeg']])/3
                if q<20: pos20+=float(x[i['sumFlowPos']])/3; neg20+=float(x[i['sumFlowNeg']])/3
        res[(arm,s)]=(pos4,neg4,pos20,neg20)
for arm,(dr,seeds) in arms.items():
    v=[res[(arm,s)] for s in seeds]
    print(arm,'q0-3 pos+neg(千人・9国) 中央',round(st.median([a+b for a,b,_,_ in v])),'範囲',round(min(a+b for a,b,_,_ in v)),round(max(a+b for a,b,_,_ in v)),
          '| q0-19 pos+neg 中央',round(st.median([c+d for _,_,c,d in v])),'範囲',round(min(c+d for _,_,c,d in v)),round(max(c+d for _,_,c,d in v)),
          '| q0-19 pos 中央',round(st.median([c for _,_,c,_ in v])),'neg',round(st.median([d for _,_,_,d in v])))
