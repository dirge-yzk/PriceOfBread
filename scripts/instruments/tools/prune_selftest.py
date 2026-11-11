# (1054) prune.py の自己試験：使い捨ての写しの表（D:/PoB_work/c100dv_work/selftest）で、門 G1〜G4 と突然変異 M1〜M6 を回す。
#   python -X utf8 -I prune_selftest.py        ＝正しい実装で全て緑・各突然変異で赤（赤になれば 〈赤を確認〉）。終了コード 0＝全部期待どおり。
# 期待値は試験の中のリテラルのバイト列から hashlib で独立に作る（prune.py の関数で作らない）。
import sys, os, shutil, hashlib, subprocess, importlib.util

HERE = os.path.dirname(os.path.abspath(__file__))
WORK = 'D:/PoB_work/c100dv_work/selftest'
ALLOW = 'c100dv_work'


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m


def load_prune(): return load(os.path.join(HERE, 'prune.py'), 'prune')


HDR = b'q\tn\ta\tb'
ROWS = [b'1\tx\t10\t20', b'2\ty\t11\t21', b'3\tz\t12\t22']


def crlf(rows, hdr=HDR): return b'\r\n'.join([hdr] + rows) + b'\r\n'
def lf(rows, hdr=HDR): return b'\n'.join([hdr] + rows) + b'\n'
def sha(b): return hashlib.sha1(b).hexdigest()


def rm_junction(p):
    if os.path.isdir(p) and (os.path.islink(p) or (hasattr(os.path, 'isjunction') and os.path.isjunction(p))): os.rmdir(p)


def cleanup():
    rm_junction(os.path.join(WORK, 'jt', 'ins'))
    shutil.rmtree(WORK, ignore_errors=True)


def build():
    """基準（CRLF）と腕の ins を作る。c100dv_work の下（許可された場所）。"""
    cleanup()
    base = os.path.join(WORK, 'base'); ins = os.path.join(WORK, 'out_ib_10', 'ins')
    os.makedirs(base); os.makedirs(ins)
    w = lambda d, n, b: open(os.path.join(d, n), 'wb').write(b)
    w(base, 'same_s1.tsv', crlf(ROWS)); w(ins, 'same_s1.tsv', crlf(ROWS))                                  # 生が一致（消える）
    w(base, 'lfonly_s1.tsv', crlf(ROWS)); w(ins, 'lfonly_s1.tsv', lf(ROWS))                               # 行末だけ違う（LF にそろえれば一致＝生は違う＝残る）
    w(base, 'diff_s1.tsv', crlf(ROWS)); w(ins, 'diff_s1.tsv', crlf(ROWS[:2] + [b'3\tz\t12\t23']))          # 最後の列の 1 バイトだけ違う（残る）
    w(base, 'grown_s1.tsv', crlf(ROWS)); w(ins, 'grown_s1.tsv', crlf([r + b'\tq' for r in ROWS], HDR + b'\tc'))  # 列が増えた（残る）
    w(ins, 'added_s1.tsv', crlf(ROWS))                                                                    # 基準に無い（残る）
    w(base, 'known_s1.tsv', crlf(ROWS)); w(ins, 'known_s1.tsv', crlf(ROWS))                               # 宣言（残る）
    w(base, 'cearn_s1.tsv', crlf(ROWS)); w(ins, 'cearn_s1.tsv', crlf(ROWS))                               # 重い表（--heavy のときだけ消える）
    w(base, 'pre_s1.tsv', crlf(ROWS)); w(ins, 'pre_s1.tsv', crlf(ROWS))                                   # 先頭の列の記録（4 列）
    open(os.path.join(WORK, 'spec.tsv'), 'w', encoding='utf-8').write('pre_s1.tsv\t4\tx\n')
    return base, ins


def names(ins): return sorted(os.listdir(ins))


def run(m, ins, base, verdict, heavy=False, heavy_keep='', copy=True):
    vf = os.path.join(WORK, 'check_out.txt')
    open(vf, 'w', encoding='utf-8').write('=== x ===\n' + (verdict + '\n' if verdict else '') + '成績表を残した\n')
    a = {'pos': [ins, base], 'verdict-file': vf, 'manifest': os.path.join(WORK, 'man.tsv'), 'pre-spec': os.path.join(WORK, 'spec.tsv'),
         'deleted': os.path.join(WORK, 'del.txt'), 'keep-prefix': 'known_s,', 'heavy-keep': heavy_keep, 'allow': ALLOW}
    if copy: a['copy-to'] = os.path.join(WORK, 'meas', 'ident_x_manifest.tsv')
    if heavy: a['heavy'] = True
    return m.cmd_prune(a)


def mkjunction(link, target):
    os.makedirs(os.path.dirname(link), exist_ok=True)
    r = subprocess.run(['cmd', '/c', 'mklink', '/J', link.replace('/', '\\'), target.replace('/', '\\')], capture_output=True)
    return r.returncode == 0


