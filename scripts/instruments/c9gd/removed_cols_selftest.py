#!/usr/bin/env python3
# (786) ident_check.py の --removed-cols の自己点検（突然変異）：基準の silverpar_s1.tsv から列 fiscalBAt3Count を消した腕を作り、
#   ①宣言あり＝恒等 ②宣言なし＝不一致 ③宣言ありでも 1 欄の値を変えたら不一致 ④宣言ありでも残りの列の順序を替えたら不一致、を確かめる。
# 使い方：python -I removed_cols_selftest.py <基準の計器の生のフォルダ（silverpar_s1.tsv がある）> <作業フォルダ>
import sys, os, csv, importlib.util, io, contextlib
base, work = sys.argv[1], sys.argv[2]
rows = list(csv.reader(open(os.path.join(base, 'silverpar_s1.tsv'), encoding='utf-8', newline=''), delimiter='\t'))
k = rows[0].index('fiscalBAt3Count')
def w(d, rs):
    os.makedirs(os.path.join(work, d), exist_ok=True)
    with open(os.path.join(work, d, 'silverpar_s1.tsv'), 'w', encoding='utf-8', newline='') as f:
        csv.writer(f, delimiter='\t', lineterminator='\n').writerows(rs)
dr = lambda r: [x for i, x in enumerate(r) if i != k]
w('base', rows); w('arm1', [dr(r) for r in rows])
r2 = [dr(r) for r in rows]; r2[5][3] = '9'; w('arm2', r2)
r3 = [dr(r) for r in rows]; c = r3[0].index('eMean'); r3[0][c], r3[0][c + 1] = r3[0][c + 1], r3[0][c]; w('arm3', r3)
sys.argv = ['x', 't']
spec = importlib.util.spec_from_file_location('ic', os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'ident', 'ident_check.py'))
ic = importlib.util.module_from_spec(spec); spec.loader.exec_module(ic)
decl = ('silverpar_s:fiscalBAt3Count',)
def run(arm, d):
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        ic.COLS_REMOVED.clear(); ic.cmp_ins(os.path.join(work, arm), os.path.join(work, 'base'), {}, [], [], [], (), d)
    return buf.getvalue()
out = [('宣言あり=恒等', '計器の表は恒等' in run('arm1', decl)), ('宣言なし=不一致', '宣言の無い違い' in run('arm1', ())),
       ('値の変更=不一致', '宣言の無い違い' in run('arm2', decl)), ('列順の変更=不一致', '宣言の無い違い' in run('arm3', decl))]
for n, okk in out: print(('OK ' if okk else 'NG ') + n)
sys.exit(0 if all(o for _, o in out) else 1)
