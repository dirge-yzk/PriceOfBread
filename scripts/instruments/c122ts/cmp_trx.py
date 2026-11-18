import sys
import xml.etree.ElementTree as ET
T='{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
def load(fs):
    d={}
    for f in fs:
        for u in ET.parse(f).getroot().iter(T+'UnitTestResult'):
            h,m,s=u.get('duration').split(':'); d[u.get('testName')]=int(h)*3600+int(m)*60+float(s)
    return d
b='C:/Users/dirge/Documents/PriceOfBread/measurements/m1212_20261118/suite_8a22a2cc5/'
old=load([b+'nonfull_PriceOfBread.Tests.trx',b+'nonfull_PriceOfBread.Tests.Ui.trx'])
new=load(['trx/nonfull_PriceOfBread.Tests.trx','trx/nonfull_PriceOfBread.Tests.Ui.trx'])
skip=lambda n:'TreasuryLedgerTests' in n
com=[n for n in old if n in new and not skip(n)]
so=sum(old[n] for n in com); sn=sum(new[n] for n in com)
print('common',len(com),round(so),round(sn),'load',round(sn/so,3))
print('old total',round(sum(old.values())),'new total',round(sum(new.values())),'new/load',round(sum(new.values())/(sn/so)))
print('missing in new',[n[-40:] for n in old if n not in new][:3],'added',[n[-40:] for n in new if n not in old][:3])
tl=lambda d:round(sum(v for n,v in d.items() if skip(n)))
print('TreasuryLedger old',tl(old),'new',tl(new),'new/load',round(tl(new)/(sn/so)))
