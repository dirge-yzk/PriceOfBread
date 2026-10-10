"""(208) 金本位の波の入力の読み手（s208 の N・W 行・読むだけ）。python an_s208.py <out_dir/s208> [ラベル] [詳細=0/1]
① 世界の銀÷金の比（同じ州・群ごと）と銀・金の値段・造幣の買いと準備の売り（10年の窓・種ごとの窓の平均→種の中央）
② 法の移り（銀→金・金→銀・ほか）の経路（AI の願い／要求／強制）と、移った四半期の入力（統計の銀の下落・交易網の引力・純支持・工業化・取引の相手の金の割合・自国の比）
③ 法の国の数（年の断面・種の中央）と主要国が金へ移った年（種ごと）
④ 取引の相手の金の割合（法ごと・10年の窓）"""
import sys, os, re, statistics as st
from collections import defaultdict, Counter

d = sys.argv[1]; lab = sys.argv[2] if len(sys.argv) > 2 else d; detail = len(sys.argv) > 3 and sys.argv[3] == '1'
WIN = [(y, y + 9) for y in range(1783, 1950, 10)]
MAJ = ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "清", "日本", "オーストリア", "スペイン", "オスマン帝国"]


def fl(x):
    try: return float(x)
    except: return float('nan')


def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')


seeds = {}
for fn in sorted(os.listdir(d)):
    m = re.match(r's(\d+)\.txt$', fn)
    if not m: continue
    N = defaultdict(list); W = []
    for ln in open(os.path.join(d, fn), encoding='utf-8'):
        if ln.startswith('N|'):
            p = ln.rstrip('\n').split('|'); N[p[4]].append(p)
        elif ln.startswith('W|'):
            W.append(ln.rstrip('\n').split('|'))
    seeds[int(m.group(1))] = (N, W)
S = sorted(seeds)
print(f'# {lab}：金本位の波の入力 s208・{len(S)}種（10年の窓＝種ごとの窓の平均→種の中央）')

# ---- ① ----
print('\n## ① 同じ州の銀÷金の比（÷参照の比）と値段（÷参照・幾何平均）・造幣と準備（量・四半期の和の窓の平均）')
print('窓 | R 全て | R 金の国の州 | R 銀の国の州 | 銀 金の国 | 金 金の国 | 銀 銀の国 | 金 銀の国 | 統計÷参照 | 造幣の買い 銀 | 造幣の買い 金 | 準備の売り 銀 | 準備 銀 | 準備 金 | 銀の産出 | 金の産出 | 銀の需要 | 金の需要 | 金の国 | 銀の国')
COL = {'Rg': 3, 'Agg': 5, 'Aug': 6, 'Rs': 7, 'Ags': 9, 'Aus': 10, 'Ra': 15, 'prodAg': 19, 'prodAu': 20, 'demAg': 21, 'demAu': 22, 'mintAg': 25, 'mintAu': 26, 'sellAg': 27, 'resAg': 29, 'resAu': 30, 'stat': 31, 'nG': 32, 'nS': 33}
for (a, b) in WIN:
    row = {k: [] for k in COL}
    for s in S:
        W = seeds[s][1]
        ws = [p for p in W if a <= int(p[2]) <= b]
        if not ws: continue
        for k, c in COL.items():
            row[k].append(st.mean([x for x in (fl(p[c]) for p in ws) if x == x] or [float('nan')]))
    f = lambda k, n=3: f"{med(row[k]):.{n}f}"
    g = lambda k: f"{med(row[k]):.3g}"
    print(f"{a}-{b} | {f('Ra')} | {f('Rg')} | {f('Rs')} | {f('Agg')} | {f('Aug')} | {f('Ags')} | {f('Aus')} | {f('stat')} | {g('mintAg')} | {g('mintAu')} | {g('sellAg')} | {g('resAg')} | {g('resAu')} | {g('prodAg')} | {g('prodAu')} | {g('demAg')} | {g('demAu')} | {f('nG', 1)} | {f('nS', 1)}")

# ---- ② ----
print('\n## ② 法の移り（四半期の終わりの法が前の四半期と違う）の経路と入力')
moves = []
for s in S:
    N = seeds[s][0]
    for nm, rows in N.items():
        prev = None
        for p in rows:
            law = int(p[5])
            if prev is not None and law != prev:
                moves.append((s, nm, int(p[2]), prev, law, p))
            prev = law
