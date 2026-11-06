#!/usr/bin/env bash
# (895) c76ls 印 ident.mark の中身：ident c76ls を回し、.log の先頭に  VERDICT ident 恒等|否 <理由>  と landshare_s の門（gate895.py）の結果を書く（(888) ident888.sh の写し）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c76ls_work/ident.mark bash scripts/instruments/c76ls/ident895.sh
#   宣言＝--added-tables landshare_s・--tests-delta 15（LandShareTests の T1〜T10 の 15 本）・--saveleak。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
OUT=D:/PoB_work/c76ls_work/ident_out.txt
cd "$REPO" || exit 9
H0=$(git rev-parse HEAD)
bash scripts/instruments/ident/ident.sh run c76ls --tree "$REPO" --added-tables landshare_s --tests-delta 15 --saveleak > "$OUT" 2>&1
RC=$?
V=$(grep -E "判定:" "$OUT" | tail -1)
case "$V" in *恒等*) K=恒等 ;; *) K=否 ;; esac
echo "VERDICT ident $K ${V:-判定の行なし} (rc=$RC)"
INS=$(ls -d D:/PoB_work/ident_c76ls/out_ib*/ins 2>/dev/null | head -1)
echo "旗 0 の生（landshare_s つき）：$INS（ident の作業フォルダ D:/PoB_work/ident_c76ls）"
echo "HEAD 測る前 $H0・後 $(git rev-parse HEAD)"
[ -n "$INS" ] && python -X utf8 "$SP/gate895.py" "$INS" D:/PoB_work/c76ls_work/gate895_ident.txt 2>&1 | tail -3
cat "$OUT"
exit $RC
