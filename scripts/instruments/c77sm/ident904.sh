#!/usr/bin/env bash
# (904) c77sm 印 ident.mark の中身：ident c77sm を回し、.log の先頭に  VERDICT ident 恒等|否 <理由>  と landshare_s の門（gate904.py）の結果を書く（(888) ident888.sh の写し）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c77sm_work/ident.mark bash scripts/instruments/c77sm/ident904.sh
#   宣言＝--grown-ok landshare_s（末尾 3 列）・--tests-delta 2（T7e と M8）・--saveleak。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
OUT=D:/PoB_work/c77sm_work/ident_out.txt
cd "$REPO" || exit 9
H0=$(git rev-parse HEAD)
bash scripts/instruments/ident/ident.sh run c77sm --tree "$REPO" --grown-ok landshare_s --tests-delta 2 --saveleak > "$OUT" 2>&1
RC=$?
V=$(grep -E "判定:" "$OUT" | tail -1)
case "$V" in *恒等*) K=恒等 ;; *) K=否 ;; esac
echo "VERDICT ident $K ${V:-判定の行なし} (rc=$RC)"
INS=$(ls -d D:/PoB_work/ident_c77sm/out_ib*/ins 2>/dev/null | head -1)
echo "旗 0 の生（landshare_s つき）：$INS（ident の作業フォルダ D:/PoB_work/ident_c77sm）"
echo "HEAD 測る前 $H0・後 $(git rev-parse HEAD)"
[ -n "$INS" ] && python -X utf8 "$SP/gate904.py" "$INS" D:/PoB_work/c77sm_work/gate904_ident.txt 2>&1 | tail -3
cat "$OUT"
exit $RC
