# (1189) 問い2：台帳 S12 の読み直し（keep_1184 の生・種 1〜10）。判定・両側・頻度はエンジンの閾値の式（計器が写した門の印）で数える。
#  母集合（混ぜない）：
#   M＝merch_s の州×四半期（計器の国の名簿の国の州・毎四半期）
#   X＝scalars_s の国×四半期（全ての国）  S＝svc_s の国×四半期（計器の国）  U＝urxpd_s の国×四半期の州×月の和（計器の国）
#   K＝class_s の国×時点（open を除く・計器の国）  E＝ed_s の国×四半期（全ての国）
#  使い方: python -X utf8 -I s12_v.py <ins> <種の起点> <種の終わり>
import csv, os, sys, collections, math
sys.stdout.reconfigure(newline='\n')
B, sb, se = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
def fl(v):
    try: return float(v)
    except (ValueError, TypeError): return float('nan')
def rd(t, s):
    p = os.path.join(B, f'{t}_s{s}.tsv')
    return csv.DictReader(open(p, encoding='utf-8'), delimiter='\t') if os.path.exists(p) else None
out = collections.OrderedDict()
def add(k, v): out[k] = out.get(k, 0) + v
SIX = ('イギリス', 'フランス', '清', '日本', 'プロイセン', 'ロシア')
for s in range(sb, se + 1):
    # X：教育法（S12-04・S12-20）
    r = rd('scalars', s)
    if r is not None:
        for x in r:
            add('X 行', 1); e = x['educationLaw']; add(f'X 教育法={e}', 1)
    # M：小営業の入口・転落（S12-07・23・29・30・31）
    r = rd('merch', s)
    if r is not None:
        first = {}
        for x in r:
            add('M 行', 1)
            if fl(x['lockBOpen']) > 0: add('M lockBOpen>0', 1)
            if fl(x['lockABlockedArtisan']) > 0: add('M lockABlockedArtisan>0', 1)
            if fl(x['artisanEmpty']) > 0: add('M artisanEmpty>0', 1)
            ap, af, mp, mf, mc = (fl(x[k]) for k in ('artisanPettyIn', 'artisanFallOut', 'merchPettyIn', 'merchFallOut', 'merchCapRiseOut'))
            if ap > 0: add('M artisanPettyIn>0', 1)
            if mf > 0: add('M merchFallOut>0', 1)
            if fl(x['merchFallGateIn']) > 0: add('M merchFallGateIn>0', 1)
            if mc > 0: add('M merchCapRiseOut>0', 1)
            for k, v in (('Σ artisanPettyIn', ap), ('Σ artisanFallOut', af), ('Σ merchPettyIn', mp), ('Σ merchFallOut', mf), ('Σ merchCapRiseOut', mc)):
                if v == v: add('M ' + k, v)
            y = int(x['year']); n = x['nation']
            if n in SIX:
                q = int(str(x['quarter']).lstrip('q'))
                if q == 0: add(f'M 開幕の職人K {n}', fl(x['artisanK']) / (se - sb + 1))
                if y <= 1800 and af == af: add(f'M 1783-1800 職人の転落K {n}', af / (se - sb + 1))
                if y <= 1800 and ap == ap: add(f'M 1783-1800 職人への入口K {n}', ap / (se - sb + 1))
    # S：役務の失業（S12-16）＝エンジンの式 unsold＝1−売れた÷作った（EconomyStep.cs:2527・州ごと）を国の和で近似
    r = rd('svc', s)
    if r is not None:
        for x in r:
            p, so = fl(x['producedPerMonth']), fl(x['soldPerMonth'])
            if not (p > 1e-12): continue
            u = 1 - so / p; add('S 行（作った＞0）', 1)
            if u > 0: add('S 使用人の売れ残り>0', 1)
            if u > 0.1: add('S 使用人の売れ残り>0.1', 1)
    # U：農奴の栓（S12-17）
    r = rd('urxpd', s)
    if r is not None:
        for x in r:
            add('U 州×月', fl(x['regionMonths'])); add('U serfGated', fl(x['serfGated'])); add('U capBound', fl(x['capBound']))
    # K：聖職者の下限・医者の割合（S12-25・S12-12 の記録）
    r = rd('class', s)
    if r is not None:
        tot = collections.defaultdict(float); cl = collections.defaultdict(float); doc = collections.defaultdict(float)
        for x in r:
            k = (x['time'], x['nation']); v = fl(x['sizeK'])
            if v != v: continue
            tot[k] += v
            if x['className'] == '聖職者': cl[k] += v
            if x['className'] == '医者': doc[k] += v
        open0 = {n: cl[(t, n)] / tot[(t, n)] for (t, n) in tot if t == 'open' and tot[(t, n)] > 0}
        for (t, n), T in tot.items():
            if t == 'open' or not (T > 0): continue
            add('K 国×時点', 1)
            if open0.get(n, 0) >= 0.002:
                add('K 開幕 0.2%以上の国×時点', 1)
                if cl[(t, n)] / T < 0.002: add('K うち聖職者<0.2%', 1)
            if doc[(t, n)] / T > 0.01: add('K 医者>1%', 1)
            if doc[(t, n)] / T > 0.02: add('K 医者>2%', 1)
    # E：奨学金（S12-36）
    r = rd('ed', s)
    if r is not None:
        for x in r:
            add('E 行', 1)
            if fl(x['higherSubsidyPerMonth']) > 0: add('E higherSubsidy>0', 1)
            if fl(x['higherWantPerMonth']) > 0: add('E higherWant>0', 1)
print(f'# {B} 種 {sb}..{se}')
for k, v in out.items(): print(f'{k}\t{v:.6g}')
