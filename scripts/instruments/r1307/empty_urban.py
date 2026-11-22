# (1307) 読むだけ：region_s から、都市の人口（populationK − ruralPopulationK）が 0 の州×四半期を数える。
#   腕ごと・種ごと・格（Colonial と本国＝Core/Integrated）ごと。鏡として農村の人口 0（都市＞0）も数える。
#   再入場：都市 0 の連なりが終わって都市＞0 に戻った回数と、連なりの長さ（四半期）の分布。
#   呼び方：python empty_urban.py <出力の tsv> <腕の名>=<生のフォルダ> ...
import sys, os, collections

EPS_ABS = 1e-9   # 千人の単位で 1e-9＝0 と読む（エンジンの Eps と同じ桁・人数で 1e-6 人）

def main():
    out = sys.argv[1]
    arms = [a.split('=', 1) for a in sys.argv[2:]]
    rows = []
    runs_rows = []
    for name, d in arms:
        for s in range(1, 21):   # 種 1〜20（無い種は飛ばす＝M は 1〜10・S11 は 11〜20）
            p = os.path.join(d, f'region_s{s}.tsv')
            if not os.path.exists(p):
                continue
            cnt = collections.Counter()
            popk = collections.Counter()
            qb = collections.Counter()
            last = {}      # (nation, region) -> 0/1 都市 0 か
            runlen = {}
            runs = collections.Counter()
            reentry = collections.Counter()
            with open(p, 'r', encoding='utf-8', newline='') as f:
                hdr = f.readline().rstrip('\r\n').split('\t')
                ix = {h: i for i, h in enumerate(hdr)}
                iq, ina, ire, iti, ipo, iru = ix['quarter'], ix['nation'], ix['region'], ix['tier'], ix['populationK'], ix['ruralPopulationK']
                for line in f:
                    c = line.rstrip('\r\n').split('\t')
                    try:
                        q = int(c[iq]); pop = float(c[ipo]); rur = float(c[iru])
                    except ValueError:
                        continue
                    if not (pop > EPS_ABS):
                        continue
                    tier = 'colony' if c[iti] == 'Colonial' else 'home'
                    urb = pop - rur
                    key = (c[ina], c[ire])
                    z = 1 if not (urb > EPS_ABS) else 0
                    if z:
                        cnt[(tier, 'urban0')] += 1; popk[(tier, 'urban0')] += pop
                        if q in (0, 1, 4, 40, 280, 400, 600):
                            qb[(tier, q)] += 1
                    elif urb < 0.001 * pop:
                        cnt[(tier, 'urban_lt0.1pct')] += 1
                    if not (rur > EPS_ABS) and urb > EPS_ABS:
                        cnt[(tier, 'rural0')] += 1; popk[(tier, 'rural0')] += pop
                    cnt[(tier, 'all')] += 1
                    prev = last.get(key)
                    if z:
                        runlen[key] = runlen.get(key, 0) + 1
                    elif prev == 1:
                        reentry[tier] += 1
                        L = runlen.pop(key, 0)
                        runs[(tier, 'closed', bucket(L))] += 1
                    last[key] = z
            for key, L in runlen.items():
                runs[('?', 'open', bucket(L))] += 1
            for (tier, k), v in sorted(cnt.items()):
                rows.append((name, s, tier, k, v, round(popk.get((tier, k), 0.0), 1)))
            for (tier, q), v in sorted(qb.items()):
                rows.append((name, s, tier, f'urban0_q{q}', v, ''))
            for tier, v in sorted(reentry.items()):
                rows.append((name, s, tier, 'reentry', v, ''))
            for (tier, kind, b), v in sorted(runs.items()):
                runs_rows.append((name, s, tier, kind, b, v))
    with open(out, 'wb') as f:
        f.write(b'arm\tseed\ttier\tkey\tregionQuarters\tsumPopK\n')
        for r in rows:
            f.write(('\t'.join(str(x) for x in r) + '\n').encode('utf-8'))
    with open(out.replace('.tsv', '_runs.tsv'), 'wb') as f:
        f.write(b'arm\tseed\ttier\tkind\trunLenBucket\tcount\n')
        for r in runs_rows:
            f.write(('\t'.join(str(x) for x in r) + '\n').encode('utf-8'))

def bucket(L):
    for b in (1, 2, 4, 8, 40, 200):
        if L <= b:
            return f'<= {b}'
    return '>200'

if __name__ == '__main__':
    main()