def forms(p):
    """同じ場所の 3 つの書き方：/d/…・D:/…・D:\\…"""
    q = p.replace('\\', '/')
    d = 'D:/' + q.split(':/', 1)[1] if ':/' in q else 'D:' + q[2:]
    return ['/d/' + d[3:], d, d.replace('/', '\\')]


def scenario(m, ck):
    """失敗の理由の一覧を返す（空＝緑）。ck＝ident_check のモジュール。"""
    bad = []
    base, ins = build()
    # M1・M2：判定が 恒等 でないときは何も消えない（未了・不一致・無し）
    for v in ('判定: 未了', '判定: 不一致', ''):
        before = names(ins); run(m, ins, base, v)
        if names(ins) != before: bad.append(f'G：判定が「{v or "無し"}」なのに消えた')
    # 恒等のとき
    run(m, ins, base, '判定: 恒等')
    left = names(ins)
    want_left = ['added_s1.tsv', 'cearn_s1.tsv', 'diff_s1.tsv', 'grown_s1.tsv', 'known_s1.tsv', 'lfonly_s1.tsv']
    if 'same_s1.tsv' in left: bad.append('G1：生が一致した表が消えていない')
    if 'pre_s1.tsv' in left: bad.append('G1：生が一致した表（先頭の列の記録つき）が消えていない')
    if 'lfonly_s1.tsv' not in left: bad.append('M3：行末だけ違う表（生は違う）が消えた')
    if 'diff_s1.tsv' not in left: bad.append('G2：最後の列の 1 バイトが違う表が消えた')
    for n in ('grown_s1.tsv', 'added_s1.tsv', 'cearn_s1.tsv'):
        if n not in left: bad.append(f'G2：残すべき表 {n} が消えた')
    if 'known_s1.tsv' not in left: bad.append('M4：揺れ・宣言の表が消えた')
    if left != want_left: bad.append(f'残り {left} ≠ {want_left}')
    # manifest の sha1 を、リテラルから独立に作った期待値と比べる
    man = {l.split('\t')[0]: l.rstrip('\n').split('\t') for l in open(os.path.join(WORK, 'man.tsv'), encoding='utf-8') if not l.startswith('#')}
    s = man.get('lfonly_s1.tsv', [''] * 8)
    if s[2] != sha(lf(ROWS)) or s[3] != sha(lf(ROWS)) or s[6] != '(大きさが違う)': bad.append('記録：lfonly の生・LF の sha1 と基準の欄（大きさが違う）が期待と違う')
    s = man.get('same_s1.tsv', [''] * 8)
    if s[2] != sha(crlf(ROWS)) or s[3] != sha(lf(ROWS)): bad.append('記録：same の生の sha1 か LF の sha1 が期待と違う')
    s = man.get('pre_s1.tsv', [''] * 8)
    if s[4] != '4' or s[5] != sha(lf(ROWS)): bad.append('記録：先頭の列（--pre-spec の 4 列）の sha1 が期待と違う')
    g = man.get('grown_s1.tsv', [''] * 8)
    if g[4] != '4' or g[5] != sha(lf(ROWS)): bad.append('記録：列が増えた表の先頭の列の sha1 が期待と違う')
    # M6：measurements の写し
    cp = os.path.join(WORK, 'meas', 'ident_x_manifest.tsv')
    if not os.path.isfile(cp) or open(cp, 'rb').read() != open(os.path.join(WORK, 'man.tsv'), 'rb').read(): bad.append('M6：manifest の写し（--copy-to）が無いか違う')
    # 重い表：--heavy のときだけ・名指しは残る
    base, ins = build(); run(m, ins, base, '判定: 恒等', heavy=True)
    if 'cearn_s1.tsv' in names(ins): bad.append('重い表が --heavy で消えない')
    base, ins = build(); run(m, ins, base, '判定: 恒等', heavy=True, heavy_keep='cearn_s')
    if 'cearn_s1.tsv' not in names(ins): bad.append('重い表が HEAVY_KEEP の名指しでも消えた')
    # G4：刈った後の再 check（ident_check.cmp_ins）が刈った表を不問にする。manifest が無い・sha1 が壊れていれば不問にしない
    base, ins = build(); run(m, ins, base, '判定: 恒等', heavy=True)
    mp = os.path.join(WORK, 'prune_manifest.tsv')
    shutil.copyfile(os.path.join(WORK, 'man.tsv'), mp)

    def recheck():
        ck.FAILS.clear(); ck.MISSING.clear()
        ck.cmp_ins(ins, base, {}, [], ['added_s1'], ['grown_s1'], (), ())
        return len(ck.FAILS) + len(ck.MISSING)
    ck.say = lambda *_a, **_k: None
    n0 = recheck()
    os.remove(mp); n_none = recheck()
    if not n_none > n0: bad.append(f'G4：manifest が無いのに刈った表が不問のまま（{n0} 対 {n_none}）')
    t = open(os.path.join(WORK, 'man.tsv'), encoding='utf-8').read().replace(sha(crlf(ROWS)), '0' * 40)
    open(mp, 'w', encoding='utf-8', newline='').write(t)
    if not recheck() > n0: bad.append('G4：manifest の sha1 が壊れているのに再 check が通った')
    # G3：リンク・保護された場所・ins/ の外は拒まれ、中身は無傷（3 つの書き方の全て）
    kd = WORK + '/keep_dummy'
    os.makedirs(kd, exist_ok=True); open(kd + '/t_s1.tsv', 'wb').write(crlf(ROWS))
    bd = os.path.join(WORK, 'base2'); os.makedirs(bd, exist_ok=True); open(bd + '/t_s1.tsv', 'wb').write(crlf(ROWS))
    vf = os.path.join(WORK, 'check_out.txt'); open(vf, 'w', encoding='utf-8').write('判定: 恒等\n')
    for f in forms(kd):
        rc = m.cmd_prune({'pos': [f, bd], 'verdict-file': vf, 'manifest': os.path.join(WORK, 'm2.tsv'), 'allow': ALLOW})
        if rc == 0 or not os.path.exists(kd + '/t_s1.tsv'): bad.append(f'G3：keep_* が {f} の形で拒まれず消えた')
    jl = WORK + '/jt/ins'
    if not mkjunction(jl, kd): bad.append('試験の準備：ジャンクションが作れない')
    else:
        for f in forms(jl):
            rc = m.cmd_prune({'pos': [f, bd], 'verdict-file': vf, 'manifest': os.path.join(WORK, 'm2.tsv'), 'allow': ALLOW})
            if rc == 0 or not os.path.exists(kd + '/t_s1.tsv'): bad.append(f'G3：ジャンクション経由が {f} の形で拒まれず消えた')
            if m.guard(f, allow=[ALLOW]) is None: bad.append(f'G3：guard がジャンクション {f} を通した')
        rm_junction(jl)
    for p in ('/d/PoB_work/keep_1053/base_ins', 'D:/PoB_work/keep_1053/base_ins', 'D:\\PoB_work\\keep_1053\\base_ins', '/d/PoB_work/wt_c100dv/x/ins',
              '/d/PoB_work/c100mx_work/out_ib_10/ins', '/d/PoB_work/ident_other/out_ib_10/ins', '/c/x/ins', '/d/PoB_work/c100dv_work/selftest/out_ib_10'):
        if m.guard(p, allow=[ALLOW]) is None: bad.append(f'G3：guard が {p} を通した')
    for f in forms(WORK + '/out_ib_10/ins'):
        if m.guard(f, allow=[ALLOW]) is not None: bad.append(f'guard が許す場所を {f} の形で拒んだ')
    # 包む／解く
    d = os.path.join(WORK, 'packme'); os.makedirs(d, exist_ok=True); open(d + '/a.tsv', 'wb').write(crlf(ROWS))
    if m.cmd_pack({'pos': [d], 'rm': True, 'allow': ALLOW}) != 0 or os.path.exists(d): bad.append('pack が失敗した')
    if m.cmd_unpack({'pos': [d + '.tar.gz', os.path.join(WORK, 'unp')], 'allow': ALLOW}) != 0 or open(os.path.join(WORK, 'unp', 'packme', 'a.tsv'), 'rb').read() != crlf(ROWS):
        bad.append('unpack の往復が合わない')
    return bad


