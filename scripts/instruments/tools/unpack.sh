#!/usr/bin/env bash
# (1046) 解く：bash unpack.sh <tar.gz> <出力先>（出力先は /d/PoB_work/ident_* か c99dv_work の下）。
exec python -X utf8 -I "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/prune.py" unpack "$@"
