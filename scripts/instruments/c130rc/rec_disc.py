# (1270) c130rc 記録（腕の後に足した・門にしない）：disc_s の E 行で、種ごとに世界で最初に発見された行の数（1950 年まで・1913 年まで）。rec_w.py の代わり（engineer_s の行の和は行が消えると動く＝W として読めない）。
# 使い方: rec_disc.py <腕の生> <基準の生> <種の始め> <種の終わり>
import sys,csv,statistics
A,B,s0,s1=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4])
def cnt(d):
    res=[]
    for s in range(s0,s1+1):
        f=open(f"{d}/disc_s{s}.tsv",encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
        first={}
        for x in r:
            if x[ix['kind']]=='E': k=x[ix['row']]; y=int(x[ix['year']]); first[k]=min(first.get(k,9999),y)
        f.close(); res.append((len(first),sum(1 for v in first.values() if v<=1913)))
    return res
a=cnt(A); b=cnt(B)
print(f"# rec_disc 腕={A} 基準={B}")
print("腕（1950 まで,1913 まで）", a, "中央", statistics.median(x[0] for x in a), statistics.median(x[1] for x in a))
print("基準", b, "中央", statistics.median(x[0] for x in b), statistics.median(x[1] for x in b))
