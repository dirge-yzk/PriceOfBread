#!/usr/bin/env python3
"""(616) 線 fsx の読み手＝事前登録 measurements/fsx616_20261028_prereg.txt の G1・G3・G4・G6・G7 を、計器の生から読む。

使い方（PYTHONUTF8=1）：
  python g_fsx.py <ラベル>=<生のフォルダ> [<ラベル>=<生のフォルダ> ...]
  例：python g_fsx.py base=D:/PoB_work/keep_609/m609_ins n1=D:/PoB_work/keep_609/n1 n2=D:/PoB_work/keep_609/n2 B=D:/PoB_work/fsx_616/B/ins
生のフォルダ＝fiscal_s<seed>.tsv（9 国・月ごと）と、腕 B だけ fsx_s<seed>.tsv（全ての国・四半期ごと）が入っている所。

読むもの：
  近似（全ての腕で同じ式）：fiscal_s から 潤沢線 ≈ (max(1, 5×(利払い＋行政＋陸の装備の請求)) ＋ 0.5×5×兵士給与) × 8（OpulenceLineOf の近似・海軍は入れない）。
    G3/G6：債務あり（govDebtStock＞0）で 国庫（treasuryEndStock）＞線 の月の割合（国別）と、その状態が連続する長さの最大（四半期＝3 か月に換算・国×種の中央値）。
    G7：同じ国×3 か月で 借入（borrowedPerMonth の和＞0）と 国債の返済（ledgerDebtRepayPerMonth は出の項で負に記帳される＝絶対値の和＞0）が両方ある割合。
    G4（記録）：国×年の govDebtStock の中央値（種をまたぐ）。
  腕 B の厳密（fsx_s）：
    G1：pressureSMax の最大・pressureForceMax の最大（≤ 1＋1e-9 のはず）・切り下げのあった国×四半期で pressureSSum ≤ pressureMSum＋1e-9 の割合と外れた行。
    G4：repayAboveFloorSum＞0 の国×四半期の数と、そのうち govDebtEnd が前の四半期より下がった割合。
    G6：四半期末に国庫＞線（treasuryOverLineFlag）の割合を債務の有無別に。
発散（jump100・nonfinite・残り）は scripts/instruments/pfix603/g_pfix.py をそのまま使う（新しい読み手は作らない）。
"""
import csv, glob, os, re, statistics, sys
from collections import defaultdict

BUF_MONTHS = 5.0   # FiscalStressBufferMonths
SOLDIER_W = 0.5    # FiscalBufferSoldierPayWeight
OPULENCE_Q = 8.0   # OpulenceBufferQuarters


def seed_of(path):
    m = re.search(r'_s(\d+)\.tsv$', path)
    return int(m.group(1)) if m else -1


def fnum(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


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
                    'claimArmyPerMonth', 'claimSoldierPayPerMonth', 'borrowedPerMonth', 'ledgerDebtRepayPerMonth']
            if any(n not in ix for n in need):
                continue
            for row in r:
                if len(row) < len(h):
                    continue
                g = lambda k: fnum(row[ix[k]])
                buf = max(1.0, BUF_MONTHS * (g('claimInterestPerMonth') + g('claimAdminPerMonth') + g('claimArmyPerMonth')))
                line = (buf + SOLDIER_W * BUF_MONTHS * g('claimSoldierPayPerMonth')) * OPULENCE_Q
                out[(sd, row[ix['nation']])].append((g('monthIndex'), g('year'), g('govDebtStock'), g('treasuryEndStock'), line, g('borrowedPerMonth'), g('ledgerDebtRepayPerMonth')))
    return out


def longest_run(flags):
    best = cur = 0
    for f in flags:
        cur = cur + 1 if f else 0
        best = max(best, cur)
    return best


