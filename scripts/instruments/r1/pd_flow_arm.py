"""(704) r1：腕の urxpd_s から計器の 9 国の PopDecision の流れ（sumFlowPos＋sumFlowNeg を 3 で割った月平均の和・千人）を q0〜3 と q0〜19 で種ごとに足す（読むだけ）。
使い方：python -X utf8 -I pd_flow_arm.py <名前>=<ins のフォルダ>,<種の起点> ...   （pd_flow_base.py の引数版）"""
import csv, statistics as st, sys
foc=['清','ロシア','オスマン','フランス','イギリス','プロイセン','オーストリア','日本','アメリカ']
def rd(p):
    with open(p,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); return {c:k for k,c in enumerate(h)},list(r)
for a in sys.argv[1:]:
    name,rest=a.split('=',1); d,_,sb=rest.partition(','); sb=int(sb or 1)
    v=[]
    for s in range(sb,sb+10):
        i,rows=rd(f'{d}/urxpd_s{s}.tsv'); p4=p20=0.0
        for x in rows:
            if x[i['nation']] in foc:
                q=int(x[i['quarter']]); t=(float(x[i['sumFlowPos']])+float(x[i['sumFlowNeg']]))/3
                if q<4: p4+=t
                if q<20: p20+=t
        v.append((p4,p20))
    print(name,'q0-3 中央',round(st.median(a for a,_ in v)),'範囲',round(min(a for a,_ in v)),round(max(a for a,_ in v)),'| q0-19 中央',round(st.median(b for _,b in v)),'範囲',round(min(b for _,b in v)),round(max(b for _,b in v)))
