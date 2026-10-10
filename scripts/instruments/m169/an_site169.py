"""(169) 産地の発見の年（読むだけ）。
  python an_site169.py ev <d165 か site の出力の dir> [ラベル]    … 開いた年（d165 の S 行 か site169 の F 行）＝種の中央値〔開いた種の数〕と史実の年との差
  python an_site169.py cf <site の出力の dir> <N0,N0,...> [ラベル] … 見つける力の半分点 N0 を変えたときに開く年（U 行の軌道から・その産地が開く前の軌道だけを使う＝一次の近似）
開かなかった種は1950（全長の終わり）として中央値に入れる（(166) an_site.py と同じ読み）。置かれていない産地（どの種にも行が無い＝コムストック）は平均から外す。"""
import sys, os, glob, statistics as st, collections
HIST = {'カリフォルニアの金': 1848, 'ヴィクトリアの金': 1851, 'ラントの金': 1886, 'クロンダイクの金': 1896, 'コムストックの銀': 1859,
        'ルイジアナの硫黄': 1894, '九州の硫黄': 1885, 'ジャワのキナ': 1854, 'ニルギリのキナ': 1860, 'セイロンのキナ': 1865}
GOLD = ['カリフォルニアの金', 'ヴィクトリアの金', 'ラントの金', 'クロンダイクの金']
mode, D = sys.argv[1], sys.argv[2]

def seeds_of(D):
    return sorted(int(os.path.basename(f)[1:-4]) for f in glob.glob(os.path.join(D, 't*.txt')))

def report(yrs, seeds, placed, lab):
    print(f'######## {lab}（種 {len(seeds)}・産地の発見の年＝種の中央値〔開いた種の数〕・開かない種は1950）')
    diffs, gd = [], []
    for site, h in HIST.items():
        if site not in placed: print(f'  {site}: 置かれていない'); continue
        ys = [yrs[site][sd][0] if sd in yrs[site] else 1950 for sd in seeds]
        n = sum(1 for sd in seeds if sd in yrs[site])
        m = st.median(ys)
        who = collections.Counter(yrs[site][sd][1] for sd in seeds if sd in yrs[site]).most_common(2)
        diffs.append(abs(m - h))
        if site in GOLD: gd.append(abs(m - h))
        print(f'  {site}: {m:.0f}〔{n}/{len(seeds)}〕 史実 {h}（{m - h:+.0f}）  種ごと {sorted(ys)}  {who}')
    print(f'  |差|の平均 {sum(diffs) / len(diffs):.1f}年（{len(diffs)}行）・金の4行 {sum(gd) / max(1, len(gd)):.1f}年')

if mode == 'ev':
    LAB = sys.argv[3] if len(sys.argv) > 3 else D
    seeds = seeds_of(D); yrs = collections.defaultdict(dict); placed = set()
    for sd in seeds:
        for ln in open(os.path.join(D, f't{sd}.txt'), encoding='utf-8'):
            if ln.startswith('S\t') or ln.startswith('F\t'):
                f = ln.rstrip('\n').split('\t'); yr, nat, site = int(f[2]), f[3], f[5]
                yrs[site].setdefault(sd, (yr, nat)); placed.add(site)
            elif ln.startswith('U\t'):
                placed.add(ln.split('\t')[5])
    # d165 の S 行は開いた産地だけ＝置かれていても開かない産地は「置かれていない」と区別できない＝場所の表で置かれている行を足す（銀は SilverRushEnabled＝0 で置かない）
    placed |= {s for s in HIST if s != 'コムストックの銀'}
    report(yrs, seeds, placed, LAB)
elif mode == 'cf':
    N0s = [float(x) for x in sys.argv[3].split(',')]
    LAB = sys.argv[4] if len(sys.argv) > 4 else D
    seeds = seeds_of(D)
    traj = collections.defaultdict(lambda: collections.defaultdict(list))   # site → seed → [(q, yr, nat, ratio, people, need)]
    found = collections.defaultdict(dict)
    for sd in seeds:
        for ln in open(os.path.join(D, f't{sd}.txt'), encoding='utf-8'):
            f = ln.rstrip('\n').split('\t')
            if f[0] == 'U':
                q, yr, nat, site = int(f[1]), int(f[2]), f[3], f[5]
                tech, diff, people, need = float(f[6]), float(f[7]), float(f[8]), float(f[12])
                ratio = 1.0 if not diff > 0 else min(1.0, max(0.0, tech / diff))
                traj[site][sd].append((q, yr, nat, ratio, people, need))
            elif f[0] == 'F':
                found[f[5]].setdefault(sd, int(f[2]))
    placed = set(traj) | set(found)
    for n0 in N0s:
        yrs = collections.defaultdict(dict)
        for site in traj:
            for sd, rows in traj[site].items():
                for (q, yr, nat, ratio, people, need) in rows:
                    thick = people / (people + n0) if people > 0 else 0.0
                    if 70.0 * ratio * thick + need >= 100.0:
                        yrs[site][sd] = (yr, nat); break
        report(yrs, seeds, placed, f'{LAB} 反実仮想 N0={n0:g}')
    # 軌道の要約（金の4行）＝種1〜の 10年ごとの 技術水準÷難易度・人の数・必要性
    print('######## 軌道（種ごと・10年ごと・年末の四半期）＝ratio / 人 / 必要性 / 国')
    for site in GOLD + ['ルイジアナの硫黄', '九州の硫黄', 'ジャワのキナ', 'ニルギリのキナ', 'セイロンのキナ']:
        if site not in traj: continue
        print(f'  {site}')
        for sd in sorted(traj[site]):
            out = []
            for (q, yr, nat, ratio, people, need) in traj[site][sd]:
                if q % 4 == 0 and yr % 10 == 0:
                    out.append(f'{yr}:{ratio:.2f}/{people:.3g}/{need:.0f}/{nat[:3]}')
            print(f'    種{sd}: ' + '  '.join(out))
