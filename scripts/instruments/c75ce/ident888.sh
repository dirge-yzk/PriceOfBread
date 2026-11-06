#!/usr/bin/env bash
# (888) c75ce 印 ident.mark の中身：ident c75ce を回し、.log の先頭に  VERDICT ident 恒等|否 <理由>  と旗 0 の恒等の門（gate888.py）の結果を書く。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c75ce_work/ident.mark bash scripts/instruments/c75ce/ident888.sh
#   宣言＝--added-tables cederule_s・--tests-delta 8（CedeRuleInstrumentTests の T1〜T8）・--saveleak。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
OUT=D:/PoB_work/c75ce_work/ident_out.txt
cd "$REPO" || exit 9
H0=$(git rev-parse HEAD)
bash scripts/instruments/ident/ident.sh run c75ce --tree "$REPO" --added-tables cederule_s --tests-delta 8 --saveleak > "$OUT" 2>&1
RC=$?
V=$(grep -E "判定:" "$OUT" | tail -1)
case "$V" in *恒等*) K=恒等 ;; *) K=否 ;; esac
echo "VERDICT ident $K ${V:-判定の行なし} (rc=$RC)"
INS=$(ls -d D:/PoB_work/ident_c75ce/out_ib*/ins 2>/dev/null | head -1)
echo "旗 0 の生（cederule_s つき）：$INS（ident の作業フォルダ D:/PoB_work/ident_c75ce）"
echo "HEAD 測る前 $H0・後 $(git rev-parse HEAD)"
[ -n "$INS" ] && python -X utf8 "$SP/gate888.py" "$INS" D:/PoB_work/c75ce_work/gate888_ident.txt 2>&1 | tail -3
cat "$OUT"
exit $RC
