# (1165) c115cr 門 E（事前登録 measurements/m1165_20261116_prereg_cr.txt §2）と紙の E 目標（§1-2）。表を読むだけ・エンジンを呼ばない。
# E＝平時の中核の州のうち、基準の盤面（旗 0・同じ種）の四半期 0 で Core・外国 0（nowRule2Share＝0）・持ち主≧0.5（nowRule0Share）の州の、
#   列（既定 nowRule2Share＝今の外国の取り分。紙では tFBShare・tFSShare＝〈D〉の案 B・案 S の外国の目標）の和を四半期で平均（持ち主が交戦中＝nation_s の atWarMonths＞0 の四半期を除く・行の無い四半期は 0）。
# 使い方（読み手として）: python -X utf8 -I e_c115.py <腕の生> <基準の生> <窓 80|668> <種 1-10> [列]
import sys, math, collections, statistics as st
sys.stdout.reconfigure(newline='\n')

def fl(x):
    try: return float(x)
    except ValueError: return float('nan')

def pop(base, s):
    out = set()
    with open(f'{base}/rulepower_s{s}.tsv', encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for l in f:
            c = l.rstrip('\n').split('\t')
            if int(c[0]) > 0: break
            if c[ix['tier']] == 'Core' and fl(c[ix['nowRule2Share']]) == 0 and fl(c[ix['nowRule0Share']]) >= 0.5: out.add((c[ix['region']], c[ix['nation']]))
    return out

def war(arm, s, win):
    w = set()
    with open(f'{arm}/nation_s{s}.tsv', encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for l in f:
            c = l.rstrip('\n').split('\t'); q = int(c[0])
            if q >= win: break
            if fl(c[ix['atWarMonths']]) > 0: w.add((q, c[ix['nation']]))
    return w

def E(arm, base, s, win, col='nowRule2Share', popset=None):
    P = pop(base, s) if popset is None else popset
    W = war(arm, s, win); tot = collections.Counter(); rows = 0
    with open(f'{arm}/rulepower_s{s}.tsv', encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        if col not in ix: return float('nan'), len(P), 0
        for l in f:
            c = l.rstrip('\n').split('\t'); q = int(c[0])
            if q >= win: break
            key = (c[ix['region']], c[ix['nation']])
            if key in P and (q, key[1]) not in W:
                x = fl(c[ix[col]]); rows += 1
                if not math.isnan(x): tot[q] += x
    return sum(tot.values()) / win, len(P), rows

def stat(arm, base, seeds, win, col='nowRule2Share'):
    v = [E(arm, base if base else arm, s, win, col) for s in seeds]
    return st.median([x[0] for x in v]), v

def upper(vals):
    mx, mn = max(vals), min(vals)
    if mx <= 0: return 0.0
    if mn <= 0: return 2 * mx
    return mx * (1 + 2 * (mx / mn - 1))

if __name__ == '__main__':
    arm, base, win = sys.argv[1], sys.argv[2], int(sys.argv[3])
    lo, hi = sys.argv[4].split('-'); seeds = range(int(lo), int(hi) + 1)
    col = sys.argv[5] if len(sys.argv) > 5 else 'nowRule2Share'
    m, v = stat(arm, base, seeds, win, col)
    print(f'E {col} 窓 {win} 種 {lo}-{hi}：種の中央 {m:.6g}・種ごと {[round(x[0], 6) for x in v]}・母集合の州 {[x[1] for x in v]}・行 {[x[2] for x in v]}')
