"""(259) 線T2：段A-0 の英の月ごとの行（fiscal_s<seed>.tsv・POB_INSTRUMENTS_NATIONS=イギリス の走行）の読み手（読むだけ）。
使い方：python -X utf8 an_g0_259_uk.py <計器の出力ディレクトリ> [国名=イギリス]
出力（窓ごと・種をまたいだ和）：Σ請求（Simulation の州の予算の和 claimAdmin／Fiscal の adminClaim／行政＋上下水道＋電化）・Σ実際（台帳 Admin の月の差分）・
  月ごとの Fisc/Sim の分布・r＜1 の月と r＝1 の月の行政の充足の分布・潤沢の上乗せの大きさ（Spendable−先月の歳入）÷先月の歳入。"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, num  # noqa: E402

WINS = [(1783, 1814), (1815, 1849), (1850, 1880), (1881, 1913), (1914, 1950)]


def pct(xs, p):
    xs = sorted(x for x in xs if x == x)
    if not xs:
        return float('nan')
    k = (len(xs) - 1) * p
    lo = int(k)
    hi = min(lo + 1, len(xs) - 1)
    return xs[lo] + (xs[hi] - xs[lo]) * (k - lo)


def dist(xs):
    xs = [x for x in xs if x == x and abs(x) != float('inf')]
    return 'n=0' if not xs else f'n={len(xs)} 中央{pct(xs, .5):.3f} 10%点{pct(xs, .1):.3f} 90%点{pct(xs, .9):.3f}'


def main():
    D = sys.argv[1]
    nation = sys.argv[2] if len(sys.argv) > 2 else 'イギリス'
    R = [r for sd, r in rows(D, 'fiscal') if r.get('nation') == nation and 'ledgerResidualPerMonth' in r]
    print(f'# {nation}・月の行 {len(R)}')
    for a, b in WINS + [(1783, 1950)]:
        xs = [r for r in R if a <= int(num(r['year'])) <= b]
        if not xs:
            continue
        sim = sum(num(r['claimAdminPerMonth']) for r in xs)
        cat = sum(num(r['claimAdminPerMonth']) + num(r['claimInfraPerMonth']) + num(r['claimElecInfraPerMonth']) for r in xs)
        fis = sum(num(r['adminClaimFiscalPerMonth']) for r in xs)
        paid = sum(-num(r['ledgerAdminPerMonth']) for r in xs)
        print(f'\n## {a}-{b}（月 {len(xs)}）')
        print(f'  Σ請求：Simulation の行政 {sim:.0f}・Fiscal の adminClaim {fis:.0f}（Fisc/Sim {fis / sim:.3f}）・行政＋上下水道＋電化 {cat:.0f}／Σ実際（台帳 Admin）{paid:.0f}')
        print(f'  実際/請求：対 Simulation の行政 {paid / sim:.3f}・対 Fiscal の adminClaim {paid / fis:.3f}・対 行政＋上下水道＋電化 {paid / cat:.3f}')
        print(f'  月ごとの Fisc/Sim {dist([num(r["adminClaimFiscalPerMonth"]) / num(r["claimAdminPerMonth"]) for r in xs if num(r["claimAdminPerMonth"]) > 0])}')
        op = [(num(r['adminSpendableAtBudgetPerMonth']) - num(r['adminLastRevenueAtBudgetPerMonth'])) / num(r['adminLastRevenueAtBudgetPerMonth']) for r in xs if num(r['adminLastRevenueAtBudgetPerMonth']) > 0]
        print(f'  潤沢の上乗せ(Spendable−先月歳入)÷先月歳入 {dist(op)}')
        lo = [r for r in xs if num(r['payRateShare']) < 1]
        hi = [r for r in xs if num(r['payRateShare']) >= 1]
        print(f'  r＜1 の月 {len(lo)}／{len(xs)}・行政の充足 r＜1 {dist([num(r["adminFillShare"]) for r in lo])}／r＝1 {dist([num(r["adminFillShare"]) for r in hi])}・r＜1 の月の r {dist([num(r["payRateShare"]) for r in lo])}')
        if lo:
            ar = [-num(r['ledgerAdminPerMonth']) / (num(r['claimAdminPerMonth']) + num(r['claimInfraPerMonth']) + num(r['claimElecInfraPerMonth'])) for r in lo if num(r['claimAdminPerMonth']) > 0]
            ah = [-num(r['ledgerAdminPerMonth']) / (num(r['claimAdminPerMonth']) + num(r['claimInfraPerMonth']) + num(r['claimElecInfraPerMonth'])) for r in hi if num(r['claimAdminPerMonth']) > 0]
            print(f'  実際/請求(行政＋上下水道＋電化)：r＜1 の月 {dist(ar)}／r＝1 の月 {dist(ah)}')


def false_shortage(D, nation):
    """偽の不足：払えない額（月初の払える額−請求＜0）のうち、行政＋上下水道＋電化の請求を実際の支払いに置き換えれば不足でなくなる月（読み手の側の反事実＝本体は動かさない）。
    払える額（Spare0＝spareBeforeBorrowPerMonth）＝払える額−請求。請求を (請求−行政系の請求＋行政系の実際) に替えると Spare0 は (行政系の請求−行政系の実際) だけ増える。
    ⚠実際は r を掛けた後の支払い（r＜1 の月は小さい）・借入や切り下げの後の月はその分も入る＝上限に近い見積り。"""
    R = [r for sd, r in rows(D, 'fiscal') if r.get('nation') == nation and 'ledgerResidualPerMonth' in r]
    print(f'\n# 偽の不足（{nation}・月 {len(R)}）')
    for a, b in WINS + [(1783, 1950)]:
        xs = [r for r in R if a <= int(num(r['year'])) <= b]
        sf = [r for r in xs if num(r['spareBeforeBorrowPerMonth']) < 0]
        if not sf:
            print(f'  {a}-{b}：払えない月 0／{len(xs)}')
            continue
        fixed = [r for r in sf if num(r['spareBeforeBorrowPerMonth']) + (num(r['claimAdminPerMonth']) + num(r['claimInfraPerMonth']) + num(r['claimElecInfraPerMonth']) + num(r['ledgerAdminPerMonth'])) >= 0]
        bor = sum(num(r['borrowedPerMonth']) for r in xs)
        borf = sum(num(r['borrowedPerMonth']) for r in fixed)
        lo = [r for r in xs if num(r['payRateShare']) < 1]
        lof = [r for r in lo if num(r['spareBeforeBorrowPerMonth']) + (num(r['claimAdminPerMonth']) + num(r['claimInfraPerMonth']) + num(r['claimElecInfraPerMonth']) + num(r['ledgerAdminPerMonth'])) >= 0]
        print(f'  {a}-{b}：払えない月 {len(sf)}／{len(xs)}・うち行政系の請求を実際にすれば不足でない月 {len(fixed)}（{len(fixed) / len(sf) * 100:.1f}%）・借入の額 Σ{bor:.0f} のうち その月の借入 {borf:.0f}（{borf / bor * 100 if bor > 0 else float("nan"):.1f}%）・r＜1 の月 {len(lo)} のうち偽の不足 {len(lof)}')


if __name__ == '__main__':
    main()
    false_shortage(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else 'イギリス')
