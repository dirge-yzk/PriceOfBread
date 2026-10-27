"""(613) 線 tl の読み手（読むだけ）：計器つきの生の scalars_s<種>.tsv から、技術水準の削除の観測量を種ごと・国ごとに出す。
使い方：python -X utf8 -I an_tl613.py <ins_dir> [<ins_dir> ...]   （例：D:/PoB_work/keep_609/m609_ins D:/PoB_work/tl_613b/out_B_10/ins）
出す量（事前登録 measurements/tl613_20261028_prereg_rev2.txt §3）：
  [列の恒等]  externalPressure（最大・正の行数・0.3 の行数）／freightRevolution（最小・最大）／megaStartedUnmetCount（最大・正の行数）／megaStartedOldGateClosedCount（正の行数・最大）
  [記録]      国ごと（種の中央値）：教育法の最大に着いた最初の年・参政権が 1／2 に着いた最初の年・女性の権利の法が 1／3 に着いた最初の年・日本の政体が最初に変わった年・
              貿易/GDP（1914 年）・自国が所有して着工済みの巨大建造物の数（1800・1850・1900・1950 年）
片側にしか無い列（基線 m609 に外圧ほかの 6 列は無い）は「—」と書く。限界：scalars_s は NATIONS に挙げた 9 国が四半期ごと・他の国は年末の行。"""
import sys, os, glob, csv, statistics as st
sys.stdout.reconfigure(encoding='utf-8')
NAT = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']

def load(d):
    runs = {}
    for f in sorted(glob.glob(os.path.join(d, 'scalars_s*.tsv')), key=lambda p: int(os.path.basename(p)[9:-4])):
        seed = int(os.path.basename(f)[9:-4])
        with open(f, encoding='utf-8', newline='') as fh:
            rd = csv.DictReader(fh, delimiter='\t')
            runs[seed] = list(rd)
    return runs

def f(x):
    try: return float(x)
    except Exception: return float('nan')

def med(xs):
    xs = [x for x in xs if x is not None]
    return st.median(xs) if xs else None

def first_year(rows, nation, col, thr):
    for r in rows:
        if r['nation'] == nation and f(r[col]) >= thr: return int(r['year'])
    return None

def first_change(rows, nation, col):
    base = None
    for r in rows:
        if r['nation'] != nation: continue
        v = r[col]
        if base is None: base = v
        elif v != base: return int(r['year'])
    return None

def at_year(rows, nation, col, year):
    xs = [f(r[col]) for r in rows if r['nation'] == nation and int(r['year']) == year]
    return sum(xs) / len(xs) if xs else None

def show(v, nd=1):
    return '—' if v is None else (f'{v:.{nd}f}' if isinstance(v, float) and v != int(v) else str(int(v)) if isinstance(v, (int, float)) else str(v))

def report(d):
    runs = load(d)
    print(f'=== {d}  種 {len(runs)}')
    if not runs: return
    hdr = next(iter(runs.values()))[0].keys()
    has = lambda c: c in hdr
    if has('externalPressure'):
        allrows = [r for rows in runs.values() for r in rows]
        ep = [f(r['externalPressure']) for r in allrows]; fr = [f(r['freightRevolution']) for r in allrows]
        um = [f(r['megaStartedUnmetCount']) for r in allrows]; oc = [f(r['megaStartedOldGateClosedCount']) for r in allrows]
        print(f'  [列] 行 {len(allrows)}：externalPressure 最大 {max(ep):.4f}・正の行 {sum(1 for x in ep if x > 0)}・0.3 の行 {sum(1 for x in ep if abs(x-0.3) < 1e-9)}／freightRevolution {min(fr):.4f}〜{max(fr):.4f}／megaStartedUnmetCount 最大 {max(um):.0f}・正の行 {sum(1 for x in um if x > 0)}／megaStartedOldGateClosedCount 最大 {max(oc):.0f}・正の行 {sum(1 for x in oc if x > 0)}')
        by = {}
        for s, rows in runs.items():
            for r in rows:
                if f(r['megaStartedOldGateClosedCount']) > 0: by.setdefault(r['nation'], set()).add(s)
        print('  旧の門が閉じたまま着工が動いた国（種の数）：' + '・'.join(f'{n}{len(v)}' for n, v in sorted(by.items(), key=lambda kv: -len(kv[1]))[:20]))
    print('  国ごと（種の中央値）  教育法最大 | 参政権≥1 | 参政権≥2 | 女性権≥1 | 女性権≥3 | 政体の最初の変化 | 貿易/GDP1914 | 着工数1800/1850/1900/1950')
    mx = max(f(r['educationLaw']) for rows in runs.values() for r in rows)
    for n in NAT:
        cells = []
        cells.append(show(med([first_year(rows, n, 'educationLaw', mx) for rows in runs.values()])))
        cells.append(show(med([first_year(rows, n, 'suffrage', 1) for rows in runs.values()])))
        cells.append(show(med([first_year(rows, n, 'suffrage', 2) for rows in runs.values()])))
        if has('womenRightsCode'):
            cells.append(show(med([first_year(rows, n, 'womenRightsCode', 1) for rows in runs.values()])))
            cells.append(show(med([first_year(rows, n, 'womenRightsCode', 3) for rows in runs.values()])))
        else:
            cells += ['—', '—']
        cells.append(show(med([first_change(rows, n, 'polityCode') for rows in runs.values()])))
        cells.append(show(med([at_year(rows, n, 'tradeToGdpRatio', 1914) for rows in runs.values()]), 3))
        if has('megaStartedCount'):
            cells.append('/'.join(show(med([at_year(rows, n, 'megaStartedCount', y) for rows in runs.values()])) for y in (1800, 1850, 1900, 1950)))
        else:
            cells.append('—')
        print(f'  {n:<6} ' + ' | '.join(cells))
    jp = [first_change(rows, '日本', 'polityCode') for _, rows in sorted(runs.items())]
    print('  日本の政体の最初の変化の年（種ごと）：' + ' '.join(show(x) for x in jp))

for d in sys.argv[1:]:
    report(d)
