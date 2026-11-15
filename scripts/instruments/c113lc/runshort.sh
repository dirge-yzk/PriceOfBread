#!/usr/bin/env bash
# (1152) c113lc 短い走り（2 種・四半期 268＝1850 年まで・railspend_s と disccontact_s）。
#   bash runshort.sh <木> <出力フォルダ> <POB_COEF>
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
export TEMP=D:/PoB_work/c113lc_work/tmp TMP=D:/PoB_work/c113lc_work/tmp TMPDIR=/d/PoB_work/c113lc_work/tmp
T="${1:?木}"; O="${2:?出力}"; CO="${3:?係数}"
mkdir -p "$O/ins" "$TEMP"
export POB_COEF="$CO" POB_INSTRUMENTS="$O/ins" POB_INSTRUMENTS_TABLES="railspend_s,disccontact_s" POB_INSTRUMENTS_NATIONS=""
SNAP_DIR="$O/snap" bash "$T/measure.sh" "$O/b2.txt" selfplay-batch 2 - 0 1 268 > "$O/ms.log" 2>&1
RC=$?
echo "rc=$RC" > "$O/rc.txt"
rm -rf "$O/snap"
exit $RC
