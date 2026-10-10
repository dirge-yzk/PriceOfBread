"""(170) 線M の読み手（読むだけ）＝inj_mil7 の A 行（列 0〜77）。徴兵法の向きを 10 カ国で読む。
  ① 平時の志願の割合（10年の窓・種をまとめた国×四半期）＋種ごとの割合の幅
  ② 本国の陸の脅威の割合（列 58）の中央（平時・窓ごと）
  ③ 法の推移（種ごと＝変わった年と向き・最大12件）
  ④ 戦時の非常の法＝戦争が終わった四半期に下がった件数（直前の四半期が交戦・今が平時・法が下がった）と、交戦中に上がった件数
  ⑤ 総動員（交戦中の対外の四半期の割合）
python an_mil170.py <腕名=hist の dir> [...] [--seeds 5]
列：1 四半期|2 年|4 名|5 生存|9 戦争月|10 内戦月|11 徴兵法|14 総動員|58 陸の脅威の割合|59 参政権|67 徴兵法の目標(前)|77 戦前の記録"""
import sys, os, glob, statistics as st, collections
args = sys.argv[1:]
maxsd = 99
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
ARMS = [a.split('=', 1) for a in args]
WATCH = ['イギリス', 'アメリカ', 'フランス', 'プロイセン', 'ロシア', '日本', '清', 'オーストリア', 'スペイン', 'オスマン']
DEC = [(y, y + 9) for y in range(1784, 1944, 10)] + [(1944, 1950)]


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def load(D):
    rows = []
    for f in sorted(glob.glob(os.path.join(D, 'a*.txt'))):
        sd = int(os.path.basename(f)[1:-4])
        if sd > maxsd: continue
        for ln in open(f, encoding='utf-8'):
            t = ln.rstrip('\n').split('|')
            if t[0] != 'A' or len(t) < 77: continue
            rows.append(dict(sd=sd, q=int(t[1]), y=int(t[2]), n=t[4], alive=int(t[5]), war=fl(t[9]), cw=fl(t[10]), law=int(t[11]), mob=int(t[14]),
                             land=fl(t[58]), suf=int(t[59]), cs=int(float(t[67])), pre=int(t[77]) if len(t) > 77 else -9))
    return rows


def med(x):
    x = [v for v in x if v == v]
    return st.median(x) if x else float('nan')


def peace(r): return r['alive'] == 1 and r['war'] == 0 and r['cw'] == 0


DATA = [(name, load(d)) for name, d in ARMS]
for name, R in DATA:
    print(f'######## {name}（種 {len({r["sd"] for r in R})}・行 {len(R)}）')

print('\n## ① 平時の志願の割合%（10年の窓・種をまとめた平時の国×四半期）｜窓ごとの種の割合の最小〜最大')
for name, R in DATA:
    print(f'#### {name}')
    print('  ' + ' '.join(f'{a:>4}' for a, _ in DEC))
    for n in WATCH:
        out = []; rng = []
        for y0, y1 in DEC:
            v = [r for r in R if r['n'] == n and peace(r) and y0 <= r['y'] <= y1]
            out.append(f'{100 * sum(1 for r in v if r["law"] == 0) / len(v):4.0f}' if v else '   -')
            per = []
            for sd in sorted({r['sd'] for r in v}):
                vv = [r for r in v if r['sd'] == sd]
                per.append(100 * sum(1 for r in vv if r['law'] == 0) / len(vv))
            rng.append(f'{min(per):.0f}-{max(per):.0f}' if per else '-')
        print(f'  {n}: ' + ' '.join(out))
        print(f'  {"":{len(n)}}  種の幅 ' + ' '.join(rng))

print('\n## ② 本国の陸の脅威の割合（列 58・平時の中央）')
for name, R in DATA:
    print(f'#### {name}')
    print('  ' + ' '.join(f'{a:>5}' for a, _ in DEC))
    for n in WATCH:
        out = []
        for y0, y1 in DEC:
            v = [r['land'] for r in R if r['n'] == n and peace(r) and y0 <= r['y'] <= y1]
            out.append(f'{med(v):5.2f}' if v else '    -')
        print(f'  {n}: ' + ' '.join(out))

print('\n## ③ 法の推移（種ごと＝開幕の法と変わった年・→法・（戦/平））')
for name, R in DATA:
    print(f'#### {name}')
    for n in WATCH:
        for sd in sorted({r['sd'] for r in R}):
            v = sorted([r for r in R if r['sd'] == sd and r['n'] == n and r['alive'] == 1], key=lambda r: r['q'])
            if not v: continue
            ch = []; prev = v[0]['law']
            for r in v[1:]:
                if r['law'] != prev:
                    ch.append(f'{r["y"]}→{r["law"]}{"戦" if (r["war"] > 0 or r["cw"] > 0) else "平"}')
                    prev = r['law']
            more = f'…（計{len(ch)}）' if len(ch) > 12 else ''
            print(f'  {n} 種{sd} 開幕{v[0]["law"]}: ' + ' '.join(ch[:12]) + more)

print('\n## ④ 戦時の非常の法＝交戦中に上がった件数｜戦争が終わった四半期に下がった件数（直前が交戦・今が平時）｜平時に上がった件数（門を通った再制定）｜平時に下がった件数（それ以外）')
for name, R in DATA:
    print(f'#### {name}')
    by = collections.defaultdict(list)
    for r in R: by[(r['sd'], r['n'])].append(r)
    tot = collections.Counter(); per = collections.defaultdict(collections.Counter)
    for (sd, n), v in by.items():
        v.sort(key=lambda r: r['q'])
        for a, b in zip(v, v[1:]):
            if b['alive'] != 1 or a['alive'] != 1: continue
            if b['law'] == a['law']: continue
            bw = b['war'] > 0 or b['cw'] > 0; aw = a['war'] > 0 or a['cw'] > 0
            k = ('戦時に上' if b['law'] > a['law'] and bw else
                 '戦後に下' if b['law'] < a['law'] and aw and not bw else
                 '平時に上' if b['law'] > a['law'] else '平時に下' if not bw else '戦時に下')
            tot[k] += 1
            if n in WATCH: per[n][k] += 1
    keys = ['戦時に上', '戦後に下', '平時に上', '平時に下', '戦時に下']
    print('  全ての国: ' + '・'.join(f'{k} {tot[k]}' for k in keys))
    for n in WATCH:
        if per[n]: print(f'  {n}: ' + '・'.join(f'{k} {per[n][k]}' for k in keys if per[n][k]))

print('\n## ⑤ 総動員（交戦中の対外の四半期のうち総動員の割合%）')
for name, R in DATA:
    out = []
    for n in WATCH + ['全ての国']:
        v = [r for r in R if r['alive'] == 1 and r['war'] > 0 and r['cw'] == 0 and (n == '全ての国' or r['n'] == n)]
        if v: out.append(f'{n} {100 * sum(1 for r in v if r["mob"] == 1) / len(v):.0f}（{len(v)}）')
    print(f'  {name}: ' + '・'.join(out))
