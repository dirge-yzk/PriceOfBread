# (1270) c130rc 史実の桁の線（10 倍）と記録。読むだけ。
# 使い方: gate_rc.py <腕の生> <基準の生> <種の始め> <種の終わり>
# H1 国ごとの技師（class_s の技師の sizeK・1851・1913 年・種の中央）：腕÷基準が 0.1 未満か 10 超＝NG（基準を史実の代わりに置く＝AI の決め・基準の中央 ≥1 千人の国×年だけ）。
# H2 招聘の植民地格への割合（popclose_s の specialistInK・格 Colonial の和÷全体の和・種 10 本の和）：腕÷基準が 0.1 未満か 10 超＝NG（上の側は割合 ≤1 で届かない＝名乗る）。
# H3 国ごとの招聘で着いた量（popclose_s の specialistInK の受け手の国の和・種 10 本の和）：腕÷基準が 0.1 未満か 10 超＝NG（基準の和 ≥100 千人の国だけ）。
# 記録：格ごとの量・国ごとに着いた州の数・最大の 1 州への集中（国の受け入れのうち最大の州の割合の中央）。
import sys,csv,statistics
A,B,s0,s1=sys.argv[1],sys.argv[2],int(sys.argv[3]),int(sys.argv[4])
def rows(p):
    f=open(p,encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
    for x in r: yield ix,x
    f.close()
def load(d):
    eng={}; tier={}; nat={}; reg={}
    for s in range(s0,s1+1):
        for ix,x in rows(f"{d}/class_s{s}.tsv"):
            if x[ix['className']]!='技師' or not x[ix['time']].startswith('q'): continue
            y=int(x[ix['year']])
            if y in (1851,1913): eng.setdefault((x[ix['nation']],y),[]).append(float(x[ix['sizeK']]))
        for ix,x in rows(f"{d}/popclose_s{s}.tsv"):
            v=float(x[ix['specialistInK']])
            if not v>0: continue
            n=x[ix['nation']]; t=x[ix['tier']]
            tier[t]=tier.get(t,0)+v; nat[n]=nat.get(n,0)+v; k=(s,n); reg.setdefault(k,{}); reg[k][x[ix['region']]]=reg[k].get(x[ix['region']],0)+v
    return eng,tier,nat,reg
ea,ta,na,ra=load(A); eb,tb,nb,rb=load(B)
ng=0
print(f"# gate_rc 腕={A} 基準={B} 種={s0}..{s1}")
print("== H1 国ごとの技師（1851・1913・種の中央・腕÷基準）")
for k in sorted(eb):
    mb=statistics.median(eb[k]); 
    if not mb>=1.0 or k not in ea: continue
    ma=statistics.median(ea[k]); r=ma/mb; bad=not (0.1<=r<=10)
    ng+=bad
    if bad or k[0] in ('イギリス','フランス','プロイセン','ロシア','オスマン','清','日本','アメリカ','オーストリア'): print(f"H1\t{k[0]}\t{k[1]}\t腕 {ma:.1f}\t基準 {mb:.1f}\t比 {r:.3f}\t{'NG' if bad else ''}")
sa=sum(ta.values()); sb=sum(tb.values())
ca=ta.get('Colonial',0)/sa if sa>0 else float('nan'); cb=tb.get('Colonial',0)/sb if sb>0 else float('nan')
r2=ca/cb if cb>0 else float('nan'); bad2=not (0.1<=r2<=10); ng+=bad2
print(f"== H2 招聘の植民地格への割合\t腕 {ca:.4f}\t基準 {cb:.4f}\t比 {r2:.3f}\t{'NG' if bad2 else ''}")
print("== H3 国ごとの着いた量（種 10 本の和・千人）")
for n in sorted(nb, key=lambda n:-nb[n]):
    if not nb[n]>=100: continue
    a=na.get(n,0); r=a/nb[n]; bad=not (0.1<=r<=10); ng+=bad
    print(f"H3\t{n}\t腕 {a:.1f}\t基準 {nb[n]:.1f}\t比 {r:.3f}\t{'NG' if bad else ''}")
print(f"== 記録：格ごと（腕／基準・千人）")
for t in sorted(set(ta)|set(tb)): print(f"格\t{t}\t{ta.get(t,0):.1f}\t{tb.get(t,0):.1f}")
print(f"合計\t{sa:.1f}\t{sb:.1f}")
def conc(R):
    v=[max(d.values())/sum(d.values()) for d in R.values() if sum(d.values())>0]; m=[len(d) for d in R.values()]
    return (statistics.median(v) if v else float('nan'), statistics.median(m) if m else float('nan'))
print(f"記録：国×種の最大の 1 州の割合の中央・着いた州の数の中央\t腕 {conc(ra)}\t基準 {conc(rb)}")
print(f"判定 NG の数\t{ng}")
