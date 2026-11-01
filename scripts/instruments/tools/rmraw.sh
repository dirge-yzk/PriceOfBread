#!/usr/bin/env bash
# (728) 自分の作った出力のフォルダだけを消す台本（rm -rf を担当に書かせない）：
#   bash rmraw.sh <フォルダ> [要約の出力ファイル]
#   ・/d/PoB_work（か D:/PoB_work）の下で、keep_ で始まる名前・branch_backup・PoB_play・作業フォルダ自身（直下 1 段）は拒む。
#   ・要約の出力ファイルが渡されたとき、消す前に keepsum.py で要約を書く（フォルダの直下のファイルだけ・ins/ があればその中身も別に）。要約を書けなければ消さない。
P="${1:?フォルダ}"; S="${2:-}"
case "$P" in [dD]:/*) P="/$(echo "${P:0:1}" | tr A-Z a-z)${P:2}" ;; esac
R=$(cd "$P" 2>/dev/null && pwd) || { echo "無いフォルダ：$1"; exit 1; }
case "$R" in /d/PoB_work/*) ;; *) echo "拒む：/d/PoB_work の外 $R"; exit 1 ;; esac
B=$(basename "$R"); REL=${R#/d/PoB_work/}
case "$REL" in */*) ;; *) case "$B" in keep_*|branch_backup|PoB_play|wt_*|.arms) echo "拒む：保護された名前 $B"; exit 1 ;; esac ;; esac
case "$REL" in keep_*|branch_backup*|PoB_play*|wt_*) echo "拒む：保護された場所 $REL"; exit 1 ;; esac
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -n "$S" ]; then
  T="$R"; [ -d "$R/ins" ] && T="$R/ins"
  python -I "$SP/keepsum.py" "$T" "$S" || { echo "要約を書けない＝消さない"; exit 1; }
fi
rm -rf "$R" && echo "消した：$R"
