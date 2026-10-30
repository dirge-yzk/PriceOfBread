# 段E の格子の走査（厳密版）：FactionSnapshot.ArgMax の規則＝同点は先頭の添字が勝つ（FactionSnapshot.cs:244）。
# 共産主義(6)が主軸になる＝r6 が r0..r5 より厳密に大きく、r7..r10 以上。格子＝窮乏・意識・闘争度・地代依存を 0..1 の 5 段・都市・近代エリート。富の執着 0（平均以下の富）。
import itertools,collections
g=[0,0.25,0.5,0.75,1.0]
n=0;tot=0;c=collections.Counter();rent=collections.Counter();poor_=collections.Counter()
for poor,cons,mil,rt,urban,elite in itertools.product(g,g,g,g,[0,1],[0,1]):
    tot+=1
    r=[0]*11
    r[0]=2*rt+0.5; r[2]=(3.0 if elite else 1.0); r[3]=2.0 if elite else 1.0
    r[5]=2*poor*(1-rt)+(1.0 if urban else 0); r[6]=1.5*poor*urban; r[7]=poor; r[8]=(1.0 if elite else 0); r[4]=0.5
    r[2]+=2.5*cons; r[3]+=(1.5*cons+mil); r[5]+=(cons+1.5*mil); r[6]+=2.0*mil; r[7]+=mil
    if r[6]>max(r[0:6]) and r[6]>=max(r[7:11]):
        n+=1;c[cons]+=1;rent[rt]+=1;poor_[poor]+=1
print(tot,n,'cons',dict(c),'rent',dict(rent),'poor',dict(poor_))
