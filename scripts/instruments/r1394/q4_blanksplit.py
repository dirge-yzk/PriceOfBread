# (1394) rv：内戦の白紙の分断（blankSplitFlag=1）の後、親の側に本国格（core・integrated）の州が残ったかを数える（読むだけ）
# 使い方: python q4_blanksplit.py <名札> <RAW>
import sys, os, glob, re
tag, raw = sys.argv[1], sys.argv[2]
def fl(x):
    try: return float(x)
    except: return float('nan')
n_split = n_cap = n_nohome = 0; ex = []
for p in sorted(glob.glob(os.path.join(raw, 'cwend_s*.tsv'))):
    s = int(re.search(r'_s(\d+)\.tsv', p).group(1))
    nat = {}
    with open(os.path.join(raw, f'nation_s{s}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            v = dict(zip(h, ln.rstrip('\n').split('\t'))); nat[(int(v['quarter']), v['nation'])] = v
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            v = dict(zip(h, ln.rstrip('\n').split('\t')))
            if v['blankSplitFlag'] != '1': continue
            n_split += 1; cap = v['capitalRisingFlag'] == '1'; n_cap += cap
            q = int(v['quarter']); par = v['parentName']; row = nat.get((q + 1, par)) or nat.get((q, par))
            home = (fl(row['coreRegions']) + fl(row['integratedRegions'])) if row else float('nan')
            if not (home > 0):
                n_nohome += 1; ex.append((s, v['year'], par, v['rebelName'], cap, row['colonialRegions'] if row else '-', row['totalPopulationK'] if row else '-'))
print(f'{tag}\t白紙の分断 {n_split}（首都の蜂起 {n_cap}）\t親に本国格の州が残らない {n_nohome}')
for e in ex: print('   種 %s %s 親 %s 反乱 %s 首都の蜂起 %s 親の植民地格 %s 親の人口 %s' % e)
