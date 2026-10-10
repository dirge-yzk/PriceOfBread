import json,re,sys,io
P='D:/PoB_work/wt_def/PriceOfBread.SimCore/world.json'
raw=open(P,'rb').read()
assert not raw.startswith(b'\xef\xbb\xbf')
txt=raw.decode('utf-8')
before_cr=txt.count('\r'); before_lines=txt.count('\n')
doc=json.loads(txt)
# NationConfig の C# 既定値（この回の後）。None＝既定の「無い」（キーが無ければ null）
D={'merchant':400,'artisan':800,'literacy':0.23,'clergyFrac':0.4,'intelligentsiaFrac':0.35,'peasantSubsistence':0.7,'polity':'Absolutist','education':'Religious','coercion':400,'treasury':200000,'govDebt':0,'pollTaxStage':1,'landTaxStage':1,'bureaucracy':'Spoils','institutionalDecay':0,'nationalConsciousness0':-1,'initialMassMilitancy':0,'initialTariff':0.4,'industrialization':0,'initialColonies':[],'initialSuffrage':'None','initialCensorship':'StatePressOnly','initialCornLaws':'Full','initialSerfEmancipation':'None','initialImmigration':'Open','initialConscription':'Selective','initialMonetaryLaw':'Silver','knownDiscoveries':[],'broadMoneyToGdp':0,'initialInterestRate':0.25,'latitude':0,'longitude':0,'farmLordShare':0.6,'culturalGoodId':54,'nobility':0,'peasant':0,'worker':0,'bourgeois':0,'professional':0,'provinces':[]}
OLD={**D,'initialTariff':0.0,'initialCornLaws':'None','farmLordShare':0.5,'culturalGoodId':-1}
NEW=D
def eff(n,k,dflt): return n[k] if k in n else dflt.get(k)
lines=txt.split('\n')
out=[];ni=-1;removed=[];in_nations=False
for ln,l in enumerate(lines):
    if l==' "nations": [': in_nations=True
    if l.startswith(' "abstractTargets"'): in_nations=False
    if re.match(r'^  \{$',l) and in_nations: ni+=1
    m=re.match(r'^   "([A-Za-z0-9]+)": (.*?),$',l)
    if m and in_nations and ni>=0:
        k,v=m.group(1),m.group(2)
        if k in NEW and k not in('provinces',):
            n=doc['nations'][ni]
            if k in n and n[k]==NEW[k]:
                if v.startswith('[') and v!='[]': raise SystemExit('multi-line array '+k)
                removed.append((ni,n['name'],k,v)); continue
    elif in_nations and ni>=0 and re.match(r'^   "([A-Za-z0-9]+)": [^,\[{][^,]*$',l) and re.match(r'^   "(\w+)"',l).group(1) in NEW:
        k=re.match(r'^   "(\w+)"',l).group(1); v=l.split(': ',1)[1]
        n=doc['nations'][ni]
        if k in n and n[k]==NEW[k]:
            # 最後のキー（末尾の , が無い）＝直前の行の末尾の , を外す
            assert out[-1].endswith(','), out[-1]
            out[-1]=out[-1][:-1]
            removed.append((ni,n['name'],k,v)); continue
    out.append(l)
new='\n'.join(out)
open(P,'wb').write(new.encode('utf-8'))
doc2=json.loads(new)
# 実効の値の同一性：旧（旧既定）と新（新既定）で 61国×全キー
bad=0
for a,b in zip(doc['nations'],doc2['nations']):
    keys=set(a)|set(b)|set(D)
    for k in keys:
        if k in('name','rule','radius'): pass
        va=eff(a,k,OLD); vb=eff(b,k,NEW)
        if va!=vb: bad+=1; print('DIFF',a['name'],k,va,vb)
    # rule・name・その他（既定表に無いキー）は完全一致
    for k in keys:
        if k not in D and a.get(k)!=b.get(k): bad+=1; print('DIFF2',a['name'],k)
print('nations',len(doc2['nations']),'bad',bad)
print('removed',len(removed),'lines before',before_lines,'after',new.count('\n'),'CR',before_cr,new.count('\r'))
import collections
c=collections.Counter(k for _,_,k,_ in removed)
for k,v in c.most_common(): print(k,v)
# 抽象標的ほか nations 以外が不変
for sec in doc:
    if sec!='nations': assert doc[sec]==doc2[sec],sec
