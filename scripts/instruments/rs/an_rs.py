"""(399) 支配の4区分の読み手：rule_s（国×四半期×州）。事前登録 measurements/rs_20261018_prereg.txt の [5] 記録の線 R1〜R8。

使い方：PYTHONUTF8=1 python -I scripts/instruments/rs/an_rs.py <計器の出力フォルダ（rule_s*.tsv がある所）> [出力ファイル]
読むだけ。列の定義＝SimCore/InstrumentsRule.cs。標準ライブラリだけ・ファイルはバイトで読み UTF-8 で解く。採否には使わない（記録の線）。

出力：
 粒度の自己申告：挙げた国（NATIONS）は毎四半期・他の国は年末だけ。年の比べには年末の行（quarter%4==3）だけを使う。
 R1 税÷GDP：英・清ほかの revenuePerMonth÷gdpPerMonth を 1800・1850・1900・1940 年の年末で（種の中央値）。
    ⚠GdpPerMonth は中間財を重ねる総産出で付加価値ではない・revenue は予算の基数（純税収＋間接税）＝史実の税÷GDP とは定義が違う（README §1-9）。
    ⚠設計 doc §1 の史実の数字（清 2〜3%・英 10% 超）は出典（著者・統計名）と分母（付加価値か総産出か）が未確認＝手元で集める物。この表は参考の記録で Q1 の判断には使わない。
 R2 国家区分の人口加重：英と、分割ポーランドの併合州（ダンツィヒ・レンベルク）を持つ国の国全体・併合州そのもの。1790・1800・1810 年。
 R3 外国の区分：ruleForeignShare＞0 の（国・州）の初年・終年・最大・出た種の数。旧の個数（treatyPortsGrantedCount＞0）の初年・種の数も並べる。
    （追記2）外国持分の寿命：持分が 0.25（付与 0.5 の半分＝treatyPortInForceFlag の線）以上だった連続の年数の分布（国別）。清の史実の窓（約 100 年）と比べるための記録。
 R4 在地の区分が農奴解放の年で消えないこと：serfLawCode が上がった四半期の前後 8 四半期の国全体の在地の区分の人口加重（挙げた国のみ）。
 R5 enforce：stateReachIndex（⑤b の値）と min(1, adminEfficiencyIndex÷0.5)（旧の門の値）の 20 年ごとの中央値。（追記2）本国の州だけの重み（stateReachHomeIndex）を並べる。
    児童労働の禁止の浸透（scalars_s の childLaborLegal：1＝合法 → 0）が 0.99 を割った年と 0.1 以下（約 90% 浸透）に届いた年・その間の年数（国別・種の中央値）。
 R6 仲介者の取り分：ruleCutPerMonth（納税者→州の貴族）÷revenuePerMonth を 1800・1850・1900・1940 年（年末・種の中央値）。（追記2）
 R10 区分の動き（追記2 ①・追記3 で R9 から振り直し）：国ごとの国家区分（人口加重）の四半期差の |Δ| の最大と、その四半期の原因の札（法の段が動いた・外国持分が動いた・州の構成が変わった・治安が動いた）。
    intermediaryDegreeIndex（D1 の仲介者の権利の残高）が 1.0 に張り付いている四半期の割合（国別・max の形が拘束しているか）。
 R11 徴兵の母数の掛け率と公共財の到達（追記3 ②）／R12 関税の倍率（追記3 ⑤）は rule_s の既存の列から組む。
 R7 英領インドの 3 州（中核ベンガル・内陸ベンガル・マドラス）の 4 区分の時系列（1790・1830・1870・1910・1940）。
 R8 無統治の区分の分布：挙げた国の州ごとの平均（上位 12）。
"""
import glob
import statistics
import sys
from collections import defaultdict

TARGET_YEARS = (1800, 1850, 1900, 1940)
PARTITION = ('ダンツィヒ', 'レンベルク')
INDIA = ('中核ベンガル', '内陸ベンガル', 'マドラス')


