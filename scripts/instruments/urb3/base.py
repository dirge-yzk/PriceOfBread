# urb3 (706)：都市化の式の計器（urxpd_s の wSubV・wHousU・wGsym・flowFlipRegionMonths）と往復（urx_s・urxrt_s）の基線の読み手（読むだけ）
# 使い方：python -X utf8 -I scripts/instruments/urb3/base.py <ラベル>=<計器の生のフォルダ> [<ラベル>=<フォルダ> ...] [--seeds 1-10]
#   各フォルダの urxpd_s<種>.tsv・urx_s<種>.tsv・urxrt_s<種>.tsv を読む（フォルダ直下・種ごと）。新しい列（wGsym 等）が無いフォルダは W1 を出さない（W2・戻す割合だけ）。
# 出力（標準出力）：
#   W1  ＝対称の相対差の絶対値の 45 窓（9 国×5 年代）の中央値。窓の値＝種の中央値（種ごとに Σ wGsym÷Σ sumTotal）の絶対値。
#   W1b ＝手計算 eq.py と同じ形（窓の合計の所得から 2((iu−hu)−(ir+sv))÷((iu−hu)+(ir+sv)））の絶対値の 45 窓の中央値＝手計算の 0.217 と並べる。ただし sv・hu は計器の実測（手計算は相対価格×物価水準の見積もり）。
#   W2  ＝対の名目の都市÷農村（Σ wIncU÷Σ wIncR）の 45 窓の中央値（窓の値＝種の中央値）。
#   戻す割合＝社会移動の都市への純÷月次政治の純の符号を替えたもの（urx_s・本国の群・9 国・全期間の和）と、urxrt_s の路 Meritocracy／PopDecision の純の比。
#   flip＝流れの向きが前の月と逆の州×月の割合（Σ flowFlipRegionMonths÷Σ regionMonths）。
import sys, os, csv, statistics as st
from collections import defaultdict
sys.stdout.reconfigure(encoding='utf-8')

ERAS = [(1790, 1814), (1815, 1849), (1850, 1879), (1880, 1913), (1914, 1950)]


def era(y):
    for a, b in ERAS:
        if a <= y <= b:
            return f"{a}-{b}"
    return None


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def rows(path):
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        for x in r:
            yield dict(zip(h, x))


def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')


def parse_seeds(a):
    lo, hi = a.split('-')
    return range(int(lo), int(hi) + 1)


def analyse(label, root, seeds):
    win = defaultdict(lambda: defaultdict(list))   # (nation, era) -> metric -> per-seed values
    hasG = None
    ret_ux = defaultdict(float)
    ret_seed = []
    rt = defaultdict(lambda: [0.0, 0.0, 0.0])
    flipN = flipD = 0.0
    flip_era = defaultdict(lambda: [0.0, 0.0])
    used = 0
    for s in seeds:
        p = os.path.join(root, f"urxpd_s{s}.tsv")
        if not os.path.exists(p):
            continue
        used += 1
        acc = defaultdict(lambda: defaultdict(float))
        for d in rows(p):
            if hasG is None:
                hasG = 'wGsym' in d
            e = era(int(d['year']))
            if not e:
                continue
            a = acc[(d['nation'], e)]
            for k in ('sumTotal', 'wIncU', 'wIncR', 'regionMonths'):
                a[k] += fl(d[k])
            if hasG:
                for k in ('wSubV', 'wHousU', 'wGsym', 'flowFlipRegionMonths'):
                    a[k] += fl(d[k])
                flip_era[e][0] += fl(d['flowFlipRegionMonths'])
                flip_era[e][1] += fl(d['regionMonths'])
                flipN += fl(d['flowFlipRegionMonths'])
                flipD += fl(d['regionMonths'])
        for key, a in acc.items():
            T = a['sumTotal']
            if T <= 0:
                continue
            m = win[key]
            iu, ir = a['wIncU'] / T, a['wIncR'] / T
            m['W2'].append(iu / ir if ir > 0 else float('nan'))
            if hasG:
                m['G'].append(a['wGsym'] / T)
                sv, hu = a['wSubV'] / T, a['wHousU'] / T
                x, y = iu - hu, ir + sv
                m['D'].append(2 * (x - y) / (x + y) if x + y > 0 else float('nan'))
                m['flip'].append(a['flowFlipRegionMonths'] / a['regionMonths'] if a['regionMonths'] > 0 else float('nan'))
                m['sv'].append(sv)
                m['hu'].append(hu)
        # 往復（urx_s・本国の群）
        pu = os.path.join(root, f"urx_s{s}.tsv")
        if os.path.exists(pu):
            pd_ = so = 0.0
            for d in rows(pu):
                if d['group'] != 'Core':
                    continue
                if d['section'] == '月次政治':
                    pd_ += fl(d['dUrbanK'])
                elif d['section'] == '社会移動':
                    so += fl(d['dUrbanK'])
            ret_ux['pd'] += pd_
            ret_ux['so'] += so
            if pd_ != 0:
                ret_seed.append(-so / pd_)
        pr = os.path.join(root, f"urxrt_s{s}.tsv")
        if os.path.exists(pr):
            for d in rows(pr):
                a = rt[d['route']]
                a[0] += fl(d['urbanInK'])
                a[1] += fl(d['urbanOutK'])
                a[2] += fl(d['calls'])
    out = {'label': label, 'seeds': used, 'hasG': bool(hasG)}
    keys = sorted(win)
    out['windows'] = len(keys)
    out['W2'] = med([med(win[k]['W2']) for k in keys])
    if hasG:
        out['W1'] = med([abs(med(win[k]['G'])) for k in keys])
        out['W1b'] = med([abs(med(win[k]['D'])) for k in keys])
        out['G_signed_median'] = med([med(win[k]['G']) for k in keys])
        out['sv'] = med([med(win[k]['sv']) for k in keys])
        out['hu'] = med([med(win[k]['hu']) for k in keys])
        out['flip'] = flipN / flipD if flipD else float('nan')
        out['flip_by_era'] = {e: (v[0] / v[1] if v[1] else float('nan')) for e, v in sorted(flip_era.items())}
        out['flip_window_median'] = med([med(win[k]['flip']) for k in keys])
        out['perwin'] = {k: (med(win[k]['G']), med(win[k]['D']), med(win[k]['W2']), med(win[k]['flip']), med(win[k]['sv']), med(win[k]['hu'])) for k in keys}
    else:
        out['perwin'] = {k: (None, None, med(win[k]['W2']), None, None, None) for k in keys}
    out['ret_ux'] = (-ret_ux['so'] / ret_ux['pd']) if ret_ux['pd'] else float('nan')
    out['ret_ux_pd'] = ret_ux['pd']
    out['ret_ux_so'] = ret_ux['so']
    out['ret_seed_min'] = min(ret_seed) if ret_seed else float('nan')
    out['ret_seed_max'] = max(ret_seed) if ret_seed else float('nan')
    mer, pdr = rt.get('Meritocracy'), rt.get('PopDecision')
    if mer and pdr and (pdr[0] - pdr[1]) != 0:
        out['ret_rt'] = -(mer[0] - mer[1]) / (pdr[0] - pdr[1])
        out['rt_pd'] = tuple(pdr)
        out['rt_mer'] = tuple(mer)
        out['pd_gross_over_net'] = (pdr[0] + pdr[1]) / abs(pdr[0] - pdr[1])
    return out


