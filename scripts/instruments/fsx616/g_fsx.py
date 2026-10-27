#!/usr/bin/env python3
"""(616) 線 fsx の読み手＝事前登録 measurements/fsx616_20261028_prereg.txt の G1・G3・G3x・G4・G6・G7・G8 を、計器の生から読む。

使い方（PYTHONUTF8=1）：
  python g_fsx.py <ラベル>=<生のフォルダ> [<ラベル>=<生のフォルダ> ...]
  ラベルの決まり（追記 2）：base・n1・n2＝比べる側（旗なしの木・近似の線で読む）／Ains＝旗なしの木の計器つき 10 種（厳密の線 fsx_s を持つ）／B＝腕 B（旗 2 本）／B1・B2＝切り分け。
  例：python g_fsx.py base=D:/PoB_work/keep_609/m609_ins n1=D:/PoB_work/keep_609/n1 n2=D:/PoB_work/keep_609/n2 Ains=D:/PoB_work/fsx_616/work/out_Ains_10/ins B=D:/PoB_work/fsx_616/work/out_B_10/ins
生のフォルダ＝fiscal_s<seed>.tsv（9 国・月ごと）と、新しい木だけ fsx_s<seed>.tsv（全ての国・四半期ごと）・money_s<seed>.tsv（9 国・四半期ごと）が入っている所。

読むもの：
  近似の線（全ての腕で同じ式・追記 2 で海軍を足した）：fiscal_s から 潤沢線 ≈ (max(1, 5×(利払い＋行政＋陸の装備＋海の装備の請求)) ＋ 0.5×5×兵士給与) × 8（OpulenceLineOf の近似）。
    G3（近似）：債務あり（govDebtStock＞0）の月で  超過の大きさ＝max(0, 国庫−線)÷線 の平均（国別・主）／国庫>線 の月の割合（記録）／その状態が連続する長さの最大（記録・四半期＝3 か月に換算・国×種の中央値）。
    G7：同じ国×3 か月で 借入と国債の返済（絶対値）が両方ある割合（記録）。
  厳密（fsx_s・旗なしの木の Ains も持つ）：
    G1（実装の確かめ）：pressureSMax の最大・pressureForceMax と pressureSMax の一致（B のみ）・S ≤ M の割合と外れた行。
    G3x（厳密）：四半期末の 債務あり の行で  超過の大きさ＝max(0, treasuryEnd−lineEnd)÷lineEnd の平均（国別）を B と Ains で並べる。
    G4：repayAboveFloorSum＞0 の国×四半期の数と、そのうち govDebtEnd が前の四半期より下がった割合。
    G6：四半期末に国庫>線（treasuryOverLineFlag）の割合を債務の有無別に。
    G8（記録）：開幕 8 四半期の repayDoneSum÷lineEnd（国別・種の中央値）と、money_s の開幕 8 四半期の ln(P後/P前) の最大・広義の貨幣の q1→q8 の比。
    線の近似の誤差の自己申告：B の fiscal_s の近似の線を、同じ四半期末の fsx_s の lineEnd と並べた比（近似÷厳密）の p5・中央値・p95（対応は月の番号 3q と国庫の一致で確かめる）。
合否（追記 2 の判定）は末尾に PASS／FAIL で出す（G1・G3・G3x の向き）。
"""
import csv, glob, math, os, re, statistics, sys
from collections import defaultdict

BUF_MONTHS = 5.0   # FiscalStressBufferMonths
SOLDIER_W = 0.5    # FiscalBufferSoldierPayWeight
OPULENCE_Q = 8.0   # OpulenceBufferQuarters
SIX = ['アメリカ', 'イギリス', 'オスマン', 'オーストリア', 'フランス', 'ロシア']   # G3 の対象（追記 1・基線の近似で最小が 20% 以上の国）
EPS = 1e-9


def seed_of(path):
    m = re.search(r'_s(\d+)\.tsv$', path)
    return int(m.group(1)) if m else -1


