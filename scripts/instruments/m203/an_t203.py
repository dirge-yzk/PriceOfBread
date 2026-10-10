"""(203) 潤沢の減税と国庫の退蔵・国債の減り方の読み手（読むだけ）。
python an_t203.py <out_dir> [ラベル]
  入力＝<out>/t203/t*.txt（T 行＝inj_t203）・<out>/hist/c*.txt（C 行＝年の名目GDP・台帳の四半期差分・国債）・<out>/f181/f*.txt（D 行＝不履行）
  ①国×時代（種ごとに四半期の中央→種の中央）：国庫÷年GDP・国庫÷潤沢線・国庫÷月の歳入・減税が掛かった四半期の割合・掛かった時の倍率の中央・
    段の不感帯＝減税が掛かった四半期のうち、減税の前の段が1以上の軸で、減税の後も同じ段に丸まった割合（所得・人頭・土地・累進）・門の後に今の段より下げた割合
  ②英の10年の窓：国庫÷年GDP・国債÷年GDP（窓の初めと終わり）・名目GDP の伸び（倍）・借入÷年GDP・減債÷年GDP・利払い÷年GDP・税と関税÷年GDP・不履行（窓の中の件数・種の和）
  ③英の国債÷年GDP の減りの分解（10年の窓・種の中央）＝ log(終わり÷初め) を ①名目GDP の伸び ②減債 ③借入 ④その他（利子の繰り入れ・不履行）へ
"""
import sys, os, glob, math, statistics as st, collections
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
NAT = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本', '清']
ERA = [(1784, 1814), (1815, 1849), (1850, 1913), (1914, 1950)]
TOP = [0.35, 0.5, 0.4, 0.8]   # 所得・人頭・土地・累進（Government.TaxLawTop）
AX = ['所得', '人頭', '土地', '累進']


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def q5(x): return math.floor(x / 0.05 + 0.5) * 0.05 if x >= 0 else -math.floor(-x / 0.05 + 0.5) * 0.05


def stage(x, top):
    if x != x: return 0
    v = q5(x) / top * 3
    s = math.floor(v + 0.5) if v >= 0 else -math.floor(-v + 0.5)
    return max(0, min(3, int(s)))


def med(x):
    x = [y for y in x if y == y]
    return st.median(x) if x else float('nan')


def era(y):
    for i, (a, b) in enumerate(ERA):
        if a <= y <= b: return i
    return None


# C 行（GDP・国債・台帳）
GDP = {}   # (sd, q, name) -> (annGdp, debt, ledger)
for f in glob.glob(os.path.join(D, 'hist', 'c*.txt')):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'C' or t[4] not in NAT or t[5] != '1': continue
        GDP[(sd, int(t[1]), t[4])] = (fl(t[6]), fl(t[25]), [fl(x) for x in t[24].split(',')], int(t[2]))
# D 行（不履行）
DEF = collections.Counter()   # (sd, name, yr)
for f in glob.glob(os.path.join(D, 'f181', 'f*.txt')):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'D' or len(t) < 6: continue
        nm = t[4]
        if nm in NAT: DEF[(sd, nm, int(t[3]))] += 1
# T 行
acc = collections.defaultdict(lambda: collections.defaultdict(list))   # (name, era) -> sd -> rows
seeds = set()
for f in glob.glob(os.path.join(D, 't203', 't*.txt')):
    sd = int(os.path.basename(f)[1:-4]); seeds.add(sd)
    seen = set()
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'T': continue
        q, nm, yr = int(t[1]), t[3], int(t[4])
        if nm not in NAT: continue
        if (q, nm) in seen: continue   # 助走の重複（q=0 の2行目以降）は最初の行だけ
        seen.add((q, nm))
        e = era(yr)
        if e is None: continue
        v = [fl(x) for x in t[5:]]
        # v: 0 treasury 1 line 2 relieved 3 mul 4-8 pre 9-13 aft 14-18 gate 19-23 cur 24 rev 25 claims 26 soldier 27 debt 28 war
        g = GDP.get((sd, q + 1, nm)) or GDP.get((sd, q, nm))
        acc[(nm, e)][sd].append((v, g[0] if g else float('nan'), yr))
