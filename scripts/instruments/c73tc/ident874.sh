#!/usr/bin/env bash
# (874) c73tc 印 f0_ready.mark の中身：ident c73tc を回し、.log の先頭に  VERDICT ident 恒等|否 <理由>  と旗 0 の生の場所を書く。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c73tc_work/f0_ready.mark bash scripts/instruments/c73tc/ident874.sh
#   宣言＝--grown-ok cearn_s（新しい 4 列）・--tests-delta 5（CearnTradeColumnsTests の 5 件）・--saveleak。他の表は全行一致を予言する。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
OUT=D:/PoB_work/c73tc_work/ident_out.txt
cd "$REPO" || exit 9
bash scripts/instruments/ident/ident.sh run c73tc --tree "$REPO" --grown-ok cearn_s --tests-delta 5 --saveleak > "$OUT" 2>&1
RC=$?
V=$(grep -E "判定:" "$OUT" | tail -1)
case "$V" in *恒等*) K=恒等 ;; *) K=否 ;; esac
echo "VERDICT ident $K ${V:-判定の行なし} (rc=$RC)"
echo "旗 0 の生（新しい列つきの cearn_s・cpop_s）：D:/PoB_work/ident_c73tc（ib のフォルダ）"
echo "HEAD $(git rev-parse HEAD)"
cat "$OUT"
exit $RC
