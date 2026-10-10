"""(193) 戦後の上げ直しの読み手（読むだけ）。python an_postwar193.py <cnl の出力フォルダ> [種の上限]
平時に段を上げた四半期ごとに、その前の戦争の始まる直前の平時の四半期（戦前）と比べる：
  割合（戦前→上げた時）・陸軍の不安（分子）・自国の養える軍（分母）・本国＋統合格の州の数・最大の寄与の国と敵意。
戦前に既に線の上だった（割合≧0.5）か、戦争で線を越えたか（分子の増え・分母の減り）を国ごとに数える。"""
import sys, os, glob, statistics as st
from collections import defaultdict
D = sys.argv[1]; NMAX = int(sys.argv[2]) if len(sys.argv) > 2 else 99
MAJ = ['イギリス', 'アメリカ', 'フランス', 'プロイセン', 'ロシア', '日本', '清', 'オーストリア', 'スペイン', 'オスマン']
rows = defaultdict(list); COL = None; seeds = []
for f in sorted(glob.glob(os.path.join(D, 'c*.txt')), key=lambda p: int(os.path.basename(p)[1:-4])):
    sd = int(os.path.basename(f)[1:-4])
    if sd > NMAX: continue
    seeds.append(sd)
    for ln in open(f, encoding='utf-8'):
        if ln.startswith('#'): COL = ln[1:].rstrip('\n').split('\t'); continue
        d = dict(zip(COL, ln.rstrip('\n').split('\t'))); rows[(sd, d['nation'])].append(d)
fl = lambda x: float(x) if x not in ('-', 'nan') else float('nan')
I = lambda x: int(x) if x.lstrip('-').isdigit() else -9
cls = defaultdict(lambda: defaultdict(int)); det = defaultdict(list)
for (sd, n), L in rows.items():
    key = n if n in MAJ else '_他'
    for i, d in enumerate(L):
        if I(d['atWar']) or I(d['law1']) <= I(d['law0']): continue
        if I(d['pre0']) >= 0 and I(d['pre1']) < 0: continue
        # 直前の戦争の始まる前の平時の四半期
        j = i - 1
        while j >= 0 and not I(L[j]['atWar']): j -= 1   # 戦争の最後の四半期へ
        if j < 0: cls[key]['戦争なし'] += 1; continue
        k = j
        while k >= 0 and I(L[k]['atWar']): k -= 1       # 戦争の始まる前の四半期
        if k < 0: cls[key]['開幕から交戦'] += 1; continue
        b = L[k]
        s0, s1 = fl(b['share']), fl(d['share'])
        t0, t1, o0, o1 = fl(b['thrArmy']), fl(d['thrArmy']), fl(b['own']), fl(d['own'])
        c0, c1 = I(b['core']) + I(b['integ']), I(d['core']) + I(d['integ'])
        if s0 >= 0.5: tag = '戦前から線の上'
        else:
            # 線を越えた理由＝分子だけを戦前に戻すと線の下か・分母だけを戻すと線の下か
            num_only = t1 / (t1 + o0) if t1 + o0 > 0 else 0   # 分母を戦前へ戻す
            den_only = t0 / (t0 + o1) if t0 + o1 > 0 else 0   # 分子を戦前へ戻す
            if num_only < 0.5 and den_only < 0.5: tag = '戦争で越えた（分子と分母の両方）'
            elif num_only < 0.5: tag = '戦争で越えた（分母の減り＝州と軍を失った）'
            elif den_only < 0.5: tag = '戦争で越えた（分子の増え）'
            else: tag = '戦争で越えた（どちらか片方でも）'
        cls[key][tag] += 1
        det[key].append(f'種{sd} {b["yr"]}→{d["yr"]} 割合 {s0:.2f}→{s1:.2f} 分子 {t0:.0f}→{t1:.0f} 分母 {o0:.0f}→{o1:.0f} 州 {c0}→{c1} 寄与 {b["top"]}→{d["top"]}（敵意 {fl(d["topHost"]):.2f}）{tag}')
print('## 平時に上げた一歩の分類（種をまとめた件数）')
for n in MAJ + ['_他']:
    if cls[n]: print(f'  {n}: ' + '・'.join(f'{k} {v}' for k, v in sorted(cls[n].items(), key=lambda x: -x[1])))
tot = defaultdict(int)
for n in cls:
    for k, v in cls[n].items(): tot[k] += v
print('  全ての国: ' + '・'.join(f'{k} {v}' for k, v in sorted(tot.items(), key=lambda x: -x[1])))
print('\n## 主要国の一件ずつ')
for n in MAJ:
    for x in det[n]: print(f'  {n}: {x}')