seeds = sorted(seeds)
print(f'######## {LAB}（種 {len(seeds)}）')
print('\n## ① 国×時代：国庫÷年GDP・国庫÷潤沢線・国庫÷月の歳入（か月）・減税の四半期の割合・倍率の中央・段の不感帯（所得/人頭/土地/累進＝減税の後も同じ段の割合〔前の段≥1 の四半期〕）・門の後に今の段より下げた割合（4軸の和）')
print('国 | 時代 | 国庫÷年GDP | 国庫÷潤沢線 | 国庫÷月の歳入 | 潤沢線÷月の歳入 | 減税の割合 | 倍率 | 不感帯 所得/人頭/土地/累進 | 下げた割合 | 平時の歳入÷請求')
for nm in NAT:
    for e, (a, b) in enumerate(ERA):
        per = acc.get((nm, e))
        if not per: continue
        cols = collections.defaultdict(list)
        for sd, rows in per.items():
            tg = [r[0][0] / r[1] for r in rows if r[1] > 0]
            tl = [r[0][0] / r[0][1] for r in rows if r[0][1] > 1e-9]
            tr = [r[0][0] / r[0][24] for r in rows if r[0][24] > 0]
            lr = [r[0][1] / r[0][24] for r in rows if r[0][24] > 0]
            rel = [r for r in rows if r[0][2] == 1]
            cols['tg'].append(med(tg)); cols['tl'].append(med(tl)); cols['tr'].append(med(tr)); cols['lr'].append(med(lr))
            cols['rel'].append(len(rel) / len(rows) if rows else float('nan'))
            cols['mul'].append(med([r[0][3] for r in rel]))
            for k in range(4):
                n0 = [r for r in rel if stage(r[0][4 + k], TOP[k]) >= 1]
                same = [r for r in n0 if stage(r[0][9 + k], TOP[k]) == stage(r[0][4 + k], TOP[k])]
                cols[f'dz{k}'].append(len(same) / len(n0) if n0 else float('nan'))
            down = 0; tot = 0
            for r in rel:
                for k in range(4):
                    cur = stage(r[0][19 + k], TOP[k]); gt = stage(r[0][14 + k], TOP[k])
                    if cur >= 1: tot += 1; down += gt < cur
            cols['down'].append(down / tot if tot else float('nan'))
            pw = [r[0][24] / r[0][25] for r in rows if r[0][28] == 0 and r[0][25] > 0]
            cols['rc'].append(med(pw))
        dz = '/'.join(f'{med(cols[f"dz{k}"]):.2f}' for k in range(4))
        print(f'{nm} | {a}-{b} | {med(cols["tg"]):.3f} | {med(cols["tl"]):.2f} | {med(cols["tr"]):.1f} | {med(cols["lr"]):.1f} | {med(cols["rel"]):.2f} | {med(cols["mul"]):.2f} | {dz} | {med(cols["down"]):.2f} | {med(cols["rc"]):.2f}')

