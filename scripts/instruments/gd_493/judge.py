#!/usr/bin/env python
# (493) 線 gd：腕 B の読み手（事前登録 measurements/gd_20261023_prereg.txt の [5]・追記 1〜3 の判定を disc_s・chem_s・chemgood_s から出す）
# 使い方：python judge.py <腕 B の ins フォルダ> [比べる側の ins フォルダ=D:/PoB_work/keep_491/m491_ins] [種の数=10]
# 読むだけ（標準ライブラリのみ・バイトの読み書きは出力の標準出力だけ）。判定は T2〜T6・M1〜M3・記録 R1〜R6。史実の年・大きさは判定に使わない。
import csv, sys, statistics as st, collections, math

NEW = sys.argv[1]
OLD = sys.argv[2] if len(sys.argv) > 2 else 'D:/PoB_work/keep_491/m491_ins'
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
# (500) 追記 4：種の起点（新しい種 11〜20 の腕を読む）
SB = int(sys.argv[4]) if len(sys.argv) > 4 else 1
SEEDS = range(SB, SB + NS)
HOR = 668
NAT8 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']

def rows(d, name, s):
    p = f'{d}/{name}{s}.tsv'
    try:
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.DictReader(f, delimiter='\t')
            return list(r)
    except FileNotFoundError:
        return None

def fl(x):
    try:
        v = float(x)
        return v
    except Exception:
        return float('nan')

def isn(v):
    return v != v

def mean(xs):
    xs = [x for x in xs if not isn(x)]
    return sum(xs) / len(xs) if xs else float('nan')

def med(xs):
    xs = [x for x in xs if not isn(x)]
    return st.median(xs) if xs else float('nan')

out = []
def P(*a):
    s = ' '.join(str(x) for x in a)
    out.append(s)
    print(s)

def fmt(v, n=4):
    return 'nan' if isn(v) else f'{v:.{n}g}'

# ---- 読み込み
chem, cg, disc = {}, {}, {}
for s in SEEDS:
    chem[s] = rows(NEW, 'chem_s', s)
    cg[s] = rows(NEW, 'chemgood_s', s)
    disc[s] = rows(NEW, 'disc_s', s)
missing = [s for s in SEEDS if chem[s] is None or cg[s] is None or disc[s] is None]
P(f'# (493) 線 gd 読み手 judge.py：NEW={NEW} OLD={OLD} 種={NS}')
if missing:
    P('!! 欠けた表の種：', missing, '（chem_s／chemgood_s／disc_s）')
SEEDS = [s for s in SEEDS if s not in missing]

# 索引：chem[(s)][(row, nation)] -> {q: rec}、cgi[(s)][(good, nation)] -> {q: rec}
chemi, cgi = {}, {}
for s in SEEDS:
    d = collections.defaultdict(dict)
    for r in chem[s]:
        d[(r['row'], r['nation'])][int(r['quarter'])] = r
    chemi[s] = d
    d = collections.defaultdict(dict)
    for r in cg[s]:
        d[(r['good'], r['nation'])][int(r['quarter'])] = r
    cgi[s] = d

# ---- 単位の確かめ（T4 の前提）：chem_s の goodOutputPerMonth〔ΣOutput〕と chemgood_s の outputPerMonth〔ΣLastProduced〕が同じ量か
ratios = []
for s in SEEDS:
    for (row, nat), qd in chemi[s].items():
        if row != 'ルブラン法':
            continue
        g = cgi[s].get(('ソーダ灰', nat), {})
        for q, r in qd.items():
            a = fl(r['goodOutputPerMonth'])
            b = fl(g[q]['outputPerMonth']) if q in g else float('nan')
            if a > 1e-6 and b > 1e-6:
                ratios.append(b / a)
unit_med = med(ratios)
P(f'\n[単位] ソーダ灰：chemgood_s の outputPerMonth ÷ chem_s の goodOutputPerMonth の中央値 {fmt(unit_med)}（n={len(ratios)}）→ ', 'T4 を判定に使う' if 0.8 <= unit_med <= 1.25 else 'T4 は記録に下げる（単位が食い違う）')
T4_VALID = 0.8 <= unit_med <= 1.25

