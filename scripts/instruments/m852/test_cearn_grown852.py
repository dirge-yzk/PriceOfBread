# (852) 合流：ident_check.py の cearn_same_but_location に足した allow_grown（cearn_s の列の追加と金を動かすファイルの行の移動が同じ木に入る形）の試し。
#   python -I -X utf8 test_cearn_grown852.py <リポジトリ> <基準の cearn_s の表> <作業フォルダ> <基準のコミット> <測る木のコミット>
#   基準の表の頭 4000 行から、測る木で行が動いた経路を含む小さな表を作り、腕の表を〈写像で写した経路＋末尾に 2 列〉で作る。
#   ⚠写像そのもの（difflib）は旧からの部品＝腕の経路を同じ写像で作るので、ここで確かめるのは〈増えた列の扱い〉と〈壊した形が落ちること〉だけ（写像の正しさの試しではない）。
import sys, os, csv, importlib.util
repo, basef, work, c0, c1 = sys.argv[1:6]
spec = importlib.util.spec_from_file_location('ic', os.path.join(repo, 'scripts', 'instruments', 'ident', 'ident_check.py'))
ic = importlib.util.module_from_spec(spec); spec.loader.exec_module(ic)
ic.CEARN.update(root=repo, base=c0, arm_commit=c1, tree=None)
os.makedirs(work, exist_ok=True)
with open(basef, encoding='utf-8', newline='') as f:
    rows = [r for _, r in zip(range(4001), csv.reader(f, delimiter='\t'))]
h = rows[0]; rc = [i for i, c in enumerate(h) if c in ic.CEARN_ROUTE_COLS]
body = rows[1:]
moved_rows = [r for r in body if any(ic._cearn_remap(r[i]) != r[i] and not ic._cearn_remap(r[i]).endswith('?') for i in rc)]
plain = [r for r in body if all(ic._cearn_remap(r[i]) == r[i] for i in rc)]
B = plain[:200] + moved_rows[:200]
def W(name, hdr, rs):
    p = os.path.join(work, name)
    with open(p, 'w', encoding='utf-8', newline='') as f:
        w = csv.writer(f, delimiter='\t', lineterminator='\n'); w.writerow(hdr); w.writerows(rs)
    return p
def remap(r): return [ic._cearn_remap(v) if i in rc else v for i, v in enumerate(r)]
pa = W('base.tsv', h, B)
arm = [remap(r) + ['1', '2'] for r in B]
res = []
def case(name, pb, allow, want):
    got = ic.cearn_same_but_location(pa, pb, allow_grown=allow)
    okv = (got[0] == want)
    res.append((name, okv, got[0], got[1], len(got[2])))
case('T1 列が増えた＋経路を写す（宣言あり）＝一致', W('t1.tsv', h + ['x1', 'x2'], arm), True, True)
case('T2 同じ表で宣言なし＝不一致', W('t2.tsv', h + ['x1', 'x2'], arm), False, False)
a3 = [r[:] for r in arm]; j = next(i for i in range(len(h)) if i not in rc and h[i] not in ('seed',)); a3[5][j] = a3[5][j] + '9'
case('T3 経路の外の列を 1 欄壊す＝不一致', W('t3.tsv', h + ['x1', 'x2'], a3), True, False)
case('T4 腕の行を 1 行落とす＝不一致', W('t4.tsv', h + ['x1', 'x2'], arm[:-1]), True, False)
h5 = h[:]; h5[3] = h5[3] + 'z'
case('T5 基準の列の見出しを変える＝不一致', W('t5.tsv', h5 + ['x1', 'x2'], arm), True, False)
case('T6 列は増えない・経路だけ写す（旧の形）＝一致', W('t6.tsv', h, [remap(r) for r in B]), False, True)
a7 = [r[:] for r in arm]; k = next(i for i, r in enumerate(B) if r in moved_rows[:200]); i7 = next(i for i in rc if ic._cearn_remap(B[k][i]) != B[k][i]); a7[k][i7] = B[k][i7]
case('T7 動いた経路を写さずに基準の字のまま＝不一致', W('t7.tsv', h + ['x1', 'x2'], a7), True, False)
a8 = [r[:] for r in arm]; a8[3] = a8[3][:-1]
case('T8 腕の 1 行の列が足りない＝不一致', W('t8.tsv', h + ['x1', 'x2'], a8), True, False)
print(f'基準の表の行 {len(B)}（動かない経路 {min(200, len(plain))}・測る木で行が動いた経路を含む {min(200, len(moved_rows))}）・基準 {c0}・測る木 {c1}')
for r in res:
    print(('合' if r[1] else '否') + f'\t{r[0]}\t戻り値 一致={r[2]} 写した欄={r[3]} 増えた列={r[4]}')
print('判定：' + ('全て予言どおり' if all(r[1] for r in res) else '予言と違う物がある'))
