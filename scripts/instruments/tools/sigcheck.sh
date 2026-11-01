#!/usr/bin/env bash
# (728) コミットの本文の署名の行（Co-Authored-By）の確かめ：範囲の全てのコミット（merge を含む）の本文の末尾に署名の行があるか。
#   bash sigcheck.sh [範囲=origin/main..HEAD]   欠けたコミットを出して 1 を返す（push の前に使う）。
R="${1:-origin/main..HEAD}"; bad=0
for c in $(git rev-list "$R"); do
  if ! git log -1 --format=%B "$c" | grep -q '^Co-Authored-By: .* <noreply@anthropic.com>$'; then echo "署名なし：$(git log -1 --format='%h %s' "$c")"; bad=1; fi
done
[ $bad = 0 ] && echo "署名の行：範囲 $R の全てにあり"
exit $bad
