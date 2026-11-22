# (1299) 問い1の道の分類（region_s だけで読む＝cederule_s の無い a11 でも同じ物差し）。
# 区間の始まり q0 について、直前の首都 C（q0-1 の最初の行）が q0 にどこにいるかで分ける。
#   born        : 国の初出の四半期から首都が植民地格（分離で生まれた国）
#   cap_seceded : C が q0 に初出の国の州＝首都の蜂起・分離（Seceded）→ 親の NextCapital
#   cap_ceded   : C が q0 に既にあった別の国の州＝割譲（Ceded）→ NextCapital
#   reorder     : C が同じ国に残る＝首都の付け替え（SetCapital）だけ
#   prev_colonial: 直前の首都も植民地格（首都の州が植民地格の別の州へ変わっただけ）
# 主権の四半期の内訳：rebel_pending＝分離で生まれた国の最後の四半期（次の四半期に国が無い）・sovereign＝それ以外の内戦の月 0 の四半期
# 使い方: python -X utf8 -I q1c_path.py <生のフォルダ> <q1 の rows.tsv> <出力>
import sys, csv, os, collections

raw, rowsp, out = sys.argv[1], sys.argv[2], sys.argv[3]

def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: yield ix, row

hits = collections.defaultdict(list)
for ix, row in rd(rowsp): hits[int(row[ix['seed']])].append({k: row[i] for k, i in ix.items()})
res = []
tot = collections.Counter()
for s, hs in sorted(hits.items()):
    first = {}; capOf = {}; holder = {}; present = set()
    for ix, row in rd(os.path.join(raw, f'region_s{s}.tsv')):
        q = int(row[ix['quarter']]); n = row[ix['nation']]
        if n not in first: first[n] = q
        if (q, n) not in capOf: capOf[(q, n)] = (row[ix['region']], row[ix['tier']])
        holder[(q, row[ix['region']])] = n
        present.add((q, n))
    hs.sort(key=lambda h: (h['nation'], int(h['quarter'])))
    prevq = {}
    for h in hs:
        n = h['nation']; q = int(h['quarter'])
        start = prevq.get(n) != q - 1
        prevq[n] = q
        born = first[n] > 0
        last = (q + 1, n) not in present
        state = h['state']
        if state == 'sov' and born and last: state = 'rebel_pending'
        if start:
            if first[n] == q: path = 'born'
            else:
                pc = capOf.get((q - 1, n))
                if pc is None: path = 'unknown'
                elif pc[1] == 'Colonial': path = 'prev_colonial'
                else:
                    hn = holder.get((q, pc[0]))
                    if hn == n: path = 'reorder'
                    elif hn is None: path = 'gone'
                    elif first.get(hn) == q: path = 'cap_seceded'
                    else: path = 'cap_ceded'
            curPath = path
        cls = ('born_' if born else 'orig_') + state
        tot[(curPath, cls)] += 1
        res.append((s, n, q, h['year'], h['capital'], curPath, cls, h['homeRegions']))
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    f.write('seed\tnation\tquarter\tyear\tcapital\tpath\tclass\thomeRegions\n')
    for r in res: f.write('\t'.join(str(x) for x in r) + '\n')
    f.write('\n# 道×区分の国×四半期\n')
    for (p, c), v in sorted(tot.items()): f.write(f'# {p}\t{c}\t{v}\n')
for (p, c), v in sorted(tot.items()): print(p, c, v)
