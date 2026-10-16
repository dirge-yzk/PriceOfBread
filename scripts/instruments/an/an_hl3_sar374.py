import csv, math, statistics as st
W='D:/PoB_work/an2_374'
def rate(arm,s,nation,q0,q1):
    p=f"{W}/out_{arm}_10/ins/pop_s{s}.tsv"; num=den=0.0
    with open(p,encoding='utf-8',newline='') as fh:
        r=csv.reader(fh,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
        for ln in r:
            if ln[ix['scope']]!='N' or ln[ix['nation']]!=nation: continue
            q=int(ln[ix['quarter']])
            if q<q0 or q>q1: continue
            try: m=float(ln[ix['monthsCount']]); cb=float(ln[ix['cbrPerMille']]); cd=float(ln[ix['cdrPerMille']])
            except: continue
            if m>0 and not(math.isnan(cb) or math.isnan(cd)): num+=(cb-cd)*m; den+=m
    return num/den/10 if den>0 else None
for arm in ['A','N1','N2','B0','B','N1B','N2B']:
    v=[x for x in (rate(arm,s,'サルデーニャ＝ピエモンテ',152,300) for s in range(1,11)) if x is not None]
    print(arm, round(st.median(v),2) if v else None, len(v))
