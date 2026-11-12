import csv,collections,sys,statistics as st
# (1076) どちらの側が発注を止めたか：国×種ごとに、fm の needSeaIndex が navalPowerIndex を最初に超える年（年の最初の fm 行）と、予算が最初に正になる年（1784 以降・stock の四半期の行）
# 必要側の目標の強さ＝NeedSea÷海の熟練（EconomyStep.cs:377）。海軍力＝艦のゲージ×海の熟練×乗員の充足ほか（Diplomacy の同期）＝NeedSea と海軍力の大小は〈必要側の目標が在庫を超えるか〉の近似
K='D:/PoB_work/keep_1069'
arm=sys.argv[1]; NS=sys.argv[2].split(',')
seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
print(arm,'国\t種\t needSea>海軍力の最初の年\t予算>0の最初の年(1784〜)\t差\t1784のneedSea\t1784の海軍力')
for N in NS:
    diffs=[]
    for s in seeds:
        cross=None; v84=None; seen=set()
        with open(f'{K}/{d}/fm_s{s}.tsv',encoding='utf-8') as f:
            for r in csv.DictReader(f,delimiter='\t'):
                if r['nation']!=N: continue
                y=int(float(r['year']))
                if y in seen: continue
                seen.add(y)
                try: ns=float(r['needSeaIndex']); npw=float(r['navalPowerIndex'])
                except: continue
                if y==1784: v84=(ns,npw)
                if y>=1784 and cross is None and ns>npw: cross=y
        bud=None
        with open(f'{K}/{d}/stock_s{s}.tsv',encoding='utf-8') as f:
            for r in csv.DictReader(f,delimiter='\t'):
                if r['nation']!=N or r['time'] in('open','warm'): continue
                y=int(float(r['year']))
                if y<1784: continue
                try: b=float(r['navalBudgetCapitalPerMonth'])+float(r['navalBudgetOtherPerMonth'])
                except: continue
                if b>0: bud=y; break
        dd=(bud-cross) if bud is not None and cross is not None else None
        if dd is not None: diffs.append(dd)
        print(N,s,cross,bud,dd,f'{v84[0]:.3f}' if v84 else '',f'{v84[1]:.3f}' if v84 else '',sep='\t')
    print(f'# {N} 差（予算の年−超えた年）の中央',st.median(diffs) if diffs else 'nan','／一致（|差|≤1）の種',sum(1 for x in diffs if abs(x)<=1),'／',len(diffs))