# ---- T2：入口
P('\n[T2] 入口：disc_s の E 行（row＝その行・どの国でも）が 1 件以上ある種の数（≧6 が合格）。⚠既存の発見の時計が満たす（追記 3 ③）＝弱い線')
t2 = {}
firsts = {}
for row in ['ルブラン法', 'ソルベー法', '電解']:
    n = 0
    yrs = []
    for s in SEEDS:
        ev = [(int(r['quarter']), int(r['year']), r['nation']) for r in disc[s] if r['kind'] == 'E' and r['row'] == row]
        if ev:
            n += 1
            ev.sort()
            yrs.append((s, ev[0][1], ev[0][2], len(ev)))
    t2[row] = n
    firsts[row] = yrs
    judged = '（判定）' if row in ('ルブラン法', 'ソルベー法') else '（記録）'
    P(f'  {row}{judged}：開いた種 {n}/{len(SEEDS)}  最初の発見 年・国（種：年 国 件数）', yrs)
T2 = all(t2[r] >= 6 for r in ('ルブラン法', 'ソルベー法'))
P('  T2 =', 'PASS' if T2 else 'FAIL')

# ---- 開いた組（種，国）：chem_s の known の 0→1
def open_q(s, row, nat):
    qd = chemi[s].get((row, nat), {})
    for q in sorted(qd):
        if qd[q]['known'] == '1':
            return q
    return None

def share(s, row, nat, q):
    r = chemi[s].get((row, nat), {}).get(q)
    return fl(r['recipeOutputShare']) if r else float('nan')

# ---- T3：ソルベー法の割合の尾の向き（追記 2・3 ⑥）
def t3(row, seeds_use, label):
    pairs = []
    pre_viol = []
    pre_join = []
    pre_max = 0.0
    for s in seeds_use:
        for nat in NAT8:
            q0 = open_q(s, row, nat)
            if q0 is None:
                continue
            # 開く前
            qd = chemi[s].get((row, nat), {})
            for q in range(0, q0):
                v = share(s, row, nat, q)
                if not isn(v):
                    pre_max = max(pre_max, v)
                    if v > 1e-3:
                        # (500) 追記 4：その四半期に recipeProducersCount が前の四半期より増えた（州が国に入った）組は除く＝別に数える
                        joined = (q - 1) in qd and q in qd and int(qd[q]['recipeProducersCount']) > int(qd[q - 1]['recipeProducersCount'])
                        (pre_join if joined else pre_viol).append((s, nat, q, v))
            if q0 + 24 > HOR or q0 == 0:
                continue
            head = mean([share(s, row, nat, q) for q in range(q0, q0 + 5)])
            tail = mean([share(s, row, nat, q) for q in range(q0 + 16, q0 + 25)])
            if isn(head) or isn(tail):
                continue
            pairs.append((s, nat, q0, head, tail))
    up = sum(1 for p in pairs if p[4] > p[3])
    P(f'  {label}：開いた組 {len(pairs)}  尾が頭より大きい {up}  割合 {fmt(up / len(pairs) if pairs else float("nan"), 3)}  開く前の最大 {fmt(pre_max)}  開く前 >1e-3 の件数（州の加入の四半期を除く） {len(pre_viol)} {pre_viol[:3]}  除いた加入の四半期 {len(pre_join)} {pre_join[:5]}')
    return pairs, pre_viol, up

P('\n[T3] 尾の向き（判定＝ソルベー法の割合・開いた後 16〜24 四半期の平均 ＞ 開いた直後 0〜4 四半期の平均 の組が過半・開く前は全て ≦1e-3）')
p3, v3, up3 = t3('ソルベー法', SEEDS, '判定 ソルベー法（全種）')
T3 = bool(p3) and up3 / len(p3) > 0.5 and not v3
P('  T3 =', 'PASS' if T3 else 'FAIL')
t3('ソルベー法', [s for s in SEEDS if s != 1], '併記 (i) seed 1 を除く 9 種')
for row in ('ルブラン法', '電解'):
    t3(row, SEEDS, f'記録 {row}（全種・ソルベー法との交絡あり）')
# 併記 (ii)：ルブラン法の割合を〈ソルベー法が同じ国で開く前〉の窓だけで読む
pairs = []
for s in SEEDS:
    for nat in NAT8:
        q0 = open_q(s, 'ルブラン法', nat)
        qs = open_q(s, 'ソルベー法', nat)
        if q0 is None or q0 == 0:
            continue
        end = (qs if qs is not None else HOR + 1)
        w0 = list(range(q0, min(q0 + 5, end)))
        w1 = [q for q in range(q0 + 5, end) if q <= HOR]
        if len(w0) < 2 or len(w1) < 4:
            continue
        a = mean([share(s, 'ルブラン法', nat, q) for q in w0])
        b = mean([share(s, 'ルブラン法', nat, q) for q in w1])
        if isn(a) or isn(b):
            continue
        pairs.append((s, nat, q0, a, b))
