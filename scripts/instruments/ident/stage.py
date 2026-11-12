# (1061) 恒等の判定の段の決定。段 1＝素の 5 種の全行＋.cost の件数の行＋計器つき 10 種の小さい表の集合だけ。段 2＝全表。
#   python -X utf8 -I stage.py <木> <基準のコミット>     ＝ 1 行目に 段=1 か 段=2・以後に理由（ident.sh が marks/stage.txt に書く）
# 段 2 に上がる条件（手で外せない＝ident.sh に段を決める引数は無い）：基準のコミットとの差（コミット済み・未コミット・未追跡）が
# 計器（Instruments*.cs）・SimCore/Display・計器の台本（scripts/instruments の直下と読み手・道具の下）・measure.sh に触れるとき。
# 線ごとの使い捨ての台本（scripts/instruments/c101py/ など）は段 2 の条件にしない（常に段 2 になって二段にした意味が無くなる）。
import re, subprocess, sys

STAGE2_RX = [
    r'^PriceOfBread\.SimCore/Instruments[^/]*\.cs$',
    r'^PriceOfBread\.SimCore/Display/',
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


def decide(tree, base):
    """(段, 理由, 当たったファイル)。差が測れなければ段 2。"""
    files = changed_files(tree, base)
    if files is None: return 2, '基準のコミットとの差が測れない（git が失敗）＝段 2', []
    st, hit = stage_of(files)
    return st, f'基準 {base} との差 {len(files)} 本・段 2 の条件に当たった {len(hit)} 本', hit


if __name__ == '__main__':
    st, why, hit = decide(sys.argv[1], sys.argv[2])
    print(f'段={st}')
    print(why)
    for h in hit[:12]: print('  ' + h)
