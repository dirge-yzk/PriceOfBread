import sys,re,collections,glob
import xml.etree.ElementTree as ET
p=sys.argv[1]; root=sys.argv[2]
NS='{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
col={}
for f in glob.glob(root+'/PriceOfBread.Tests/*.cs'):
    L=open(f,encoding='utf-8-sig').read().split('\n')
    for i,l in enumerate(L):
        m=re.match(r'\s*(?:public\s+|internal\s+)?(?:sealed\s+|static\s+)?(?:partial\s+)?class\s+(\w+)',l)
        if m:
            for j in range(max(0,i-6),i+1):
                c=re.search(r'Collection\("([^"]+)"\)',L[j])
                if c: col[m.group(1)]=c.group(1)
d=collections.defaultdict(float);n=collections.Counter()
for r in ET.parse(p).getroot().iter(NS+'UnitTestResult'):
    nm=r.get('testName'); h,m_,s_=r.get('duration','0:0:0').split(':'); sec=int(h)*3600+int(m_)*60+float(s_)
    cls=nm.split('(')[0].rsplit('.',2)
    cls=cls[-2] if len(cls)>=2 else cls[0]
    d[cls]+=sec;n[cls]+=1
tot=sum(d.values()); ci=sum(v for k,v in d.items() if col.get(k)=='Instruments')
print('total min',round(tot/60,1),'Instruments coll min',round(ci/60,1),'classes in Instruments',sum(1 for k in d if col.get(k)=='Instruments'))
for k,v in sorted(d.items(),key=lambda x:-x[1])[:16]: print(f'{k}\t{v/60:.1f}\t{n[k]}\t{col.get(k,"-")}')
print('--- Instruments collection top')
for k,v in sorted(((k,v) for k,v in d.items() if col.get(k)=='Instruments'),key=lambda x:-x[1])[:12]: print(f'{k}\t{v/60:.2f}\t{n[k]}')
print('--- other collections')
print(collections.Counter(col.values()))
