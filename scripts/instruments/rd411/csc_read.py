# ★(411) 読む線（opus・読むだけ・2026-10-18）の読み手の写し＝(412) 合流で scratchpad から scripts/instruments/rd411/ へ移した（README §1-2 道具は scripts/instruments/）。
# ★(436) 計器の限界：清算の係数 ClearingIdleThrottledScan＝1（(434) 以後の既定）の生では種類 1 に律速の州の起こした行が混ざる＝欠けた割合の見積りは過大に出る（throttledRowsCount を除いて読む）。
# 清算の見る範囲（csc_s）から、四半期の最初の清算で番が飛ばした行の欠けた量を見積もる（成約の量×3＝N=4 の系統抽出）。引数＝計器の生のフォルダ（例 D:/PoB_work/keep_407/m407_ins）。
# シミュを回さない・計器の TSV を読むだけ。
import csv,sys,collections
NAT=['清','ロシア','イギリス','フランス','プロイセン','日本','オーストリア','アメリカ']
d=sys.argv[1]
acc=collections.defaultdict(lambda:[0,0,0.0])  # (nat,era,cls)->rows,dealt,qty
def era(y): return '1784-1829' if y<1830 else '1830-1869' if y<1870 else '1870-1950'
for s in range(1,11):
    with open(f'{d}/csc_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            if r['roleCode']!='0': continue
            n=r['nation']
            if n not in NAT: continue
            a=acc[(n,era(int(r['year'])),int(r['classCode']))]
            a[0]+=int(r['rowsCount']); a[1]+=int(r['dealtRowsCount']); a[2]+=float(r['qtyPerYear'])
print('nat era | cls0 rows dealt% qty | cls1(ban) rows dealt% qty | cls4 skipped rows | est lost qty share of first-clearing qty')
for n in NAT:
  for e in ['1784-1829','1830-1869','1870-1950']:
    c0=acc[(n,e,0)];c1=acc[(n,e,1)];c4=acc[(n,e,4)]
    if c0[0]==0: continue
    q1row=c1[2]/c1[0] if c1[0] else 0
    lost=c4[0]*q1row
    print(f'{n} {e} | {c0[0]} {100*c0[1]/max(1,c0[0]):.0f}% {c0[2]:.3g} | {c1[0]} {100*c1[1]/max(1,c1[0]):.0f}% {c1[2]:.3g} | {c4[0]} | {lost:.3g} {lost/max(1e-9,c0[2]+c1[2]+lost):.0%}')
print('--- 3x class1 estimate (N=4 systematic) of first-clearing deal qty missing')
for n in NAT:
  out=[]
  for e in ['1784-1829','1830-1869','1870-1950']:
    c0=acc[(n,e,0)][2];c1=acc[(n,e,1)][2]
    lost=3*c1; out.append(f'{e}:{lost/(c0+c1+lost):.0%}')
  print(n,' '.join(out))
