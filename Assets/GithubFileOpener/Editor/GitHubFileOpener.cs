#nullable enable

using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace GitHubFileOpener
{
	public static class GitHubFileOpener
	{
		[MenuItem("Assets/Open In GitHub", false, 1000)]
		static void OpenInGithub()
		{
			var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (string.IsNullOrEmpty(selectedPath))
			{
				EditorUtility.DisplayDialog("Error", "No valid file selected.", "OK");
				return;
			}

			var relativePath = GetRelativePath(selectedPath);
			Debug.Log($"relativePath = {relativePath}");
			var branchName = GetCurrentBranchName();
			Debug.Log($"branchName = {branchName}");
			var remoteUrl = GetRemoteUrl();
			Debug.Log($"remoteUrl = {remoteUrl}");

			if (string.IsNullOrEmpty(branchName) || string.IsNullOrEmpty(remoteUrl))
			{
				EditorUtility.DisplayDialog("Error", "Failed to retrieve Git information.", "OK");
				return;
			}

			var githubUrl = GenerateGithubUrl(remoteUrl!, branchName!, relativePath);

			if (!string.IsNullOrEmpty(githubUrl))
			{
				Debug.Log($"Open: {githubUrl}");
				Application.OpenURL(githubUrl);
			}
			else
			{
				EditorUtility.DisplayDialog("Error", "Failed to generate GitHub URL.", "OK");
			}
		}

		[MenuItem("Assets/Open on GitHub", true)]
		static bool ValidateOpenOnGithub()
		{
			var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			return !string.IsNullOrEmpty(selectedPath);
		}

		static string? GetCurrentBranchName()
		{
			return RunGitCommand("rev-parse --abbrev-ref HEAD");
		}

		static string? GetRemoteUrl()
		{
			var url = RunGitCommand("config --get remote.origin.url");
			if (string.IsNullOrEmpty(url)) return url;

			url = url!.Trim();

			// Normalize URL for GitHub
			if (url.EndsWith(".git"))
			{
				url = url.Substring(0, url.Length - 4);
			}

			// Handle SSH URL conversion to HTTPS
			if (url.StartsWith("git@"))
			{
				url = url.Replace(":", "/").Replace("git@", "https://");
			}
			return url;
		}

		static string? RunGitCommand(string arguments)
		{
			try
			{
				var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "git",
						Arguments = arguments,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true,
						WorkingDirectory = Application.dataPath
					}
				};

				process.Start();
				var output = process.StandardOutput.ReadToEnd().Trim();
				process.WaitForExit();

				if (process.ExitCode == 0)
				{
					return output;
				}
				else
				{
					Debug.LogError($"Git error: {process.StandardError.ReadToEnd()}");
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"Exception while running Git command: {ex}");
			}

			return null;
		}

		// リポジトリのルートディレクトリを取得
		static string GetRepositoryRoot()
		{
			var repoRoot = RunGitCommand("rev-parse --show-toplevel");
			if (!string.IsNullOrEmpty(repoRoot))
			{
				return repoRoot!.Trim().Replace("\\", "/");
			}
			return string.Empty;
		}

		// Unityのプロジェクトルート（Assetsフォルダ）からの相対パスを計算
		static string GetRelativePath(string absolutePath)
		{
			var projectRoot = GetRepositoryRoot(); // リポジトリのルート
			var assetsDirectoryFullPath = Path.GetFullPath(Application.dataPath).Replace("\\", "/"); // UnityのAssetsパス

			var unityProjectPath = assetsDirectoryFullPath.Replace(projectRoot, "")
				.Replace("\\", "/").Replace("Assets", "").Substring(1);
			Debug.Log($"unityProjectPath = {unityProjectPath}");

			return Path.Combine(unityProjectPath, absolutePath);
		}

		static string GenerateGithubUrl(string remoteUrl, string branchName, string relativePath)
		{
			return $"{remoteUrl}/blob/{branchName}/{relativePath}";
		}
	}
}
