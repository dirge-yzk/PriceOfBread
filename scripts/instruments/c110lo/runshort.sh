#!/usr/bin/env bash
# (1130) c110lo 短い走り（2 種・四半期 268＝1850 年まで・railspend_s だけ）。突然変異と旗 1 の短い走りに使う。
#   bash runshort.sh <木> <出力フォルダ> <旗の値 0|1> [変異の名]
#   変異の名を渡すと、<木> を使い捨ての写しと見なして muts.py を当ててから回す（<木> は git archive で作った写し）。
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
export TEMP=D:/PoB_work/c110lo_work/tmp TMP=D:/PoB_work/c110lo_work/tmp TMPDIR=/d/PoB_work/c110lo_work/tmp
T="${1:?木}"; O="${2:?出力}"; FLAG="${3:?旗}"; MU="${4:-}"
S="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p "$O/ins" "$TEMP"
if [ -n "$MU" ]; then python -X utf8 -I "$S/muts.py" "$T" "$MU" > "$O/apply.txt" 2>&1 || { echo "apply-fail" > "$O/rc.txt"; exit 2; }; fi
export POB_COEF="RailLocoNeedsDiscovery=$FLAG" POB_INSTRUMENTS="$O/ins" POB_INSTRUMENTS_TABLES="railspend_s" POB_INSTRUMENTS_NATIONS=""
SNAP_DIR="$O/snap" bash "$T/measure.sh" "$O/b2.txt" selfplay-batch 2 - 0 1 268 > "$O/ms.log" 2>&1
RC=$?
echo "rc=$RC" > "$O/rc.txt"
rm -rf "$O/snap"
exit $RC
