import sys,re,collections
import xml.etree.ElementTree as ET
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
rows=[]
for f in sys.argv[1:]:
    r=ET.parse(f).getroot()
    for u in r.iter('{%s}UnitTestResult'%ns['t']):
        d=u.get('duration') or '0'
        h,m,s=d.split(':'); sec=int(h)*3600+int(m)*60+float(s)
        rows.append((sec,u.get('testName'),u.get('outcome'),f.split('/')[-1]))
rows.sort(reverse=True)
tot=sum(x[0] for x in rows); print('n',len(rows),'sum',round(tot),'s')
print('top50',round(sum(x[0] for x in rows[:50])))
for x in rows[:50]: print(round(x[0],1),x[1][:110],x[3][:14])
print('--classes')
c=collections.Counter(); n=collections.Counter()
for s,t,o,f in rows:
    k=t.rsplit('.',1)[0] if '(' not in t.split('.')[-2:][0] else t
    c[k]+=s;n[k]+=1
for k,v in c.most_common(25): print(round(v),n[k],k)
