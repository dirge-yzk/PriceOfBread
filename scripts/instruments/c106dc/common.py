# c106dc 共通：基準の木 ffb3838 の元ファイルを git show で読み、バイトで扱う
import subprocess, sys, re
BASE = 'ffb3838055a49bc32ef0fa901b473ddeccd52253'
REPO = 'D:/PoB_work/wt_c106dc'
F_CUR = 'docs/CURRENT.md'
F_LOG = 'docs/202609/99_実装ログ_20260903.md'
F_ONE = 'docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
F_RD = 'docs/README.md'
A_CUR = 'docs/CURRENT_書庫.md'
A_LOG9 = 'docs/202609/99_実装ログ_書庫_202609.md'
A_LOG10 = 'docs/202609/99_実装ログ_書庫_202610.md'
A_ONE = 'docs/202609/00_一覧_書庫.md'
def base(path):
    return subprocess.run(['git', '-C', REPO, 'show', BASE + ':' + path], capture_output=True, check=True).stdout
def lines(b):
    # 改行 \n で切る（\r は行の中に残す）。最後が \n で終わるなら末尾の空要素を保つ
    return b.split(b'\n')
def join(ls):
    return b'\n'.join(ls)
