# (1242) c126al 記録：発見の行ごとの最初の年（種の中央・未発見は 1951）を腕と基準で並べる（disc_s の E 行）
import csv,statistics as st,sys
csv.field_size_limit(10**9)
def first(p):
    r=csv.reader(open(p,encoding='utf-8'),delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
    d={}
    for x in r:
        if x[ix['kind']]!='E': continue
        row=x[ix['row']]; y=int(x[ix['year']]) if 'year' in ix else int(x[ix['quarter']])//4+1783
        d[row]=min(d.get(row,9999),y)
    return d
arm,base=sys.argv[1],sys.argv[2]
A=[first(f'{arm}/disc_s{s}.tsv') for s in range(1,11)]
B=[first(f'{base}/disc_s{s}.tsv') for s in range(1,11)]
print('発見された行の数（種の中央） 腕',st.median([len(a) for a in A]),'基準',st.median([len(b) for b in B]))
out=[]
for rw in sorted(set().union(*A,*B)):
    a=[x.get(rw,1951) for x in A]; b=[x.get(rw,1951) for x in B]
    out.append((st.median(a)-st.median(b),rw,st.median(b),st.median(a),sum(v<1951 for v in b),sum(v<1951 for v in a)))
out.sort(key=lambda o:-o[0])
print('遅れ>10年の行',sum(o[0]>10 for o in out),'早まり>10年',sum(o[0]<-10 for o in out),'全',len(out))
print('差\t行\t基準の中央\t腕の中央\t基準の種\t腕の種')
for o in out: print('\t'.join(str(v) for v in (o[0],o[1],o[2],o[3],o[4],o[5])))