up = sum(1 for p in pairs if p[4] > p[3])
P(f'  併記 (ii) ルブラン法（ソルベー法が同じ国で開く前の窓だけ・頭＝開いた 0〜4・尾＝5 以降〜ソルベー法の前）：組 {len(pairs)}  尾が大きい {up}')

# ---- T4：2 段の鎖
P('\n[T4] 鎖：ルブラン法の recipeOutputShare ≧ 0.05 の（種，国，四半期）で、同じ国の硫酸（産出＋輸入）≧ 0.25 × ルブラン法の recipeOutputPerMonth の割合 ≧ 0.9')
n4 = ok4 = 0
bad = collections.Counter()
for s in SEEDS:
    for nat in NAT8:
        qd = chemi[s].get(('ルブラン法', nat), {})
        g = cgi[s].get(('硫酸', nat), {})
        for q, r in qd.items():
            if fl(r['recipeOutputShare']) >= 0.05 and q in g:
                n4 += 1
                supply = fl(g[q]['outputPerMonth']) + fl(g[q]['importPerMonth'])
                if supply >= 0.25 * fl(r['recipeOutputPerMonth']):
                    ok4 += 1
                else:
                    bad[nat] += 1
r4 = ok4 / n4 if n4 else float('nan')
P(f'  該当 {n4}  合格 {ok4}  割合 {fmt(r4, 3)}  外れの国別 {dict(bad)}')
T4 = (not isn(r4)) and r4 >= 0.9 and T4_VALID
P('  T4 =', 'PASS' if T4 else ('FAIL' if T4_VALID else '記録（単位の食い違い）'))

# ---- T5：買い手（産んだ物が売れて積んでいない）
P('\n[T5] 売れ行き：1840 年・1900 年の断面で、(産出＋輸入) ≧ 1e-6 の（種，国，財）の soldPerMonth ÷ (産出＋輸入) ≧ 0.5 の割合 ≧ 0.8（財＝ソーダ灰・硫酸）。⚠酸とアルカリの割り振りは見分けない（番人 [4]-③ が持つ）')
n5 = ok5 = 0
dem_pos = 0
per = collections.Counter(); perok = collections.Counter()
for s in SEEDS:
    for good in ('ソーダ灰', '硫酸'):
        for nat in NAT8:
            g = cgi[s].get((good, nat), {})
            for Y in (1840, 1900):
                qs = [q for q in g if int(g[q]['year']) == Y]
                if not qs:
                    continue
                q = min(qs)
                o = fl(g[q]['outputPerMonth']) + fl(g[q]['importPerMonth'])
                if o >= 1e-6:
                    n5 += 1
                    per[(good, Y)] += 1
                    if fl(g[q]['demandPerMonth']) > 0:
                        dem_pos += 1
                    if fl(g[q]['soldPerMonth']) / o >= 0.5:
                        ok5 += 1
                        perok[(good, Y)] += 1
r5 = ok5 / n5 if n5 else float('nan')
P(f'  該当 {n5}  合格 {ok5}  割合 {fmt(r5, 3)}  内訳 {dict((k, f"{perok[k]}/{per[k]}") for k in sorted(per))}  （記録：旧の形〈需要が正〉の割合 {fmt(dem_pos / n5 if n5 else float("nan"), 3)}）')
T5 = (not isn(r5)) and r5 >= 0.8
P('  T5 =', 'PASS' if T5 else 'FAIL')

# ---- T6：値段の尾
P('\n[T6] 値段：ソルベー法が開いた（種，国）で、開いた後 16〜24 四半期のソーダ灰 priceReal の平均 ＜ 開く前 −4〜0 四半期の平均 の組の割合 ≧ 0.6（開く前の窓の年が 1790 未満の組は外す）')
n6 = ok6 = 0
t6rows = []
for s in SEEDS:
    for nat in NAT8:
        q0 = open_q(s, 'ソルベー法', nat)
        if q0 is None or q0 < 28 or q0 + 24 > HOR:
            continue
        g = cgi[s].get(('ソーダ灰', nat), {})
        pre = mean([fl(g[q]['priceReal']) for q in range(q0 - 4, q0 + 1) if q in g])
        post = mean([fl(g[q]['priceReal']) for q in range(q0 + 16, q0 + 25) if q in g])
        if isn(pre) or isn(post):
            continue
        n6 += 1
        t6rows.append((s, nat, q0, pre, post))
        if post < pre:
            ok6 += 1
