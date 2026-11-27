# (1417) 国ごとの蜂起（factionRevoltsCum の最大）と内戦の始まり（nation の civilWarMonths が 0 から正）を、旗 0（base_ins）と旗 1（c150sdB）で 10 種の平均に
import csv, collections
def rev(d):
    m=collections.defaultdict(float)
    for s in range(1,11):
        for x in csv.DictReader(open(f'{d}/dcont_s{s}.tsv', newline='', encoding='utf-8'), delimiter='\t'):
            k=(s,x['nation']); m[k]=max(m[k], float(x['factionRevoltsCum']))
    out=collections.defaultdict(float)
    for (s,n),v in m.items(): out[n]+=v/10
    return out
def cw(d):
    out=collections.defaultdict(float)
    for s in range(1,11):
        prev={}
        for x in csv.DictReader(open(f'{d}/nation_s{s}.tsv', newline='', encoding='utf-8'), delimiter='\t'):
            n=x['nation']; v=float(x['civilWarMonths'] or 0)
            if v>0 and prev.get(n,0)<=0: out[n]+=0.1
            prev[n]=v
    return out
B='D:/PoB_work/keep_1411/base_ins'; A='D:/PoB_work/keep_1411_rej/c150sdB/ins'
rb,ra,cb,ca=rev(B),rev(A),cw(B),cw(A)
print('total per-seed: revolts', round(sum(rb.values()),2), '->', round(sum(ra.values()),2), ' civil-war starts', round(sum(cb.values()),2), '->', round(sum(ca.values()),2))
names=sorted(set(rb)|set(cb), key=lambda n:-(rb.get(n,0)+cb.get(n,0)))[:14]
print('nation\trev0\trev1\tcw0\tcw1')
for n in names: print(f'{n}\t{rb.get(n,0):.1f}\t{ra.get(n,0):.1f}\t{cb.get(n,0):.1f}\t{ca.get(n,0):.1f}')
for n in ('フランス','イギリス','プロイセン','オーストリア','ロシア','清','アメリカ'):
    print('major', n, f'rev {rb.get(n,0):.1f}->{ra.get(n,0):.1f}  cw {cb.get(n,0):.1f}->{ca.get(n,0):.1f}')
