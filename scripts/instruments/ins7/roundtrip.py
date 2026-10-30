"""(697) 線 ins7 の読み手＝往復の総量（urxrt_s）と州の生産の列（region_s の末尾 6 列）。読むだけ。
使い方：python roundtrip.py <計器の出力 ins フォルダ> [--prod [種 国 州]]  → 標準出力へ表。
  往復の表：国×十年×路（PopDecision・Meritocracy・Territory ほか）の都市への入り・都市からの出（千人・種の平均）。往復の総量＝入＋出・純＝入−出・総量÷|純|（純が小さい区間は大きくなる）。
  --prod：州の生産の列の使い方の 1 例＝全ての国の全ての州のうち、人口がピークから最も落ちた州（ピーク 1000 千人以上）の十年ごとの人口・工場の産出・採取の産出・操業中の数。
⚠限界は SimCore/InstrumentsUrx.cs（urxrt）と InstrumentsRegionProd.cs の頭に書いてある（挙げた国の POP だけ・助走は数えない・断面は四半期の末の月）。"""
import csv, glob, os, re, sys
from collections import defaultdict

def read(path):
    with open(path, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t')
        h = next(rd)
        for r in rd:
            yield dict(zip(h, r))

def seeds(D, kind):
    out = []
    for f in glob.glob(os.path.join(D, f'{kind}_s*.tsv')):
        m = re.match(rf'{kind}_s(\d+)\.tsv$', os.path.basename(f))
        if m: out.append(int(m.group(1)))
    return sorted(out)

def roundtrip(D):
    sd = seeds(D, 'urxrt')
    acc = defaultdict(lambda: [0.0, 0.0, 0.0])   # (nation, decade, route) -> in, out, calls（全種の和）
    for s in sd:
        for r in read(os.path.join(D, f'urxrt_s{s}.tsv')):
            dec = int(r['year']) // 10 * 10
            a = acc[(r['nation'], dec, r['route'])]
            a[0] += float(r['urbanInK']); a[1] += float(r['urbanOutK']); a[2] += float(r['calls'])
    n = max(len(sd), 1)
    print(f'# 往復の表 urxrt_s ＝ {len(sd)} 種（種={sd}）・単位＝千人/十年（種の平均）・総量＝入＋出・純＝入−出・比＝総量÷|純|')
    print('nation\tdecade\troute\tinK\toutK\tgrossK\tnetK\tgross/|net|')
    for k in sorted(acc, key=lambda k: (k[0], k[1], k[2])):
        i, o, c = acc[k]
        i /= n; o /= n
        net = i - o
        print(f'{k[0]}\t{k[1]}\t{k[2]}\t{i:.1f}\t{o:.1f}\t{i+o:.1f}\t{net:.1f}\t' + (f'{(i+o)/abs(net):.1f}' if abs(net) > 1e-9 else 'inf'))
    # 国×十年の合計（全ての路）
    tot = defaultdict(lambda: [0.0, 0.0])
    for (nat, dec, route), (i, o, c) in acc.items():
        tot[(nat, dec)][0] += i / n; tot[(nat, dec)][1] += o / n
    print('\n# 国×十年の合計（全ての路・種の平均）')
    print('nation\tdecade\tinK\toutK\tgrossK\tnetK\tgross/|net|')
    for k in sorted(tot):
        i, o = tot[k]; net = i - o
        print(f'{k[0]}\t{k[1]}\t{i:.1f}\t{o:.1f}\t{i+o:.1f}\t{net:.1f}\t' + (f'{(i+o)/abs(net):.1f}' if abs(net) > 1e-9 else 'inf'))
    # 路ごとの全期間の総量
    rt = defaultdict(lambda: [0.0, 0.0])
    for (nat, dec, route), (i, o, c) in acc.items():
        rt[route][0] += i / n; rt[route][1] += o / n
    print('\n# 路ごとの全期間・全ての挙げた国の合計（千人/試合）')
    for route, (i, o) in sorted(rt.items()):
        print(f'{route}\tin={i:.0f}\tout={o:.0f}\tgross={i+o:.0f}\tnet={i-o:.0f}')

def prod(D, want=None, s=None):
    sd = seeds(D, 'region')
    s = s or sd[0]
    peak = {}; rows = defaultdict(list)
    for r in read(os.path.join(D, f'region_s{s}.tsv')):
        key = (r['nation'], r['region']); p = float(r['populationK'])
        peak[key] = max(peak.get(key, 0), p); rows[key].append(r)
    best = None
    if want:
        best = (float(rows[want][-1]['populationK']) / peak[want], want)
    for key, rs in ([] if want else rows.items()):
        if peak[key] < 1000: continue
        last = float(rs[-1]['populationK']); ratio = last / peak[key]
        if best is None or ratio < best[0]: best = (ratio, key)
    ratio, key = best
    what = '指定の州' if want else '人口がピークから最も落ちた州（ピーク 1000 千人以上）'
    print(f'\n# 州の生産の列の例（種 {s}）：{what}＝{key[0]}・{key[1]}・最後/ピーク={ratio:.3f}（ピーク {peak[key]:.0f} 千人）')
    print('year\tpopulationK\tfactoryOutRef/月\textractionOutRef/月\tfactoryActive\tfactoryBankrupt\textractionActive\textractionBankrupt')
    for r in rows[key]:
        if int(r['quarter']) % 20 != 3: continue
        print('\t'.join([r['year'], f"{float(r['populationK']):.0f}", f"{float(r['factoryOutputRefValuePerMonth']):.1f}", f"{float(r['extractionOutputRefValuePerMonth']):.1f}",
                         r['factoryActiveCount'], r['factoryBankruptCount'], r['extractionActiveCount'], r['extractionBankruptCount']]))

if __name__ == '__main__':
    D = sys.argv[1]
    args = sys.argv[2:]
    if '--prod' not in args or '--only-prod' not in args: roundtrip(D)
    if '--prod' in args:
        prod(D)
        # --prod の後ろに「種 国 州」を渡すと、その州の例（hol の型＝植民地格の州の例：--prod 4 イギリス ナイン）
        i = args.index('--prod')
        if len(args) >= i + 4: prod(D, (args[i+2], args[i+3]), int(args[i+1]))
