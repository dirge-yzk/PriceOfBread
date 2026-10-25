# (560) 合流の記録（読むだけ）：Q0 の ivf_s・建設の律速の配給率・発見の年（disc_s の E 行）・ロシアの農奴解放（reform_s）・戦争の数（wars_s）
import csv,glob,os,sys,statistics as st
def rows(p):
    with open(p,encoding='utf-8',newline='') as f:
        r=csv.reader(f,delimiter='\t');h=next(r)
        for x in r:
            if len(x)==len(h): yield dict(zip(h,x))
def fl(v):
    try: return float(v)
    except: return float('nan')
def med(v):
    v=[x for x in v if x==x]; return st.median(v) if v else float('nan')
def sid(p,pre): return int(os.path.basename(p)[len(pre):-4])
NAT=['イギリス','清','フランス','プロイセン','アメリカ','日本','ロシア','オスマン','オーストリア']
arms=[a.split('=',1) for a in sys.argv[1:]]
print('# ① Q0（四半期 0・10 種の中央値）')
for lab,d in arms:
    acc={}
    for p in glob.glob(os.path.join(d,'ivf_s*.tsv')):
        for x in rows(p):
            if x['quarter']=='0': acc.setdefault(x['nation'],[]).append(x)
    for col in ['steamMcOverWageMain','laborProdOverDemandMain','laborPriceOverRefMain','bundleSteelOverRefMain','bundleBuildingOverRefMain']:
        print(f'  {lab} {col}：'+'・'.join(f'{n} {med([fl(x.get(col,"nan")) for x in acc.get(n,[])]):.3f}' for n in NAT))
print('# ② 建設の束の律速の配給率（bottleneckRationSum÷bottleneckMonths・25 年ごと・10 種の中央値）')
B=[1783,1808,1833,1858,1883,1908,1933]
for lab,d in arms:
    acc={}
    for p in glob.glob(os.path.join(d,'ivf_s*.tsv')):
        s=sid(p,'ivf_s')
        for x in rows(p):
            y=int(x['year']);b=max([bb for bb in B if bb<=y])
            k=(s,x['nation'],b); a=acc.setdefault(k,[0.0,0.0]); a[0]+=fl(x['bottleneckRationSum']) if fl(x['bottleneckRationSum'])==fl(x['bottleneckRationSum']) else 0; a[1]+=fl(x['bottleneckMonths']) if fl(x['bottleneckMonths'])==fl(x['bottleneckMonths']) else 0
    print(f'  {lab}\t'+'\t'.join(f'{b}〜' for b in B))
    for n in ['イギリス','フランス','プロイセン','アメリカ','日本','清']:
        print(f'  {n}\t'+'\t'.join(f'{med([a[0]/a[1] for (s,nn,bb),a in acc.items() if nn==n and bb==b and a[1]>0]):.3f}' for b in B))
print('# ③ 発見の年（disc_s の E 行・最初の解禁・10 種の中央値・未発見は 9999）')
ROWS=['蒸気機関','機関車','転炉鋼','汽船','鋳鉄の骨組み','電動機','石炭火力']
for lab,d in arms:
    ev={}; seeds=set(); n1800={}
    for p in glob.glob(os.path.join(d,'disc_s*.tsv')):
        s=sid(p,'disc_s'); seeds.add(s)
        for x in rows(p):
            if x['kind']!='E': continue
            k=(s,x['nation'],x['row']); y=int(x['year'])
            if k not in ev or y<ev[k]: ev[k]=y
    for r in ROWS:
        print(f'  {lab} {r}：'+'・'.join(f'{n} {med([ev.get((s,n,r),9999) for s in seeds]):.0f}' for n in NAT))
    c=[sum(1 for (s,n,r),y in ev.items() if s==ss and r=='蒸気機関' and y<=1800) for ss in seeds]
    print(f'  {lab} 1800 年までに蒸気機関を解禁した国（種の中央値）{med(c)}')
print('# ④ ロシアの農奴解放（reform_s・serfLawCode が 0 から 1 以上・2 以上になった最初の四半期の中央値）')
for lab,d in arms:
    f1=[];f2=[]
    for p in glob.glob(os.path.join(d,'reform_s*.tsv')):
        a=b=None
        for x in rows(p):
            if x['nation']!='ロシア': continue
            q=int(x['quarter']);c=fl(x['serfLawCode'])
            if a is None and c>=1: a=q
            if b is None and c>=2: b=q
        f1.append(a if a is not None else 9999); f2.append(b if b is not None else 9999)
    print(f'  {lab} 最初の限定 {med(f1)}（≈{1783+med(f1)/4:.0f} 年）・最初の全面 {med(f2)}（≈{1783+med(f2)/4:.0f} 年）')
print('# ⑤ 戦争の数（wars_s・種の中央値）')
for lab,d in arms:
    iw=[];cw=[]
    for p in glob.glob(os.path.join(d,'wars_s*.tsv')):
        a=c=0
        for x in rows(p):
            civ=x.get('civilWarFlag','0')
            if civ in ('1','True','true'): c+=1
            else: a+=1
        iw.append(a);cw.append(c)
    print(f'  {lab} 国家間 {med(iw)}・内戦 {med(cw)}')
