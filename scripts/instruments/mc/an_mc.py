"""(393) 軍の力の入口の読み手：coupgate_s（Politics.Transition の第 1 の門を通った行）と scalars_s の polityCode の遷移。

使い方：PYTHONUTF8=1 python -I scripts/instruments/mc/an_mc.py <計器の出力フォルダ（coupgate_s*.tsv・scalars_s*.tsv がある所）> [出力ファイル]
読むだけ。列の定義＝SimCore/InstrumentsCoup.cs（coupgate_s）・SimCore/InstrumentsScalars.cs（scalars_s の polityCode）。事前登録 measurements/mc_20261018_prereg.txt の [5]（R1・R3・R5）と追記3 [11]。

出力：
 粒度の自己申告：scalars_s は挙げた国（NATIONS）が毎四半期・他の国は年末だけ。行数が最大の 9 割以上の国を〈毎四半期〉、それ以外を〈年末のみ〉と数える（年末のみの国は、年の中の A→B→A が消え A→B→C が A→C に見える＝過少計数）
 R5 coupgate_s：行数・a（＝1−d）の分布・a＝1 の行の割合・守る側の高さ(2a−1)の分布と(2a−1)<0.2 の割合・passOld×passSplit の 2×2 と passSplit÷passOld の倍率・soldierMob（動員の素の量に兵士が占める割合）の分布
        ・route（第 1 の門の後の経路）の数・現政体別／引力点別／平時戦時別（coupgate_s の atWar 列）の a・soldierW＝0（兵士の思想が読めない縁）の割合
 R1 軍政への流入：scalars_s の polityCode が変わった四半期の（前→後）の数。毎四半期の国と全ての国を分けて出す。軍政（7）への流入は前の政体別。絶対（0）・専制（1）・幕府（2）からの内数も出す
 R1b 10 年ごとの総政変と軍政への流入（窓の外の政変の数を並べて読むため）
 R3 政体の往復：同じ国で 20 年（80 四半期）以内に A→B→A となった数（毎四半期の国のみ）。総政変の数
標準ライブラリだけ・ファイルはバイトで読み UTF-8 で解く。
"""
import glob
import statistics
import sys
from collections import Counter, defaultdict

NAMES = ['絶対', '専制', '幕府', '立憲', '共和', 'ブルジョア独裁', 'プロレタリア独裁', '軍政', '無政府']
ROUTES = {0: '印なし', 1: '門2で止まる', 2: '平和な漸進で止まる', 3: '首都の蜂起', 4: '下賜', 5: '暴力の即時の政変', 6: '非暴力の即時の政変', 7: '内戦中・蜂起の予約中で止まる'}
MD = 7
AUTH = (0, 1, 2)


def tsv(path):
    with open(path, 'rb') as fh:
        head = fh.readline().decode('utf-8').rstrip('\r\n').split('\t')
        for raw in fh:
            cols = raw.decode('utf-8').rstrip('\r\n').split('\t')
            if len(cols) == len(head):
                yield dict(zip(head, cols))


def quantiles(v):
    if not v:
        return '-'
    v = sorted(v)
    q = lambda p: v[min(len(v) - 1, int(p * (len(v) - 1) + 0.5))]
    return 'Q1=%.3f 中央=%.3f Q3=%.3f' % (q(0.25), statistics.median(v), q(0.75))


def frac(n, d):
    return '%.3f' % (n / d) if d else '-'


