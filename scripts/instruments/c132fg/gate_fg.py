# (1283) c132fg 史実の桁の線（この線が一番動かす量）と記録（読むだけ）。
#  H1 国ごとの都市化率＝本国の州（Core と Integrated）の (populationK−ruralPopulationK)÷populationK（region_s）・1850 年と 1913 年の 4 四半期の平均・種の中央。
#     史実の値（〈推〉・都市の定義は国ごとに違う＝桁だけを見る）：英 1851 年の国勢調査の都市 0.54・1911 年 0.79（イングランドとウェールズ）／仏 1851 年 0.26・1911 年 0.44（人口 2000 以上の市町村）／
#     普 1849 年 0.28・独 1910 年 0.60（2000 以上）／米 1850 年 0.15・1910 年 0.46（2500 以上）／露 1850 年 0.07・1913 年 0.15（1897 年の国勢調査 0.13）／
#     日 1850 年 0.12・1913 年 0.18（〈推〉）／清 1850 年 0.06・1913 年 0.06（Rozman ほか〈推〉）／墺 1850 年 0.15・1910 年 0.35〈推〉／オスマン 1850 年 0.15・1913 年 0.20〈推〉。
#     NG＝腕÷史実が 0.1 未満か 10 超で、かつ基準÷史実が 0.1〜10 の内（線が原因の桁の外れ）。基準もすでに外なら〈元から〉で記録。
#  H2 国ごとの飢えの死（popclose_s の deathsFamineK・全期間・種 10 本の和・基準の和 ≥100 千人の国）＝史実の数が手元に無い＝基準を代わりに置く（AI の決め）。腕÷基準が 0.1 未満か 10 超＝NG。
#  記録：都市と農村の往復（urxrt_s の route=PopDecision の urbanInK・urbanOutK・9 国）・飢えの門の州×月（urxpd_s の famineGated）・本国の州の都市の割合の 1800・1850・1900・1940 年。
# 使い方: python gate_fg.py <腕の生> <基準の生> <種の起点>
import sys,statistics,collections
A,B,SB=sys.argv[1],sys.argv[2],int(sys.argv[3]); seeds=list(range(SB,SB+10))
HIST={'イギリス':(0.54,0.79),'フランス':(0.26,0.44),'プロイセン':(0.28,0.60),'アメリカ':(0.15,0.46),'ロシア':(0.07,0.15),'日本':(0.12,0.18),'清':(0.06,0.06),'オーストリア':(0.15,0.35),'オスマン':(0.15,0.20)}
def key(n):
    for k in HIST:
        if k in n: return k
    return None
def urb(D,s,years):
    acc=collections.defaultdict(lambda:[0.0,0.0])
    f=open(f'{D}/region_s{s}.tsv',encoding='utf-8'); h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
    for line in f:
        c=line.rstrip('\n').split('\t'); y=int(c[ix['year']])
        if y not in years or c[ix['tier']] not in ('Core','Integrated'): continue
        k=key(c[ix['nation']])
        if not k: continue
        p=float(c[ix['populationK']]); ru=float(c[ix['ruralPopulationK']])
        a=acc[(k,y)]; a[0]+=p; a[1]+=p-ru
    f.close()
    return {kk:(v[1]/v[0] if v[0]>0 else float('nan')) for kk,v in acc.items()}
def famine(D,s):
    acc=collections.Counter()
    f=open(f'{D}/popclose_s{s}.tsv',encoding='utf-8'); h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
    for line in f:
        c=line.rstrip('\n').split('\t')
        try: acc[c[ix['nation']]]+=float(c[ix['deathsFamineK']])
        except ValueError: pass
    f.close(); return acc
def rt(D,s):
    acc=collections.defaultdict(lambda:[0.0,0.0])
    try: f=open(f'{D}/urxrt_s{s}.tsv',encoding='utf-8')
    except FileNotFoundError: return None
    h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
    for line in f:
        c=line.rstrip('\n').split('\t')
        if c[ix['route']]!='PopDecision': continue
        a=acc[key(c[ix['nation']]) or c[ix['nation']]]; a[0]+=float(c[ix['urbanInK']]); a[1]+=float(c[ix['urbanOutK']])
    f.close(); return acc
