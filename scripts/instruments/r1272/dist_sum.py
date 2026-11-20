# (1272) hol_dist の出力を腕ごとにまとめる（例外の 7 州を除く・自前の定義）
import sys,csv,statistics as st
EXC={'イカルイト','チャーチル','ナイン','ヌーク','バロー','フォートシンプソン','フォートユーコン'}
T='カルメン・デ・パタゴネス'
for p in sys.argv[1:]:
    R=[r for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t') if r['region'] not in EXC]
    col=[float(r['ratio']) for r in R if r['tierLast']=='Colonial']
    allr=[float(r['ratio']) for r in R]
    n10=sum(x<0.1 for x in allr); n25=sum(x<0.25 for x in allr)
    c25=sum(x<0.25 for x in col); c40=sum(x<0.4 for x in col)
    q=st.quantiles(col,n=20)
    pat=[(r['seed'],r['ratio'],r['peakYear'],r['minYear'],r['foodMean']) for r in R if r['region']==T]
    print(f"{p}: 州×種 {len(allr)}・1/10 未満 {n10}・1/4 未満 {n25}｜植民地格 {len(col)}・1/4 未満 {c25}・0.4 未満 {c40}・5%点 {q[0]:.3f}・10%点 {q[1]:.3f}・中央 {st.median(col):.3f}")
    print('   パタゴネス 種:比(ピーク年→最小年・食べ物):',' '.join(f"{s}:{float(x):.3f}({a}→{b},{float(f):.2f})" for s,x,a,b,f in pat))