def fnum(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def pct(xs, p):
    xs = sorted(xs)
    if not xs:
        return float('nan')
    k = (len(xs) - 1) * p
    lo, hi = int(math.floor(k)), int(math.ceil(k))
    return xs[lo] + (xs[hi] - xs[lo]) * (k - lo)


def approx_line(g):
    """FiscalBufferOf・OpulenceLineOf の近似（Last* の写しは請求の列）。海軍は追記 2 で足した。"""
    buf = max(1.0, BUF_MONTHS * (g('claimInterestPerMonth') + g('claimAdminPerMonth') + g('claimArmyPerMonth') + g('claimNavyPerMonth')))
    return (buf + SOLDIER_W * BUF_MONTHS * g('claimSoldierPayPerMonth')) * OPULENCE_Q


def read_fiscal(folder):
    """(種, 国) -> [(monthIndex, year, debt, treasury, line, borrowed, repay)]"""
    out = defaultdict(list)
    for f in sorted(glob.glob(os.path.join(folder, 'fiscal_s*.tsv'))):
        sd = seed_of(f)
        with open(f, encoding='utf-8') as fh:
            r = csv.reader(fh, delimiter='\t')
            h = next(r)
            ix = {n: i for i, n in enumerate(h)}
            need = ['monthIndex', 'nation', 'year', 'govDebtStock', 'treasuryEndStock', 'claimInterestPerMonth', 'claimAdminPerMonth',
                    'claimArmyPerMonth', 'claimNavyPerMonth', 'claimSoldierPayPerMonth', 'borrowedPerMonth', 'ledgerDebtRepayPerMonth']
            if any(n not in ix for n in need):
                continue
            for row in r:
                if len(row) < len(h):
                    continue
                g = lambda k: fnum(row[ix[k]])
                out[(sd, row[ix['nation']])].append((g('monthIndex'), g('year'), g('govDebtStock'), g('treasuryEndStock'), approx_line(g), g('borrowedPerMonth'), g('ledgerDebtRepayPerMonth')))
    return out


def longest_run(flags):
    best = cur = 0
    for f in flags:
        cur = cur + 1 if f else 0
        best = max(best, cur)
    return best


def fiscal_report(label, folder):
    """返り値＝国 -> 超過の大きさの平均（G3 近似の主）。"""
    data = read_fiscal(folder)
    if not data:
        print(f'[{label}] fiscal_s なし'); return {}
    by_nat = defaultdict(lambda: {'months': 0, 'debt_months': 0, 'over_debt': 0, 'ratio_sum': 0.0, 'runs': [], 'qs': 0, 'churn': 0})
    for (sd, nat), rows in data.items():
        rows.sort()
        b = by_nat[nat]
        flags = []
        for (mi, yr, debt, tre, line, bor, rep) in rows:
            on = debt > EPS and tre > line
            flags.append(on)
            b['months'] += 1; b['over_debt'] += 1 if on else 0
            if debt > EPS and line > 0:
                b['debt_months'] += 1; b['ratio_sum'] += max(0.0, tre - line) / line if tre == tre else 0.0
        b['runs'].append(longest_run(flags) / 3.0)
        for k in range(0, len(rows) - 2, 3):
            seg = rows[k:k + 3]
            b['qs'] += 1
            if sum(x[5] for x in seg if x[5] == x[5]) > 0 and sum(abs(x[6]) for x in seg if x[6] == x[6]) > 0:
                b['churn'] += 1
    print(f'[{label}] 近似の線（fiscal_s・海軍込み）  国  超過の大きさ（債務ありの月の平均 max(0,国庫−線)÷線・主）  債務あり&国庫>線の月の割合（記録）  連続の最大（四半期・種の中央値・記録）  借入と返済が同じ3か月にある割合（G7）')
    res = {}
    for nat in sorted(by_nat):
        b = by_nat[nat]
        ov = b['ratio_sum'] / max(1, b['debt_months'])
        res[nat] = ov
        print(f'  {nat:<8} {ov:9.4f}   {100 * b["over_debt"] / max(1, b["months"]):6.1f}%   {statistics.median(b["runs"]):7.1f}   {100 * b["churn"] / max(1, b["qs"]):6.1f}%')
    return res


def read_fsx(folder):
    """(種, 国) -> [(順番, row dict)]（q の行だけ）"""
    out = defaultdict(list)
    for f in sorted(glob.glob(os.path.join(folder, 'fsx_s*.tsv'))):
        sd = seed_of(f)
        with open(f, encoding='utf-8') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {n: i for i, n in enumerate(h)}
            for row in r:
                if len(row) < len(h) or not row[ix['time']].startswith('q'):
                    continue
                d = {n: row[i] for n, i in ix.items()}
                out[(sd, d['nation'])].append((int(row[ix['time']][1:]), d))
    for k in out:
        out[k].sort(key=lambda x: x[0])
    return out


def exact_overshoot(fsx):
    """国 -> 超過の大きさの平均（四半期末・債務ありの行・max(0,T−L)÷L）"""
    acc = defaultdict(lambda: [0.0, 0])
    for (sd, nat), rows in fsx.items():
        for (_, d) in rows:
            debt, tre, line = fnum(d['govDebtEnd']), fnum(d['treasuryEnd']), fnum(d['lineEnd'])
            if debt > EPS and line > 0 and tre == tre:
                acc[nat][0] += max(0.0, tre - line) / line; acc[nat][1] += 1
    return {n: a[0] / max(1, a[1]) for n, a in acc.items()}


def fsx_report(label, folder, is_arm):
    """返り値＝(G1 の合否, 国 -> 厳密な超過の大きさ)。fsx_s が無ければ (None, {})。"""
    fsx = read_fsx(folder)
    if not fsx:
        print(f'[{label}] fsx_s なし'); return None, {}
    smax = fmax = 0.0; dev_rows = s_le_m = 0; bad = []; force_ne_s = 0
    above_rows = above_down = 0; over_d = n_d = over_n = n_n = 0
    per_seed_above = defaultdict(int)
    for (sd, nat), rows in fsx.items():
        prev_debt = None
        for (_, d) in rows:
            g = lambda k: fnum(d[k])
            smax = max(smax, g('pressureSMax')); fmax = max(fmax, g('pressureForceMax'))
            if abs(g('pressureForceMax') - g('pressureSMax')) > 1e-12 and is_arm and g('pressureForceMax') > 0:
                force_ne_s += 1
            if int(g('devalueMonths')) > 0:
                dev_rows += 1
                if g('pressureSSum') <= g('pressureMSum') + EPS:
                    s_le_m += 1
                elif len(bad) < 20:
                    bad.append((sd, nat, d['time'], g('pressureSSum'), g('pressureMSum')))
            debt = g('govDebtEnd')
            if g('repayAboveFloorSum') > 0:
                above_rows += 1; per_seed_above[sd] += 1
                if prev_debt is not None and debt < prev_debt:
                    above_down += 1
            prev_debt = debt
            if debt > EPS:
                n_d += 1; over_d += 1 if d['treasuryOverLineFlag'] == '1' else 0
            else:
                n_n += 1; over_n += 1 if d['treasuryOverLineFlag'] == '1' else 0
    g1 = (smax <= 1 + EPS) and (force_ne_s == 0)
    print(f'[{label}] fsx_s（全ての国・厳密）')
    print(f'  G1  pressureSMax の最大 {smax:.12g}（≤ 1+1e-9 のはず）・pressureForceMax の最大 {fmax:.12g}・効いた式と S の不一致の行 {force_ne_s}（旗 1 の腕だけ数える）  → {"PASS" if g1 else "FAIL"}（実装の確かめ）')
    print(f'      切り下げのあった国×四半期 {dev_rows}・S ≤ M の割合 {100 * s_le_m / max(1, dev_rows):.2f}%  外れた行（最大 20）：{bad}')
    print(f'  G4  repayAboveFloorSum＞0 の国×四半期 {above_rows}・うち govDebtEnd が前の四半期より下がった割合 {100 * above_down / max(1, above_rows):.1f}%  種ごと {sorted(per_seed_above.items())}')
    print(f'  G6  四半期末に国庫>線（厳密・等号なしの `>`）：債務あり {100 * over_d / max(1, n_d):.1f}%（{n_d} 行）・債務なし {100 * over_n / max(1, n_n):.1f}%（{n_n} 行）')
    ex = exact_overshoot(fsx)
    print('  G3x 厳密な超過の大きさ（四半期末・債務ありの行の平均 max(0,国庫−線)÷線）：' + '  '.join(f'{n}={v:.4f}' for n, v in sorted(ex.items())))
    # G8：開幕 8 四半期の返済
    rep = defaultdict(list)
    for (sd, nat), rows in fsx.items():
        for (_, d) in rows[:8]:
            ln = fnum(d['lineEnd'])
            if ln > 0:
                rep[nat].append(fnum(d['repayDoneSum']) / ln)
    print('  G8  開幕 8 四半期の repayDoneSum÷lineEnd（国別・全種全四半期の中央値／最大）：' + '  '.join(f'{n}={statistics.median(v):.4f}/{max(v):.3f}' for n, v in sorted(rep.items()) if v))
    return g1, ex


def line_error(label, folder):
    """B の fiscal_s の近似の線÷fsx_s の厳密な lineEnd（四半期末＝月の番号 3q）。対応は国庫の一致で確かめる。"""
    fsx = read_fsx(folder); fis = read_fiscal(folder)
    if not fsx or not fis:
        return
    ratios = []; match = tot = 0
    for key, rows in fsx.items():
        fr = {int(m[0]): m for m in fis.get(key, [])}
        for (q, d) in rows:
            m = fr.get(3 * q)
            if m is None:
                continue
            tot += 1
            tre_x = fnum(d['treasuryEnd'])
            if abs(m[3] - tre_x) <= 1e-6 * max(1.0, abs(tre_x)):
                match += 1
            ln = fnum(d['lineEnd'])
            if ln > 0:
                ratios.append(m[4] / ln)
    if not ratios:
        print(f'[{label}] 線の近似の誤差：対応する行なし'); return
    print(f'[{label}] 線の近似の誤差の自己申告（近似÷厳密・四半期末の対応 {tot} 行・国庫が一致した割合 {100 * match / max(1, tot):.1f}%）：p5 {pct(ratios, .05):.4f}・p25 {pct(ratios, .25):.4f}・中央値 {pct(ratios, .5):.4f}・p75 {pct(ratios, .75):.4f}・p95 {pct(ratios, .95):.4f}')


def opening_money(label, folder):
    """G8（記録）：money_s の開幕 8 四半期の ln(P後/P前) の最大と、広義の貨幣の q1→q8 の比。"""
    acc = defaultdict(lambda: [[], []])
    for f in sorted(glob.glob(os.path.join(folder, 'money_s*.tsv'))):
        with open(f, encoding='utf-8') as fh:
            r = csv.DictReader(fh, delimiter='\t'); rows = defaultdict(list)
            for d in r:
                if d['time'].startswith('q'):
                    rows[d['nation']].append((int(d['time'][1:]), d))
        for nat, lst in rows.items():
            lst.sort(key=lambda x: x[0]); head = lst[:8]
            if len(head) < 8:
                continue
            mx = max((math.log(fnum(d['priceLevelAfterIndex']) / fnum(d['priceLevelBeforeIndex'])) for _, d in head
                      if fnum(d['priceLevelBeforeIndex']) > 0 and fnum(d['priceLevelAfterIndex']) > 0), default=float('nan'))
            b1, b8 = fnum(head[0][1]['broadMoneyStock']), fnum(head[7][1]['broadMoneyStock'])
            acc[nat][0].append(mx)
            if b1 > 0:
                acc[nat][1].append(b8 / b1)
    if not acc:
        print(f'[{label}] money_s なし'); return
    print(f'[{label}] G8 開幕 8 四半期（money_s・9 国）  ln(P後/P前) の最大（種の最大）／広義の貨幣 q8÷q1（種の中央値）：'
          + '  '.join(f'{n}={max(v[0]):.3f}/{statistics.median(v[1]):.3f}' for n, v in sorted(acc.items()) if v[0] and v[1]))


def main(argv):
    if not argv:
        print(__doc__); return 1
    approx = {}; exact = {}; g1s = {}
    for a in argv:
        label, _, folder = a.partition('=')
        approx[label] = fiscal_report(label, folder)
        is_arm = label.startswith('B')
        g1, ex = fsx_report(label, folder, is_arm)
        if g1 is not None:
            g1s[label] = g1; exact[label] = ex
        if is_arm:
            line_error(label, folder)
        opening_money(label, folder)
    # 合否（追記 2）
    refs = [l for l in ('base', 'n1', 'n2') if l in approx and approx[l]]
    for arm in [l for l in approx if l.startswith('B')]:
        print(f'\n=== 判定 {arm} ===')
        if arm in g1s:
            print(f'G1（実装の確かめ） {"PASS" if g1s[arm] else "FAIL"}')
        if refs:
            fails = []
            print('G3（近似・超過の大きさ）  国：' + '  '.join(f'{r}' for r in refs) + f'  最小  {arm}')
            for nat in SIX:
                mn = min(approx[r].get(nat, float('inf')) for r in refs)
                v = approx[arm].get(nat, float('nan'))
                ok = v < mn
                if not ok:
                    fails.append(nat)
                print(f'  {nat:<8} ' + '  '.join(f'{approx[r].get(nat, float("nan")):.4f}' for r in refs) + f'  {mn:.4f}  {v:.4f}  {"PASS" if ok else "FAIL"}')
            print(f'G3 {"PASS" if not fails else "FAIL " + str(fails)}')
        if 'Ains' in exact and arm in exact:
            fails = []
            print('G3x（厳密・超過の大きさ）  国：Ains  ' + arm)
            for nat in SIX:
                a, b = exact['Ains'].get(nat, float('nan')), exact[arm].get(nat, float('nan'))
                ok = b < a
                if not ok:
                    fails.append(nat)
                print(f'  {nat:<8} {a:.4f}  {b:.4f}  {"PASS" if ok else "FAIL"}')
            print(f'G3x {"PASS" if not fails else "FAIL " + str(fails)}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
