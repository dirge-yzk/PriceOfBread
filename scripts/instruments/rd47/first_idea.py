"""(698) rd47 ①：思想と製法の行が世界で最初に開いた種ごとの国・四半期と、点数の 3 項・必要性の強度の分解（読むだけ）。
使い方：python -X utf8 -I first_idea.py <腕名>=<計器の出力フォルダ（…/ins）>[:<種の起点>] ...（種は起点から 10 本）
出力（標準出力・TSV）：
  F 行＝腕・種・行の名前・最初に開いた四半期と年・国・技術・必要性・伝わり（disc_s の E 行の値）
  D 行＝思想の行で、初開の国が計器の 9 国（idn_s に行がある国）のとき、その四半期の idn_s の強度の分解：
       workerMil（工場労働者の人数×闘争度÷人口）・wageShare（賃金だけで暮らす人の人口比）・popMil（全員の人数×闘争度÷人口）・
       class_s から引いた工場労働者の割合 wShare（兵士を除く人口に対する工場労働者の人数）と、その平均闘争度 mW＝workerMil÷wShare・
       ファシズムは crisis・decline・needFas。
  T 行＝計器の 9 国ごとに、開幕から最初に〈社会主義の必要性の強度 workerMil≧0.15（＝必要性の項 30＝技術 70 と合わせて門 100）〉に達した四半期。
⚠限界の申告：
  ・disc_s は quarter 0 始まり・idn_s と class_s は quarter 1 始まり（idn_s の quarter q は disc_s の q−1 の四半期末の判定と同じ盤面＝計器が判定の後に書く）。結合は disc_s の q を idn_s の q+1 で引く。
  ・wShare の分母は class_s の全階級の和−兵士（Discovery.Score の popSum と同じ規則＝Size>0 の兵士でない POP）。class_s の時刻の札は q<n>（idn_s の quarter と同じ番号）。
  ・9 国以外（小国・建国の国）が初開の国だと D 行は出ない（idn_s は挙げた国だけ）。
"""
import csv, os, sys, collections
csv.field_size_limit(10**9)
ROWS = ['社会主義', '共産主義', 'ファシズム', 'ルブラン法', 'ガス灯']
IDEAS = {'社会主義', '共産主義', 'ファシズム'}
def rows(p):
    if not os.path.exists(p): return
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def fl(v):
    try: return float(v)
    except: return float('nan')
print('kind\tarm\tseed\trow\tquarter\tyear\tnation\ttech\tneed\ttrans\tworkerMil\twageShare\tpopMil\twShare\tmW\tcrisis\tdecline\tneedFas')
for a in sys.argv[1:]:
    name, rest = a.split('=', 1)
    d, s0 = (rest.rsplit(':', 1) if ':' in rest[2:] else (rest, '1'))
    s0 = int(s0)
    for s in range(s0, s0 + 10):
        first = {}
        for r in rows(os.path.join(d, f'disc_s{s}.tsv')):
            if r['kind'] != 'E' or r['row'] not in ROWS: continue
            q = int(fl(r['quarter']))
            if r['row'] not in first or q < first[r['row']][0]:
                first[r['row']] = (q, r)
        need_q = {}  # (nation, idn quarter) needed
        for k, (q, r) in first.items():
            if k in IDEAS: need_q[(r['nation'], q + 1)] = True
        idn = {}; tcross = {}
        for r in rows(os.path.join(d, f'idn_s{s}.tsv')):
            q = int(fl(r['quarter'])); n = r['nation']
            if (n, q) in need_q: idn[(n, q)] = r
            if n not in tcross and fl(r['workerMil']) >= 0.15: tcross[n] = (q, r['year'])
        cls = collections.defaultdict(lambda: [0.0, 0.0])
        keys = {f'q{q}': n for (n, q) in need_q}
        for r in rows(os.path.join(d, f'class_s{s}.tsv')):
            t = r['time']
            if t not in keys: continue
            n = r['nation']
            if (n, int(t[1:])) not in need_q: continue
            sz = fl(r['sizeK']); c = r['className']
            if c == '兵士' or not (sz > 0): continue
            cls[(n, int(t[1:]))][0] += sz
            if c == '工場労働者': cls[(n, int(t[1:]))][1] += sz
        for k in ROWS:
            if k not in first: print(f'F\t{name}\t{s}\t{k}\t-\t-\t-'); continue
            q, r = first[k]
            line = f"F\t{name}\t{s}\t{k}\t{q}\t{r['year']}\t{r['nation']}\t{fl(r['techTerm']):.1f}\t{fl(r['needTerm']):.1f}\t{fl(r['transmitTerm']):.1f}"
            print(line)
            if k in IDEAS and (r['nation'], q + 1) in idn:
                x = idn[(r['nation'], q + 1)]
                tot, w = cls.get((r['nation'], q + 1), (0.0, 0.0))
                ws = w / tot if tot > 0 else float('nan'); wm = fl(x['workerMil'])
                mw = wm / ws if ws > 0 else float('nan')
                print(f"D\t{name}\t{s}\t{k}\t{q}\t{r['year']}\t{r['nation']}\t\t\t\t{wm:.4f}\t{fl(x['wageShare']):.4f}\t{fl(x['popMil']):.4f}\t{ws:.4f}\t{mw:.4f}\t{fl(x['crisis']):.4f}\t{fl(x['decline']):.4f}\t{fl(x['needFas']):.2f}")
        for n, (q, y) in sorted(tcross.items(), key=lambda t: t[1][0]):
            print(f'T\t{name}\t{s}\tworkerMil>=0.15\t{q}\t{y}\t{n}')
