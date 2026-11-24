# (1354) paper1354.txt の中央だけを国×期の表へ（読むだけ）。使い方：python -I summ1354.py <paper1354.txt> <出力>
import sys
L=open(sys.argv[1],encoding='utf-8').read().split('\n')
keys=["T（今）","EffFill（今）","R＝主食÷効率化材","z＝w÷p̄","w÷mc","mc÷p̄","売れた÷能力 s","v_f÷w（今）","g 冪","g 冪(満)","g 対数","丙の a（今の T）","丙の a（A冪）","a＜s の人口（A冪）","x̄ 今","κ_E 今","x̄ 丙だけ(GE)","x̄ 丙だけ(PE)","x̄ A冪","κ_E A冪","x̄ A冪(満)","κ_E A冪(満)","x̄ A対数","x̄ A冪+丙(GE)","κ_E A冪+丙(GE)","x̄ A冪+丙(PE)","κ_E A冪+丙(PE)","工業化度 I（国）","g K","丙の a（K）","a＜s の人口（K）","丙の a（K+A冪）","x̄ K","κ_E K","x̄ K+丙(GE)","κ_E K+丙(GE)","x̄ K+丙(PE)","κ_E K+丙(PE)","x̄ K+A冪","κ_E K+A冪","x̄ K+A冪+丙(GE)","κ_E K+A冪+丙(GE)","x̄ K+A冪+丙(PE)","κ_E K+A冪+丙(PE)","z′ 丙・需要固定（今の T）","z′ 丙・需要固定（K）","農民の市場の主食の買い÷必要量（今）","農民の市場の主食の買い K÷今","農民の市場の主食の買い K+A冪÷今"]
d={};cur=None
for l in L:
    if l.startswith('## '): cur=l[3:]; d[cur]={}
    elif cur and ': ' in l:
        k,v=l.strip().split(': ',1); d[cur][k]=v.split('〔')[0]
o=[]
for nat in ("ロシア","清","イギリス"):
    ps=[p for p in d if p.startswith(nat)]
    o.append("| "+nat+" | "+" | ".join(p.split()[1] for p in ps)+" |")
    for k in keys: o.append("| "+k+" | "+" | ".join(d[p].get(k,"-") for p in ps)+" |")
open(sys.argv[2],'wb').write(("\n".join(o)+"\n").encode('utf-8'))
