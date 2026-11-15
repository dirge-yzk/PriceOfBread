# (1158) c114qn 紙の台本（読むだけ・近似）：熱帯の標的（world.json の abstractTargets の quinine）への遠征の件数と、
# 数える（在庫は goods_s が計器の 9 国〔小国〕だけ・nationgood_s は年の間引き＝侵略の四半期の首都の在庫は生に無い＝腕 B の colqn_s の遠征の列で読む）。
# 使い方: python -X utf8 -I exppaper.py <リポジトリ> <ラベル=生のフォルダ> ...
import sys, os, json
sys.stdout.reconfigure(newline='\n')
REPO = sys.argv[1]
W = json.load(open(os.path.join(REPO, 'PriceOfBread.SimCore', 'world.json'), encoding='utf-8'))
QT = {t['name'] for t in W['abstractTargets'] if t.get('quinine')}
QN = {c['name'] for t in W['abstractTargets'] if t.get('quinine') for c in t['colonies']}


def fl(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def rows(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for l in f:
            x = l.rstrip('\n').split('\t')
            if len(x) >= len(h):
                yield dict(zip(h, x))


for a in sys.argv[2:]:
    lab, d = a.split('=', 1)
    seeds = [s for s in range(1, 21) if os.path.exists(os.path.join(d, f'invade_s{s}.tsv'))]
    nInv = nQ = 0
    per = []
    for s in seeds:
        inv = [r for r in rows(os.path.join(d, f'invade_s{s}.tsv')) if r['target'] in QT]
        nQ += len(inv); nInv += sum(1 for _ in rows(os.path.join(d, f'invade_s{s}.tsv')))
        per.append(f'{s}:{len(inv)}')
    print(f'{lab}\tseeds={len(seeds)}\tinvades={nInv}\tquinineTargetInvades={nQ}\tper_seed={" ".join(per)}')
