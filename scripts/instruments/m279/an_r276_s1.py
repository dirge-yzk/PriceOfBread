# ★(377) 開幕の貯蓄の置き直しの機構（OpeningSavingsRebaseMode・SavingsYearsByClass）は (375) で削除した＝再走は削除の前の木で
"""(279) 線R′ (276) の反証役 v 中#1 の処置：腕 A（基線）と腕 B（OpeningSavingsRebaseMode=2）の英・種1 の財政の内訳を、生の計器から並べる（読むだけ）。
使い方：python an_r276_s1.py <腕Aの計器の出力> <腕Bの計器の出力> [種=1]
出す物：①月 0〜8 の財政の行（歳入・直接税・間接税・台帳の項目・利払いの請求・利払い÷歳入・国債の金利・平均クーポン・兌換・金本位・借入の門の拘束・払える率・切り下げ）
②四半期 0〜3 の国債の金利の内訳（nation_s の bond…＝基礎・債務比・期待インフレ・信認喪失・政策金利・議会・上限の項）と GDP デフレーター（名目÷実質）
③断面 warm（助走の直後）と q0 の全階級の貯蓄÷年所得（反証役 v 低#2：置き直した直後と q0 の差）。
⚠限界：種1 だけ＝帰属の傍証で証明ではない。腕 B の置き直しはモード2＝助走の最初の四半期の後＝warm はその 19 四半期後（置き直しの瞬間の断面は計器に無い）。"""
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
UK = 'イギリス'


def table(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for line in f:
            yield dict(zip(h, line.rstrip('\n').split('\t')))


def fl(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return float('nan')


def load(D, sd):
    fis = [r for r in table(os.path.join(D, f'fiscal_s{sd}.tsv')) if r['nation'] == UK]
    nat = {int(r['quarter']): r for r in table(os.path.join(D, f'nation_s{sd}.tsv')) if r['nation'] == UK}
    stk = {r['time']: r for r in table(os.path.join(D, f'stock_s{sd}.tsv')) if r['nation'] == UK}
    cls = {}
    for r in table(os.path.join(D, f'class_s{sd}.tsv')):
        if r['nation'] == UK:
            cls.setdefault(r['time'], []).append(r)
    return fis, nat, stk, cls


def g(x, n=4):
    return 'nan' if not (x == x) else (f'{x:.{n}f}' if abs(x) < 100 else f'{x:,.0f}')


def main():
    A, B = sys.argv[1], sys.argv[2]; sd = sys.argv[3] if len(sys.argv) > 3 else '1'
    LA, LB = load(A, sd), load(B, sd)
    out = []
    out.append(f'# (279) 線R′ の種{sd}・英の財政の内訳（腕 A＝基線 → 腕 B＝モード2）')
    cols = ['revenueBasePerMonth', 'directTaxNetPerMonth', 'indirectRevenuePerMonth', 'claimInterestPerMonth', 'claimTotalPerMonth', 'interestToRevenueShareNow',
            'govBondRatePerYear', 'govAvgCouponPerYear', 'govDebtStock', 'convertibilityShare', 'onGold', 'borrowBinder', 'borrowedPerMonth', 'payRateShare', 'devaluedPerMonth', 'defaultCount']
    fa = {int(r['monthIndex']): r for r in LA[0]}; fb = {int(r['monthIndex']): r for r in LB[0]}
    ledg = [c for c in (LA[0][0].keys() if LA[0] else []) if c.startswith('ledger') and c.endswith('PerMonth') and c not in ('ledgerSumPerMonth', 'ledgerResidualPerMonth')]
    out.append('\n## ① 月ごと（monthIndex 0〜8）＝ A → B')
    for c in cols + ledg:
        vals = []
        for m in range(0, 9):
            ra, rb = fa.get(m), fb.get(m)
            if c == 'borrowBinder':
                vals.append(f'{ra[c] if ra else "-"}→{rb[c] if rb else "-"}')
            else:
                vals.append(f'{g(fl(ra[c])) if ra and c in ra else "-"}→{g(fl(rb[c])) if rb and c in rb else "-"}')
        if c in ledg and all(v in ('0.0000→0.0000', '-→-') for v in vals):
            continue
        out.append(f'{c}\t' + '\t'.join(f'm{m}:{v}' for m, v in enumerate(vals)))
    out.append('\n## ② 四半期 0〜3：国債の金利の内訳（nation_s・年率）と GDP デフレーター（名目÷実質）＝ A → B')
    bc = [c for c in (LA[1][0].keys() if 0 in LA[1] else []) if c.startswith('bond')]
    for c in bc + ['nominalGdpPerYear']:
        out.append(f'{c}\t' + '\t'.join(f'Q{q + 1}:{g(fl(LA[1][q][c])) if q in LA[1] else "-"}→{g(fl(LB[1][q][c])) if q in LB[1] else "-"}' for q in range(4)))
    def defl(L, q):
        n = L[1].get(q); s = L[2].get(f'q{q}')
        return fl(n['nominalGdpPerYear']) / fl(s['gdpRealPerYear']) if n and s and fl(s['gdpRealPerYear']) > 0 else float('nan')
    out.append('gdpDeflator\t' + '\t'.join(f'Q{q + 1}:{g(defl(LA, q))}→{g(defl(LB, q))}' for q in range(4)))
    out.append('\n## ③ 全階級の貯蓄÷年所得（Σ savingsStock ÷ (Σ earnedPerMonth×12)）')
    for t in ('warm', 'q0', 'q1', 'q4'):
        def r_(L):
            rows = L[3].get(t, [])
            s = sum(fl(r['savingsStock']) for r in rows); e = sum(fl(r['earnedPerMonth']) for r in rows)
            return s / (12 * e) if e > 0 else float('nan')
        out.append(f'{t}\tA {g(r_(LA), 3)}\tB {g(r_(LB), 3)}')
    def w_(L):   # warm の Earned は計器が読まない（助走の月を数えない＝nan）＝warm の貯蓄を q0 の年所得で割る
        sw = sum(fl(r['savingsStock']) for r in L[3].get('warm', [])); e0 = sum(fl(r['earnedPerMonth']) for r in L[3].get('q0', []))
        return sw / (12 * e0) if e0 > 0 else float('nan')
    out.append(f'warm の貯蓄÷q0 の年所得\tA {g(w_(LA), 3)}\tB {g(w_(LB), 3)}')
    print('\n'.join(out))


if __name__ == '__main__':
    main()
