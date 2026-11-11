#!/usr/bin/env bash
# (928) c81im 印 ident.mark の中身：ident c81im を回し、.log の先頭に  VERDICT ident 恒等|否 <理由>  と生の場所を書く。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c81im_work/ident.mark bash scripts/instruments/c81im/ident928.sh
#   宣言＝--added-tables imp_s（新しい表）・--grown-ok crownpc_s（末尾に 1 列）・--tests-delta 3（ImposeAndCrownSourceTests の 3 件）・--saveleak（savetest と leakhunt を回す指定）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
OUT=D:/PoB_work/c81im_work/ident_out.txt
cd "$REPO" || exit 9
H0=$(git rev-parse HEAD)
bash scripts/instruments/ident/ident.sh run c81im --tree "$REPO" --added-tables imp_s --grown-ok crownpc_s --tests-delta 3 --saveleak > "$OUT" 2>&1
RC=$?
V=$(grep -E "判定:" "$OUT" | tail -1)
case "$V" in *恒等*) K=恒等 ;; *) K=否 ;; esac
echo "VERDICT ident $K ${V:-判定の行なし} (rc=$RC)"
echo "生：$(ls -d D:/PoB_work/ident_c81im/out_ib*/ins 2>/dev/null | head -1)（ident の作業フォルダ D:/PoB_work/ident_c81im）"
echo "HEAD 測る前 $H0・後 $(git rev-parse HEAD)"
cat "$OUT"
exit $RC
