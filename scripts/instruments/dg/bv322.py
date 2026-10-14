"""(322) 合流：反証役 dg〔bv〕#6・#7 の処置＝同じ生の計器から読み手だけで出す（読むだけ・シミュは回さない）。
  python -X utf8 bv322.py <出力.md> A=<ins> B=<ins> [N=<ins>]
出す物：
  #6 蒸気機関を持つ国の数（年の末・種の中央〔最小〜最大〕）＝disc_s の E 行（蒸気機関）の国の累計＋開幕で知っている英（1 と数える）。
     ★限界：解禁した後に消えた国（併合・吸収）も数に残る＝〈その年までに解禁したことのある国の数〉。nation_s で生きている国に絞った数も並べる（その年の最後の四半期に nation_s の行がある国）。
  #7 プロイセンと蘭が 1950 まで蒸気機関を解禁しない種：その種でその国の nation_s の最後の四半期（国が残っていたか）と、蒸気機関の T 行（未解禁の軌道）の最後の四半期。"""
import sys, os, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows as irows, seeds as iseeds

OUT = sys.argv[1]
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[2:])
YEARS = (1784, 1790, 1800, 1810, 1820, 1840, 1860, 1880, 1900, 1950)
WATCH = ('プロイセン', 'ネーデルラント')


def rng(xs):
    xs = [x for x in xs if x == x]
    return '—' if not xs else f'{statistics.median(xs):g}（{min(xs):g}〜{max(xs):g}）'


res = {}
for arm, D in ARMS.items():
    sds = sorted(iseeds(D, 'disc'))
    first = collections.defaultdict(dict)      # sd → nation → 最初の年
    lastT = collections.defaultdict(dict)      # sd → nation → 蒸気機関の T 行の最後の四半期
    for sd, r in irows(D, 'disc'):
        if r['row'] != '蒸気機関': continue
        if r['kind'] == 'E': first[sd].setdefault(r['nation'], int(r['year']))
        else: lastT[sd][r['nation']] = int(r['quarter'])
    alive = collections.defaultdict(lambda: collections.defaultdict(set))   # sd → 年 → その年の最後の四半期に行がある国
    lastq = collections.defaultdict(dict)
    yq = {}
    for sd, r in irows(D, 'nation'):
        q, y = int(r['quarter']), int(r['year'])
        if q % 4 == 3 or y == 1950: alive[sd][y].add(r['nation'])
        lastq[sd][r['nation']] = (q, y)
    cnt = {y: [] for y in YEARS}; cnta = {y: [] for y in YEARS}
    for sd in sds:
        for y in YEARS:
            have = {n for n, fy in first[sd].items() if fy <= y} | {'イギリス'}
            cnt[y].append(len(have))
            al = alive[sd].get(y) or alive[sd].get(max(k for k in alive[sd] if k <= y), set())
            cnta[y].append(len(have & al))
    miss = []
    for sd in sds:
        for n in WATCH:
            if n not in first[sd]: miss.append((sd, n, lastq[sd].get(n), lastT[sd].get(n)))
    res[arm] = dict(cnt=cnt, cnta=cnta, miss=miss, n=len(sds))

L = [f'# (322) 反証役 dg〔bv〕#6・#7 の処置（読むだけ）・{" / ".join(f"{k}={v}" for k, v in ARMS.items())}', '']
L.append('## #6 蒸気機関を持つ国の数（年の末・種の中央〔最小〜最大〕・英を含む）'); L.append('')
L.append('| 年 | ' + ' | '.join(f'{k} 解禁したことのある国 | {k} うち生きている国' for k in ARMS) + ' |')
L.append('|---|' + '---|---|' * len(ARMS))
for y in YEARS:
    L.append(f'| {y} | ' + ' | '.join(f'{rng(res[k]["cnt"][y])} | {rng(res[k]["cnta"][y])}' for k in ARMS) + ' |')
L.append(''); L.append('## #7 プロイセン・蘭が 1950 まで蒸気機関を解禁しない種'); L.append('')
for k in ARMS:
    L.append(f'### {k}（{res[k]["n"]} 種）')
    if not res[k]['miss']: L.append('- 無し')
    for sd, n, lq, lt in res[k]['miss']:
        L.append(f'- s{sd} {n}：nation_s の最後の行＝{"なし" if lq is None else f"四半期 {lq[0]}（{lq[1]} 年）"}・蒸気機関の T 行の最後＝{"なし" if lt is None else f"四半期 {lt}"}')
    L.append('')
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
print('\n'.join(L))