def fiscal_report(label, folder):
    data = read_fiscal(folder)
    if not data:
        print(f'[{label}] fiscal_s なし'); return
    by_nat = defaultdict(lambda: {'months': 0, 'over_debt': 0, 'runs': [], 'qs': 0, 'churn': 0, 'debt_year': defaultdict(list)})
    for (sd, nat), rows in data.items():
        rows.sort()
        b = by_nat[nat]
        flags = []
        for (mi, yr, debt, tre, line, bor, rep) in rows:
            on = debt > 1e-9 and tre > line
            flags.append(on)
            b['months'] += 1; b['over_debt'] += 1 if on else 0
            b['debt_year'][int(yr)].append(debt)
        b['runs'].append(longest_run(flags) / 3.0)
        for k in range(0, len(rows) - 2, 3):
            seg = rows[k:k + 3]
            b['qs'] += 1
            if sum(x[5] for x in seg if x[5] == x[5]) > 0 and sum(abs(x[6]) for x in seg if x[6] == x[6]) > 0:
                b['churn'] += 1
    print(f'[{label}] 近似の線（fiscal_s）  国  債務あり&国庫>線の月の割合  連続の最大（四半期・種の中央値）  借入と返済が同じ3か月にある割合')
    for nat in sorted(by_nat):
        b = by_nat[nat]
        print(f'  {nat:<8} {100 * b["over_debt"] / max(1, b["months"]):6.1f}%   {statistics.median(b["runs"]):7.1f}   {100 * b["churn"] / max(1, b["qs"]):6.1f}%')


def fsx_report(label, folder):
    files = sorted(glob.glob(os.path.join(folder, 'fsx_s*.tsv')))
    if not files:
        print(f'[{label}] fsx_s なし'); return
    smax = fmax = 0.0; dev_rows = s_le_m = 0; bad = []
    above_rows = above_down = 0; over_d = n_d = over_n = n_n = 0
    per_seed_above = {}
    for f in files:
        sd = seed_of(f); prev_debt = {}; cnt = 0
        with open(f, encoding='utf-8') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {n: i for i, n in enumerate(h)}
            for row in r:
                if len(row) < len(h) or not row[ix['time']].startswith('q'):   # 助走の行（w…）は外す
                    continue
                g = lambda k: fnum(row[ix[k]])
                nat = row[ix['nation']]
                smax = max(smax, g('pressureSMax')); fmax = max(fmax, g('pressureForceMax'))
                if int(g('devalueMonths')) > 0:
                    dev_rows += 1
                    if g('pressureSSum') <= g('pressureMSum') + 1e-9:
                        s_le_m += 1
                    elif len(bad) < 20:
                        bad.append((sd, nat, row[ix['time']], g('pressureSSum'), g('pressureMSum')))
                debt = g('govDebtEnd')
                if g('repayAboveFloorSum') > 0:
                    above_rows += 1; cnt += 1
                    if nat in prev_debt and debt < prev_debt[nat]:
                        above_down += 1
                prev_debt[nat] = debt
                if debt > 1e-9:
                    n_d += 1; over_d += 1 if row[ix['treasuryOverLineFlag']] == '1' else 0
                else:
                    n_n += 1; over_n += 1 if row[ix['treasuryOverLineFlag']] == '1' else 0
        per_seed_above[sd] = cnt
    print(f'[{label}] fsx_s（全ての国）')
    print(f'  G1  pressureSMax の最大 {smax:.12g}（≤ 1+1e-9 のはず）・pressureForceMax の最大 {fmax:.12g}')
    print(f'      切り下げのあった国×四半期 {dev_rows}・S ≤ M の割合 {100 * s_le_m / max(1, dev_rows):.2f}%  外れた行（最大 20）：{bad}')
    print(f'  G4  repayAboveFloorSum＞0 の国×四半期 {above_rows}・うち govDebtEnd が前の四半期より下がった割合 {100 * above_down / max(1, above_rows):.1f}%  種ごと {sorted(per_seed_above.items())}')
    print(f'  G6  四半期末に国庫>線：債務あり {100 * over_d / max(1, n_d):.1f}%（{n_d} 行）・債務なし {100 * over_n / max(1, n_n):.1f}%（{n_n} 行）')


def main(argv):
    if not argv:
        print(__doc__); return 1
    for a in argv:
        label, _, folder = a.partition('=')
        fiscal_report(label, folder)
        fsx_report(label, folder)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
