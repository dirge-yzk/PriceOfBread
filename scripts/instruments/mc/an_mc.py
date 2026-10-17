"""(393) 軍の力の入口の読み手：coupgate_s（Politics.Transition の第 1 の門を通った行）と scalars_s の polityCode の遷移。

使い方：PYTHONUTF8=1 python -I scripts/instruments/mc/an_mc.py <計器の出力フォルダ（coupgate_s*.tsv・scalars_s*.tsv がある所）> [出力ファイル]
読むだけ。列の定義＝SimCore/InstrumentsCoup.cs（coupgate_s）・SimCore/InstrumentsScalars.cs（scalars_s の polityCode・atWar）。事前登録 measurements/mc_20261018_prereg.txt の [5]（R1・R3・R5）。

出力：
 R5 coupgate_s：行数・a の四分位（中央値）・a<0.5 の割合・soldierW=0（縁）の割合・passOld／passSplit の 2×2（旗 0 の表なら〈今、分けが入ったら門 2 がどう動くか〉）・現政体別と引力点別の a の中央値・平時／戦時別（scalars_s の atWar を同じ国×年で引く）
 R1 軍政への流入：scalars_s の polityCode が変わった四半期の（前→後）の数。軍政（7）への流入は前の政体別に。絶対（0）・専制（1）・幕府（2）からの内数を別に出す
 R3 政体の往復：同じ国で 20 年（80 四半期）以内に A→B→A となった数。総政変の数
標準ライブラリだけ・ファイルはバイトで読み UTF-8 で解く。
"""
import glob
import statistics
import sys
from collections import Counter, defaultdict

NAMES = ['絶対', '専制', '幕府', '立憲', '共和', 'ブルジョア独裁', 'プロレタリア独裁', '軍政', '無政府']
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


def main():
    folder = sys.argv[1]
    out = []
    # ---- R5 coupgate_s ----
    war = {}
    for path in sorted(glob.glob(folder + '/scalars_s*.tsv')):
        seed = path.rsplit('_s', 1)[-1].split('.')[0]
        for r in tsv(path):
            war[(seed, r['nation'], r['quarter'])] = r['atWar']
    a_all, by_cur, by_att, by_war = [], defaultdict(list), defaultdict(list), defaultdict(list)
    n = edge = 0
    cross = Counter()
    for path in sorted(glob.glob(folder + '/coupgate_s*.tsv')):
        seed = path.rsplit('_s', 1)[-1].split('.')[0]
        for r in tsv(path):
            a = float(r['a'])
            n += 1
            a_all.append(a)
            if float(r['soldierW']) == 0:
                edge += 1
            by_cur[int(r['polity'])].append(a)
            by_att[int(r['attractor'])].append(a)
            by_war[war.get((seed, r['nation'], r['quarter']), '?')].append(a)
            cross[(int(r['passOld']), int(r['passSplit']))] += 1
    out.append('## R5 coupgate_s（第 1 の門を通った国×四半期）')
    out.append('行数 %d／a の分布 %s／a<0.5 の割合 %s／soldierW=0（縁）の割合 %s' % (
        n, quantiles(a_all), '%.3f' % (sum(1 for a in a_all if a < 0.5) / n) if n else '-', '%.3f' % (edge / n) if n else '-'))
    out.append('passOld×passSplit（行=旧の門 2 を通る／列=分けた門 2 を通る）：00=%d 01=%d 10=%d 11=%d' % (cross[(0, 0)], cross[(0, 1)], cross[(1, 0)], cross[(1, 1)]))
    out.append('現政体別の a：' + '；'.join('%s n=%d %s' % (NAMES[k], len(v), quantiles(v)) for k, v in sorted(by_cur.items())))
    out.append('引力点別の a：' + '；'.join('%s n=%d %s' % (NAMES[k], len(v), quantiles(v)) for k, v in sorted(by_att.items())))
    out.append('平時(0)／戦時(1)別の a：' + '；'.join('atWar=%s n=%d %s' % (k, len(v), quantiles(v)) for k, v in sorted(by_war.items())))
    # ---- R1・R3 scalars_s ----
    flows = Counter()
    into_md = Counter()
    rev = total = 0
    seeds_seen = set()
    for path in sorted(glob.glob(folder + '/scalars_s*.tsv')):
        seed = path.rsplit('_s', 1)[-1].split('.')[0]
        seeds_seen.add(seed)
        per = defaultdict(list)
        for r in tsv(path):
            per[r['nation']].append((int(r['quarter']), int(r['year']), int(r['polityCode'])))
        for nation, rows in per.items():
            rows.sort()
            changes = []
            for (q0, y0, p0), (q1, y1, p1) in zip(rows, rows[1:]):
                if p1 != p0:
                    flows[(p0, p1)] += 1
                    total += 1
                    changes.append((q1, p0, p1))
                    if p1 == MD:
                        into_md[p0] += 1
            for (qa, pa0, pa1), (qb, pb0, pb1) in zip(changes, changes[1:]):
                if pb1 == pa0 and qb - qa <= 80:
                    rev += 1
    out.append('## R1 政体の遷移（scalars_s・種 %d）' % len(seeds_seen))
    out.append('総政変 %d' % total)
    out.append('軍政への流入 %d（前の政体別：%s）／うち絶対・専制・幕府から %d' % (
        sum(into_md.values()), '，'.join('%s=%d' % (NAMES[k], v) for k, v in sorted(into_md.items())) or '-', sum(into_md[k] for k in AUTH)))
    out.append('遷移（前→後）の数：' + '；'.join('%s→%s=%d' % (NAMES[a], NAMES[b], v) for (a, b), v in sorted(flows.items(), key=lambda kv: -kv[1])[:30]))
    out.append('## R3 往復（同じ国で 80 四半期以内に A→B→A）：%d' % rev)
    text = '\n'.join(out) + '\n'
    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as fh:
            fh.write(text.encode('utf-8'))
    sys.stdout.buffer.write(text.encode('utf-8'))


if __name__ == '__main__':
    main()