def main():
    folder = sys.argv[1]
    out = []
    # ---- R5 coupgate_s ----
    a_all, hold_all, mob_all = [], [], []
    by_cur, by_att, by_war = defaultdict(list), defaultdict(list), defaultdict(list)
    n = edge = a1 = low = 0
    cross = Counter()
    routes = Counter()
    routes_by_cur = defaultdict(Counter)
    for path in sorted(glob.glob(folder + '/coupgate_s*.tsv')):
        for r in tsv(path):
            a = float(r['a'])
            n += 1
            a_all.append(a)
            h = 2 * a - 1
            hold_all.append(h)
            if h < 0.2:
                low += 1
            if a == 1.0:
                a1 += 1
            if float(r['soldierW']) == 0:
                edge += 1
            mob_all.append(float(r['soldierMob']))
            by_cur[int(r['polity'])].append(a)
            by_att[int(r['attractor'])].append(a)
            by_war[r['atWar']].append(a)
            cross[(int(r['passOld']), int(r['passSplit']))] += 1
            routes[int(r['route'])] += 1
            routes_by_cur[int(r['polity'])][int(r['route'])] += 1
    old_n = cross[(1, 0)] + cross[(1, 1)]
    split_n = cross[(0, 1)] + cross[(1, 1)]
    out.append('## R5 coupgate_s（第 1 の門を通った国×四半期）')
    out.append('行数 %d／a(=1−d) の分布 %s／a＝1 の割合 %s／soldierW＝0（兵士の思想が読めない縁）の割合 %s' % (n, quantiles(a_all), frac(a1, n), frac(edge, n)))
    out.append('守る側の高さ(2a−1)の分布 %s／(2a−1)<0.2 の割合 %s' % (quantiles(hold_all), frac(low, n)))
    out.append('passOld×passSplit（行=旧の門 2 を通る／列=分けた門 2 を通る）：00=%d 01=%d 10=%d 11=%d／passOld の行数 %d・passSplit の行数 %d・倍率 passSplit÷passOld＝%s' % (
        cross[(0, 0)], cross[(0, 1)], cross[(1, 0)], cross[(1, 1)], old_n, split_n, '%.2f' % (split_n / old_n) if old_n else '-'))
    out.append('soldierMob（動員の素の量に兵士 POP が占める割合）の分布 %s' % quantiles(mob_all))
    out.append('現政体別の a：' + '；'.join('%s n=%d %s' % (NAMES[k], len(v), quantiles(v)) for k, v in sorted(by_cur.items())))
    out.append('引力点別の a：' + '；'.join('%s n=%d %s' % (NAMES[k], len(v), quantiles(v)) for k, v in sorted(by_att.items())))
    out.append('平時(0)／戦時(1)別の a：' + '；'.join('atWar=%s n=%d %s' % (k, len(v), quantiles(v)) for k, v in sorted(by_war.items())))
    out.append('route（第 1 の門の後の経路）：' + '；'.join('%d %s=%d' % (k, ROUTES.get(k, '?'), v) for k, v in sorted(routes.items())))
    out.append('route を現政体別に：' + '；'.join('%s[%s]' % (NAMES[c], ' '.join('%d:%d' % (k, v) for k, v in sorted(rc.items()))) for c, rc in sorted(routes_by_cur.items())))
    # ---- R1・R3 scalars_s ----
    flows_q, flows_all = Counter(), Counter()
    into_q, into_all = Counter(), Counter()
    decade_tot, decade_md = Counter(), Counter()
    rev = total_q = total_all = 0
    seeds_seen = set()
    quarterly = yearly = 0
    for path in sorted(glob.glob(folder + '/scalars_s*.tsv')):
        seed = path.rsplit('_s', 1)[-1].split('.')[0]
        seeds_seen.add(seed)
        per = defaultdict(list)
        for r in tsv(path):
            per[r['nation']].append((int(r['quarter']), int(r['year']), int(r['polityCode'])))
        mx = max((len(v) for v in per.values()), default=0)
        for nation, rows in per.items():
            rows.sort()
            is_q = len(rows) >= 0.9 * mx
            if is_q:
                quarterly += 1
            else:
                yearly += 1
            changes = []
            for (q0, y0, p0), (q1, y1, p1) in zip(rows, rows[1:]):
                if p1 != p0:
                    flows_all[(p0, p1)] += 1
                    total_all += 1
                    decade_tot[y1 // 10 * 10] += 1
                    if p1 == MD:
                        into_all[p0] += 1
                        decade_md[y1 // 10 * 10] += 1
                    if is_q:
                        flows_q[(p0, p1)] += 1
                        total_q += 1
                        changes.append((q1, p0, p1))
                        if p1 == MD:
                            into_q[p0] += 1
            for (qa, pa0, pa1), (qb, pb0, pb1) in zip(changes, changes[1:]):
                if pb1 == pa0 and qb - qa <= 80:
                    rev += 1
    out.append('## 粒度の自己申告（scalars_s）：種 %d・毎四半期の国×種 %d 本・年末のみの国×種 %d 本。年末のみの国は年の中の往復が消え A→B→C が A→C に見える（過少計数）。軍政流入と窓の国の読みは年の粒度' % (len(seeds_seen), quarterly, yearly))

    def block(title, total, flows, into):
        out.append('## %s' % title)
        out.append('総政変 %d' % total)
        out.append('軍政への流入 %d（前の政体別：%s）／うち絶対・専制・幕府から %d' % (
            sum(into.values()), '，'.join('%s=%d' % (NAMES[k], v) for k, v in sorted(into.items())) or '-', sum(into[k] for k in AUTH)))
        out.append('遷移（前→後）の数：' + '；'.join('%s→%s=%d' % (NAMES[a], NAMES[b], v) for (a, b), v in sorted(flows.items(), key=lambda kv: -kv[1])[:30]))
    block('R1 政体の遷移（毎四半期の国だけ・scalars_s）', total_q, flows_q, into_q)
    block('R1 政体の遷移（全ての国・年末のみの国は年の粒度）', total_all, flows_all, into_all)
    out.append('## R1b 10 年ごとの総政変（全ての国）／うち軍政への流入')
    out.append('；'.join('%ds=%d/%d' % (d, decade_tot[d], decade_md[d]) for d in sorted(decade_tot)))
    out.append('## R3 往復（毎四半期の国だけ・同じ国で 80 四半期以内に A→B→A）：%d' % rev)
    text = '\n'.join(out) + '\n'
    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as fh:
            fh.write(text.encode('utf-8'))
    sys.stdout.buffer.write(text.encode('utf-8'))


if __name__ == '__main__':
    main()
