# (1208) c121b1：狙いの観測量（記録・門にしない）を生から数える＝エンジンの関数を呼ばない（読む行と財の組は台本が自分で選ぶ）。
# 使い方: python -X utf8 -I b1read.py <生のフォルダ> <種の始め> <種の終わり> [基線の生（③ の突き合わせ用）]
# ① 級 E（産地）と級 N（非産地）の単位費用の比 N÷E（recipeout_s・産出の重み・和÷和・E と N の両方を持つ財の組だけ・種ごと→中央）。旗 0 の紙の予言（unitCostIfAbundance）も同じ式で並べる。
# ② 産地の取り分＝級 E・X の産出÷（E・X・N の産出）（同じ財の組・種ごと→中央）。
# ③ 開幕（1784 の最初の q%4==3 の断面）の prodcap_s の marginalCost を州×財×レシピ（出現の順）で基線と突き合わせ、級 E の行は 新÷旧 が 1/3.5 か 1/4（賦存の割る数）に 1e-6 で合う行の数・他の級は 新＝旧 の行の数（基線を渡したときだけ）。
import sys, csv, collections, statistics as st
csv.field_size_limit(1 << 30)
raw, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); base = sys.argv[4] if len(sys.argv) > 4 else None
YEARS = (1790, 1870, 1913)
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
per = collections.defaultdict(dict)   # (year, key) -> seed -> value
for s in range(s0, s1 + 1):
    acc = collections.defaultdict(lambda: [0.0, 0.0, 0.0])   # (y, good, cls) -> out, out*unit, out*ifAb
    with open(f'{raw}/recipeout_s{s}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {c: i for i, c in enumerate(h)}
        for r in rd:
            y = int(r[ix['year']]); c = r[ix['class']]
            if y not in YEARS or c not in ('E', 'N', 'X'): continue
            o = fl(r[ix['outputSum']]); u = fl(r[ix['unitCostOutW']]); a = fl(r[ix['unitCostIfAbundance']])
            if not (o > 0) or u != u: continue
            A = acc[(y, r[ix['good']], c)]; A[0] += o; A[1] += o * u; A[2] += o * (a if a == a else u)
    for y in YEARS:
        goods = {g for (yy, g, c) in acc if yy == y}
        both = [g for g in goods if (y, g, 'E') in acc and (y, g, 'N') in acc]
        E = [0.0, 0.0, 0.0]; N = [0.0, 0.0, 0.0]; X = 0.0
        for g in both:
            for k in range(3): E[k] += acc[(y, g, 'E')][k]; N[k] += acc[(y, g, 'N')][k]
            if (y, g, 'X') in acc: X += acc[(y, g, 'X')][0]
        if E[0] > 0 and N[0] > 0:
            per[(y, 'N÷E 単位費用')][s] = (N[1] / N[0]) / (E[1] / E[0])
            per[(y, 'N÷E 紙（ifAb）')][s] = (N[2] / N[0]) / (E[2] / E[0])
            per[(y, 'E 単位費用')][s] = E[1] / E[0]
            per[(y, 'N 単位費用')][s] = N[1] / N[0]
            per[(y, '産地の取り分 (E+X)÷(E+X+N)')][s] = (E[0] + X) / (E[0] + X + N[0])
            per[(y, '財の組の数')][s] = len(both)
print(f'# b1read 生={raw} 種 {s0}〜{s1}（種ごとの値→中央・最小・最大）')
for k in sorted(per, key=lambda t: (t[1], t[0])):
    v = [per[k][s] for s in sorted(per[k])]
    print(f'{k[0]}\t{k[1]}\t中央 {st.median(v):.4f}\t最小 {min(v):.4f}\t最大 {max(v):.4f}\t種 {len(v)}')
if base:
    def open1784(d, s):
        out = {}; seen = collections.Counter()
        with open(f'{d}/prodcap_s{s}.tsv', encoding='utf-8', newline='') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {c: i for i, c in enumerate(h)}; q0 = None
            for r in rd:
                q = int(r[ix['q']])
                if q % 4 != 3: continue
                if q0 is None: q0 = q
                if q != q0: break
                k = (r[ix['region']], r[ix['good']], r[ix['recipe']]); seen[k] += 1
                out[k + (seen[k],)] = (r[ix['class']], fl(r[ix['marginalCost']]), fl(r[ix['endowFromProductivity']]))
        return out
    for s in range(s0, s1 + 1):
        a, b = open1784(raw, s), open1784(base, s)
        dev = collections.defaultdict(list); miss = 0
        for k, (c, mc, e) in a.items():
            if k not in b: miss += 1; continue
            c0, mc0, e0 = b[k]
            if not (mc0 > 0): continue
            r = mc / mc0
            dev[c].append(min(abs(r * 3.5 - 1.0), abs(r * 4.0 - 1.0)) if c == 'E' else abs(r - 1.0))
        line = ' '.join(f'{c}: 行 {len(v)} 外れ≦1e-6 {sum(1 for x in v if x <= 1e-6)}' for c, v in sorted(dev.items()))
        print(f'③ 種 {s} 1784 の mc（E は 新÷旧 が 1/3.5 か 1/4・他は 新＝旧）：{line} 鍵の欠け {miss}')
