#!/usr/bin/env bash
# (880) c74id 印 f0_ready.mark の中身：ident c74id を回し、.log の先頭に  VERDICT ident 恒等|否 <理由>  と旗 0 の生の場所を書く。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c74id_work/f0_ready.mark bash scripts/instruments/c74id/ident880.sh
#   宣言＝--grown-ok cearn_s（末尾 1 列）・--tests-delta 1（CearnTradeColumnsTests の新しい 1 件・--list-tests で数え直した）・--saveleak。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
OUT=D:/PoB_work/c74id_work/ident_out.txt
cd "$REPO" || exit 9
H0=$(git rev-parse HEAD)
bash scripts/instruments/ident/ident.sh run c74id --tree "$REPO" --grown-ok cearn_s --tests-delta 1 --saveleak > "$OUT" 2>&1
RC=$?
V=$(grep -E "判定:" "$OUT" | tail -1)
case "$V" in *恒等*) K=恒等 ;; *) K=否 ;; esac
echo "VERDICT ident $K ${V:-判定の行なし} (rc=$RC)"
echo "旗 0 の生（cearn_s の末尾に新しい列 importBudgetExcessMaxPerClearingSubLive つき・cpop_s は列が変わらない）：$(ls -d D:/PoB_work/ident_c74id/out_ib*/ins 2>/dev/null | head -1)（ident の作業フォルダ D:/PoB_work/ident_c74id）"
echo "HEAD 測る前 $H0・後 $(git rev-parse HEAD)"
[ "$K" = 恒等 ] && python -X utf8 -I "$SP/live880.py" "$(ls -d D:/PoB_work/ident_c74id/out_ib*/ins 2>/dev/null | head -1)" 2>&1 | tail -8
cat "$OUT"
exit $RC
