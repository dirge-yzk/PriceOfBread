"""(160) 解禁した時点の3項の読み手（inj_need の n<seed>.txt の U 行）。python an_need.py <need ディレクトリ> [製法の半分点=0.2] [新製品の半分点=0.4]
(159) 反証役の指摘 vb#4＝必要性が「高いか」の強さではなく、ほぼ「参照を超えたか」の2値になっていないか。
  ・必要性の飽和＝必要性÷70（最大）。60 以上（最大の 86%）を「飽和に近い」と数える（AI の区切り）。
  ・物を生む行（製法・新製品）は強度 s＝半分点×必要性÷(70−必要性)＝ln（代替する物の平滑化した値段÷参照価格）＝値段の倍 v＝e^s を戻せる。
  ・自力（伝わり<1）と伝わりあり（≥1）を分ける。開幕（1783年）の既知は別に数える。"""
import sys, os, glob, math, statistics as st, collections
D = sys.argv[1]
HALF = {'Process': float(sys.argv[2]) if len(sys.argv) > 2 else 0.2, 'Product': float(sys.argv[3]) if len(sys.argv) > 3 else 0.4}
rows = []
for f in sorted(glob.glob(os.path.join(D, 'n*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] != 'U': continue
        rows.append(dict(sd=sd, yr=int(t[1]), q=int(t[2]), n=t[3], row=t[4], field=t[5], tech=float(t[6]), need=float(t[7]), tr=float(t[8]), score=float(t[9])))
def med(x): return st.median(x) if x else float('nan')
def v_of(r):
    h = HALF.get(r['field'])
    if h is None or not (r['need'] < 70): return float('nan')
    return math.exp(h * r['need'] / (70 - r['need']))
print(f'######## 解禁した時点の3項（種 {len({r["sd"] for r in rows})}・解禁 {len(rows)} 件）')
for opening in (True, False):
    sel = [r for r in rows if (r['yr'] <= 1783) == opening]
    print(f'## {"1783年中の解禁（★(172) 反証役 vd3#5＝開幕のデータの既知は Step の計器を通らない＝数えていない）" if opening else "1784年以後の解禁"}：{len(sel)} 件')
    if opening:   # ★(172) vd3#5＝国名を出す（行ごと・解禁した国の数と国の名）
        byr = collections.defaultdict(list)
        for r in sel: byr[r['row']].append(r)
        for rw, s2 in sorted(byr.items(), key=lambda kv: -len(kv[1])):
            cn = collections.Counter(r['n'] for r in s2)
            print(f'  {rw}: {len(s2)} 件（種あたり {len(s2) / max(1, len({r["sd"] for r in rows})):.1f} 国）・自力 {sum(r["tr"] < 1 for r in s2)}・国＝' + '・'.join(f'{k} {v}' for k, v in cn.most_common(12)))
    print('分野 | 件数 | 自力（伝わり<1）| 必要性の中央 | 必要性≥60 の割合 | 必要性<10 の割合 | 値段の倍 v の中央（自力）| 技術力の中央')
    for fld in ('Process', 'Product', 'Weapon', 'Institution', 'Idea'):
        s = [r for r in sel if r['field'] == fld]
        if not s: continue
        own = [r for r in s if r['tr'] < 1]
        vs = [v_of(r) for r in own if v_of(r) == v_of(r)]
        print(f'{fld} | {len(s)} | {len(own)} | {med([r["need"] for r in s]):.1f} | {sum(r["need"] >= 60 for r in s) / len(s):.0%} | {sum(r["need"] < 10 for r in s) / len(s):.0%} | '
              f'{med(vs):.3g} | {med([r["tech"] for r in s]):.1f}')
# 行ごと（1784年以後・物を生む行）
print('## 行ごと（1784年以後・製法と新製品）＝件数・自力・必要性の中央・≥60 の割合・自力の値段の倍 v の中央〔四分位〕')
by = collections.defaultdict(list)
for r in rows:
    if r['yr'] > 1783 and r['field'] in HALF: by[(r['field'], r['row'])].append(r)
for (fld, name), s in sorted(by.items(), key=lambda kv: (kv[0][0], -len(kv[1]))):
    own = [r for r in s if r['tr'] < 1]
    vs = sorted(v_of(r) for r in own if v_of(r) == v_of(r))
    q = (f'{vs[len(vs) // 4]:.3g}〜{vs[(3 * len(vs)) // 4]:.3g}' if len(vs) >= 4 else '')
    print(f'  {fld[:4]} {name}: {len(s)}・自力 {len(own)}・必要性 {med([r["need"] for r in s]):.1f}・≥60 {sum(r["need"] >= 60 for r in s) / len(s):.0%}・v {med(vs):.3g}〔{q}〕')
# 世界で最初（種ごとに最も早い解禁）の必要性
print('## 世界で最初の解禁（種ごと・行ごとに最も早い1件）の必要性の分布（物を生む行・1784年以後）')
first = {}
for r in sorted(rows, key=lambda r: (r['yr'], r['q'])):
    k = (r['sd'], r['row'])
    if k not in first: first[k] = r
fs = [r for r in first.values() if r['yr'] > 1783 and r['field'] in HALF]
if fs:
    print(f'  件数 {len(fs)}・必要性の中央 {med([r["need"] for r in fs]):.1f}・≥60 {sum(r["need"] >= 60 for r in fs) / len(fs):.0%}・v の中央 {med([v_of(r) for r in fs]):.3g}・技術力の中央 {med([r["tech"] for r in fs]):.1f}')
