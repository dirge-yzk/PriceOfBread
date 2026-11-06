#!/usr/bin/env bash
# (889) c75cp 印 ident.mark の中身：ident c75cp を回し、.log の先頭に  VERDICT ident 恒等|否 <理由>  と生の場所を書く。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c75cp_work/ident.mark bash scripts/instruments/c75cp/ident889.sh
#   宣言＝--added-tables crownpc_s（新しい表）・--tests-delta 8（CrownPcInstrumentTests の 8 件・--list-tests で数え直した）・--saveleak（savetest と leakhunt を回す指定＝許しの宣言ではない）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
OUT=D:/PoB_work/c75cp_work/ident_out.txt
cd "$REPO" || exit 9
H0=$(git rev-parse HEAD)
bash scripts/instruments/ident/ident.sh run c75cp --tree "$REPO" --added-tables crownpc_s --tests-delta 8 --saveleak > "$OUT" 2>&1
RC=$?
V=$(grep -E "判定:" "$OUT" | tail -1)
case "$V" in *恒等*) K=恒等 ;; *) K=否 ;; esac
echo "VERDICT ident $K ${V:-判定の行なし} (rc=$RC)"
echo "生：$(ls -d D:/PoB_work/ident_c75cp/out_ib*/ins 2>/dev/null | head -1)（ident の作業フォルダ D:/PoB_work/ident_c75cp）"
echo "HEAD 測る前 $H0・後 $(git rev-parse HEAD)"
cat "$OUT"
exit $RC