def mutate(name):
    m = load_prune()
    if name == 'M1': m.VERDICT_GATE = False              # 判定の前に刈る
    elif name == 'M2': m.ONLY_IDENT = False               # 不一致でも刈る
    elif name == 'M3': m.RAW_KEY = False                  # LF にそろえた sha1 で消す
    elif name == 'M4': m.KEEP_FLAKY = False               # 揺れの表を消す
    elif name == 'M5': m.CAN_REALPATH = False; m.GUARD_NATIVE = lambda p: p   # /d/ の形・リンクの拒みを外す（元の c99dv の形）
    elif name == 'M6': m.COPY_TO = False                  # --copy-to を落とす
    return m


if __name__ == '__main__':
    ok = True
    ck = load(os.path.join(HERE, '..', 'ident', 'ident_check.py'), 'ident_check')
    g = scenario(load_prune(), ck)
    print('正しい実装：' + ('緑' if not g else '赤 ' + '; '.join(g))); ok &= not g
    for n, desc in (('M1', '判定の前に刈る'), ('M2', '不一致でも刈る'), ('M3', 'LF にそろえた sha1 で消す'), ('M4', '揺れの表を消す'),
                    ('M5', '/d/ の形・リンクの拒みを外す'), ('M6', '--copy-to を落とす')):
        r = scenario(mutate(n), ck)
        print(f'{n} {desc}：' + ('赤を確認（' + str(len(r)) + ' 件）← ' + r[0] if r else '緑のまま（穴）')); ok &= bool(r)
    cleanup()
    sys.exit(0 if ok else 1)
