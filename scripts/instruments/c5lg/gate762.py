#!/usr/bin/env python3
# (762) c5lg の門の読み手：事前登録 measurements/m762_20261102_prereg_lg.txt の S1〜S6（静的）と P1〜P3（動的）を、計器の表 landfix_g・landfix_s から読む。読むだけ。
#   python -I scripts/instruments/c5lg/gate762.py --A <旗 0 の腕の ins> --B <旗 1 の腕の ins> [--S11 <種 11〜20 の腕の ins>] [--paper measurements/m762_20261102_paper_lg_out.txt]
#       [--routedump measurements/hol744_20261101/routedump_344.tsv.gz] [--mut <A|B を差し替える使い捨ての木の ins>（突然変異の確かめ用・門の判定は同じ）]
#   旧＝旗 0 の腕（A）の landfix_g・新＝旗 1 の腕（B）の landfix_g。陸グラフは静的で軌道に依らない＝種の間でバイト一致することも確かめる。
#   終了コード：0＝全ての門が通る・1＝落ちた門がある。
import argparse, glob, gzip, math, os, re, statistics, sys
sys.stdout.reconfigure(encoding='utf-8')   # python -I は PYTHONUTF8 を無視する＝cp932 の端末でも日本語を落とさない

ap = argparse.ArgumentParser()
ap.add_argument('--A', required=True)
ap.add_argument('--B', required=True)
ap.add_argument('--S11', default='')
ap.add_argument('--paper', default='measurements/m762_20261102_paper_lg_out.txt')
ap.add_argument('--routedump', default='measurements/hol744_20261101/routedump_344.tsv.gz')
a = ap.parse_args()

SIX = [('パドヴァ', 'ヴェネツィア'), ('トビリシ', 'ロストフ'), ('トゥールーズ', 'バルセロナ'), ('クラウゼンブルク', 'ブカレスト'), ('ソフィア', 'ブカレスト'), ('バクー', 'ロストフ')]
SAME13 = [('ジブラルタル', 'タンジェ'), ('ワルシャワ', 'ヨーテボリ'), ('ドレスデン', 'ストックホルム'), ('ボローニャ', 'オラン'), ('シンゲッティ', 'ジブラルタル'),
          ('バローダ', 'マスカット'), ('ゴンダール', 'モカ'), ('ムルズク', 'バルセロナ'), ('ヘルシングフォシュ', 'リガ'), ('ラパス', 'ペルー'), ('チリ', 'プンタアレナス'),
          ('ウジジ', 'キサンガニ'), ('オムスク', 'グリエフ')]
RES = []


def gate(name, ok, detail):
    RES.append((name, ok))
    print(f'{"通る" if ok else "落ちる"}  {name}  {detail}')


def key(x, y): return (x, y) if x <= y else (y, x)


def num(s):
    return float(s.replace('Infinity', 'inf').replace('NaN', 'nan'))


def load_g(path):
    H = {}; E = {}; P = {}
    with open(path, encoding='utf-8') as f:
        next(f)
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if c[0] == 'H': H[c[1]] = c[2]
            elif c[0] == 'E': E[key(c[1], c[2])] = c[4]
            elif c[0] == 'P': P[(c[1], c[2])] = (c[3], c[4])
    return H, E, P


def seeds_of(d, kind):
    return sorted(glob.glob(os.path.join(d, f'{kind}_s*.tsv')), key=lambda p: int(re.search(r'_s(\d+)\.tsv$', p).group(1)))


def load_static(d, label):
    fs = seeds_of(d, 'landfix_g')
    assert fs, f'{label}: landfix_g が無い（{d}）'
    raw0 = open(fs[0], 'rb').read()
    same = all(open(p, 'rb').read() == raw0 for p in fs[1:])
    gate(f'静的 {label} の landfix_g が種の間でバイト一致', same, f'{len(fs)} 種')
    return load_g(fs[0])


def pair(P, x, y): return P[(x, y)] if (x, y) in P else P[(y, x)]


HA, EA, PA = load_static(a.A, 'A（旗 0）')
HB, EB, PB = load_static(a.B, 'B（旗 1）')
print(f'見出し A: {HA}\n見出し B: {HB}')