def gated(D,s):
    try: f=open(f'{D}/urxpd_s{s}.tsv',encoding='utf-8')
    except FileNotFoundError: return None
    h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}; g=rm=0; fn=0.0
    for line in f:
        c=line.rstrip('\n').split('\t'); g+=int(c[ix['famineGated']]); rm+=int(c[ix['regionMonths']]); fn+=float(c[ix['sumFlowNeg']])
    f.close(); return g,rm,fn
YRS=[1800,1850,1900,1913,1940]
res={}
for tag,D in (('arm',A),('base',B)):
    U=collections.defaultdict(list); F=collections.Counter(); R=collections.defaultdict(lambda:[0.0,0.0]); G=[0,0,0.0]
    for s in seeds:
        u=urb(D,s,set(YRS))
        for kk,v in u.items(): U[kk].append(v)
        for n,v in famine(D,s).items(): F[n]+=v
        r=rt(D,s)
        if r:
            for n,v in r.items(): R[n][0]+=v[0]; R[n][1]+=v[1]
        g=gated(D,s)
        if g: G[0]+=g[0]; G[1]+=g[1]; G[2]+=g[2]
    res[tag]=(U,F,R,G)
print(f'# gate_fg 腕 {A}・基準 {B}・種 {seeds[0]}..{seeds[-1]}')
ng=0
print('== H1 都市化率（本国の州・種の中央）== 国\t年\t腕\t基準\t史実\t腕÷史実\t基準÷史実\t判定')
for k,(h50,h13) in HIST.items():
    for y,hv in ((1850,h50),(1913,h13)):
        a=res['arm'][0].get((k,y)); b=res['base'][0].get((k,y))
        if not a or not b: print(f'{k}\t{y}\t-\t-\t{hv}\t-\t-\t読めない'); continue
        am=statistics.median(a); bm=statistics.median(b); ra=am/hv; rb=bm/hv
        out=not(0.1<=ra<=10); bout=not(0.1<=rb<=10)
        v='NG' if out and not bout else ('元から外' if out else '内')
        ng+=v=='NG'
        print(f'{k}\t{y}\t{am:.4f}\t{bm:.4f}\t{hv}\t{ra:.3f}\t{rb:.3f}\t{v}')
print('== H2 飢えの死（千人・種 10 本の和・基準 ≥100 千人の国）== 国\t腕\t基準\t腕÷基準\t判定')
Fa,Fb=res['arm'][1],res['base'][1]
for n in sorted(Fb,key=lambda x:-Fb[x]):
    if Fb[n]<100: continue
    r=Fa[n]/Fb[n]; v='NG' if not(0.1<=r<=10) else '内'; ng+=v=='NG'
    print(f'{n}\t{Fa[n]:.1f}\t{Fb[n]:.1f}\t{r:.3f}\t{v}')
print(f'飢えの死の世界の和（千人・種 10 本）\t腕 {sum(Fa.values()):.1f}\t基準 {sum(Fb.values()):.1f}')
print(f'NG の数\t{ng}')
print('== 記録：本国の都市化率（種の中央）== 国\t年\t腕\t基準')
for k in HIST:
    for y in YRS:
        a=res['arm'][0].get((k,y)); b=res['base'][0].get((k,y))
        if a and b: print(f'{k}\t{y}\t{statistics.median(a):.4f}\t{statistics.median(b):.4f}')
print('== 記録：都市と農村の往復（urxrt route=PopDecision・千人・種 10 本の和）== 国\t腕 都市へ\t腕 都市から\t基準 都市へ\t基準 都市から')
Ra,Rb=res['arm'][2],res['base'][2]
for n in sorted(set(Ra)|set(Rb)):
    a=Ra.get(n,[0,0]); b=Rb.get(n,[0,0]); print(f'{n}\t{a[0]:.1f}\t{a[1]:.1f}\t{b[0]:.1f}\t{b[1]:.1f}')
Ga,Gb=res['arm'][3],res['base'][3]
print(f'== 記録：飢えの門（urxpd・本国の州・9 国）== 腕 門の州×月 {Ga[0]}／州×月 {Ga[1]}・都市→農村の流れの和 {Ga[2]:.1f} 千人\t基準 {Gb[0]}／{Gb[1]}・{Gb[2]:.1f} 千人')