def main():
    seeds = range(1, 11)
    args = []
    a = sys.argv[1:]
    i = 0
    while i < len(a):
        if a[i] == '--seeds':
            seeds = parse_seeds(a[i + 1])
            i += 2
        else:
            args.append(a[i])
            i += 1
    res = [analyse(x.split('=', 1)[0], x.split('=', 1)[1], seeds) for x in args]
    print('# urb3 (706) 基線の値（手計算の基線＝W1 0.217・W2 1.000・戻す割合 0.438〔m694 の軌道・eq.py〕）')
    for o in res:
        print(f"## {o['label']}  seeds={o['seeds']} windows={o['windows']} hasG={o['hasG']}")
        print(f"W2（名目の都市÷農村の窓の中央値）\t{o['W2']:.4f}")
        if o['hasG']:
            print(f"W1（|wGsym÷sumTotal|の窓の中央値）\t{o['W1']:.4f}")
            print(f"W1b（手計算と同じ形・計器の実測の自給と家賃）\t{o['W1b']:.4f}")
            print(f"g の符号つきの窓の中央値（正＝都市の方が値打ちが高い）\t{o['G_signed_median']:.4f}")
            print(f"自給の値÷人（窓の中央値）\t{o['sv']:.4f}\t住居の費用÷人\t{o['hu']:.4f}")
            print(f"flowFlip（向きが前の月と逆の州×月の割合・全期間）\t{o['flip']:.4f}\t窓の中央値\t{o['flip_window_median']:.4f}")
            print("flowFlip 年代別\t" + '\t'.join(f"{e}:{v:.4f}" for e, v in o['flip_by_era'].items()))
        print(f"戻す割合（urx_s・社会移動÷月次政治・本国・合計）\t{o['ret_ux']:.4f}\t月次政治の純 {o['ret_ux_pd']:.0f}千人\t社会移動の純 {o['ret_ux_so']:.0f}千人\t種ごとの最小〜最大 {o['ret_seed_min']:.3f}〜{o['ret_seed_max']:.3f}")
        if 'ret_rt' in o:
            print(f"戻す割合（urxrt_s・路 Meritocracy の純÷路 PopDecision の純）\t{o['ret_rt']:.4f}\tPopDecision 入 {o['rt_pd'][0]:.0f}・出 {o['rt_pd'][1]:.0f}・Meritocracy 入 {o['rt_mer'][0]:.0f}・出 {o['rt_mer'][1]:.0f}（千人）\tPopDecision の総量÷純 {o['pd_gross_over_net']:.2f}")
        print()
    if len(res) >= 2:
        print('# 幅（最大−最小）')
        for k in ('W2', 'W1', 'W1b', 'flip', 'ret_ux', 'ret_rt'):
            v = [o[k] for o in res if k in o]
            if len(v) >= 2:
                print(f"{k}\t" + ' '.join(f"{o['label']}={o[k]:.4f}" for o in res if k in o) + f"\t幅 {max(v) - min(v):.4f}")
    print()
    print('# 窓ごと（種の中央値）：国\t年代\tG\tD(手計算の形)\tW2\tflip\t自給/人\t家賃/人\t[先頭のラベルのみ]')
    o = res[0]
    for k, v in o['perwin'].items():
        f = lambda x: 'NA' if x is None else f"{x:.4f}"
        print(f"{k[0]}\t{k[1]}\t" + '\t'.join(f(x) for x in v))


main()
