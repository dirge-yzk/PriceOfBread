# (470) 天然ゴムの需要と供給の読み手（読むだけ）。事前登録 measurements/rs470_20261020_prereg.txt の [5] と追記 2〔[11]〕の門 G0・a1・a3・a6 と記録 a2・a4・r1〜r5 を数字で出す。
# 追記 2（反証役の点検の処置）：G0 の線＝基線の中央値−ヌル腕 m466n の中央値の幅（G0-c は記録）／a3 の列＝demandWeightedIndex／a2・a4 は記録へ／r4 気候ゲート財の広がり・r5 産地の州の食料。
# 使い方：PYTHONUTF8=1 python scripts/instruments/rs470/judge.py <腕の出力>/ins <種の数> [--base D:/PoB_work/keep_466/m466_ins] [--abase <腕 A の出力>/ins（r4 の基線・種 1〜5）]
# 窓：マッキントッシュ 1823 に合わせて 1825〜1850（rb の 1800〜1839 は行が開く前の窓＝使わない）。供給÷需要は 1830〜1880。
import csv, sys, statistics, collections, os
def rd(p):
    if not os.path.exists(p): return []
    return list(csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'))
def med(x):
    x = [v for v in x if v == v]
    return statistics.median(x) if x else float('nan')
def eng(d, s): return rd(f'{d}/engineer_s{s}.tsv')
def g0(d, seeds):
    a, b, c, first = [], [], 0, []
    for s in seeds:
        rs = [r for r in eng(d, s) if r['row'] == '加硫ゴム']
        T = [r for r in rs if r['kind'] == 'T']
        a.append(sum(1 for r in T if 1795 <= int(r['year']) <= 1860 and float(r['needTerm']) > 0))
        b.append(sum(1 for r in T if 1830 <= int(r['year']) <= 1860 and float(r['needTerm']) > 0))
        E = [r for r in rs if r['kind'] == 'E']
        if E: c += 1; first.append(min(int(r['year']) for r in E))
    return a, b, c, first
def firsts(d, seeds, row):
    out = []
    for s in seeds:
        E = [int(r['year']) for r in eng(d, s) if r['row'] == row and r['kind'] == 'E']
        out.append(min(E) if E else None)
    return out
def year_end(rows, good):
    """国ごとにその年の最後の四半期の行を取り、年ごとに (産出和, 需要和, 売れた和, 需要の重みつきの参照比, 需要が正の国の数) を返す。
    ★追記 2：参照比は列 demandWeightedIndex（州ごとの需要の重みつき＝張り付きが平均で隠れない）を国の需要で重みづける（旧は priceOverRefTimesLevelIndex＝稼働中の全州の単純平均）"""
    by = collections.defaultdict(dict)
    for r in rows:
        if r['goodName'] != good: continue
        y = int(r['year']); n = r['nation']; q = int(r['quarter'])
        if n not in by[y] or q > int(by[y][n]['quarter']): by[y][n] = r
    out = {}
    for y, d in by.items():
        P = sum(float(r['producedPerMonth']) for r in d.values()); D = sum(float(r['demandPerMonth']) for r in d.values())
        S = sum(float(r['soldPerMonth']) for r in d.values())
        w = sum(float(r['demandPerMonth']) * float(r['demandWeightedIndex']) for r in d.values() if float(r['demandPerMonth']) > 0)
        out[y] = (P, D, S, w / D if D > 0 else float('nan'), sum(1 for r in d.values() if float(r['demandPerMonth']) > 0 or float(r['soldPerMonth']) > 0))
    return out
COLONIAL = ['生ゴム', 'パーム油', '象牙', 'サトウキビ', 'カカオ', '香辛料', 'アヘン', 'グアノ', '茶', 'コーヒー', '金鶏樹皮']
def good_stats(d, seeds, name, y0=1830, y1=1880):
    """気候ゲート財 1 財の (産出和の年の中央値, 売れた和の年の中央値, 需要の重みつきの参照比の年の中央値) を種ごとに返す（rb_s の年末の行）"""
    P, S, W = [], [], []
    for s in seeds:
        yy = year_end(rd(f'{d}/rb_s{s}.tsv'), name)
        ys = [y for y in range(y0, y1 + 1) if y in yy]
        P.append(med([yy[y][0] for y in ys])); S.append(med([yy[y][2] for y in ys])); W.append(med([yy[y][3] for y in ys]))
    return P, S, W
def null_width(key):
    """G0 の線の幅＝ヌル腕 n1・n2 の中央値と基線 m466_ins の中央値の差の大きい方（keep_466 の生・無ければ 0）"""
    root = 'D:/PoB_work/keep_466'
    if not os.path.isdir(f'{root}/n1'): return 0.0
    bm = med(g0(f'{root}/m466_ins', range(1, 11))[key])
    return max(abs(med(g0(f'{root}/{n}', range(1, 11))[key]) - bm) for n in ('n1', 'n2'))
def site_regions(d, s):
    return {(r['nation'], r['region']) for r in rd(f'{d}/rbsite_s{s}.tsv') if 1830 <= int(r['year']) <= 1880 and float(r['output']) > 0}
def food_of(d, s, keys, y0=1830, y1=1880):
    """産地の州の食料の満たされた割合（region_s の foodFillShare）の平均と、0.5 未満の州×四半期の割合"""
    v = [float(r['foodFillShare']) for r in rd(f'{d}/region_s{s}.tsv') if (r['nation'], r['region']) in keys and y0 <= int(r['year']) <= y1 and r['foodFillShare'] != '']
    return (sum(v) / len(v) if v else float('nan'), sum(1 for x in v if x < 0.5) / len(v) if v else float('nan'), len(v))
def rnd(t):
    return tuple(round(x, 3) if isinstance(x, float) else x for x in t)
def main():
    d = sys.argv[1]; n = int(sys.argv[2]); seeds = range(1, n + 1)
    base = sys.argv[sys.argv.index('--base') + 1] if '--base' in sys.argv else 'D:/PoB_work/keep_466/m466_ins'
    abase = sys.argv[sys.argv.index('--abase') + 1] if '--abase' in sys.argv else None
    ga, gb, gc, gf = g0(d, seeds); ba, bb, bc, bf = g0(base, range(1, 11))
    wa, wb = null_width(0), null_width(1)
    la, lb = med(ba) - wa, med(bb) - wb
    print('== G0 加硫ゴム（機構の向き・線＝基線の中央値−ヌル腕 m466n の中央値の幅〔追記 2〕）==')
    print(f'G0-a needTerm>0 の行（1795-1860）種ごと {ga} 中央値 {med(ga)}  基線 {med(ba)}  ヌル腕の幅 {wa}  線 {la} → {"通る" if med(ga) >= la else "外れ"}')
    print(f'G0-b needTerm>0 の行（1830-1860）種ごと {gb} 中央値 {med(gb)}  基線 {med(bb)}  ヌル腕の幅 {wb}  線 {lb} → {"通る" if med(gb) >= lb else "外れ"}')
    print(f'G0-c（記録）加硫ゴムを発見した種の数 {gc}/{n}（{gf}）  基線 {bc}/10')
    a1 = []; a2 = []; a3 = []; a4 = []; a6 = []; r1 = []
    for s in seeds:
        E = [int(r['year']) for r in eng(d, s) if r['row'] == 'マッキントッシュ防水' and r['kind'] == 'E']
        a1.append(min(E) if E else None)
        rs = rd(f'{d}/rb_s{s}.tsv')
        raw = year_end(rs, '生ゴム')
        win = [y for y in range(1825, 1851) if y in raw]
        a2.append(med([raw[y][4] for y in win]))
        a3.append(med([raw[y][3] for y in win]))
        a4.append(med([raw[y][0] / raw[y][1] for y in range(1830, 1881) if y in raw and raw[y][1] > 0]))
        sites = rd(f'{d}/rbsite_s{s}.tsv')
        prodsites = {(r['nation'], r['region']) for r in sites if 1830 <= int(r['year']) <= 1880 and float(r['output']) > 0}
        a6.append(len(prodsites))
        r1.append(sum(1 for r in sites if 1825 <= int(r['year']) <= 1850 and float(r['priceOverRef']) < 0.2))
    ok1 = sum(1 for x in a1 if x is not None and x <= 1850)
    print(f'== a1 マッキントッシュ防水を 1850 年までに発見した種 {ok1}/{n}（初発見の年 {a1}）→ {"通る" if ok1 >= 7 else "外れ"}（7種以上）')
    print(f'== a3 生ゴムの需要の重みつきの参照比〔列 demandWeightedIndex〕（1825-1850 の年の中央値）種ごと {[round(x,2) for x in a3]} 中央値 {round(med(a3),2)} → {"通る" if med(a3) <= 10 else "外れ"}（10倍以内）')
    print(f'== a6 生ゴムが産出される産地の州の数（1830-1880）種ごと {a6} 中央値 {med(a6)} → {"通る" if med(a6) >= 2 else "外れ"}（2州以上）')
    print(f'== a2（記録に下げた）生ゴムに需要が立つ国（1825-1850 の年末の中央値）種ごと {a2} 中央値 {med(a2)}（旧の線＝3国以上）')
    print(f'== a4（記録に下げた・史実の分母が無い）供給÷需要（1830-1880 の年の中央値）種ごと {[round(x,3) for x in a4]} 中央値 {round(med(a4),3)}（旧の線＝0.5以上）')
    print(f'== r1（記録）産地の州×年末で地元の値段が参照の 0.2 倍未満の行の数（1825-1850・冷間始動の罠の印）{r1}')
    print('== r2（記録）最初の発見の年（種ごと）')
    for row in ['加硫ゴム', '合成ゴム', '自転車', '自動車', 'マッキントッシュ防水']:
        print('  ', row, firsts(d, seeds, row))
    # r3（記録）F2 の広がり＝基線で代替する物の値段が立たなかった（−1）T 行が腕で立った行の数（1810 年まで＝行〈マッキントッシュ防水〉が開く前＝F2 以外の差が小さい期間）を行ごとに種で合計
    chg = collections.Counter()
    for s in seeds:
        b = {(r['quarter'], r['nation'], r['row']): r for r in eng(base, s) if r['kind'] == 'T'}
        for r in eng(d, s):
            if r['kind'] != 'T' or int(r['year']) > 1810: continue
            br = b.get((r['quarter'], r['nation'], r['row']))
            if br and float(br['replaced']) < 0 and float(r['replaced']) > 0: chg[r['row']] += 1
    print('== r3（記録）F2 の広がり：基線で値段が立たず腕で立った T 行の数（1810 年まで・種の合計）', dict(chg.most_common(15)))
    # r4（記録）F3 の広がり＝気候ゲート財の産出・値段の比（腕 ÷ 腕 A の同じ種〔恒等〕・種 1〜5・1830-1880 の年末の中央値）
    if abase:
        s5 = [s for s in seeds if s <= 5]
        print(f'== r4（記録）気候ゲート財の広がり：産出の比（腕÷腕 A・種 {list(s5)}・1830-1880 の年末の中央値）と参照比')
        for name in COLONIAL:
            P1, S1, W1 = good_stats(d, s5, name); P0, S0, W0 = good_stats(abase, s5, name)
            ratio = [round(a / b, 2) if b == b and b > 0 else None for a, b in zip(P1, P0)]
            print(f'   {name}：産出の比 {ratio}  腕の産出 {[round(x,1) for x in P1]}  基線の産出 {[round(x,1) for x in P0]}  参照比（腕）{[round(x,2) for x in W1]}  参照比（基線）{[round(x,2) for x in W0]}')
    else:
        print('== r4（記録）--abase が無い＝未計測')
    # r5（記録）産地の州の食料：腕の産地の州（rbsite の output>0）の foodFillShare の平均と 0.5 未満の割合を腕と基線 m466_ins で比べる（同じ種・同じ州）
    print('== r5（記録）産地の州の食料の満たされた割合（region_s・1830-1880・種ごと〔平均, 0.5未満の割合, 件数〕腕 / 基線）')
    for s in seeds:
        keys = site_regions(d, s)
        if keys: print('   種', s, '州', len(keys), '腕', rnd(food_of(d, s, keys)), '基線', rnd(food_of(base, s, keys)))
    gall = med(ga) >= la and med(gb) >= lb
    allok = gall and ok1 >= 7 and med(a3) <= 10 and med(a6) >= 2
    print('== 判定（追記 2 の門＝G0・a1・a3・a6）：G0', '通る' if gall else '外れ', '/ a1', ok1 >= 7, '/ a3', med(a3) <= 10, '/ a6', med(a6) >= 2, '→', '働いた' if allok else '働かない（採らない）')
main()
