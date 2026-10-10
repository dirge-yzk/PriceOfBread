"""(242) 標準の計器 fiscal_s<seed>.tsv の読み手（読むだけ）：請求の項目と国庫の台帳の区分の対応（請求と実際の支払い）。
使い方：python -X utf8 an_fiscal242.py <計器の出力ディレクトリ> <国名> [窓の開始年-終了年 ...]
  例：python -X utf8 an_fiscal242.py ins_on10 イギリス 1783-1814 1815-1849 1850-1880 1881-1913 1914-1950
出力：窓ごとに、台帳の区分ごとの Σ請求・Σ r×請求・Σ実際の支払い（−台帳の月の差分）・実際÷(r×請求)・実際÷請求、
      全体（利払いと減債を除く／含む）の Σ実際÷Σ請求。種の和（月の数で割らない）。(216) の「請求は実際の支払いの 1.6〜2.8 倍」の数え直し。
⚠対応表は MAP（コードの読みで作った＝measurements/ins242_20261010 の §2 に出典の行）。MAP に無い区分の支払いは「請求の外」に出す。
"""
import sys, os, glob, csv, collections

# 請求の項目（列名）→ 実際に払われる台帳の区分（TreasuryCat）。出典は measurements/ins242_20261010 §2。
MAP = {
    'claimInterestPerMonth': 'Interest', 'claimWelfarePerMonth': 'Welfare', 'claimPensionPerMonth': 'Pension',
    'claimColonialUpkeepPerMonth': 'ColonialUpkeep', 'claimTributeOutAtQuarterEnd': 'TributeOut', 'claimRecruitAtQuarterEnd': 'Modernization',
    'claimMegaForeignYieldAtQuarterEnd': 'Megaproject', 'claimTradePolicyPerMonth': 'Subsidy', 'claimPortAtQuarterEnd': 'OtherOut',
    'claimArmyPerMonth': 'Army', 'claimSoldierPayPerMonth': 'Army', 'claimNavyPerMonth': 'Navy',
    'claimAdminPerMonth': 'Admin', 'claimInfraPerMonth': 'Admin', 'claimElecInfraPerMonth': 'Admin',
    'claimStateBuildPerMonth': 'Modernization', 'claimHigherEduPerMonth': 'Modernization', 'claimMonumentBuildPerMonth': 'Megaproject',
    'claimLastMonthEducationPerMonth': 'Education', 'claimLastMonthWelfareSubsidyPerMonth': 'Welfare',
    'claimLastMonthFactoryBuildPerMonth': 'FactoryBuild', 'claimLastMonthSubsidyPerMonth': 'Subsidy',
    'claimStateConstructionPerMonth': 'FactoryBuild', 'claimStateLossPerMonth': 'StateFactory',
}
OUTFLOW = ['Army', 'Navy', 'Admin', 'Education', 'Welfare', 'Pension', 'Modernization', 'AntiCorruption', 'Interest', 'FactoryBuild', 'Subsidy',
           'Megaproject', 'ColonialUpkeep', 'TributeOut', 'WarCost', 'OtherOut']   # DebtRepay は請求の外（r＝1 の余剰から）・StateFactory は損益で符号が両側
def f(x):
    try: return float(x)
    except Exception: return float('nan')

def load(d, nation):
    out = []
    for p in sorted(glob.glob(os.path.join(d, 'fiscal_s*.tsv'))):
        with open(p, encoding='utf-8') as fh:
            rd = csv.reader(fh, delimiter='\t'); h = next(rd)
            for r in rd:
                if len(r) == len(h) and r[h.index('nation')] == nation:
                    out.append((p, dict(zip(h, r))))
    return out

def main():
    d, nation = sys.argv[1], sys.argv[2]
    wins = [tuple(int(v) for v in w.split('-')) for w in sys.argv[3:]] or [(1783, 1950)]
    rows = load(d, nation)
    seeds = len({p for p, _ in rows})
    print(f'# {nation}・{seeds}種・月の行 {len(rows)}・窓＝暦年（含む）')
    for (a, b) in wins:
        claim = collections.defaultdict(float); rclaim = collections.defaultdict(float); paid = collections.defaultdict(float); n = 0
        unmapped = collections.defaultdict(float)
        for _, r in rows:
            y = int(f(r['year']))
            if not (a <= y <= b): continue
            n += 1; rate = f(r['payRateShare'])
            for col, cat in MAP.items():
                v = f(r[col])
                if v == v: claim[cat] += v; rclaim[cat] += rate * v
            for cat in OUTFLOW + ['StateFactory', 'DebtRepay']:
                v = f(r['ledger' + cat + 'PerMonth'])
                if v == v: paid[cat] += -v
        print(f'\n## {a}-{b}（月の行 {n}）  単位＝自国通貨の名目の和（種をまたいだ和）')
        print(f'{"区分":<16}{"Σ請求":>14}{"Σ r×請求":>14}{"Σ実際":>14}{"実際/(r×請求)":>14}{"実際/請求":>12}')
        tc = tr = tp = 0.0; tc2 = tr2 = tp2 = 0.0
        for cat in OUTFLOW + ['StateFactory']:
            c, rc, p = claim.get(cat, 0.0), rclaim.get(cat, 0.0), paid.get(cat, 0.0)
            if c == 0 and abs(p) < 1e-9: continue
            note = '' if c > 0 else '（請求の外）'
            print(f'{cat:<16}{c:14.1f}{rc:14.1f}{p:14.1f}{(p / rc if rc > 0 else float("nan")):14.3f}{(p / c if c > 0 else float("nan")):12.3f} {note}')
            if cat != 'StateFactory':
                tc += c; tr += rc; tp += p
                if cat != 'Interest': tc2 += c; tr2 += rc; tp2 += p
        print(f'{"合計（請求の外を含む）":<14}{tc:14.1f}{tr:14.1f}{tp:14.1f}{(tp / tr if tr > 0 else float("nan")):14.3f}{(tp / tc if tc > 0 else float("nan")):12.3f}')
        print(f'{"合計（利払いを除く）":<14}{tc2:14.1f}{tr2:14.1f}{tp2:14.1f}{(tp2 / tr2 if tr2 > 0 else float("nan")):14.3f}{(tp2 / tc2 if tc2 > 0 else float("nan")):12.3f}')
        in_ = {c: sum(f(r['ledger' + c + 'PerMonth']) for _, r in rows if a <= int(f(r['year'])) <= b) for c in ['Tax', 'Tariff', 'StateMonopoly', 'StateFactory', 'TributeIn', 'OtherIn']}
        rev = sum(in_.values())
        tc3 = tr3 = tp3 = 0.0
        for cat in OUTFLOW:
            if cat in ('Interest', 'Admin'): continue
            tc3 += claim.get(cat, 0.0); tr3 += rclaim.get(cat, 0.0); tp3 += paid.get(cat, 0.0)
        print(f'{"合計（利払いと行政を除く）":<12}{tc3:14.1f}{tr3:14.1f}{tp3:14.1f}{(tp3 / tr3 if tr3 > 0 else float("nan")):14.3f}{(tp3 / tc3 if tc3 > 0 else float("nan")):12.3f}')
        print(f'歳入の台帳（Tax+Tariff+専売+国営工場+貢納入+その他入）Σ={rev:.1f}・Σ請求/Σ歳入={tc / rev if rev else float("nan"):.3f}・Σ実際/Σ歳入={tp / rev if rev else float("nan"):.3f}')

if __name__ == '__main__': main()
