# ★(436) 計器の限界：清算の係数 ClearingIdleThrottledScan＝1（(434) 以後の既定）の生では種類 1 に律速の州の起こした行が混ざる＝欠けた割合の見積りは過大に出る（throttledRowsCount を除いて読む）。
"""(403) 清算の見る範囲の絞り（旗 ClearingIdleSearchQuarters・ClearingPartnerScope）の読み手（読むだけ）。事前登録 measurements/cs_20261018_prereg.txt の [5] 記録の線 R4・R5・R9・R10（追記3 ②④）。
使い方：PYTHONUTF8=1 python -X utf8 -I scripts/instruments/cs/an_cs403.py A=<出力>/ins B=<出力>/ins N1=<出力>/ins N2=<出力>/ins [B1=… B2=…] [sec=csc,port]
読む表＝csc_s<種>.tsv（InstrumentsScope.cs・全ての国・年ごと）と port_s<種>.tsv（POB_INSTRUMENTS_NATIONS に挙げた国だけ・四半期ごと）。標準ライブラリだけ。採否には使わない（記録の線）。
出すもの：
 csc ① 行の種類ごとの走査した行・成約した行の割合・成約の数量・単価の比（払う単価÷最良の着値・払う単価÷自州価格・売り手の受取÷自州価格）・覚えが尽きて探し直した行の割合（R5）
     ② 価格の効果 R10：旗 0 の月の行（種類 2＝前の月に輸入があった行）と B の覚えで絞った行（種類 3）の単価の比の差・国別（役 B＝輸入国・役 S＝輸出国）の単価の比の A／B／ヌル腕
     ③ 先送りの見込み R4：B の番でなくて飛ばした行数×（A の種類 1 の成約した行÷走査した行）＝旗 0 なら成約していた行の見込み
 port R9：輸送の容量の取り分の偏り（②の帰還）＝港州の tradePotentialWeight の最大÷平均の国別の中央値・tradeThrottleIndex・portCapacityEnd の和の A／B／ヌル腕
⚠限界（自己申告）：①単価の比は財の構成の違いを含む（国×年の和＝数量加重）②種類 4 の行数は 1a の門で飛ばした行を含まない③ port は挙げた国だけ。④腕の差が揺れの幅（ヌル腕 N1・N2）の内かは読む人が見る（この読み手は判定しない）。"""
import csv, glob, os, re, statistics, sys, collections

csv.field_size_limit(1 << 30)
FIN = lambda x: x == x and abs(x) != float('inf')
med = lambda xs: statistics.median(xs) if xs else float('nan')


