# (1326) 問い3：内戦（wars_s の civilWarFlag 1）と首都発の建国の国庫の分与（sectreas_s の capitalRisingFlag 1）を (親＝sideA, 四半期 q) で突き合わせ、長さ・死者と運転資金の関係を読む
#   python -X utf8 q3_cw.py <出力> <名前=生のフォルダ> ...
import sys,os,csv,statistics as st,collections
out=open(sys.argv[1],'w',encoding='utf-8')
def rank(v):
    o=sorted(range(len(v)),key=lambda i:v[i]); r=[0]*len(v)
    i=0
    while i<len(o):
        j=i
        while j+1<len(o) and v[o[j+1]]==v[o[i]]: j+=1
        for k in range(i,j+1): r[o[k]]=(i+j)/2
        i=j+1
    return r
def spear(a,b):
    if len(a)<3: return float('nan')
    ra,rb=rank(a),rank(b); ma,mb=st.mean(ra),st.mean(rb)
    num=sum((x-ma)*(y-mb) for x,y in zip(ra,rb)); den=(sum((x-ma)**2 for x in ra)*sum((y-mb)**2 for y in rb))**0.5
    return num/den if den>0 else float('nan')
for a in sys.argv[2:]:
    name,d=a.split('=',1); rows=[]; unmatched=0; wars=0; dead_all=0
    seeds=sorted({f[6:-4] for f in os.listdir(d) if f.startswith('wars_s') and f.endswith('.tsv')},key=lambda x:int(x))
    for s in seeds:
        st_rows=collections.defaultdict(list)
        p=os.path.join(d,f'sectreas_s{s}.tsv')
        if os.path.exists(p):
            for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t'):
                if r['capitalRisingFlag']=='1': st_rows[(r['parent'],r['quarter'])].append(r)
        for r in csv.DictReader(open(os.path.join(d,f'wars_s{s}.tsv'),encoding='utf-8'),delimiter='\t'):
            if r['civilWarFlag']!='1': continue
            wars+=1; dead=float(r['deadK']); dead_all+=dead
            m=st_rows.get((r['sideA'],r['q']))
            if not m: unmatched+=1; rows.append(dict(s=s,y=r['startYear'],p=r['sideA'],mo=float(r['months']),dead=dead,T=None)); continue
            x=m[0]
            rows.append(dict(s=s,y=r['startYear'],p=r['sideA'],mo=float(r['months']),dead=dead,T=float(x['actualT']),old=float(x['oldRuleT']),new=float(x['newRuleT']),pop=float(x['movedPopK']),ppop=float(x['parentPopK']),ptr=float(x['parentTreasury'])))
    mt=[r for r in rows if r['T'] is not None]
    out.write(f'== {name}\t種 {len(seeds)}\t内戦 {wars}\t突き合った {len(mt)}\t突き合わない {unmatched}\t死者の和 {dead_all:.0f} 千人\n')
    if not mt: continue
    tpc=[r['T']/max(r['pop'],1e-9) for r in mt]; rel=[r['T']/max(r['ptr'],1e-9) for r in mt]; ratio=[r['new']/r['old'] if r['old']>0 else float('nan') for r in mt]
    mo=[r['mo'] for r in mt]; de=[r['dead'] for r in mt]; dpc=[r['dead']/max(r['pop'],1e-9) for r in mt]
    out.write(f'  順位相関（スピアマン）：長さ×国庫/人={spear(tpc,mo):.3f}  死者×国庫/人={spear(tpc,de):.3f}  長さ×国庫/親の国庫={spear(rel,mo):.3f}  死者×国庫/親の国庫={spear(rel,de):.3f}  長さ×新÷旧={spear(ratio,mo):.3f}  死者×新÷旧={spear(ratio,de):.3f}  死者×移る人口={spear([r["pop"] for r in mt],de):.3f}\n')
    up=[r for r,q in zip(mt,ratio) if q>1]; dn=[r for r,q in zip(mt,ratio) if q<=1]
    for lab,g in (('新÷旧>1',up),('新÷旧≦1',dn)):
        if g: out.write(f'  {lab}：{len(g)} 件・長さの中央 {st.median(r["mo"] for r in g):.1f}・死者の中央 {st.median(r["dead"] for r in g):.2f}・死者の和 {sum(r["dead"] for r in g):.0f}・新÷旧の中央 {st.median(r["new"]/r["old"] for r in g):.3f}\n')
    out.write(f'  新÷旧の中央 {st.median([q for q in ratio if q==q]):.3f}・死者の和の上位 5 件の割合 {sum(sorted(de,reverse=True)[:5])/max(sum(de),1e-9):.2f}\n')
    for r in sorted(rows,key=lambda r:-r['dead'])[:6]:
        extra=f"新÷旧={r['new']/r['old']:.3f} 国庫/人={r['T']/max(r['pop'],1e-9):.3g} 移る人口={r['pop']:.0f}" if r['T'] is not None else '分与の行なし'
        out.write(f'  上位：s{r["s"]} {r["y"]} {r["p"]} 長さ {r["mo"]:.0f} 死者 {r["dead"]:.1f} {extra}\n')
