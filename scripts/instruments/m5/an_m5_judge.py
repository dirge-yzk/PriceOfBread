"""(324) 流れM 段5 後半の判定の読み手＝読むだけ・挙動は変えない。事前登録 measurements/m5_20261014_prereg.txt の [4]（線 L1〜L5）と [3]（雑音の床）。
腕を開く前にコミットする（反証役 m5〔bv〕#1・追記 [13]）。線の文は事前登録のまま・判定のコードだけをここに置く。
使い方：python an_m5_judge.py A=<ins> B=<ins> N1=<ins> N2=<ins> [NG_A=<navy表> NG_B=<navy表> NG_N1=<navy表> NG_N2=<navy表>]
  <ins>＝`INS=1 bash scripts/instruments/batch.sh run <腕>` が書く <出力>/ins（stock_s<seed>.tsv・nation_s<seed>.tsv）。
  <navy表>＝`bash scripts/instruments/ng.sh <ins> <タグ> D:/…` が書く navy_<タグ>.txt（L1 だけがこれを読む。無ければ L1 は〈読めず〉と出す）。
出す物：
 ・L2 英の艦（fleetUnitsStock）が {蘭・葡・仏・西・露} の全てより多い種の数（warm・q3・q7・q19・q39・q79・q159・q239）＝採否は q19・q39 の各で B ≥ A−2
 ・L3 艦の数と fixlist 段5 D1 の目安（19 国）の Spearman の種の中央（an_n5.py の T1 と同じ計算）＝q19・q39 の両方で B ＞ A＋max(0.05, 2δ)・δ＝max(|N1−A|, |N2−A|)
 ・L4 q19・q39 で、A で艦が 0 でない国×種のうち X で 0 になる数（X＝B・N1・N2）＝B ≤ max(2, 2×max(N1, N2))
 ・L5 q19・q39 で、X で艦が英を超える国×種のうち A には無い物の数（X＝B・N1・N2）＝B ≤ 5＋2δ（δ＝max(N1, N2)。〈5〉は雑音の床から導かれた数ではなく事前登録 [4] のまま）
 ・L1 ng.sh の navy 表の清（英＝1）の 1855-1864 と 1904-1913 の海軍力＝B ≤ 0.5×A かつ A−B ＞ 2δ（δ＝max(|N1−A|, |N2−A|)）の両窓
 ・記録：艦が 0 でない国の数（warm・q3・q19）／重みが跳ねる国×四半期（越境の輸出入÷名目GDP ＞ 1・nation_s）／B の各線の δ=0 の注
限界の自己申告：stock_s は全ての国で warm・年末（q%4==3）の行がある。q19・q39 はどちらも年末の行。艦の数は隻の単位が任意。fixlist の目安は記憶による【推】。
  L3 の Spearman は ties が多く上限が低い。δ は N1・N2 の 2 本だけで置いた床（分布の裾は見ていない）。"""
import sys, os, re, statistics, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, num, seeds as seeds_of

ARGS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[1:])
ARMS = [k for k in ('A', 'B', 'N1', 'N2') if k in ARGS]
RIVALS = ['ネーデルラント', 'ポルトガル', 'フランス', 'スペイン', 'ロシア']       # L2
HIST = {'イギリス': 150, 'ロシア': 150, 'オスマン': 90, 'フランス': 75, 'スペイン': 65, 'ネーデルラント': 35, 'デンマーク': 30, 'ヴェネツィア': 30,
        'スウェーデン': 25, '両シチリア': 25, 'ポルトガル': 12, 'プロイセン': 0, 'ハノーファー': 0, 'ライン諸邦': 0, 'アメリカ': 0, '日本': 0,
        'ハワイ王国': 0, 'エチオピア': 0, 'ペルシャ': 0}                            # L3（an_n5.py と同じ表）
TIMES = ['warm', 'q3', 'q7', 'q19', 'q39', 'q79', 'q159', 'q239']
GATE = ['q19', 'q39']

def ranks(x):
    s = sorted(range(len(x)), key=lambda i: x[i]); r = [0.0] * len(x); i = 0
    while i < len(s):
        j = i
        while j + 1 < len(s) and x[s[j + 1]] == x[s[i]]: j += 1
        for k in range(i, j + 1): r[s[k]] = (i + j) / 2
        i = j + 1
    return r
