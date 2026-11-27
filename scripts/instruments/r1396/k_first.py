# (1396) c148dm 問い 2（dz の v 低）：内戦の反乱の国の K' の分布を台本で出す（読むだけ）。
#   (a) 反乱の国の crownpc の行が無い内戦の数（m1362 の 84 件のうち 31 件＝(1389) が K' を 1.0・0.305・0.1 で入れた物）
#   (b) 行がある内戦の、反乱の国の最初の行の K'（(1389) の p10 0.055・p25 0.064・中央 0.084・p75 0.54・p90 0.69 を数え直す）
#   使い方: python k_first.py [生のフォルダ ...]（既定は load.BASE）
import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from load import rows, fl, kprime, pct, SEEDS, BASE

VERD = {'1': 'parent', '2': 'rebel', '0': 'blank'}   # verdictCode：1＝A（親）の勝ち・2＝B（反乱）の勝ち・0＝白紙


def one(base):
    n = miss = 0
    miss_v = collections.Counter(); all_v = collections.Counter()
    k_first = []; k_by_v = collections.defaultdict(list); lag = []; mo_miss = []; mo_have = []
    for s in SEEDS:
        by_uid = collections.defaultdict(list)
        for v in rows('crownpc', s, base):
            by_uid[v['nationUid']].append((int(v['quarter']), kprime(fl(v['mobilizationIndex']), fl(v['effectiveCoerceIndex']))))
        for v in rows('cwend', s, base):
            n += 1
            vd = VERD.get(v['verdictCode'], v['verdictCode']); all_v[vd] += 1
            qe = int(v['quarter']); qs = qe - (-(-int(v['months']) // 3))   # (1408) 項 7：端数は切り上げ（切り捨てだと開戦の四半期が窓から落ちる）
            seq = sorted(x for x in by_uid.get(v['rebelUid'], []) if qs <= x[0] <= qe)
            if not seq:
                miss += 1; miss_v[vd] += 1; mo_miss.append(int(v['months']))
                continue
            mo_have.append(int(v['months'])); k_first.append(seq[0][1]); k_by_v[vd].append(seq[0][1]); lag.append(seq[0][0] - qs)
    print(f'== {base}')
    print(f'  内戦（cwend の行） {n}・判定 {dict(all_v)}')
    print(f'  反乱の国の crownpc の行が戦争の間に無い {miss}・その判定 {dict(miss_v)}')
    print(f'  月数（行なし） {pct(mo_miss, (.1, .5, .9))}・（行あり） {pct(mo_have, (.1, .5, .9))}')
    print(f'  反乱の国の最初の行の K\'  {pct(k_first)}')
    for vd, a in sorted(k_by_v.items()):
        print(f'    判定 {vd}: {pct(a, (.1, .5, .9))}')
    print(f'  最初の行の四半期 − 始まりの四半期（四半期）  {pct(lag, (.1, .5, .9))}')


if __name__ == '__main__':
    for b in (sys.argv[1:] or [BASE]):
        one(b)
