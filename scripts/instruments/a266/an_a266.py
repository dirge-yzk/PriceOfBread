"""(266) 線A′（0g-A′＝行政の請求を1本に・必要額で切る）の読み手（読むだけ）。腕A・腕B に同じスクリプトを当てて表を比べる。
  python -X utf8 an_a266.py <計器の出力ディレクトリ(ins)> <出力の名前(.md .tsv .keys を同じ場所に書く)> [自国=イギリス] [--pair <腕A の .keys>] [--seeds N]
  入力＝標準の計器（`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins）の
        nation_s（全ての国×四半期）と fiscal_s（挙げた国の月ごと）。⚠nation_s が全ての国か確かめる（「全国」はその国の数で決まる）。
  出力＝<出力の名前>.md（人が読む表）・.tsv（量の名前＋値＝cmp_a266.py が腕A と腕B を読む）・.keys（r＜1 を含む国×種×四半期の集合＝対の記録の入力）。
  数え方は先に決めた（数字を読む前・実装ログ (266) §1）：
    払えない月＝nation_s の shortfallMonths の和／借入の和＝fiscal_s の borrowedPerMonth の和／率＜1 の月＝nation_s の payRateBelowOneMonths（double の厳密な <1）の和
    （fiscal_s の G10 の印字で数えた 556 は使わない＝(265) の指摘。1−1e-9 未満の数え方は計器の列が無いので立てない）
    r＜1 を含む四半期の行政の充足＝nation_s で adminRBelow1MonthsCount＞0 の四半期の adminFillMeanShare の中央
    r＜0.7 の国×四半期＝nation_s の payRateMeanShare＜0.7（四半期の平均の率）
    歳入基数÷年GDP（英・r＝1 の四半期の中央）＝revenueBaseMeanPerMonth×12÷nominalGdpPerYear（adminRBelow1MonthsCount＝0 の四半期）
    交戦の国×四半期＝atWarMonths＞0 の国×四半期／開戦の回数＝国×種で atWarMonths が 0 から正へ変わった回数（最初の四半期が正なら1回）
    兌換停止＝帯の報告の先頭の行〔総N回〕（この読み手は読まない）／英の不履行＝fiscal_s の defaultCount の種ごとの最後の値の和
  計器が自分で申告する限界：英の月の行は挙げた国だけ（fiscal_s）。nation_s の月数は四半期3か月（最初の四半期は2か月）。
    r＜1 の四半期の選び方は腕で変わる（A′ で r＜1 が減れば残る四半期は重い方に偏る）＝対の記録（腕A の r＜1 の四半期と同じ国×種×四半期の集合）を腕B で別に出す（--pair）。"""
import sys, os, csv, glob, re, statistics, math, json

D, OUT = sys.argv[1], sys.argv[2]
UK = 'イギリス'
args = []
PAIR = None
MAXSEED = 10**9
i = 3
while i < len(sys.argv):
    if sys.argv[i] == '--pair':
        PAIR = sys.argv[i + 1]; i += 2
    elif sys.argv[i] == '--seeds':   # 種 1..N だけ読む（5種の腕を腕A の種1〜5 と比べる）
        MAXSEED = int(sys.argv[i + 1]); i += 2
    else:
        args.append(sys.argv[i]); i += 1
if args: UK = args[0]
NAN = float('nan')


def fl(x):
    try: return float(x)
    except Exception: return NAN


def med(v):
    v = [a for a in v if a == a]
    return statistics.median(v) if v else NAN


def seeds_of(kind):
    out = []
    for f in glob.glob(os.path.join(D, f'{kind}_s*.tsv')):
        m = re.match(rf'{kind}_s(\d+)\.tsv$', os.path.basename(f))
        if m and int(m.group(1)) <= MAXSEED: out.append(int(m.group(1)))
    return sorted(out)


NCOLS = ['quarter', 'year', 'nation', 'nominalGdpPerYear', 'atWarMonths', 'civilWarMonths', 'adminRBelow1MonthsCount', 'adminFillMeanShare', 'payRateMeanShare',
         'revenueBaseMeanPerMonth', 'govDebtStock', 'shortfallMonths', 'payRateBelowOneMonths', 'borrowedMeanPerMonth', 'adminMonthsCount', 'adminClaimSimPerMonth', 'adminClaimFiscalPerMonth']
