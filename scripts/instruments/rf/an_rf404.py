"""(404) 専制の改革の経路（00-REFORM）の読み手（読むだけ）。事前登録 measurements/rf_20261018_prereg.txt の [4] 止める線・[5] 記録の線・追記2 の定義どおり。
使い方：python an_rf404.py A=<出力>/ins B=<出力>/ins N1=<出力>/ins N2=<出力>/ins [BF=<出力>/ins] [sec=lock,leg,col,top,r1,r2,r3,r4,r5,r6,r8,d2]
  読む表＝reform_s・nation_s（<出力>/ins の *_s<種>.tsv）。腕の結果を見てから式を書かない＝式は事前登録の定義だけ。
  ヌル腕＝A・N1・N2（BF・B はヌルに含めない）。[止]＝止める線に使う物差し／[記]＝記録（採否に使わない）。
  行の重み：reform_s は挙げた 8 国が毎四半期・他の全ての国は年末だけ＝全国の比は年末の行（q%4==3）だけで数える（全ての国が 1 行ずつ＝一様の重み）。
        挙げた 8 国の毎四半期の行は、凍結の長さ・開幕の q を読む節だけで使う。
  ⚠限界（自己申告）：reform_s の mandateStockIndex は四半期末（Politics.Step の回復の後）の S／法の段の整数は enum の序数（教育 0-3・参政権 0-3・報道 0-2・官僚 0-2・土地改革 0-2・農奴 0-2）／
        nation_s の polityCode の変化は四半期の末の断面で数える（四半期内の往復は見えない）／wallPassAxes は軸名の文字列。
"""
import csv, glob, math, os, re, sys, collections

csv.field_size_limit(1 << 30)
LISTED = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']
AUTOCRAT = {0, 1, 2, 5, 6, 7}          # Absolutist・Despotic・Shogunate・BourgeoisDictatorship・ProletarianDictatorship・MilitaryDictatorship
LAWCOLS = [('educationLawCode', 3), ('suffrageLawCode', 3), ('censorshipLawCode', 2), ('landReformLawCode', 2), ('bureaucracyLawCode', 2), ('serfLawCode', 2)]
POLITY = ['絶対', '専制', '幕府', '立憲', '共和', 'ブル独', 'プロ独', '軍政', '無政府']


def fl(x, d=float('nan')):
    try:
        return float(x)
    except Exception:
        return d


def fin(x):
    return x == x and x not in (float('inf'), float('-inf'))


