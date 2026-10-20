import sys,collections
sys.stdout.reconfigure(encoding='utf-8')
hdr=None; tot=collections.defaultdict(float); by=collections.defaultdict(dict)
for line in open('D:/PoB_work/bk473/D2.tsv',encoding='utf-8'):
    if line.startswith('#'): continue
    x=line.rstrip('\n').split('\t')
    if hdr is None: hdr=x; ix={h:i for i,h in enumerate(hdr)}; continue
    q=int(x[0]); b=float(x[ix['baseMoney']]); tot[q]+=b; by[x[1]][q]=b
for q in (0,20,40,80,120,160,200,240,280,320,360,400): print(q,'%.4g'%tot[q],end=' | ')
print()
for q in (120,280,400):
    top=sorted(by,key=lambda n:-(by[n][q]-by[n][0]))
    print('q',q,'biggest gainers',[(n,'%.3g'%(by[n][q]-by[n][0])) for n in top[:3]],'losers',[(n,'%.3g'%(by[n][q]-by[n][0])) for n in top[-4:]])
