﻿using GVFS.FunctionalTests.FileSystemRunners;
using GVFS.FunctionalTests.Should;
using GVFS.FunctionalTests.Tools;
using GVFS.Tests.Should;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace GVFS.FunctionalTests.Tests.EnlistmentPerTestCase
{
    [TestFixture]
    public class ModifiedPathsTests : TestsWithEnlistmentPerTestCase
    {
        private static readonly string FileToAdd = Path.Combine("GVFS", "TestAddFile.txt");
        private static readonly string FileToUpdate = Path.Combine("GVFS", "GVFS", "Program.cs");
        private static readonly string FileToDelete = "Readme.md";
        private static readonly string FileToRename = Path.Combine("GVFS", "GVFS.Mount", "MountVerb.cs");
        private static readonly string RenameFileTarget = Path.Combine("GVFS", "GVFS.Mount", "MountVerb2.cs");
        private static readonly string FolderToCreate = "PersistedSparseExcludeTests_NewFolder";
        private static readonly string FolderToRename = "PersistedSparseExcludeTests_NewFolderForRename";
        private static readonly string RenameFolderTarget = "PersistedSparseExcludeTests_NewFolderForRename2";
        private static readonly string DotGitFileToCreate = Path.Combine(".git", "TestFileFromDotGit.txt");
        private static readonly string RenameNewDotGitFileTarget = "TestFileFromDotGit.txt";
        private static readonly string FileToCreateOutsideRepo = "PersistedSparseExcludeTests_outsideRepo.txt";
        private static readonly string FolderToCreateOutsideRepo = "PersistedSparseExcludeTests_outsideFolder";
        private static readonly string FolderToDelete = "Scripts";
        private static readonly string ExpectedModifiedFilesContentsAfterRemount =
@"A .gitattributes
A GVFS/TestAddFile.txt
A GVFS/GVFS/Program.cs
A Readme.md
A GVFS/GVFS.Mount/MountVerb.cs
A GVFS/GVFS.Mount/MountVerb2.cs
A PersistedSparseExcludeTests_NewFolder/
A PersistedSparseExcludeTests_NewFolderForRename/
A PersistedSparseExcludeTests_NewFolderForRename2/
A TestFileFromDotGit.txt
A PersistedSparseExcludeTests_outsideRepo.txt
A PersistedSparseExcludeTests_outsideFolder/
A Scripts/CreateCommonAssemblyVersion.bat
A Scripts/CreateCommonCliAssemblyVersion.bat
A Scripts/CreateCommonVersionHeader.bat
A Scripts/RunFunctionalTests.bat
A Scripts/RunUnitTests.bat
A Scripts/
";

        [Category(Categories.MacTODO.M2)]
        [TestCaseSource(typeof(FileSystemRunner), FileSystemRunner.TestRunners)]
        public void ModifiedPathsSavedAfterRemount(FileSystemRunner fileSystem)
        {
            string fileToAdd = this.Enlistment.GetVirtualPathTo(FileToAdd);
            fileSystem.WriteAllText(fileToAdd, "Contents for the new file");

            string fileToUpdate = this.Enlistment.GetVirtualPathTo(FileToUpdate);
            fileSystem.AppendAllText(fileToUpdate, "// Testing");

            string fileToDelete = this.Enlistment.GetVirtualPathTo(FileToDelete);
            fileSystem.DeleteFile(fileToDelete);
            fileToDelete.ShouldNotExistOnDisk(fileSystem);

            string fileToRename = this.Enlistment.GetVirtualPathTo(FileToRename);
            fileSystem.MoveFile(fileToRename, this.Enlistment.GetVirtualPathTo(RenameFileTarget));

            string folderToCreate = this.Enlistment.GetVirtualPathTo(FolderToCreate);
            fileSystem.CreateDirectory(folderToCreate);

            string folderToRename = this.Enlistment.GetVirtualPathTo(FolderToRename);
            fileSystem.CreateDirectory(folderToRename);
            string folderToRenameTarget = this.Enlistment.GetVirtualPathTo(RenameFolderTarget);
            fileSystem.MoveDirectory(folderToRename, folderToRenameTarget);

            // Moving the new folder out of the repo should not change the always_exclude file
            string folderTargetOutsideSrc = Path.Combine(this.Enlistment.EnlistmentRoot, RenameFolderTarget);
            folderTargetOutsideSrc.ShouldNotExistOnDisk(fileSystem);
            fileSystem.MoveDirectory(folderToRenameTarget, folderTargetOutsideSrc);
            folderTargetOutsideSrc.ShouldBeADirectory(fileSystem);
            folderToRenameTarget.ShouldNotExistOnDisk(fileSystem);

            // Moving a file from the .git folder to the working directory should add the file to the sparse-checkout
            string dotGitfileToAdd = this.Enlistment.GetVirtualPathTo(DotGitFileToCreate);
            fileSystem.WriteAllText(dotGitfileToAdd, "Contents for the new file in dot git");
            fileSystem.MoveFile(dotGitfileToAdd, this.Enlistment.GetVirtualPathTo(RenameNewDotGitFileTarget));

            // Move a file from outside of src into src
            string fileToCreateOutsideRepoPath = Path.Combine(this.Enlistment.EnlistmentRoot, FileToCreateOutsideRepo);
            fileSystem.WriteAllText(fileToCreateOutsideRepoPath, "Contents for the new file outside of repo");
            string fileToCreateOutsideRepoTargetPath = this.Enlistment.GetVirtualPathTo(FileToCreateOutsideRepo);
            fileToCreateOutsideRepoTargetPath.ShouldNotExistOnDisk(fileSystem);
            fileSystem.MoveFile(fileToCreateOutsideRepoPath, fileToCreateOutsideRepoTargetPath);
            fileToCreateOutsideRepoTargetPath.ShouldBeAFile(fileSystem);
            fileToCreateOutsideRepoPath.ShouldNotExistOnDisk(fileSystem);

            // Move a folder from outside of src into src
            string folderToCreateOutsideRepoPath = Path.Combine(this.Enlistment.EnlistmentRoot, FolderToCreateOutsideRepo);
            fileSystem.CreateDirectory(folderToCreateOutsideRepoPath);
            folderToCreateOutsideRepoPath.ShouldBeADirectory(fileSystem);
            string folderToCreateOutsideRepoTargetPath = this.Enlistment.GetVirtualPathTo(FolderToCreateOutsideRepo);
            folderToCreateOutsideRepoTargetPath.ShouldNotExistOnDisk(fileSystem);
            fileSystem.MoveDirectory(folderToCreateOutsideRepoPath, folderToCreateOutsideRepoTargetPath);
            folderToCreateOutsideRepoTargetPath.ShouldBeADirectory(fileSystem);
            folderToCreateOutsideRepoPath.ShouldNotExistOnDisk(fileSystem);

            string folderToDelete = this.Enlistment.GetVirtualPathTo(FolderToDelete);
            fileSystem.DeleteDirectory(folderToDelete);
            folderToDelete.ShouldNotExistOnDisk(fileSystem);

            // Remount
            this.Enlistment.UnmountGVFS();
            this.Enlistment.MountGVFS();

            this.Enlistment.WaitForBackgroundOperations().ShouldEqual(true, "Background operations failed to complete.");

            string modifiedPathsDatabase = Path.Combine(this.Enlistment.DotGVFSRoot, TestConstants.Databases.ModifiedPaths);
            modifiedPathsDatabase.ShouldBeAFile(fileSystem);
            using (StreamReader reader = new StreamReader(File.Open(modifiedPathsDatabase, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.ReadToEnd().ShouldEqual(ExpectedModifiedFilesContentsAfterRemount);
            }
        }

        [TestCaseSource(typeof(HardLinkRunners), HardLinkRunners.TestRunners)]
        public void ModifiedPathsCorrectAfterHardLinking(FileSystemRunner fileSystem)
        {
            const string ExpectedModifiedFilesContentsAfterHardlinks =
@"A .gitattributes
A LinkToReadme.md
A LinkToFileOutsideSrc.txt
";

            // Create a link from src\LinkToReadme.md to src\Readme.md
            string existingFileInRepoPath = this.Enlistment.GetVirtualPathTo("Readme.md");
            string contents = existingFileInRepoPath.ShouldBeAFile(fileSystem).WithContents();
            string hardLinkToFileInRepoPath = this.Enlistment.GetVirtualPathTo("LinkToReadme.md");
            hardLinkToFileInRepoPath.ShouldNotExistOnDisk(fileSystem);
            fileSystem.CreateHardLink(hardLinkToFileInRepoPath, existingFileInRepoPath);
            hardLinkToFileInRepoPath.ShouldBeAFile(fileSystem).WithContents(contents);

            // Create a link from src\LinkToFileOutsideSrc.txt to FileOutsideRepo.txt
            string fileOutsideOfRepoPath = Path.Combine(this.Enlistment.EnlistmentRoot, "FileOutsideRepo.txt");
            string fileOutsideOfRepoContents = "File outside of repo";
            fileOutsideOfRepoPath.ShouldNotExistOnDisk(fileSystem);
            fileSystem.WriteAllText(fileOutsideOfRepoPath, fileOutsideOfRepoContents);
            string hardLinkToFileOutsideRepoPath = this.Enlistment.GetVirtualPathTo("LinkToFileOutsideSrc.txt");
            hardLinkToFileOutsideRepoPath.ShouldNotExistOnDisk(fileSystem);
            fileSystem.CreateHardLink(hardLinkToFileOutsideRepoPath, fileOutsideOfRepoPath);
            hardLinkToFileOutsideRepoPath.ShouldBeAFile(fileSystem).WithContents(fileOutsideOfRepoContents);

            // Create a link from LinkOutsideSrcToInsideSrc.cs to src\GVFS\GVFS\Program.cs
            string secondFileInRepoPath = this.Enlistment.GetVirtualPathTo("GVFS", "GVFS", "Program.cs");
            contents = secondFileInRepoPath.ShouldBeAFile(fileSystem).WithContents();
            string hardLinkOutsideRepoToFileInRepoPath = Path.Combine(this.Enlistment.EnlistmentRoot, "LinkOutsideSrcToInsideSrc.cs");
            hardLinkOutsideRepoToFileInRepoPath.ShouldNotExistOnDisk(fileSystem);
            fileSystem.CreateHardLink(hardLinkOutsideRepoToFileInRepoPath, secondFileInRepoPath);
            hardLinkOutsideRepoToFileInRepoPath.ShouldBeAFile(fileSystem).WithContents(contents);

            string modifiedPathsDatabase = Path.Combine(this.Enlistment.DotGVFSRoot, TestConstants.Databases.ModifiedPaths);
            modifiedPathsDatabase.ShouldBeAFile(fileSystem);
            using (StreamReader reader = new StreamReader(File.Open(modifiedPathsDatabase, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                reader.ReadToEnd().ShouldEqual(ExpectedModifiedFilesContentsAfterHardlinks);
            }
        }

        private class HardLinkRunners
        {
            public const string TestRunners = "Runners";

            public static object[] Runners
            {
                get
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        return new[]
                        {
                            new object[] { new CmdRunner() },
                            new object[] { new BashRunner() },
                        };
                    }

                    return new[] { new object[] { new BashRunner() } };
                }
            }
        }
    }
}