#!/usr/bin/env bash
# (1398) c149sd 旗 0 の木で ㉘ の走査の番人と突然変異を続けて回す（bg.sh で起こす）。bash pre_sd.sh <コミット>
C=${1:?}; D0="$(cd "$(dirname "$0")" && pwd)"
bash $D0/scan_sd.sh D:/PoB_work/c149sd_work/scan0; RS=$?
bash $D0/mut_sd.sh $C D:/PoB_work/wt_c149sd/measurements/m1398_20261127/mut
echo "scan rc=$RS"; exit $RS
