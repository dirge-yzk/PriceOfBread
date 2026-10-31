# (711) 合流：prereg_base.sh の読み手の出力を 1 枚の表へ（読むだけ・値は読み手の出力の写し・幅＝基線・N1・N2 の最大−最小）。
import sys, re, os, statistics
sys.stdout.reconfigure(encoding='utf-8')
O = sys.argv[1]
ARMS = ['base_ins', 'n1', 'n2', 'a11']
def rd(p):
    return open(p, encoding='utf-8').read() if os.path.exists(p) else ''
print('# (711) 波49 の事前登録の材料（読むだけ・読み手 scripts/instruments/m711/prereg_base.sh）。生＝D:/PoB_work/keep_711（base_ins＝基線 m702 と同じ軌道の計器つき 10 種・n1／n2＝ヌル腕 SupplySlope 0.50000001／0.49999999・a11＝種 11〜20）。')
print('# 幅＝基線・N1・N2 の最大−最小（a11 は別の種なので幅に入れない＝並べるだけ）。値の定義は各読み手の頭（urb3/base.py・u4z/base.py・prd2/shadow_n.py）。')
print('# ⚠ urbd の W1 は〈和の絶対値〉（wGsym＝Σtotal×g の符号つき）＝旗 1 で符号が混ざると作りで下がる（(711) の合流での訂正）＝門には wAbsGsym の列を足してから（波49）。')
print()
# ---- 1. urbd
print('## 1. urbd（都市化の目標の式）：W1・W2・往復の戻す割合・向きの反転')
vals = {}
for f, labs in (('urb3_base_3arms.txt', ['base', 'n1', 'n2']), ('urb3_base_a11.txt', ['a11'])):
    t = rd(os.path.join(O, f)); cur = None
    for ln in t.splitlines():
        m = re.match(r'## (\S+)\s', ln)
        if m: cur = m.group(1); vals.setdefault(cur, {}); continue
        if cur is None: continue
        for key, pat in (('W2', r'^W2（'), ('W1', r'^W1（'), ('W1b', r'^W1b（'), ('flip', r'^flowFlip（'), ('ret_ux', r'^戻す割合（urx_s'), ('ret_rt', r'^戻す割合（urxrt_s')):
            if re.match(pat, ln):
                vals[cur][key] = float(ln.split('\t')[1])
print('| 量 | 基線 | N1 | N2 | 幅（基線・N1・N2） | a11（種 11〜20） |')
print('|---|---|---|---|---|---|')
for k, nm in (('W1', 'W1＝|Σtotal×g÷Σtotal| の 45 窓の中央値'), ('W1b', 'W1b＝手計算と同じ形'), ('W2', 'W2＝名目の都市÷農村の窓の中央値'), ('ret_ux', '戻す割合（urx_s・社会移動÷月次政治）'), ('ret_rt', '戻す割合（urxrt_s・Meritocracy の純÷PopDecision の純）'), ('flip', '向きが前の月と逆の州×月の割合')):
    v = [vals.get(a, {}).get(k) for a in ('base', 'n1', 'n2')]
    w = (max(v) - min(v)) if all(x is not None for x in v) else None
    f = lambda x: 'NA' if x is None else f'{x:.4f}'
    print(f'| {nm} | {f(v[0])} | {f(v[1])} | {f(v[2])} | {f(w)} | {f(vals.get("a11", {}).get(k))} |')
print()
# ---- 2. U4
print('## 2. U4（私的な建設の予算）：今の規則で予算が縛る州×月の割合 bind（本国格＝home・種の中央値）と流れの影 fbind')
data = {}
for a in ARMS:
    for ln in rd(os.path.join(O, f'u4z_{a}.txt')).splitlines():
        p = ln.split('\t')
        if len(p) < 6 or p[2] != 'home' or not p[1].isdigit(): continue
        g = lambda s: float(s.split('(')[0]) if s not in ('nan', '') else float('nan')
        data[(a, p[0], int(p[1]))] = (g(p[4]), g(p[5]))
