"""(631) 線 dbt の読み手＝事前登録の狙いの数字（P1・G1〜G10）を計器つき 10 種の生（fiscal_s・fsx_s・nation_s）から出す。読むだけ。
使い方：
  python g_dbt.py <ins フォルダ> ...                         （複数の腕を並べて数字だけ出す）
  python g_dbt.py --judge <B> <基線> <N1> <N2>                （B を基線とヌル腕 2 本と並べ、事前登録の合否の行を出す）
主要 9 国＝計器の NATIONS。4 国＝仏・墺・日・米（P1・G9 の対象）。
P1 採否の量：4 国×十年（1800〜1949）×種で十年の終わりに 国債÷年GDP＜0.02 の割合（0 に張り付く）。B が ヌル腕 2 本の小さい方を下回れば〈命中〉
G9m 入口の大きさ：4 国の 1820〜1900 年代 360 セルの 発行÷年GDP の平均。B が max(基線,N1,N2) を超えれば〈入口が働いた〉（G9 の件数は記録）
PATH 経路：B−基線 の 発行の平均 が 床の返済の平均の減りより大きい（借入が主）なら〈借入の経路〉。床の返済が減っての 0 離れは〈狙いと別の経路〉
G1 払えない額の恒等式 ／ G2 同じ月に借りながら返さない ／ G4 準備金のための借入（債務あり・前の四半期末の国庫≥線 で 四半期末の国庫＞線＋1 か月の歳入）の割合 ＜10%
G5 発散（絶対）／ G7 英の国債÷年GDP の十年の終わりの中央値 B−基線 が 1800〜1940 の全ての十年で ＜ 0.19 ／ 記録：中間の帯・利払い÷歳入・門の閉じた月・国債の十年中央値
単位の注記：fiscal_s の台帳の列は収入が正・支出が負。"""
import csv, sys, glob, re, os, io, statistics as st
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline=None)
csv.field_size_limit(1 << 30)
NAT = ['アメリカ', 'イギリス', 'オスマン', 'オーストリア', 'フランス', 'プロイセン', 'ロシア', '日本', '清']
G9N = ['フランス', 'オーストリア', '日本', 'アメリカ']
EPS = 0.02

def rows(f):
    with open(f, 'rb') as fh:
        rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd)
        ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            if len(r) == len(h): yield ix, r

def fl(x):
    try: return float(x)
    except: return float('nan')

