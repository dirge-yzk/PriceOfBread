"""(349) 必需の割合 n の動き（読むだけ）。python an_ce_n.py <ins> [国=イギリス] [財,財,...]
  ce_s の N 行の necessityRecordShare（実際に記録した n・人口加重の窓平均）を窓（40 四半期）ごと・財ごとに 10 種の中央値〔最小〜最大〕で出す。
  あわせて C 行の layerCap2PerMonth／layerBud3PerMonth／layerNecPaidPerMonth／layerLuxPaidPerMonth（英の大衆・富裕層の窓平均の中央値）＝旗 1 で層が働いている証拠。"""
import sys, os, csv, glob, statistics, collections

D = sys.argv[1]
NATION = sys.argv[2] if len(sys.argv) > 2 else 'イギリス'
SHOW = (sys.argv[3] if len(sys.argv) > 3 else '砂糖,茶,コーヒー,灯油,缶詰,ガス,自転車,ラジオ,ワイン,チョコレート,時計,蒸留酒').split(',')


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


rows = collections.defaultdict(list)   # (財, 窓) → [seed ごとの値]
lay = collections.defaultdict(list)    # (列, 階級集合, 窓) → [...]
paths = sorted(glob.glob(os.path.join(D, 'ce_s*.tsv')))
for p in paths:
    with open(p, encoding='utf-8', errors='replace', newline='') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['nation'] != NATION:
                continue
            if r['scope'] == 'N':
                v = fl(r.get('necessityRecordShare', 'nan'))
                if r['goodName'] in SHOW and v == v:
                    rows[(r['goodName'], int(r['quarter']))].append(v)
cols = ['layerCap2PerMonth', 'layerBud3PerMonth', 'layerNecPaidPerMonth', 'layerLuxPaidPerMonth', 'transferInPerMonth']
acc = collections.defaultdict(lambda: collections.defaultdict(list))   # (列, 集合) → seed → [四半期の値]
for p in paths:
    seed = int(os.path.basename(p)[len('ce_s'):-4])
    with open(p, encoding='utf-8', errors='replace', newline='') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['nation'] != NATION or r['scope'] != 'C':
                continue
            q = int(r['quarter'])
            if not 4 <= q <= 39:
                continue
            cid = int(r['classId'])
            grp = 'mass' if cid in (1, 2, 11) else 'rich' if cid in (0, 3, 10) else None
            if grp is None:
                continue
            for c in cols:
                acc[(c, grp)][seed].append(fl(r[c]))
print(f'## {NATION}：必需の割合 n（necessityRecordShare・窓 40 四半期ごと・10 種の中央値）')
wins = sorted({w for (_, w) in rows})
print('財\t' + '\t'.join(f'q{w}-{w+39}' for w in wins))
for g in SHOW:
    cells = []
    for w in wins:
        v = rows.get((g, w), [])
        cells.append(f'{statistics.median(v):.3f} [{min(v):.3f}–{max(v):.3f}]' if v else '-')
    print(g + '\t' + '\t'.join(cells))
print(f'## {NATION}：予算の層の列（Q5–Q40 の月平均・種の中央値）')
for (c, grp), d in sorted(acc.items()):
    per = [sum(v) / len(v) for v in d.values() if v]
    print(f'{c}\t{grp}\t{statistics.median(per):.4g}')
