# (904) c77sm 〈100 倍〉の記録の読み手＝gdp_jump890.py の上に足した物（元の台本は変えない・止める線は替えない）。
#   足したもの（記録だけ・採否に使わない・890 の型の件数は変えない）：
#     ① 890 の型 3〈時間の分母の痩せ〉に入った跳びを、窓 N に依らない基準で割る：〈1 四半期の落ち込みと戻り〉＝直前の 1 四半期だけが前の四半期の 1/THIN 以下に落ちていて（H[-1]≦H[-2]/THIN）
#        跳びの後の値が落ちる前の値の THIN 倍未満（g÷H[-2]＜THIN）の物（オマーン 1803 q79 の型）／〈窓に依存〉＝それ以外の型 3（N を替えると行き先が動く＝刃の上。q84 の 3,180.16＞3,080.8 が N＝9 で免除へ移った型）。
#        オーストラリア王国 1806 q94（7 四半期の下り坂の後の跳び）は 1 四半期の型ではない＝〈それ以外〉のまま（890 の予言どおり）。
#     ② --sweep：N と THIN を振り、基線とヌル腕の〈それ以外〉・型 3 の内訳が動くかを表で出す。固定する値＝N＝8・THIN＝10（890 のまま）。振りの結果は measurements/m904_* に写す。
#   使い方: python -I -X utf8 gdp_jump904.py [--thr=<倍>] [--sweep] [--rows] <名前>=<ins フォルダ> ...   ／   python -I -X utf8 gdp_jump904.py --selftest
import csv, glob, importlib.util, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location('gdp_jump890', os.path.join(HERE, '..', 'c75ts', 'gdp_jump890.py'))
g890 = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(g890)
SUB1 = '1 四半期の落ち込みと戻り'
SUBW = '窓に依存'


def scan_rows(rows, thr):
    """戻り (型ごとの件数, (型 3 のうち 1 四半期の件数, 窓に依存の件数), 1e10 倍以上, 非有限, 行の一覧)。区分は g890.classify と同じ・型 3 だけ副区分で割る。"""
    hist = {}; first = {}; ppop = {}; cnt = {t: 0 for t in g890.TYPES}; sub1 = subw = 0; j1e10 = nonf = 0; out = []
    for u, nm, y, q, g, pop in rows:
        if u not in first: first[u] = q
        if not math.isfinite(g): nonf += 1; continue
        H = hist.setdefault(u, [])
        if H and H[-1] > 0:
            ra = g / H[-1]
            if ra >= 1e10: j1e10 += 1
            if ra >= thr:
                popr = g890.pratio(pop, ppop[u]); recent = H[-(g890.N + 1):]
                t = g890.classify(q - first[u], ra, popr, g, recent)
                cnt[t] += 1
                s = ''
                if t == g890.TYPES[2]:
                    if len(H) >= 2 and H[-2] > 0 and H[-1] <= H[-2] / g890.THIN and g / H[-2] < g890.THIN: sub1 += 1; s = SUB1
                    else: subw += 1; s = SUBW
                out.append((t, s, nm, y, q, q - first[u], round(ra, 1)))
        H.append(g); ppop[u] = pop
    return cnt, (sub1, subw), j1e10, nonf, out


def read_seed(path):
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        return [(x[ix['nationUid']], x[ix['nation']], int(x[ix['year']]), int(x[ix['quarter']]), g890.num(x[ix['nominalGdpPerYear']]), g890.num(x[ix['populationK']])) for x in r]


_CACHE = {}


