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
    def git(*a):
        return subprocess.run(['git', '-C', tree] + list(a), capture_output=True, text=True, encoding='utf-8').stdout.splitlines()
    fs = set(git('diff', '--name-only', base)) | set(git('ls-files', '-o', '--exclude-standard'))
    return sorted(f.strip('"') for f in fs if f)


if __name__ == '__main__':
    tree, base = sys.argv[1], sys.argv[2]
    files = changed_files(tree, base)
    st, hit = stage_of(files)
    print(f'段={st}')
    print(f'基準 {base} との差 {len(files)} 本・段 2 の条件に当たった {len(hit)} 本')
    for h in hit[:12]: print('  ' + h)
