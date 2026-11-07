#!/usr/bin/env bash
# (937) c82lt 印 ident.mark の中身：ident c82ltz を回し、.log の先頭に  VERDICT ident 恒等|否 <理由>  と生の場所を書く。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c82lt_work/ident.mark bash scripts/instruments/c82lt/ident937.sh
#   宣言＝--added-tables landfix_s（新しい表）・--tests-delta 16（LandTaxFixedBasisTests 13 件と LandfixInstrumentTests 3 件・形の変え直しの後）・--saveleak（savetest と leakhunt を回す指定）。旗 LandTaxFixedBasis は 0（器のコミットの木）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
OUT=D:/PoB_work/c82lt_work/ident_out.txt
cd "$REPO" || exit 9
H0=$(git rev-parse HEAD)
bash scripts/instruments/ident/ident.sh run c82ltz --tree "$REPO" --added-tables landfix_s --tests-delta 16 --saveleak > "$OUT" 2>&1
RC=$?
V=$(grep -E "判定:" "$OUT" | tail -1)
case "$V" in *恒等*) K=恒等 ;; *) K=否 ;; esac
echo "VERDICT ident $K ${V:-判定の行なし} (rc=$RC)"
echo "生：$(ls -d D:/PoB_work/ident_c82ltz/out_ib*/ins 2>/dev/null | head -1)（ident の作業フォルダ D:/PoB_work/ident_c82ltz）"
echo "HEAD 測る前 $H0・後 $(git rev-parse HEAD)"
cat "$OUT"
exit $RC