def seeds_of(d):
    """d の nation_s*.tsv を 1 回だけ読む（振りで同じ表を何十回も読まない）。"""
    if d not in _CACHE:
        _CACHE[d] = [(os.path.basename(p), read_seed(p)) for p in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv')), key=lambda p: int(os.path.basename(p)[len('nation_s'):-4]))]
    return _CACHE[d]


def scan_dir(d, thr):
    cnt = {t: 0 for t in g890.TYPES}; sub = [0, 0]; j = nf = 0; rows = []
    for name, data in seeds_of(d):
        c, s, j1, n1, out = scan_rows(data, thr)
        for t in cnt: cnt[t] += c[t]
        sub[0] += s[0]; sub[1] += s[1]; j += j1; nf += n1; rows += [(name,) + o for o in out]
    return cnt, tuple(sub), j, nf, rows


def selftest():
    ok = True; L = []
    def chk(name, got, want):
        nonlocal ok
        ok = ok and got == want; L.append('%s：%s（期待 %s）→ %s' % (name, got, want, '期待どおり' if got == want else '**外れ**'))
    def ser(vals, uid='A', nm='甲国'):
        return [(uid, nm, 1800 + i // 4, i, v, 100.0) for i, v in enumerate(vals)]
    T = g890.TYPES
    # 1 四半期だけ 1/1000 に落ちて戻る（オマーン型）：窓 N が何でも型 3 の〈1 四半期の落ち込みと戻り〉。
    v = [1000.0] * 12 + [0.9, 1000.0]
    for n in (2, 4, 8, 9, 12):
        g890.N = n
        c, s, _, _, _ = scan_rows(ser(v), 100.0)
        chk('1 四半期の落ち込みと戻り N=%d：（型 3・それ以外・副区分 1 四半期・窓に依存）' % n, (c[T[2]], c[T[3]], s[0], s[1]), (1, 0, 1, 0))
    # 刃の上：N＝8 では〈それ以外〉・N＝9 では窓に古い高値が入って型 3（窓に依存）。
    v1 = [100.0] * 5 + [5000.0] + [60.0] * 8 + [5.0, 40000.0]
    g890.N = 8
    c, s, _, _, _ = scan_rows(ser(v1, 'K', '壬国'), 100.0)
    chk('刃の上 N=8：（型 3・それ以外・1 四半期・窓に依存）', (c[T[2]], c[T[3]], s[0], s[1]), (0, 1, 0, 0))
    g890.N = 9
    c, s, _, _, _ = scan_rows(ser(v1, 'K', '壬国'), 100.0)
    chk('刃の上 N=9：（型 3・それ以外・1 四半期・窓に依存）', (c[T[2]], c[T[3]], s[0], s[1]), (1, 0, 0, 1))
    # 7 四半期の下り坂の後の跳び（オーストラリア型）：どの N でも〈それ以外〉・副区分に入らない。
    v2 = [1500.0] * 12 + [1128.0, 648.0, 407.0, 227.0, 167.0, 45.9, 30808.0]
    for n in (8, 12):
        g890.N = n
        c2, s2, _, _, _ = scan_rows(ser(v2, 'B', '乙国'), 100.0)
        chk('7 四半期の下り坂の後の跳び N=%d：（それ以外・副区分）' % n, (c2[T[3]], s2), (1, (0, 0)))
    g890.N = 8
    # 戻りが大きすぎる跳び（落ちる前の 20 倍へ）：窓に古い高値が無ければ〈それ以外〉・副区分に入らない。
    v3 = [1000.0] * 12 + [0.9, 20000.0]
    c3, s3, _, _, _ = scan_rows(ser(v3, 'C', '丙国'), 100.0)
    chk('落ちる前の 20 倍へ戻る跳び：（それ以外・副区分）', (c3[T[3]], s3), (1, (0, 0)))
    return ok, L


def sweep(arms, thr):
    print('# 振り：N×THIN ごとの〈それ以外〉・型 3 の内訳（%g 倍以上）' % thr)
    for N in (4, 6, 8, 9, 10, 12, 16):
        for TH in (5.0, 10.0, 20.0):
            g890.N = N; g890.THIN = TH
            cells = []
            for nm, d in arms:
                cnt, sub, _, _, _ = scan_dir(d, thr)
                cells.append('%s それ以外 %d・型3 %d（1四半期 %d・窓に依存 %d）' % (nm, cnt[g890.TYPES[3]], cnt[g890.TYPES[2]], sub[0], sub[1]))
            print('N=%d THIN=%g\t%s' % (N, TH, '\t'.join(cells)))
    g890.N = 8; g890.THIN = 10.0


def main():
    a = sys.argv[1:]
    if a and a[0] == '--selftest':
        ok, L = selftest(); print('\n'.join(L)); print('自己試験：%s' % ('全て期待どおり' if ok else '外れあり')); return 0 if ok else 1
    thr = 100.0
    for x in a:
        if x.startswith('--thr='): thr = float(x[6:])
    arms = [tuple(x.split('=', 1)) for x in a if not x.startswith('--')]
    print('# gdp_jump904 N=%d THIN=%g 閾値=%g（890 の型＋型 3 の副区分は記録だけ）' % (g890.N, g890.THIN, thr))
    for nm, d in arms:
        cnt, sub, j, nf, rows = scan_dir(d, thr)
        print('%s\t%g 倍以上 %d（%s）\t型 3 のうち 1 四半期の落ち込みと戻り %d・窓に依存 %d\t1e10 倍以上 %d\t非有限 %d' % (nm, thr, sum(cnt.values()), '・'.join('%s %d' % (t, cnt[t]) for t in g890.TYPES), sub[0], sub[1], j, nf))
        if '--rows' in a:
            for r in rows: print('   ', r)
    if '--sweep' in a: sweep(arms, thr)
    return 0


if __name__ == '__main__':
    sys.exit(main())