# 紙の台本の出力から 38 対と辺の一覧
paper = open(a.paper, encoding='utf-8').read().split('\n')
LONGER = {key(c[1], c[2]) for c in (l.split('\t') for l in paper if l.startswith('  LONGER\t'))}
def edges_line(prefix):
    ln = [l for l in paper if l.startswith(prefix)][0][len(prefix):].strip()
    out = set()
    for it in ln.split(' / '):
        nm = it.rsplit(' ', 1)[0]
        x, y = nm.split('–')
        out.add(key(x, y))
    return out
GONE = edges_line('消える辺:'); NEW = edges_line('増える辺:')
assert len(LONGER) == 38 and len(GONE) == 4 and len(NEW) == 18, (len(LONGER), len(GONE), len(NEW))

# --- S1 下限
ok = True; det = []
for x, y in SIX:
    gc = num(pair(PB, x, y)[0]); lo = num(pair(PA, x, y)[1]); ln = num(pair(PB, x, y)[1])
    r0, r1 = lo / gc, ln / gc
    ok &= (r0 > 3 and r1 <= 2.0); det.append(f'{x}–{y} {r0:.2f}→{r1:.2f}')
gate('S1 下限（名指しの 6 対が 旧＞3 かつ 新≦2.0）', ok, ' / '.join(det))
# --- S2 件数
def gt3(P):
    return sum(1 for (g, l) in P.values() if num(g) > 0 and math.isfinite(num(l)) and num(l) / num(g) > 3)
gate('S2 件数（陸路÷大圏＞3 の対 603）', gt3(PB) == 603, f'A={gt3(PA)}（旗 0 で 616 のはず） B={gt3(PB)}')
gate('S2′ 旗 0 の腕は 616', gt3(PA) == 616, f'A={gt3(PA)}')
# --- S3 上限（押し出し）
longer = {};
assert set(PA) == set(PB)
for k, (g, lo) in PA.items():
    lo_ = num(lo); ln_ = num(PB[k][1])
    if (math.isinf(lo_) and math.isinf(ln_)) or lo_ == ln_: continue
    if ln_ > lo_ or (math.isfinite(lo_) and math.isinf(ln_)): longer[key(*k)] = ln_ / lo_ if lo_ > 0 else float('inf')
extra = sorted(set(longer) - LONGER)
mx = max(longer.values()) if longer else 0
gate('S3 上限（長くなる対は台本の 38 対の部分集合・最大 ≦1.29）', not extra and mx <= 1.29, f'長くなる対 {len(longer)}（台本の外 {len(extra)}）最大 ×{mx:.4f}')
# --- S4 辺
gone = set(EA) - set(EB); new = set(EB) - set(EA)
comm_diff = [k for k in set(EA) & set(EB) if EA[k] != EB[k]]
gate('S4 上限（辺：消える 4・増える 18・共通の重みはビット一致）', gone == GONE and new == NEW and not comm_diff,
     f'消える {len(gone)}（台本と同じ={gone == GONE}）増える {len(new)}（同じ={new == NEW}）共通の重みの差 {len(comm_diff)}')
# --- S5 直さない対
bad = [f'{x}–{y}' for x, y in SAME13 if pair(PA, x, y)[1] != pair(PB, x, y)[1]]
gate('S5 上限（直さない 13 対がビット一致）', not bad, '差のある対: ' + (', '.join(bad) if bad else 'なし'))
# --- S6 恒等（旗 0 の landfix_g が routedump の land と一致）
maxrel = 0.0; ncmp = 0
with gzip.open(a.routedump, 'rt', encoding='utf-8') as f:
    next(f)
    for ln in f:
        c = ln.rstrip('\n').split('\t')
        k = (c[0], c[1]) if (c[0], c[1]) in PA else (c[1], c[0]) if (c[1], c[0]) in PA else None
        if k is None: continue
        v = num(PA[k][1]); w = num(c[5])
        if math.isinf(v) and math.isinf(w): continue
        d = abs(v - w) / max(abs(w), 1e-12) if math.isfinite(v) and math.isfinite(w) else float('inf')
        maxrel = max(maxrel, d); ncmp += 1