r6 = ok6 / n6 if n6 else float('nan')
P(f'  該当 {n6}  下がった {ok6}  割合 {fmt(r6, 3)}  後÷前の中央値 {fmt(med([p[4] / p[3] for p in t6rows if p[3] > 0]))}')
T6 = (not isn(r6)) and r6 >= 0.6
P('  T6 =', 'PASS' if T6 else 'FAIL')

# ---- M の総括
P('\n[M] 〈機構が狙いどおり働いたか〉の段（追記 3 の読み替え後）')
P('  M1（入口）= T2 :', 'PASS' if T2 else 'FAIL', '（既存の時計が満たす弱い線）')
P('  M2（尾・鎖）= T3 かつ T4 かつ T6 :', 'PASS' if (T3 and T4 and T6) else 'FAIL', f'(T3 {T3}, T4 {T4}, T6 {T6})')
P('  M3（売れ行き）= T5 :', 'PASS' if T5 else 'FAIL', '（割り振りは見分けない＝盤面の確かめとしては弱い。割り振りは番人 [4]-③）')

# ---- 記録
P('\n[R1] 草木灰の割合の年代（ソーダ灰の産出のうち 新しい製法でない分＝1−Σ新）・8 国の中央値（種×国の中央値の中央値）')
for Y in (1783, 1800, 1820, 1840, 1860, 1880, 1900, 1920, 1940, 1950):
    vals = []
    for s in SEEDS:
        for nat in NAT8:
            qs = [q for q, r in chemi[s].get(('ルブラン法', nat), {}).items() if int(r['year']) == Y]
            if not qs:
                continue
            q = min(qs)
            tot = 0.0
            ok = True
            for row in ('ルブラン法', 'ソルベー法', '電解'):
                v = share(s, row, nat, q)
                if isn(v):
                    ok = False
                    break
                tot += v
            if ok:
                vals.append(1 - tot)
    P(f'  {Y}: 草木灰の割合の中央値 {fmt(med(vals), 3)}（n={len(vals)}）')

P('\n[R2] 硫酸・ソーダ灰の実質の値段（chemgood_s priceReal）の年代・8 国×種の中央値')
for good in ('硫酸', 'ソーダ灰'):
    line = []
    for Y in (1783, 1790, 1800, 1820, 1840, 1860, 1880, 1900, 1930, 1950):
        vals = []
        for s in SEEDS:
            for nat in NAT8:
                g = cgi[s].get((good, nat), {})
                qs = [q for q in g if int(g[q]['year']) == Y]
                if qs:
                    vals.append(fl(g[min(qs)]['priceReal']))
        line.append(f'{Y}:{fmt(med(vals), 3)}')
    P(f'  {good}  ' + '  '.join(line))

