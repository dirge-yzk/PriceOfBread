"""(266) 線A′の事前登録の線を、腕A と腕B の an_a266.py の .tsv に当てる（読むだけ）。
  python -X utf8 cmp_a266.py <腕A.tsv> <腕B.tsv> [--banda <腕A の帯の報告>] [--bandb <腕B の帯の報告>]
  線は実装ログ (266) §4 の事前登録のとおり（数字を読む前に書いた・ここで動かさない）。倍率で書いてあるので 5種（種1〜5）も 10種も同じ式で当てる
  （絶対数の線 1358・7,434,698・4,946,573・629・152・218・73932 は 10種で A の数に倍率を掛けたものと一致する）。
  兌換停止（帯の報告の先頭の行〔総N回〕）は帯の報告を渡したときだけ読む。"""
import sys, re, math

P_SHORT, P_BORROW = 0.269, 0.497   # (265) の静的な反事実（事前登録の §3）


def tsv(p):
    d = {}
    for line in open(p, encoding='utf-8'):
        k, v = line.rstrip('\n').split('\t')
        d[k] = float(v)
    return d


A, B = tsv(sys.argv[1]), tsv(sys.argv[2])
band = {}
for i, a in enumerate(sys.argv):
    if a in ('--banda', '--bandb'): band[a] = sys.argv[i + 1]


def susp(path):
    for line in open(path, encoding='utf-8'):
        m = re.search(r'兌換停止\）: 総(\d+)回', line)
        if m: return float(m.group(1))
    return math.nan


rows = []   # (番号, 名前, 腕A, 腕B, 線, 判定)
STOP = []


def add(no, name, a, b, rule, ok, stop=False):
    rows.append((no, name, a, b, rule, 'OK' if ok else ('止める' if stop else '不合格')))
    if stop and not ok: STOP.append(no)


r = lambda k: B[k] / A[k] if A[k] else math.nan
add('1', '英の払えない月', A['uk_short'], B['uk_short'], f'≦ 基線×{1 - 0.5 * P_SHORT:.4f}', B['uk_short'] <= A['uk_short'] * (1 - 0.5 * P_SHORT))
add('2a', '英の借入の和', A['uk_borrow'], B['uk_borrow'], f'≦ 基線×{1 - 0.5 * P_BORROW:.4f}', B['uk_borrow'] <= A['uk_borrow'] * (1 - 0.5 * P_BORROW))
add('2b', '英の借入の和（下限）', A['uk_borrow'], B['uk_borrow'], '≧ 基線×0.50', B['uk_borrow'] >= A['uk_borrow'] * 0.50)
add('3', '英の率＜1 の月（nation_s）', A['uk_rbelow1'], B['uk_rbelow1'], '≦ 基線×0.90', B['uk_rbelow1'] <= A['uk_rbelow1'] * 0.90)
add('6', '英の借入の和 ＜0.50倍', A['uk_borrow'], B['uk_borrow'], '≧ 基線×0.50（未満で止める）', B['uk_borrow'] >= A['uk_borrow'] * 0.50, True)
add('7a', 'r＜1 を含む四半期の充足の中央（英）', A['fill_r1_median_uk'], B['fill_r1_median_uk'], '≧ 基線−0.10', B['fill_r1_median_uk'] >= A['fill_r1_median_uk'] - 0.10, True)
add('7b', 'r＜1 を含む四半期の充足の中央（全ての国）', A['fill_r1_median_all'], B['fill_r1_median_all'], '≧ 基線−0.10', B['fill_r1_median_all'] >= A['fill_r1_median_all'] - 0.10, True)
add('7c', 'r＜0.7 の国×四半期（全ての国）', A['rate_below_07_all'], B['rate_below_07_all'], '≦ 基線×2', B['rate_below_07_all'] <= A['rate_below_07_all'] * 2, True)
add('8', '英の歳入基数÷年GDP（r＝1）', A['uk_rev_gdp_r1_median'], B['uk_rev_gdp_r1_median'], '≧ 基線×0.85', B['uk_rev_gdp_r1_median'] >= A['uk_rev_gdp_r1_median'] * 0.85, True)
if '--banda' in band and '--bandb' in band:
    sa, sb = susp(band['--banda']), susp(band['--bandb'])
    add('9a', '兌換停止（帯の先頭）', sa, sb, '≦ 基線×1.2', sb <= sa * 1.2, True)
add('9b', '英の不履行', A['uk_default_count'], B['uk_default_count'], '≦ 基線＋2', B['uk_default_count'] <= A['uk_default_count'] + 2, True)
add('10a', '開戦の回数（全ての国）', A['war_starts_all'], B['war_starts_all'], '≦ 基線×1.2', B['war_starts_all'] <= A['war_starts_all'] * 1.2, True)
add('10b', '交戦の国×四半期（全ての国）', A['war_quarters_all'], B['war_quarters_all'], '≦ 基線×1.2', B['war_quarters_all'] <= A['war_quarters_all'] * 1.2, True)
add('10c', '開戦の回数（英）', A['war_starts_uk'], B['war_starts_uk'], '≦ 基線×1.2', B['war_starts_uk'] <= A['war_starts_uk'] * 1.2, True)
add('10d', '交戦の国×四半期（英）', A['war_quarters_uk'], B['war_quarters_uk'], '≦ 基線×1.2', B['war_quarters_uk'] <= A['war_quarters_uk'] * 1.2, True)
print('| 線 | 量 | 腕A | 腕B | 倍率 | 規則 | 判定 |')
print('|---|---|---|---|---|---|---|')
for no, name, a, b, rule, v in rows:
    ratio = (b / a) if a else math.nan
    print(f'| {no} | {name} | {a:.6g} | {b:.6g} | {ratio:.3f} | {rule} | {v} |')
print()
print('止める線に当たった：' + (','.join(STOP) if STOP else 'なし'))
print('合格の線で不合格：' + (','.join(n for n, *_x, v in rows if v == '不合格') or 'なし'))
print()
print('記録（A→B）：')
for k in ('uk_claim_admin', 'uk_claim_army', 'uk_claim_navy', 'uk_claim_soldier', 'uk_claim_welfare', 'uk_claim_pension', 'uk_claim_infra', 'uk_claim_elec', 'uk_claim_port', 'uk_claim_interest', 'uk_milshare_mean',
          'uk_admin_fill_month_median', 'uk_treasury_gdp_median', 'uk_convertibility_1821_1913', 'uk_debt_gdp_1890_1909', 'uk_debt_gdp_1910_1950', 'civilwar_quarters_all', 'rate_below_07_uk', 'all_short', 'all_borrow', 'all_rbelow1',
          'uk_short_Q1-40', 'uk_short_Q41-160', 'uk_short_Q161-', 'uk_borrow_Q1-40', 'uk_borrow_Q41-160', 'uk_borrow_Q161-', 'uk_rbelow1_Q1-40', 'uk_rbelow1_Q41-160', 'uk_rbelow1_Q161-', 'fill_pair_median_uk', 'fill_pair_median_all'):
    if k in B: print(f'  {k}: {A.get(k, math.nan):.6g} → {B[k]:.6g}' + (f'（{B[k] / A[k]:.3f}倍）' if A.get(k) else ''))
