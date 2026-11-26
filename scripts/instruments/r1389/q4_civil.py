# (1389) 問い 4 の低 2：内戦（cwend・keep_1383）を案③の月の歩みで。反乱の国の crownpc の行が無い内戦の K′ を 1.0・0.305・0.1 の 3 通りで入れ、勝ち手の幅を出す（r1382 q2_trans.py の写し・置き場と代入だけ替えた）
import sys, random, collections; sys.path.insert(0, 'D:/PoB_work/r1389_work/scripts')
from load import *; from walk import *
for KMISS in (1.0, 0.305, 0.1):
    rng = random.Random(1382); T = collections.Counter(); tot = collections.Counter(); n = miss = 0; firstm = []; base_c = collections.Counter(); miss_base = collections.Counter(); col_lt = collections.Counter()
    for s in SEEDS:
        _, byu = crown_k(s)
        for v in rows('cwend', s):
            n += 1; qe = int(v['quarter']); mo = int(v['months']); qs = qe - mo // 3; sc = fl(v['scoreAtPeace']); coll = v['collapseFlag'] == '1' if 'collapseFlag' in v else v['endPath'] == 'collapse'
            kp = [byu.get((q, v['parentUid']), float('nan')) for q in range(qs, qe + 1)]
            kr = [byu.get((q, v['rebelUid']), float('nan')) for q in range(qs, qe + 1)]
            ismiss = all(x != x for x in kr)
            if ismiss: kr = [KMISS]; miss += 1
            base = {'1': '親の勝ち', '2': '反乱の勝ち', '0': '白紙'}.get(v['verdictCode'], v['verdictCode']); base_c[base] += 1
            if ismiss: miss_base[base] += 1
            if coll and abs(sc) < 1.5 - 1e-9 and base == '反乱の勝ち': col_lt['崩壊・reach<1 の反乱の勝ち'] += 1
            c = collections.Counter(); f1 = 0
            for r in range(200):
                reach = abs(sc) / 1.5
                if coll:
                    w = reach / 2 if abs(sc) < 1.5 - 1e-9 else 0.5 + 0.5 * rng.random(); pw = 2 * w - 1; pA = pw if sc > 0 else -pw; fr = reach == 0
                else: pA = max(-1, min(1, sc / max(1, mo) / PUSH)); fr = sc == 0
                m, S, who = walk(pA, traj(kp), traj(kr), fr, rng)
                c['続く' if m is None else ('親の勝ち' if who == 'B負' else '反乱の勝ち')] += 1; f1 += m == 1
            maj = c.most_common(1)[0][0]; T[(base, maj, '代入' if ismiss else '実')] += 1; tot[maj] += 1; firstm.append(f1 / 200)
    print(f'K′(行なし)={KMISS}: 内戦 {n} 行なし {miss} 基線 {dict(base_c)} 行なしの基線 {dict(miss_base)} {dict(col_lt)}')
    print('  多数の行き先', dict(tot), ' 初月に終わる平均', f'{sum(firstm)/len(firstm):.1%}')
    for k, x in sorted(T.items()): print('   ', k, x)