FCOLS = ['monthIndex', 'nation', 'year', 'claimInterestPerMonth', 'claimWelfarePerMonth', 'claimPensionPerMonth', 'claimArmyPerMonth', 'claimNavyPerMonth', 'claimAdminPerMonth', 'claimInfraPerMonth',
         'claimElecInfraPerMonth', 'claimSoldierPayPerMonth', 'claimPortAtQuarterEnd', 'adminFillShare', 'borrowedPerMonth', 'convertibilityShare', 'defaultCount', 'treasuryEndStock', 'claimTotalPerMonth']


def load(kind, cols, want=None):
    res = {}
    for sd in seeds_of(kind):
        rows = []
        with open(os.path.join(D, f'{kind}_s{sd}.tsv'), encoding='utf-8') as fh:
            head = fh.readline().rstrip('\n').split('\t')
            idx = {c: head.index(c) for c in cols if c in head}
            for line in fh:
                t = line.rstrip('\n').split('\t')
                row = {c: (t[i] if c == 'nation' else fl(t[i])) for c, i in idx.items()}
                if want is None or want(row): rows.append(row)
        res[sd] = rows
    return res


nat = load('nation', NCOLS)
fis = load('fiscal', FCOLS, lambda r: r['nation'] == UK)
M = {}      # 量の名前 -> 値（tsv へ）
lines = []  # md


def out(s=''): lines.append(s)


nations = set(r['nation'] for rs in nat.values() for r in rs)
M['nation_count'] = len(nations)
M['seeds'] = len(nat)
out(f'# (266) 線A′の読み手：{os.path.basename(OUT)}（入力 {D}）')
out(f'- 種 {len(nat)}・nation_s の国 {len(nations)}（全ての国なら 61 前後）・英＝{UK}')
out()


def qwin(r):
    q = int(r['quarter'])
    return 'Q1-40' if q < 40 else ('Q41-160' if q < 160 else 'Q161-')


def nsum(sel, col):
    return sum(r[col] for rs in nat.values() for r in rs if sel(r) and r[col] == r[col])


isuk = lambda r: r['nation'] == UK
allr = lambda r: True
# ---- 英：払えない月・借入・率＜1 の月（全期・窓別）
out('## 英（全期・窓別・10種の和）')
out('| 窓 | 払えない月 | 借入の和 | 率＜1 の月 |')
out('|---|---|---|---|')
borrow_by_win = {}
for rs in fis.values():
    for r in rs:
        q = int(r['monthIndex']) // 3
        w = 'Q1-40' if q < 40 else ('Q41-160' if q < 160 else 'Q161-')
        borrow_by_win[w] = borrow_by_win.get(w, 0.0) + (r['borrowedPerMonth'] if r['borrowedPerMonth'] == r['borrowedPerMonth'] else 0.0)
for w in ('Q1-40', 'Q41-160', 'Q161-'):
    sf = nsum(lambda r, w=w: isuk(r) and qwin(r) == w, 'shortfallMonths'); rb = nsum(lambda r, w=w: isuk(r) and qwin(r) == w, 'payRateBelowOneMonths')
    out(f'| {w} | {sf:.0f} | {borrow_by_win.get(w, 0.0):.0f} | {rb:.0f} |')
    M[f'uk_short_{w}'] = sf; M[f'uk_borrow_{w}'] = borrow_by_win.get(w, 0.0); M[f'uk_rbelow1_{w}'] = rb
M['uk_short'] = nsum(isuk, 'shortfallMonths'); M['uk_borrow'] = sum(borrow_by_win.values()); M['uk_rbelow1'] = nsum(isuk, 'payRateBelowOneMonths')
out(f'| 全期 | {M["uk_short"]:.0f} | {M["uk_borrow"]:.0f} | {M["uk_rbelow1"]:.0f} |')
out()
# ---- 全ての国
M['all_short'] = nsum(allr, 'shortfallMonths'); M['all_rbelow1'] = nsum(allr, 'payRateBelowOneMonths')
M['all_borrow'] = sum(r['borrowedMeanPerMonth'] * r['adminMonthsCount'] for rs in nat.values() for r in rs if r['borrowedMeanPerMonth'] == r['borrowedMeanPerMonth'])
out(f'## 全ての国：払えない月 {M["all_short"]:.0f}・借入（四半期の月平均×月数の和）{M["all_borrow"]:.0f}・率＜1 の月 {M["all_rbelow1"]:.0f}')
out()
# ---- (7) r<1 を含む四半期の行政の充足の中央
out('## 止める線 (7)：r＜1 を含む四半期の行政の充足の中央・r＜0.7 の国×四半期')
keys_r1 = []
for lab, sel in (('uk', isuk), ('all', allr)):
    v = [r['adminFillMeanShare'] for rs in nat.values() for r in rs if sel(r) and r['adminRBelow1MonthsCount'] > 0]
    M[f'fill_r1_median_{lab}'] = med(v); M[f'fill_r1_quarters_{lab}'] = len(v)
    out(f'- {lab}：r＜1 を含む四半期 {len(v)} 件・行政の充足（四半期平均）の中央 {med(v):.4f}')
