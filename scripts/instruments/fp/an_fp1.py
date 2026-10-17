# (394) B 対 A の読み手：旗 1（B）の計器つきの出力と旗 0（A）のそれを並べ、工場の能力の1人あたり（capacityRefValuePerPopK）の B/A を
# 国×四半期ごとに出す（事前登録 R5・R6）。使い方：PYTHONUTF8=1 python an_fp1.py <A の ins フォルダ> <B の ins フォルダ> [種の最大=全て] [区切りの四半期=40]
# 読むだけ。計器の列は足さない（設備 Pop.FactoryPlantK そのものの列は線 ic2 への依頼）。
# ⚠限界（この読み手が自分で申告する）：
#   ①A と B は別の軌道（旗 1 は最初の数四半期から乱流の差を生む）＝同じ四半期の B/A は〈同じ世界の中の設備の効き〉でなく〈別の世界の比〉。
#      ゆえに個々の比は両方向に振れる（最小・最大も出す）。系統的な偏りは 国の集計（Σ能力÷Σ担い手）の A と B の比と、比の中央値・上側/下側の割合で読む。
#   ②挙げた国（毎四半期）の行だけ。他の国は年末の行のみ。
import sys, glob, csv, os, re, collections, statistics as st

a_dir, b_dir = sys.argv[1], sys.argv[2]
smax = int(sys.argv[3]) if len(sys.argv) > 3 else 10**9
step = int(sys.argv[4]) if len(sys.argv) > 4 else 40


def load(d):
    out = {}
    for p in sorted(glob.glob(os.path.join(d, 'factory_s*.tsv'))):
        s = int(re.search(r'_s(\d+)\.tsv$', p).group(1))
        if s > smax: continue
        with open(p, encoding='utf-8') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                out[(s, int(r['quarter']), r['nation'])] = (float(r['capacityRefValuePerMonth']), float(r['factoryLaborPopK']), float(r['outputRefValuePerMonth']))
    return out


A, B = load(a_dir), load(b_dir)
keys = sorted(set(A) & set(B))
print(f'共通の行 {len(keys)}（A {len(A)}・B {len(B)}）・種 {sorted({k[0] for k in keys})}')
buck = collections.defaultdict(list)
sums = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0, 0.0, 0.0])
for k in keys:
    ca, pa, oa = A[k]; cb, pb, ob = B[k]
    b = k[1] // step
    if ca > 0 and cb > 0 and pa > 0 and pb > 0:
        buck[b].append((cb / pb) / (ca / pa))
    s = sums[b]
    if pa == pa and pb == pb:
        s[0] += ca; s[1] += pa; s[2] += oa; s[3] += cb; s[4] += pb; s[5] += ob
print('四半期の区切りごと：能力/担い手の B/A（国×四半期の比）の 中央値・平均・最小・最大・1.001 超の割合・0.999 未満の割合 | 集計：Σ能力/Σ担い手 の B/A・Σ産出 の B/A')
for b in sorted(buck):
    r = buck[b]; s = sums[b]
    aggc = (s[3] / s[4]) / (s[0] / s[1]) if s[0] > 0 and s[4] > 0 and s[1] > 0 else float('nan')
    aggo = s[5] / s[2] if s[2] > 0 else float('nan')
    print(f'  Q{b * step:>3}-{b * step + step - 1:<3} n={len(r):>5} 中央値 {st.median(r):.3f} 平均 {st.mean(r):.3f} 最小 {min(r):.3f} 最大 {max(r):.2f}'
          f' 上 {sum(1 for x in r if x > 1.001) / len(r):.3f} 下 {sum(1 for x in r if x < 0.999) / len(r):.3f} | 集計 能力/人 {aggc:.3f}・産出 {aggo:.3f}')
