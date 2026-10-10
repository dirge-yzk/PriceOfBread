"""(204) 0f 内戦の少なすぎ＝蜂起の漏斗の読み手（読むだけ）。python an_rv204.py <out_dir（rv204 と w171 を含む）> [タグ]
入力＝inj_rv204 の v<seed>.txt の R 行（国×四半期）・inj_war171 の w<seed>.txt の WS 行（内戦の札・分離の札）。
漏斗（時代ごと・種の和÷種＝1試合あたりの国×四半期）：運動のある国×四半期 → 結束が戦いの線以上の運動がいる → F3b の判定に入った → 状態の門で止まった →
  最も強い運動が立った → 力の門（強さ＞保持力＋郷勇）を越えた → 危機（法の経路へ）→ 法の経路の結果（通知・本丸が動いた・別の軸・蜂起）→ 蜂起の立ち方（地図・即時）／
  首都の蜂起（Transition）／分離独立の州。WS 行の内戦（蜂起＝分離の札0／分離独立＝札1）を時代ごとに並べる。"""
import sys, os, glob, statistics as st, collections
D = sys.argv[1]; TAG = sys.argv[2] if len(sys.argv) > 2 else D
NATS = ['イギリス', 'フランス', 'アメリカ', 'ロシア', 'オスマン', 'プロイセン', 'オーストリア', '日本', 'スペイン', '清']
ERAS = [(1783, 1815), (1816, 1849), (1850, 1913), (1914, 1950)]


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def era(y):
    for i, (a, b) in enumerate(ERAS):
        if a <= y <= b: return i
    return 3


def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')


K = ['nFac', 'maxCoh', 'nCand', 'entered', 'blocked', 'best', 'bestStr', 'hold', 'militia', 'powerPass', 'crisis', 'law', 'raise', 'capRevolt', 'secGo', 'minDom', 'legit', 'coercion', 'inCivil', 'pop']
A = collections.defaultdict(lambda: collections.defaultdict(float))   # (grp, era) -> counts
RAT = collections.defaultdict(list)   # (grp, era) -> strength/(hold+militia)
seeds = []
for f in sorted(glob.glob(os.path.join(D, 'rv204', 'v*.txt'))):
    seeds.append(os.path.basename(f))
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'R': continue
        d = {k: fl(v) for k, v in zip(K, t[5:])}
        e = era(int(t[2])); n = t[4]
        for g in ([n, '_主要', '_全て'] if n in NATS else ['_他', '_全て']):
            a = A[(g, e)]
            a['rows'] += 1
            if d['nFac'] > 0: a['fac'] += 1
            if d['nCand'] > 0: a['cand'] += 1
            if d['entered'] > 0: a['entered'] += 1
            if d['blocked'] > 0: a['blocked'] += 1
            if d['best'] > 0:
                a['best'] += 1
                den = d['hold'] + d['militia']
                if den > 0: RAT[(g, e)].append(d['bestStr'] / den)
            if d['powerPass'] > 0: a['power'] += 1
            if d['crisis'] > 0: a['crisis'] += 1
            lw = int(d['law']) if d['law'] == d['law'] else 0
            if lw: a[f'law{lw}'] += 1
            rz = int(d['raise']) if d['raise'] == d['raise'] else 0
            if rz: a[f'raise{rz}'] += 1
            if d['capRevolt'] > 0: a['cap'] += 1
            a['sec'] += d['secGo']
            if d['inCivil'] > 0: a['inCivil'] += 1
            a['minDom'] += d['minDom']; a['legit'] += d['legit']
ns = max(1, len(seeds))
print(f'######## {TAG}（種 {len(seeds)}）＝蜂起の漏斗（1試合あたりの国×四半期＝種の和÷種）')
if not seeds:
    print('# rv204 の行が無い'); sys.exit(0)
print('時代 | 群 | 国×四半期 | 運動あり | 結束≥線 | 判定に入った | 状態の門（内戦中・予約中） | best | 力の門を越えた | 強さ÷(保持力＋郷勇) の中央〔p90〕 | 危機 | 法の経路 通知/本丸/別の軸/蜂起 | 蜂起 地図/即時 | 首都の蜂起 | 分離独立の州 | 内戦中 | 正統性の平均')
for g in ['_全て', '_主要', '_他'] + NATS:
    for e in range(4):
        a = A.get((g, e))
        if not a: continue
        r = sorted(RAT[(g, e)])
        p90 = r[int(0.9 * (len(r) - 1))] if r else float('nan')
        print(f'{ERAS[e][0]}-{ERAS[e][1]} | {g} | {a["rows"] / ns:.0f} | {a["fac"] / ns:.0f} | {a["cand"] / ns:.1f} | {a["entered"] / ns:.0f} | {a["blocked"] / ns:.1f} | {a["best"] / ns:.1f} | {a["power"] / ns:.2f} | {med(r):.3f}〔{p90:.3f}〕 | {a["crisis"] / ns:.2f} | '
              f'{a["law1"] / ns:.2f}/{a["law2"] / ns:.2f}/{a["law3"] / ns:.2f}/{a["law4"] / ns:.2f} | {a["raise1"] / ns:.2f}/{a["raise2"] / ns:.2f} | {a["cap"] / ns:.2f} | {a["sec"] / ns:.2f} | {a["inCivil"] / ns:.0f} | {a["legit"] / max(1, a["rows"]):.2f}')
# WS 行の内戦（段0）を蜂起と分離独立に分ける
cw = collections.defaultdict(lambda: collections.defaultdict(int)); nsw = 0
for f in sorted(glob.glob(os.path.join(D, 'w171', 'w*.txt'))):
    nsw += 1
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'WS' or int(t[4]) != 0 or int(t[5]) != 1: continue
        e = era(int(t[3]))
        cw[e]['分離独立' if int(t[6]) else '蜂起'] += 1
print('## 内戦の組（WS 行・段0・内戦の札）＝種の和÷種：' + ' | '.join(f'{ERAS[e][0]}-{ERAS[e][1]} 蜂起 {cw[e]["蜂起"] / max(1, nsw):.1f}・分離独立 {cw[e]["分離独立"] / max(1, nsw):.1f}' for e in range(4)))
