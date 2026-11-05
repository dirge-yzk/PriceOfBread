#!/usr/bin/env bash
# (852) 合流：採らなかった腕の生を、門と止める線の読み手が開く表だけに縮めて keep_852_rej/<名> へ移す（★(839) の型・keep_839_rej/abDW0_md_10 と同じ形）。
#   bash shrinkrej852.sh <腕の出力フォルダ（ins の親）> <keep_852_rej の下の名> <要約の出力（measurements/ の下）> <残す表の頭,…> [一緒に移すフォルダ…]
#   ①ins の全ての表の要約（sha256・バイト・行数・見出し）を先に書く（keepsum.py）＝書けなければ何もしない。
#   ②残す表と成績表（b10_*.txt・.cost・.manifest）を移す（同じドライブの名の付け替え＝中身は 1 バイトも変えない）。要約の写しも置く。
#   ③残りは rmraw.sh で消す（keep_* と D:/PoB_work の外は拒む）。
set -u
SRC="${1:?腕の出力フォルダ}"; NAME="${2:?名}"; SUM="${3:?要約}"; KEEP="${4:?残す表}"; shift 4
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DST=D:/PoB_work/keep_852_rej/$NAME
[ -d "$SRC/ins" ] || { echo "ins が無い：$SRC"; exit 1; }
[ -e "$DST" ] && { echo "もうある：$DST"; exit 1; }
python -I "$SP/tools/keepsum.py" "$SRC/ins" "$SUM" || { echo "要約を書けない＝何もしない"; exit 1; }
mkdir -p "$DST/ins" || exit 1
cp "$SUM" "$DST/keepsum_all_tables.tsv" || exit 1
IFS=',' read -r -a K <<< "$KEEP"
n=0
for t in "${K[@]}"; do
  for f in "$SRC/ins/${t}"[0-9]*.tsv; do
    [ -f "$f" ] || continue
    mv "$f" "$DST/ins/" || exit 1; n=$((n + 1))
  done
done
for f in "$SRC"/b10_*.txt "$SRC"/b10_*.txt.cost "$SRC"/b10_*.txt.manifest; do [ -f "$f" ] && mv "$f" "$DST/"; done
for d in "$@"; do   # フォルダは移す・ファイル（腕の作業フォルダの成績表など）は写す
  if [ -d "$d" ]; then mv "$d" "$DST/" && echo "移した：$d"; elif [ -f "$d" ]; then cp "$d" "$DST/" && echo "写した：$d"; fi
done
echo "移した表 $n 本 → $DST/ins"
bash "$SP/tools/rmraw.sh" "$SRC" || exit 1
du -sh "$DST"
echo SHRINKDONE