def spearman(a, b):
    ra, rb = ranks(a), ranks(b); n = len(a)
    ma, mb = sum(ra) / n, sum(rb) / n
    nu = sum((x - ma) * (y - mb) for x, y in zip(ra, rb))
    da = sum((x - ma) ** 2 for x in ra) ** .5; db = sum((y - mb) ** 2 for y in rb) ** .5
    return nu / (da * db) if da * db > 0 else float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')

S = {}      # arm -> {(seed,time): {nation: fleet}}
SEEDS = {}
for arm in ARMS:
    d = collections.defaultdict(dict)
    for sd, r in rows(ARGS[arm], 'stock'):
        if 'fleetUnitsStock' not in r: continue      # 書きかけの最後の行
        v = num(r['fleetUnitsStock'])
        d[(sd, r['time'])][r['nation']] = v
    S[arm] = d; SEEDS[arm] = seeds_of(ARGS[arm], 'stock')
seeds = SEEDS[ARMS[0]]
for arm in ARMS:
    if SEEDS[arm] != seeds: print(f'★種の集合が違う：{arm} {SEEDS[arm]} 対 {seeds}')

def l2count(arm, t):
    ok = tot = 0
    for sd in seeds:
        d = S[arm].get((sd, t), {})
        if 'イギリス' not in d or any(n not in d for n in RIVALS): continue
        tot += 1
        if all(d['イギリス'] > d[n] for n in RIVALS): ok += 1
    return ok, tot
def l3med(arm, t):
    sps = []
    for sd in seeds:
        d = S[arm].get((sd, t), {})
        ns = [n for n in HIST if n in d]
        if len(ns) >= 10: sps.append(spearman([HIST[n] for n in ns], [d[n] for n in ns]))
    return med(sps)
def zero_count(a, x, t):      # A で艦が 0 でない国×種のうち x で 0 になる数
    c = 0
    for sd in seeds:
        da, dx = S[a].get((sd, t), {}), S[x].get((sd, t), {})
        for n, v in da.items():
            if v == v and v > 0 and n in dx and dx[n] == 0: c += 1
    return c
def over_uk(arm, sd, t):
    d = S[arm].get((sd, t), {})
    uk = d.get('イギリス')
    return {n for n, v in d.items() if n != 'イギリス' and uk is not None and v == v and v > uk}
def l5count(a, x, t):         # x で英を超える国×種のうち A には無い物の数
    c = 0
    for sd in seeds:
        c += len(over_uk(x, sd, t) - over_uk(a, sd, t))
    return c

print(f'######## 判定（腕 {ARMS}・種 {len(seeds)}）')
print('--- L2 英の艦が {蘭・葡・仏・西・露} の全てより多い種の数（分母は五国と英の行がある種）')
for t in TIMES:
    print(t + '\t' + '\t'.join(f'{a} {l2count(a, t)[0]}/{l2count(a, t)[1]}' for a in ARMS))
if 'B' in S and 'A' in S:
    r2 = []
    for t in GATE:
        a, b = l2count('A', t)[0], l2count('B', t)[0]
        r2.append(b >= a - 2); print(f'L2 {t}: A {a}・B {b}・線 B ≥ A−2 → {"通る" if b >= a - 2 else "外れる"}')
    print('L2 判定:', '通る' if all(r2) else '外れる')

print('--- L3 艦の数と fixlist D1 の目安（19 国）の Spearman の種の中央')
print('時点\t' + '\t'.join(ARMS))
for t in TIMES:
    print(t + '\t' + '\t'.join(f'{l3med(a, t):.3f}' for a in ARMS))
if all(k in S for k in ('A', 'B', 'N1', 'N2')):
    r3 = []
    for t in GATE:
        a, b = l3med('A', t), l3med('B', t)
        dl = max(abs(l3med('N1', t) - a), abs(l3med('N2', t) - a))
        need = max(0.05, 2 * dl)
        ok = b > a + need; r3.append(ok)
        print(f'L3 {t}: A {a:.3f}・B {b:.3f}・δ {dl:.3f}{"（δ=0＝絶対の幅だけで判定）" if dl == 0 else ""}・線 B ＞ A＋{need:.3f} → {"通る" if ok else "外れる"}')
    print('L3 判定:', '通る' if all(r3) else '外れる')

print('--- L4 A で艦が 0 でない国×種のうち X で 0 になる数')
for t in TIMES:
    print(t + '\t' + '\t'.join(f'{x} {zero_count("A", x, t)}' for x in ARMS if x != 'A'))
