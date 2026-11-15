# 読むだけ (1169)：S01-V02〈必要性の項が立つ〉＝未解禁（knownFlag 0）の国×行で、必要性 needTerm が 0 のまま 50 年以上（engineer_s は 8 四半期ごと＝連続 25 標本以上）続いた物を数える。
#  技術力 techTerm が門の 70（Discovery.TermMax）に届いたまま必要性 0 の物も別に数える。計器が書かない四半期は数えない（標本の間隔が 8 四半期を超えたら連続を切る）。
# 使い方: python -X utf8 -I s01_v02.py <生のフォルダ> <種の数>
import sys, os, csv, collections
B, NS = sys.argv[1], int(sys.argv[2])
pairs = 0; hit = 0; hit70 = 0; rows = collections.Counter(); rows70 = collections.Counter(); miss = 0; seeds_row = collections.defaultdict(set)
for s in range(1, NS + 1):
    p = os.path.join(B, f'engineer_s{s}.tsv')
    if not os.path.exists(p): miss += 1; continue
    ser = collections.defaultdict(list)
    for r in csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'):
        if r['kind'] != 'T': continue
        ser[(r['nation'], r['row'])].append((int(r['quarter']), float(r['needTerm']), float(r['techTerm']), r['knownFlag'] if r['replaced'] != 'nan' else 'nan'))
    for (n, row), xs in ser.items():
        xs.sort(); pairs += 1
        run = run70 = 0; best = best70 = 0; lastq = None
        for q, nd, te, kn in xs:
            if lastq is not None and q - lastq > 8: run = run70 = 0
            lastq = q
            if kn != '0': run = run70 = 0; continue   # 解禁済みか、必要性を計算していない（replaced が nan＝前提の段で飛ばした・新兵器）標本は数えない＝連続を切る
            if nd == 0.0:
                run += 1; run70 = run70 + 1 if te >= 69.999 else 0
            else: run = run70 = 0
            best = max(best, run); best70 = max(best70, run70)
        if best >= 25: hit += 1; rows[row] += 1; seeds_row[row].add(s)
        if best70 >= 25: hit70 += 1; rows70[row] += 1
print(f'種 {NS - miss}/{NS}・国×行 {pairs}・必要性 0 が 50 年以上 {hit}（行 {len(rows)}）・うち技術力が門の 70 のまま {hit70}（行 {len(rows70)}）')
print('行ごと（国×行の数・種の数）：' + ' '.join(f'{k}:{v}/{len(seeds_row[k])}種' for k, v in rows.most_common()))
print('技術力 70 のまま：' + ' '.join(f'{k}:{v}' for k, v in rows70.most_common()))