P('\n[R3] 電解の開いた国・年（chem_s の known）')
for s in SEEDS:
    ev = [(nat, open_q(s, '電解', nat)) for nat in NAT8 if open_q(s, '電解', nat) is not None]
    P(f'  seed {s}: ', [(n, 1783 + q // 4) for n, q in ev])

# 旧との比べ：disc_s の T 行 replaced（黄鉄鉱法）と E 行の最初の年の差
def tab_T(d, s, row):
    r = rows(d, 'engineer_s', s)
    res = {}
    if r is None:
        return res
    for x in r:
        if x['kind'] == 'T' and x['row'] == row:
            res[(x['nation'], int(x['quarter']))] = fl(x['replaced'])
    return res

P('\n[R4] 黄鉄鉱法の行の replaced（平滑した実質の値段・旧は化学薬品、新は硫酸）の旧→新・8 国×種の中央値（同じ種・国・四半期）')
for Y in (1783, 1790, 1800, 1830, 1860, 1900, 1930, 1950):
    q = (Y - 1783) * 4 + 3   # 年の最後の四半期の前後で最も近い物を取る
    rr = []
    ro, rn = [], []
    for s in SEEDS:
        a = tab_T(OLD, s, '黄鉄鉱法')
        b = tab_T(NEW, s, '黄鉄鉱法')
        for nat in NAT8:
            qq = next((c for c in (q, q - 1, q - 2, q - 3) if (nat, c) in a and (nat, c) in b), None)
            x, y = (a[(nat, qq)], b[(nat, qq)]) if qq is not None else (float('nan'), float('nan'))
            if not isn(x) and not isn(y) and x > 0:
                rr.append(y / x)
                ro.append(x); rn.append(y)
    P(f'  {Y}: 旧の中央値 {fmt(med(ro), 3)}  新の中央値 {fmt(med(rn), 3)}  比の中央値 {fmt(med(rr), 3)}（n={len(rr)}）')

P('\n[R5] 下流・材の価格の新÷旧の中央値（同じ種・国・四半期・記録・採否に使わない）。(a) pxgoods_s＝8 国の実質〔priceMean÷物価水準〕・開幕 1783〜1794 だけ（その表の範囲）。(b) goods_s＝その表に出る国（8 国ではない）の名目 priceMean・10 年刻み')
WATCH = ['石鹸', '紙', 'ガラス', '医薬品', '肥料', 'ダイナマイト', '絹織物', '染料', '加工肉', '缶詰', 'セメント', '内燃機関', '冷蔵庫', '砂糖', 'ゴム', '火薬', '化学兵器', '木', '硝石', '硫黄', '塩', '石炭', '石灰石', '銅鉱']
def load_px(d, s_):
    st_ = {}
    r = rows(d, 'pxgoods_s', s_)
    for x in (r or []):
        if x['goodName'] in WATCH and x['nation'] in NAT8 and x['time'].startswith('q'):
            lv = fl(x['regionPriceLevelMeanIndex'])
            st_[(s_, x['nation'], x['goodName'], int(x['time'][1:]))] = fl(x['priceMean']) / lv if lv > 0 else float('nan')
    return st_
def load_g(d, s_, qs):
    st_ = {}
    r = rows(d, 'goods_s', s_)
    for x in (r or []):
        if x['goodName'] in WATCH:
            q = int(x['quarter'])
            if q in qs:
                st_[(s_, x['nation'], x['goodName'], q)] = fl(x['priceMean'])
    return st_
def table(gold, gnew, qlist, label):
    P('  ' + label + '  財   ' + ' '.join(f'{1783 + q // 4:>6}' for q in qlist))
    for g in WATCH:
        cells = []
        for q in qlist:
            rr = []
            for k, v in gnew.items():
                if k[2] == g and k[3] == q:
                    o = gold.get(k, float('nan'))
                    if not isn(o) and o > 0 and not isn(v):
                        rr.append(v / o)
            cells.append(f'{fmt(med(rr), 3):>6}' if rr else '   n/a')
        P(f'  {g}  ' + ' '.join(cells))
QA = [0, 4, 8, 11, 16, 28]
QB = [(Y - 1783) * 4 for Y in (1783, 1800, 1820, 1840, 1860, 1880, 1900, 1920, 1940)]
ga, gb = {}, {}
oa, ob = {}, {}
for s_ in SEEDS:
    oa.update(load_px(OLD, s_)); ga.update(load_px(NEW, s_))
    ob.update(load_g(OLD, s_, set(QB))); gb.update(load_g(NEW, s_, set(QB)))
table(oa, ga, [q for q in QA if q <= 47], '(a)')
table(ob, gb, QB, '(b)')

P('\n[R6] 発見の最初の年の中央値（disc_s の E 行・種の中央値）の旧と新で 5 年以上違う行（化学兵器・合成ゴムの電力待ちの影響ほか）')
def first_years(d):
    res = collections.defaultdict(list)
    for s in SEEDS:
        r = rows(d, 'disc_s', s)
        if r is None:
            continue
        seen = {}
        for x in r:
            if x['kind'] == 'E' and int(x['quarter']) > 0:
                y = int(x['year'])
                if x['row'] not in seen or y < seen[x['row']]:
                    seen[x['row']] = y
        for k, v in seen.items():
            res[k].append(v)
    return res
fo, fn = first_years(OLD), first_years(NEW)
diffs = []
for k in sorted(set(fo) | set(fn)):
    a, b = med(fo.get(k, [])), med(fn.get(k, []))
    if isn(a) or isn(b):
        diffs.append((k, fmt(a), fmt(b), '片方に無し'))
    elif abs(a - b) >= 5:
        diffs.append((k, a, b, f'{len(fo.get(k, []))}→{len(fn.get(k, []))} 種'))
for d_ in diffs:
    P('  ', d_)
if not diffs:
    P('   差なし')

import os
with open(os.environ.get('JUDGE_OUT', 'D:/PoB_work/gd_493/judge_out.txt'), 'w', encoding='utf-8') as f:
    f.write('\n'.join(out) + '\n')
