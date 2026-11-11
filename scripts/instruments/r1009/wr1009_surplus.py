# (1009) c93wn：余剰戦力の候補の項を、時点をそろえた fm_s の行（全ての国で q%4==3）で引き直す（読むだけ）。
#   r1004/wr1004_surplus.py・wr1004_warpeace.py を 1 本にまとめて直した：旧は (国, 年) の最後の行＝9 国は q%4==2・他は q%4==3 を混ぜていた。
# 使い方：python -X utf8 -I wr1009_surplus.py <keep のルート> <出力のフォルダ> [盤面 ...]
# 時点（prereg 追記 0・1）：ラベル y・q%4==3 の行＝暦 y−1 年末の盤面（外交の段の後に書く）。侵略（invade_s の year＝ラベル y）はこの行で読む。
# 量：R_A＝landPotentialIndex÷(needLandIndex＋needInternalIndex)（旧の候補 A の比）
#     R_C′＝landPotentialIndex×(1−b)÷(同)・b＝min(1, warScaleRatioAtDecision)（NaN・0 以下は 0）＝前線に縛られた兵の近似（推・WarScale は
#     〈相手の陣営の届く陸の力÷自陣営の陸の力〉で自国の縛られた兵そのものではない）。
import sys, os
from collections import defaultdict

ROOT, OUT = sys.argv[1], sys.argv[2]
BOARDS = sys.argv[3:] or ['base_ins', 'n1', 'n2', 'a11']
SEEDS = {'base_ins': range(1, 11), 'n1': range(1, 11), 'n2': range(1, 11), 'a11': range(11, 21)}
NINE = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'オスマン', 'オーストリア']

def fl(x):
    try: return float(x)
    except ValueError: return float('nan')

def q(xs, p):
    xs = sorted(xs)
    if not xs: return float('nan')
    k = (len(xs) - 1) * p; i = int(k); f = k - i
    return xs[i] if i + 1 >= len(xs) else xs[i] * (1 - f) + xs[i + 1] * f

def f2(x, d=3): return 'nan' if x != x else f'{x:.{d}f}'

