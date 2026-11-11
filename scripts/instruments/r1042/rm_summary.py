# (1042) rm_count.json の表（和÷和・腕ごとに 10 種を合わせる・ヌル腕の幅＝3 腕の最小〜最大・種ごとの最小〜最大）
import sys, json, collections
D = json.load(open(sys.argv[1], encoding='utf-8'))
arms = ['B', 'N1', 'N2']
by = collections.defaultdict(dict)
for r in D: by[r['arm']][r['seed']] = r['v']
def S(arm, k): return sum(v.get(k, 0.0) for v in by[arm].values())
def ratio_arm(arm, num, den): d = S(arm, den); return S(arm, num) / d if d else float('nan')
def ratio_seed(arm, seed, num, den): v = by[arm][seed]; d = v.get(den, 0); return v.get(num, 0) / d if d else float('nan')
def line(label, num, den):
    a = [ratio_arm(x, num, den) for x in arms]
    ss = [ratio_seed(x, s, num, den) for x in arms for s in by[x]]
    ss = [v for v in ss if v == v]
    print(f'{label}\tB {a[0]:.4f}\tN1 {a[1]:.4f}\tN2 {a[2]:.4f}\t幅 {min(a):.4f}〜{max(a):.4f}\t種 {min(ss):.4f}〜{max(ss):.4f}')
P = ['all', 'p1', 'p2', 'p3']
PN = {'all': '全体', 'p1': '1783-1849', 'p2': '1850-1899', 'p3': '1900-1950'}
print('# 総量（千人・10 種の和）', {x: round(S(x, 'tot|all'), 1) for x in arms})
print('# 問1 文化の分類の割合（流量の和÷総量）')
for p in P:
    for c in ['same', 'fam', 'diff', 'none']: line(f'文化 {c} {PN[p]}', f'c|{c}|{p}', f'tot|{p}')
print('# 問1 宗教の分類の割合')
for p in P:
    for c in ['same', 'fam', 'diff', 'none']: line(f'宗教 {c} {PN[p]}', f'r|{c}|{p}', f'tot|{p}')
print('# 文化×宗教（全体）')
for cc in ['same', 'fam', 'diff']:
    for rc in ['same', 'fam', 'diff']: line(f'文化{cc}×宗教{rc}', f'cr|{cc}{rc}|all', 'tot|all')
print('# 参照 A（他国の全ての活きた州へ人口比）の割合＝札に中立・距離なし')
for p in P:
    for k in ['c', 'r']:
        for c in ['same', 'fam']: line(f'参照A {k} {c} {PN[p]}', f'refA|{k}|{c}|{p}', f'refA|{k}|base|{p}')
print('# 参照 B（実際に受けた州の中で人口比）の割合＝行き先の選びと距離を固定')
for p in P:
    for k in ['c', 'r']:
        for c in ['same', 'fam', 'diff', 'none']: line(f'参照B {k} {c} {PN[p]}', f'refB|{k}|{c}|{p}', f'refB|base|{p}')
print('# 札の近似の州・札なしの州に触れる流れの割合')
for p in P: line(f'近似の札 {PN[p]}', f'approx|{p}', f'tot|{p}'); line(f'札なし {PN[p]}', f'nolab|{p}', f'tot|{p}')
print('# 受入の上限（近似）：上限の 95% 以上に届いた州へ入った量の割合・流入で重みを付けた 流入÷上限 の平均')
line('上限に届いた割合', 'cap|bind', 'cap|inflow'); line('流入÷上限の重み付き平均', 'cap|ratio_w', 'cap|inflow')
print('# 機会の差（流量で重みを付けた平均）＝行き先の機会−出る州の機会×持ち分×食料−0.5')
line('差の加重平均', 'gapw', 'tot|all')
print('# 距離（流量の重みの分位・種の平均 km）')
for qq in ['0.1', '0.5', '0.9']:
    print(qq, {x: round(sum(v.get(f'km|{qq}', 0) for v in by[x].values()) / len(by[x])) for x in arms})
print('# 連鎖：組の年の流れ と それより前の累計 の順位相関の平均')
for p in ['p1', 'p2', 'p3']: line(f'rho {PN[p]}', f'rho|{p}|sum', f'rho|{p}|n')
print('# 行き先の国教の法ごとの宗教の分類（0 国教・1 内心の自由・2 信仰の自由・3 国家無神論）')
for s in range(4):
    tot = f'__srl{s}'
    for x in arms:
        for v in by[x].values(): v[tot] = sum(v.get(f'srl|{s}|{c}', 0) for c in ['same', 'fam', 'diff', 'none'])
    if S('B', tot) > 0:
        print(f'国教の法 {s} の量 B {S("B", tot):.1f}')
        for c in ['same', 'fam', 'diff']: line(f'  宗教 {c}', f'srl|{s}|{c}', tot)
print('# 行き先の移民の法ごと（0 開放・1 選別・2 閉鎖）')
for s in range(3):
    tot = f'__imm{s}'
    for x in arms:
        for v in by[x].values(): v[tot] = sum(v.get(f'imm|{s}|{c}', 0) for c in ['same', 'fam', 'diff', 'none'])
    print(f'移民の法 {s} の量 B {S("B", tot):.1f} N1 {S("N1", tot):.1f} N2 {S("N2", tot):.1f}')
    if S('B', tot) > 0:
        for c in ['same', 'fam', 'diff']: line(f'  宗教 {c}', f'imm|{s}|{c}', tot)
print('# 出身の国の多数派の宗教と違う州からの流れの割合（州の札・ユグノー型の代用）')
for p in ['p1', 'p2', 'p3']:
    tot = f'__mn{p}'
    for x in arms:
        for v in by[x].values(): v[tot] = v.get(f'minor|min|{p}', 0) + v.get(f'minor|maj|{p}', 0)
    line(f'少数派の州から {PN[p]}', f'minor|min|{p}', tot)
print('# アメリカへの流入（u1 〜1849・u2 1850-1881・u3 1882-1923・u4 1924〜）')
for u in ['u1', 'u2', 'u3', 'u4']:
    print(f'{u} 量 B {S("B", f"us|{u}|tot"):.1f} N1 {S("N1", f"us|{u}|tot"):.1f} N2 {S("N2", f"us|{u}|tot"):.1f}')
    for k in ['c', 'r']:
        for c in ['same', 'fam', 'diff', 'none']:
            if S('B', f'us|{u}|{k}|{c}') > 0: line(f'  {k} {c}', f'us|{u}|{k}|{c}', f'us|{u}|tot')
    fr = collections.Counter()
    for v in by['B'].values():
        for kk, vv in v.items():
            if kk.startswith(f'usfrom|{u}|'): fr[kk.split('|')[2]] += vv
    T = S('B', f'us|{u}|tot')
    print('  出身の国の上位（B）', [(k, round(vv / T, 3)) for k, vv in fr.most_common(6)] if T else '')
