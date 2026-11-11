# (925) 途中で現れる州（四半期 3 より後に初めて行が出る州の名）のうち、割譲・建国・返還の事象に名の無い物＝接収・植民（AcquireColony→PromoteColonyToRegion・支配度 0 から）の候補。
# 最初の行の支配度・上限に届く年数・同じ持ち主の間の goFlag（分離独立の戦争へ）・後で Seceded の事象に名が出るか。rule_s と secession_s は間引き（picked 以外は年の末）。
import sys,glob,os,statistics
src=sys.argv[1]; res={'n':0,'go10':0,'goever':0,'never_top':0,'seceded_later':0}; d0=[]; ytop=[]; mfirst=[]
for p in sorted(glob.glob(os.path.join(src,'rule_s*.tsv'))):
    sd=p[len(os.path.join(src,'rule_s')):-4]
    evn={}; 
    for line in open(os.path.join(src,f'cederule_s{sd}.tsv'),encoding='utf-8'):
        c=line.split('\t')
        if c[0]!='quarter': evn.setdefault(c[5],[]).append((int(c[0]),c[2]))
    ser={}
    for line in open(p,encoding='utf-8'):
        c=line.split('\t')
        if c[0]=='quarter': continue
        ser.setdefault(c[3],{}).setdefault(int(c[0]),(c[2],float(c[12]) if c[12] not in ('nan','') else float('nan')))
    cand={}
    for n,s in ser.items():
        q0=min(s)
        if q0>3 and n not in evn: cand[(s[q0][0],n)]=q0
    sec={}
    for line in open(os.path.join(src,f'secession_s{sd}.tsv'),encoding='utf-8'):
        c=line.split('\t')
        if c[0]=='quarter': continue
        k=(c[2],c[3])
        if k in cand: sec.setdefault(k,{})[int(c[0])]=(float(c[32]) if c[32] not in ('nan','') else float('nan'), c[34]=='1')
    for (na,n),q0 in cand.items():
        res['n']+=1; s=ser[n]; d0.append(s[q0][1])
        own=[t for t in sorted(s) if s[t][0]==na]
        hit=next((t for t in own if s[t][1]>=0.999),None)
        if hit is None: res['never_top']+=1
        else: ytop.append((hit-q0)/4)
        sk=sec.get((na,n),{})
        if any(v[1] for t,v in sk.items() if t<q0+40): res['go10']+=1
        if any(v[1] for v in sk.values()): res['goever']+=1
        ms=[v[0] for t,v in sorted(sk.items()) if t<q0+8 and v[0]==v[0]]
        if ms: mfirst.append(statistics.median(ms))
        if any(h=='Seceded' for _,h in evn.get(n,[])): res['seceded_later']+=1
def m(v): 
    v=sorted(x for x in v if x==x); return f'n {len(v)} p10 {v[int(.1*(len(v)-1))]:.4f} med {v[len(v)//2]:.4f} p90 {v[int(.9*(len(v)-1))]:.4f}' if v else 'n 0'
print('# acquired', src, res)
print('dom_first_row', m(d0)); print('years_to_top', m(ytop)); print('margin_med_first2y', m(mfirst))
