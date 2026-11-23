# (1324) 紙：基準 keep_1321/base_ins の arrears_s（旗 0 の影）で、金本位の月に新の述語が真の月と、そのうち 1−r＜1e-12 の月を数える
import csv,sys,collections
raw=sys.argv[1]; s0,s1=int(sys.argv[2]),int(sys.argv[3])
tot=0;tiny=0;tinyuk=0;tot_uk=0;per=[];near=collections.Counter()
for s in range(s0,s1+1):
    n=0
    for r in csv.DictReader(open(f'{raw}/arrears_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
        if r['onGoldBefore']=='1' and r['predNew']=='1':
            tot+=1;n+=1; d=1-float(r['payRate'])
            uk=r['nation']=='イギリス'; tot_uk+=uk
            if d<1e-12: tiny+=1; tinyuk+=uk
            for k,t in (('<1e-12',1e-12),('<1e-6',1e-6),('<1e-3',1e-3),('<1e-2',1e-2)):
                if d<t: near[k]+=1
    per.append(n)
print(f'金本位の月に新の述語が真 {tot}（英 {tot_uk}）種ごと {per}')
print(f'そのうち 1−r＜1e-12 {tiny}（英 {tinyuk}）・累積 {dict(near)}')
