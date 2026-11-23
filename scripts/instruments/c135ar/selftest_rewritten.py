# (1303) 道具の自己試験：ident_check.py の --rewritten-tables と検算の台本 arrears_recheck.py。合成した基準と腕の小さな生で
#   組 1（規則どおりの違い）→ 台本 0・cmp_ins に不一致なし（宣言なしなら不一致）
#   組 2（規則の外の 1 行）→ 台本は非 0・cmp_ins は不一致（2a 共通の行の自由でない列・2b 基準だけの行が owes＝0）
#   組 3（宣言の外の表の 1 バイト）→ arrears_s は通っても cmp_ins は不一致
#   組 4（(1316)）規則そのものの違反の行（4a＝(f) owes＝0 で arrNewMonth が立つ・4b＝(g) cumNew が減る）→ 台本は非 0・cmp_ins は不一致
# 使い方：python -I selftest_rewritten.py   → 0＝全て期待どおり・1＝外れ。環境変数 SELFTEST_MUT で突然変異を試す（M1 台本を常に 0・M4 の確かめ）。
import sys, os, tempfile, importlib.util, subprocess, shutil, io, contextlib

HERE = os.path.dirname(os.path.abspath(__file__))
IDENT = os.environ.get('SELFTEST_IDENT') or os.path.normpath(os.path.join(HERE, '..', 'ident', 'ident_check.py'))   # 突然変異 M2 は書き換えた写しを渡す
RECHECK = os.environ.get('SELFTEST_RECHECK') or os.path.join(HERE, 'arrears_recheck.py')   # M4 も同じ
HEAD = ("quarter\tyear\tnation\tlaw\tmonetary\tonGoldBefore\tconvertibility\tpayRate\tgovDebt\tforeignShare\tinterestToRevenue"
        "\tunderLimit\twarm\tspare\tunopenedRoom\tcoverage\towes\tpredOld\tpredNew\tarrOldMonth\tarrNewMonth\tcumOld\tcumNew\tinsolvencyBefore\tprevInterestToRevenue")


def row(q, pay, under, owes, pold, pnew, ao, an, co, cn, spare='-1', nat='A', debt='100', prev='nan'):
    return '\t'.join([q, '1800', nat, '1', '1', '1', '0.9', pay, debt, '0.01', '0.5', under, '0', spare, '1', '0.98', owes, pold, pnew, ao, an, co, cn, '0', prev])


def write(d, name, rows):
    os.makedirs(d, exist_ok=True)
    with open(os.path.join(d, name), 'w', encoding='utf-8', newline='') as f:
        f.write(HEAD + '\n' + '\n'.join(rows) + '\n')


# 基準の行（旧の underLimit）：R1 共通・R2 基準だけ・R3 共通（本位の月の pred で残る）
B = [row('1', '0.9', '0', '1', '0', '1', '0.1', '0', '0.1', '0', prev='0.9'),
     row('1', '0.8', '0', '1', '0', '1', '0.2', '0', '0.3', '0'),
     row('1', '0.95', '0', '1', '1', '1', '0.05', '0', '0.35', '0')]
# 腕の行（新の underLimit）：R1 同じ・R3 は under＝1・owes＝0・predNew＝0・量 0・R4 腕だけ
A = [B[0],
     row('1', '0.95', '1', '0', '1', '0', '0', '0', '0', '0'),
     row('1', '0.7', '0', '1', '0', '1', '0.3', '0', '0.3', '0')]


def recheck(base_dir, arm_dir, script=RECHECK):
    r = subprocess.run([sys.executable, '-X', 'utf8', '-I', script, os.path.join(base_dir, 'arrears_s1.tsv'), os.path.join(arm_dir, 'arrears_s1.tsv')],
                       capture_output=True, text=True, encoding='utf-8')
    return r.returncode, r.stdout.strip().splitlines()[:2]


def load_ident():
    spec = importlib.util.spec_from_file_location('ident_check_st', IDENT)
    m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
    return m


def cmp(m, base, arm, rewritten):
    m.FAILS.clear(); m.MISSING.clear()
    with contextlib.redirect_stdout(io.StringIO()):
        m.cmp_ins(arm, base, {}, [], [], [], rewritten=rewritten)
    return len(m.FAILS)


