# ★(411) 読む線（opus・読むだけ・2026-10-18）の読み手の写し＝(412) 合流で scratchpad から scripts/instruments/rd411/ へ移した（README §1-2 道具は scripts/instruments/）。
# 発見の表（disc_s の E 行）から、主要 6 国が各行を見つけた年の種の中央値を 2 つの生で並べる。引数＝計器の生のフォルダ 2 つと行の名前（カンマ区切り）。
# シミュを回さない・計器の TSV を読むだけ。
import csv,sys,collections,statistics as st
NAT=['清','ロシア','イギリス','フランス','プロイセン','日本']
ROWS=sys.argv[3].split(',')
def load(d):
    res=collections.defaultdict(list)
    kinds=collections.Counter()
    for s in range(1,11):
        seen={}
        with open(f'{d}/disc_s{s}.tsv',encoding='utf-8') as f:
            for r in csv.DictReader(f,delimiter='\t'):
                kinds[r['kind']]+=1
                if r['kind'] not in ('event','found','discover','E'): pass
                k=(r['nation'],r['row'])
                if r['kind']=='E' and k[0] in NAT and k[1] in ROWS and k not in seen: seen[k]=int(r['year'])
        for k,v in seen.items(): res[k].append(v)
    return res,kinds
A,ka=load(sys.argv[1]);B,kb=load(sys.argv[2])
print(ka.most_common(5))
for n in NAT:
  for rw in ROWS:
    a=A.get((n,rw),[]);b=B.get((n,rw),[])
    print(n,rw,len(a),st.median(a) if a else '-',len(b),st.median(b) if b else '-')
