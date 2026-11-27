import sys
sys.path.insert(0,'D:/PoB_work/wt_c151lk/scripts/instruments/c151lk')
from lk_gate import load, groups, WP
I='D:/PoB_work/c151lk_work/out_c151lkB_10/ins'
fo=[]
for s in range(1,11):
  ix, rows = load(f'{I}/wexit_s{s}.tsv')
  for g, rs in groups(ix, rows).items():
    prev=None; pc=False
    for k,r in enumerate(rs):
      x=float(r[ix['scoreIndex']])
      if k==0:
        if abs(x)>WP*(1+1e-9)+1e-12: fo.append((s,g,x,r[1],len(rs), r[ix['civilFlag']]))
      elif not (r[ix['forcedFlag']]=='1' or pc or abs(x)>=1.5-1e-9 or abs(prev)>=1.5-1e-9):
        d=x-prev
        if abs(d)>WP*(1+1e-9)+1e-12: print('OVER',s,g,k,repr(rs[k-1][ix['scoreIndex']]),repr(r[ix['scoreIndex']]),'excess=%.3g'%(abs(d)-0.1))
      prev=x; pc = r[ix['oldCollapseAFlag']]=='1' or r[ix['oldCollapseBFlag']]=='1'
import collections
print('firstOver',len(fo), 'abs-score hist', collections.Counter(round(abs(f[2]),1) for f in fo))
for f in fo[:6]: print(f)
