# (1382) 問い 2・3：案③（出口＝S >= K'_B か -S >= K'_A・崩壊の代入なし）を月の歩みで見積もる（AI の近似・読むだけ・シミュではない）
#   S_t = clamp(S_{t-1} + WarPushSpeed(0.10) * p_A + 運 U(-0.12, 0.12), ±1.5)。押しの和が 0 の戦争は運も 0（Diplomacy.cs の luck *= 0.0）。
#   p_A＝その戦争の押しの割合（累計の PushA, PushB から）を一定と置く〈推〉。崩壊の講和は |戦勝点| = 1.5 * reach から勝ち側の取り分 w = reach/2（reach < 1 の時だけ確定・reach = 1 は w を 0.5〜1 で振る）。
#   K' は crownpc の四半期の値（C/(C+X)）を月に引き延ばし、基線の講和の後は最後の値のまま〈推〉。行の無い側は 1（Nation が無い Power と同じ）。
import sys, random; sys.path.insert(0, 'D:/PoB_work/r1382_work/scripts')
from load import *
PUSH, LUCK, CAP, NREP, HORIZON = 0.10, 0.12, 1.5, 200, 600
def walk(pA, kA, kB, frozen, rng, horizon=HORIZON):
    """kA, kB: 月ごとの K'（関数 m -> 値）。戻り値 (月, S, 'A負'/'B負'/'両方'/None)"""
    S = 0.0
    for m in range(1, horizon + 1):
        if not frozen: S = max(-CAP, min(CAP, S + PUSH * pA + LUCK * (2 * rng.random() - 1)))
        a, b = kA(m), kB(m)
        bl, al = S >= b, -S >= a
        if bl or al: return m, S, ('両方' if (bl and al) else ('B負' if bl else 'A負'))
    return None, S, None
import os
RALLY = float(os.environ.get('RALLY', '1'))   # 段 1 の後の見込み：戦時の C を 1/RALLY に（K'' = k/(k + RALLY(1-k))）〈推〉
def traj(seq):
    seq = [x for x in seq if x == x] or [1.0]
    if RALLY != 1: seq = [x / (x + RALLY * (1 - x)) if x < 1 else x for x in seq]
    return lambda m: seq[min(len(seq) - 1, (m - 1) // 3)]
