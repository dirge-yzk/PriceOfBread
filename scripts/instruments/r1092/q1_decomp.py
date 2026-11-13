# (1092) 問い 1：L を [兵÷69.125]×[暮らし]×[装備の充足]×[残り] に分ける（stock の表・計器 9 国）。rulepower の L と stock の landPowerIndex の一致も数える（問い 2 (c)）。
# 使い方: python -X utf8 -I q1_decomp.py <表のフォルダ> > q1_out.txt
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
d = sys.argv[1]; QS = [0, 1, 2, 3, 4, 8, 12, 16, 20, 28, 40]; SPM = 250.0 * 0.2765
NAT = ['フランス', 'オーストリア', 'プロイセン', 'ロシア', 'イギリス', 'オスマン', '清', '日本', 'アメリカ']
V = collections.defaultdict(lambda: collections.defaultdict(lambda: collections.defaultdict(list)))  # V[nat][key][q] -> list over seeds
mism = 0; nmatch = 0; maxrel = 0.0
for s in range(1, 11):
    rl = {}
    with open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fi:
            c = line.rstrip('\n').split('\t'); q = int(c[0])
            if q > max(QS): break
            rl.setdefault((q, c[ix['nation']]), float(c[ix['lNationIndex']]))
    with open(os.path.join(d, f'stock_s{s}.tsv'), encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fi:
            c = line.rstrip('\n').split('\t'); t = c[0]
            if not t.startswith('q'): continue
            q = int(t[1:])
            if q > max(QS): break
            n = c[ix['nation']]; L = float(c[ix['landPowerIndex']])
            if (q, n) in rl:
                nmatch += 1; r = rl[(q, n)]
                rel = abs(r - L) / max(1e-12, abs(L)); maxrel = max(maxrel, rel)
                if rel > 1e-9: mism += 1
            if q not in QS or n not in NAT: continue
            sol = float(c[ix['soldierK']]); rd = float(c[ix['militaryReadinessShare']]); eq = float(c[ix['capitalEquipFillShare']])
            wel = rd / eq if eq > 1e-12 else float('nan')
            base = sol / SPM * rd
            rest = L / base if base > 1e-12 else float('nan')
            for k, v in (('L', L), ('兵K', sol), ('暮らし', wel), ('装備', eq), ('即応', rd), ('残り', rest), ('訓練', float(c[ix['trainingFillShare']])),
                         ('市火砲', float(c[ix['capitalArtilleryStock']])), ('市小銃', float(c[ix['capitalRifleStock']])), ('市弾薬', float(c[ix['capitalAmmunitionStock']])), ('戦', float(c[ix['atWarFlag']]))):
                V[n][k][q].append(v)
print(f'(c) rulepower の L と stock の landPowerIndex：照合 {nmatch} 行・相対差 1e-9 超 {mism} 行・最大の相対差 {maxrel:.3g}')
def med(a):
    a = [x for x in a if x == x]
    return st.median(a) if a else float('nan')
print('値は 10 種の中央（括弧は最小〜最大）・四半期 ' + ' '.join(map(str, QS)))
for n in NAT:
    print(f'== {n}')
    for k in ('L', '兵K', '暮らし', '装備', '残り', '訓練', '市火砲', '市小銃', '市弾薬', '戦'):
        print(f'{k}\t' + '\t'.join(f'{med(V[n][k][q]):.3g}' for q in QS))
    # 比（四半期 20÷0）の種ごとの幅
    for k in ('L', '兵K', '暮らし', '装備', '残り'):
        rs = [b / a for a, b in zip(V[n][k][0], V[n][k][20]) if a == a and b == b and a > 1e-12]
        if rs: print(f'  比20/0 {k}\t中央 {st.median(rs):.3g}\t幅 {min(rs):.3g}〜{max(rs):.3g}')
    # 底の深さと時期（四半期 0〜40 の L の最小÷四半期 0 の L・その四半期）は別の台本で
