import sys, glob, os, statistics as st
D = sys.argv[1]; NAT = ['イギリス','フランス','アメリカ','日本','清','ロシア','プロイセン']
YRS=[int(x) for x in sys.argv[2].split(',')]
K=['landBorder','landProj','seaCross','seaFleet','seaTrade','mil','share']
COLS=None; Y={}; seeds=[]
for f in sorted(glob.glob(os.path.join(D,'d*.txt'))):
    sd=int(os.path.basename(f)[1:-4]); seeds.append(sd)
    for ln in open(f,encoding='utf-8'):
        t=ln.rstrip('\n').split('\t')
        if t[0]=='#N': COLS=t[1:]
        elif t[0]=='N':
            d=dict(zip(COLS,t[1:])); Y[(sd,int(d['yr']),d['nation'])]=d
def med(xs):
    xs=[x for x in xs if x==x]; return st.median(xs) if xs else float('nan')
def fl(x):
    try: return float(x)
    except: return float('nan')
for y in YRS:
    print(f'== {y}')
    print('国'.ljust(6)+''.join(k.rjust(11) for k in K)+'  海の最大の相手（種ごと）/陸の最大の相手')
    for n in NAT:
        row=n.ljust(6)+''.join(f'{med([fl(Y[(s,y,n)][k]) for s in seeds if (s,y,n) in Y]):11.4g}' for k in K)
        tops=[(Y[(s,y,n)]['topSea'],fl(Y[(s,y,n)]['topSeaV'])) for s in seeds if (s,y,n) in Y]
        topl=[(Y[(s,y,n)]['topLand'],fl(Y[(s,y,n)]['topLandV'])) for s in seeds if (s,y,n) in Y]
        print(row+'  '+','.join(f'{a}{b:.2g}' for a,b in tops)+' / '+','.join(f'{a}{b:.2g}' for a,b in topl))
