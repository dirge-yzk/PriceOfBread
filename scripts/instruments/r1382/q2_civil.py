# (1382) 問い 2：内戦 84 件（cwend_s）を案③の月の歩みで（親＝A・反乱＝B）
import sys, random; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *; from walk import *
import os
VAR = os.environ.get('VAR', '')
rng = random.Random(1382)
res = []; nrows = 0; rebel_missing = 0; vote = []
for s in SEEDS:
    _, byu = crown_k(s)
    for v in rows('cwend', s):
        nrows += 1; qe = int(v['quarter']); mo = int(v['months']); qs = qe - mo // 3
        sc = fl(v['scoreAtPeace']); coll = v['collapseFlag'] == '1'; sec = v['secessionFlag'] == '1'
        kp = [byu.get((q, v['parentUid']), float('nan')) for q in range(qs, qe + 1)]
        kr = [byu.get((q, v['rebelUid']), float('nan')) for q in range(qs, qe + 1)]
        if all(x != x for x in kr): rebel_missing += 1
        kr_f = kr if any(x == x for x in kr) else [0.305]   # 行の無い反乱の国は (1375) の反乱の中央 0.305〈推〉
        reach = abs(sc) / 1.5
        for r in range(NREP):
            if coll:
                if abs(sc) < 1.5 - 1e-9: w = reach / 2
                else: w = 0.5 if 'p0' in VAR else 0.5 + 0.5 * rng.random()
                pw = 2 * w - 1; pA = pw if sc > 0 else -pw   # 戦勝点が正＝親（A）の勝ち
                frozen = reach == 0
            else:
                pA = max(-1, min(1, sc / max(1, mo) / PUSH)); frozen = sc == 0
            if 'p0' in VAR: pA = 0.0; frozen = False
            if 'fix' in VAR: kp_u = [next((x for x in kp if x == x), 1.0)]; kr_u = [next((x for x in kr_f if x == x), 1.0)]
            else: kp_u, kr_u = kp, kr_f
            m, S, who = walk(pA, traj(kp_u), traj(kr_u), frozen, rng)
            res.append((m, S, who, sec, coll, min(x for x in kp_u + kr_u if x == x)))
N = len(res)
def share(f): return sum(1 for x in res if f(x)) / N
print('内戦', nrows, '反乱の国の行なし', rebel_missing, '歩み', N)
print('初月に終わる', f"{share(lambda x: x[0] == 1):.1%}", ' 3 か月以内', f"{share(lambda x: x[0] is not None and x[0] <= 3):.1%}")
print('50 年で終わらない', f"{share(lambda x: x[0] is None):.1%}")
print('終わった時の |S|<0.25', f"{share(lambda x: x[0] is not None and abs(x[1]) < 0.25):.1%}", ' |S|<0.1', f"{share(lambda x: x[0] is not None and abs(x[1]) < 0.1):.1%}")
print('月数', pct([x[0] for x in res if x[0]], (.1, .25, .5, .75, .9)))
print('親の負け', f"{share(lambda x: x[2] == 'A負'):.1%}", '反乱の負け', f"{share(lambda x: x[2] == 'B負'):.1%}", '両方（S=0 かつ両方の K\'=0）', f"{share(lambda x: x[2] == '両方'):.1%}")
print('白紙（分離独立で |S|<0.25・政体の内戦は S≠0 なら勝ち負け）', f"{share(lambda x: x[0] and x[3] and abs(x[1]) < 0.25):.2%}")
lo = [x for x in res if x[5] < 0.12]; print('min K\'<0.12 の内戦の歩み', len(lo) // NREP, '件 そのうち初月', f"{sum(1 for x in lo if x[0] == 1)/max(1,len(lo)):.1%}")
