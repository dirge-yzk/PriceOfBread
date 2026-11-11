# (1002) c92gf 門（996 の G1・G4・G5・G2′ を引き継ぎ、〈それ以外〉≧100 は記録へ）。元の説明：(996) 門 G1〜G5（実データの 4 腕）。使い方: python -X utf8 -I gates1002f.py <腕名>=<stoplines1002 の出力>=<基準の stop906（m988）> ...
#   腕名が N2 のときはシャムと共和制イギリスの行も読む。--selfneg は G1 の負の対照（出力の 1 バイトを変えた写しで G1 が赤になること）。
import re, sys
A = [a for a in sys.argv[1:] if not a.startswith('--')]
SIAM = next((a[len('--siam='):] for a in sys.argv[1:] if a.startswith('--siam=')), None)
bad = 0
def ok(n, c, note=''):
    global bad
    print(('緑' if c else '赤'), n, note)
    if not c: bad += 1
def norm(t):
    # 結果の後の変更（996）：生の経路の文字列（D:/PoB_work/ の ins フォルダと fixrows の一時ファイル）だけを同じ印にして比べる。件数・表の中身は変えない。
    return re.sub(r'D:/PoB_work/[^：\t \n]+', '<PATH>', t)
def g1(out, ref, strict=False):
    o = out.split('\n'); r = ref.split('\n')
    if r and r[-1] == '': r = r[:-1]
    if not strict: o = norm('\n'.join(o)).split('\n'); r = norm('\n'.join(r)).split('\n')
    return len(o) >= len(r) and o[1:len(r)] == r[1:]
for a in A:
    arm, outp, refp = a.split('=', 2)
    out = open(outp, 'rb').read().decode('utf-8'); ref = open(refp, 'rb').read().decode('utf-8')
    ok(f'{arm} G1 旧の節が基準と 2 行目以降バイト一致（経路の文字列だけ印へ・結果の後の変更）', g1(out, ref))
    print(f'   参考：経路を印にしない厳密な比較＝{"緑" if g1(out, ref, True) else "赤（経路の 2 行が違う）"}')
    if '--selfneg' in sys.argv[1:]:
        bad_out = out.replace('== ukdefault', '== ukdefaulT', 1)
        ok(f'{arm} G1 の負の対照（見出し 1 字を変えた写しで G1 が赤）', not g1(bad_out, ref))
        ok(f'{arm} G1 の負の対照 2（表の数字 1 字を変えた写し）', not g1(out.replace('不履行のあった種', '不履行のあつた種', 1), ref))
    i = out.index('== 記録（1002')
    new = out[i:]; old = out[:i]
    # 旧の --founding 節の件数
    mo = re.search(r'建国の直後〔≦4 四半期〕(\d+)・建国の後の 0 の続き〔[^〕]*〕(\d+)・分母の痩せ (\d+)・それ以外 (\d+)）\t1e10 倍以上 (\d+)\t非有限 (\d+)', old)
    mn = re.search(r'上がり≧100 \d+（建国の直後 (\d+)・建国の後の0の続き (\d+)・分母の痩せ (\d+)・州の出入り (\d+)・それ以外 (\d+)）\t1e10 倍以上 (\d+)\t非有限 (\d+)', new)
    ok(f'{arm} 旧と新の件数の行が読める', bool(mo and mn))
    if mo and mn:
        of, oz, ot, oo, o10, onf = map(int, mo.groups()); nf, nz, nt, nm, no, n10, nnf = map(int, mn.groups())
        ok(f'{arm} G4 建国の直後・1e10・非有限が旧と同じ', (nf, n10, nnf) == (of, o10, onf), f'旧 {of}/{o10}/{onf} 新 {nf}/{n10}/{nnf}')
        ok(f'{arm} G5 0 の続き・痩せが旧と同じ・州の出入り＋それ以外＝旧のそれ以外', (nz, nt, nm + no) == (oz, ot, oo), f'旧 z{oz} t{ot} o{oo} 新 z{nz} t{nt} m{nm}+o{no}')
    m2 = re.search(r'〈それ以外〉≧100（上がり・下がり） (\d+)', new)
    print(f'   記録 {arm} 〈それ以外〉≧100（採否に使わない・m1001 の参照の最大は 0）：{m2.group(0) if m2 else "読めない"}')
    if arm.startswith('N2'):
        rows = [l.split('\t') for l in new.split('\n') if l.startswith('   行\t')]
        if SIAM:   # (1002) シャムの跳びが実在する生の gdp_jump1002 --rows の出力を --siam=<ファイル> で渡したときだけ（m1001 の N2 には無い）
            srows = [l.split('\t') for l in open(SIAM, 'rb').read().decode('utf-8').replace('\r', '').split('\n') if l.startswith('   行\t')]
            sia = {(c[1], c[3]): c[4] for c in srows if c[2] == 'シャム'}
            ok(f'{arm} G2′ シャム 種 7 q445（上がり）と q444（下がり）が 州の出入り', sia.get(('7', 'q445')) == '州の出入り' and sia.get(('7', 'q444')) == '州の出入り', str({k: v for k, v in sia.items() if k[0] == '7'}))
        uk = [c for c in rows if c[1] == '6' and c[2] == '共和制イギリス' and c[3] == 'q151']
        ok(f'{arm} 参考（996 の G3）共和制イギリス 種 6 q151 が それ以外', len(uk) == 1 and uk[0][4] == 'それ以外', str(uk[0][4:6]) if uk else '行なし')
sys.exit(1 if bad else 0)