nats = sorted({k[1] for k in data}, key=lambda n: ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "清", "日本", "アメリカ"].index(n) if n in ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "清", "日本", "アメリカ"] else 99)
decs = [1790, 1800, 1810, 1820, 1830, 1840, 1850, 1870, 1890, 1910, 1930]
print('| 国 | 十年 | bind 基線 | N1 | N2 | 幅 | a11 | fbind 基線 | N1 | N2 | a11 |')
print('|---|---|---|---|---|---|---|---|---|---|---|')
for n in nats:
    for d in decs:
        r = [data.get((a, n, d)) for a in ARMS]
        if all(x is None for x in r): continue
        f = lambda x, i: 'NA' if x is None or x[i] != x[i] else f'{x[i]:.3f}'
        b3 = [x[0] for x in r[:3] if x is not None and x[0] == x[0]]
        w = f'{max(b3) - min(b3):.3f}' if len(b3) == 3 else 'NA'
        print(f'| {n} | {d} | {f(r[0],0)} | {f(r[1],0)} | {f(r[2],0)} | {w} | {f(r[3],0)} | {f(r[0],1)} | {f(r[1],1)} | {f(r[2],1)} | {f(r[3],1)} |')
print()
# ---- 3. 影の N
print('## 3. 影の N（prdd §3-2・頻度の物差し）')
print('| 量 | 基線 | N1 | N2 | a11 |')
print('|---|---|---|---|---|')
rows = {}
for a in ARMS:
    t = rd(os.path.join(O, f'shadowN_{a}.txt')); r = {}
    m = re.search(r'全体：N≧1 の行 (\d+)／(\d+)（([\d.]+)%）', t); r['all'] = m.group(3) + '%' if m else 'NA'
    m = re.search(r'うちイギリスの行 \d+（N≧1 の行の ([\d.]+)%・イギリス自身の発見した行 \d+ の ([\d.]+)%）', t)
    r['uk1'] = (m.group(1) + '%') if m else 'NA'; r['uk2'] = (m.group(2) + '%') if m else 'NA'
    m = re.search(r'イギリスが N≧1 の行の半分以上の種：(\d+/\d+)', t); r['ukseeds'] = m.group(1) if m else 'NA'
    for L in ('24', '40'):
        m = re.search(r'^\| ' + L + r' \| (\d+) \| (\d+) \| (\d+) \| (\d+) \| (\d+) \| (\d+) \|', t, re.M)
        r['p' + L] = f'同じセル {m.group(1)}・先進国（広）の国×種 {m.group(4)}・国全体 {m.group(6)}' if m else 'NA'
        m = re.search(r'^\| ' + L + r' \| \d+ \| \d+ \| [\d.]+ \| (\d+) \| (\d+) \| ([\d.]+) \| ([\d.]+) \| ([\d.]+) \| ([\d.]+) \|', t, re.M)
        r['f' + L] = f'入口（同じセル・下側の見積り） {m.group(6)}・期待の勝った蜂起（広） {m.group(3)}' if m else 'NA'
    rows[a] = r
for k, nm in (('all', 'N≧1 の行の割合（共産主義を見つけた国×四半期）'), ('uk1', '読み方 1＝N≧1 の行のうち英の割合'), ('uk2', '読み方 2＝英が見つけた行のうち N≧1 の割合'), ('ukseeds', '英が N≧1 の行の半分以上の種'), ('p24', '持続 L≧24 四半期'), ('p40', '持続 L≧40 四半期'), ('f24', '頻度（L＝24・1 試合あたり・推）'), ('f40', '頻度（L＝40・推）')):
    print(f'| {nm} | ' + ' | '.join(rows[a].get(k, 'NA') for a in ARMS) + ' |')
print()
print('# ⚠ 頻度の〈入口の上限〉は下側の見積り（(711) の合流での訂正＝結束は在庫・最大でないセルも育つ）。作者の目安〈先進国 1〜2 か国・ファシズムより少ない〉は記録の物差し（門にしない）。')