# ② 英の10年の窓
WIN = [(y, y + 9) for y in range(1784, 1950, 10)]
print('\n## ② 10年の窓（英・仏・米）：国庫÷年GDP（中央）・国債÷年GDP（初め→終わり）・名目GDP の伸び・借入・減債・利払い・税＋関税（÷年GDP の窓の和÷年数）・不履行（種の和）')
for nm in ['イギリス', 'フランス', 'アメリカ', 'プロイセン']:
    print(f'### {nm}')
    print('窓 | 国庫÷年GDP | 国債÷年GDP 初め→終わり | 名目GDP の伸び | 借入 | 減債 | 利払い | 税＋関税 | 不履行')
    for (a, b) in WIN:
        cols = collections.defaultdict(list)
        ndef = sum(c for (sd, n, y), c in DEF.items() if n == nm and a <= y <= b)
        for sd in seeds:
            qs = sorted(q for (s, q, n) in GDP if s == sd and n == nm and a <= GDP[(s, q, n)][3] <= b)
            if len(qs) < 8: continue
            g0, d0 = GDP[(sd, qs[0], nm)][0], GDP[(sd, qs[0], nm)][1]
            g1, d1 = GDP[(sd, qs[-1], nm)][0], GDP[(sd, qs[-1], nm)][1]
            gs = sum(GDP[(sd, q, nm)][0] / 4 for q in qs)
            led = [sum(GDP[(sd, q, nm)][2][k] for q in qs) for k in range(25)]
            cols['d0'].append(d0 / g0 if g0 > 0 else float('nan')); cols['d1'].append(d1 / g1 if g1 > 0 else float('nan'))
            cols['gg'].append(g1 / g0 if g0 > 0 else float('nan'))
            cols['bor'].append(led[3] / gs); cols['rep'].append(-led[17] / gs); cols['int'].append(-led[16] / gs); cols['tax'].append((led[0] + led[1]) / gs)
            tg = []
            for (v, gd, yr) in [x for e in range(4) for x in acc.get((nm, e), {}).get(sd, [])]:
                if a <= yr <= b and gd > 0: tg.append(v[0] / gd)
            cols['tg'].append(med(tg))
        if not cols['d0']: continue
        print(f'{a}-{b} | {med(cols["tg"]):.3f} | {med(cols["d0"]):.3f}→{med(cols["d1"]):.3f} | {med(cols["gg"]):.2f} | {100*med(cols["bor"]):.2f}% | {100*med(cols["rep"]):.2f}% | {100*med(cols["int"]):.2f}% | {100*med(cols["tax"]):.1f}% | {ndef}')

# ③ 英の国債÷年GDP の減りの分解
print('\n## ③ 英の国債÷年GDP の変化の分解（10年の窓・種の中央）＝Δlog(国債÷GDP)＝Δlog(国債)−Δlog(GDP)・Δlog(国債)≈(借入−減債＋その他)÷平均の国債')
print('窓 | Δlog(国債÷GDP) | −Δlog(名目GDP) | 借入÷平均の国債 | −減債÷平均の国債 | その他（利子の繰り入れ・不履行・残差）')
for (a, b) in WIN:
    cols = collections.defaultdict(list)
    for sd in seeds:
        nm = 'イギリス'
        qs = sorted(q for (s, q, n) in GDP if s == sd and n == nm and a <= GDP[(s, q, n)][3] <= b)
        if len(qs) < 8: continue
        g0, d0 = GDP[(sd, qs[0], nm)][0], GDP[(sd, qs[0], nm)][1]
        g1, d1 = GDP[(sd, qs[-1], nm)][0], GDP[(sd, qs[-1], nm)][1]
        if not (g0 > 0 and g1 > 0 and d0 > 0 and d1 > 0): continue
        led = [sum(GDP[(sd, q, nm)][2][k] for q in qs[1:]) for k in range(25)]
        dm = (d1 - d0) / math.log(d1 / d0) if abs(d1 - d0) > 1e-9 else d0
        dl = math.log(d1 / d0); gl = math.log(g1 / g0)
        bo = led[3] / dm; rp = led[17] / dm
        cols['x'].append(dl - gl); cols['g'].append(-gl); cols['b'].append(bo); cols['r'].append(rp); cols['o'].append(dl - bo - rp)
    if not cols['x']: continue
    print(f'{a}-{b} | {med(cols["x"]):+.3f} | {med(cols["g"]):+.3f} | {med(cols["b"]):+.3f} | {med(cols["r"]):+.3f} | {med(cols["o"]):+.3f}')
