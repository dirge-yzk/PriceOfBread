import sys
sys.path.insert(0,'.')
from an_g214 import *
H={'プロイセン':1873,'アメリカ':1879,'フランス':1878,'オーストリア':1892,'ロシア':1897,'日本':1897}
def first_gold(N,nm):
    ys=[int(c[2]) for c in N if c[4]==nm and int(c[5])==2]
    return min(ys) if ys else None
for d in sys.argv[1:]:
    R=load(d); print('==',d)
    tot=0;n=0
    for seed,(W,O,N) in R.items():
        row=[];hit=0
        for nm,h in H.items():
            y=first_gold(N,nm)
            row.append(f"{nm[:2]}{y if y else '-'}")
            if y and abs(y-h)<=20: hit+=1
        # UK first non-gold year (1783 start)
        uk=[int(c[2]) for c in N if c[4]=='イギリス' and int(c[5])!=2]
        print(f" seed{seed} hit{hit}/6 ", ' '.join(row), '英の非金の最初の年', min(uk) if uk else None)
        tot+=hit;n+=1
    print(' 合計',tot,'/',6*n)
