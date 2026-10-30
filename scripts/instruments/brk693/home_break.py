#!/usr/bin/env python
"""(693) 準備：〈壊す〉①（同じ国が毎回壊れる）の読み手の本国版（読むだけ・C# は変えない・既存の列だけで読む）。
使い方：python -X utf8 -I home_break.py <名前=計器の出力フォルダ> ... [--test 名前] [--min-peak 500] [--keys 清,ロシア,...]
  名前=フォルダ を 2 本以上。--test を渡すと、その腕を〈B〉・残りを〈ヌル腕〉として ① が鳴るかを国ごとに判定する（B が 8 種以上・ヌル腕の最大が 2 種以下）。
読む列（既存の計器の表だけ）：
  nation_s<種>.tsv ＝ nationUid・nation・quarter・populationK（植民地を含む）・coreRegions・integratedRegions・colonialRegions
  region_s<種>.tsv ＝ quarter・nationIndex・tier（Core/Integrated/Colonial）・populationK ＝全ての国の全ての州（活きた州だけ）
本国格の州 ＝ tier が Core か Integrated（Colonial 以外）。本国の人口 ＝ その州の populationK の和。本国の州の数 ＝ その州の件数。
  （stock_s の homePopulationK は 4 四半期ごと＝毎四半期の region_s から作り、stock_s の年末の行と突き合わせて一致を確かめる）
国の鍵 ＝ nationUid（改名・統一をまたいで同じ個体。nationIndex は建国で再利用されるので鍵にしない）。国の名札 ＝ 系列の最初の名前（腕をまたぐ集計用）。
崩れの定義（3 通りを全部出す）：
  旧   ＝ 旧の読み手と同じ：populationK（植民地込み）の最小が開幕（系列の最初の四半期）の 0.5 倍を割る（年末の四半期だけを見る）。
  開幕 ＝ 本国の人口（P）・本国の州の数（R）が系列の最初の値の 0.5 倍を割る、または最後の四半期より前に途切れる。E ＝ P か R。
  走る ＝ 走る最大の 0.5 倍を割る、または途切れる（ar/stop.py の collapsed と同じ）。Ep・Rp・E。
限界の申告：
  * region_s の本国格の人口の和と nation_s の populationK の差（全部の州の和との相対差の最大）を最後に出す。0 でなければ読み手の穴。
  * 本国の州の数は region_s の件数と nation_s の core＋integrated の突き合わせ（不一致の行の数）も出す。
  * 名札は系列の最初の名前＝建国の国は種ごとに名札が違う（分母＝出現した種の数を併記）。開幕に居た国だけで読むなら --opening-only。
"""
import csv, glob, os, sys
from collections import defaultdict


def rd(path):
    f = open(path, encoding='utf-8', newline='')
    r = csv.reader(f, delimiter='\t')
    h = next(r)
    ix = {c: i for i, c in enumerate(h)}

    def gen():
        try:
            yield from r
        finally:
            f.close()
    return ix, gen()


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def seeds_of(d):
    return sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))


def collapsed_open(series, last_q):
    """series＝[(q, v)]。最初の値の半分を割った or 最後の四半期より前に途切れた。"""
    if not series:
        return False
    if series[-1][0] < last_q - 1:
        return True
    v0 = series[0][1]
    if not (v0 > 0):
        return False
    return any(v == v and v < 0.5 * v0 for _, v in series)


def collapsed_run(series, last_q):
    if not series:
        return False
    if series[-1][0] < last_q - 1:
        return True
    mx = 0.0
    for _, v in series:
        if v != v:
            continue
        mx = max(mx, v)
        if mx > 0 and v < 0.5 * mx:
            return True
    return False