M['rate_below_07_all'] = sum(1 for rs in nat.values() for r in rs if r['payRateMeanShare'] < 0.7)
M['rate_below_07_uk'] = sum(1 for rs in nat.values() for r in rs if isuk(r) and r['payRateMeanShare'] < 0.7)
out(f'- r＜0.7 の国×四半期（payRateMeanShare＜0.7）：全ての国 {M["rate_below_07_all"]}・英 {M["rate_below_07_uk"]}')
for sd, rs in nat.items():
    for r in rs:
        if r['adminRBelow1MonthsCount'] > 0: keys_r1.append((sd, r['nation'], int(r['quarter'])))
if PAIR:
    base = set(tuple(x) for x in json.load(open(PAIR, encoding='utf-8')))
    v_uk = [r['adminFillMeanShare'] for sd, rs in nat.items() for r in rs if (sd, r['nation'], int(r['quarter'])) in base and isuk(r)]
    v_all = [r['adminFillMeanShare'] for sd, rs in nat.items() for r in rs if (sd, r['nation'], int(r['quarter'])) in base]
    M['fill_pair_median_uk'] = med(v_uk); M['fill_pair_median_all'] = med(v_all)
    out(f'- 対の記録（腕A の r＜1 の四半期と同じ国×種×四半期）：英 {len(v_uk)} 件の中央 {med(v_uk):.4f}・全ての国 {len(v_all)} 件の中央 {med(v_all):.4f}')
json.dump(keys_r1, open(OUT + '.keys', 'w', encoding='utf-8'))
out()
# ---- (8) 歳入基数÷年GDP
v = [r['revenueBaseMeanPerMonth'] * 12 / r['nominalGdpPerYear'] for rs in nat.values() for r in rs
     if isuk(r) and r['adminRBelow1MonthsCount'] == 0 and r['nominalGdpPerYear'] > 1e-9 and r['revenueBaseMeanPerMonth'] == r['revenueBaseMeanPerMonth']]
M['uk_rev_gdp_r1_median'] = med(v)
out(f'## 止める線 (8)：英の歳入基数÷年GDP（r＝1 の四半期 {len(v)} 件）の中央 {med(v):.4f}')
out()
# ---- (9)(10)
dflt = sum((rs[-1]['defaultCount'] if rs and rs[-1]['defaultCount'] == rs[-1]['defaultCount'] else 0) for rs in fis.values())
M['uk_default_count'] = dflt
war_q = 0; war_start = 0; war_q_uk = 0; war_start_uk = 0; cw_q = 0
for sd, rs in nat.items():
    prev = {}
    for r in sorted(rs, key=lambda r: (r['nation'], r['quarter'])):
        k = r['nation']; w = r['atWarMonths'] > 0
        if w:
            war_q += 1
            if not prev.get(k, False): war_start += 1
            if isuk(r):
                war_q_uk += 1
                if not prev.get(k, False): war_start_uk += 1
        if r['civilWarMonths'] > 0: cw_q += 1
        prev[k] = w
M['war_quarters_all'] = war_q; M['war_starts_all'] = war_start; M['war_quarters_uk'] = war_q_uk; M['war_starts_uk'] = war_start_uk; M['civilwar_quarters_all'] = cw_q
out('## 止める線 (9)(10)')
out(f'- 英の不履行（fiscal_s の defaultCount の種ごとの最後の値の和）：{dflt:.0f}')
out(f'- 交戦の国×四半期：全ての国 {war_q}・英 {war_q_uk}／開戦の回数：全ての国 {war_start}・英 {war_start_uk}／内戦の国×四半期（記録）{cw_q}')
out('- 兌換停止は帯の報告の先頭の行〔金本位の強制離脱（兌換停止）: 総N回〕（基線 182）')
out()
# ---- 記録
out('## 記録（線ではない）')
M['uk_debt_gdp_1890_1909'] = med([r['govDebtStock'] / r['nominalGdpPerYear'] for rs in nat.values() for r in rs if isuk(r) and 1890 <= r['year'] <= 1909 and r['nominalGdpPerYear'] > 1e-9])
M['uk_debt_gdp_1910_1950'] = med([r['govDebtStock'] / r['nominalGdpPerYear'] for rs in nat.values() for r in rs if isuk(r) and 1910 <= r['year'] <= 1950 and r['nominalGdpPerYear'] > 1e-9])
out(f'- 英の国債÷年GDP の中央：1890-1909 {M["uk_debt_gdp_1890_1909"]:.3f}・1910-1950 {M["uk_debt_gdp_1910_1950"]:.3f}')
gq = {}
for sd, rs in nat.items():
    for r in rs:
        if isuk(r): gq[(sd, int(r['quarter']))] = r['nominalGdpPerYear']


