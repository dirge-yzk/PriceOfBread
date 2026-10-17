"""(399) 支配の4区分の読み手：rule_s（国×四半期×州）。事前登録 measurements/rs_20261018_prereg.txt の [5] 記録の線 R1〜R8。

使い方：PYTHONUTF8=1 python -I scripts/instruments/rs/an_rs.py <計器の出力フォルダ（rule_s*.tsv がある所）> [出力ファイル]
読むだけ。列の定義＝SimCore/InstrumentsRule.cs。標準ライブラリだけ・ファイルはバイトで読み UTF-8 で解く。採否には使わない（記録の線）。

出力：
 粒度の自己申告：挙げた国（NATIONS）は毎四半期・他の国は年末だけ。年の比べには年末の行（quarter%4==3）だけを使う。
 R1 税÷GDP：英・清ほかの revenuePerMonth÷gdpPerMonth を 1800・1850・1900・1940 年の年末で（種の中央値）。
    ⚠GdpPerMonth は中間財を重ねる総産出で付加価値ではない・revenue は予算の基数（純税収＋間接税）＝史実の税÷GDP とは定義が違う（README §1-9）。
 R2 国家区分の人口加重：英と、分割ポーランドの併合州（ダンツィヒ・レンベルク）を持つ国の国全体・併合州そのもの。1790・1800・1810 年。
 R3 外国の区分：ruleForeignShare＞0 の（国・州）の初年・終年・最大・出た種の数。旧の個数（treatyPortsGrantedCount＞0）の初年・種の数も並べる。
 R4 在地の区分が農奴解放の年で消えないこと：serfLawCode が上がった四半期の前後 8 四半期の国全体の在地の区分の人口加重（挙げた国のみ）。
 R5 enforce：stateReachIndex（⑤b の値）と min(1, adminEfficiencyIndex÷0.5)（旧の門の値）の 20 年ごとの中央値。
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
            if r['treatyPortsGrantedCount'] != '0':
                e = port_old.setdefault(n, [y, set()])
                e[0] = min(e[0], y)
                e[1].add(si)
            ungov[(n, rg)].append(un)
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

    # R5
    P('\n== R5 enforce：stateReachIndex（新）と min(1, adminEfficiencyIndex÷0.5)（旧）・20 年ごとの中央値 ==')
    for n in focus:
        row = []
        for dec in range(1790, 1950, 20):
            a = [f(r['stateReachIndex']) for (si, q, nn), r in nat_rows.items() if nn == n and dec <= int(r['year']) < dec + 20]
            o = [min(1.0, f(r['adminEfficiencyIndex']) / 0.5) for (si, q, nn), r in nat_rows.items() if nn == n and dec <= int(r['year']) < dec + 20]
            row.append('%d:%.2f/%.2f' % (dec, med(a), med(o)))
        P('  %s\t新/旧 ' % n + ' '.join(row))

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
    if out is not sys.stdout:
        out.close()


if __name__ == '__main__':
    main()
