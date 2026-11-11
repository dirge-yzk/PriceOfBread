#!/usr/bin/env bash
# (1046) 読み終えた生を包む：bash pack.sh <フォルダ> [--rm]　＝<フォルダ>.tar.gz（--rm は包めたと数え合わせた後に元を消す）。
# 使う側が決める（読み手が生を読む keep_* と INSRAW には使えない＝拒む）。
exec python -X utf8 -I "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/prune.py" pack "$@"
