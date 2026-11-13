# (1061) 恒等の判定の段の決定。段 1＝素の 5 種の全行＋.cost の件数の行＋計器つき 10 種の小さい表の集合だけ。段 2＝全表。
#   python -X utf8 -I stage.py <木> <基準のコミット>     ＝ 1 行目に 段=1 か 段=2・以後に理由（ident.sh が marks/stage.txt に書く）
# 段 2 に上がる条件（手で外せない＝ident.sh に段を決める引数は無い）：基準のコミットとの差（コミット済み・未コミット・未追跡）が
# 計器（SimCore/Instruments/ の下・直下の Instruments*.cs）・SimCore/Display・計器の台本（scripts/instruments の直下と読み手・道具の下）・measure.sh に触れるとき。
# 線ごとの使い捨ての台本（scripts/instruments/c101py/ など）は段 2 の条件にしない（常に段 2 になって二段にした意味が無くなる）。
import os, re, subprocess, sys

STAGE2_RX = [
    r'^PriceOfBread\.SimCore/(Instruments/|Instruments[^/]*\.cs$)',   # (1105) 計器は SimCore/Instruments/ の下（直下の Instruments*.cs も段 2 のまま）
    r'^PriceOfBread\.SimCore/Display/',
    r'^PriceOfBread\.Display/',   # (1111) 表示の層は別アセンブリ PriceOfBread.Display（旧の SimCore/Display の行は残す）
    r'^measure\.sh$',
    r'^scripts/instruments/[^/]+$',
    r'^scripts/instruments/(ident|tools|mon|hol|brk693|brk|m808|c92gf|c78gz|m1017|r833|c11fl|c10cp)/',
]


def stage_of(files, rx=None):
    """ファイル名（リポジトリからの相対・/ 区切り）の一覧 → (段, 当たったファイル)。"""
    rx = [re.compile(x) for x in (rx or STAGE2_RX)]
    hit = sorted({f for f in files if any(r.search(f) for r in rx)})
    return (2 if hit else 1), hit


def changed_files(tree, base):
    """基準との差のファイル名。git が失敗したとき（基準のコミットが引けない・git の木でない）は None（呼び手が段 2 にする＝差が空で段 1 にならない）。"""
    def git(*a):
        r = subprocess.run(['git', '-C', tree] + list(a), capture_output=True, text=True, encoding='utf-8')
        return r.stdout.splitlines() if r.returncode == 0 else None
    d, u = git('diff', '--name-only', base), git('ls-files', '-o', '--exclude-standard')
    if d is None or u is None: return None
    return sorted(f.strip('"') for f in set(d) | set(u) if f)


# (1080) ident.cfg の差のうち、基準を替えるだけの鍵（名で列挙・模様にしない）。SMALL・BYPASS・PRESPEC・KNOWN・PRUNE_KEEP ほか判定を変える鍵は入れない。
# ヌル腕の鍵は ident.cfg に無い（足すときはここへ名で足す）。
CFG_PATH = 'scripts/instruments/ident/ident.cfg'
BASELINE_KEYS = ('B5', 'B10INS', 'MON', 'NG_BASE', 'INSRAW', 'BASE_COMMIT', 'STAGE_BASE', 'TESTS_NONFULL', 'TESTS_FULL')


def cfg_diff_baseline_only(old_text, new_text, keys=None):
    """ident.cfg の旧と新 → (基準の鍵と注と空行だけの差か, 判定を変える差の行の一覧)。行の重なりを数えて比べる。"""
    keys = tuple(keys if keys is not None else BASELINE_KEYS)
    from collections import Counter
    a = Counter(l.rstrip() for l in old_text.splitlines())
    b = Counter(l.rstrip() for l in new_text.splitlines())
    diff = list((a - b).elements()) + list((b - a).elements())
    bad = []
    for l in diff:
        t = l.strip()
        if not t or t.startswith('#'): continue
        k = t.split('=', 1)[0].strip() if '=' in t else None
        if k not in keys: bad.append(t[:60])
    return not bad, bad


def cfg_only_baseline(tree, base):
    """木の ident.cfg が基準のコミットのものと、基準の鍵・注・空行だけ違うか。読めなければ (False, ['読めない'])。"""
    r = subprocess.run(['git', '-C', tree, 'show', f'{base}:{CFG_PATH}'], capture_output=True, text=True, encoding='utf-8')
    if r.returncode != 0: return False, ['基準のコミットに ident.cfg が無い']
    try: new = open(os.path.join(tree, CFG_PATH), 'rb').read().decode('utf-8')
    except OSError: return False, ['木の ident.cfg が読めない']
    return cfg_diff_baseline_only(r.stdout, new)


def decide(tree, base):
    """(段, 理由, 当たったファイル)。差が測れなければ段 2。ident.cfg は基準の鍵・注だけの差なら段 2 の条件に数えない。"""
    files = changed_files(tree, base)
    if files is None: return 2, '基準のコミットとの差が測れない（git が失敗）＝段 2', []
    skipped = ''
    if CFG_PATH in files:
        ok, bad = cfg_only_baseline(tree, base)
        if ok:
            files = [f for f in files if f != CFG_PATH]
            skipped = '・ident.cfg は基準の鍵と注だけの差＝数えない'
        else:
            skipped = '・ident.cfg の判定を変える差：' + ' | '.join(bad[:3])
    st, hit = stage_of(files)
    return st, f'基準 {base} との差 {len(files)} 本・段 2 の条件に当たった {len(hit)} 本{skipped}', hit


if __name__ == '__main__':
    st, why, hit = decide(sys.argv[1], sys.argv[2])
    print(f'段={st}')
    print(why)
    print('STAGE_BASE=' + sys.argv[2])
    for h in hit[:12]: print('  段 2 の理由：' + h)
