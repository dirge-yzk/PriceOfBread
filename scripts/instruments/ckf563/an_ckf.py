# (563) 線 ckf の読み手：電化と機関車の発見の入口の定義の門（事前登録 measurements/ckf563_20261026_prereg.txt [4]）。
# 使い方：PYTHONUTF8=1 python an_ckf.py <B の ins> <A（基線）の ins> [種の起点,種の数]
#   例）PYTHONUTF8=1 python an_ckf.py D:/PoB_work/ckf_563/out_B/ins D:/PoB_work/keep_560/m560_ins 1,10
# 読むのは計器 engineer_s*.tsv（kind=E：発見の事象／kind=T：標本〔8 四半期ごと・国×行〕）。読むだけ。期待値を本体と同じ出所から作らない（基線の生と比べる）。
import csv, collections, statistics as st, sys

MAIN = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', 'アメリカ', '清', '日本']
AM = 12.5677                   # 自国の項の半分点（DiscoveryOwnHalfPerMonth の既定・事前登録 G3）
FAMILY = ['電動機', '電化製品', '窒素固定', '合成ゴム', '水力発電', '石油火力', '蒸気タービン']


def mainname(n):
    for m in MAIN:
        if n == m or n.endswith(m):
            return m
    return None


_cache = {}


def load(d, s):
    k = (d, s)
    if k in _cache:
        return _cache[k]
    E, T = [], []
    with open(f'{d}/engineer_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            r['quarter'] = int(r['quarter'])
            r['year'] = int(r['year'])
            r['decade'] = int(r['decade'])
            for c in ('engineerPerMonth', 'techTerm', 'needTerm', 'replaced', 'cost'):
                try:
                    r[c] = float(r[c])
                except ValueError:
                    r[c] = float('nan')
            (E if r['kind'] == 'E' else T).append(r)
    _cache[k] = (E, T)
    return E, T


def samples(d, seeds):
    """主要 8 国の未発見の行の標本（kind=T・knownFlag=0）。"""
    for s in seeds:
        for r in load(d, s)[1]:
            if r['knownFlag'] == '0' and mainname(r['nation']):
                yield s, r


def events(d, s):
    return load(d, s)[0]


def share_cost_neg(d, seeds):
    tot = collections.Counter()
    neg = collections.Counter()
    for s, r in samples(d, seeds):
        tot[r['row']] += 1
        neg[r['row']] += r['cost'] < 0
    return {k: neg[k] / tot[k] for k in tot}


def need_pos(d, seeds):
    tot = collections.Counter()
    pos = collections.Counter()
    for s, r in samples(d, seeds):
        tot[r['row']] += 1
        pos[r['row']] += r['needTerm'] > 0
    return {k: pos[k] / tot[k] for k in tot}


def loco_over_am(d, seeds):
    """機関車の A が半分点を越えた (種, 主要国) の数と、そのうち英が越えた種の数（1850 年までの標本）。"""
    pairs = set()
    uk = set()
    for s, r in samples(d, seeds):
        if r['row'] == '機関車' and r['year'] <= 1850 and r['engineerPerMonth'] > AM:
            m = mainname(r['nation'])
            pairs.add((s, m))
            if m == 'イギリス':
                uk.add(s)
    return len(pairs), len(uk)


def g4_counts(d, seeds):
    out = {}
    for s in seeds:
        e = events(d, s)
        out[s] = (sum(1 for r in e if r['decade'] >= 1800 and r['year'] <= 1800),
                  sum(1 for r in e if r['decade'] >= 1800 and r['year'] <= 1786))
    return out


def china_first(d, seeds):
    a = b = 0
    for s in seeds:
        by = collections.defaultdict(list)
        for r in events(d, s):
            if r['decade'] > 1780:
                by[r['row']].append((r['year'], r['nation']))
        for row, L in by.items():
            y0 = min(y for y, _ in L)
            b += 1
            a += any(n == '清' or n.endswith('清') for y, n in L if y == y0)
    return a, b


def sim_within_1y(d, seeds, row='機関車'):
    out = {}
    for s in seeds:
        L = sorted((r['year'], mainname(r['nation'])) for r in events(d, s) if r['row'] == row and mainname(r['nation']))
        if L:
            out[s] = sum(1 for y, _ in L if y <= L[0][0] + 1)
    return out


def d8_by_1913(d, seeds):
    out = {}
    for s in seeds:
        out[s] = len({r['row'] for r in events(d, s) if r['decade'] >= 1860 and r['year'] <= 1913 and mainname(r['nation'])})
    return out


def first_years(d, seeds, rows):
    res = {}
    for row in rows:
        L = []
        for s in seeds:
            ys = [r['year'] for r in events(d, s) if r['row'] == row]
            L.append((min(ys), len(ys)) if ys else None)
        res[row] = L
    return res


def quant(xs, q):
    xs = sorted(xs)
    return xs[min(len(xs) - 1, int(q * len(xs)))] if xs else float('nan')


def main():
    B, A = sys.argv[1], sys.argv[2]
    s0, n = (int(x) for x in (sys.argv[3] if len(sys.argv) > 3 else '1,10').split(','))
    seeds = list(range(s0, s0 + n))
    ok = lambda b: 'OK ' if b else 'NG '
    nan = float('nan')
    print(f'# an_ckf B={B} A={A} 種={seeds[0]}..{seeds[-1]}')
    pflag = 'P' in sys.argv[4:]   # 4 つ目以降の引数に P を書くと、旗 P を立てた腕として G1 と G8 を門に数える（立てない腕では参考）
    cb, ca = share_cost_neg(B, seeds), share_cost_neg(A, seeds)
    g1 = cb.get('電動機', nan)
    print(f"{ok(g1 <= 0.10) if pflag else 'ref'} G1 電動機の費用の欄が負の割合 B={g1:.3f} A={ca.get('電動機', nan):.3f}（旗 P の腕だけ門：B ≤ 0.10）")
    worse = [(k, round(cb[k], 3), round(ca.get(k, 0), 3)) for k in cb if cb[k] > ca.get(k, 0) + 0.06]   # 軌道が変わるので標本の構成も動く＝0.06 の許し（追記2：ヌル腕 N1・N2 の最大の増加 0.048 から。追記1 の 0.02 はヌル腕で外れた）
    npb, npa = need_pos(B, seeds), need_pos(A, seeds)
    moved = [(k, round(npa.get(k, 0), 3), round(npb[k], 3)) for k in npb if ca.get(k, 1.0) < 0.9999 and npb[k] - npa.get(k, 0) > 0.10]
    print(f"{ok(not worse and not moved)}G2 他の行を悪くしない 費用の欄が負の割合が増えた行={worse} 必要性＞0 の割合が 0.10 超増えた行（基線で穴が全標本でない行）={moved}")
    print('   R1 必要性＞0 の割合が 0.02 超動いた行（A→B）：' + ', '.join(f'{k}:{npa.get(k, 0):.3f}→{npb[k]:.3f}' for k in sorted(npb) if abs(npb[k] - npa.get(k, 0)) > 0.02))
    print('   R1 電力の家族の費用の欄が負の割合（A→B）：' + ', '.join(f"{k}:{ca.get(k, nan):.2f}→{cb.get(k, nan):.2f}" for k in FAMILY))
    pb, ub = loco_over_am(B, seeds)
    pa, ua = loco_over_am(A, seeds)
    cells = 8 * len(seeds)
    print(f"ref G3 機関車の A＞半分点（主要 8 国×種・1850 年まで）B={pb}/{cells}（英 {ub}/{len(seeds)}） A={pa}/{cells}（英 {ua}/{len(seeds)}）（追記2：記録へ。旗 L が A の定義を変えたかの確認＝番人が手計算で確かめた物で、採否の判別力は無い）")
    gb, ga = g4_counts(B, seeds), g4_counts(A, seeds)
    bad4 = [s for s in seeds if gb[s][1] != 0]
    t0, t1 = sum(v[0] for v in gb.values()), sum(v[0] for v in ga.values())
    print(f"{ok(not bad4)}G4 開幕の一斉解禁（追記2：1783〜86 年に d≧2 の行を見つけた 国×行＝0 のまま）B={ {s: gb[s][1] for s in seeds} } 超過した種={bad4}")
    print(f"   R ≤1800 年の d≧2 の発見 種ごと（記録・ヌル腕でも振れる）B={ {s: gb[s][0] for s in seeds} } 合計 B={t0} A={t1}（ヌル腕 N1 104・N2 142）")
    cfb, cfa = china_first(B, seeds), china_first(A, seeds)
    rb, ra = cfb[0] / max(1, cfb[1]), cfa[0] / max(1, cfa[1])
    print(f"{ok(rb <= ra + 0.08)}G5 最初の発見者に清が入る行の割合 B={cfb[0]}/{cfb[1]}={rb:.3f} A={cfa[0]}/{cfa[1]}={ra:.3f}（B ≤ A+0.08＝追記2：ヌル腕 N1 の +0.066 から）")
    sb, sa = sim_within_1y(B, seeds), sim_within_1y(A, seeds)
    med = st.median(sb.values()) if sb else nan
    mx = max(sb.values()) if sb else nan
    print(f"{ok(bool(sb) and med <= 4 and mx <= 6)}G6 機関車で最初の主要国から 1 年以内に開く主要国の数 B={sb} 中央 {med}・最大 {mx}（≤4・≤6） A={sa}")
    d8b, d8a = d8_by_1913(B, seeds), d8_by_1913(A, seeds)
    print(f"ref G7 d≧8 の行が 1913 年までに主要国へ届いた数 B={d8b} A={d8a} 合計 B={sum(d8b.values())} A={sum(d8a.values())}（追記2：記録へ。ヌル腕の合計 N1 14・N2 11 対 基線 25＝揺らぎで門にならない）")
    def appliance_first(d):
        n = 0
        for s in seeds:
            coal = {r['nation']: r['year'] for r in events(d, s) if r['row'] == '石炭火力'}
            n += sum(1 for r in events(d, s) if r['row'] == '電化製品' and not (r['nation'] in coal and coal[r['nation']] <= r['year']))
        return n
    b8, a8 = appliance_first(B), appliance_first(A)
    print(f"{ok(b8 <= a8) if pflag else 'ref'} G8 電化製品を、同じ国が石炭火力を見つけるより前に見つけた 国×種 の数 B={b8} A={a8}（旗 P の腕だけ門：B ≤ A）")
    rows = ['石炭火力', '電動機', '送電網', '電化製品', '機関車', '転炉鋼', '汽船']
    fb, fa = first_years(B, seeds, rows), first_years(A, seeds, rows)
    for r in rows:
        print(f"   R2/R3 {r} 最初の国の年/国数（種ごと）B={fb[r]} A={fa[r]}")
    for lab, d in (('B', B), ('A', A)):
        firsts = []
        for s in seeds:
            L = sorted((r['year'], mainname(r['nation'])) for r in events(d, s) if r['row'] == '機関車' and mainname(r['nation']))
            firsts.append(f"{s}:{L[0][1]}{L[0][0]}" if L else f"{s}:なし")
        print(f"   R3 機関車の最初の主要国（種:国年）{lab} {' '.join(firsts)}")
        allv, tv, rc, mt = [], [], [], []
        for s, r in samples(d, seeds):
            if r['row'] == '石炭火力':
                allv.append(r['needTerm'] > 0)
                if r['techTerm'] > 0:
                    tv.append(r['needTerm'] > 0)
            if r['row'] == '電動機' and r['replaced'] > 0 and r['cost'] > 0:
                rc.append(r['replaced'] / r['cost'])
            if r['row'] == '電動機':
                mt.append(r['techTerm'] > 0)
        print(f"   R 電動機の 技術力＞0 の標本 {lab} {sum(mt)}/{len(mt)}（旗 M の働きの見え・基線 91/6669）")
        print(f"   R6/R7 {lab} 石炭火力の必要性＞0 の割合 全行 {sum(allv)}/{len(allv)}・技術力＞0 の行 {sum(tv)}/{len(tv)}／電動機の 値段÷費用 の標本 {len(rc)} 分位 10%={quant(rc, .1):.3f} 50%={quant(rc, .5):.3f} 90%={quant(rc, .9):.3f}（1 超で必要性＞0）")


if __name__ == '__main__':
    main()
