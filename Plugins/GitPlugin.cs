using LibGit2Sharp;
using Microsoft.SemanticKernel;
using System.Text;

namespace SemanticKernelPlayground.Plugins
{
    internal class GitPlugin
    {
        private string? _repositoryPath;

        [KernelFunction]
        public string SetRepositoryPath(string path)
        {
            _repositoryPath = path;
            return $"Repository path set to: {_repositoryPath}";
        }

        [KernelFunction]
        public string GetLatestCommits(int count)
        {
            if (string.IsNullOrWhiteSpace(_repositoryPath) || !Repository.IsValid(_repositoryPath))
            {
                return "Invalid or missing repository path.";
            }

            using var repo = new Repository(_repositoryPath);
            var sb = new StringBuilder();

            foreach (var commit in repo.Commits.Take(count))
            {
                sb.AppendLine($"- {commit.MessageShort} ({commit.Author.Name}, {commit.Author.When:dd-MM-yyyy})");
            }

            return sb.ToString();
        }
    }
}