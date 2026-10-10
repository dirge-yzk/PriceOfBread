"""(181) 戦後の施行・不履行・戦時段の請求の払われ方・国庫と潤沢線の読み手（読むだけ）。
  G 行（w176＝inj_war181・施行の後の値は失効と Move の後）・F 行と D 行（f181＝inj_fin181）・hist の A 行（年GDP）。
python an_r181.py <out_dir> [ラベル]"""
import sys, os, glob, statistics as st, collections
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本', '清']
ERAS = [(1784, 1849), (1850, 1913), (1914, 1950)]
HIGH, S4 = 0.75, 0.875


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def era(y):
    return next((f'{a}-{b}' for a, b in ERAS if a <= y <= b), None)


def med(x):
    x = [y for y in x if y == y]
    return st.median(x) if x else float('nan')


def pct(x, p):
    x = sorted(y for y in x if y == y)
    if not x: return float('nan')
    return x[min(len(x) - 1, int(p * (len(x) - 1) + 0.5))]


seeds = sorted(int(os.path.basename(f)[1:-4]) for f in glob.glob(os.path.join(D, 'w176', 'g*.txt')))
print(f'######## {LAB}（種 {len(seeds)}）')
# ---------- ① 施行（G 行）----------
peace = collections.Counter(); peace4 = collections.Counter(); peace3 = collections.Counter()
peaceMaj = collections.Counter(); peace4Maj = collections.Counter()
ev = []   # (era, quarters-to-return or None, x0 at fall, name)
for sd in seeds:
    seq = collections.defaultdict(list)
    for ln in open(os.path.join(D, 'w176', f'g{sd}.txt'), encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'G': continue
        v = [fl(x) for x in t[5:]]
        if not (v[0] > 0): continue
        q, yr, idx, nm = int(t[1]), int(t[2]), int(t[3]), t[4]
        unl = v[1] > 0 and v[2] > 0
        seq[idx].append((q, yr, nm, v[1] > 0, unl, v[3], v[6]))
        e = era(yr)
        if e and not v[1] > 0:
            peace[e] += 1
            if v[6] >= S4 - 1e-9: peace4[e] += 1
            if v[6] > HIGH + 1e-9: peace3[e] += 1
            if nm in MAJ:
                peaceMaj[(nm, e)] += 1
                if v[6] >= S4 - 1e-9: peace4Maj[(nm, e)] += 1
    for idx, rows in seq.items():
        rows.sort()
        for k in range(1, len(rows)):
            q, yr, nm, war, unl, x0, x1 = rows[k]
            if not (rows[k - 1][4] and not unl and rows[k - 1][0] == q - 1): continue
            if not (x0 > HIGH + 1e-9): continue
            n = None
            for j in range(k, len(rows)):
                if rows[j][4] or (j > k and rows[j][0] != rows[j - 1][0] + 1): break
                if rows[j][6] <= HIGH + 1e-9: n = j - k; break
            ev.append((era(yr), n, x0, nm, yr, sd))
print('① 平時（交戦していない国×四半期）の施行＝段4 の水準（x≧0.875）・段3 の上（x＞0.75）の割合')
for a, b in ERAS:
    e = f'{a}-{b}'
    print(f'  {e} | 平時 {peace[e]} | 段4 の水準 {peace4[e] / max(1, peace[e]):.3f} | 段3 の上 {peace3[e] / max(1, peace[e]):.3f}')
print('  主要国（平時の段4 の水準の割合・時代ごと）')
for nm in MAJ:
    print('   ' + nm + ' | ' + ' | '.join(f'{a}-{b} {peace4Maj[(nm, f"{a}-{b}")] / max(1, peaceMaj[(nm, f"{a}-{b}")]):.3f}（{peaceMaj[(nm, f"{a}-{b}")]}）' for a, b in ERAS))
print('② 解禁（交戦かつ総動員）が落ちた四半期（施行が段3 の上）→ 施行が段3 以下に戻るまでの四半期（0＝その四半期の頭）')
for a, b in ERAS:
    e = f'{a}-{b}'
    xs = [x for x in ev if x[0] == e]
    done = [x[1] for x in xs if x[1] is not None]
    cens = sum(1 for x in xs if x[1] is None)
    print(f'  {e} | 件数 {len(xs)} | 戻った {len(done)}・戻る前に再び解禁か終局 {cens} | 中央 {med(done):.1f}・p75 {pct(done, .75):.0f}・p90 {pct(done, .9):.0f}・最大 {max(done) if done else float("nan")} | 落ちた時の施行の中央 {med([x[2] for x in xs]):.3f}')
bx = [x for x in ev if x[3] == 'イギリス']
print(f'  英 件数 {len(bx)}・戻るまでの中央 {med([x[1] for x in bx if x[1] is not None]):.1f}・戻らない {sum(1 for x in bx if x[1] is None)}')
# ---------- GDP ----------
A = {}
for f in glob.glob(os.path.join(D, 'hist', 'a*.txt')):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == 'A': A[(sd, int(t[1]), int(t[3]))] = fl(t[6])
# ---------- ③ 不履行（D 行）----------
print('③ 不履行（Fiscal のソブリン・デフォルト＝D 行）')
Dall = collections.Counter(); Dmaj = collections.defaultdict(list); Fq = {}
Wsum = collections.defaultdict(lambda: [0.0] * 8)
TR = collections.defaultdict(list)
for sd in seeds:
    p = os.path.join(D, 'f181', f'f{sd}.txt')
    if not os.path.exists(p): continue
    for ln in open(p, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == 'D':
            q, idx, yr, nm = int(t[1]), int(t[2]), int(t[3]), t[4]
            v = [fl(x) for x in t[5:]]
            Dall[sd] += 1
            if nm in MAJ: Dmaj[nm].append((sd, yr, v))
        elif t[0] == 'F':
            q, yr, idx, nm = int(t[1]), int(t[2]), int(t[3]), t[4]
            v = [fl(x) for x in t[5:]]
            e = era(yr)
            if e is None or not (v[0] > 0): continue
            # ④ 戦時段の請求の払われ方
            if v[22] > 0:
                s = Wsum[e]
                for k in range(6): s[k] += v[15 + k]
                s[6] += v[21]; s[7] += v[22]
            # ⑤ 国庫と潤沢線
            if nm in MAJ:
                gdp = A.get((sd, q, idx), float('nan'))
                m = v[0]
                T, R, line, spend = v[3] / m, v[4] / m, v[23] / m, v[24] / m
                TR[(nm, e)].append((T / gdp if gdp > 0 else float('nan'), line / gdp if gdp > 0 else float('nan'),
                                    T / line if line > 0 else float('nan'), (spend - R) / R if R > 0 else float('nan'), T / R if R > 0 else float('nan')))
print(f'  全ての国 {sum(Dall.values())} 件（種あたり {sum(Dall.values()) / max(1, len(seeds)):.1f}）')
for nm in MAJ:
    xs = Dmaj.get(nm, [])
    print(f'  {nm}: {len(xs)} 件（種 {len(set(x[0] for x in xs))}）　' + '・'.join(f'種{s} {y}' for s, y, _ in sorted(xs)))
print('  英の不履行の盤面（国債÷年GDP・歳入×12÷年GDP・利払い÷歳入・金利・クーポン・国庫÷年GDP・施行・交戦・総動員・兌換・張り付きの月｜窓の平均＝請求÷払える額・借入÷請求・切り下げ÷請求・r・利払い÷請求・植民地÷請求・福祉÷請求・軍÷請求・行政÷請求）')
for sd, yr, v in sorted(Dmaj.get('イギリス', [])):
    debt, ag, rev, it, br, cp, tr, ms, aw, mb, cv, nm_ = v[:12]
    c, pay, bo, dv, r, i2, col, wel, mil, adm, tr2, rv2 = v[12:24]
    print(f'   種{sd} {yr} | {debt / ag:.2f} | {rev * 12 / ag:.3f} | {it / rev if rev > 0 else float("nan"):.2f} | {br:.3f} | {cp:.3f} | {tr / ag:.3f} | {ms:.2f} | {int(aw)} | {int(mb)} | {cv:.2f} | {int(nm_)}'
          f' ｜ {c / pay if pay > 0 else float("nan"):.2f} | {bo / c:.3f} | {dv / c:.3f} | {r:.3f} | {i2 / c:.2f} | {col / c:.2f} | {wel / c:.2f} | {mil / c:.2f} | {adm / c:.2f}')
print('④ 戦時段の請求 W（限界の見方＝他の行を先に置いた残り）の払われ方（月の和の比）・重なり＝W のある月のうち他の行だけで払える額を超えた月')
for a, b in ERAS:
    s = Wsum.get(f'{a}-{b}')
    if not s or not (s[0] > 0): print(f'  {a}-{b} | W なし'); continue
    W = s[0]
    print(f'  {a}-{b} | W のある国×月 {int(s[7])} | 歳入の余り {s[1] / W:.3f} | 国庫の蓄え {s[2] / W:.3f} | 借入 {s[3] / W:.3f} | 切り下げ {s[4] / W:.3f} | 払えない（r<1）{s[5] / W:.3f} | 重なりの月 {s[6] / max(1, s[7]):.3f}')
print('⑤ 国庫と潤沢線（主要国×時代・国×四半期の中央）＝国庫÷年GDP・潤沢線÷年GDP・国庫÷潤沢線・使える歳入の上乗せ÷歳入・国庫÷月の歳入')
for nm in MAJ:
    for a, b in ERAS:
        rs = TR.get((nm, f'{a}-{b}'))
        if not rs: continue
        print(f'  {nm} | {a}-{b} | {len(rs)} | {med([r[0] for r in rs]):.3f} | {med([r[1] for r in rs]):.3f} | {med([r[2] for r in rs]):.2f} | {med([r[3] for r in rs]):.3f} | {med([r[4] for r in rs]):.1f}')
