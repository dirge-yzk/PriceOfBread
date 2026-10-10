"""(204) 不履行の判定の鎖の読み手（読むだけ）。python an_d204.py <out_dir（d204 を含む）> [タグ] [--seeds N]
入力＝inj_d204 の d<seed>.txt（X＝不履行の瞬間・E＝不履行にならずに閉じた窓・Q＝国×四半期）。
段（README §1-8＝鎖の各段を計器で読む）：
  ① 借入の門が閉じる（利払い÷歳入≧限界）② 払えない月（r＜1）③ 払えない額を何が縛ったか（兌換＝金・銀の窓／月の上限／移しの不足／ほか）
  ④ 張り付きの窓が8か月続く→不履行／途中で閉じる（r＝1・門が開く）
出すもの＝主要10か国と全ての国：不履行の件数・前触れ（窓の r の平均の帯）・縛った段・兌換・通貨の法・交戦・利払い÷歳入・国債÷年GDP・税率の動き／
  窓が不履行で終わった割合（r の平均の帯ごと）／時代ごとの不履行。"""
import sys, os, glob, statistics as st, collections
args = sys.argv[1:]
maxsd = 99
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
D = args[0]; TAG = args[1] if len(args) > 1 else D
NATS = ['イギリス', 'フランス', 'アメリカ', 'ロシア', 'オスマン', 'プロイセン', 'オーストリア', '日本', 'スペイン', '清']
LAW = {0: '不換', 1: '銀', 2: '金'}
BIND = ['兌換', '上限', '移し', 'ほか', '額0']


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')


def band(r):
    return '≥0.99' if r >= 0.99 else ('0.95-0.99' if r >= 0.95 else ('0.70-0.95' if r >= 0.7 else '<0.70'))


BANDS = ['≥0.99', '0.95-0.99', '0.70-0.95', '<0.70']
X = []; E = []; Q = collections.defaultdict(lambda: [0.0] * 6); seeds = []
for f in sorted(glob.glob(os.path.join(D, 'd204', 'd*.txt')), key=lambda p: int(os.path.basename(p)[1:-4])):
    sd = int(os.path.basename(f)[1:-4])
    if sd > maxsd: continue
    seeds.append(sd)
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] in ('X', 'E'):
            # X|q|idx|yr|name|kind|n|avgR|minR|bConv|bCap|bXfer|bOther|bNoDef|conv|law|intRev|debtGdp|bondRate|war|tax0|tax1|M;...
            d = dict(sd=sd, q=int(t[1]), yr=int(t[3]), name=t[4], why=t[5])
            keys = ['n', 'avgR', 'minR', 'bConv', 'bCap', 'bXfer', 'bOther', 'bNoDef', 'conv', 'law', 'intRev', 'debtGdp', 'bondRate', 'war', 'tax0', 'tax1']
            for k, v in zip(keys, t[6:22]): d[k] = fl(v)
            if t[0] == 'X':
                ms = '|'.join(t[22:])
                d['months'] = [[fl(x) for x in m.split(',')] for m in ms.split(';')[1:]] if ms.startswith('M') else []
                X.append(d)
            else:
                E.append(d)
        elif t[0] == 'Q':
            yr = int(t[2]); era = 0 if yr < 1816 else (1 if yr < 1850 else (2 if yr < 1914 else 3))
            for k in range(6):
                v = fl(t[5 + k])
                if k == 5: Q[(t[4], era)][k] = max(Q[(t[4], era)][k], v)
                else: Q[(t[4], era)][k] += v
ns = len(seeds)
print(f'######## {TAG}（種 {ns}）＝不履行の判定の鎖（借入の門→r＜1→払えない額を縛った段→8か月の張り付き→不履行）')
if not seeds:
    print('# d204 の行が無い'); sys.exit(0)


def bindmain(d):
    b = [d['bConv'], d['bCap'], d['bXfer'], d['bOther'], d['bNoDef']]
    return BIND[max(range(5), key=lambda i: b[i])]