out = []
def P(s=''): out.append(s)
dirs = {}   # (盤面, 国, 量, 時点) → (平時の中央, 交戦の中央)
for b in BOARDS:
    P(f'#### 盤面 {b}')
    pools = defaultdict(list)
    per = defaultdict(lambda: defaultdict(list))
    inv_n = inv_missing = 0; sub = defaultdict(lambda: [0, 0])
    bound_share = []
    for s in SEEDS[b]:
        d = f'{ROOT}/{b}'
        L = open(f'{d}/fm_s{s}.tsv', 'rb').read().decode('utf-8').splitlines()
        h = L[0].split('\t'); ix = {k: i for i, k in enumerate(h)}
        Rq3 = {}
        for l in L[1:]:
            c = l.split('\t')
            qq = int(c[ix['quarter']]); n = c[ix['nation']]; y = int(c[ix['year']])
            lp, nl, ni = fl(c[ix['landPotentialIndex']]), fl(c[ix['needLandIndex']]), fl(c[ix['needInternalIndex']])
            sc = fl(c[ix['warScaleRatioAtDecision']]); wm = fl(c[ix['atWarMonthsCount']])
            need = nl + ni
            if not (need > 0) or lp != lp: continue
            bnd = min(1.0, sc) if sc > 0 else 0.0
            ra, rc = lp / need, lp * (1 - bnd) / need
            war = 'war' if wm > 0 else 'peace'
            if n in NINE:   # 旧の比べ（全ての四半期）
                per[n][('A', war, 'all')].append(ra); per[n][('C', war, 'all')].append(rc)
            if qq % 4 != 3: continue
            Rq3[(n, y)] = (ra, rc)
            for k, r in (('A', ra), ('C', rc)):
                pools[(k, war)].append(r); pools[(k, 'all')].append(r)
            if war == 'war': bound_share.append(bnd)
            if n in NINE:
                per[n][('A', war, 'q3')].append(ra); per[n][('C', war, 'q3')].append(rc)
        for l in open(f'{d}/invade_s{s}.tsv', 'rb').read().decode('utf-8').splitlines()[1:]:
            c = l.split('\t'); y = int(c[0]); key = (c[1], y)
            inv_n += 1
            if key not in Rq3: inv_missing += 1; continue
            ra, rc = Rq3[key]
            for k, r in (('A', ra), ('C', rc)):
                pools[(k, 'inv')].append(r)
                sub[(k, r < 1)][0] += 1; sub[(k, r < 1)][1] += (c[4] == '1')
    P(f'q%4==3 の交戦の行（atWarMonthsCount>0）の b＝min(1,warScale): 件 {len(bound_share)}・b>0 の割合 {f2(sum(1 for v in bound_share if v>0)/max(1,len(bound_share)))}・中央 {f2(q(bound_share,.5))}・四分位 {f2(q(bound_share,.25))}〜{f2(q(bound_share,.75))}・b=1 の割合 {f2(sum(1 for v in bound_share if v>=1)/max(1,len(bound_share)))}')
    for k, lab in (('A', 'R_A'), ('C', 'R_C′')):
        for g, gl in (('all', '全て'), ('war', '交戦'), ('peace', '平時'), ('inv', '侵略した国の年')):
            xs = pools[(k, g)]
            P(f'{lab} {gl}: 件 {len(xs)}・中央 {f2(q(xs,.5))}・四分位 {f2(q(xs,.25))}〜{f2(q(xs,.75))}・R<1 の割合 {f2(sum(1 for v in xs if v<1)/max(1,len(xs)))}')
        for neg in (False, True):
            a, sb = sub[(k, neg)]
            P(f'  {lab} 侵略した国の R<1={neg}: 件 {a}・屈服の割合 {f2(sb/max(1,a))}')
    P(f'侵略 {inv_n} 件のうちラベル y・q%4==3 の fm_s の行が無い {inv_missing}')
    for k, lab in (('A', 'R_A'), ('C', 'R_C′')):
        cells = []
        for n in NINE:
            row = []
            for t in ('q3', 'all'):
                pm, wmed = q(per[n][(k, 'peace', t)], .5), q(per[n][(k, 'war', t)], .5)
                row.append(f'{f2(pm,2)}→{f2(wmed,2)}({len(per[n][(k, "war", t)])})')
                dirs[(b, n, k, t)] = (pm, wmed)
            cells.append(f'{n} q3 {row[0]} 全 {row[1]}')
        P(f'{lab} 9 国の平時→交戦の中央（括弧は交戦の行の数・q3＝q%4==3 の行だけ・全＝全ての四半期＝旧）: ' + '・'.join(cells))
    P()
P('#### 向き（交戦の中央＞平時の中央）の 4 盤面のそろい（prereg：そろわない国は向きなし）')
for k, lab in (('A', 'R_A'), ('C', 'R_C′')):
    for t in ('q3', 'all'):
        up, down, mix = [], [], []
        for n in NINE:
            sg = []
            for b in BOARDS:
                pm, wm = dirs[(b, n, k, t)]
                sg.append(None if (pm != pm or wm != wm) else wm > pm)
            if all(v is True for v in sg): up.append(n)
            elif all(v is False for v in sg): down.append(n)
            else: mix.append(n + '(' + ''.join('↑' if v else ('↓' if v is False else '?') for v in sg) + ')')
        P(f'{lab} {t}: 交戦で高い {len(up)} 国 {up}・交戦で低い {len(down)} 国 {down}・向きなし {mix}')
os.makedirs(OUT, exist_ok=True)
with open(os.path.join(OUT, 'wr1009_surplus.txt'), 'wb') as f:
    f.write(('\n'.join(out) + '\n').encode('utf-8'))
print('wrote', os.path.join(OUT, 'wr1009_surplus.txt'), len(out))
