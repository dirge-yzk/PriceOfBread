# (911) c78lt：盤面 m905 の landbase_s・nation_s・subst_s を読む（設計の材料・記録・採否でない）。
#   python -X utf8 r911read.py <出力.txt> <腕の名=ins のフォルダ> ...
#   [Q1] 開幕の取り分 r×V÷R（= landClaimExtShareUpperPerMonth ÷ (landTaxEndShare × baseRPerMonth)）の国の中央と 10 倍超の国の数・免税の度合いが正の国・州の数
#   [Q2] 限界 ⑨：同じ国の前の行から 率・政体・度合い・州の数 が変わった行（(ii) で外す行の上界）と、取れた÷請求 ＞1 の行
#   [G8] 案 Y の候補の国の |log baseOverFirstSeenRatio| の中央（腕ごと）と、物価の向きと B の向き（反証役 (3)）
#   [G2] fpShortPops の窓ごとの和（開幕 1783-85 を含む）
#   [NG] no-go の規則 (ii) を土地税の無い国（ロシア）に当てて恒真でないことを示す
#   [Q7] subst_s の売れ残りの 2 つの基準 D1（能力）・D2（産出）の窓×格ごとの中央と producedZeroFlag の割合
import csv, glob, math, os, re, sys
from collections import defaultdict

WIN = [(1783, 1785), (1786, 1799), (1800, 1849), (1850, 1899), (1900, 1950)]
CAND = ['イギリス', '清', 'フランス', 'プロイセン']
REF = ['日本', 'ロシア', 'オスマン', 'オーストリア']
csv.field_size_limit(1 << 30)


def win(y):
    for a, b in WIN:
        if a <= y <= b:
            return '%d-%02d' % (a, b % 100)
    return None


def med(xs):
    xs = sorted(x for x in xs if x == x)
    if not xs:
        return float('nan')
    n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])


def fl(s):
    try:
        return float(s)
    except ValueError:
        return float('nan')


def seed_of(p):
    m = re.search(r'_s(\d+)\.tsv$', p)
    return int(m.group(1))


