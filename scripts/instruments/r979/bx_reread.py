# (979) c88rb 読むだけの台本：basisx_s の生から、AI の規則 A・B・C1・C2・S を〈往復の上限 K〉で数え直す（腕ごと・錨の国×規則×種）。
# 使い方：python -X utf8 -I bx_reread.py <書き出すファイル> <腕の名前>=<basisx_s*.tsv のあるフォルダ> [...]
# 物差しは draft.md §0（数える前に固定）：窓＝錨の年の前後 5 年 かつ q>=20。no-go＝窓の中の錨の向きの切り替えが全ての種で 0。
#   種ごとの上限 K＝5（窓の中の全ての向きの切り替え na）。過多＝10 種の na の中央値が K を超える。旧の過多（na×2>=窓の四半期の数の種がある）も並べる。
# 生は読むだけ（rb で開く）。bx_read.py（scripts/instruments/c87bx/）の数え方を写した（-I では import できない＝関数ごと写す）。
import sys, os, glob, statistics

ANCHOR = {'フランス': ('P->F', 1790), 'プロイセン': ('P->F', 1861), '日本': ('P->F', 1873), '清': ('F->P', 1941)}
RULES = ['A', 'B', 'C1', 'C2', 'S']
WIN_YEARS = 5
OPEN_Q = 20
K = 5

def anchor_counts(seq, years, qs, direction, anchor_year):
    """bx_read.py の anchor_counts と同じ：(窓の中の錨の向きの切り替え n, 窓の中の全ての切り替え na, 窓の四半期の数 wq)。"""
    n = 0; na = 0; wq = 0
    for i in range(len(seq)):
        if qs[i] < OPEN_Q or abs(years[i] - anchor_year) > WIN_YEARS:
            continue
        wq += 1
        if i == 0:
            continue
        a, b = seq[i - 1], seq[i]
        if (a >= 0.5) != (b >= 0.5):
            na += 1
        if direction == 'P->F' and a < 0.5 <= b:
            n += 1
        if direction == 'F->P' and a >= 0.5 > b:
            n += 1
    return n, na, wq

def verdict_new(counts):
    if not counts or all(wq == 0 for _, _, wq in counts):
        return '窓なし'
    if all(n == 0 for n, _, _ in counts):
        return 'no-go'
    if statistics.median([na for _, na, _ in counts]) > K:
        return '過多'
    return '通る'

def verdict_old(counts):
    if not counts or all(wq == 0 for _, _, wq in counts):
        return '窓なし'
    if all(n == 0 for n, _, _ in counts):
        return 'no-go'
    if any(wq > 0 and na * 2 >= wq for _, na, wq in counts):
        return '過多'
    return '通る'

