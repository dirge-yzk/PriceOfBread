# (831) c66ds の読むだけの台本：〈売れ残った労働〉の基準の材料（設計の材料の線・コードを変えない・シミュを回さない）。
# 入力：基線 m826 の計器の生（labor_s と factory_s・国×四半期・種 1〜10）。
# 出力：農業労働の 3 つの基準（能力 Capacity・産出 LastProduced・売れた LastSold）の比の分布を、時期ごとに標準出力へ。
#   sold/cap  ＝ labor_s の farmSoldPerMonth ÷ farmCapPerMonth（(826) の反証役が数えた比＝能力を基準にした〈売れた割合〉）
#   sold/prod ＝ farmSoldPerMonth ÷ factory_s の farmLaborMarketProducedUnitsPerMonth（EconomyStep.cs の失業の式〈1−LastSold÷LastProduced〉の分母）
#   prod/cap  ＝ 産出÷能力（能力のうち実際に作られた割合）
#   cap/head  ＝ 能力÷担い手の Size の和（factory_s の farmLaborSupplierPopK＝Size の和）＝Size 1 単位あたりの能力（Simulation.cs の農業労働力の生産性 1.0×7 と比べる）
#   unempModel＝ max(0, 1−sold/prod−0.7)（国の和で近似した今の失業の式・自給率は 61 カ国の既定 0.7 で置いた＝4 国の上書きは無視＝近似）
#   farmUnemp ＝ labor_s の farmUnemploymentShare（計器が POP の失業率を人数で重み付け）
# 使い方：python farm_unsold.py D:/PoB_work/keep_826/base_ins
import sys, os, statistics

def read(path):
    with open(path, 'rb') as f:
        lines = f.read().decode('utf-8').splitlines()
    h = lines[0].split('\t')
    return h, [l.split('\t') for l in lines[1:] if l]

def q(xs, p):
    if not xs: return float('nan')
    xs = sorted(xs); k = (len(xs) - 1) * p
    lo = int(k); hi = min(lo + 1, len(xs) - 1)
    return xs[lo] + (xs[hi] - xs[lo]) * (k - lo)

def main(d):
    bands = [(1783, 1785), (1786, 1799), (1800, 1849), (1850, 1899), (1900, 1950)]
    acc = {b: {k: [] for k in ('sc', 'sp', 'pc', 'cph', 'um', 'fu')} for b in bands}
    agg = {b: [0.0, 0.0, 0.0] for b in bands}   # Σsold・Σprod・Σcap（能力で重み付けた比）
    rows = 0
    for s in range(1, 11):
        lp = os.path.join(d, f'labor_s{s}.tsv'); fp = os.path.join(d, f'factory_s{s}.tsv')
        if not (os.path.exists(lp) and os.path.exists(fp)): print('欠け', s); continue
        hl, rl = read(lp); hf, rf = read(fp)
        il = {n: i for i, n in enumerate(hl)}; jf = {n: i for i, n in enumerate(hf)}
        fac = {}
        for r in rf:
            fac[(r[jf['quarter']], r[jf['nation']])] = r
        for r in rl:
            y = int(r[il['year']])
            b = next((bb for bb in bands if bb[0] <= y <= bb[1]), None)
            if b is None: continue
            f = fac.get((r[il['quarter']], r[il['nation']]))
            if f is None: continue
            try:
                cap = float(r[il['farmCapPerMonth']]); sold = float(r[il['farmSoldPerMonth']]); fu = float(r[il['farmUnemploymentShare']])
                prod = float(f[jf['farmLaborMarketProducedUnitsPerMonth']]); popk = float(f[jf['farmLaborSupplierPopK']])
            except ValueError:
                continue
            if not (cap > 0): continue
            rows += 1
            a = acc[b]
            a['sc'].append(sold / cap); a['pc'].append(prod / cap); a['fu'].append(fu)
            if prod > 0:
                a['sp'].append(sold / prod); a['um'].append(max(0.0, 1.0 - sold / prod - 0.7))
            if popk > 0: a['cph'].append(cap / popk)
            agg[b][0] += sold; agg[b][1] += prod; agg[b][2] += cap
    print(f'行（国×四半期・種 1〜10）＝{rows}')
    names = {'sc': 'sold/cap', 'sp': 'sold/prod', 'pc': 'prod/cap', 'cph': 'cap/head', 'um': 'unempModel', 'fu': 'farmUnemp'}
    for b in bands:
        a = acc[b]
        print(f'== {b[0]}〜{b[1]}  行 {len(a["sc"])}  能力で重み付けた sold/cap {agg[b][0]/max(1e-9,agg[b][2]):.4f}  sold/prod {agg[b][0]/max(1e-9,agg[b][1]):.4f}  prod/cap {agg[b][1]/max(1e-9,agg[b][2]):.4f}')
        for k in ('sc', 'sp', 'pc', 'cph', 'um', 'fu'):
            xs = a[k]
            if not xs: continue
            nz = sum(1 for x in xs if x > 1e-12) / len(xs)
            print(f'  {names[k]:10s} p10 {q(xs,.1):.4f}  中央 {q(xs,.5):.4f}  p90 {q(xs,.9):.4f}  最大 {max(xs):.4f}  正の割合 {nz:.3f}')

def side(d):
    # 補い 1：house_s の家内工業が使う農業労働の割合（homeFarmLaborUse÷farmLaborBought）＝(418) の型がすでに農民の時間をどれだけ使うか
    # 補い 2：fd_s の州×四半期の 自給÷必要量・市場に出す量÷必要量（開幕 1783〜85・州の格ごと）＝0.7 と 0.3 の実際の割れ方
    hs, fs = [], {'Core': ([], []), 'Colonial': ([], [])}
    for s in range(1, 11):
        hp = os.path.join(d, f'house_s{s}.tsv'); fp = os.path.join(d, f'fd_s{s}.tsv')
        if os.path.exists(hp):
            h, rows = read(hp); i = {n: k for k, n in enumerate(h)}
            for r in rows:
                try:
                    if int(r[i['year']]) > 1785: continue
                    b = float(r[i['farmLaborBoughtPerMonth']]); u = float(r[i['homeFarmLaborUsePerMonth']])
                except ValueError: continue
                if b > 0: hs.append(u / b)
        if os.path.exists(fp):
            h, rows = read(fp); i = {n: k for k, n in enumerate(h)}
            for r in rows:
                try:
                    if int(r[i['year']]) > 1785: continue
                    need = float(r[i['stapleNeedPerMonth']]); sub = float(r[i['subPerMonth']]); mk = float(r[i['marketNeedPerMonth']])
                except ValueError: continue
                t = r[i['tier']]
                if need > 0 and t in fs: fs[t][0].append(sub / need); fs[t][1].append(mk / need)
    if hs: print(f'== 家内工業の農業労働÷買われた農業労働（1783〜85・国×四半期 {len(hs)}）p10 {q(hs,.1):.4f} 中央 {q(hs,.5):.4f} p90 {q(hs,.9):.4f}')
    for t, (a, b) in fs.items():
        if a: print(f'== fd_s {t}（1783〜85・州×四半期 {len(a)}）自給÷必要量 p10 {q(a,.1):.4f} 中央 {q(a,.5):.4f} p90 {q(a,.9):.4f}／市場に出す量÷必要量 p10 {q(b,.1):.4f} 中央 {q(b,.5):.4f} p90 {q(b,.9):.4f}')

if __name__ == '__main__':
    dd = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/keep_826/base_ins'
    main(dd); side(dd)
