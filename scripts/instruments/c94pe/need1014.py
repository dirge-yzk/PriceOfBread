# (1014) c94pe 必要性の確かめ（紙・読むだけ・使い方 python need1014.py <生>）：錠 A が止めた行（lockABlockedMerchant=1）で、エンジンの attract の近似と >1 の件数を tier ごとに
import glob,sys,math
from collections import defaultdict
st=defaultdict(lambda: defaultdict(int)); seedst=defaultdict(lambda: defaultdict(int))
flowK=defaultdict(float)
for f in sorted(glob.glob(sys.argv[1]+'/merch_s*.tsv')):
  sd=f.split('merch_s')[-1].split('.')[0]
  with open(f,'rb') as fh:
    h=fh.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix={k:i for i,k in enumerate(h)}
    for ln in fh:
      x=ln.decode('utf-8').rstrip('\r\n').split('\t'); t=x[ix['tier']]
      S=st[t]; S['rows']+=1
      if x[ix['merchantEmpty']]=='1': S['empty']+=1; seedst[sd][t+'_empty']+=1
      if x[ix['lockABlockedMerchant']]!='1': continue
      S['lockA']+=1; seedst[sd][t+'_lockA']+=1
      ea=float(x[ix['emptyMerchantEarnedPerMonth']]); mk=float(x[ix['merchantK']])
      wi=float(x[ix['workerIncomePerCap']]); pr=float(x[ix['promotionPremium']])
      if ea>0: S['earn>0']+=1
      a=max(0,ea)/max(1e-9,mk)/max(1e-9,wi*pr)
      if a>1: S['attract>1']+=1; flowK[t]+=0.02*float(x[ix['workerK']])*min(a-1,1)
      if a>2: S['attract>2(cap)']+=1
      if mk==0: S['mk==0']+=1
for t in st: print(t, dict(st[t]), 'flowK_sum(0.02率)=%.1f'%flowK[t])
for sd in sorted(seedst,key=int): print('seed',sd,dict(seedst[sd]))
