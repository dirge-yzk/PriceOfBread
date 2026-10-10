"""(177) 植民地の収支の読み手（読むだけ・an_col175 の直し＝反証役 vc1#1）。python an_col177.py <col の出力フォルダ> [国名,...]
C 行（inj_col175.py）を国×10年の窓に畳み、種ごとの和の中央値（種の数＝ファイルの数）を出す。
費用＝植民地格の州の 陸（兵の給与と軍需）＋行政＋その他の正の支出＋国の植民地維持費（補給財＝台帳の減り）。
収入＝植民地格の州の POP が払った税×（国庫へ動いた税÷全ての税＝漏れの割合）。地税＝そのうちの地税の分。
地代＝植民地格の州の RGO の残余（国営持分の後）＝輸出作物（賦存かつ主食でない）と それ以外（主食ほか）・国内の売上と輸出代金の和。
比＝費用÷国の税（国庫へ動いた税）・植民地の収入÷費用。汽船＝首都の汽船の解禁度が初めて正になった年（種ごと）と窓の終わりの汽船の割合・海運占有。"""
import sys, os, glob, statistics as st
d = sys.argv[1]
names = sys.argv[2].split(',') if len(sys.argv) > 2 else ['イギリス', 'フランス', 'ネーデルラント', 'スペイン', 'ポルトガル']
files = sorted(glob.glob(os.path.join(d, 'c*.txt')))
W = [(y, min(y + 9, 1950)) for y in range(1783, 1951, 10)]
def win(y):
    for i, (a, b) in enumerate(W):
        if a <= y <= b: return i
    return None
def f(x):
    try: return float(x)
    except: return float('nan')
# acc[name][seed][win] = dict
acc = {n: [] for n in names}
gate_year = {n: [] for n in names}
for fp in files:
    per = {n: [dict() for _ in W] for n in names}
    gy = {n: None for n in names}
    for ln in open(fp, encoding='utf-8'):
        if not ln.startswith('C|'): continue
        c = ln.rstrip('\n').split('|')
        base = c[4]
        if base not in per: continue
        y = int(c[2]); wi = win(y)
        if wi is None: continue
        v = per[base][wi]
        nc, cp, npop = f(c[6]), f(c[7]), f(c[8])
        army, admin, oth, dup, dtax = f(c[9]), f(c[10]), f(c[11]), f(c[12]), f(c[13])
        taxc, landc, taxa, landa, moved = f(c[14]), f(c[15]), f(c[16]), f(c[17]), f(c[18])
        rde, rdl, rxe, rxl, invl = f(c[19]), f(c[20]), f(c[21]), f(c[22]), f(c[23])
        steam, cap, burden, gate = f(c[24]), f(c[25]), f(c[26]), f(c[27])
        leak = moved / taxa if taxa > 0 else 1.0
        cost = army + admin + oth + max(0.0, -dup)
        rev = taxc * leak
        for k, x in [('cost', cost), ('army', army), ('admin', admin), ('supply', max(0.0, -dup)), ('rev', rev), ('land', landc * leak), ('nattax', moved),
                     ('rexp', rde + rxe), ('rloc', rdl + rxl), ('invl', invl), ('q', 1.0), ('nc', nc), ('cp', cp), ('np', npop), ('burden', burden), ('steam', steam)]:
            v[k] = v.get(k, 0.0) + x
        v['steam_end'] = steam; v['burden_end'] = burden
        if gy[base] is None and gate > 0: gy[base] = y
    for n in names:
        acc[n].append(per[n]); gate_year[n].append(gy[n])
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
print(f'# (177) 植民地の収支（{len(files)}種・種ごとの窓の和の中央値）＝{d}')
print('# 列＝窓｜植民地格の州（窓の平均）｜植民地の人口÷国の人口｜費用÷国の税｜植民地の収入÷費用｜地税÷費用｜費用の内訳（陸・行政・補給財の割合）｜'
      '地代：輸出作物÷全体｜輸出作物の地代÷国の税（発生の側）｜不在地主が受け取った地代÷国の税（受け取りの側＝inj_col177）｜受け取り÷発生｜汽船の割合（窓の終わり）｜海運占有（窓の平均）')
for n in names:
    print(f'\n## {n}　汽船の解禁（首都）の年＝' + ' '.join(str(g) if g else '無' for g in gate_year[n]))
    for wi, (a, b) in enumerate(W):
        rows = [s[wi] for s in acc[n] if s[wi].get('q', 0) > 0]
        if not rows: continue
        def m(fn): return med([fn(r) for r in rows])
        q = lambda r: r['q']
        nc = m(lambda r: r['nc'] / q(r)); cps = m(lambda r: r['cp'] / max(1e-9, r['np']))
        cr = m(lambda r: r['cost'] / r['nattax'] if r['nattax'] > 0 else float('nan'))
        rc = m(lambda r: r['rev'] / r['cost'] if r['cost'] > 0 else float('nan'))
        lc = m(lambda r: r['land'] / r['cost'] if r['cost'] > 0 else float('nan'))
        ar = m(lambda r: r['army'] / r['cost'] if r['cost'] > 0 else float('nan'))
        ad = m(lambda r: r['admin'] / r['cost'] if r['cost'] > 0 else float('nan'))
        sp = m(lambda r: r['supply'] / r['cost'] if r['cost'] > 0 else float('nan'))
        re_ = m(lambda r: r['rexp'] / (r['rexp'] + r['rloc']) if (r['rexp'] + r['rloc']) != 0 else float('nan'))
        rt = m(lambda r: r['rexp'] / r['nattax'] if r['nattax'] > 0 else float('nan'))
        it = m(lambda r: r['invl'] / r['nattax'] if r['nattax'] > 0 else float('nan'))
        rr = m(lambda r: r['invl'] / r['rexp'] if r['rexp'] != 0 else float('nan'))
        se = m(lambda r: r['steam_end']); bu = m(lambda r: r['burden'] / q(r))
        if not (nc >= 0.5):   # 植民地格の州が無い窓（種の中央）＝比は意味を持たない
            print(f'{a}-{b}｜州 {nc:5.1f}｜人口 {cps:5.3f}｜（植民地格の州が無い）｜汽船 {se:4.2f}'); continue
        print(f'{a}-{b}｜州 {nc:5.1f}｜人口 {cps:5.3f}｜費用÷税 {cr:6.3f}｜収入÷費用 {rc:6.2f}｜地税÷費用 {lc:6.2f}｜陸 {ar:4.2f} 行政 {ad:4.2f} 補給 {sp:4.2f}｜'
              f'輸出作物 {re_:5.2f}｜輸出作物の地代÷税 {rt:6.3f}｜受け取り÷税 {it:6.3f}｜受け取り÷発生 {rr:5.2f}｜汽船 {se:4.2f}｜占有 {bu:5.2f}')
