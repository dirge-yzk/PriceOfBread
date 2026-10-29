"""(647) 線 fsc2 段0：関税の選好 Pref(関税, ブロック) の各項が何の関数かを実測する読み手。読むだけ。
入力＝計器 tarpref_s<seed>.tsv を含む計器つきの生の 1 フォルダ（挙げた国は毎四半期の行）。使い方：python -I an_tarpref.py <ins フォルダ> [国名,...]
出力（種をまとめた行の中央値）：
 [A] 今の式：議席の枝の割合・ブロックごとの Pref の中央値・負の割合・連合の一致度（議会の枝は議席・それ以外は政治力で加重）の中央値と負の割合
 [B] 項の大きさ（中央値）：地代の項 rentTerm0..2・近代の競争力の項・輸出利害の項・国家の歳入要求の項・大衆の −1
 [C] 旧 fsc の形（地代の項×地代で加重した楔なしの競争力）の一致度：負の割合（今の式と並べる）
 [D] 候補の統一形：稼ぎの割合・保護の得を受ける割合 expo・消費財の輸入÷GDP・輸出利害の素 xShare・釣り合い点 T*・実効の関税 T の分布と、T の上での一致度の符号と (T*−T) の符号の一致
 [E] 局所の安定：書き手が T+0.25×一致度 のとき、釣り合い点のまわりで 1 歩の収縮率が 1 以上（振動か発散）になる行の割合"""
import csv, sys, glob, re, os, io, statistics as st
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline=None)
csv.field_size_limit(1 << 30)
d = sys.argv[1]
NAT = ['フランス', 'オーストリア', '日本', 'イギリス', 'アメリカ', 'プロイセン', 'ロシア', 'オスマン', '清']
if len(sys.argv) > 2: NAT = sys.argv[2].split(',')
STEP = 0.25   # TariffReactionMax（係数の既定）

def fl(x):
    try: return float(x)
    except: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
def frac(xs, f):
    xs = [x for x in xs if x == x]
    return sum(1 for x in xs if f(x)) / len(xs) if xs else float('nan')
def q(xs, p):
    xs = sorted(x for x in xs if x == x)
    return xs[min(len(xs) - 1, int(p * len(xs)))] if xs else float('nan')
dec = lambda y: (int(y) // 10) * 10

rows = []   # dict per row
for ff in sorted(glob.glob(os.path.join(d, 'tarpref_s*.tsv'))):
    with open(ff, 'rb') as fh:
        rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd)
        ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            if len(r) != len(h): continue
            if r[ix['nation']] not in NAT: continue
            o = {n: (fl(r[i]) if n != 'nation' else r[i]) for n, i in ix.items()}
            rows.append(o)
print(f'# 入力 {d}  行数 {len(rows)}（挙げた国 {len(NAT)}）')

def agree_uni(o, T):
    """候補の統一形の一致度を実効の関税 T で評価（計器の列から組み直す＝計器の agreeUni と別の経路）"""
    wt = [o['seats0'], o['seats1'], o['seats2']] if o['parl'] == 1 else [o['power0'], o['power1'], o['power2']]
    expW = 3.0 * min(1.0, max(0.0, o['xShare'])) if o['xShare'] == o['xShare'] else 0.0
    pref = []
    for b in range(3):
        g = o[f'share{b}'] * o[f'expo{b}'] + o['stateTerm']
        k = o[f'share{b}'] * o['mImpConsumer'] + (expW if b == 1 else 0.0)
        pref.append(g - k * T)
    num = sum(wt[b] * pref[b] for b in range(3)); den = sum(wt[b] * abs(pref[b]) for b in range(3))
    return num / den if den > 1e-9 else 0.0

def section(title, fn, key=lambda o: dec(o['year'])):
    print('\n' + title)
    for n in NAT:
        line = []
        for dd in range(1780, 1950, 20):
            sub = [o for o in rows if o['nation'] == n and dd <= o['year'] < dd + 20]
            if sub: line.append(f'{dd}:{fn(sub)}')
        print(n, ' '.join(line))

def A(sub):
    p = [med([o[f'prefMain{b}'] for o in sub]) for b in range(3)]
    ag = [o['agreeMainSeats'] if o['parl'] == 1 else o['agreeMainPower'] for o in sub]
    return f"議会{frac([o['parl'] for o in sub], lambda x: x == 1):.2f} P{p[0]:+.2f}/{p[1]:+.2f}/{p[2]:+.2f} 一致度{med(ag):+.2f}(負{frac(ag, lambda x: x < 0):.2f})"
section('[A] 今の式：議会の枝の割合・Pref の中央値(旧/近代/大衆)・一致度の中央値と負の割合（20 年ごと）', A)

def B(sub):
    return ' '.join(f"{k}{med([o[k] for o in sub]):+.2f}" for k in ['rentTerm0', 'rentTerm1', 'rentTerm2', 'tiTermMod', 'expTermMod', 'stateTerm'])
