import sys, glob, os, statistics as st, collections
D = sys.argv[1]
NAT = ['イギリス','フランス','ネーデルラント','スペイン','ポルトガル','ロシア','アメリカ','日本','清']
T = {}; E = []; seeds = []
for f in sorted(glob.glob(os.path.join(D,'t*.txt'))):
    sd = int(os.path.basename(f)[1:-4]); seeds.append(sd)
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] == 'T': T[(sd, int(t[1]), t[2])] = list(map(int, t[3:]))
        elif t[0] == 'E': E.append((sd,)+tuple(t[1:]))
WIN = [(1784,1793),(1815,1824),(1855,1864),(1880,1889),(1904,1913),(1940,1949)]
def wmean(s, n, k, a, b):
    xs = [T[(s,y,n)][k] for y in range(a,b+1) if (s,y,n) in T]
    return sum(xs)/len(xs) if xs else float('nan')
def med(xs):
    xs=[x for x in xs if x==x]; return st.median(xs) if xs else float('nan')
names = ['本土','編入','植民地','海の向こうの本国の格','うち1783の持ち主が自分でない','首都の陸塊の本国の格']
for k, nm in enumerate(names):
    print(f'## {nm}（10年の窓の平均・種の中央）')
    print('国'.ljust(8) + ''.join(f'{a}-{b%100:02d}'.rjust(10) for a,b in WIN))
    for n in NAT:
        print(n.ljust(8) + ''.join(f'{med([wmean(s,n,k,a,b) for s in seeds]):10.3g}' for a,b in WIN))
print('## 出来事（全ての種の合計）＝種類・前→後・同じ陸塊か')
c = collections.Counter((e[2], e[4], e[5], e[9]) for e in E)
for k, v in sorted(c.items(), key=lambda x: -x[1]): print(k, v)
print('## 本国の格へ入った海の向こうの州（sameLm=0 で後が Core/Integrated）の出来事を受け手の国ごとに')
c2 = collections.Counter((e[7], e[2], e[4], e[5]) for e in E if e[9] == '0' and e[5] in ('Core','Integrated'))
for k, v in sorted(c2.items(), key=lambda x: -x[1])[:40]: print(k, v)
