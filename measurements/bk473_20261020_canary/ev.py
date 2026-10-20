import sys,collections
sys.stdout.reconfigure(encoding='utf-8')
fn=sys.argv[1]
hdr=None; d=collections.defaultdict(dict)
for line in open('D:/PoB_work/bk473/'+fn+'.tsv',encoding='utf-8'):
    if line.startswith('#'): continue
    x=line.rstrip('\n').split('\t')
    if hdr is None: hdr=x; ix={h:i for i,h in enumerate(hdr)}; continue
    d[x[1]][int(x[0])]=(float(x[ix['capitalistSavTotal']]),float(x[ix['priceLevel']]),float(x[ix['gdpY']]))
MAIN=['イギリス','フランス','プロイセン','オーストリア','ロシア','オスマン','清','日本']
import statistics as st
# first q where capSav falls below 50% of its running max
cnt=collections.Counter(); allq=[]
for n,s in d.items():
    mx=0; ev=None
    for q in sorted(s):
        mx=max(mx,s[q][0])
        if mx>0 and s[q][0]<0.5*mx and q>40: ev=q; break
    if n in MAIN: print(n,'capSav<50% of max at q',ev, 'gdp q150->min', min(s[q][2] for q in s if q>=150)/s[150][2])
    if ev: allq.append(ev)
print('all nations with event:',len(allq),'of',len(d),'quartiles',st.quantiles(allq,n=4) if len(allq)>3 else allq)
