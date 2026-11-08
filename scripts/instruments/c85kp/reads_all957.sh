#!/usr/bin/env bash
cd D:/PoB_work/wt_c85kp
bash scripts/instruments/c85kp/reads957.sh D:/PoB_work/c85kp_work/arm_c85kpB/out_c85kpB_10 D:/PoB_work/c85kp_work/arm_c85kpB/b10_c85kpB_10.txt D:/PoB_work/c85kp_work/readB 1; echo B rc=$?
bash scripts/instruments/c85kp/reads957.sh D:/PoB_work/c85kp_work/arm_c85kpS/out_c85kpS_10_s11 D:/PoB_work/c85kp_work/arm_c85kpS/b10_c85kpS_10_s11.txt D:/PoB_work/c85kp_work/readS 11; echo S rc=$?
