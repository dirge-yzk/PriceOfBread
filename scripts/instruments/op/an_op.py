"""アヘンの計器 opium_s の読み手（(342) 段1・読むだけ）。python an_op.py <計器の出力ディレクトリ> [-n 国名の部分文字列] [-v]
  入力＝標準の計器（`INS=1 bash batch.sh run <腕>` の <出力>/ins）の opium_s<seed>.tsv と nation_s<seed>.tsv。
  表の形（SimCore/Instruments.cs の OpiumRows）：scope 列で R（州×四半期・中毒度か販売量が正の州だけ）と N（国×四半期・法か緊張か輸出入が有る国だけ）。
  出すもの：国ごとに（種をまたいで）
    peakAddiction＝N 行の nationalAddictionIndex（人口加重）の最大・その年／peakStateAddiction＝R 行の addictionIndex の最大
    sold＝N 行の soldTotalPerMonth の四半期の和÷種の数（販売量の月平均×3＝四半期の和）／exports＝exportsPerClearing の最大
    firstPossession＝保有の法が初めて 0 でなくなった年（種の中央値）・firstExportBan＝輸出の法が初めて 1 になった年
    tension＝tensionAddedPerQuarter の和（種あたり）・pairMonths＝積んだ組×月の数
  -v＝州の中毒度の上位（種をまたいだ最大）も出す。財の番号は読まない（表に財の列が無い）。"""
import sys, os, collections, statistics
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, seeds, num

args = sys.argv[1:]
V = '-v' in args
sub = args[args.index('-n') + 1] if '-n' in args else None
dirs = [a for i, a in enumerate(args) if not a.startswith('-') and not (i > 0 and args[i - 1] == '-n')]
D = dirs[0]
sds = seeds(D, 'opium')
if not sds:
    print('opium_s が無い（INS=1 で回した出力の ins/ を渡す）'); sys.exit(1)
nat = collections.defaultdict(lambda: collections.defaultdict(list))   # nation -> key -> per-seed list
cur = {}
peakS = collections.defaultdict(float); peakSwhere = {}
for sd, r in rows(D, 'opium'):
    n = r['nation']; yr = int(r['year'])
    if r['scope'] == 'N':
        c = cur.setdefault((sd, n), dict(peak=0.0, peakYr=None, sold=0.0, ex=0.0, im=0.0, pos=None, ban=None, tens=0.0, pm=0, tlev=0.0))
        a = num(r['nationalAddictionIndex'])
        if a > c['peak']: c['peak'] = a; c['peakYr'] = yr
        c['sold'] += 3 * num(r['soldTotalPerMonth'])
        c['ex'] = max(c['ex'], num(r['exportsPerClearing'])); c['im'] = max(c['im'], num(r['importsPerClearing']))
        if c['pos'] is None and num(r['possessionLawCode']) > 0: c['pos'] = yr
        if c['ban'] is None and num(r['exportBanFlag']) > 0: c['ban'] = yr
        c['tens'] += num(r['tensionAddedPerQuarter']); c['pm'] += int(num(r['tensionPairMonthsCount']))
        c['tlev'] = max(c['tlev'], num(r['tensionLevelMaxIndex']))
    else:
        a = num(r['addictionIndex'])
        if a > peakS[(n, r['region'])]: peakS[(n, r['region'])] = a; peakSwhere[(n, r['region'])] = (sd, yr)
names = sorted({n for (_, n) in cur})
print(f'種 {len(sds)} 本  国×種の行 {len(cur)}')
print('国\t出た種\tpeakAddiction(中央/最大)\tpeakYear(中央)\t販売の四半期和(種あたり)\texports最大\t保有の法の初年(中央)\t輸出の法の初年(中央)\ttension積み(種あたり)\t積んだ組月\tlevel最大')
for n in names:
    if sub and sub not in n: continue
    cs = [cur[(sd, n)] for sd in sds if (sd, n) in cur]
    if not cs: continue
    pk = [c['peak'] for c in cs]
    pyr = [c['peakYr'] for c in cs if c['peakYr'] is not None and c['peak'] > 0]
    pos = [c['pos'] for c in cs if c['pos'] is not None]; ban = [c['ban'] for c in cs if c['ban'] is not None]
    if not sub and max(pk) < 0.005 and not pos and not ban and sum(c['tens'] for c in cs) == 0 and max(c['ex'] for c in cs) == 0: continue
    med = lambda xs: f'{statistics.median(xs):.0f}' if xs else '-'
    print(f"{n}\t{len(cs)}\t{statistics.median(pk):.4f}/{max(pk):.4f}\t{med(pyr)}\t{sum(c['sold'] for c in cs)/len(sds):.3f}\t{max(c['ex'] for c in cs):.3f}\t"
          f"{med(pos)}({len(pos)})\t{med(ban)}({len(ban)})\t{sum(c['tens'] for c in cs)/len(sds):.2f}\t{sum(c['pm'] for c in cs)}\t{max(c['tlev'] for c in cs):.2f}")
if V:
    print('\n州の中毒度の上位（種をまたいだ最大）')
    for (n, rg), v in sorted(peakS.items(), key=lambda kv: -kv[1])[:20]:
        sd, yr = peakSwhere[(n, rg)]
        print(f'{n}\t{rg}\t{v:.4f}\tseed{sd}\t{yr}')
