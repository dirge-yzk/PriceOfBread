"""(147) 史実比較の桁の読み手：国×時代（10年の窓）の兵士÷人口と軍事費÷年GDP（平時の四半期・戦時は別）＝種ごとの窓の平均の中央値を史実と比べる。
  python an_hist.py <出力dir（a*.txt と c*.txt）> [ラベル]
  判定＝比（シミュ÷史実）が 10 倍以上か 1/10 以下で NG・3〜10 倍は記録。史実がほぼ0の量は床（兵÷人口 0.1%・軍÷GDP 1%）＝両方が床の下なら可。"""
import sys, os, glob, statistics as st, collections
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
WIN = [(1784, 1793), (1815, 1824), (1855, 1864), (1904, 1913)]
# 史実の概数（平時の常備軍＝陸海・人口比%／平時の軍事費÷GDP%）【推】＝実装ログ (147) §0 の表
HS = {'イギリス': [0.5, 1.0, 1.0, 0.9], 'フランス': [0.6, 0.6, 1.6, 2.2], 'プロイセン': [3.3, 1.3, 1.1, 1.2], 'ロシア': [1.4, 1.8, 1.2, 0.8],
      '清': [0.3, 0.3, 0.25, 0.25], '日本': [1.5, 1.5, 1.5, 0.5], 'アメリカ': [0.02, 0.2, 0.05, 0.17]}
HM = {'イギリス': [3.5, 3.0, 3.0, 3.1], 'フランス': [3.0, 3.0, 3.0, 4.5], 'プロイセン': [5.0, 4.0, 2.5, 3.5], 'ロシア': [4.0, 4.0, 3.5, 5.0],
      '清': [1.0, 1.0, 1.0, 1.0], '日本': [1.0, 1.0, 1.0, 4.5], 'アメリカ': [0.5, 1.0, 0.5, 0.8]}
FLOOR_S, FLOOR_M = 0.1, 1.0
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
A = {}
for f in sorted(glob.glob(os.path.join(D, 'a*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'A': continue
        A[(sd, int(t[1]), int(t[3]))] = (int(t[2]), t[4], int(t[5]), fl(t[6]), fl(t[7]), fl(t[8]), fl(t[9]), fl(t[10]))
C = {}
for f in sorted(glob.glob(os.path.join(D, 'c*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'C': continue
        led = [fl(x) for x in t[24].split(',')]
        C[(sd, int(t[1]), int(t[3]))] = -(led[8] + led[9])   # 陸＋海の四半期の支出
seeds = sorted({k[0] for k in A})
def ratio_judge(sim, hist, floor):
    if sim != sim or hist != hist: return 'nan', '—'
    s = max(sim, 1e-12); h = max(hist, 1e-12)
    if s < floor and h < floor: return '床の下', '可'
    s2 = max(s, floor); h2 = max(h, floor)
    r = s2 / h2
    tag = 'NG' if (r >= 10 or r <= 0.1) else ('記録' if (r >= 3 or r <= 1 / 3) else '可')
    return f'{r:.2f}', tag
print(f'######## {LAB}（種 {len(seeds)}・平時＝その四半期に戦争と内戦の月が0）')
print('国 | 窓 | 兵÷人口%（平時・中央）| 史実 | 比 | 判定 | 戦時の兵÷人口%（中央）| 軍÷年GDP%（平時）| 史実 | 比 | 判定 | 戦時の軍÷年GDP%')
ng = []
for n in HS:
    for wi, (y0, y1) in enumerate(WIN):
        sp, sw, mp, mw = [], [], [], []
        for sd in seeds:
            vp, vw, gp, gw = [], [], [], []
            for (s2, q, i), v in A.items():
                if s2 != sd: continue
                yr, name, alive, gdp, pop, sold, warm, cwm = v
                if name != n or alive != 1 or yr < y0 or yr > y1 or pop <= 0 or gdp <= 0: continue
                mil = C.get((sd, q, i))
                peace = (warm == 0 and cwm == 0)
                (vp if peace else vw).append(100 * sold / pop)
                if mil is not None: (gp if peace else gw).append(100 * 4 * mil / gdp)
            if vp: sp.append(st.mean(vp))
            if vw: sw.append(st.mean(vw))
            if gp: mp.append(st.mean(gp))
            if gw: mw.append(st.mean(gw))
        med = lambda x: st.median(x) if x else float('nan')
        s_, m_ = med(sp), med(mp)
        rs, js = ratio_judge(s_, HS[n][wi], FLOOR_S)
        rm, jm = ratio_judge(m_, HM[n][wi], FLOOR_M)
        if js == 'NG': ng.append(f'{n} {y0}-{y1} 兵÷人口 {s_:.2f}%［{HS[n][wi]}］×{rs}')
        if jm == 'NG': ng.append(f'{n} {y0}-{y1} 軍÷GDP {m_:.2f}%［{HM[n][wi]}］×{rm}')
        print(f'{n} | {y0}-{y1} | {s_:.2f}（{len(sp)}種） | {HS[n][wi]} | {rs} | {js} | {med(sw):.2f}（{len(sw)}種） | {m_:.2f} | {HM[n][wi]} | {rm} | {jm} | {med(mw):.2f}')
print('NG（10倍以上）:', '・'.join(ng) if ng else 'なし')
# 世界の兵士÷人口（全国・人口加重・種の中央）
tot = collections.defaultdict(lambda: [0.0, 0.0])
for (sd, q, i), v in A.items():
    yr, name, alive, gdp, pop, sold, warm, cwm = v
    if alive != 1 or pop <= 0: continue
    tot[(sd, yr)][0] += sold; tot[(sd, yr)][1] += pop
line = []
for yr in (1784, 1800, 1815, 1840, 1860, 1880, 1900, 1913, 1930, 1950):
    xs = [100 * tot[(sd, yr)][0] / tot[(sd, yr)][1] for sd in seeds if tot[(sd, yr)][1] > 0]
    if xs: line.append(f'{yr} {st.median(xs):.2f}')
print('世界の兵士÷人口%（全ての国・人口加重・種の中央）:', '  '.join(line))