if all(k in S for k in ('A', 'B', 'N1', 'N2')):
    r4 = []
    for t in GATE:
        b = zero_count('A', 'B', t); n1 = zero_count('A', 'N1', t); n2 = zero_count('A', 'N2', t)
        lim = max(2, 2 * max(n1, n2)); ok = b <= lim; r4.append(ok)
        print(f'L4 {t}: B {b}・N1 {n1}・N2 {n2}・線 B ≤ {lim} → {"通る" if ok else "外れる"}')
    print('L4 判定:', '通る' if all(r4) else '外れる')

print('--- L5 X で艦が英を超える国×種のうち A には無い物の数')
for t in TIMES:
    print(t + '\t' + '\t'.join(f'{x} {l5count("A", x, t)}' for x in ARMS if x != 'A') + '\t（A 自身で英を超える国×種 ' + str(sum(len(over_uk('A', sd, t)) for sd in seeds)) + '）')
if all(k in S for k in ('A', 'B', 'N1', 'N2')):
    r5 = []
    for t in GATE:
        b = l5count('A', 'B', t); n1 = l5count('A', 'N1', t); n2 = l5count('A', 'N2', t)
        lim = 5 + 2 * max(n1, n2); ok = b <= lim; r5.append(ok)
        print(f'L5 {t}: B {b}・N1 {n1}・N2 {n2}・線 B ≤ {lim} → {"通る" if ok else "外れる"}')
        names = collections.Counter(n for sd in seeds for n in (over_uk('B', sd, t) - over_uk('A', sd, t)))
        print(f'   B にだけある物の国の内訳: {dict(names)}')
    print('L5 判定:', '通る' if all(r5) else '外れる')

print('--- L1 清÷英の海軍力（ng.sh の navy 表・種の中央・英＝1）')
def ng_cn(arm, win):
    p = ARGS.get('NG_' + arm)
    if not p or not os.path.exists(p): return None
    for ln in open(p, encoding='utf-8'):
        if ln.startswith(win + ' 海軍力'):
            m = re.search(r'清 ([0-9.eE+-]+)［', ln)
            if m: return float(m.group(1))
    return None
if all(('NG_' + k) in ARGS for k in ('A', 'B', 'N1', 'N2')):
    r1 = []
    for win in ('1855-1864', '1904-1913'):
        a, b, n1, n2 = (ng_cn(k, win) for k in ('A', 'B', 'N1', 'N2'))
        if None in (a, b, n1, n2): print(f'{win}: 読めず（A {a}・B {b}・N1 {n1}・N2 {n2}）'); r1.append(False); continue
        dl = max(abs(n1 - a), abs(n2 - a))
        ok = b <= 0.5 * a and (a - b) > 2 * dl; r1.append(ok)
        print(f'L1 {win}: A {a}・B {b}・N1 {n1}・N2 {n2}・δ {dl:.4g}・線 B ≤ {0.5 * a:.4g} かつ A−B ＞ {2 * dl:.4g} → {"通る" if ok else "外れる"}')
    print('L1 判定:', '通る' if all(r1) else '外れる')
else:
    print('L1: NG_A／NG_B／NG_N1／NG_N2 が揃わず読めず')

print('--- 記録 艦が 0 でない国の数（種の合計の平均）')
for t in ('warm', 'q3', 'q19', 'q39'):
    print(t + '\t' + '\t'.join(f'{a} {sum(1 for sd in seeds for v in S[a].get((sd, t), {}).values() if v == v and v > 0) / max(1, len(seeds)):.1f}' for a in ARMS))

print('--- 記録 重みが跳ねる国×四半期（nation_s の越境の輸出入÷名目GDP ＞ 1・年換算＝clearingsPerYear 倍・平滑なしの清算単位の値）')
for arm in ARMS:
    cnt = collections.Counter(); tot = 0
    for sd, r in rows(ARGS[arm], 'nation'):
        if 'clearingsPerYear' not in r: continue
        g = num(r['nominalGdpPerYear'])
        if not (g > 1e-9): continue
        o = (num(r['exportsPerClearing']) + num(r['importsPerClearing'])) * num(r['clearingsPerYear']) / g
        if o > 1: cnt[r['nation']] += 1; tot += 1
    print(f'{arm}\t国×四半期の合計 {tot}（10 種の合計）\t上位: ' + '・'.join(f'{n} {c}' for n, c in cnt.most_common(8)))
