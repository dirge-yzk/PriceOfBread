# (1293) c133fr 紙（読むだけ・腕なし）：移る値打ちから食べる量の費用 c を引いた形 g′ を、国×年代の平均で近似する
#   使い方: python -X utf8 -I gnet1293.py <出力 tsv> <ins のフォルダ> <種の起点>
#   V_r＝稼ぎ_r＋自給の値・V_u＝稼ぎ_u−家賃（urxpd_s の wIncR・wSubV・wIncU・wHousU÷sumTotal＝今の MigrationValues と同じ項の平均）
#   g＝2(V_u−V_r)/(V_u+V_r)（平均から作る＝州×月の g の平均 wGsym とは別物＝並べて名乗る）
#   c（1 人・月の主食の必要量の値）は生に無い＝括弧で置く：下＝自給の値 subV（c≧subV＝自給の値は必要量で切ってある）・上＝subV÷σ（σ＝subst_s の subRateWeighted を本国格の州の subPopK で重みづけ）
#   Ṽ＝max(0, V−c)・g′＝2(Ṽ_u−Ṽ_r)/(Ṽ_u+Ṽ_r)
import sys, csv, os, statistics as st, collections
NAT = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'オスマン', 'オーストリア']
WIN = [(1783, 1814), (1815, 1849), (1850, 1879), (1880, 1913), (1914, 1950)]
def win_of(y):
    for a, b in WIN:
        if a <= y <= b: return f'{a}-{b}'
    return None
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
def gs(vu, vr):
    vu = max(0.0, vu); vr = max(0.0, vr); s = vu + vr
    return 2 * (vu - vr) / s if s > 1e-12 else 0.0
outp, d, sb = sys.argv[1], sys.argv[2], int(sys.argv[3])
res = collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(sb, sb + 10):
    pu = os.path.join(d, f'urxpd_s{s}.tsv'); ps = os.path.join(d, f'subst_s{s}.tsv')
    if not (os.path.exists(pu) and os.path.exists(ps)): continue
    a = collections.defaultdict(lambda: collections.defaultdict(float))
    with open(pu, encoding='utf-8') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            w = win_of(int(r['year']))
            if r['nation'] in NAT and w:
                for c in ('sumTotal', 'wIncU', 'wIncR', 'wSubV', 'wHousU', 'wGsym', 'famineGated', 'regionMonths'): a[(r['nation'], w)][c] += float(r[c])
    with open(ps, encoding='utf-8') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            w = win_of(int(r['year']))
            if r['nation'] in NAT and w and r['tier'] == 'Core':
                sp = float(r['subPopK']); sr = float(r['subRateWeighted'])
                if sp > 0 and sr == sr: a[(r['nation'], w)]['sigW'] += sp * sr; a[(r['nation'], w)]['sigN'] += sp
    for k, v in a.items():
        t = v['sumTotal']
        if t <= 0: continue
        iu, ir, sv, ho = v['wIncU'] / t, v['wIncR'] / t, v['wSubV'] / t, v['wHousU'] / t
        sig = v['sigW'] / v['sigN'] if v['sigN'] > 0 else float('nan')
        vr, vu = ir + sv, iu - ho
        x = res[k]
        x['incU'].append(iu); x['incR'].append(ir); x['subV'].append(sv); x['hous'].append(ho); x['sigma'].append(sig)
        x['g_mean'].append(gs(vu, vr)); x['wGsym'].append(v['wGsym'] / t); x['gated'].append(v['famineGated'] / v['regionMonths'] if v['regionMonths'] > 0 else float('nan'))
        for tag, c in (('lo', sv), ('hi', sv / sig if sig == sig and sig > 0 else float('nan'))):
            x['c_' + tag].append(c)
            x['gnet_' + tag].append(gs(vu - c, vr - c) if c == c else float('nan'))
            x['VuNet0_' + tag].append(1.0 if c == c and vu - c <= 0 else 0.0)
cols = ['incU', 'incR', 'subV', 'hous', 'sigma', 'c_lo', 'c_hi', 'g_mean', 'wGsym', 'gnet_lo', 'gnet_hi', 'VuNet0_lo', 'VuNet0_hi', 'gated']
with open(outp, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('nation\twindow\t' + '\t'.join(cols) + '\n')
    for n in NAT:
        for lo, hi in WIN:
            x = res[(n, f'{lo}-{hi}')]
            fo.write(f'{n}\t{lo}-{hi}\t' + '\t'.join('nan' if med(x[c]) != med(x[c]) else f'{med(x[c]):.4f}' for c in cols) + '\n')
print('ok', outp)