kinds = Counter((m[3], m[4]) for m in moves)
print('移り（前→後・0不換1銀2金）: ' + '・'.join(f"{k[0]}→{k[1]} {v}" for k, v in sorted(kinds.items())) + f'（{len(S)}種の和）')
WN = {0: '願い無し', 1: '他', 2: '工業化の閾', 3: '金の法'}
for key in [(1, 2), (2, 1), (2, 0), (0, 1), (1, 0), (0, 2)]:
    ms = [m for m in moves if (m[3], m[4]) == key]
    if not ms: continue
    path = Counter()
    for m in ms:
        p = m[5]
        path[('強制' if int(p[9]) > 0 else '要求' if int(p[8]) > 0 else 'AI') + '・' + WN.get(int(p[7]), '?')] += 1
    print(f"\n### {key[0]}→{key[1]}：{len(ms)} 件・経路 " + '・'.join(f"{k} {v}" for k, v in path.most_common()))
    dec = defaultdict(int)
    for m in ms: dec[(m[2] - 1783) // 10] += 1
    print('  年代（10年）: ' + ' '.join(f"{1783 + k * 10}:{v}" for k, v in sorted(dec.items())))
    print('  入力の中央（移った四半期）: 統計の銀の下落 {:.3f}・交易網の引力 {:.3f}・純支持 {:.3f}・工業化 {:.2f}（先端 {:.2f}）・取引の金の割合 {:.3f}・銀の割合 {:.3f}・自国の比 {:.3f}・期待インフレ {:.4f}'.format(
        med([fl(m[5][14]) for m in ms]), med([fl(m[5][15]) for m in ms]), med([fl(m[5][10]) for m in ms]), med([fl(m[5][17]) for m in ms]), med([fl(m[5][18]) for m in ms]),
        med([fl(m[5][22]) for m in ms]), med([fl(m[5][23]) for m in ms]), med([fl(m[5][19]) for m in ms]), med([fl(m[5][16]) for m in ms])))
    if detail:
        for m in sorted(ms, key=lambda x: (x[2], x[0])):
            p = m[5]
            print(f"    s{m[0]} {m[2]} {m[1]} 経路={'強制' if int(p[9]) > 0 else '要求' if int(p[8]) > 0 else 'AI'} 願い={WN.get(int(p[7]))} 下落={fl(p[14]):.2f} 引力={fl(p[15]):.3f} 純={fl(p[10]):.2f} 工={fl(p[17]):.2f} 金割={fl(p[22]):.2f} 比={fl(p[19]):.3f}")

# ---- ③ ----
print('\n## ③ 法の国の数（年の最後の四半期・人口>0・種の中央）と主要国が金になった最初の年（種ごと）')
for yr in (1793, 1813, 1833, 1853, 1863, 1873, 1883, 1893, 1903, 1913, 1923, 1933, 1949):
    g = []; sv = []; fi = []
    for s in S:
        N = seeds[s][0]
        cg = cs = cf = 0
        for nm, rows in N.items():
            rs = [p for p in rows if int(p[2]) == yr]
            if not rs: continue
            p = rs[-1]
            if not fl(p[32]) > 0: continue
            law = int(p[5]); cg += law == 2; cs += law == 1; cf += law == 0
        g.append(cg); sv.append(cs); fi.append(cf)
    print(f"{yr} 金 {med(g):.0f}・銀 {med(sv):.0f}・不換 {med(fi):.0f}")
for nm in MAJ:
    yrs = []
    for s in S:
        rows = seeds[s][0].get(nm, [])
        y = next((int(p[2]) for p in rows if int(p[5]) == 2), None)
        yrs.append(str(y) if y else '—')
    print(f"  {nm}: " + ' '.join(yrs))

# ---- ④ ----
print('\n## ④ 取引の相手の金の割合（越境の成約の額・法ごと・国×四半期の中央→種の中央）')
print('窓 | 銀の国の金の割合 | 銀の国の銀の割合 | 金の国の金の割合 | 金の国の銀の割合 | 銀の国で金が過半の割合')
for (a, b) in WIN:
    sg = []; ss = []; gg = []; gs = []; maj = []
    for s in S:
        N = seeds[s][0]
        x1 = []; x2 = []; y1 = []; y2 = []
        for nm, rows in N.items():
            for p in rows:
                if not (a <= int(p[2]) <= b) or not fl(p[21]) > 0: continue
                if p[5] == '1': x1.append(fl(p[22])); x2.append(fl(p[23]))
                elif p[5] == '2': y1.append(fl(p[22])); y2.append(fl(p[23]))
        sg.append(med(x1)); ss.append(med(x2)); gg.append(med(y1)); gs.append(med(y2)); maj.append(sum(1 for v in x1 if v > 0.5) / len(x1) if x1 else float('nan'))
    print(f"{a}-{b} | {med(sg):.3f} | {med(ss):.3f} | {med(gg):.3f} | {med(gs):.3f} | {med(maj):.3f}")
