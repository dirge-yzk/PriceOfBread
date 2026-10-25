import csv,glob,sys
from collections import Counter,defaultdict
base='D:/PoB_work/keep_552/'
for arm in ['m552_ins','n1','n2']:
    dec=Counter(); inc=Counter(); rows=0
    for fp in glob.glob(base+arm+'/reform_s*.tsv'):
        last={}
        for r in csv.DictReader(open(fp,encoding='utf-8'),delimiter='\t'):
            rows+=1
            for k in ('serfLawCode','landReformLawCode'):
                key=(r['nation'],k); v=int(r[k])
                if key in last:
                    if v<last[key]: dec[k]+=1
                    elif v>last[key]: inc[k]+=1
                last[key]=v
    print(arm,'rows',rows,'dec',dict(dec),'inc',dict(inc))
# dem0Top SerfEmancipation- counts by decade (picked rows only have dem0Top), and gain>0 nations
dem=Counter(); picked=Counter(); gainnat=Counter()
def cap(lv,full): return full if lv==2 else (full+(1-full)*0.5 if lv==1 else 1.0)
for fp in glob.glob(base+'m552_ins/rs2_s*.tsv'):
    for r in csv.DictReader(open(fp,encoding='utf-8'),delimiter='\t'):
        s=int(r['serfLawCode']);l=int(r['landReformLawCode']);c=float(r['customaryRentShare']);now=float(r['effRentShareNow']);ratio=float(r['lordRentToIncomeShare'])
        for ax,code,cp in (('serf',s,0.5),('land',l,0.3)):
            prev=max(code-1,0)
            if ax=='serf': pn=min(c,cap(prev,0.5),cap(l,0.3))
            else: pn=min(c,cap(s,0.5),cap(prev,0.3))
            g=(pn-now)/now*ratio if now>1e-9 else 0
            if g>1e-9: gainnat[(ax,r['nation'])]+=1
        if r['dem0Top']!='' :
            d=(int(r['year'])//10)*10
            picked[d]+=1
            if 'SerfEmancipation-' in r['dem0Top']: dem[d]+=1
print('dem0Top SerfEmancipation- by decade',{d:(dem[d],picked[d]) for d in sorted(picked)})
print('gain>0 rows by nation',gainnat.most_common(30))
