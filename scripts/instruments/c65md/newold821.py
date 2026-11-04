# (821) c65md 記録：同じ盤面の新÷旧（腕 B の cpmed_s の反実仮想と実際）と、腕をまたぐ崩壊の講和の賠償の Σ（記録・門ではない＝ヌル腕どうしの幅と並べる）。
#   使い方: python -I -X utf8 newold821.py <腕 B の ins> <旗 0 の盤面の cpmed_s・cpeace_s のフォルダ> <基準の ins> <ヌル腕 1 の ins> <ヌル腕 2 の ins>
import sys, os, glob, csv
B, Z, M, N1, N2 = sys.argv[1:6]
def F(x):
    try: return float(x)
    except Exception: return float('nan')
def rows(d, head):
    out = []
    for f in sorted(glob.glob(os.path.join(d, head + '*.tsv'))):
        with open(f, encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                r['seed'] = os.path.basename(f)[len(head):-4]; out.append(r)
    return out
def med(d):
    rs = [r for r in rows(d, 'cpmed_s') if r['civilFlag'] == '0']
    fc = [r for r in rs if r['forcedFlag'] == '1' and r['collapsedIterFlag'] == '1']
    return len(rs), sum(1 for r in rs if r['forcedFlag'] == '1'), len(fc), sum(1 for r in fc if F(r['repWouldOldIndex']) > 0), sum(F(r['repWouldOldIndex']) for r in fc), sum(F(r['repMovedIndex']) for r in rs if r['forcedFlag'] == '1'), sum(1 for r in rs if r['forcedFlag'] == '1' and r['hitFlag'] == '1')
print('# 同じ盤面（cpmed_s）：外交の講和・調停の戦争・うち崩壊・うち旧の規則で払う（反実仮想＞0）・Σ反実仮想・Σ実際の賠償（調停の戦争）・賠償の枝まで進んだ調停の戦争')
for name, d in (('腕B', B), ('旗0の盤面', Z)):
    n, nf, nc, nw, w, rep, hit = med(d)
    print(f'{name}\t講和 {n}\t調停 {nf}\t崩壊 {nc}\t旧なら払う {nw}\tΣ反実仮想 {w:.6g}\tΣ実際 {rep:.6g}\t枝 {hit}\t新÷旧（Σ実際÷Σ反実仮想）{(rep / w if w > 0 else float("nan")):.4f}')
print('# 腕をまたぐ崩壊の講和の Σ 賠償（cpeace_s・記録＝門にしない・ヌル腕どうしの幅と並べる）：repMovedIndex（無い木は repRealIndex）')
for name, d in (('腕B', B), ('旗0の盤面', Z), ('基準 keep_818', M), ('N1 keep_797', N1), ('N2 keep_797', N2)):
    cp = rows(d, 'cpeace_s')
    if not cp: print(f'{name}\t表なし'); continue
    col = 'repMovedIndex' if 'repMovedIndex' in cp[0] else 'repRealIndex'
    print(f'{name}\t事象 {len(cp)}\tΣ {col} {sum(F(e[col]) for e in cp):.6g}\tΣ repRealIndex {sum(F(e["repRealIndex"]) for e in cp):.6g}')
