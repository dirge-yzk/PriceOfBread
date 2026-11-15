# (1165) c115cr 紙（事前登録 §1-2）：旗 0 の器の〈D〉の案 B・案 S の外国の目標から、E 目標・形の選び・go／no-go の予言を出す。表を読むだけ。
# 使い方: python -X utf8 -I paper_c115.py <旗 0 の紙の生> <基準の生> <窓> <種 1-2> <thr_c115.txt>
#   E 目標＝e_c115.E（列 tFBShare・tFSShare）。予言＝州ごとに x←x＋0.025×(目標−x)（開幕 0）を四半期ごとに当て、E と同じ母集合・平時で和を四半期で平均。
import sys, os, math, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
D = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, D)
import e_c115
arm, base, win = sys.argv[1], sys.argv[2], int(sys.argv[3])
lo, hi = sys.argv[4].split('-'); seeds = range(int(lo), int(hi) + 1)
thr = {}
for line in open(sys.argv[5], encoding='utf-8'):
    if '=' in line and not line.startswith('#'): k, v = line.strip().split('=', 1); thr[k] = float(v)
ehi = thr['E_hi_short'] if win == 80 else thr['E_hi_full']
fl = e_c115.fl

def pred(s, col):
    P = e_c115.pop(base, s); W = e_c115.war(arm, s, win)
    x = collections.defaultdict(float); tot = collections.Counter(); tgt = collections.Counter(); nst = collections.Counter()
    with open(f'{arm}/rulepower_s{s}.tsv', encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for l in f:
            c = l.rstrip('\n').split('\t'); q = int(c[0])
            if q >= win: break
            key = (c[ix['region']], c[ix['nation']])
            if key not in P: continue
            t = fl(c[ix[col]]); t = 0.0 if math.isnan(t) else t
            x[key] += 0.025 * (t - x[key])
            if (q, key[1]) not in W:
                tot[q] += x[key]; tgt[q] += t
                if t >= 0.01: nst[q] += 1
    return sum(tot.values()) / win, sum(tgt.values()) / win, (max(nst.values()) if nst else 0), len(P)

print(f'# (1165) paper_c115.py 紙 {arm}・基準 {base}・窓 {win}・種 {lo}-{hi}・E の上限 {ehi:.6g}')
pa = [len(e_c115.pop(arm, s) ^ e_c115.pop(base, s)) for s in seeds]
print(f'母集合（★(1164) ③）：紙の四半期 0 で選んだ母集合と基準の母集合の違い（州・種ごと）{pa}')
res = {}
for form, col in (('B', 'tFBShare'), ('S', 'tFSShare'), ('2', None)):
    if col is None: continue
    v = [pred(s, col) for s in seeds]
    res[form] = v
    print(f'案 {form}：E 目標（種ごと）{[round(a[1], 5) for a in v]}・種の中央 {st.median([a[1] for a in v]):.6g}｜寄せの積み上がりの予言 E（種ごと）{[round(a[0], 5) for a in v]}・種の中央 {st.median([a[0] for a in v]):.6g}｜目標≧0.01 の州の数の最大 {[a[2] for a in v]}・母集合 {[a[3] for a in v]}')
eB = st.median([a[1] for a in res['B']]); eS = st.median([a[1] for a in res['S']])
ratio = eS / eB if eB > 0 else float('inf')
form = 'S' if ratio <= 2 else 'B'
print(f'形の選び（§1-2）：E 目標の種の中央 案 S÷案 B＝{ratio:.4g}（≦2 なら S）→ {form}')
pv = st.median([a[0] for a in res[form]])
verdict = 'no-go（上限の 2 倍を超える）' if pv > 2 * ehi else ('回す（上限の 1〜2 倍＝理由を追記）' if pv > ehi else '回す（上限の内）')
print(f'go／no-go（§1-2・★(719)）：選んだ形 {form} の予言 E {pv:.6g}・上限 {ehi:.6g}・比 {pv / ehi if ehi > 0 else float("inf"):.4g} → {verdict}')
