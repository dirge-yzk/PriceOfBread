"""(449) 国力の物差しの点火の読み手（読むだけ・事前登録 measurements/sp_20261020_prereg.txt [6] の G2・G3・G5・R1 を機械的に出す）。
python an_sp449.py <計器の出力ディレクトリ> [--mech=max|median] [--tag=名前]
  入力＝標準の計器（INS=1）の score_s<seed>.tsv（国×年末＋最初の四半期の末 q=0）。
  列が無い入力（m443_ins＝rankInForce・usedTerms・treatyStrength・foreignShare が無い）は、その節を〈列なし〉と名乗って飛ばすか近似で出す（近似は近似と書く）。
  G2：q=0 の行の rankInForce==Great の集合と q=0 の isGreatShare==1 の集合の重なり／1784 年末〔q=3〕の isGreatShare との重なり／
      q=0 の rankInForce と q=0 の rankShare から定員制で引いた階級との不一致の数（定員 7・5・7）／q=0 → q=3 の rankInForce の変わり（記録）／usedTerms（記録）
  G3：ハワイ王国・エチオピアが rankShare 10 以内の年×種の割合（rankOld でも）
  G5：条約港の風化の係数 Clamp01(比÷0.5)。比＝treatyStrength÷基準。基準は rankInForce==Great の treatyStrength の最大〔max〕か中央値〔median・上側の中央＝添字 n÷2〕。
      foreignShare>0 の国の平均（機構が働く場所）と全国の平均。--mech で今の木の機構がどちらの基準かを名乗る（旗 0＝max・旗 1＝median）。
  R1：清・英・米・日・露・普の rankShare の中央値（最小〜最大）"""
import sys, os, glob, statistics


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for line in fh:
            v = line.rstrip('\n').split('\t')
            if len(v) == len(head):
                yield dict(zip(head, v))


args = [a for a in sys.argv[1:] if not a.startswith('--')]
opts = dict(a[2:].split('=', 1) for a in sys.argv[1:] if a.startswith('--') and '=' in a)
D = args[0]
MECH = opts.get('mech', 'max')
TAG = opts.get('tag', '')
CAP = (7, 5, 7)
files = sorted(glob.glob(os.path.join(D, 'score_s*.tsv')), key=lambda p: int(os.path.basename(p)[7:-4]))
rows = {}
for p in files:
    rows[os.path.basename(p)[7:-4]] = list(table(p))
print(f'== (449) 読み手 tag={TAG} dir={D} score_s={len(files)} 本 mech={MECH}')
cols = set(rows[next(iter(rows))][0].keys()) if rows else set()
has_force = 'rankInForce' in cols
has_treaty = 'treatyStrength' in cols
has_fs = 'foreignShare' in cols
print(f'   列：rankInForce={has_force} usedTerms={"usedTerms" in cols} treatyStrength={has_treaty} foreignShare={has_fs}')


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def f(x, nd=1):
    return 'NA' if x != x else f'{x:.{nd}f}'


def base(n):
    for pre in ('共和制', '立憲王政'):
        if n.startswith(pre):
            return n[len(pre):]
    return n


def yearend(rs, y):
    return [r for r in rs if int(r['year']) == y and int(r['quarter']) % 4 == 3]


def q0(rs):
    return [r for r in rs if int(r['quarter']) == 0]


# ---- G2 ----
print('\n[G2] 開幕のランク')
if has_force:
    ov0, ov1, mism, ch, used0 = [], [], [], [], []
    for sd, rs in rows.items():
        a = q0(rs)
        if not a:
            continue
        if 'usedTerms' in cols:
            used0.append(int(a[0]['usedTerms']))
        forceG = {r['nation'] for r in a if r['rankInForce'] == 'Great'}
        shareG = {r['nation'] for r in a if r['isGreatShare'] == '1'}
        ov0.append(len(forceG & shareG))
        b = yearend(rs, 1784)
        shareG3 = {r['nation'] for r in b if r['isGreatShare'] == '1'}
        ov1.append(len(forceG & shareG3))
        srt = sorted(a, key=lambda r: int(r['rankShare']))
        cls = {}
        for i, r in enumerate(srt):
            cls[r['nation']] = 'Great' if i < CAP[0] else 'Secondary' if i < CAP[0] + CAP[1] else 'Middle' if i < sum(CAP) else 'Minor'
        mism.append(sum(1 for r in a if r['rankInForce'] in ('Great', 'Secondary', 'Middle', 'Minor') and r['rankInForce'] != cls[r['nation']]))
        fb = {r['nation']: r['rankInForce'] for r in b}
        ch.append(sum(1 for r in a if r['nation'] in fb and r['rankInForce'] != fb[r['nation']]))
    print(f'   q=0 の rankInForce==Great ∩ q=0 の isGreatShare: 種ごと {ov0}（7/7 が {sum(1 for x in ov0 if x >= 7)} 試合・6/7 以上が {sum(1 for x in ov0 if x >= 6)} 試合／{len(ov0)}）')
    print(f'   q=0 の rankInForce==Great ∩ 1784 年末の isGreatShare: 種ごと {ov1}（7/7 が {sum(1 for x in ov1 if x >= 7)} 試合・6/7 以上が {sum(1 for x in ov1 if x >= 6)} 試合）')
    print(f'   q=0 の rankInForce と rankShare の定員制の階級の不一致（国の数）: 種ごと {mism}・中央値 {f(med(mism))}（取り直しなしの probe は約 10）')
    print(f'   [記録] q=0 → 1784 年末の rankInForce の変わり: 種ごと {ch}・中央値 {f(med(ch))}')
    print(f'   [記録] q=0 の usedTerms: {used0}（自明に 6 になる＝合否に使わない）')
