import itertools
best=(0,None); best_nosoc=(0,None)
g=[0,0.25,0.5,0.75,1.0]
for poor,cons,mil,rent,urban,elite,eraSoc in itertools.product(g,g,g,g,[0,1],[0,1],[0,1]):
    rich=0  # poor>0 -> wealthRel<1 -> rich=0 ; (poor=0 allows rich>0 but that lowers prog)
    conserv=0; prog=1.0
    r=[0]*11
    r[0]=2*rent+0.5; r[2]=(3.0 if elite else 1.0)+0; r[3]=2.0 if elite else 1.0
    r[5]=2*poor*(1-rent)+(1.0 if urban else 0); r[6]=1.5*poor*urban; r[7]=poor; r[8]=(1.0 if elite else 0); r[4]=0.5
    r[1]+=2*mil*conserv; r[2]+=2.5*cons*prog; r[3]+=(1.5*cons+1.0*mil)*prog; r[5]+=(1.0*cons+1.5*mil)*prog; r[6]+=2.0*mil*prog; r[7]+=1.0*mil*prog
    r[5]*=eraSoc
    s=sum(r); sh=r[6]/s
    if sh>best[0]: best=(sh,(poor,cons,mil,rent,urban,elite,eraSoc))
print(best)