gate('S6 恒等（旗 0 の landfix_g の landKm が routedump の land と相対 1e-9）', maxrel <= 1e-9 and HA.get('edges') == '1070', f'比べた対 {ncmp} 最大の相対差 {maxrel:.3g} 辺 {HA.get("edges")}')


# --- 動的
def load_s(d):
    out = {}
    for p in seeds_of(d, 'landfix_s'):
        seed = int(re.search(r'_s(\d+)\.tsv$', p).group(1)); rows = []
        with open(p, encoding='utf-8') as f:
            next(f)
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                rows.append((int(c[0]), c[1], c[2], c[3], num(c[4]), num(c[5]), num(c[6]), num(c[7])))
        out[seed] = rows
    return out


def dynamic(d, label, strict):
    S = load_s(d)
    if not S:
        gate(f'{label} の landfix_s が無い', False, d); return
    # P1
    nbad = 0; ntot = 0; nmissing = 0
    for seed, rows in S.items():
        for (yr, nat, reg, port, gc, used, cur, ratio) in rows:
            ntot += 1
            if math.isfinite(cur) and abs(used - cur) > 1e-9 * max(1.0, abs(cur)): nbad += 1
    gate(f'P1 配線だけ（{label}）', nbad == 0, f'行 {ntot}・不一致 {nbad}')
    # P2 / P3
    per_seed = {}; p3bad = []; p3max = 0.0; gt3o = {}; gt3n = {}; nrows = 0
    for seed, rows in S.items():
        bysum = {}; byn = {}
        for (yr, nat, reg, port, gc, used, cur, ratio) in rows:
            k = (reg, port) if (reg, port) in PA else (port, reg) if (port, reg) in PA else None
            if k is None: nmissing += 1; continue
            old = num(PA[k][1])
            if not (old > 0 and math.isfinite(old) and math.isfinite(cur)): continue
            nrows += 1
            idx = cur / old
            bysum[yr] = bysum.get(yr, 0.0) + (1 - idx)
            byn[yr] = byn.get(yr, 0) + 1
            gt3o.setdefault(yr, 0); gt3n.setdefault(yr, 0)
            if gc > 0 and old / gc > 3: gt3o[yr] += 1
            if gc > 0 and cur / gc > 3: gt3n[yr] += 1
            if idx > 1 + 1e-9:
                p3max = max(p3max, idx)
                if key(reg, port) not in LONGER: p3bad.append((seed, yr, reg, port, idx))
        per_seed[seed] = sum(bysum.values()) / len(bysum) if bysum else 0.0
    med = statistics.median(per_seed.values()) if per_seed else 0.0
    gate(f'P2 大きさ（{label}：種ごとの年あたり Σ(1−新÷旧) の種の中央値 ≧0.50）', med >= 0.50 if strict else True,
         f'中央値 {med:.3f}（種 {len(per_seed)}・{"採否に使う" if strict else "記録"}）種ごと: ' + ' '.join(f'{s}:{v:.3f}' for s, v in sorted(per_seed.items())))
    gate(f'P3 上限（{label}：新÷旧＞1+1e-9 の (州,港) は 38 対に入り最大 ≦1.29）', not p3bad and p3max <= 1.29, f'違反 {len(p3bad)}・最大 ×{p3max:.4f}' + (f'・例 {p3bad[:3]}' if p3bad else ''))
    print(f'  記録（{label}）：読めなかった (州,港) 行 {nmissing}・比べた行 {nrows}・陸路÷大圏＞3 の行（年ごとの平均）旧 {statistics.mean(gt3o.values()) if gt3o else 0:.2f}→新 {statistics.mean(gt3n.values()) if gt3n else 0:.2f}')


dynamic(a.B, 'B（種 1〜10）', True)
if a.S11: dynamic(a.S11, 'S11（種 11〜20）', False)
bad = [n for n, ok in RES if not ok]
print('\n判定:', '全ての門が通る' if not bad else f'落ちた門 {len(bad)} 本: ' + ' / '.join(bad))
sys.exit(0 if not bad else 1)