section('[B] 項の大きさ（中央値・大衆の −1 は固定）', B)

def C(sub):
    a1 = [o['agreeMainSeats'] if o['parl'] == 1 else o['agreeMainPower'] for o in sub]
    a2 = [o['agreeFscSeats'] if o['parl'] == 1 else o['agreeFscPower'] for o in sub]
    return f"今{med(a1):+.2f}(負{frac(a1, lambda x: x < 0):.2f}) 旧fsc{med(a2):+.2f}(負{frac(a2, lambda x: x < 0):.2f}) rentTi{med([o['rentTi0'] for o in sub]):+.2f}"
section('[C] 旧 fsc の形の一致度（負の割合）と地代で加重した楔なしの競争力 rentTi0', C)

def Dd(sub):
    ts = [(o['tStarSeats'] if o['parl'] == 1 else o['tStarPower']) for o in sub]
    return (f"share{med([o['share0'] for o in sub]):.2f}/{med([o['share1'] for o in sub]):.2f}/{med([o['share2'] for o in sub]):.2f} "
            f"expo{med([o['expo0'] for o in sub]):.2f}/{med([o['expo1'] for o in sub]):.2f}/{med([o['expo2'] for o in sub]):.2f} "
            f"mCons{med([o['mImpConsumer'] for o in sub]):.3f} x{med([o['xShare'] for o in sub]):.2f} T*{med(ts):+.2f} T{med([o['tariffEff'] for o in sub]):+.2f}")
section('[D] 統一形：稼ぎの割合・保護の得を受ける割合・消費財の輸入÷GDP・輸出利害の素・釣り合い点 T*・実効の関税 T（中央値）', Dd)

allts = [(o['tStarSeats'] if o['parl'] == 1 else o['tStarPower']) for o in rows]
print('\n[D2] T* の分布（全行）：', end='')
nn = sum(1 for x in allts if x != x)
print(f"nan {nn}/{len(allts)}  T*<0 {frac(allts, lambda x: x < 0):.3f}  0≤T*≤1 {frac(allts, lambda x: 0 <= x <= 1):.3f}  T*>1 {frac(allts, lambda x: x > 1):.3f}  T*>3 {frac(allts, lambda x: x > 3):.3f}")
print('    分位 10/25/50/75/90：', ' '.join(f'{q(allts, p):+.3f}' for p in [0.1, 0.25, 0.5, 0.75, 0.9]))
ts_ = [o['tariffEff'] for o in rows]
print('    実効の関税 T の分布 10/25/50/75/90：', ' '.join(f'{q(ts_, p):+.3f}' for p in [0.1, 0.25, 0.5, 0.75, 0.9]), f' T<0 {frac(ts_, lambda x: x < -1e-9):.3f}  T≥0.9 {frac(ts_, lambda x: x >= 0.9):.3f}')
ok = [(o, (o['tStarSeats'] if o['parl'] == 1 else o['tStarPower'])) for o in rows]
ok = [(o, t) for o, t in ok if t == t and abs(t - o['tariffEff']) > 1e-6]
sg = sum(1 for o, t in ok if (agree_uni(o, o['tariffEff']) > 0) == (t > o['tariffEff']))
print(f'    一致度の符号＝(T*−T) の符号：{sg}/{len(ok)} = {sg / len(ok) if ok else float("nan"):.4f}（統一形の内部の整合＝ほぼ 1 のはず）')
cm = [(o['agreeUniSeats'] if o['parl'] == 1 else o['agreeUniPower']) for o in rows]
cm2 = [agree_uni(o, o['tariffEff']) for o in rows]
print(f'    計器の agreeUni と組み直しの差の最大：{max((abs(a - b) for a, b in zip(cm, cm2) if a == a and b == b), default=float("nan")):.2e}')

# [E] 局所の安定：T* のまわりで T→T+STEP×一致度(T) の 1 歩の収縮率
bad = tot = 0; facs = []
for o, t in ok:
    if not (0 <= t <= 3): continue
    h = 1e-4
    a1, a0 = agree_uni(o, t + h), agree_uni(o, t - h)
    slope = (a1 - a0) / (2 * h)       # dA/dT（負のはず）
    fac = 1 + STEP * slope            # 1 歩の写像 T→T+STEP×A の導関数
    facs.append(fac); tot += 1
    if abs(fac) >= 1: bad += 1
print(f'\n[E] 局所の安定（書き手 T+{STEP}×一致度）：釣り合い点で |1+{STEP}×dA/dT| ≥ 1（収縮しない）の行 {bad}/{tot} = {bad / tot if tot else float("nan"):.3f}  収縮率の分位 10/50/90：{q(facs, .1):+.2f} {q(facs, .5):+.2f} {q(facs, .9):+.2f}')
# 釣り合い点を直接の目標にする書き手（目標＝T*）なら収縮率は 0（1 歩で着く）＝上の写像が収縮しない行は T* 目標なら起きない
