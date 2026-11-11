# (1046) prune.py の自己試験：使い捨ての写しの表（D:/PoB_work/c99dv_work/selftest）で、門 G1〜G3 と突然変異 M1〜M5 を回す。
#   python -X utf8 -I prune_selftest.py        ＝正しい実装で全て緑・各突然変異で赤（赤になれば 〈赤を確認〉）。終了コード 0＝全部期待どおり。
# 期待値は試験の中のリテラルのバイト列から hashlib で独立に作る（prune.py の関数で作らない）。
import sys, os, shutil, hashlib, importlib.util

HERE = os.path.dirname(os.path.abspath(__file__))
WORK = 'D:/PoB_work/c99dv_work/selftest'
spec = importlib.util.spec_from_file_location('prune', os.path.join(HERE, 'prune.py'))


def load():
    m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m


HDR = b'q\tn\ta\tb'
ROWS = [b'1\tx\t10\t20', b'2\ty\t11\t21', b'3\tz\t12\t22']


def crlf(rows, hdr=HDR): return b'\r\n'.join([hdr] + rows) + b'\r\n'
def lf(rows, hdr=HDR): return b'\n'.join([hdr] + rows) + b'\n'


def build():
    """基準（CRLF）と腕の ins を作る。ident_selftest 配下（許可された場所）。"""
    shutil.rmtree(WORK, ignore_errors=True)
    base = os.path.join(WORK, 'base'); ins = os.path.join(WORK, 'out_ib_10', 'ins')
    os.makedirs(base); os.makedirs(ins)
    w = lambda d, n, b: open(os.path.join(d, n), 'wb').write(b)
    w(base, 'same_s1.tsv', crlf(ROWS)); w(ins, 'same_s1.tsv', crlf(ROWS))                                  # 全体が一致（消える）
    w(base, 'lfonly_s1.tsv', crlf(ROWS)); w(ins, 'lfonly_s1.tsv', lf(ROWS))                               # 行末だけ違う（LF にそろえれば一致＝消える）
    w(base, 'diff_s1.tsv', crlf(ROWS)); w(ins, 'diff_s1.tsv', crlf(ROWS[:2] + [b'3\tz\t12\t23']))          # 最後の列の 1 バイトだけ違う（残る）
    w(base, 'grown_s1.tsv', crlf(ROWS)); w(ins, 'grown_s1.tsv', crlf([r + b'\tq' for r in ROWS], HDR + b'\tc'))  # 列が増えた（残る・先頭の列は一致）
    w(ins, 'added_s1.tsv', crlf(ROWS))                                                                    # 基準に無い（残る）
    w(base, 'known_s1.tsv', crlf(ROWS)); w(ins, 'known_s1.tsv', crlf(ROWS))                               # 宣言（残る）
    w(base, 'cearn_s1.tsv', crlf(ROWS)); w(ins, 'cearn_s1.tsv', crlf(ROWS))                               # 重い表（--heavy のときだけ消える）
    return base, ins


def names(ins): return sorted(os.listdir(ins))


def run(m, ins, base, verdict, heavy=False, heavy_keep=''):
    vf = os.path.join(WORK, 'check_out.txt')
    open(vf, 'w', encoding='utf-8').write('=== x ===\n' + (verdict + '\n' if verdict else '') + '成績表を残した\n')
    a = {'pos': [ins, base], 'verdict-file': vf, 'manifest': os.path.join(WORK, 'man.tsv'), 'pre-out': os.path.join(WORK, 'pre.tsv'),
         'deleted': os.path.join(WORK, 'del.txt'), 'keep-prefix': 'known_s,', 'heavy-keep': heavy_keep}
    if heavy: a['heavy'] = True
    return m.cmd_prune(a)


