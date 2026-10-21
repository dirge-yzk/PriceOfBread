# 段0：旧の木（m428）の死の側を計器の生（pop_s）から読む。読むだけ。
import csv, glob, math, statistics, sys, collections
import os as _os, sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), '..', 'pop'))
import age_guard as _age_guard
_age_guard.require_bands8_argv()   # (490) 8 層（10 歳刻み）の pop_s だけを前提にする＝11 層の生なら止める
sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1]
NAT = ['イギリス','フランス','プロイセン','ロシア','日本','清','ネーデルラント']
def num(v):
    try: return float(v)
    except: return float('nan')
def med(xs):
    xs=[x for x in xs if x==x and not math.isinf(x)]
    return statistics.median(xs) if xs else float('nan')
ERAS=[(1783,1799),(1800,1849),(1850,1899),(1900,1949)]
acc=collections.defaultdict(list)
for f in sorted(glob.glob(D+'/pop_s*.tsv')):
    with open(f,encoding='utf-8') as fh:
        for r in csv.DictReader(fh,delimiter='\t'):
            if r['scope']!='N' or r['nation'] not in NAT: continue
            q=int(r['quarter']); y=int(r['year'])
            if q<=12: continue
            for i,(a,b) in enumerate(ERAS):
                if a<=y<=b: acc[(r['nation'],i)].append(r)
print('国\t時代\t行\tcbr‰\tcdr‰\t平常の死‰\t飢え比\tmu0\tmu1\tmu5\tmu7\tl60\ts10\ttfr代理')
for n in NAT:
    for i,(a,b) in enumerate(ERAS):
        rs=acc.get((n,i),[])
        if not rs: continue
        fam=med([num(r['deathsFamineK'])/max(1e-9,num(r['deathsNormalK'])+num(r['deathsFamineK'])+num(r['deathsOtherK'])) for r in rs])
        print(f"{n}\t{a}-{b}\t{len(rs)}\t{med([num(r['cbrPerMille']) for r in rs]):.1f}\t{med([num(r['cdrPerMille']) for r in rs]):.1f}\t{med([num(r['cdrNormalPerMille']) for r in rs]):.1f}\t{fam:.3f}\t{med([num(r['muFrom0PerYear']) for r in rs]):.3f}\t{med([num(r['muFrom10PerYear']) for r in rs]):.3f}\t{med([num(r['muFrom50PerYear']) for r in rs]):.3f}\t{med([num(r['muFrom70PerYear']) for r in rs]):.3f}\t{med([num(r['l60Years']) for r in rs]):.1f}\t{med([num(r['s10Share']) for r in rs]):.3f}\t{med([num(r['tfrProxy']) for r in rs]):.2f}")
