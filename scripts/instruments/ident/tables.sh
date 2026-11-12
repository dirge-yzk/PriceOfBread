#!/usr/bin/env bash
# (1061) 計器の出す表を選ぶ口（POB_INSTRUMENTS_TABLES）に渡す名前の並びを作る。腕 B・ヌル腕など ident の外の腕が、読み手の要る表だけを出すための道具。
#   export POB_INSTRUMENTS_TABLES="$(bash scripts/instruments/ident/tables.sh [その門の台本が読む表...])"   ＝ 標準の読み手の表（ident.cfg の SMALL）＋名指しの表
#   表の名は nation_s の形（完全一致）。未設定または空なら全表（今まで通り）。門の台本は自分の読む表を名乗ること（名乗らない表は出ない）。
D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
{ grep '^SMALL=' "$D/ident.cfg" | head -1 | cut -d= -f2- | tr -d '\r' | tr ',' '\n'; for t in "$@"; do echo "$t"; done; } | awk 'NF && !s[$0]++' | paste -sd, -
