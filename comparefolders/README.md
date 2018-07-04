

comparefolders flags dir1 dir2 matchPattern ignorePattern

FLAGS:
  D - Consider only directories
  F - Consider files
  R - Recursive
  L - Consider file length when comparing files
  W - Consider last write time when comparing files

  If D is supplied files will be ignored, even if F is present.
  If neither D nor F are present, F is assumed.

  When comparing files, if none of the flags L or W is supplied, only the existence of the file is checked.
  The order of the flags is irrelevant.

Examples:
    comparefolders FR "\\nas01\my Home" "\\nas03\backup\my Home" *.* thumbs.db
    comparefolders FRWL e:\myHome \\nas03\backup\myHome *.* *.db
    comparefolders FRL e:\myHome \\nas03\backup\myHome *.* none
if ignorePattern is 'NONE' all items will be considered.

If you want to see if there are extra files on dir2 you must run the same command switching the order:
1)
    Check for missing files on "\\nas03\backup\myHome":
    comparefolders FR e:\myHome \\nas03\backup\myHome *.* none
2)
    Now check for missing files on "e:\\myHome":
    comparefolders FR \\nas03\backup\myHome e:\myHome *.* none