def med(v):
    v = sorted(x for x in v if fin(x))
    n = len(v)
    if n == 0:
        return float('nan')
    return v[n // 2] if n % 2 else 0.5 * (v[n // 2 - 1] + v[n // 2])


def quant(v, p):
    v = sorted(x for x in v if fin(x))
    if not v:
        return float('nan')
    return v[min(len(v) - 1, int(p * len(v)))]


def listed_name(n):
    for b in LISTED:
        if b in n:
            return b
    return None


def read_tsv(path, cols=None):
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        hdr = next(r)
        idx = {h: i for i, h in enumerate(hdr)}
        want = cols or hdr
        ii = [idx[c] for c in want if c in idx]
        names = [c for c in want if c in idx]
        for row in r:
            yield {n: row[i] for n, i in zip(names, ii) if i < len(row)}


def seeds_of(d, tbl):
    out = []
    for p in glob.glob(os.path.join(d, tbl + '_s*.tsv')):
        m = re.search(r'_s(\d+)\.tsv$', p)
        if m:
            out.append(int(m.group(1)))
    return sorted(out)


class Arm:
    def __init__(self, name, d):
        self.name, self.d = name, d
        self.seeds = seeds_of(d, 'reform')
        self._reform, self._nat = {}, {}

    def reform(self, s):
        if s not in self._reform:
            rows = []
            for r in read_tsv(os.path.join(self.d, f'reform_s{s}.tsv')):
                rows.append({k: (v if k in ('nation', 'wallPassAxes') else fl(v)) for k, v in r.items()})
            self._reform[s] = rows
        return self._reform[s]

    def nat(self, s):
        if s not in self._nat:
            rows = []
            for r in read_tsv(os.path.join(self.d, f'nation_s{s}.tsv'), ['quarter', 'year', 'nation', 'nationUid', 'populationK', 'realGdpPerYear', 'polityCode', 'supplyFillShare']):
                rows.append({k: (v if k == 'nation' else fl(v)) for k, v in r.items()})
            self._nat[s] = rows
        return self._nat[s]

    def drop(self, s):
        self._reform.pop(s, None)
        self._nat.pop(s, None)


def yearend(r):
    return int(r['quarter']) % 4 == 3


def sec_lock(arms):
    print('=== [止 ③] 片方向の錠：年末の行（全ての国）で法の段が最大の段にある割合（6 法の和 ÷ 6 法×行）===')
    res = {}
    for a in arms:
        tot, at = 0, collections.Counter()
        for s in a.seeds:
            for r in a.reform(s):
                if not yearend(r):
                    continue
                tot += 1
                for c, mx in LAWCOLS:
                    if int(r[c]) >= mx:
                        at[c] += 1
        res[a.name] = (tot, at)
        print(f'  {a.name}：年末の行 {tot}・最大の段の行 ' + '・'.join(f'{c[:-7]} {at[c]}({at[c] / max(1, tot):.2%})' for c, _ in LAWCOLS) + f'・和 {sum(at.values())}')
    nulls = [res[n][1] for n in ('A', 'N1', 'N2') if n in res]
    nullmax = max(sum(x.values()) for x in nulls) if nulls else float('nan')
    for a in arms:
        if a.name in ('A', 'N1', 'N2'):
            continue
        s = sum(res[a.name][1].values())
        print(f'  → {a.name}：和 {s}／ヌル腕の最大 {nullmax}＝{s / max(1, nullmax):.2f} 倍（線は 2 倍以上）' + ('　★線に当たる' if s >= 2 * nullmax and nullmax > 0 else ''))


def sec_leg(arms):
    print('=== [止 ④] 正統性 ＜ 0.05 の国×四半期の比（年末の行＝全ての国が一様の重み）===')
    res = {}
    for a in arms:
        n_all = n_low = n_au = n_au_low = 0
        per_seed = []
        for s in a.seeds:
            lo = tot = 0
            for r in a.reform(s):
                if not yearend(r):
                    continue
                low = r['legitimacyShare'] < 0.05
                n_all += 1; n_low += low; lo += low; tot += 1
                if int(r['polityCode']) in AUTOCRAT:
                    n_au += 1; n_au_low += low
            per_seed.append(lo)
        res[a.name] = (n_low, n_all, n_au_low, n_au)
        print(f'  {a.name}：全ての国 {n_low}/{n_all}（{n_low / max(1, n_all):.3%}）・専制系 {n_au_low}/{n_au}（{n_au_low / max(1, n_au):.3%}）・種ごとの件数 {per_seed}')
    nm = max((res[n][2] / max(1, res[n][3]) for n in ('A', 'N1', 'N2') if n in res), default=float('nan'))
    for a in arms:
        if a.name in ('A', 'N1', 'N2'):
            continue
        r = res[a.name][2] / max(1, res[a.name][3])
        print(f'  → {a.name}：専制系の比 {r:.3%}／ヌル腕の最大 {nm:.3%}＝{r / nm if nm > 0 else float("inf"):.2f} 倍（線は 2 倍以上）' + ('　★線に当たる' if (nm > 0 and r >= 2 * nm) or (nm == 0 and r > 0.005) else ''))


def sec_col(arms):
    print('=== [止 ⑥①] 同じ国が毎回壊れる：最大人口 5000 千人以上の国が、最大から最後の人口が半分未満になった種の数（国名ごと・8/10 以上が線）===')
    for a in arms:
        cnt = collections.Counter()
        for s in a.seeds:
            by = collections.defaultdict(list)
            for r in a.nat(s):
                by[r['nationUid']].append(r)
            for uid, rows in by.items():
                rows.sort(key=lambda r: r['quarter'])
                mx = max(r['populationK'] for r in rows)
                if mx >= 5000 and rows[-1]['quarter'] >= 600 and rows[-1]['populationK'] < 0.5 * mx:
                    cnt[rows[-1]['nation']] += 1
            a.drop(s)
        top = cnt.most_common(6)
        print(f'  {a.name}：' + ('・'.join(f'{n} {c}/{len(a.seeds)}' for n, c in top) if top else '無し') + ('　★8/10 以上あり' if any(c >= 8 for _, c in top) else ''))


def sec_top(arms):
    print('=== [止 ⑥②] 一国の発散：最終の四半期の世界の実質GDPに占める首位の取り分（種ごと）===')
    res = {}
    for a in arms:
        L = []
        for s in a.seeds:
            rows = a.nat(s)
            qmax = max(r['quarter'] for r in rows)
            last = [r for r in rows if r['quarter'] == qmax and fin(r['realGdpPerYear'])]
            tot = sum(r['realGdpPerYear'] for r in last)
            if tot > 0 and last:
                b = max(last, key=lambda r: r['realGdpPerYear'])
                L.append((b['nation'], b['realGdpPerYear'] / tot))
            a.drop(s)
        res[a.name] = L
        c = collections.Counter(n for n, _ in L)
        print(f'  {a.name}：首位 {dict(c)}・取り分の中央値 {med([x for _, x in L]):.3f}・最大 {max((x for _, x in L), default=float("nan")):.3f}・種ごと ' + ' '.join(f'{n}{x:.2f}' for n, x in L))
    nm = [med([x for _, x in res[n]]) for n in ('A', 'N1', 'N2') if n in res]
    if nm:
        lo, hi = min(nm), max(nm)
        for a in arms:
            if a.name in ('A', 'N1', 'N2'):
                continue
            m = med([x for _, x in res[a.name]])
            same = max(collections.Counter(n for n, _ in res[a.name]).values(), default=0)
            print(f'  → {a.name}：中央値 {m:.3f}／ヌル腕の中央値の幅 [{lo:.3f}, {hi:.3f}]・上限+2×幅 {hi + 2 * (hi - lo):.3f}・同じ国が首位の種 {same}/10' + ('　★線に当たる' if m > hi + 2 * (hi - lo) and same >= 8 else ''))


def sec_r1(arms):
    print('=== [記 R1] 法の動き：種×国で法の段が最初に上がった年の中央値（挙げた 8 国・動いた種×国の数／全体）===')
    for a in arms:
        print(f'  -- {a.name}')
        for c, mx in LAWCOLS:
            line = []
            for nm in LISTED:
                ys, tot, moved = [], 0, 0
                for s in a.seeds:
                    rows = [r for r in a.reform(s) if listed_name(r['nation']) == nm]
                    rows.sort(key=lambda r: r['quarter'])
                    if not rows:
                        continue
                    tot += 1
                    base = int(rows[0][c])
                    for r in rows:
                        if int(r[c]) > base:
                            ys.append(r['year']); moved += 1
                            break
                line.append(f'{nm}:{moved}/{tot}' + (f'@{med(ys):.0f}' if ys else ''))
            print(f'    {c[:-7]:12s} ' + ' '.join(line))


def sec_r2(arms):
    print('=== [記 R2] 正統性：専制系の年末の行の中央値／5% 点（1823・1850・1900・1928 年）と、挙げた 8 国の開幕（q=3,4,8,12,20,40・計器の最初の行は q=3）の平均 ===')
    for a in arms:
        out = []
        for y in (1823, 1850, 1900, 1928):
            v = [r['legitimacyShare'] for s in a.seeds for r in a.reform(s) if yearend(r) and int(r['year']) == y and int(r['polityCode']) in AUTOCRAT]
            out.append(f'{y}:{med(v):.3f}/{quant(v, 0.05):.3f}(n={len(v)})')
        op = []
        for q in (3, 4, 8, 12, 20, 40):
            v = [r['legitimacyShare'] for s in a.seeds for r in a.reform(s) if int(r['quarter']) == q and listed_name(r['nation'])]
            op.append(f'q{q}:{sum(v) / max(1, len(v)):.3f}')
        print(f'  {a.name}：' + ' '.join(out) + '｜開幕 ' + ' '.join(op))


def sec_r3(arms):
    print('=== [記 R3] 政体の遷移（nation_s の polityCode が前の四半期から変わった数・国の個体ごと）と軍政への入り・q≤40 と q＞40 ===')
    for a in arms:
        tot = [0, 0]; mil = [0, 0]; dist = collections.Counter(); ng = 0
        for s in a.seeds:
            by = collections.defaultdict(list)
            for r in a.nat(s):
                by[r['nationUid']].append(r)
            for uid, rows in by.items():
                rows.sort(key=lambda r: r['quarter'])
                for p, c in zip(rows, rows[1:]):
                    if fin(p['polityCode']) and fin(c['polityCode']) and p['polityCode'] != c['polityCode']:
                        k = 0 if c['quarter'] <= 40 else 1
                        tot[k] += 1
                        if int(c['polityCode']) == 7:
                            mil[k] += 1
                if rows[-1]['quarter'] >= 600 and fin(rows[-1]['polityCode']):
                    dist[int(rows[-1]['polityCode'])] += 1
            a.drop(s)
        print(f'  {a.name}：遷移 q≤40 {tot[0]}・q＞40 {tot[1]}｜軍政への入り q≤40 {mil[0]}・q＞40 {mil[1]}｜最終の政体の分布 ' + ' '.join(f'{POLITY[k]}{v}' for k, v in sorted(dist.items())))


def sec_r4(arms):
    print('=== [記 R4] 識字（清・ロシア・オスマン）の中央値：1823／1900／1950 年の年末 ===')
    for a in arms:
        out = []
        for nm in ('清', 'ロシア', 'オスマン'):
            ys = []
            for y in (1823, 1900, 1950):
                v = [r['literacyShare'] for s in a.seeds for r in a.reform(s) if yearend(r) and int(r['year']) == y and nm in r['nation']]
                ys.append(f'{med(v):.3f}')
            out.append(f'{nm} ' + '/'.join(ys))
        print(f'  {a.name}：' + '｜'.join(out))


def runs(flags):
    out, n = [], 0
    for f in flags:
        if f:
            n += 1
        elif n:
            out.append(n); n = 0
    if n:
        out.append(n)
    return out


def sec_r5(arms):
    print('=== [記 R5/R9/R1] 壁の一歩・凍結（S＞b）・資本の門で止まった数：挙げた 8 国の毎四半期の行 ===')
    for a in arms:
        law = sli = early = late = 0; axes = collections.Counter(); blkL = blkS = blkF = 0; rows_n = frozen_n = 0; lens = []
        first_after = []   # 開幕後（q＞4）の壁の一歩の間隔（種×国）
        clamp0 = 0
        for s in a.seeds:
            by = collections.defaultdict(list)
            for r in a.reform(s):
                nm = listed_name(r['nation'])
                if nm:
                    by[nm].append(r)
            for nm, rows in by.items():
                rows.sort(key=lambda r: r['quarter'])
                last_q = None
                for r in rows:
                    w = int(r['wallPassCount'])
                    law += int(r['wallPassLawCount']); sli += int(r['wallPassSliderCount'])
                    if int(r['quarter']) <= 4:
                        early += w
                    else:
                        late += w
                    if w:
                        for ax in str(r['wallPassAxes']).split(','):
                            axes[ax] += 1
                        if r['legitimacyShare'] <= 0:
                            clamp0 += 1
                        if int(r['quarter']) > 4:
                            if last_q is not None:
                                first_after.append(int(r['quarter']) - last_q)
                            last_q = int(r['quarter'])
                    if int(r['quarter']) > 3:   # 最初の行（q=3）は助走を含む累積なので資本の門で止まった数には数えない
                        blkL += int(r['blockedLawCount']); blkS += int(r['blockedSliderCount']); blkF += int(r['blockedWhileFrozenCount'])
                    rows_n += 1
                    frozen_n += r['mandateStockIndex'] > r['mandateMaxIndex'] + 1e-12
                lens += runs([r['mandateStockIndex'] > r['mandateMaxIndex'] + 1e-12 for r in rows])
        print(f'  {a.name}：壁の一歩 法 {law}・スライダー {sli}（q≤4 {early}・q＞4 {late}）・軸 {dict(axes.most_common(8))}・開幕後の間隔の中央値 {med(first_after):.0f}(n={len(first_after)})・同じ行で正統性 0 の壁の一歩 {clamp0}')
        print(f'      凍結（S＞b）の行 {frozen_n}/{rows_n}（{frozen_n / max(1, rows_n):.2%}）・連続の長さ 中央値 {med(lens):.0f}・90% 点 {quant(lens, 0.9):.0f}・最大 {max(lens, default=0)}・数 {len(lens)}｜資本の門で止まった 法 {blkL}・スライダー {blkS}・うち凍結で {blkF}')


def sec_r6(arms):
    print('=== [記 R6/追記2 高] S の読み手：挙げた 8 国の毎四半期の行の平均（q≤40 と q＞40）：militaryPushIndex・coopParliamentCrownIndex・min(1,S)・凍結の割合 ===')
    for a in arms:
        acc = {0: collections.defaultdict(list), 1: collections.defaultdict(list)}
        for s in a.seeds:
            for r in a.reform(s):
                if not listed_name(r['nation']):
                    continue
                k = 0 if r['quarter'] <= 40 else 1
                acc[k]['push'].append(r['militaryPushIndex']); acc[k]['coop'].append(r['coopParliamentCrownIndex'])
                acc[k]['S'].append(min(1.0, r['mandateStockIndex'])); acc[k]['b'].append(r['mandateMaxIndex'])
        f = lambda k, n: sum(x for x in acc[k][n] if fin(x)) / max(1, sum(1 for x in acc[k][n] if fin(x)))
        print(f'  {a.name}：q≤40 push {f(0, "push"):.4f}・coop {f(0, "coop"):.4f}・min(1,S) {f(0, "S"):.4f}・b {f(0, "b"):.4f}｜q＞40 push {f(1, "push"):.4f}・coop {f(1, "coop"):.4f}・min(1,S) {f(1, "S"):.4f}・b {f(1, "b"):.4f}')


def sec_r8(arms):
    print('=== [記 R8] 英・仏（議会・民主の国で壁が当たっていないはず）：壁の一歩の数と最終の法の段 ===')
    for a in arms:
        wp = collections.Counter(); fin_codes = collections.defaultdict(collections.Counter)
        for s in a.seeds:
            rows = [r for r in a.reform(s) if listed_name(r['nation']) in ('イギリス', 'フランス')]
            for r in rows:
                wp[listed_name(r['nation'])] += int(r['wallPassCount'])
            rows.sort(key=lambda r: r['quarter'])
            for nm in ('イギリス', 'フランス'):
                rr = [r for r in rows if listed_name(r['nation']) == nm]
                if rr:
                    fin_codes[nm][tuple(int(rr[-1][c]) for c, _ in LAWCOLS)] += 1
        print(f'  {a.name}：壁の一歩 {dict(wp)}・最終の法の段 ' + ' '.join(f'{nm}:' + ','.join(f'{"".join(map(str, k))}×{v}' for k, v in c.most_common(3)) for nm, c in fin_codes.items()))


def sec_d2(arms):
    print('=== [記 R4/D2] 教区学校への国庫の購入：educationBuyPerMonth の和（挙げた 8 国の行）と parishFundingShare が正の行・教育の法が Religious(1) の行 ===')
    for a in arms:
        buy = collections.Counter(); pos = rel = n = 0
        for s in a.seeds:
            for r in a.reform(s):
                nm = listed_name(r['nation'])
                if not nm:
                    continue
                n += 1
                buy[nm] += r['educationBuyPerMonth'] if fin(r['educationBuyPerMonth']) else 0
                pos += r['parishFundingShare'] > 0
                rel += int(r['educationLawCode']) == 1
        print(f'  {a.name}：行 {n}・Religious の行 {rel}・財源が立つ行 {pos}・購入の和 ' + ' '.join(f'{k}:{v:.1f}' for k, v in buy.most_common(8)))


SECS = {'lock': sec_lock, 'leg': sec_leg, 'col': sec_col, 'top': sec_top, 'r1': sec_r1, 'r2': sec_r2, 'r3': sec_r3, 'r4': sec_r4, 'r5': sec_r5, 'r6': sec_r6, 'r8': sec_r8, 'd2': sec_d2}


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    arms, secs = [], list(SECS)
    for x in sys.argv[1:]:
        k, _, v = x.partition('=')
        if k == 'sec':
            secs = v.split(',')
        else:
            arms.append(Arm(k, v))
    for s in secs:
        SECS[s](arms)


if __name__ == '__main__':
    main()
