# 段0の診断（読むだけ・腕ではない）：基線の盤面で、旧の標的（割合が最大の 1 州）と、登録する形（標的 ∝ 都市の人口・発生 ∝ 国の都市の人口割合）を並べる。
# 使い方：python -X utf8 -I tgt2.py <計器の出力ディレクトリ> [種の数]
import csv,collections,sys,statistics as st
d=sys.argv[1]; NS=int(sys.argv[2]) if len(sys.argv)>2 else 10
KEY=['イギリス','フランス','プロイセン','ネーデルラント','日本','清','アメリカ','ロシア','オーストリア']
CORE=[('ネーデルラント','アムステルダム'),('イギリス','ロンドン'),('プロイセン','ベルリン'),('フランス','パリ'),('日本','江戸')]
hit_old=collections.defaultdict(list); hit_new=collections.defaultdict(list)   # (nation,region) -> 種ごとの期待件数（係数前）
burden=collections.defaultdict(list); burden2=collections.defaultdict(list); burden3=collections.defaultdict(list)   # nation -> 種ごとの (new, old) 期待死者（deathFrac 前・千人/年）
cnt=collections.defaultdict(list)      # nation -> 種ごとの (new, old) 期待発生件数（chance 前）
for s in range(1,NS+1):
    byq=collections.defaultdict(list)
    for r in csv.DictReader(open(f'{d}/region_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
        pop=float(r['populationK'])
        if pop>1e-9: byq[(int(r['quarter']),r['nation'])].append((r['region'],pop,pop-float(r['ruralPopulationK'])))
    HO=collections.defaultdict(float); HN=collections.defaultdict(float)
    BN3=collections.defaultdict(float); BN2=collections.defaultdict(float); BO=collections.defaultdict(float); BN=collections.defaultdict(float); CO=collections.defaultdict(float); CN=collections.defaultdict(float)
    for (q,n),L in byq.items():
        tot=sum(x[1] for x in L); urb=sum(x[2] for x in L)
        if urb<=1e-9: continue
        us=sorted(((x[2]/x[1],x) for x in L),key=lambda t:-t[0])
        mx,a=us[0]; sec=us[1] if len(us)>1 else None
        # 旧：発生確率 ∝ 最大の割合・標的＝最大の州・二次＝次点の州（0.5 倍）
        CO[n]+=mx/4; HO[(n,a[0])]+=mx/4
        BO[n]+=mx/4*(a[1]+(0.5*sec[1][1] if sec else 0))
        # 新：発生確率 ∝ 国の都市の人口割合・標的 ∝ 都市の人口・二次＝1 つ目を除いた同じ重み（0.5 倍）
        ush=urb/tot; CN[n]+=ush/4
        w={x[0]:x[2] for x in L}
        for x in L: HN[(n,x[0])]+=ush/4*x[2]/urb
        e=0.0; e2=0.0; e3=0.0
        for x in L:
            p=x[2]/urb; e+=p*x[1]; e2+=p*x[2]; e3+=p*x[1]*(x[2]/x[1])/ush
            if len(L)>1:
                rest=urb-x[2]
                if rest>1e-9:
                    e+=p*0.5*sum((y[2]/rest)*y[1] for y in L if y is not x)
                    e2+=p*0.5*sum((y[2]/rest)*y[2] for y in L if y is not x); e3+=p*0.5*sum((y[2]/rest)*y[1]*(y[2]/y[1])/ush for y in L if y is not x)
        BN[n]+=ush/4*e; BN2[n]+=ush/4*e2; BN3[n]+=ush/4*e3
    for n,reg in CORE:
        hit_old[(n,reg)].append(HO.get((n,reg),0)); hit_new[(n,reg)].append(HN.get((n,reg),0))
    for n in set(k for k in CO): burden[n].append((BN[n],BO[n])); burden2[n].append((BN2[n],BO[n])); burden3[n].append((BN3[n],BO[n])); cnt[n].append((CN[n],CO[n]))
print('## 発生の件数（chance 前の和）新÷旧・国ごとの中央値（種の中央値）')
rat=[]
for n in sorted(cnt):
    v=[a/b for a,b in cnt[n] if b>0]
    if v: rat.append((n,st.median(v)))
print('  全国の中央値 %.2f・最小 %s・最大 %s'%(st.median([r for _,r in rat]),min(rat,key=lambda t:t[1]),max(rat,key=lambda t:t[1])))
for n in KEY: print('  ',n,'%.2f'%st.median([a/b for a,b in cnt[n]]))
print('## 期待死者（deathFrac 前・千人換算の和）新÷旧')
rb=[]
for n in sorted(burden):
    v=[a/b for a,b in burden[n] if b>0]
    if v: rb.append((n,st.median(v)))
print('  全国の中央値 %.2f・四分位 %.2f〜%.2f'%(st.median([r for _,r in rb]),st.quantiles([r for _,r in rb],n=4)[0],st.quantiles([r for _,r in rb],n=4)[2]))
for n in KEY: print('  ',n,'%.2f'%st.median([a/b for a,b in burden[n]]))
print('## 中核の州の期待件数（chance 前・種の中央値）旧→新')
for n,reg in CORE:
    print('  ',n,reg,'%.1f → %.1f'%(st.median(hit_old[(n,reg)]),st.median(hit_new[(n,reg)])))

print('## 期待死者 新（死が都市の POP だけに掛かる形）÷旧')
rb2=[]
for n in sorted(burden2):
    v=[a/b for a,b in burden2[n] if b>0]
    if v: rb2.append((n,st.median(v)))
print('  全国の中央値 %.2f・四分位 %.2f〜%.2f'%(st.median([r for _,r in rb2]),st.quantiles([r for _,r in rb2],n=4)[0],st.quantiles([r for _,r in rb2],n=4)[2]))
for n in KEY: print('  ',n,'%.2f'%st.median([a/b for a,b in burden2[n]]))

print('## 期待死者 新（死の率が州の割合÷国の割合に比例）÷旧')
rb3=[]
for n in sorted(burden3):
    v=[a/b for a,b in burden3[n] if b>0]
    if v: rb3.append((n,st.median(v)))
print('  全国の中央値 %.2f・四分位 %.2f〜%.2f'%(st.median([r for _,r in rb3]),st.quantiles([r for _,r in rb3],n=4)[0],st.quantiles([r for _,r in rb3],n=4)[2]))
for n in KEY: print('  ',n,'%.2f'%st.median([a/b for a,b in burden3[n]]))