else:
    print('   列なし（rankInForce）＝この入力では出せない')

# ---- G3 ----
print('\n[G3] 上位 10 に入る年×種の割合（年末の行）')
for nm in ('ハワイ王国', 'エチオピア'):
    for col in ('rankShare', 'rankOld'):
        n = k = 0
        seeds_hit = set()
        for sd, rs in rows.items():
            for r in rs:
                if r['nation'] == nm and int(r['quarter']) % 4 == 3:
                    n += 1
                    if int(r[col]) <= 10:
                        k += 1
                        seeds_hit.add(sd)
        print(f'   {nm} {col}: {k}/{n} = {100 * k / max(n, 1):.2f}%（{len(seeds_hit)} 試合）')

# ---- G5 ----
print(f'\n[G5] 条約港の風化の係数 Clamp01(比÷0.5)（今の機構の基準＝{MECH}）')
STR = 'treatyStrength' if has_treaty else 'scoreOld'
if not has_treaty:
    print('   ⚠treatyStrength の列が無い＝近似（scoreOld＝4 項の和・列強は isGreatOld）。旗 0 の機構の 2 項とは違う')
GY = [1784, 1800, 1820, 1850, 1880, 1900, 1914, 1930, 1950]
for y in GY:
    per_h = {'max': [], 'median': []}
    per_all = {'max': [], 'median': []}
    nh = 0
    for sd, rs in rows.items():
        a = yearend(rs, y)
        if not a:
            continue
        key_g = (lambda r: r['rankInForce'] == 'Great') if has_force else (lambda r: r['isGreatOld'] == '1')
        gs = sorted(fl(r[STR]) for r in a if key_g(r))
        if not gs:
            continue
        mx, md = gs[-1], gs[len(gs) // 2]
        for key, ref in (('max', mx), ('median', md)):
            if not (ref == ref and ref > 1e-9):
                continue
            cs = [min(1.0, fl(r[STR]) / ref / 0.5) for r in a]
            per_all[key].append(sum(cs) / len(cs))
            if has_fs:
                hs = [min(1.0, fl(r[STR]) / ref / 0.5) for r in a if fl(r['foreignShare']) > 0]
                if hs:
                    per_h[key].append(sum(hs) / len(hs))
        if has_fs:
            nh += sum(1 for r in a if fl(r['foreignShare']) > 0)
    msg = f'   {y}: 全国の平均 max {f(med(per_all["max"]), 3)} median {f(med(per_all["median"]), 3)}'
    if has_fs:
        msg += f'／持分のある国の平均（種の中央値）max {f(med(per_h["max"]), 3)} median {f(med(per_h["median"]), 3)}（持分のある国 のべ {nh}・種 {len(per_h["max"])}）'
    print(msg)

# ---- R1 ----
print('\n[R1] rankShare（年末・種の中央値〈最小〜最大〉）')
for nm in ['清', 'イギリス', 'アメリカ', '日本', 'ロシア', 'プロイセン']:
    out = []
    for y in GY:
        xs = [int(r['rankShare']) for rs in rows.values() for r in yearend(rs, y) if base(r['nation']) == nm]
        out.append(f'{y}:{f(med(xs), 0)}' + (f'({min(xs)}-{max(xs)})' if xs else ''))
    print(f'   {nm:6s} ' + ' '.join(out))
