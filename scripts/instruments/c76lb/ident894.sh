#!/usr/bin/env bash
# (894) c76lb 印 ident.mark の中身：ident c76lb を回し、.log の先頭に  VERDICT ident 恒等|否 <理由>  と R の照合の門（gate894.py）の結果を書く。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c76lb_work/ident.mark bash scripts/instruments/c76lb/ident894.sh
#   宣言＝--added-tables landbase_s,landbasereg_s・--tests-delta 13（LandbaseInstrumentTests の T1〜T13）・--saveleak（savetest と leakhunt を回す）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
OUT=D:/PoB_work/c76lb_work/ident_out.txt
cd "$REPO" || exit 9
H0=$(git rev-parse HEAD)
bash scripts/instruments/ident/ident.sh run c76lb --tree "$REPO" --added-tables landbase_s,landbasereg_s --tests-delta 13 --saveleak > "$OUT" 2>&1
RC=$?
V=$(grep -E "判定:" "$OUT" | tail -1)
case "$V" in *恒等*) K=恒等 ;; *) K=否 ;; esac
echo "VERDICT ident $K ${V:-判定の行なし} (rc=$RC)"
INS=$(ls -d D:/PoB_work/ident_c76lb/out_ib*/ins 2>/dev/null | head -1)
echo "旗 0 の生（landbase_s・landbasereg_s つき）：$INS（ident の作業フォルダ D:/PoB_work/ident_c76lb）"
echo "HEAD 測る前 $H0・後 $(git rev-parse HEAD)"
[ -n "$INS" ] && python -X utf8 "$SP/gate894.py" "$INS" D:/PoB_work/c76lb_work/gate894_ident.txt 2>&1 | tail -3
cat "$OUT"
exit $RC
