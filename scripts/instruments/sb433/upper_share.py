# (433) 買えた量の上位(rank≠0)割合を spend_s から出す。rank: 大衆(0)=1,2,11,12 / 近代エリート(1)=3,4,7,8,9,10,13 / その他(2)=0,5,6（svcbuy_s の rank 列から確認した対応）
import csv,sys,glob,statistics,collections,os
src=sys.argv[1]
RANK0={1,2,11,12,14}
PICK={'清','ロシア','オスマン','フランス','イギリス','プロイセン','オーストリア','日本'}
WIN={"Q8-39":["2"],"Q40-159":["3"],"Q160-399":["4","5","6","7"]}
res=collections.defaultdict(list)
for path in sorted(glob.glob(os.path.join(src,"spend_s*.tsv"))):
    acc=collections.defaultdict(float)
    for r in csv.DictReader(open(path,encoding='utf8'),delimiter='\t'):
        if r['kind']=='good' and r['slotName']=='家事奉仕':
            for w,bs in WIN.items():
                if r['bucket'] in bs:
                    q=float(r['qtyPerMonth'])*float(r['monthsCount'])
                    acc[(r['nation'],w,'U' if int(r['classId']) not in RANK0 else 'M')]+=q
    for n in {k[0] for k in acc if k[0] in PICK}:
        for w in WIN:
            u=acc[(n,w,'U')]; m=acc[(n,w,'M')]
            if u+m>0: res[(n,w)].append(u/(u+m))
for w in WIN:
    per={n:statistics.median(v) for (n,ww),v in res.items() if ww==w}
    print(w, "国の中央の中央 %.3f"%statistics.median(per.values()), "最小 %.3f 最大 %.3f"%(min(per.values()),max(per.values())), "国",len(per), {n:round(x,2) for n,x in sorted(per.items())})