def rows(p, cols):
    with open(p, encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t')
        h = next(r)
        ix = [h.index(c) for c in cols]
        for row in r:
            if len(row) < len(h):
                continue
            yield [row[i] for i in ix]


def arm(ins, out):
    W = out.append
    nat = {}   # (seed, q, nation) -> (rate, polity, nreg, price)
    for p in sorted(glob.glob(os.path.join(ins, 'nation_s*.tsv'))):
        s = seed_of(p)
        for q, y, n, rate, pol, c1, c2, c3, c4, pl, tg in rows(p, ['quarter', 'year', 'nation', 'landTaxEndShare', 'polityCode', 'coreRegions', 'integratedRegions', 'colonialRegions', 'nonCoreRegions', 'priceLevel', 'landTaxTargetEndShare']):
            nat[(s, int(q), n)] = (fl(rate), pol, sum(x for x in (fl(c1), fl(c2), fl(c3), fl(c4)) if x == x), fl(pl), fl(tg))
    lb = defaultdict(list)   # (seed, nation) -> [(q, y, deg, claim, paid, poc, ext, R, first, short, carriers)]
    for p in sorted(glob.glob(os.path.join(ins, 'landbase_s*.tsv'))):
        s = seed_of(p)
        for q, y, n, deg, claim, paid, poc, ext, R, first, short, car in rows(p, ['quarter', 'year', 'nation', 'privExemptDegreeShare', 'landClaimPerMonth', 'landTaxPaidPerMonth', 'landPaidOverClaim', 'landClaimExtShareUpperPerMonth', 'baseRPerMonth', 'baseOverFirstSeenRatio', 'fpShortPops', 'fpCarrierPops']):
            lb[(s, n)].append((int(q), int(y), fl(deg), fl(claim), fl(paid), fl(poc), fl(ext), fl(R), fl(first), int(short), int(car)))
    nrows = sum(len(v) for v in lb.values())
    joined = sum(1 for (s, n), v in lb.items() for t in v if (s, t[0], n) in nat)
    W('  landbase_s の行 %d・nation_s と突き合わせられた行 %d' % (nrows, joined))
    # [Q1]
    rv_q0 = defaultdict(list); deg_q0 = {}; reg_q0 = defaultdict(list); rv_win = defaultdict(list)
    for (s, n), v in lb.items():
        for t in v:
            q, y, deg, claim, paid, poc, ext, R = t[:8]
            k = nat.get((s, q, n))
            if k is None:
                continue
            rate = k[0]
            rv = ext / (rate * R) if rate > 1e-9 and R > 0 and ext == ext else float('nan')
            if q == 0:
                rv_q0[n].append(rv); deg_q0[n] = deg; reg_q0[n].append(k[2])
            w = win(y)
            if w and rv == rv:
                rv_win[w].append(rv)
    medn = {n: med(v) for n, v in rv_q0.items()}
    vals = [x for x in medn.values() if x == x]
    W('[Q1] 開幕 q0 の r×V÷R：国の中央（種の中央）の全ての国の中央 %.3f・国の数 %d・10 倍超 %d・30 倍以上 %d' % (med(vals), len(vals), sum(1 for x in vals if x > 10), sum(1 for x in vals if x >= 30)))
    top = sorted(((x, n) for n, x in medn.items() if x == x), reverse=True)[:6]
    W('     上位：' + '・'.join('%s %.2f' % (n, x) for x, n in top))
    W('     10 倍超の国：' + '・'.join(sorted(n for n, x in medn.items() if x == x and x > 10)))
    W('     窓ごとの r×V÷R（国×四半期の中央）：' + '・'.join('%s %.3f' % (w, med(rv_win[w])) for w in ['%d-%02d' % (a, b % 100) for a, b in WIN]))
    W('     開幕で免税の度合い d′＞0 の国 %d／%d・州の数（q0・種の中央）の全ての国の和 %.0f' % (sum(1 for x in deg_q0.values() if x > 0), len(deg_q0), sum(med(v) for v in reg_q0.values())))
    for n in CAND + REF:
        if n in medn:
            W('     %s：q0 の r×V÷R %.3f・d′ %.2f・州の数 %.0f' % (n, medn[n], deg_q0.get(n, float('nan')), med(reg_q0[n])))
    # [Q2]
    tot = defaultdict(int); ch = defaultdict(lambda: defaultdict(int)); over = defaultdict(int); allch = defaultdict(int)
    for (s, n), v in lb.items():
        prev = None; prevdeg = None
        for t in sorted(v):
            q, y, deg, claim = t[0], t[1], t[2], t[3]
            k = nat.get((s, q, n))
            g = n if n in CAND else ('他' )
            if claim > 0:
                tot[g] += 1; tot['全'] += 1
                if t[5] == t[5] and t[5] > 1 + 1e-9:
                    over[g] += 1; over['全'] += 1
            if prev is not None and k is not None and claim > 0:
                any_ = False
                for name, a, b in [('率', prev[0], k[0]), ('政体', prev[1], k[1]), ('州の数', prev[2], k[2]), ('度合い', prevdeg, deg)]:
                    if not (a == b or (a != a and b != b)):
                        ch[g][name] += 1; ch['全'][name] += 1; any_ = True
                if any_:
                    allch[g] += 1; allch['全'] += 1
                if not (prev[0] == k[0]) and abs(k[0] - k[4]) < 1e-12:
                    ch[g]['率＝目標'] += 1; ch['全']['率＝目標'] += 1
                if (not (prev[1] == k[1])) or (not (prev[2] == k[2])) or (not (prevdeg == deg)):
                    ch[g]['率以外'] += 1; ch['全']['率以外'] += 1
            prev = k; prevdeg = deg
    W('[Q2] 限界 ⑨：請求＞0 の国×四半期のうち、前の行から変わった行（(ii) で外す上界）と 取れた÷請求＞1 の行')
    for g in CAND + ['他', '全']:
        if tot[g]:
            c = ch[g]
            W('     %s：行 %d・いずれか %d（%.2f%%）［率 %d（うち変わった後の率＝目標 %d）・政体 %d・度合い %d・州の数 %d・率以外のいずれか %d（%.2f%%）］・取れた÷請求＞1 %d（%.2f%%）' % (g, tot[g], allch[g], 100.0 * allch[g] / tot[g], c['率'], c['率＝目標'], c['政体'], c['度合い'], c['州の数'], c['率以外'], 100.0 * c['率以外'] / tot[g], over[g], 100.0 * over[g] / tot[g]))
    yc = sum(tot[g] for g in CAND); ya = sum(allch[g] for g in CAND)
    W('     候補 4 国の和：行 %d・外れる上界 %d（%.2f%%）' % (yc, ya, 100.0 * ya / yc if yc else float('nan')))
    # [G8]
    W('[G8] 候補の国の旗 0 の B（=baseOverFirstSeenRatio＝q0 の R÷今の R）')
    for n in CAND + REF:
        L = [t for (s, nn), v in lb.items() if nn == n for t in v]
        if not L:
            continue
        lg = [abs(math.log(t[8])) for t in L if t[8] == t[8] and t[8] > 0]
        bw = defaultdict(list)
        for t in L:
            w = win(t[1])
            if w and t[8] == t[8] and t[8] > 0:
                bw[w].append(t[8])
        # 物価の向き
        dn = dn_ok = up = up_ok = 0
        for (s, nn), v in lb.items():
            if nn != n:
                continue
            v2 = sorted(v)
            k0 = nat.get((s, v2[0][0], n))
            if k0 is None or not (k0[3] > 0):
                continue
            for t in v2:
                k = nat.get((s, t[0], n))
                if k is None or not (k[3] > 0) or not (t[8] == t[8] and t[8] > 0):
                    continue
                pr = k[3] / k0[3]
                if pr < 0.98:
                    dn += 1; dn_ok += t[8] > 1
                elif pr > 1.02:
                    up += 1; up_ok += t[8] < 1
        W('     %s：|log B| の中央 %.3f（行 %d）・B の窓ごとの中央 %s・物価が 2%% 超下がった行 %d のうち B＞1 %.3f・上がった行 %d のうち B＜1 %.3f' % (
            n, med(lg), len(lg), '・'.join('%s %.3f' % (w, med(bw[w])) for w in sorted(bw)), dn, dn_ok / dn if dn else float('nan'), up, up_ok / up if up else float('nan')))
    # [G2]
    sw = defaultdict(int); cw = defaultdict(int)
    for v in lb.values():
        for t in v:
            w = win(t[1])
            if w:
                sw[w] += t[9]; cw[w] += t[10]
    W('[G2] fpShortPops の窓ごとの和（担い手の POP×四半期）：' + '・'.join('%s %d／%d' % (w, sw[w], cw[w]) for w in sorted(sw)))
    # [NG] 規則 (ii)：候補の国×四半期で請求＞0 の行の数（0 なら no-go）
    for n in CAND + ['ロシア']:
        k = sum(1 for (s, nn), v in lb.items() if nn == n for t in v if t[3] > 0)
        W('[NG] 規則 (ii) の母集合：%s の請求＞0 の国×四半期 %d' % (n, k))


def subst(ins, out):
    W = out.append
    d1 = defaultdict(list); d2 = defaultdict(list); dd = defaultdict(list); z = defaultdict(int); n = defaultdict(int)
    r1 = defaultdict(list); r2 = defaultdict(list)
    for p in sorted(glob.glob(os.path.join(ins, 'subst_s*.tsv'))):
        for y, tier, a, b, zf, o1, o2 in rows(p, ['year', 'tier', 'unsoldD1Share', 'unsoldD2Share', 'producedZeroFlag', 'derivedD1OverNominal', 'derivedD2OverNominal']):
            w = win(int(y))
            if not w:
                continue
            k = (w, tier)
            a, b = fl(a), fl(b)
            n[k] += 1; z[k] += zf == '1'
            if a == a:
                d1[k].append(a)
            if b == b:
                d2[k].append(b)
            if a == a and b == b:
                dd[k].append(a - b)
            r1[k].append(fl(o1)); r2[k].append(fl(o2))
    W('[Q7] 売れ残りの基準（州×四半期の中央）：窓・格・行・D1（能力）・D2（産出）・D1−D2・producedZeroFlag の割合・導いた自給÷名目（D1・D2）')
    for k in sorted(n):
        W('     %s %s：行 %d・D1 %.3f・D2 %.3f・差 %.3f・産出 0 %.4f・D1 由来÷名目 %.3f・D2 由来÷名目 %.3f' % (k[0], k[1], n[k], med(d1[k]), med(d2[k]), med(dd[k]), z[k] / n[k], med(r1[k]), med(r2[k])))


def main():
    outp = sys.argv[1]
    out = []
    for a in sys.argv[2:]:
        name, ins = a.split('=', 1)
        out.append('== 腕 %s（%s）' % (name, ins))
        arm(ins, out)
        if os.environ.get('NOSUBST') != '1':
            subst(ins, out)
    with open(outp, 'wb') as fh:
        fh.write(('\n'.join(out) + '\n').encode('utf-8'))


if __name__ == '__main__':
    main()