def selftest():
    qs = list(range(80)); years = [1783 + q // 4 for q in qs]
    ay = 1790   # 窓＝1785〜1795・q>=20（1788 以後）
    def path(sw_qs):
        s = []; v = 0.0
        for q in qs:
            if q in sw_qs: v = 1.0 - v
            s.append(v)
        return s
    cases = [
        ('毎四半期に反転', [float(q % 2) for q in qs], '過多'),
        ('何もしない', [0.0] * 80, 'no-go'),
        ('1 回だけ', path({30}), '通る'),
        ('片道＋往復 1 回（3 回）', path({30, 34, 38}), '通る'),
        ('片道＋往復 2 回（5 回）', path({26, 30, 34, 38, 42}), '通る'),
        ('片道＋往復 3 回（7 回）', path({24, 26, 30, 34, 38, 42, 46}), '過多'),
    ]
    out = []; ok = True
    for name, seq, want in cases:
        c = anchor_counts(seq, years, qs, 'P->F', ay)
        got = verdict_new([c] * 10)
        out.append('%s→%s（n=%d・na=%d・窓 %d）' % (name, got, c[0], c[1], c[2]))
        if got != want: ok = False
    return ok, out

def load(path):
    with open(path, 'rb') as f:
        data = f.read().decode('utf-8').split('\n')
    head = data[0].rstrip('\r').split('\t')
    rows = []
    for ln in data[1:]:
        if not ln.strip():
            continue
        rows.append(dict(zip(head, ln.rstrip('\r').split('\t'))))
    return rows

def num(s):
    try:
        return float(s)
    except (ValueError, TypeError):
        return float('nan')

def seed_of(p):
    b = os.path.basename(p)
    return int(b[len('basisx_s'):-len('.tsv')])

def read_arm(ins):
    """腕 1 本：per[(国,規則)] = [(種, n, na, wq)]・tot[(国,規則)] = [全長の切り替え]・pis[国] = [(種, 窓の中の π の符号の変化, 窓の中の π の中央, 窓の |π| の中央)]・sw[(国,規則)] = [切り替えた四半期の |ΔC_exp|÷B]"""
    files = sorted(glob.glob(os.path.join(ins, 'basisx_s*.tsv')), key=seed_of)
    per = {}; tot = {}; pis = {}; swr = {}
    for p in files:
        sd = seed_of(p)
        byn = {}
        for r in load(p):
            byn.setdefault(r['nation'], []).append(r)
        for nat, rs in byn.items():
            rs.sort(key=lambda r: int(r['quarter']))
            qs = [int(r['quarter']) for r in rs]; years = [int(r['year']) for r in rs]
            for k in RULES:
                seq = [num(r['bxTarget' + k]) for r in rs]
                tot.setdefault((nat, k), []).append(int(num(rs[-1]['bxSwitch' + k])))
                if nat in ANCHOR:
                    d, ay = ANCHOR[nat]
                    n, na, wq = anchor_counts(seq, years, qs, d, ay)
                    per.setdefault((nat, k), []).append((sd, n, na, wq))
                    # 切り替えた四半期の 1 年先の ΔC の大きさ÷基数（記録・門にしない）
                    for i in range(1, len(rs)):
                        if qs[i] < OPEN_Q or abs(years[i] - ay) > WIN_YEARS: continue
                        if (seq[i - 1] >= 0.5) != (seq[i] >= 0.5):
                            B = num(rs[i]['bxBasePbPerMonth']); tau = num(rs[i]['bxRate'])
                            col = 'bxDeltaCExpToFixedPerMonth' if seq[i] >= 0.5 else 'bxDeltaCExpToPropPerMonth'
                            dc = num(rs[i][col])
                            if B > 0 and tau > 0 and dc == dc:
                                swr.setdefault((nat, k), []).append(abs(dc) / (tau * B))
            if nat in ANCHOR:
                d, ay = ANCHOR[nat]
                w = [num(r['bxExpInflRulePerYear']) for r, q, y in zip(rs, qs, years) if q >= OPEN_Q and abs(y - ay) <= WIN_YEARS]
                w = [x for x in w if x == x]
                cross = sum(1 for i in range(1, len(w)) if (w[i - 1] < 0) != (w[i] < 0))
                pis.setdefault(nat, []).append((sd, cross, statistics.median(w) if w else float('nan'), statistics.median([abs(x) for x in w]) if w else float('nan'), sum(1 for x in w if x < 0), len(w)))
    return len(files), per, tot, pis, swr

def main():
    out = sys.argv[1]
    arms = [a.split('=', 1) for a in sys.argv[2:]]
    L = []; W = L.append
    ok, st = selftest()
    W('# bx_reread（(979) c88rb）：規則 A・B・C1・C2・S を往復の上限 K＝%d（窓の中の全ての向きの切り替え・種ごと）と中央値で数え直す' % K)
    W('# 窓＝錨の年の前後 %d 年 かつ q>=%d。no-go＝窓の中の錨の向きの切り替えが全ての種で 0。過多＝10 種の na の中央値＞K。旧＝na×2>=窓の四半期の数の種がある。' % (WIN_YEARS, OPEN_Q))
    W('# 自己検査：' + '・'.join(st) + ' → ' + ('合格' if ok else '不合格（台本を直す）'))
    if not ok:
        open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8')); sys.exit(2)
    data = {}
    for name, ins in arms:
        nf, per, tot, pis, swr = read_arm(ins)
        data[name] = (per, tot, pis, swr)
        W('# 腕 %s：%s・種のファイル %d 本' % (name, ins, nf))
    W('')
    W('## 1. 錨の国×規則×腕：判定（新・旧）・上限の外の種の本数・種ごとの na（窓の中の全ての切り替え）と n（錨の向き）')
    W('規則\t国\t腕\t新\t旧\tna の中央\tK の外の種\t種ごとの na\t種ごとの n')
    summ = {}
    for k in RULES:
        for nat in ANCHOR:
            for name, _ in arms:
                cs = data[name][0].get((nat, k), [])
                c3 = [(n, na, wq) for _, n, na, wq in cs]
                vn, vo = verdict_new(c3), verdict_old(c3)
                summ[(k, nat, name)] = vn
                over = [sd for sd, n, na, wq in cs if na > K]
                W('%s\t%s\t%s\t%s\t%s\t%g\t%d（%s）\t%s\t%s' % (k, nat, name, vn, vo, statistics.median([na for _, _, na, _ in cs]) if cs else float('nan'), len(over), ','.join(map(str, over)), ','.join('%d:%d' % (sd, na) for sd, _, na, _ in cs), ','.join(str(n) for _, n, _, _ in cs)))
    W('')
    W('## 2. 規則ごとの総括（新の物差し・腕ごと）')
    for name, _ in arms:
        for k in RULES:
            bad = [nat for nat in ANCHOR if summ[(k, nat, name)] in ('no-go', '過多')]
            W('%s\t%s：%s%s' % (name, k, ' '.join('%s=%s' % (nat, summ[(k, nat, name)]) for nat in ANCHOR), '　→ 使えない（%s）' % ','.join(bad) if bad else '　→ 錨の国の全てで通る'))
        allbad = all(any(summ[(k, nat, name)] in ('no-go', '過多') for nat in ANCHOR) for k in RULES)
        W('%s\t全ての候補が no-go か過多か：%s' % (name, 'はい' if allbad else 'いいえ'))
    W('')
    W('## 3. 窓の中の期待インフレ π（規則の入力）：種ごとの〈π の符号の変化の回数／π＜0 の四半期／窓の四半期〉と窓の中の π の中央（年率）')
    W('国\t腕\t符号の変化（種ごと）\tπ＜0 の四半期／窓（種ごと）\tπ の中央（種の中央）\t|π| の中央（種の中央）')
    for nat in ANCHOR:
        for name, _ in arms:
            ps = data[name][2].get(nat, [])
            W('%s\t%s\t%s\t%s\t%.4g\t%.4g' % (nat, name, ','.join('%d:%d' % (sd, c) for sd, c, _, _, _, _ in ps), ','.join('%d/%d' % (neg, n) for _, _, _, _, neg, n in ps), statistics.median([m for _, _, m, _, _, _ in ps]), statistics.median([m for _, _, _, m, _, _ in ps])))
    W('')
    W('## 4. 窓の中で切り替えた四半期の |ΔC_exp|÷(τ×B)（1 年先の請求の変化の大きさを基数で割った物・記録・門にしない）：本数／中央／p90／最大')
    for k in ('A', 'B'):
        for nat in ANCHOR:
            for name, _ in arms:
                xs = sorted(data[name][3].get((nat, k), []))
                if xs:
                    W('%s\t%s\t%s\t%d\t%.4g\t%.4g\t%.4g' % (k, nat, name, len(xs), statistics.median(xs), xs[min(len(xs) - 1, int(round(0.9 * (len(xs) - 1))))], xs[-1]))
                else:
                    W('%s\t%s\t%s\t0' % (k, nat, name))
    W('')
    W('## 5. 全長の切り替えの回数（記録・史実は 1 試合に切り替え 1〜5 回の桁）：錨の国×規則・腕ごとの種の最小／中央／最大')
    for nat in ANCHOR:
        for name, _ in arms:
            t = data[name][1]
            W('%s\t%s\t%s' % (nat, name, '\t'.join('%s %d/%g/%d' % (k, min(t[(nat, k)]), statistics.median(t[(nat, k)]), max(t[(nat, k)])) for k in RULES)))
    open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))

if __name__ == '__main__':
    main()
