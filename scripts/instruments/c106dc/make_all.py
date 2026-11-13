# c106dc 作成：基準の木 ffb3838 の元ファイルから、残りと書庫を書く（バイトで）。決定的。
import sys, re
import os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # (1110)
from common import *
from split import *

def pieces(L, dest, key):
    assert L[-1] == b''
    L = L[:-1]; dest = dest[:-1]
    return b''.join(l + b'\n' for l, d in zip(L, dest) if d == key)

def put(path, data):
    open(REPO + '/' + path, 'wb').write(data)

# 付け足す行（元に無い行）。check.py も同じ表を読む。
ADD = {
  'cur_keep': (9, '⚠ 18 行目以降（前の更新の行・基線の行・旧の節）は `docs/CURRENT_書庫.md` へ移した（中身と並びは元のまま）。新しい合流は最終更新の行を最上段に置き、前の更新の行が 3 本を超えた分は書庫の先頭へ移す。\n\n'.encode()),
  'one_keep': (2, '★2026-11-13 (1107)：打消し線の行のうち行の中の最後の日付が 2026-10-13 より前の物は `00_一覧_書庫.md`（同じフォルダ）へ移した（行は消していない・中身と並びは元のまま）。新しい打消しの行はこの一覧に残し、書庫へは次の整理でまとめて移す。\n★書き場所（2026-11-13 (1107)）：合流が書くのは、実装ログ＝`99_実装ログ_20260903.md`（見出しの日付が当月の回だけ・過ぎた月は同じフォルダの `99_実装ログ_書庫_<年月>.md` へ移す）・CURRENT＝`docs/CURRENT.md`（最終更新と前の更新 2 本・古い行は `CURRENT_書庫.md` の先頭へ）・この一覧。書庫へは追記しない。\n\n'.encode()),
  'cur_arc': (0, '# CURRENT の書庫（`CURRENT.md` の 18 行目以降をそのまま移した物・2026-11-13 (1107)・追記しない）\n\n'.encode()),
  'one_arc': (0, '# 00 一覧の書庫（打消し線の行のうち最後の日付が 2026-10-13 より前の物・元の並びのまま・2026-11-13 (1107)・追記しない）\n\n'.encode()),
  'log9_arc': (0, '# 実装ログの書庫 2026-09（`99_実装ログ_20260903.md` の 2026-09 の回を元の並びのまま移した物・2026-11-13 (1107)・追記しない）\n\n'.encode()),
  'log10_arc': (0, '# 実装ログの書庫 2026-10（`99_実装ログ_20260903.md` の 2026-10 の回を元の並びのまま移した物・2026-11-13 (1107)・追記しない）\n\n'.encode()),
}
CUR_KEEP_LINES = 17   # 〈今〉＝先頭の 17 行（見出し・最終更新・前の更新 2 本）

def with_add(key, data):
    idx, b = ADD[key]
    parts = data.split(b'\n')   # 行の頭の位置 idx（0 始まりの行）に挿入
    # idx 行目の頭へ
    pos = 0
    for _ in range(idx):
        pos = data.index(b'\n', pos) + 1
    return data[:pos] + b + data[pos:]

def main():
    # --- 実装ログ ---
    L, d = split_log(base(F_LOG))
    put(F_LOG, pieces(L, d, 'keep'))
    put(A_LOG9, with_add('log9_arc', pieces(L, d, '09')))
    put(A_LOG10, with_add('log10_arc', pieces(L, d, '10')))
    # --- 00 一覧 ---
    L, d = split_one(base(F_ONE))
    put(F_ONE, with_add('one_keep', pieces(L, d, 'keep')))
    put(A_ONE, with_add('one_arc', pieces(L, d, 'arc')))
    # --- CURRENT ---
    L = lines(base(F_CUR)); assert L[-1] == b''
    d = ['keep' if i < CUR_KEEP_LINES else 'arc' for i in range(len(L))]
    put(F_CUR, with_add('cur_keep', pieces(L, d, 'keep')))
    put(A_CUR, with_add('cur_arc', pieces(L, d, 'arc')))

if __name__ == '__main__':
    main()