def load_seed(d, s):
    # region_s -> (q, nationIndex) -> [homePop, homeN, allPop]
    agg = defaultdict(lambda: [0.0, 0, 0.0])
    ix, r = rd(os.path.join(d, f'region_s{s}.tsv'))
    iq, ii, it, ip = ix['quarter'], ix['nationIndex'], ix['tier'], ix['populationK']
    for x in r:
        a = agg[(int(x[iq]), int(x[ii]))]
        p = fl(x[ip])
        a[2] += p
        if x[it] != 'Colonial':
            a[0] += p
            a[1] += 1
    # nation_s -> uid series
    ix, r = rd(os.path.join(d, f'nation_s{s}.tsv'))
    iq, ii, iu, inm, ip = ix['quarter'], ix['nationIndex'], ix['nationUid'], ix['nation'], ix['populationK']
    ic, igr, icl = ix['coreRegions'], ix['integratedRegions'], ix['colonialRegions']
    ig = ix['realGdpPerYear']; ukmax = 0.0; ukcol = False   # 別の読み手：英の崩れ（実質GDP が最大の 5% を割る・名前＝イギリス・年末の四半期）＝gaud_682/null_raw675.py と同じ
    ser = defaultdict(list)
    last_q = 0
    mism_n = 0; rows_n = 0; maxdev = 0.0
    for x in r:
        q = int(x[iq]); idx = int(x[ii]); last_q = max(last_q, q)
        a = agg.get((q, idx), [0.0, 0, 0.0])
        pop = fl(x[ip])
        ser[x[iu]].append((q, x[inm], pop, a[0], a[1]))
        rows_n += 1
        if x[inm] == 'イギリス' and q % 4 == 0:
            g = fl(x[ig])
            if g == g:
                if ukmax > 0 and g < 0.05 * ukmax: ukcol = True
                ukmax = max(ukmax, g)
        if int(fl(x[ic]) + fl(x[igr])) != a[1]:
            mism_n += 1
        if pop > 0:
            maxdev = max(maxdev, abs(a[2] - pop) / pop)
    return ser, last_q, mism_n, rows_n, maxdev, ukcol


def stock_check(d, s, ser):
    """stock_s の homePopulationK（挙げた国・年末の行）と突き合わせる。返り値＝(比べた行数, 相対差の最大)。"""
    p = os.path.join(d, f'stock_s{s}.tsv')
    if not os.path.exists(p):
        return 0, 0.0
    byname_q = {}
    for uid, rows in ser.items():
        for q, nm, pop, hp, hn in rows:
            byname_q[(nm, q)] = hp
    ix, r = rd(p)
    if 'homePopulationK' not in ix:
        return 0, 0.0
    it, inn, ih = ix['time'], ix['nation'], ix['homePopulationK']
    n = 0; dev = 0.0
    for x in r:
        t = x[it]
        if not (t.startswith('q') and t[1:].isdigit()):
            continue
        q = int(t[1:])
        key = (x[inn], q)
        if key in byname_q and fl(x[ih]) > 0:
            n += 1; dev = max(dev, abs(byname_q[key] - fl(x[ih])) / fl(x[ih]))
    return n, dev


def analyse(d, min_peak, opening_only, check):
    out = defaultdict(lambda: defaultdict(int))   # label -> counter
    seeds = seeds_of(d)
    mism = 0; rows = 0; maxdev = 0.0; sn = 0; sdev = 0.0; uk = [0]
    for s in seeds:
        ser, last_q, m, n, dv, ukc = load_seed(d, s)
        uk[0] += ukc
        mism += m; rows += n; maxdev = max(maxdev, dv)
        if check:
            a, b = stock_check(d, s, ser)
            sn += a; sdev = max(sdev, b)
        for uid, rws in ser.items():
            label = rws[0][1]
            if opening_only and rws[0][0] != 0:
                continue
            peak = max((r[3] for r in rws if r[3] == r[3]), default=0.0)
            c = out[label]
            c['present'] += 1
            if peak < min_peak:
                continue
            c['eligible'] += 1
            P = [(r[0], r[3]) for r in rws]; R = [(r[0], float(r[4])) for r in rws]
            # 旧＝populationK（植民地込み）の年末の四半期の最小が開幕の 0.5 倍を割る
            old = [(r[0], r[2]) for r in rws]
            v0 = old[0][1]
            leg = v0 > 0 and any(q % 4 == 0 and v == v and v < 0.5 * v0 for q, v in old)
            po, ro = collapsed_open(P, last_q), collapsed_open(R, last_q)
            pr, rr = collapsed_run(P, last_q), collapsed_run(R, last_q)
            c['legacy'] += leg; c['P_open'] += po; c['R_open'] += ro; c['E_open'] += (po or ro)
            c['P_run'] += pr; c['R_run'] += rr; c['E_run'] += (pr or rr)
    return out, seeds, (mism, rows, maxdev, sn, sdev, uk[0])


