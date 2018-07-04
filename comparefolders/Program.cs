using System;
using System.IO;
using System.Text.RegularExpressions;

namespace comparefolders
{
    internal class Program
    {
        private static int missingFilesCount;
        private static int differentFilesCount;
        private static int missingDirectoriesCount;
        private static Regex ignorePatternRegex;
        private static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                PrintInstructions();
            }
            else
            {
                string flags = args[0].ToUpper();
                missingFilesCount = 0;
                differentFilesCount = 0;
                missingDirectoriesCount = 0;
                ignorePatternRegex = args[4].ToUpper() == "NONE" ? null : new Regex(WildcardToRegex(args[4]), RegexOptions.IgnoreCase);

                Console.WriteLine($"\nScan from:  {args[1]}");
                Console.WriteLine($"Compare to: {args[2]}");
                Console.WriteLine($"Flags:\t{flags}");
                Console.WriteLine($"Match:\t{args[3]}");
                Console.WriteLine($"Ignore:\t{args[4]} (regex: {(ignorePatternRegex != null ? ignorePatternRegex.ToString() : "NONE")})");
                Console.WriteLine($"Start:\t{DateTime.Now.ToString()}");

                RenameOldReports();

                TimeSpan t1 = new TimeSpan(DateTime.UtcNow.Ticks);
                Comparefolders(
                    args[1], // directory to scan
                    args[2], // directory to compare
                    args[3], // Match pattern
                    ref ignorePatternRegex,  // Ignore pattern
                    flags.Contains("L"), // Consider or not lenght when comparing files
                    flags.Contains("W"), // Consider or not last write time when comparing files
                    flags.Contains("D"), // Check only for directories
                    flags.Contains("R")  // Check subdirectories (or not)
                    );
                TimeSpan t2 = new TimeSpan(DateTime.UtcNow.Ticks);

                Console.WriteLine();
                Console.Write($"End:\t{DateTime.Now.ToString()}\t");
                Console.WriteLine($"Duration: {(t2 - t1).ToString(@"hh\:mm\:ss")}");
                if ((missingFilesCount + differentFilesCount + missingDirectoriesCount) == 0)
                {
                    Console.WriteLine($"All {(flags.Contains("D") ? "directories" : "files")} exist on target");
                    if (!flags.Contains("R"))
                    {
                        Console.WriteLine("Only first level of scanned directory was analyzed. Did not scan subfolders.");
                    }
                }

            }
        }
        public static void Comparefolders(
            string f1, string f2,
            string matchPattern, ref Regex ignorePattern,
            bool compareLenght, bool compareWriteTime,
            bool directoriesOnly, bool recurse)
        {

            if (directoriesOnly)
            {
                foreach (string dirLeft in Directory.EnumerateDirectories(f1, matchPattern, SearchOption.TopDirectoryOnly))
                {
                    string dirRight = string.Concat(f2, dirLeft.Substring(f1.Length));
                    if (ignorePattern != null)
                    {
                        if (!ignorePattern.IsMatch(dirLeft))
                        {
                            if (!Directory.Exists(dirRight))
                            {
                                missingDirectoriesCount++;
                                Console.Write($"Missing directories: {missingDirectoriesCount.ToString()}\r");
                                File.AppendAllText(@".\MissingDirs.txt", dirLeft + "\r\n");
                            }
                        }
                    }
                    else
                    {
                        if (!Directory.Exists(dirRight))
                        {
                            missingDirectoriesCount++;
                            Console.Write($"Missing directories: {missingDirectoriesCount.ToString()}\r");
                            File.AppendAllText(@".\MissingDirs.txt", dirLeft + "\r\n");
                        }
                    }
                }
            }
            else
            {
                string file2;
                foreach (string file1 in Directory.EnumerateFiles(f1, matchPattern, SearchOption.TopDirectoryOnly))
                {
                    file2 = string.Concat(f2, file1.Substring(f1.Length));
                    if (ignorePattern != null)
                    {
                        if (!ignorePattern.IsMatch(file1))
                        {
                            CompareTheseFiles(file1, file2, compareLenght, compareWriteTime);
                        }
                    }
                    else
                    {
                        CompareTheseFiles(file1, file2, compareLenght, compareWriteTime);
                    }
                }
            }

            if (recurse)
            {
                foreach (string dirLeft in Directory.EnumerateDirectories(f1, matchPattern, SearchOption.TopDirectoryOnly))
                {
                    string dirRight = string.Concat(f2, dirLeft.Substring(f1.Length));
                    Comparefolders(dirLeft, dirRight, matchPattern, ref ignorePattern, compareLenght, compareWriteTime, directoriesOnly, true);
                }
            }

        }
        private static void CompareTheseFiles(string file1, string file2, bool compareLenght, bool compareWriteTime)
        {
            if (!File.Exists(file2)) // check if file1 exists on target directory as file2
            {
                missingFilesCount++;
                File.AppendAllText(@".\MissingFiles.txt", file1 + "\r\n");
                if (compareLenght || compareWriteTime)
                {
                    Console.Write($"Missing files: {missingFilesCount.ToString()}\tDifferent files: {differentFilesCount.ToString()}\r");
                }
                else
                {
                    Console.Write($"Missing files: {missingFilesCount.ToString()}\r");
                }
            }
            else
            {
                // File exists. Compare size and/or last write time?
                if (compareLenght || compareWriteTime)
                {
                    FileInfo fi1 = new FileInfo(file1);
                    FileInfo fi2 = new FileInfo(file2);

                    string flags = string.Concat(
                            (fi1.Length != fi2.Length) ? "S" : " ",
                            (fi1.LastWriteTimeUtc != fi2.LastWriteTimeUtc) ? "W" : " ");

                    if (compareLenght && compareWriteTime)
                    {
                        if ((fi1.Length != fi2.Length) || (fi1.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
                        {
                            differentFilesCount++;
                            File.AppendAllText(@".\DifferentFiles.txt", $"{flags} {file1}\r\n");
                            Console.Write(
                            $"Missing files: {missingFilesCount.ToString()}\tDifferent files: {differentFilesCount.ToString()}\r");

                        }
                    }
                    else
                    {
                        if (compareLenght && (fi1.Length != fi2.Length))
                        {
                            differentFilesCount++;
                            File.AppendAllText(@".\DifferentFiles.txt", $"{flags} {file1}\r\n");
                            Console.Write(
                            $"Missing files: {missingFilesCount.ToString()}\tDifferent files: {differentFilesCount.ToString()}\r");

                        }
                        if (compareWriteTime && (fi1.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
                        {
                            differentFilesCount++;
                            File.AppendAllText(@".\DifferentFiles.txt", $"{flags} {file1}\r\n");
                            Console.Write(
                            $"Missing files: {missingFilesCount.ToString()}\tDifferent files: {differentFilesCount.ToString()}\r");

                        }
                    }
                }
            }
        }
        private static void RenameOldReports()
        {
            if (File.Exists(@".\MissingDirs.txt"))
            {
                string tm = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                File.Move(@".\MissingDirs.txt", $@".\MissingDirs_{tm}.txt");
            }
            if (File.Exists(@".\MissingFiles.txt"))
            {
                string tm = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                File.Move(@".\MissingFiles.txt", $@".\MissingFiles_{tm}.txt");
            }
            if (File.Exists(@".\DifferentFiles.txt"))
            {
                string tm = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                File.Move(@".\DifferentFiles.txt", $@".\DifferentFiles_{tm}.txt");
            }
        }
        private static void PrintInstructions()
        {
            Console.WriteLine();
            Console.WriteLine(@"comparefolders flags dir1 dir2 matchPattern ignorePattern

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
    comparefolders FR ""\\nas01\my Home"" ""\\nas03\backup\my Home"" *.* thumbs.db
    comparefolders FRWL e:\myHome \\nas03\backup\myHome *.* *.db
    comparefolders FRL e:\myHome \\nas03\backup\myHome *.* none
if ignorePattern is 'NONE' all items will be considered.

If you want to see if there are extra files on dir2 you must run the same command switching the order:
1)
    Check for missing files on ""\\nas03\backup\myHome"":
    comparefolders FR e:\myHome \\nas03\backup\myHome *.* none
2)
    Now check for missing files on ""e:\\myHome"":
    comparefolders FR \\nas03\backup\myHome e:\myHome *.* none


");
        }
        private static string WildcardToRegex(string pattern)
        {
            // https://www.codeproject.com/Articles/11556/Converting-Wildcards-to-Regexes
            return Regex.Escape(pattern)
                              .Replace(@"\*", ".*")
                              .Replace(@"\?", ".")
                       + "$";
            /*
                Original:
                return "^" + Regex.Escape(pattern)
                                  .Replace(@"\*", ".*")
                                  .Replace(@"\?", ".")
                           + "$";
                 https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
            */
        }

    }
}