def num(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def seeds_of(d, stem):
    out = []
    for f in glob.glob(os.path.join(d, stem + '_s*.tsv')):
        m = re.search(r'_s(\d+)\.tsv$', f)
        if m:
            out.append(int(m.group(1)))
    return sorted(out)


def rows_of(d, stem):
    for sd in seeds_of(d, stem):
        with open(os.path.join(d, f'{stem}_s{sd}.tsv'), 'rb') as fh:
            rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t')
            h = next(rd)
            for r in rd:
                if len(r) == len(h):
                    yield sd, dict(zip(h, r))


ERAS = [('1783-1829', 1783, 1830), ('1830-1913', 1830, 1914), ('1914-1950', 1914, 1951)]
CLS = {0: '最初・対象外', 1: '最初・眠らせる対象', 2: '月・輸入があった行', 3: '月・覚えで絞った行', 4: '番でなくて飛ばした行', 5: '月・不足で起きた行'}
COLS = ['rowsCount', 'dealtRowsCount', 'dealsCount', 'fallbackRowsCount', 'qtyPerYear', 'qtyTimesUnitPrice', 'qtyTimesBestDelivered', 'qtyTimesLocalPrice', 'qtyTimesSellerShipPrice', 'qtyAfterFallbackPerYear']


def load_csc(d):
    """(role, cls, era or 'all') -> 合計の辞書 ／ 国別 (nation, role, cls) -> 合計"""
    tot = collections.defaultdict(lambda: collections.defaultdict(float))
    nat = collections.defaultdict(lambda: collections.defaultdict(float))
    seen = set()
    for sd, r in rows_of(d, 'csc'):
        seen.add(sd)
        y = int(num(r['year'])); role = int(num(r['roleCode'])); cls = int(num(r['classCode']))
        for en, a, b in ERAS:
            if a <= y < b:
                for c in COLS: tot[(role, cls, en)][c] += num(r[c])
        for c in COLS:
            tot[(role, cls, 'all')][c] += num(r[c])
            nat[(r['nation'], role, cls)][c] += num(r[c])
    return tot, nat, len(seen)


def ratio(a, b):
    return a / b if b else float('nan')


def fm(x, p=3):
    return f'{x:.{p}f}' if FIN(x) else '-'


def sec_csc(ARMS, ORDER):
    data = {k: load_csc(ARMS[k]) for k in ORDER}
    print('=== csc ① 行の種類ごと（役 B＝買い手の国の側・全期・種の合計）===')
    print('  腕\t種類\t種数\t走査した行\t成約した行の割合\t成約の件数\t数量\t単価÷最良の着値\t単価÷自州価格\t売り手の受取÷自州価格\t探し直した行の割合\t探し直しの後の数量の割合')
    for k in ORDER:
        tot, _, ns = data[k]
        for cls in (0, 1, 2, 3, 4, 5):
            t = tot.get((0, cls, 'all'))
            if not t or not t['rowsCount']: continue
            print(f"  {k}\t{cls} {CLS[cls]}\t{ns}\t{t['rowsCount']:.0f}\t{fm(ratio(t['dealtRowsCount'], t['rowsCount']), 4)}\t{t['dealsCount']:.0f}\t{t['qtyPerYear']:.6g}\t"
                  f"{fm(ratio(t['qtyTimesUnitPrice'], t['qtyTimesBestDelivered']), 4)}\t{fm(ratio(t['qtyTimesUnitPrice'], t['qtyTimesLocalPrice']), 4)}\t{fm(ratio(t['qtyTimesSellerShipPrice'], t['qtyTimesLocalPrice']), 4)}\t"
                  f"{fm(ratio(t['fallbackRowsCount'], t['rowsCount']), 4)}\t{fm(ratio(t['qtyAfterFallbackPerYear'], t['qtyPerYear']), 4)}")
    print()
    print('=== csc ② 価格の効果 R10（旗 0 の月の行〔種類 2〕と、B の覚えで絞った行〔種類 3〕・時代別・数量加重）===')
    print('  腕\t時代\t種類\t単価÷最良の着値\t単価÷自州価格\t売り手の受取÷自州価格\t数量')
    for k in ORDER:
        tot, _, _ = data[k]
        for en, _, _ in ERAS:
            for cls in (2, 3):
                t = tot.get((0, cls, en))
                if not t or not t['qtyPerYear']: continue
                print(f"  {k}\t{en}\t{cls}\t{fm(ratio(t['qtyTimesUnitPrice'], t['qtyTimesBestDelivered']), 4)}\t{fm(ratio(t['qtyTimesUnitPrice'], t['qtyTimesLocalPrice']), 4)}\t{fm(ratio(t['qtyTimesSellerShipPrice'], t['qtyTimesLocalPrice']), 4)}\t{t['qtyPerYear']:.6g}")
    print()
    print('=== csc ② 国別の単価の比（A の数量が大きい上位 14 国・全成約・数量加重・括弧は数量）===')
    A = data[ORDER[0]][1]

    def nat_sum(nat, n, role):
        s = collections.defaultdict(float)
        for cls in (0, 1, 2, 3, 5):
            t = nat.get((n, role, cls))
            if t:
                for c in ('qtyPerYear', 'qtyTimesUnitPrice', 'qtyTimesLocalPrice', 'qtyTimesSellerShipPrice'): s[c] += t[c]
        return s

    for role, nm in ((0, '輸入国（払う単価÷自州価格）'), (1, '輸出国（売り手の受取÷自州価格）')):
        names = sorted({n for (n, r, c) in A if r == role}, key=lambda n: -sum(A[(n, role, c)]['qtyPerYear'] for c in range(6) if (n, role, c) in A))[:14]
        print(f'  [{nm}]\t' + '\t'.join(ORDER))
        for n in names:
            vals = []
            for k in ORDER:
                s = nat_sum(data[k][1], n, role)
                vals.append(fm(ratio(s['qtyTimesUnitPrice'] if role == 0 else s['qtyTimesSellerShipPrice'], s['qtyTimesLocalPrice']), 4) + f"({s['qtyPerYear']:.3g})")
            print(f'  {n}\t' + '\t'.join(vals))
    print()
    print('=== csc ③ 先送りの見込み R4（B の番でなくて飛ばした行×A の眠らせる対象の行の成約率）===')
    for k in ORDER:
        if k == ORDER[0]: continue
        tA = data[ORDER[0]][0]; tB = data[k][0]
        a1 = tA.get((0, 1, 'all')); b4 = tB.get((0, 4, 'all'))
        if not a1 or not b4:
            print(f'  {k}\t飛ばした行が無い'); continue
        rate = ratio(a1['dealtRowsCount'], a1['rowsCount']); qrow = ratio(a1['qtyPerYear'], a1['dealtRowsCount'])
        skipped = b4['rowsCount']; post = skipped * rate
        allA = sum(tA[(0, c, 'all')]['dealtRowsCount'] for c in range(6) if (0, c, 'all') in tA)
        allAq = sum(tA[(0, c, 'all')]['qtyPerYear'] for c in range(6) if (0, c, 'all') in tA)
        print(f"  {k}\t飛ばした行 {skipped:.0f}・A の種類 1 の成約率 {rate:.4f}・成約した行あたりの数量 {qrow:.4g}・先送りした成約の見込み {post:.0f} 行（A の成約した行の全体 {allA:.0f} の {100*post/allA if allA else float('nan'):.2f}%）・数量 {post*qrow:.6g}（A の数量の全体 {allAq:.6g} の {100*post*qrow/allAq if allAq else float('nan'):.2f}%）")
    print()


def sec_port(ARMS, ORDER):
    print('=== port R9 輸送の容量の取り分の偏り（挙げた国の港州・四半期 40 以後）===')
    res = {}
    for k in ORDER:
        by = collections.defaultdict(list)
        for sd, r in rows_of(ARMS[k], 'port'):
            by[(sd, r['nation'], int(num(r['quarter'])))].append((num(r['tradePotentialWeight']), num(r['tradeThrottleIndex']), num(r['portCapacityEnd']), num(r['portThroughput'])))
        res[k] = by
    nations = sorted({n for by in res.values() for (_, n, _) in by})
    print('  国\t指標\t' + '\t'.join(ORDER))
    for n in nations:
        for label, f in (('取り分の最大÷平均（中央値）', 'share'), ('潜在量の和（中央値）', 'pot'), ('律速率の平均（中央値）', 'thr'), ('港の容量の和（中央値）', 'cap')):
            vals = []
            for k in ORDER:
                xs = []
                for (sd, nn, q), lst in res[k].items():
                    if nn != n or q < 40: continue
                    pots = [p for p, _, _, _ in lst if FIN(p)]
                    if f == 'share':
                        m = sum(pots) / len(pots) if pots else 0
                        if m > 0: xs.append(max(pots) / m)
                    elif f == 'pot': xs.append(sum(pots))
                    elif f == 'thr':
                        t = [x for _, x, _, _ in lst if FIN(x)]
                        if t: xs.append(sum(t) / len(t))
                    else: xs.append(sum(c for _, _, c, _ in lst if FIN(c)))
                vals.append(fm(med(xs), 4) if xs else '-')
            print(f'  {n}\t{label}\t' + '\t'.join(vals))
    print()


def main():
    args = [a for a in sys.argv[1:] if '=' in a]
    ARMS = {}; secs = ['csc', 'port']
    for a in args:
        k, v = a.split('=', 1)
        if k == 'sec': secs = v.split(',')
        else: ARMS[k] = v
    ORDER = [k for k in ('A', 'B', 'N1', 'N2', 'B1', 'B2', 'B3') if k in ARMS]
    print('腕：' + '・'.join(f'{k}={ARMS[k]}' for k in ORDER))
    if 'csc' in secs: sec_csc(ARMS, ORDER)
    if 'port' in secs: sec_port(ARMS, ORDER)


if __name__ == '__main__':
    main()
