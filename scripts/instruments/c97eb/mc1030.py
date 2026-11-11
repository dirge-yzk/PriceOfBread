# (1030) c97eb：門 N1・N1′（機構が入口に届いたか・事前登録の追記 1）。prodcap_s の開幕の年（最初の q%4==3 の行）を、腕と基線で州×生産者ごとに突き合わせる。
# 使い方: python -X utf8 -I mc1030.py <腕の生> <基線の生> <種の範囲 1-10>
#   鍵＝(region, good, recipe, その鍵の中の出現の順)。marginalCost は Producer の素の値（州の物価水準を掛けない）。
#   N1 ：級 E・X の行で |新÷旧×endowFromProductivity(基線) − 1| ≦ 1e-6 の割合 ≧0.99（旗 0 なら新÷旧＝1＝落ちる）。
#   N1′：級 N・S・F・Z の行で 新＝旧（相対 1e-12）の割合 ≧0.99。
import sys, csv, collections
csv.field_size_limit(1 << 30)
A, B, RNG = sys.argv[1:4]; lo, hi = map(int, RNG.split('-'))
def load(p):
    f = open(p, encoding='utf-8', newline=''); r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    d = {}; cnt = collections.Counter(); y0 = None
    for row in r:
        y = int(row[ix['year']])
        if y0 is None: y0 = y
        if y != y0: break
        k = (row[ix['region']], row[ix['good']], row[ix['recipe']]); cnt[k] += 1
        d[k + (cnt[k],)] = (row[ix['class']], float(row[ix['marginalCost']]), float(row[ix['endowFromProductivity']]))
    return y0, d
e_ok = e_n = o_ok = o_n = miss = 0; worst = []; big = []
for s in range(lo, hi + 1):
    ya, a = load(f'{A}/prodcap_s{s}.tsv'); yb, b = load(f'{B}/prodcap_s{s}.tsv')
    for k, (cl, mcb, eb) in b.items():
        if k not in a: miss += 1; continue
        mca = a[k][1]
        if cl in ('E', 'X'):
            e_n += 1; v = (mca / mcb) * eb if mcb != 0 else float('nan')
            if abs(v - 1) <= 1e-6: e_ok += 1
            elif len(worst) < 5: worst.append((s, k, cl, mcb, mca, eb))
            if eb > 10: big.append((s, k[0], k[1], eb, mca / mcb if mcb else float('nan')))
        else:
            o_n += 1
            if abs(mca - mcb) <= 1e-12 * max(abs(mcb), 1e-300): o_ok += 1
            elif len(worst) < 10: worst.append((s, k, cl, mcb, mca, eb))
    miss += sum(1 for k in a if k not in b)
print(f'開幕の年 {ya}／{yb}・鍵の不一致 {miss}')
print(f'N1  級 E・X：{e_ok}/{e_n}＝{e_ok/max(1,e_n):.4f}（≧0.99）{"通る" if e_n and e_ok/e_n >= 0.99 else "落ちる"}')
print(f'N1′ 他の級：{o_ok}/{o_n}＝{o_ok/max(1,o_n):.4f}（≧0.99）{"通る" if o_n and o_ok/o_n >= 0.99 else "落ちる"}')
for x in big[:5]: print('倍率 10 超の行（種・州・財・賦存・新÷旧）', x)
for x in worst: print('外れ', x)