def fsum(col, f=lambda r: True):
    return sum(r[col] for rs in fis.values() for r in rs if r[col] == r[col] and f(r))


for name, col in (('uk_claim_admin', 'claimAdminPerMonth'), ('uk_claim_army', 'claimArmyPerMonth'), ('uk_claim_navy', 'claimNavyPerMonth'), ('uk_claim_soldier', 'claimSoldierPayPerMonth'),
                  ('uk_claim_welfare', 'claimWelfarePerMonth'), ('uk_claim_pension', 'claimPensionPerMonth'), ('uk_claim_infra', 'claimInfraPerMonth'), ('uk_claim_elec', 'claimElecInfraPerMonth'),
                  ('uk_claim_port', 'claimPortAtQuarterEnd'), ('uk_claim_interest', 'claimInterestPerMonth'), ('uk_claim_total', 'claimTotalPerMonth')):
    M[name] = fsum(col)
out('- 英の請求の和（fiscal_s・10種の全期・r を掛ける前）：' + '／'.join(f'{k[9:]} {M[k]:.0f}' for k in M if k.startswith('uk_claim_')))
tot = 0.0; n = 0
for rs in fis.values():
    for r in rs:
        mil = max(0, (r['claimArmyPerMonth'] + r['claimNavyPerMonth'])) + max(0, r['claimSoldierPayPerMonth']); oth = max(0, r['claimAdminPerMonth']) + max(0, r['claimInterestPerMonth'])
        if mil + oth > 1e-9: tot += min(1.0, mil / (mil + oth)); n += 1
M['uk_milshare_mean'] = tot / n if n else NAN
out(f'- 英の MilitaryBudgetShare の月平均（請求の列からの再構成＝（陸海の装備＋兵士の給与）÷（それ＋行政＋利払い））：{M["uk_milshare_mean"]:.4f}（{n} 月）')
M['uk_admin_fill_month_median'] = med([r['adminFillShare'] for rs in fis.values() for r in rs])
out(f'- 英の行政の充足（月）の中央 {M["uk_admin_fill_month_median"]:.4f}')
tg = [r['treasuryEndStock'] / gq[(sd, int(r['monthIndex']) // 3)] for sd, rs in fis.items() for r in rs if gq.get((sd, int(r['monthIndex']) // 3), 0) > 1e-9]
M['uk_treasury_gdp_median'] = med(tg)
out(f'- 英の国庫÷年GDP（月末の国庫÷その四半期の年GDP）の中央 {M["uk_treasury_gdp_median"]:.4f}')
cv = [r['convertibilityShare'] for rs in fis.values() for r in rs if 1821 <= r['year'] <= 1913]
M['uk_convertibility_1821_1913'] = (sum(cv) / len(cv)) if cv else NAN
out(f'- 英の兌換（月の平均・1821-1913）：{M["uk_convertibility_1821_1913"]:.3f}')
dif = max((abs(r['adminClaimSimPerMonth'] - r['adminClaimFiscalPerMonth']) / max(1.0, abs(r['adminClaimSimPerMonth'])) for rs in nat.values() for r in rs if r['adminClaimSimPerMonth'] == r['adminClaimSimPerMonth']), default=NAN)
M['admin_two_formula_max_rel_gap'] = dif
out(f'- 行政の請求の2つの量（Sim の州の和と LastAdminClaimPerMonth）の四半期平均の相対差の最大 {dif:.3e}（A′ 後は 0 に近い）')
open(OUT + '.md', 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
with open(OUT + '.tsv', 'w', encoding='utf-8') as fh:
    for k, v in M.items(): fh.write(f'{k}\t{v}\n')
print('\n'.join(lines))
