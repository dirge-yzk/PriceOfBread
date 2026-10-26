import sys, os; sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from replay2 import *
import statistics as st
MAJOR=['イギリス','フランス','プロイセン','アメリカ','ロシア','オーストリア','オスマン','清','日本']
def metrics(D,op,show=False):
    out={}
    firsts={}
    for (n,r),q in op.items():
        if r not in firsts or q<firsts[r][0] or (q==firsts[r][0] and n<firsts[r][1]): firsts[r]=(q,n)
    # ④ 最初の発見者 vs 技師の総数が最大の国（その四半期の engAll）
    notmax=0; tot=0
    for r,(q,n) in firsts.items():
        best=max(D.nats,key=lambda m:D.N.get((q,m),{}).get('engAll',0))
        tot+=1; notmax+= (best!=n)
    out['first_not_maxeng']=(notmax,tot)
    # ③ 主要国で最初の主要国から1年(4q)以内に開く主要国の数
    simult=[]
    for r in D.rows:
        qs=sorted(op[(n,r)] for n in MAJOR if (n,r) in op)
        if len(qs)>=2: simult.append(sum(1 for x in qs if x<=qs[0]+4))
    out['simult_med']=st.median(simult) if simult else None
    out['simult_max']=max(simult) if simult else None
    # ② 蒸気機関・機関車が英以外の主要国へ 1850 年までに（開幕の既知を除く）
    def reach(r,y):
        got=[n for n in MAJOR if n!='イギリス' and (n,r) in op and D.year[op[(n,r)]]<=y]
        return len(got)
    out['steam_by1850']=reach('蒸気機関',1850); out['loco_by1850']=reach('機関車',1850)
    # 清が最初の行
    out['cn_first']=[r for r,(q,n) in firsts.items() if n=='清']
    out['firsts']={r:(D.year[q],n) for r,(q,n) in firsts.items()}
    return out
