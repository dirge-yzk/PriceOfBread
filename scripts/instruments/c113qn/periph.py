# (1153) c113qn 周辺の点検（記録）：キニーネの市場（goods_s の 9 国）・植民地の維持費の請求（fiscal_s）・キニーネの標的への侵略（invade_s）・疫病の死（epi_s があれば）。読むだけ。
# 使い方: python -X utf8 -I periph.py <リポジトリ> <ラベル=生のフォルダ> ...
import sys, os, json
sys.stdout.reconfigure(newline='\n')
REPO = sys.argv[1]
W = json.load(open(os.path.join(REPO, 'PriceOfBread.SimCore', 'world.json'), encoding='utf-8'))
QT = {t['name'] for t in W['abstractTargets'] if t.get('quinine')}
PER = ((0, 1830), (1830, 1870), (1870, 1910), (1910, 2000))


def fl(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def med(v):
    v = sorted(x for x in v if x == x)
    return v[len(v) // 2] if v else float('nan')


def arm(lab, d):
    seeds = [s for s in range(1, 21) if os.path.exists(os.path.join(d, f'nation_s{s}.tsv'))]
    px = {p: [] for p in PER}; st = {p: [] for p in PER}; dm = {p: [] for p in PER}
    claim = 0.0; claimTot = 0.0; inv = invQ = 0
    for s in seeds:
        p = os.path.join(d, f'goods_s{s}.tsv')
        if os.path.exists(p):
            f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t')
            iy, ig, ip, istk, idm = (h.index(k) for k in ('year', 'goodName', 'priceMean', 'stockQty', 'lastDemandQty'))
            for l in f:
                x = l.rstrip('\n').split('\t')
                if x[ig] != 'キニーネ':
                    continue
                y = int(fl(x[iy]))
                for pp in PER:
                    if pp[0] <= y < pp[1]:
                        px[pp].append(fl(x[ip])); st[pp].append(fl(x[istk])); dm[pp].append(fl(x[idm]))
            f.close()
        p = os.path.join(d, f'fiscal_s{s}.tsv')
        if os.path.exists(p):
            f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ic, it = h.index('claimColonialUpkeepPerMonth'), h.index('claimTotalPerMonth')
            for l in f:
                x = l.rstrip('\n').split('\t'); a = fl(x[ic]); t = fl(x[it])
                if a == a: claim += a
                if t == t: claimTot += t
            f.close()
        p = os.path.join(d, f'invade_s{s}.tsv')
        if os.path.exists(p):
            f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); it = h.index('target')
            for l in f:
                x = l.rstrip('\n').split('\t'); inv += 1; invQ += len(x) > it and x[it] in QT
            f.close()
    seg = ' '.join(f'{a}-{b - 1}:px={med(px[(a, b)]):.4g}/stk={med(st[(a, b)]):.4g}/dem={med(dm[(a, b)]):.4g}(n={len(px[(a, b)])})' for a, b in PER)
    print(f'{lab}\tseeds={len(seeds)}\tquinineMarket9nations_median\t{seg}\tcolonialUpkeepClaimSum={claim:.6g}\tshareOfClaimTotal={claim / claimTot if claimTot else float("nan"):.4g}\tinvade={inv}\tinvadeQuinineTargets={invQ}')


for a in sys.argv[2:]:
    lab, d = a.split('=', 1)
    arm(lab, d)