print('## ① 不履行（X 行・種の和）＝件数・窓の r の平均の帯（≥0.99＝前触れなし）・縛った段（窓の月の多数）・兌換の中央・通貨の法・交戦の窓の割合の中央・利払い÷歳入・国債÷年GDP・国債の金利・税率（窓の初め→終わり）・年')
for grp in NATS + ['_他', '_全て']:
    if grp == '_全て': ev = X
    elif grp == '_他': ev = [d for d in X if d['name'] not in NATS]
    else: ev = [d for d in X if d['name'] == grp]
    if not ev:
        print(f'{grp} | 0 件'); continue
    bc = collections.Counter(band(d['avgR']) for d in ev)
    bm = collections.Counter(bindmain(d) for d in ev)
    lw = collections.Counter(LAW.get(int(d['law'] + 0.5), '?') for d in ev)
    yrs = '' if grp.startswith('_') else ' | 年 ' + ' '.join(str(d['yr']) for d in sorted(ev, key=lambda d: d['yr']))
    print(f'{grp} | {len(ev)} 件（種あたり {len(ev) / ns:.1f}） | r の帯 ' + '・'.join(f'{b} {bc[b]}' for b in BANDS)
          + ' | 縛った段 ' + '・'.join(f'{k} {v}' for k, v in bm.most_common()) + f' | 兌換 {med([d["conv"] for d in ev]):.2f} | 法 ' + '・'.join(f'{k} {v}' for k, v in lw.most_common())
          + f' | 交戦 {med([d["war"] for d in ev]):.2f} | 利払い÷歳入 {med([d["intRev"] for d in ev]):.2f} | 国債÷年GDP {med([d["debtGdp"] for d in ev]):.2f} | 金利 {med([d["bondRate"] for d in ev]):.3f}'
          + f' | 税率 {med([d["tax0"] for d in ev]):.3f}→{med([d["tax1"] for d in ev]):.3f}' + yrs)
maj = [d for d in X if d['name'] in NATS]
print(f'## 主要10か国の不履行 {len(maj)} 件（種あたり {len(maj) / ns:.2f}）・うち r の平均 ≥0.99 が {sum(1 for d in maj if d["avgR"] >= 0.99)}・全ての国 {len(X)} 件（≥0.99 が {sum(1 for d in X if d["avgR"] >= 0.99)}）')

print('## ② 前触れのない不履行（r の平均 ≥0.99）の月ごとの中身（窓の8か月の中央）＝払えない額÷請求・余地÷払えない額・上限÷払えない額・切り下げ÷頼んだ額・兌換・縛った段の月の数')
for lab, sel in (('≥0.99', lambda d: d['avgR'] >= 0.99), ('<0.99', lambda d: d['avgR'] < 0.99)):
    ev = [d for d in X if sel(d)]
    if not ev: continue
    ms = [m for d in ev for m in d['months']]
    if not ms: continue
    bc = collections.Counter(BIND[int(m[6])] if m[6] == m[6] else '?' for m in ms)
    print(f'{lab} | 不履行 {len(ev)}・月 {len(ms)} | 払えない額÷請求 {med([m[1] for m in ms]):.4f} | 余地÷払えない額 {med([m[2] for m in ms]):.3f} | 上限÷払えない額 {med([m[3] for m in ms]):.2f} | 切り下げ÷頼んだ額 {med([m[4] for m in ms]):.3f} | 兌換 {med([m[5] for m in ms]):.2f} | 縛った段の月 ' + '・'.join(f'{k} {v}' for k, v in bc.most_common()))
    lw = collections.Counter(LAW.get(int(d['law'] + 0.5), '?') for d in ev)
    print(f'    法 ' + '・'.join(f'{k} {v}' for k, v in lw.most_common()) + f' | 兌換≥0.99 の不履行 {sum(1 for d in ev if d["conv"] >= 0.99)}・兌換≤0.01 {sum(1 for d in ev if d["conv"] <= 0.01)}')

