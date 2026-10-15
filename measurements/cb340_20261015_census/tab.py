import glob
R={}
for d in sorted(glob.glob('D:/PoB_work/cb/res_f*')):
    kv={}
    for l in open(d+'/cb340.txt'):
        k,v=l.split(); kv[k]=float(v)
    f=kv['freq']; s=lambda k: kv[k]/f
    n=d.split('/')[-1]
    g=s('price')-s('daily'); rest=s('bil')-s('rows')
    R[n]=dict(price=s('price'),daily=s('daily'),goods=g,bil=s('bil'),rows=s('rows'),rest=rest,cpu=kv['cpuSec'],wall=kv['wallSec'],cnt=(int(kv['rowsSeen']),int(kv['fills']),int(kv['priceGoods']),int(kv['priceDailyRuns'])))
    print(n,'price %.2f daily %.2f goods %.2f | bil %.2f rows %.2f rest %.2f | cpu %.1f wall %.1f'%(s('price'),s('daily'),g,s('bil'),s('rows'),rest,kv['cpuSec'],kv['wallSec']),R[n]['cnt'])
import statistics as st
for key in ['price','daily','goods','bil','rows','rest','cpu']:
    b=[v[key] for n,v in R.items() if 'base' in n]; w=[v[key] for n,v in R.items() if 'new' in n]
    print(key,'base mean %.2f (%s) new mean %.2f (%s) ratio %.4f'%(st.mean(b),' '.join('%.2f'%x for x in b),st.mean(w),' '.join('%.2f'%x for x in w),st.mean(w)/st.mean(b)))
