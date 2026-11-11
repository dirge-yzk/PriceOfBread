# (1004) c92wr 戦争の段0：導いた余剰戦力の候補 A（陸の大きさ−陸の必要量−内の守り）を fm_s から読む（読むだけ）。
# 使い方：python -X utf8 -I wr1004_surplus.py <keep のルート> <出力のフォルダ> [盤面 ...]
# fm_s：9 国は毎四半期・他の国は年末だけ＝(国, 年) の最後の行（年末）で全ての国をそろえる。
# 列：landPotentialIndex（ArmyShape.LandPotential）・needLandIndex（NeedLand）・needInternalIndex（NeedInternal）・forcePotentialIndex・landSideBindsFlag・atWarMonthsCount
import sys, os, statistics as st
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
for b in BOARDS:
    P(f'#### 盤面 {b}')
    allR = []; warR = []; peaceR = []; invR = []; nonfin = 0; rows_n = 0; bind_mismatch = 0; bind_n = 0
    per_nat = defaultdict(list); inv_neg = 0; inv_n = 0; inv_missing = 0
    sub_by_neg = defaultdict(lambda: [0, 0])
    for s in SEEDS[b]:
        d = f'{ROOT}/{b}'
        with open(f'{d}/fm_s{s}.tsv', 'rb') as f:
            L = f.read().decode('utf-8').splitlines()
        h = L[0].split('\t'); ix = {k: i for i, k in enumerate(h)}
        last = {}
        for l in L[1:]:
            c = l.split('\t')
            last[(c[ix['nation']], int(c[ix['year']]))] = c   # 同じ (国, 年) は後の行で上書き＝年末
        R = {}
        for (n, y), c in last.items():
            lp, nl, ni, fp = (fl(c[ix[k]]) for k in ('landPotentialIndex', 'needLandIndex', 'needInternalIndex', 'forcePotentialIndex'))
            rows_n += 1
            need = nl + ni
            if not (need > 0) or lp != lp:
                nonfin += 1; continue
            r = lp / need
            R[(n, y)] = r
            allR.append(r); per_nat[n].append(r)
            wm = fl(c[ix['atWarMonthsCount']])
            (warR if wm > 0 else peaceR).append(r)
            bf = c[ix['landSideBindsFlag']]
            if bf in ('0', '1') and fp == fp:
                bind_n += 1
                if (bf == '1') != (need > fp): bind_mismatch += 1
        with open(f'{d}/invade_s{s}.tsv', 'rb') as f:
            for l in f.read().decode('utf-8').splitlines()[1:]:
                c = l.split('\t'); y = int(c[0]); key = (c[1], y)
                inv_n += 1
                if key not in R: inv_missing += 1; continue
                r = R[key]; invR.append(r)
                neg = r < 1
                if neg: inv_neg += 1
                sub_by_neg[neg][0] += 1; sub_by_neg[neg][1] += (c[4] == '1')
    P(f'(国,年) の行 {rows_n}・必要量が正でない・非有限で読めない {nonfin}')
    P(f'landSideBindsFlag と〈陸の必要量＋内の守り＞軍の大きさ〉の食い違い {bind_mismatch}/{bind_n}（0 なら列の読みが ArmyShape.NavalSpendShareOf と一致）')
    for lab, xs in (('全て', allR), ('交戦の月あり', warR), ('交戦なし', peaceR), ('侵略した国の年', invR)):
        P(f'R＝陸の大きさ÷(陸の必要量＋内の守り) {lab}: 件 {len(xs)}・中央 {f2(q(xs,.5))}・四分位 {f2(q(xs,.25))}〜{f2(q(xs,.75))}・R<1 の割合 {f2(sum(1 for v in xs if v<1)/max(1,len(xs)))}')
    P(f'侵略 {inv_n} 件のうち fm_s の行が無い {inv_missing}・R<1（候補 A なら余剰が負）{inv_neg}')
    for neg in (False, True):
        a, sb = sub_by_neg[neg]
        P(f'  侵略した国の R<1={neg}: 件 {a}・屈服の割合 {f2(sb/max(1,a))}')
    P('9 国の R（全ての種の年・中央・R<1 の割合）: ' + '・'.join(f'{n} {f2(q(per_nat[n],.5),2)}/{f2(sum(1 for v in per_nat[n] if v<1)/max(1,len(per_nat[n])),2)}' for n in NINE))
    P()
os.makedirs(OUT, exist_ok=True)
with open(os.path.join(OUT, 'wr1004_surplus.txt'), 'wb') as f:
    f.write(('\n'.join(out) + '\n').encode('utf-8'))
print('wrote', os.path.join(OUT, 'wr1004_surplus.txt'), len(out))