def main():
    args = sys.argv[1:]
    arms = []; test = None; min_peak = 500.0; keys = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
    opening_only = False
    i = 0
    while i < len(args):
        a = args[i]
        if a == '--test': test = args[i + 1]; i += 2
        elif a == '--min-peak': min_peak = float(args[i + 1]); i += 2
        elif a == '--keys': keys = args[i + 1].split(','); i += 2
        elif a == '--opening-only': opening_only = True; i += 1
        elif '=' in a: arms.append(tuple(a.split('=', 1))); i += 1
        else: raise SystemExit(__doc__)
    assert len(arms) >= 1, __doc__
    res = {}
    for name, d in arms:
        o, seeds, chk = analyse(d, min_peak, opening_only, True)
        res[name] = (o, seeds, chk)
    names = [n for n, _ in arms]
    print('# 本国格の州だけで数えた〈壊す〉①（本国＝Colonial 以外・人口のピークが %g 千人以上の国・開幕に居た国だけ=%s）' % (min_peak, opening_only))
    for n in names:
        o, seeds, (mism, rows, dev, sn, sdev, ukn) = res[n]
        print('# 腕 %s：種 %s・限界の申告＝region_s の全州の和と nation_s の populationK の相対差の最大 %.3g／本国の州の数の不一致の行 %d/%d／stock_s の homePopulationK との相対差の最大 %.3g（%d 行）'
              % (n, '%d-%d(%d)' % (seeds[0], seeds[-1], len(seeds)) if seeds else '-', dev, mism, rows, sdev, sn))
    print('# 別の読み手（①ではない）：英の崩れ＝実質GDP がそれまでの最大の 5% を割った種の数（名前＝イギリス・年末の四半期）：' + '　'.join('%s %d/%d' % (n, res[n][2][5], len(res[n][1])) for n in names))
    cols = [('legacy', '旧'), ('P_open', 'P開'), ('R_open', 'R開'), ('E_open', 'E開'), ('E_run', 'E走')]
    print('\n## 主要国の表（各腕：出現した種 / 崩れた種数。旧＝植民地込みの人口が開幕比で半減・P開＝本国の人口・R開＝本国の州の数・E開＝P開かR開・E走＝走る最大比の E）')
    hdr = '国\t' + '\t'.join('%s[%s]' % (n, '/'.join(c for _, c in cols)) for n in names)
    print(hdr)
    for k in keys:
        cells = []
        for n in names:
            c = res[n][0].get(k)
            cells.append('-' if not c or not c['eligible'] else '%d: ' % c['eligible'] + '/'.join(str(c[f]) for f, _ in cols))
        print(k + '\t' + '\t'.join(cells))
    # ① の判定（--test）と鳴り得る国
    nulls = [n for n in names if n != test]
    for tag, f in (('E開', 'E_open'), ('E走', 'E_run')):
        labels = set()
        for n in names:
            labels |= {k for k, c in res[n][0].items() if c['eligible']}
        elig = []; noel = []
        for k in sorted(labels):
            mx = max((res[n][0].get(k, {}).get(f, 0) for n in nulls), default=0) if nulls else 0
            (elig if mx <= 2 else noel).append((k, mx))
        print('\n## ①で鳴り得る国（%s・ヌル腕 %s の崩れの種数の最大が 2 以下）= %d 国 / 母集団 %d 国' % (tag, ','.join(nulls) if nulls else '-', len(elig), len(labels)))
        print('   鳴り得ない国（最大が 3 以上）上位：' + ','.join('%s(%d)' % (k, m) for k, m in sorted(noel, key=lambda t: -t[1])[:15]))
        if test:
            fire = []
            for k in sorted(labels):
                b = res[test][0].get(k, {}).get(f, 0)
                mx = max((res[n][0].get(k, {}).get(f, 0) for n in nulls), default=0)
                if b >= 8 and mx <= 2: fire.append('%s(B=%d・ヌル最大=%d)' % (k, b, mx))
            print('   ① 鳴る国（B=%s が 8 以上かつヌル最大が 2 以下）：%s' % (test, ','.join(fire) if fire else 'なし'))
    # ヌル腕で崩れの多い国（鳴り得ない側の確認）
    print('\n## 全国の表（崩れた種数 E開 / 出現した種数）')
    labels = sorted({k for n in names for k, c in res[n][0].items() if c['eligible']})
    print('国\t' + '\t'.join(names))
    rowsout = []
    for k in labels:
        vals = [res[n][0].get(k, {}) for n in names]
        mx = max(v.get('E_open', 0) for v in vals)
        rowsout.append((mx, k, vals))
    for mx, k, vals in sorted(rowsout, key=lambda t: (-t[0], t[1]))[:40]:
        print(k + '\t' + '\t'.join('%d/%d' % (v.get('E_open', 0), v.get('eligible', 0)) for v in vals))


if __name__ == '__main__':
    main()