def run(d):
    seeds = sorted(int(re.search(r'_s(\d+)', f).group(1)) for f in glob.glob(os.path.join(d, 'fiscal_s*.tsv')))
    M = dict(stick={}, nat_stick={}, med={})
    g1_n = g1_bad = 0
    lgdp = {}
    g2_b = g2_co = 0
    g4_den = g4_num = 0; g4_nat = {}
    mid_n = mid_all = 0; mid_debt = []
    maxratio = []; maxrate = 0.0; maxq = 0.0
    borrowm = {}; dflt = {}
    g9 = {}; g9r = {}; g9f = {}; g9gdp = {}   # (種, 国, 十年) -> 発行・返済・床の返済 の合計（4 国の 1820〜1900 年代）と十年平均の年GDP
    intr = rev = 0.0; gclosed = short = 0
    for sd in seeds:
        gdpq = {}; gdpD = {}
        last = {}
        mx = 0.0
        prev = {}
        for ix, r in rows(os.path.join(d, f'nation_s{sd}.tsv')):
            n = r[ix['nation']]
            if n not in NAT: continue
            try: y = int(r[ix['year']]); q = int(r[ix['quarter']]); dbt = fl(r[ix['govDebtStock']]); g = fl(r[ix['nominalGdpPerYear']])
            except: continue
            if not (g > 0 and dbt == dbt and g == g): continue
            gdpq[(n, q)] = g
            gdpD.setdefault((n, (y // 10) * 10), []).append(g)
            x = dbt / g
            mx = max(mx, x)
            if (n in prev) and prev[n] > 0.05 and x / prev[n] > maxq: maxq = x / prev[n]
            prev[n] = x
            D = (y // 10) * 10
            if 1800 <= D <= 1940:
                if (n, D) not in last or q >= last[(n, D)][0]: last[(n, D)] = (q, x)
        maxratio.append(mx)
        for (n, D), (q, x) in last.items():
            M['stick'].setdefault('all', []).append(x < EPS); M['nat_stick'].setdefault(n, []).append(x < EPS); M['med'].setdefault((n, D), []).append(x)
        fsxrows = []
        for ix, r in rows(os.path.join(d, f'fsx_s{sd}.tsv')):
            n = r[ix['nation']]
            if n not in NAT or not r[ix['time']].startswith('q'): continue
            q = int(r[ix['time']][1:]); y = int(r[ix['year']])
            g = gdpq.get((n, q))
            if not g: continue
            debt = fl(r[ix['govDebtEnd']]); t = fl(r[ix['treasuryEnd']]); L = fl(r[ix['lineEnd']])
            if L == L and L > 0 and 1800 <= y <= 1913: lgdp.setdefault(n, []).append(L / g)
            fsxrows.append((n, q, y, debt / g if debt == debt else float('nan'), t, L))
            fl_ = fl(r[ix['repayFloorSum']])
            if n in G9N and 1820 <= y < 1910 and fl_ == fl_: g9f[(sd, n, (y // 10) * 10)] = g9f.get((sd, n, (y // 10) * 10), 0.0) + fl_
        revY = {}
        for ix, r in rows(os.path.join(d, f'fiscal_s{sd}.tsv')):
            n = r[ix['nation']]
            if n not in NAT: continue
            b = fl(r[ix['borrowedPerMonth']]); rp = -fl(r[ix['ledgerDebtRepayPerMonth']])
            y = int(r[ix['year']])
            rv = fl(r[ix['revenueBasePerMonth']])
            if rv == rv: a = revY.setdefault((n, y), [0.0, 0]); a[0] += rv; a[1] += 1
            if n in G9N and 1820 <= y < 1910:
                k = (sd, n, (y // 10) * 10)
                if b == b: g9[k] = g9.get(k, 0.0) + b
                if rp == rp: g9r[k] = g9r.get(k, 0.0) + rp
                gg = gdpD.get((n, (y // 10) * 10))
                if gg: g9gdp[k] = sum(gg) / len(gg)
            m = borrowm.setdefault(n, [0, 0]); m[1] += 1
            if b == b and b > 0: m[0] += 1
            if b == b and b > 1e-9:
                g2_b += 1
                if rp == rp and rp > 1e-9: g2_co += 1
            res = fl(r[ix['borrowReserveHeldStock']]) if 'borrowReserveHeldStock' in ix else 0.0   # 旧い生（列が無い）は準備金 0
            sp = fl(r[ix['spareBeforeBorrowPerMonth']]); bd = r[ix['borrowBinder']]; sf = fl(r[ix['borrowShortfallPerMonth']])
            if bd != 'noShortfall':
                short += 1
                if bd == 'gateClosed': gclosed += 1
            if res == res and sp == sp:
                g1_n += 1
                need = res - sp   # 準備金を引いた余剰が負なら 借りる月の払えない額 = res - sp
                tol = 1e-6 * (1.0 + abs(sp) + abs(res))
                if bd == 'noShortfall':
                    if need > tol: g1_bad += 1
                else:
                    if not (sf == sf and abs(sf - need) <= tol and need > 0): g1_bad += 1
            rate = fl(r[ix['govBondRatePerYear']])
            if rate == rate and rate > maxrate: maxrate = rate
            df = fl(r[ix['defaultCount']])
            if df == df: dflt[(sd, n)] = max(dflt.get((sd, n), 0), df)
            ii = -fl(r[ix['ledgerInterestPerMonth']])
            if ii == ii and rv == rv and 1800 <= y <= 1913: intr += ii; rev += rv
        # G4（準備金のための借入）：債務あり・前の四半期末の国庫≥線 で 四半期末の国庫＞線＋その年の月の歳入の平均
        byn = {}
        for rr in fsxrows: byn.setdefault(rr[0], []).append(rr)
        for n, lst in byn.items():
            lst.sort(key=lambda z: z[1])
            for i, (nn, q, y, dg, t, L) in enumerate(lst):
                if i > 0 and dg == dg and dg > EPS and t == t and L == L:
                    pt, pL = lst[i - 1][4], lst[i - 1][5]
                    if pt == pt and pL == pL and pL > 0 and pt >= pL:
                        ra = revY.get((n, y)); mg = ra[0] / ra[1] if ra and ra[1] else 0.0
                        g4_den += 1; g4_nat.setdefault(n, [0, 0])[1] += 1
                        if t > L + mg: g4_num += 1; g4_nat[n][0] += 1
                if dg == dg and dg > EPS and t == t and L == L and L > 0 and 1800 <= y <= 1913:
                    mid_all += 1
                    if 0.3 <= t / L < 1.0: mid_n += 1; mid_debt.append(dg)
    out = []
    g9cells = sum(1 for sd in seeds for n in G9N for D in range(1820, 1910, 10))
    g9pos = sum(1 for v in g9.values() if v > 1e-9)
    def cellmean(dd):
        return sum(dd.get(k, 0.0) / g9gdp[k] for k in g9gdp) / g9cells if g9cells else float('nan')
    M['g9pos'] = g9pos; M['g9cells'] = g9cells
    M['g9m'] = cellmean(g9); M['g9r'] = cellmean(g9r); M['g9f'] = cellmean(g9f)
    s4 = [x for n in G9N for x in M['nat_stick'].get(n, [])]
    M['stick4'] = sum(s4) / len(s4) if s4 else float('nan')
    allv = M['stick'].get('all', [])
    M['stick9'] = sum(allv) / len(allv) if allv else float('nan')
    M['g1'] = (g1_bad, g1_n); M['g2'] = (g2_co, g2_b)
    M['g4'] = (g4_num / g4_den if g4_den else float('nan'), g4_num, g4_den)
    M['g5'] = (sum(1 for m in maxratio if m > 5), maxrate, maxq)
    M['brit'] = {D: st.median(M['med'][('イギリス', D)]) for D in range(1800, 1950, 10) if ('イギリス', D) in M['med']}
    out.append(f'G1 払えない額の恒等式（Borrow が受けた払えない額 = 準備金 − 払える額−請求・無ければ noShortfall）の食い違い {g1_bad} 月／{g1_n} 月')
    out.append(f'P1 4 国（仏・墺・日・米）の 0 に張り付く {M["stick4"]:.4f}（n={len(s4)}）')
    out.append(f'G9 入口の件数：仏・墺・日・米 の 1820〜1900 年代 発行＞0 のセル {g9pos}／{g9cells}（基線 34・N1 81・N2 37）')
    out.append(f'G9m 入口の大きさ：発行÷年GDP のセル平均 {M["g9m"]:.5f}／返済 {M["g9r"]:.5f}／うち床の返済 {M["g9f"]:.5f}')
    out.append(f'G2 借りた月 {g2_b}・うち減債も出た月 {g2_co}（割合 {g2_co / g2_b if g2_b else float("nan"):.4f}）')
    out.append(f'G3 記録：0 に張り付く 全体 {M["stick9"]:.3f}（n={len(allv)}）／' + '・'.join(f'{n} {sum(v) / len(v):.3f}' for n, v in sorted(M['nat_stick'].items())))
    out.append(f'G4 準備金のための借入：債務ありで前の四半期末の国庫≥線 の国×四半期 {g4_den} のうち 四半期末の国庫＞線＋1 か月の歳入 {g4_num}（割合 {M["g4"][0]:.4f}）／' + '・'.join(f'{n} {v[0]}/{v[1]}' for n, v in sorted(g4_nat.items())))
    out.append(f'G5 国債÷年GDP の最大（種ごと）{" ".join(f"{m:.2f}" for m in maxratio)}／5 超の種 {M["g5"][0]}／国債の金利の最大 {maxrate:.3f}／四半期の比の最大 {maxq:.2f}')
    out.append('G6 借りた月の割合 ' + '・'.join(f'{n} {v[0] / v[1]:.3f}' for n, v in sorted(borrowm.items())))
    out.append('G6 不履行の数（種の和） ' + '・'.join(f'{n} {int(sum(dflt.get((sd, n), 0) for sd in seeds))}' for n in NAT))
    out.append('G7 記録：潤沢線÷年名目GDP の中央値（1800〜1913） ' + '・'.join(f'{n} {st.median(v):.3f}' for n, v in sorted(lgdp.items())))
    out.append(f'G10 記録：中間の帯（債務あり・0.3≤国庫÷線＜1）の国×四半期の割合 {mid_n}／{mid_all}（{mid_n / mid_all if mid_all else float("nan"):.3f}）・その債務÷年GDP の平均 {sum(mid_debt) / len(mid_debt) if mid_debt else float("nan"):.3f}')
    out.append(f'G10 記録：利払い÷歳入（1800〜1913・9 国の和） {intr / rev if rev > 0 else float("nan"):.4f}／払えない額が出た月のうち門が閉じていた割合 {gclosed / short if short else float("nan"):.4f}（{gclosed}／{short}）')
    for n in ['フランス', 'オーストリア', '日本', 'イギリス', 'アメリカ', 'プロイセン']:
        out.append(f'G6 国債÷年GDP 十年の終わりの中央値 {n}：' + ' '.join(f'{D}:{st.median(M["med"][(n, D)]):.2f}' for D in range(1800, 1950, 10) if (n, D) in M['med']))
    return out, M

def judge(B, base, n1, n2):
    ok = lambda b: '命中' if b else '外れ'
    nul = [base[1], n1[1], n2[1]]
    L = []
    L.append(f'P1 4 国の 0 に張り付く：B {B[1]["stick4"]:.4f}／基線 {base[1]["stick4"]:.4f}・N1 {n1[1]["stick4"]:.4f}・N2 {n2[1]["stick4"]:.4f}（ヌル腕の小さい方 {min(n1[1]["stick4"], n2[1]["stick4"]):.4f}）→ ' + ok(B[1]['stick4'] < min(n1[1]['stick4'], n2[1]['stick4'])))
    g = [x['g9m'] for x in nul]; thr = max(g)
    L.append(f'G9m 入口の大きさ：B {B[1]["g9m"]:.5f}／基線 {base[1]["g9m"]:.5f}・N1 {n1[1]["g9m"]:.5f}・N2 {n2[1]["g9m"]:.5f}（しきい値 max(基線,N1,N2) = {thr:.5f}）→ ' + ok(B[1]['g9m'] > thr))
    di = B[1]['g9m'] - base[1]['g9m']; df = B[1]['g9f'] - base[1]['g9f']
    L.append(f'PATH 経路：Δ発行 {di:+.5f}・Δ床の返済 {df:+.5f}（床が減った分 {max(0.0, -df):.5f}）→ ' + ('借入の経路' if di > max(0.0, -df) else '狙いと別の経路（床の返済の減り以下）'))
    L.append(f'G9 件数（記録）：B {B[1]["g9pos"]}／基線 {base[1]["g9pos"]}・N1 {n1[1]["g9pos"]}・N2 {n2[1]["g9pos"]}')
    L.append(f'G1 恒等式の食い違い {B[1]["g1"][0]}／{B[1]["g1"][1]} → ' + ('通る' if B[1]['g1'][0] == 0 and B[1]['g1'][1] > 0 else '落ちる'))
    L.append(f'G2 借りた月に減債も出た月 {B[1]["g2"][0]}／{B[1]["g2"][1]} → ' + ('通る' if B[1]['g2'][0] == 0 else '落ちる'))
    L.append(f'G4 準備金のための借入の割合 {B[1]["g4"][0]:.4f}（{B[1]["g4"][1]}／{B[1]["g4"][2]}）→ ' + ('通る（10% 未満）' if B[1]['g4'][0] < 0.10 else '落ちる（10% 以上）'))
    bb = B[1]['brit']; ba = base[1]['brit']
    dd = {D: bb[D] - ba[D] for D in bb if D in ba and 1800 <= D <= 1940}
    mxd = max(dd.values()) if dd else float('nan')
    L.append(f'G7 英の国債÷年GDP の十年の終わりの中央値 B−基線 の最大 {mxd:.3f}（全ての十年で＜0.19）→ ' + ('通る' if mxd < 0.19 else '落ちる'))
    L.append(f'G5 発散：5 超の種 {B[1]["g5"][0]}・国債の金利の最大 {B[1]["g5"][1]:.3f}・四半期の比の最大 {B[1]["g5"][2]:.2f}（基線 {base[1]["g5"][2]:.2f}・N1 {n1[1]["g5"][2]:.2f}・N2 {n2[1]["g5"][2]:.2f}）→ ' + ('100 倍未満で通る' if B[1]['g5'][2] < 100 else '落ちる'))
    return L

if __name__ == '__main__':
    a = sys.argv[1:]
    if a and a[0] == '--judge':
        res = []
        for d in a[1:5]:
            print(f'=== {d}'); o, M = run(d)
            for l in o: print(l)
            res.append((o, M))
        print('=== 判定')
        for l in judge(*res): print(l)
    else:
        for d in a:
            print(f'=== {d}')
            for l in run(d)[0]: print(l)