def scenario(m):
    """失敗の理由の一覧を返す（空＝緑）。"""
    bad = []
    base, ins = build()
    # M1 の検査：判定が未了／無いときは何も消えない
    for v in ('判定: 未了', ''):
        before = names(ins); run(m, ins, base, v)
        if names(ins) != before: bad.append(f'判定が「{v or "無し"}」なのに消えた')
    run(m, ins, base, '判定: 恒等')
    left = names(ins)
    want_left = ['added_s1.tsv', 'cearn_s1.tsv', 'diff_s1.tsv', 'grown_s1.tsv', 'known_s1.tsv']
    if 'same_s1.tsv' in left: bad.append('G1：全体が一致した表が消えていない')
    if 'lfonly_s1.tsv' in left: bad.append('G1/M5：行末だけ違う表（LF にそろえれば一致）が消えていない')
    if 'diff_s1.tsv' not in left: bad.append('G2/M2/M3：最後の列の 1 バイトが違う表が消えた')
    for n in ('grown_s1.tsv', 'added_s1.tsv', 'known_s1.tsv', 'cearn_s1.tsv'):
        if n not in left: bad.append(f'G2：残すべき表 {n} が消えた')
    if left != want_left: bad.append(f'残り {left} ≠ {want_left}')
    # manifest の sha1 を、リテラルから独立に作った期待値と比べる
    man = {l.split('\t')[0]: l.rstrip('\n').split('\t') for l in open(os.path.join(WORK, 'man.tsv'), encoding='utf-8') if not l.startswith('#')}
    exp_full = hashlib.sha1(lf(ROWS)).hexdigest()
    if man.get('lfonly_s1.tsv', ['', '', ''])[2] != exp_full: bad.append('M5：LF にそろえた全体の sha1 が期待と違う')
    exp_pre = hashlib.sha1(lf(ROWS)).hexdigest()   # 列が増えた表の先頭 4 列＝元の表の LF 版
    g = man.get('grown_s1.tsv', ['', '', '', '', ''])
    if g[3] != '4' or g[4] != exp_pre: bad.append('先頭の列の sha1 が期待と違う')
    # 重い表：--heavy のときだけ・名指しは残る
    base, ins = build(); run(m, ins, base, '判定: 恒等', heavy=True)
    if 'cearn_s1.tsv' in names(ins): bad.append('重い表が --heavy で消えない')
    base, ins = build(); run(m, ins, base, '判定: 恒等', heavy=True, heavy_keep='cearn_s')
    if 'cearn_s1.tsv' not in names(ins): bad.append('重い表が HEAVY_KEEP の名指しでも消えた')
    # G3：keep_* への呼び出しは拒まれ、中身は無傷
    kd = WORK + '/keep_x'
    os.makedirs(kd, exist_ok=True); open(kd + '/t_s1.tsv', 'wb').write(crlf(ROWS))
    bd = os.path.join(WORK, 'base2'); os.makedirs(bd, exist_ok=True); open(bd + '/t_s1.tsv', 'wb').write(crlf(ROWS))
    rc = m.cmd_prune({'pos': [kd, bd], 'verdict-file': os.path.join(WORK, 'check_out.txt'), 'manifest': os.path.join(WORK, 'm2.tsv')})
    vf = os.path.join(WORK, 'check_out.txt'); open(vf, 'w', encoding='utf-8').write('判定: 恒等\n')
    rc = m.cmd_prune({'pos': [kd, bd], 'verdict-file': vf, 'manifest': os.path.join(WORK, 'm2.tsv')})
    if rc == 0 or not os.path.exists(kd + '/t_s1.tsv'): bad.append('G3：keep_* が拒まれず消えた')
    for p in ('/d/PoB_work/keep_1044/base_ins', '/d/PoB_work/wt_c99cd/x', '/d/PoB_work/c99cd_work/x', '/c/x'):
        if m.guard(p) is None: bad.append(f'G3：guard が {p} を通した')
    if m.guard(WORK + '/out_ib_10/ins') is not None: bad.append('guard が許す場所を拒んだ')
    # 包む／解く
    d = os.path.join(WORK, 'packme'); os.makedirs(d, exist_ok=True); open(d + '/a.tsv', 'wb').write(crlf(ROWS))
    if m.cmd_pack({'pos': [d], 'rm': True}) != 0 or os.path.exists(d): bad.append('pack が失敗した')
    if m.cmd_unpack({'pos': [d + '.tar.gz', os.path.join(WORK, 'unp')]}) != 0 or open(os.path.join(WORK, 'unp', 'packme', 'a.tsv'), 'rb').read() != crlf(ROWS):
        bad.append('unpack の往復が合わない')
    return bad


def mutate(name):
    m = load()
    if name == 'M1': m.VERDICT_GATE = False
    elif name == 'M2':
        orig = m.lf_sha1
        def drop(p):   # 全体の sha1 から最後の列を落とす
            h = hashlib.sha1()
            for line in open(p, 'rb'):
                h.update(b'\t'.join(line.rstrip(b'\r\n').split(b'\t')[:-1]) + b'\n')
            return h.hexdigest()
        m.lf_sha1 = drop
    elif name == 'M3':
        m.should_delete = lambda name, bf, f, kp, ho, hk, g: (bf is not None and not any(name.startswith(x) for x in kp), 'x')
    elif name == 'M4': m.guard = lambda p, protect=(): None
    elif name == 'M5': m.LF_NORMALIZE = False
    return m


if __name__ == '__main__':
    ok = True
    g = scenario(load())
    print('正しい実装：' + ('緑' if not g else '赤 ' + '; '.join(g))); ok &= not g
    for n, desc in (('M1', '判定の前に刈る'), ('M2', 'sha1 で最後の列を落とす'), ('M3', '食い違った表を消す'), ('M4', 'keep_* の拒みを外す'), ('M5', 'CRLF をそろえない')):
        r = scenario(mutate(n))
        print(f'{n} {desc}：' + ('赤を確認 ← ' + r[0] if r else '緑のまま（穴）')); ok &= bool(r)
    shutil.rmtree(WORK, ignore_errors=True)
    sys.exit(0 if ok else 1)
