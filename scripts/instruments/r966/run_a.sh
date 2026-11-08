cd /d/PoB_work/r966_work
python -X utf8 -I D:/PoB_work/r966_work/r966_ef.py D:/PoB_work/r966_work/m961 b961=D:/PoB_work/keep_961/base_ins:1:10 n1_961=D:/PoB_work/keep_961/n1:1:10 n2_961=D:/PoB_work/keep_961/n2:1:10 a11_961=D:/PoB_work/keep_961/a11:11:20 || exit 1
python -X utf8 -I D:/PoB_work/r966_work/r966_chain.py D:/PoB_work/r966_work/m961_pairs.tsv D:/PoB_work/r966_work/m961_chain.tsv b961=D:/PoB_work/keep_961/base_ins n1_961=D:/PoB_work/keep_961/n1 n2_961=D:/PoB_work/keep_961/n2 a11_961=D:/PoB_work/keep_961/a11 || exit 1
python -X utf8 -I D:/PoB_work/r966_work/r966_chain.py --case D:/PoB_work/keep_961/n2 3 共和制イギリス 400 667 4 D:/PoB_work/r966_work/case_n2_961_s3_rep_uk.tsv || exit 1
python -X utf8 -I D:/PoB_work/r966_work/r966_chain.py --case D:/PoB_work/keep_961/n1 6 ネーデルラント 520 667 4 D:/PoB_work/r966_work/case_n1_961_s6_nl.tsv
