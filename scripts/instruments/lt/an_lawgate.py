# (423) 線 lt の読み手：lawgate_s（法の門の旧と新の表）と reform_s／metal_s から、事前登録 measurements/lt_20261019_prereg.txt [6] の T1〜T4 と [5] の R1〜R2 を読む。
# 使い方：python -I scripts/instruments/lt/an_lawgate.py <計器の出力フォルダ（…/ins）> [線=0.75]
#   ・nGoldLaw の年ごとの平均（metal_s・T2〜T4）
#   ・識字＞線を一度でも超えた国の数の見積もり（reform_s・全国・年末・兵士を除く全州の識字＝本国のみの lawgate_s の literacyHome より下限）
#   ・lawgate_s（旗の段・願いの旧と新・目標の番号）：国ごとの願いの最初の年（挙げた国は四半期・他は年末）
# 読むだけ。力学を再計算しない。NaN は nan のまま。文字化けを避けるため標準出力を UTF-8 にする。
import csv, collections, glob, os, re, statistics as st, sys
sys.stdout.reconfigure(encoding='utf-8')

def rows(path):
    with open(path, encoding='utf-8', newline='') as f:
        yield from csv.DictReader(f, delimiter='\t')

def seeds(d, kind):
    return sorted(glob.glob(os.path.join(d, f'{kind}_s*.tsv')), key=lambda p: int(re.search(r'_s(\d+)\.tsv$', p).group(1)))

def main():
    d = sys.argv[1]
    line = float(sys.argv[2]) if len(sys.argv) > 2 else 0.75
    years = (1793, 1803, 1823, 1833, 1843, 1853, 1873, 1893, 1913, 1933)
    # 1) nGoldLaw（metal_s・10 種×4 四半期の年平均）
    by = collections.defaultdict(list)
    for p in seeds(d, 'metal'):
        for r in rows(p): by[int(r['year'])].append(float(r['nGoldLaw']))
    print('nGoldLaw（年平均）：', '  '.join(f'{y}={st.mean(by[y]):.1f}' for y in years if by[y]))
    # 2) 識字＞線を一度でも超えた国の数（reform_s）
    ever = {}
    for p in seeds(d, 'reform'):
        s = int(re.search(r'_s(\d+)\.tsv$', p).group(1)); e = {}
        for r in rows(p):
            if int(r['quarter']) % 4 == 3 and float(r['literacyShare']) > line: e.setdefault(r['nation'], int(r['year']))
        ever[s] = e
    if ever:
        print(f'識字＞{line} を一度でも超えた国の数（reform_s・見積もりの下限側）：',
              '  '.join(f'{y}={st.mean(sum(1 for v in e.values() if v <= y) for e in ever.values()):.1f}' for y in years))
    # 3) lawgate_s：国ごとの最初の年（種の中央値）
    first = collections.defaultdict(lambda: collections.defaultdict(list))
    for p in seeds(d, 'lawgate'):
        per = collections.defaultdict(dict)
        for r in rows(p):
            n = r['nation']; y = int(r['year'])
            for col, ok in (('goldWishOld', r['goldWishOld'] == '1'), ('goldWishNew', r['goldWishNew'] == '1'), ('goldLawGold', r['goldLawCode'] == '2'),
                            ('antiOld', r['antiTargetOld'] != '-1'), ('antiNew', r['antiTargetNew'] != '-1'), ('antiLaw', r['antitrustLawCode'] != '0')):
                if ok: per[n].setdefault(col, y)
        for n, dd in per.items():
            for col, y in dd.items(): first[n][col].append(y)
    if first:
        cols = ('goldWishOld', 'goldWishNew', 'goldLawGold', 'antiOld', 'antiNew', 'antiLaw')
        print('国ごとの最初の年（種の中央値／何種で起きたか）：国\t' + '\t'.join(cols))
        for n in sorted(first):
            print(n + '\t' + '\t'.join((f'{st.median(first[n][c]):.0f}/{len(first[n][c])}' if first[n].get(c) else '-') for c in cols))

main()
