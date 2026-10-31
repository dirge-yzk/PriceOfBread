"""(704) r1 段0 ③：probe_decomp.txt（普通の助走・社会移動だけ止める・階級遷移だけ止める・旗 1 の 4 通りの助走直後の階級の人数）の読み。
probe_decomp.txt は一時のテスト（コミットしない）が書いた：open〔構築直後〕・normal・stopMeritOnly〔社会移動だけ止める〕・stopPopDecOnly〔階級遷移だけ止める〕・flag1。クラス番号 2＝工場労働者・1＝農民。
"""
import collections
d=collections.defaultdict(dict)
for l in open('D:/PoB_work/r1/probe.txt',encoding='utf-8'):
    x=l.rstrip('\n').split('\t'); tag,n=x[0],x[1]
    d[(tag,n)]={k.split('=')[0]:float(k.split('=')[1]) for k in x[2:]}
foc=['清','ロシア','オスマン','フランス','イギリス','プロイセン','オーストリア','日本','アメリカ']
tags=['open','normal','stopMeritOnly','stopPopDecOnly','flag1']
def sh(t,n,c): s=sum(d[(t,n)].values()); return d[(t,n)].get(c,0)/s
for c,nm in (('2','工場労働者'),('1','農民')):
    print('==',nm)
    for n in foc:
        print(n, ' '.join(f'{t}:{sh(t,n,c):.4f}' for t in tags))
import statistics as st
for t in tags[1:]:
    v=[abs(sh(t,n,'2')-sh('open',n,'2')) for n in foc]
    v61=[abs(sh(t,n,'2')-sh('open',n,'2')) for (tt,n) in list(d) if tt==t]
    print(t,'9国|Δ工場労働者割合|中央値',round(st.median(v),5),'最大',round(max(v),5),'| 61国中央値',round(st.median(v61),5))
# all-class L1 for flag1
for t in tags[1:]:
    for n in foc:
        keys=d[('open',n)].keys()
        l1=sum(abs(sh(t,n,k)-sh('open',n,k)) for k in keys)
        print(t,n,'L1',round(l1,5), end=' | ')
    print()
# soldiers (class id?) per class
print(sorted(d[('open','日本')].items(), key=lambda kv:int(kv[0])))
print(sorted(d[('flag1','日本')].items(), key=lambda kv:int(kv[0])))