def tsv(path):
    with open(path, 'rb') as fh:
        head = fh.readline().decode('utf-8').rstrip('\r\n').split('\t')
        for raw in fh:
            cols = raw.decode('utf-8').rstrip('\r\n').split('\t')
            if len(cols) == len(head):
                yield dict(zip(head, cols))


def f(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def med(v):
    v = [x for x in v if x == x]
    return statistics.median(v) if v else float('nan')


def wavg(rows, idx, only=None, home=False):
    """人口加重の平均。only＝州名の集合・home＝植民地格の州を除く（本国の州だけ）。"""
    num = den = 0.0
    for t in rows:
        if only and t[0] not in only:
            continue
        if home and t[6] == 'Colonial':
            continue
        if t[1] > 0 and t[idx] == t[idx]:
            num += t[1] * t[idx]
            den += t[1]
    return num / den if den > 0 else float('nan')


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return
    sys.stdout.reconfigure(encoding='utf-8')   # python -I は PYTHONUTF8 を無視する＝Windows の既定の cp932 に化けない
    out = open(sys.argv[2], 'w', encoding='utf-8') if len(sys.argv) > 2 else sys.stdout
    P = lambda *a: print(*a, file=out)
    files = sorted(glob.glob(sys.argv[1].rstrip('/\\') + '/rule_s*.tsv'))
    P('== (399) an_rs.py 読み手・ファイル %d 本 ==' % len(files))
    nat_rows = {}                       # (seed, q, nation) -> 国の列を持つ最初の行
    reg = defaultdict(list)             # (seed, q, nation) -> [(region, popK, st, lo, fo, un)]
    foreign = {}                        # (nation, region) -> [firstYear, lastYear, max, set(seeds)]
    port_old = {}                       # nation -> [firstYear, set(seeds)]
    serf = {}                           # (seed, nation) -> [(q, serfLawCode)]
    ungov = defaultdict(list)           # (nation, region) -> [ungov]
    india = defaultdict(list)           # (year, region) -> [(st, lo, fo, un)]
    fser = defaultdict(list)            # (seed, nation, region) -> [(q, foreignShare)]（R3 の寿命）
    locq = defaultdict(dict)            # (seed, nation, region) -> {q: localShare}（R4 の州別）
    stq = defaultdict(dict)             # (seed, nation) -> {q: 国家区分の人口加重}（R9）
    lawq = {}                           # (seed, q, nation) -> (serf, bur, land, order)（R9）
    interm = defaultdict(list)          # nation -> [(seed, q, intermediaryDegreeIndex)]（R9）
    qcount = defaultdict(int)
    for si, p in enumerate(files):
        for r in tsv(p):
            q = int(r['quarter'])
            y = int(r['year'])
            n = r['nation']
            rg = r['region']
            key = (si, q, n)
            if key not in nat_rows:
                nat_rows[key] = r
                lawq[key] = (r['serfLawCode'], r['bureaucracyLawCode'], r['landReformLawCode'], f(r['publicOrderIndex']))
                interm[n].append((si, q, f(r['intermediaryDegreeIndex'])))
                qcount[(si, n)] += 1
                s = serf.setdefault((si, n), [])
                if not s or s[-1][1] != r['serfLawCode']:
                    s.append((q, r['serfLawCode']))
            st, lo, fo, un = f(r['ruleStateShare']), f(r['ruleLocalShare']), f(r['ruleForeignShare']), f(r['ruleUngovShare'])
            reg[key].append((rg, f(r['popK']), st, lo, fo, un, r['tier']))
            if fo > 1e-9:
                e = foreign.setdefault((n, rg), [y, y, 0.0, set()])
                e[0] = min(e[0], y)
                e[1] = max(e[1], y)
                e[2] = max(e[2], fo)
                e[3].add(si)
            if r.get('treatyPortsGrantedCount', '0') != '0':   # (399) の採用で列を消した＝新しい出力には無い（旧の腕 A〔measurements/rs399_20261018_*〕の出力にはある）
                e = port_old.setdefault(n, [y, set()])
                e[0] = min(e[0], y)
                e[1].add(si)
            ungov[(n, rg)].append(un)
            fser[(si, n, rg)].append((q, fo))
            locq[(si, n, rg)][q] = lo
            if rg in INDIA and n == 'イギリス' and q % 4 == 3:
                india[(y, rg)].append((st, lo, fo, un))
    nseed = len(files)
    maxq = max(qcount.values()) if qcount else 0
    P('粒度：毎四半期の国（行数が最大の 9 割以上）＝', sorted({n for (si, n), c in qcount.items() if c >= 0.9 * maxq}))

    # R1
    P('\n== R1 税÷GDP（revenuePerMonth÷gdpPerMonth・種の中央値・年末）⚠定義が史実と違う ==')
    names = sorted({n for (_, _, n) in nat_rows})
    focus = [n for n in ('イギリス', '清', 'フランス', 'ロシア', 'オスマン', 'プロイセン', 'オーストリア', '日本') if n in names]
    P('国\t' + '\t'.join(str(y) for y in TARGET_YEARS))
    for n in focus:
        row = []
        for y in TARGET_YEARS:
            v = [f(r['revenuePerMonth']) / f(r['gdpPerMonth']) for (si, q, nn), r in nat_rows.items()
                 if nn == n and int(r['year']) == y and q % 4 == 3 and f(r['gdpPerMonth']) > 0]
            row.append('%.4f' % med(v) if v else '-')
        P(n + '\t' + '\t'.join(row))

    # R2
    P('\n== R2 国家区分（人口加重・種の中央値）==')
    owner = {}
    for (si, q, n), rows in reg.items():
        for t in rows:
            if t[0] in PARTITION:
                owner[t[0]] = n
    P('併合州の持ち主（最後に見えた国）：', owner)
    for y in (1790, 1800, 1810):
        P('-- %d年' % y)
        for n in ['イギリス'] + sorted(set(owner.values())):
            v = [wavg(rows, 2) for (si, q, nn), rows in reg.items() if nn == n and nat_rows[(si, q, nn)]['year'] == str(y) and q % 4 == 3]
            h = [wavg(rows, 2, home=True) for (si, q, nn), rows in reg.items() if nn == n and nat_rows[(si, q, nn)]['year'] == str(y) and q % 4 == 3]
            P('  %s 国全体の国家区分	%.3f（本国の州だけ %.3f）' % (n, med(v), med(h)))
        for pr in PARTITION:
            v = [wavg(rows, 2, only={pr}) for (si, q, nn), rows in reg.items()
                 if nat_rows[(si, q, nn)]['year'] == str(y) and q % 4 == 3 and any(t[0] == pr for t in rows)]
            P('  %s 併合州の国家区分\t%.3f' % (pr, med(v)))

    # R3
    P('\n== R3 外国の区分（ruleForeignShare＞0）==')
    if not foreign:
        P('  なし')
    for (n, rg), e in sorted(foreign.items(), key=lambda kv: (-len(kv[1][3]), kv[0])):
        P('  %s %s\t初年 %d・終年 %d・最大 %.3f・出た種 %d/%d' % (n, rg, e[0], e[1], e[2], len(e[3]), nseed))
    P('外国持分の寿命（持分 0.25 以上の連続の年数・種×州ごと・国別の中央値と最大）：')
    life = defaultdict(list)
    for (si, n, rg), seq in fser.items():
        seq.sort()
        start = None
        for q, fo in seq:
            if fo >= 0.25 and start is None:
                start = q
            elif fo < 0.25 and start is not None:
                life[n].append((q - start) / 4.0)
                start = None
        if start is not None:
            life[n].append(-(seq[-1][0] - start) / 4.0)   # 負＝最後まで続いた（打ち切り）
    if not life:
        P('  なし')
    for n, v in sorted(life.items()):
        done = [x for x in v if x > 0]
        P('  %s\t完結 %d 件・中央値 %.1f 年・最大 %.1f 年／打ち切り（最後まで続いた）%d 件' % (n, len(done), med(done), max(done) if done else float('nan'), len(v) - len(done)))
    P('旧の個数（treatyPortsGrantedCount＞0）：')
    if not port_old:
        P('  なし')
    for n, e in sorted(port_old.items()):
        P('  %s\t初年 %d・出た種 %d/%d' % (n, e[0], len(e[1]), nseed))

    # R4
    P('\n== R4 農奴解放の法が上がった四半期の前後 8 四半期の在地の区分（国全体・人口加重・毎四半期の国のみ）==')
    shown = 0
    for (si, n), ch in sorted(serf.items()):
        if qcount[(si, n)] < 0.9 * maxq:
            continue
        for (q0, code), (q1, code1) in zip(ch, ch[1:]):
            if int(code1) > int(code):
                bef = [wavg(reg[(si, q, n)], 3) for q in range(q1 - 8, q1) if (si, q, n) in reg]
                aft = [wavg(reg[(si, q, n)], 3) for q in range(q1, q1 + 8) if (si, q, n) in reg]
                P('  種%d %s 四半期%d（%s年）法 %s→%s\t前 %.3f → 後 %.3f' % (si + 1, n, q1, nat_rows[(si, q1, n)]['year'], code, code1, med(bef), med(aft)))
                shown += 1
    if not shown:
        P('  該当なし')
    P('  州別（上の事象ごと：前後 4 四半期の平均の差〔後−前〕・両方に出る州のみ・差が −0.05 以下の州の数と最小）：')
    for (si, n), ch in sorted(serf.items()):
        if qcount[(si, n)] < 0.9 * maxq:
            continue
        for (q0, code), (q1, code1) in zip(ch, ch[1:]):
            if int(code1) > int(code):
                ds = []
                for (s2, n2, rg), d in locq.items():
                    if s2 != si or n2 != n:
                        continue
                    bf = [d[q] for q in range(q1 - 4, q1) if q in d]
                    af = [d[q] for q in range(q1, q1 + 4) if q in d]
                    if bf and af:
                        ds.append(statistics.fmean(af) - statistics.fmean(bf))
                if ds:
                    P('    種%d %s 四半期%d\t州 %d・差が −0.05 以下 %d・最小 %.3f' % (si + 1, n, q1, len(ds), sum(1 for x in ds if x <= -0.05), min(ds)))

    # R5
    P('\n== R5 enforce：stateReachIndex（新）と min(1, adminEfficiencyIndex÷0.5)（旧）・20 年ごとの中央値 ==')
    for n in focus:
        row = []
        for dec in range(1790, 1950, 20):
            a = [f(r['stateReachIndex']) for (si, q, nn), r in nat_rows.items() if nn == n and dec <= int(r['year']) < dec + 20]
            o = [min(1.0, f(r['adminEfficiencyIndex']) / 0.5) for (si, q, nn), r in nat_rows.items() if nn == n and dec <= int(r['year']) < dec + 20]
            row.append('%d:%.2f/%.2f' % (dec, med(a), med(o)))
        P('  %s\t新/旧 ' % n + ' '.join(row))
    P('  本国の州だけの重み（stateReachHomeIndex）の 20 年ごとの中央値（植民地込み／本国だけ）：')
    for n in focus:
        row = []
        for dec in range(1790, 1950, 20):
            a = [f(r['stateReachIndex']) for (si, q, nn), r in nat_rows.items() if nn == n and dec <= int(r['year']) < dec + 20]
            h = [f(r.get('stateReachHomeIndex', 'nan')) for (si, q, nn), r in nat_rows.items() if nn == n and dec <= int(r['year']) < dec + 20]
            row.append('%d:%.2f/%.2f' % (dec, med(a), med(h)))
        P('  %s\t込み/本国 ' % n + ' '.join(row))
    # 児童労働の禁止の浸透（scalars_s）
    P('  児童労働の浸透（childLaborLegal：1＝合法→0）：0.99 を割った年／0.1 以下に届いた年／間の年数（種の中央値・割った種の数・届いた種の数）：')
    cl = defaultdict(lambda: defaultdict(list))
    for sp in sorted(glob.glob(sys.argv[1].rstrip('/\\') + '/scalars_s*.tsv')):
        si = int(''.join(ch for ch in sp.rsplit('scalars_s', 1)[1] if ch.isdigit()) or 0)
        for r in tsv(sp):
            cl[r['nation']][si].append((int(r['quarter']), int(r['year']), f(r['childLaborLegal'])))
    for n in focus:
        y1s, y2s, gaps = [], [], []
        for si, seq in cl.get(n, {}).items():
            seq.sort()
            a = next((y for q, y, v in seq if v < 0.99), None)
            c2 = next((y for q, y, v in seq if v <= 0.1), None)
            if a is not None:
                y1s.append(a)
                if c2 is not None:
                    y2s.append(c2)
                    gaps.append(c2 - a)
        P('    %s\t%s／%s／%s（割った種 %d・届いた種 %d）' % (n, '%.0f' % med(y1s) if y1s else '-', '%.0f' % med(y2s) if y2s else '-', '%.0f' % med(gaps) if gaps else '-', len(y1s), len(y2s)))

    # R6
    P('\n== R6 仲介者の取り分（ruleCutPerMonth÷revenuePerMonth・年末・種の中央値）==')
    P('国\t' + '\t'.join(str(y) for y in TARGET_YEARS))
    for n in focus:
        row = []
        for y in TARGET_YEARS:
            v = [f(r['ruleCutPerMonth']) / f(r['revenuePerMonth']) for (si, q, nn), r in nat_rows.items()
                 if nn == n and int(r['year']) == y and q % 4 == 3 and f(r['revenuePerMonth']) > 0]
            row.append('%.4f' % med(v) if v else '-')
        P(n + '\t' + '\t'.join(row))

    # R9
    P('\n== R10 区分の動き（追記2 ①・追記3 で R9 から振り直し）==')
    P('国ごとの国家区分（人口加重）の四半期差 |Δ| の最大（毎四半期の国のみ・種ごとの最大の中央値と全種の最大・最大の四半期の札）：')
    for (si, q, n), rows in reg.items():
        if qcount[(si, n)] >= 0.9 * maxq:
            stq[(si, n)][q] = wavg(rows, 2)
    big = defaultdict(list)
    for (si, n), d in stq.items():
        qs = sorted(d)
        best = None
        for a, c2 in zip(qs, qs[1:]):
            if c2 - a != 1 or d[a] != d[a] or d[c2] != d[c2]:
                continue
            dv = abs(d[c2] - d[a])
            if best is None or dv > best[0]:
                best = (dv, c2)
        if best:
            dv, c2 = best
            la, lb = lawq.get((si, c2 - 1, n)), lawq.get((si, c2, n))
            tags = []
            if la and lb:
                if la[:3] != lb[:3]:
                    tags.append('法の段が動いた')
                if abs(la[3] - lb[3]) > 0.05:
                    tags.append('治安が動いた')
            fa = sum(t[4] * t[1] for t in reg[(si, c2 - 1, n)]) if (si, c2 - 1, n) in reg else 0
            fb = sum(t[4] * t[1] for t in reg[(si, c2, n)]) if (si, c2, n) in reg else 0
            if abs(fb - fa) > 1e-6 * (1 + abs(fa)):
                tags.append('外国持分が動いた')
            na = {t[0] for t in reg.get((si, c2 - 1, n), [])}
            nb = {t[0] for t in reg.get((si, c2, n), [])}
            if na != nb:
                tags.append('州の構成が変わった')
            big[n].append((dv, c2, nat_rows[(si, c2, n)]['year'], '・'.join(tags) or '札なし（治安と到達の連続の動き）'))
    for n in focus:
        v = big.get(n, [])
        if v:
            top = max(v)
            P('  %s\t中央値 %.3f・最大 %.3f（%s年・%s）' % (n, med([x[0] for x in v]), top[0], top[2], top[3]))
    P('仲介者の権利の残高（intermediaryDegreeIndex）が 1.0 の四半期の割合（毎四半期の国・年代別。1.0 のまま＝max の形が拘束していない）：')
    for n in focus:
        row = []
        for dec in range(1790, 1950, 20):
            v = [x for (si, q, x) in interm.get(n, []) if qcount[(si, n)] >= 0.9 * maxq and dec <= 1783 + (q + 1) // 4 < dec + 20 and x == x]
            row.append('%d:%.2f' % (dec, sum(1 for x in v if x > 0.9999) / len(v)) if v else '%d:-' % dec)
        P('  %s\t' % n + ' '.join(row))

    # R7
    P('\n== R7 英領インドの 3 州（国家/在地/外国/無統治・種の中央値）==')
    for rg in INDIA:
        for y in (1790, 1830, 1870, 1910, 1940):
            v = india.get((y, rg), [])
            if v:
                P('  %s %d\t%s' % (rg, y, '/'.join('%.2f' % med([t[i] for t in v]) for i in range(4))))

    # R8
    P('\n== R8 無統治の区分の平均（挙げた国の州・上位 12）==')
    rows = [(statistics.fmean(v), n, rg, len(v)) for (n, rg), v in ungov.items() if n in focus and v]
    for m, n, rg, c in sorted(rows, reverse=True)[:12]:
        P('  %s %s\t平均 %.3f（行 %d）' % (n, rg, m, c))
    # R11（追記3）徴兵の母数と公共財の到達：rule_s の既存の列から読み手が組む（コードを変えない）。B（RuleSharesTaxBase=1・RuleSharesReach=1）の式のまま。
    #   徴兵の母数（Military.RegionManpower の掛け率）＝国家区分＋在地区分×(1−PrivilegeTaxDiscount=0.7)＝Politics.RuleDelivery(g=1)・公共財の到達＝国家区分＝Politics.RuleReach(g=1)。
    #   A（旗 0）では実際の掛け率は 1.0（この表の値は B でだけ意味がある）。popK で人口加重（兵の母数の重みは徴募元の男＝人口に比例とみなす近似）。
    P('\n== R11 徴兵の母数の掛け率（国家区分＋在地区分×0.7）／公共財の到達（国家区分）・人口加重・20 年ごとの種の中央値（B の式。A では実際は 1.0）==')
    for n in focus:
        row = []
        for dec in range(1790, 1950, 20):
            dv, rc = [], []
            for (si, q, nn), rows in reg.items():
                if nn != n or qcount[(si, n)] < 0.9 * maxq or not (dec <= 1783 + (q + 1) // 4 < dec + 20) or q % 4 != 3:
                    continue
                den = sum(t[1] for t in rows if t[1] > 0)
                if den > 0:
                    dv.append(sum(t[1] * min(1.0, t[2] + t[3] * 0.7) for t in rows if t[1] > 0) / den)
                    rc.append(sum(t[1] * t[2] for t in rows if t[1] > 0) / den)
            row.append('%d:%.2f/%.2f' % (dec, med(dv), med(rc)) if dv else '%d:-' % dec)
        P('  %s\t' % n + ' '.join(row))

    # R12（追記3）関税の倍率：A＝1−0.25×treatyPortsGrantedCount（旧の個数式）・B＝1−portForeignShare（港州の外国持分の人口加重）。倍率が 1 未満の四半期の割合と最小。
    P('\n== R12 関税の倍率（挙げた国・毎四半期の国・A は 1−0.25×個数・B は 1−portForeignShare・1 未満の四半期の割合/最小）==')
    for n in focus:
        a, b = [], []
        for (si, q, nn), r in nat_rows.items():
            if nn != n or qcount[(si, n)] < 0.9 * maxq:
                continue
            a.append(max(0.0, 1.0 - 0.25 * f(r.get('treatyPortsGrantedCount', '0'))))
            b.append(1.0 - f(r['portForeignShare']))
        fm = lambda v: ('%.3f/%.2f' % (sum(1 for x in v if x < 0.9999) / len(v), min(v))) if v else '-'
        P('  %s\tA式 %s\tB式 %s' % (n, fm(a), fm(b)))
    if out is not sys.stdout:
        out.close()


if __name__ == '__main__':
    main()
