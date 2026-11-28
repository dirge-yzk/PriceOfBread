# (1437) 問い4 の続き：輪の固まり＝州の regionShare÷gdpOutShare が国の中央の 1/10 未満（〈錠〉と呼ぶ）の州×年が、10 年後も錠のままか（読むだけ）
# 入力：<out>/<tag>_q3_track.tsv。出力：標準出力（合流が <out>/<tag>_q3b_lock.txt へ写す）
import sys, csv, collections
out, tag = sys.argv[1], sys.argv[2]
R = list(csv.DictReader(open(f"{out}/{tag}_q3_track.tsv", encoding='utf-8'), delimiter='\t'))
def lock(r):
    try:
        a, b = float(r['shareRatio']), float(r['natMedShareRatio'])
        return a < 0.1 * b if a == a and b == b and b > 0 else None
    except Exception: return None
ix = {(r['seed'], r['region'], int(r['year'])): r for r in R}
for lab, cond in (('本国格', lambda t: t in ('Core', 'Integrated')), ('植民地格', lambda t: t == 'Colonial')):
    n = locked = stay = 0; inW = []
    for r in R:
        if not cond(r['tier']): continue
        n += 1; L = lock(r)
        if L:
            locked += 1
            nx = ix.get((r['seed'], r['region'], int(r['year']) + 10))
            if nx is not None and lock(nx): stay += 1
            inW.append(float(r['inWt']))
    print(f"{lab}：州×年 {n}・錠 {locked}（{locked / max(1, n):.2f}）・10 年後も錠 {stay}/{locked}・錠の年の入った重さが 0 の割合 {sum(1 for x in inW if x == 0) / max(1, len(inW)):.2f}")
