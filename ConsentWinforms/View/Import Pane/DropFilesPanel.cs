﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using StripV3Consent.Model;

namespace StripV3Consent.View
{
	class DropFilesPanel : Panel
	{
		public FileList<ImportFileItem, ImportFile> FileList = new FileList<ImportFileItem, ImportFile>();

		public DropFilesPanel()
		{

			AllowDrop = true;
			DragEnter += DropFiles_DragEnter;
			DragDrop += DropFiles_DragDrop;
		}


        private void DropFiles_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Move;
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		/// <summary>
		/// Provides preliminary validation that a file can be safely loaded into the program
		/// </summary>
		/// <param name="FilePath"></param>
		/// <returns></returns>
		private bool IsFileValidForDropping(string FilePath)
        {
			FileAttributes Attributes = File.GetAttributes(FilePath);
			if (Attributes == FileAttributes.Directory | Attributes == FileAttributes.Device) //Prevent doctors from trying to drag and drop folders, devices and all sorts of nonsense
			{
				MessageBox.Show("Sorry but we don't support that type of file");
				return false;
            }

			const long MaxFileSize = 10000000; //10MB
			long FileSize = new FileInfo(FilePath).Length;
			if (FileSize > MaxFileSize)
            {
				MessageBox.Show($"That file is too large for this program to handle (Size was {FileSize} and Maximum Size is {MaxFileSize}");
				return false;
            }

			return true;
		}

		/// <summary>
		/// Takes any directories in Paths and replaces them with their contents
		/// </summary>
		/// <param name="Paths">A list of strings indicating paths indicating files or directories</param>
		/// <returns></returns>
		private void RecursivelyExpandFolders(List<string> Paths)
		{
			do
			{
				Func<string, bool> IsDirectory = path => File.GetAttributes(path) == FileAttributes.Directory;

				string[] Directories = Paths.Where(IsDirectory).ToArray();	//Cast to string[] rather than keep as IEnumerable<string> otherwise it will be lost on the next line
				Paths.RemoveAll(new Predicate<string>(IsDirectory));

				IEnumerable<string> NewFiles = Directories.SelectMany(DirectoryPath => new DirectoryInfo(DirectoryPath).GetFiles())
														  .Select(FileAttr => FileAttr.FullName);

				Paths.AddRange(NewFiles);
			} while (Paths.Select(path => File.GetAttributes(path)).Any(attr => attr == FileAttributes.Directory));
		}

		private void DropFiles_DragDrop(object sender, DragEventArgs e)
		{
			List<string> InputPaths = ((string[])e.Data.GetData(DataFormats.FileDrop)).ToList<string>();
			RecursivelyExpandFolders(InputPaths);

			string[] FilePaths = InputPaths.Where(path => IsFileValidForDropping(path)).ToArray(); //Prevent doctors from trying to drag and drop folders, devices and all sorts of nonsense
			this.Controls.Clear();
			this.Controls.Add(FileList);
			FileList.AddRange(FilePaths.Select<string, ImportFile>(Path => {
				string FileContents = null;
				using (StreamReader reader = new StreamReader(Path))
				{
					FileContents = reader.ReadToEnd();
				}
				return new ImportFile(System.IO.Path.GetFileName(Path), FileContents) {FilePath = Path };
			}).ToArray());

		}


		public string MostCommonSourceDirectory {
			get => FileList.Files.Select(ImportFile => ImportFile.FilePath).Where(Path => Path != null).Select(Path => System.IO.Path.GetDirectoryName(Path))
									.GroupBy(Path => Path)
									.OrderBy(t => t.Count())
									.First().Key;
		}


	}
}