print('## ③ 張り付きの窓の行き先（X＝不履行・E＝閉じた窓）＝r の平均の帯ごと：窓の数・不履行で終わった割合・閉じた理由（r1＝払えた月／gate＝門が開いた／debt0／warm）・窓の長さの中央')
for b in BANDS:
    xe = [d for d in X if band(d['avgR']) == b]; ee = [d for d in E if band(d['avgR']) == b]
    n = len(xe) + len(ee)
    if not n: continue
    wc = collections.Counter(d['why'] for d in ee)
    print(f'{b} | 窓 {n} | 不履行 {len(xe)}（{len(xe) / n:.2f}） | 閉じた ' + '・'.join(f'{k} {v}' for k, v in wc.most_common()) + f' | 閉じた窓の長さの中央 {med([d["n"] for d in ee]):.0f} か月')
eb = collections.Counter(bindmain(d) for d in E)
print('## 閉じた窓の縛った段 ' + '・'.join(f'{k} {v}' for k, v in eb.most_common()) + f'・窓の長さ 1〜7 か月の分布 ' + ' '.join(f'{k}:{v}' for k, v in sorted(collections.Counter(int(d["n"]) for d in E).items())))

print('## ④ 時代ごと（Q 行・種の和÷種）＝r＜1 の月・張り付きの月・門が閉じた月・(1−r) の和・張り付きの月の (1−r) の和（主要10か国）')
ERAS = ['1783-1815', '1816-1849', '1850-1913', '1914-1950']
for n in NATS:
    parts = []
    for e in range(4):
        v = Q.get((n, e))
        if not v: parts.append(f'{ERAS[e]} 0'); continue
        parts.append(f'{ERAS[e]} {v[0] / ns:.0f}/{v[1] / ns:.0f}/{v[2] / ns:.0f}/{v[3] / ns:.2f}/{v[4] / ns:.2f}')
    print(f'{n} | ' + ' | '.join(parts))
dec = collections.Counter((d['yr'] // 10) * 10 for d in maj)
print('## 主要10か国の不履行の年代（種の和）：' + ' '.join(f'{k}s {v}' for k, v in sorted(dec.items())))

# ⑤ 史実の頻度＝Reinhart & Rogoff (2008) "This Time is Different: A Panoramic View of Eight Centuries of Financial Crises"（NBER WP 13882）
#   表3（19世紀の対外の不履行と組み替え）・表4〜5（20世紀）と表2（1800年より前・仏 1788）の年を 1783〜1950 で数えた（後続の2年以内は1件＝R&R の数え方）。
#   英・米（連邦）は0（R&R の「不履行の無い国」）。プロイセンはプロイセン 1807・1813＋ドイツ 1932・1939。
RR = {'イギリス': [], 'フランス': [1788, 1812], 'アメリカ': [], 'ロシア': [1839, 1885, 1918], 'オスマン': [1876, 1915, 1931, 1940],
      'プロイセン': [1807, 1813, 1932, 1939], 'オーストリア': [1802, 1805, 1811, 1816, 1868, 1938, 1940], '日本': [1942], 'スペイン': [1809, 1820, 1831, 1834, 1851, 1867, 1872, 1882], '清': [1921, 1939]}
H = sum(len(v) for v in RR.values())
per = collections.Counter(d['sd'] for d in X if d['name'] in NATS)
vals = sorted(per.get(s, 0) for s in seeds)
m = st.median(vals)
r = m / H
lab = 'NG（少なすぎ）' if r <= 0.1 else ('NG（多すぎ）' if r >= 10 else ('記録' if (r <= 1 / 3 or r >= 3) else '可'))
print(f'## ⑤ 史実の頻度（主要10か国・1783〜1950）＝R&R 2008 表2〜5 の {H} 件｜シミュ＝種ごとの件数の中央 {m:.1f}〔{vals[0]}〜{vals[-1]}〕｜比 {r:.3f}＝{lab}')
print('国ごと（シミュ＝種の和÷種｜史実の件数・年）：' + '・'.join(f'{n} {sum(1 for d in X if d["name"] == n) / ns:.1f}｜{len(RR[n])}' for n in NATS))
print('前触れのない（r の平均 ≥0.99）主要10か国の不履行：種の和 ' + str(sum(1 for d in X if d['name'] in NATS and d['avgR'] >= 0.99)) + '・英 ' + str(sum(1 for d in X if d['name'] == 'イギリス')) + '（うち前触れなし ' + str(sum(1 for d in X if d['name'] == 'イギリス' and d['avgR'] >= 0.99)) + '）')