def main():
    m = load_ident()
    tmp = tempfile.mkdtemp(prefix='c135ar_st_', dir=os.environ.get('SELFTEST_DIR') or None)
    bad = []
    try:
        script = RECHECK
        if os.environ.get('SELFTEST_MUT') == 'M1':   # 突然変異 M1：台本が常に 0 を返す
            script = os.path.join(tmp, 'always0.py'); open(script, 'w').write('import sys\nsys.exit(0)\n')
        rw = (f'arrears_s@{script}',)

        def case(name, base_rows, arm_rows, other_arm=None):
            bd, ad = os.path.join(tmp, name, 'base'), os.path.join(tmp, name, 'arm')
            write(bd, 'arrears_s1.tsv', base_rows); write(ad, 'arrears_s1.tsv', arm_rows)
            write(bd, 'clock_s1.tsv', ['1\t2', '3\t4']); write(ad, 'clock_s1.tsv', other_arm or ['1\t2', '3\t4'])
            return bd, ad

        # 組 1
        bd, ad = case('g1', B, A)
        rc, o = recheck(bd, ad)
        if rc != 0: bad.append(f'組 1：台本 rc={rc} {o}')
        if cmp(m, bd, ad, rw) != 0: bad.append('組 1：宣言ありで不一致')
        if cmp(m, bd, ad, ()) == 0: bad.append('組 1：宣言なしなのに不一致にならない')
        # 組 2a：共通の行の自由でない列（spare）を 1 つ変える
        A2a = [A[0].replace('\t-1\t', '\t-2\t', 1)] + A[1:]
        bd, ad = case('g2a', B, A2a)
        rc, o = recheck(bd, ad)
        if rc == 0: bad.append('組 2a：規則の外の違いで台本が 0')
        if cmp(m, bd, ad, rw) == 0: bad.append('組 2a：cmp_ins が不一致にならない')
        # 組 2b：基準だけの行の owes が 0（腕で消える理由が無い）
        B2b = [B[0], row('1', '0.8', '0', '0', '0', '0', '0', '0', '0.3', '0'), B[2]]
        bd, ad = case('g2b', B2b, A)
        rc, o = recheck(bd, ad)
        if rc == 0: bad.append('組 2b：基準だけの行が規則の外なのに台本が 0')
        if cmp(m, bd, ad, rw) == 0: bad.append('組 2b：cmp_ins が不一致にならない')
        # 組 2c：基準だけの行だが predOld＝1（本位の旧の述語で行が残るはず＝腕で消える理由が無い）。owes・underLimit の自分の列は規則どおり
        B2c = [B[0], row('1', '0.8', '0', '1', '1', '1', '0.2', '0', '0.3', '0'), B[2]]
        bd, ad = case('g2c', B2c, A)
        rc, o = recheck(bd, ad)
        if rc == 0: bad.append('組 2c：predOld＝1 の基準だけの行で台本が 0')
        if cmp(m, bd, ad, rw) == 0: bad.append('組 2c：cmp_ins が不一致にならない')
        # 組 3：arrears_s は規則どおり・別の表の 1 バイト
        bd, ad = case('g3', B, A, other_arm=['1\t2', '3\t5'])
        rc, o = recheck(bd, ad)
        if rc != 0: bad.append(f'組 3：台本 rc={rc}')
        if cmp(m, bd, ad, rw) == 0: bad.append('組 3：宣言の外の表の違いで不一致にならない')
        # 組 4a (1316)：共通の行（基準と腕に同じ行）で owes＝0 なのに arrNewMonth が立っている（govDebt＝0 なので (b) は owes＝0 を求める＝(f) だけが拾う）
        bad4a = row('1', '0.5', '0', '0', '0', '0', '0', '0.2', '0', '0', nat='C', debt='0')
        bd, ad = case('g4a', B + [bad4a], A + [bad4a])
        rc, o = recheck(bd, ad)
        if rc == 0: bad.append('組 4a：(f) の違反の行で台本が 0')
        if cmp(m, bd, ad, rw) == 0: bad.append('組 4a：cmp_ins が不一致にならない')
        # 組 4b (1316)：共通の行で国の cumNew が減る（owes・arrNewMonth は規則どおり 0＝(g) だけが拾う）
        r4b1 = row('1', '0.5', '0', '0', '0', '0', '0', '0', '0', '5', nat='D', debt='0')
        r4b2 = row('1', '0.6', '0', '0', '0', '0', '0', '0', '0', '3', nat='D', debt='0')
        bd, ad = case('g4b', B + [r4b1, r4b2], A + [r4b1, r4b2])
        rc, o = recheck(bd, ad)
        if rc == 0: bad.append('組 4b：(g) の違反の行で台本が 0')
        if cmp(m, bd, ad, rw) == 0: bad.append('組 4b：cmp_ins が不一致にならない')
        # 組 5 (1322)：(h) だけが拾う違反（underLimit＝1 なのに前の月の比 0.9 が L＝0.67 以上・他の (a)〜(g) は規則どおり）
        bad5 = row('1', '0.5', '1', '0', '0', '0', '0', '0', '0', '0', nat='E', debt='0', prev='0.9')
        bd, ad = case('g5', B + [bad5], A + [bad5])
        rc, o = recheck(bd, ad)
        if rc == 0: bad.append('組 5：(h) の違反の行で台本が 0')
        if cmp(m, bd, ad, rw) == 0: bad.append('組 5：cmp_ins が不一致にならない')
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    for b in bad: print('外れ：', b)
    print('自己試験：' + ('全て期待どおり（組 1・2a・2b・2c・3・4a・4b・5）' if not bad else f'外れ {len(bad)} 件'))
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())
